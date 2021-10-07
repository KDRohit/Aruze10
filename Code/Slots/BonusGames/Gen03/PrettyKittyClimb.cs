using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PrettyKittyClimb : ChallengeGame
{
	private const float TIME_BETWEEN_REVEALS = 0.5f;

	#region Static Members
	private static int[] MULTIPLIERS = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 15 };  
	#endregion Static Members

	#region Private Members
	private PickemOutcome outcome;
	private bool finishedRevealing = false;
	private int pickCount = 0;    
	private float lastTimeChanged;
	private SkippableWait revealWait = new SkippableWait();
	#endregion Private Members

	#region Public Members
	public UILabel[] revealTexts;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] revealTextsWrapperComponent;

	public List<LabelWrapper> revealTextsWrapper
	{
		get
		{
			if (_revealTextsWrapper == null)
			{
				_revealTextsWrapper = new List<LabelWrapper>();

				if (revealTextsWrapperComponent != null && revealTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in revealTextsWrapperComponent)
					{
						_revealTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in revealTexts)
					{
						_revealTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _revealTextsWrapper;
		}
	}
	private List<LabelWrapper> _revealTextsWrapper = null;	
	
	public UILabel[] loseLabels;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] loseLabelsWrapperComponent;

	public List<LabelWrapper> loseLabelsWrapper
	{
		get
		{
			if (_loseLabelsWrapper == null)
			{
				_loseLabelsWrapper = new List<LabelWrapper>();

				if (loseLabelsWrapperComponent != null && loseLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in loseLabelsWrapperComponent)
					{
						_loseLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in loseLabels)
					{
						_loseLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _loseLabelsWrapper;
		}
	}
	private List<LabelWrapper> _loseLabelsWrapper = null;	
	

	public UIButton[] buttonSelections;
	public GameObject[] buttonPickMeAnimations;  ///< This will be used once the assets are commited to the project, see lines 89 and 135 
	public GameObject[] buttonChosenEffects;
	public UISprite[] multiplierObjects;  

	public string gameOverSpriteName = "spray_bottle_m";

	public GameObject multiplierObjectTrail;

	public GameObject movingScoreObject;
	public UILabel movingScoreObjectText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent movingScoreObjectTextWrapperComponent;

	public LabelWrapper movingScoreObjectTextWrapper
	{
		get
		{
			if (_movingScoreObjectTextWrapper == null)
			{
				if (movingScoreObjectTextWrapperComponent != null)
				{
					_movingScoreObjectTextWrapper = movingScoreObjectTextWrapperComponent.labelWrapper;
				}
				else
				{
					_movingScoreObjectTextWrapper = new LabelWrapper(movingScoreObjectText);
				}
			}
			return _movingScoreObjectTextWrapper;
		}
	}
	private LabelWrapper _movingScoreObjectTextWrapper = null;
	
	public GameObject movingScoreObjectTrail;

	public GameObject combinationBurstObject;

	public UILabel winLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winLabelWrapperComponent;

	public LabelWrapper winLabelWrapper
	{
		get
		{
			if (_winLabelWrapper == null)
			{
				if (winLabelWrapperComponent != null)
				{
					_winLabelWrapper = winLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winLabelWrapper = new LabelWrapper(winLabel);
				}
			}
			return _winLabelWrapper;
		}
	}
	private LabelWrapper _winLabelWrapper = null;
	

	public float titleSwapFrequency = 4.5f;
	public GameObject infoObjectA;
	public GameObject infoObjectB;
	public GameObject infoObjectC;   
	#endregion Public Members

	private List<int> _pickedIndices = new List<int>();

	#region Public Methods
	public override void init() 
	{
		outcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];

		//Hide all pick labels
		foreach (LabelWrapper revealText in revealTextsWrapper)
		{
			revealText.alpha = 0;
		}

		movingScoreObjectTextWrapper.alpha = 0;
		winLabelWrapper.text = "0";

		lastTimeChanged = Time.time;
		this.StartCoroutine("swapping", this.doSwapping());        

		_didInit = true;
	}
	
	public void onPickSelected(GameObject button)
	{
		// Let's find which button index was clicked on.	
		int index = System.Array.IndexOf(buttonSelections, button.GetComponent<UIButton>());

		_pickedIndices.Add(index);

		//---------------------------------------------------------
		//Commented out until the pickme anims are on the objects
		//NGUITools.SetActive(buttonPickMeAnimations[index], false); ///< Lets turn off the flashy pickme animation
		//---------------------------------------------------------

		//Get the info
		PickemPick pick = outcome.getNextEntry();

		//Show spray bottle or reveal credits
		if (pick.isGameOver)
		{
			Audio.play("KC_tag_reveal_pooper");

			long winAmount = pick.credits;	// points are still awarded on losing pick
			loseLabelsWrapper[index].text = CreditsEconomy.convertCredits(winAmount);
			loseLabelsWrapper[index].gameObject.SetActive(true);

			revealTextsWrapper[index].alpha = 0;
			button.GetComponent<UISprite>().spriteName = gameOverSpriteName;
			button.GetComponent<UISprite>().MakePixelPerfect();
			for (int i = 0; i < buttonSelections.Length; i++)
			{
				buttonSelections[i].GetComponent<Collider>().enabled = false;
			}

			//Do credit roll
			long initialCredits = BonusGamePresenter.instance.currentPayout;
			BonusGamePresenter.instance.currentPayout += winAmount;
			StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, winLabelWrapper));

			this.StartCoroutine(this.revealRemainingPicks());
		}
		else
		{
			NGUITools.SetActive(button, false);
			revealTextsWrapper[index].text = CreditsEconomy.convertCredits(pick.credits); 
			this.StartCoroutine(this.revealWinningChoice(index, pick));            
		}
	}
	#endregion Public Methods
	
	#region Private Methods
	private IEnumerator revealRemainingPicks()
	{
		// Give a little time between the last pick and revealing the remaining.
		// Also prevents always automatically skipping reveals after the last pick.
		yield return new WaitForSeconds(.5f);
		
		PickemPick pick;
		UISprite buttonSprite;

		float meowTimer = Time.realtimeSinceStartup;

		for (int i = 0; i < revealTextsWrapper.Count; i++)
		{
			buttonSprite = buttonSelections[i].GetComponent<UISprite>();
			//Make sure this is a previously selected option
			if (revealTextsWrapper[i].alpha == 0 && buttonSprite.spriteName != gameOverSpriteName)
			{
				pick = outcome.getNextReveal();
				if (pick != null)
				{

					//---------------------------------------------------------
					//Commented out until the pickme anims are on the objects
					//NGUITools.SetActive(buttonPickMeAnimations[index], false);///< Turn off the remaining pickme animations as we reveal the distractors
					//---------------------------------------------------------                    

					revealTextsWrapper[i].alpha = 1;

					if (!pick.isGameOver)
					{
						revealTextsWrapper[i].color = Color.gray;
						revealTextsWrapper[i].text = CreditsEconomy.convertCredits(pick.credits);

						//Turn on and grey out yarn ball 
						buttonSprite.color = Color.grey;
						//NOTE: This does not match the web game very well this may need to be updated with new art during a second pass
					}
					else
					{
						revealTextsWrapper[i].alpha = 0;
						loseLabelsWrapper[i].text = CreditsEconomy.convertCredits(pick.credits);
						loseLabelsWrapper[i].gameObject.SetActive(true);
						loseLabelsWrapper[i].color = new Color(0.5f, 0, 0, 1f);
						buttonSprite.spriteName = gameOverSpriteName;
						buttonSprite.color = Color.grey;
						buttonSprite.MakePixelPerfect();
					}

					if(!revealWait.isSkipping)
					{
						Audio.play("reveal_others");
					}

					// Play a random meow sound.
					if (meowTimer <= Time.realtimeSinceStartup)
					{
						// Inclusive Min, exclusive Max.
						float rand = Random.Range(1, 3);
						Audio.play("symbol_kitty" + rand.ToString());
						// Set a timeout on the meowing so the sounds don't overlap.
						meowTimer = Time.realtimeSinceStartup + (rand == 1 ? 4 : 2);
					}

					yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
				}
			}
		}
		finishedRevealing = true;
		StartCoroutine(endGame(0.0f));
	}

	//This handles the reveal sequence for a winning pick
	private IEnumerator revealWinningChoice(int index, PickemPick pick)
	{
		//Keep the other buttons from being clicked until we are ready
		for (int i = 0; i < buttonSelections.Length; i++)
		{
			buttonSelections[i].GetComponent<Collider>().enabled = false;
		}

		//Do the yarn reveal animation
		buttonChosenEffects[index].SetActive(true);

		Audio.play("KC_tag_reveal");

		//Wait for the clip to be over
		yield return new WaitForSeconds(0.75f);

		if (!pick.isGameOver)
		{
			Audio.play("KC_tag_reveal_purr");
		}
		//Turn on and grey out yarn ball 
		buttonSelections[index].gameObject.SetActive(true);
		buttonSelections[index].GetComponent<UISprite>().color = Color.grey;///< NOTE: This does not match the web game very well this will need to be updated with new art during a second pass
		
		buttonChosenEffects[index].SetActive(false);        

		//Determine win amount and override title
		long winAmount = (MULTIPLIERS[pickCount] * pick.credits);

		this.pauseCoroutine("swapping");
		infoObjectA.SetActive(false);
		infoObjectB.SetActive(false);

		LabelWrapperComponent wrapper = infoObjectC.GetComponent<LabelWrapperComponent>();
		wrapper.text = CreditsEconomy.convertCredits(pick.credits) + " X " + MULTIPLIERS[pickCount] + " = " + CreditsEconomy.convertCredits(winAmount);
		infoObjectC.SetActive(true);


		//Set a local variable with the referemce to the currently selected label
		LabelWrapper revealText = revealTextsWrapper[index];
		revealText.alpha = 0;

		//Set the tween label to the same value as that of the reveal text
		movingScoreObjectTextWrapper.text = revealText.text;
		movingScoreObjectTextWrapper.alpha = 1;

		//Move the tween label to the proper starting position
		movingScoreObject.transform.parent = revealText.transform.parent.transform;
		TweenPosition.Begin(movingScoreObject, 0.0f, Vector3.zero);
		movingScoreObject.transform.parent = winLabelWrapper.transform.parent.transform;

		//Set the particle trail for the multiplier under the proper object and move it into position
		multiplierObjectTrail.transform.parent = multiplierObjects[pickCount].transform;
		TweenPosition.Begin(multiplierObjectTrail, 0.0f, new Vector3(0, 0, 1));

		//Determine the amount of time need to tween the objects to the combination point
		float tweenDistance = Vector3.Distance(movingScoreObject.transform.localPosition, Vector3.zero);
		float tweenVelocity = 1200.0f;
		float tweenTime = (tweenDistance / tweenVelocity);

		//Turn on the particle trails
		multiplierObjectTrail.SetActive(true);
		movingScoreObjectTrail.SetActive(true);

		//Change multiplier to the glowing version of itself
		UISprite multiplierSprite = multiplierObjects[pickCount].GetComponent<UISprite>();
		string multiplierName = multiplierSprite.spriteName;
		string multiplierGlowName = multiplierName.Insert(multiplierName.Length - 2, "_glow");
		multiplierSprite.spriteName = multiplierGlowName;
		multiplierSprite.MakePixelPerfect();

		//Lets start moving towards the combination point
		TweenPosition.Begin(movingScoreObject, tweenTime, Vector3.zero);
		TweenPosition.Begin(multiplierObjects[pickCount].gameObject, tweenTime, new Vector3(winLabelWrapper.transform.parent.transform.localPosition.x, 
																										winLabelWrapper.transform.parent.transform.localPosition.y,
																										multiplierObjects[pickCount].transform.localPosition.z));
		Audio.play("value_move");

		yield return new WaitForSeconds(tweenTime);

		Audio.play("value_land");

		//Play combination particle burst
		combinationBurstObject.SetActive(true);

		//Turn off the particle trails
		multiplierObjectTrail.SetActive(false);
		movingScoreObjectTrail.SetActive(false);

		//Turn off multiplier object
		multiplierObjects[pickCount].enabled = false;

		//update the labels        
		movingScoreObjectTextWrapper.text = CreditsEconomy.convertCredits(winAmount);
		revealText.text = CreditsEconomy.convertCredits(winAmount);

		//Begin 
		TweenPosition.Begin(movingScoreObject, 0.25f, winLabelWrapper.transform.localPosition);

		//Wait for tween
		yield return new WaitForSeconds(0.25f);

		//Do credit roll
		long initialCredits = BonusGamePresenter.instance.currentPayout;
		BonusGamePresenter.instance.currentPayout += winAmount;
		StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, winLabelWrapper));

		//Update label visibility
		movingScoreObjectTextWrapper.alpha = 0;
		revealText.alpha = 1;

		//Update/reset for next run through
		combinationBurstObject.SetActive(false);
		pickCount++;
		this.resumeCoroutine("swapping");
		lastTimeChanged = Time.time;
		infoObjectA.SetActive(true);
		infoObjectB.SetActive(false);
		infoObjectC.SetActive(false);
		//Turn the other buttons back on
		for (int i = 0; i < buttonSelections.Length; i++)
		{
			if (!_pickedIndices.Contains(i))
			{
				buttonSelections[i].GetComponent<Collider>().enabled = true;
			}
		}

		if (pickCount > 10)
		{
			for (int i = 0; i < buttonSelections.Length; i++)
			{
				buttonSelections[i].GetComponent<Collider>().enabled = false;
			}
			this.StartCoroutine(this.revealRemainingPicks());
		}
	}

	private IEnumerator endGame(float delay)
	{
		yield return new WaitForSeconds(delay);

		while (!finishedRevealing)
		{
			yield return new WaitForSeconds(0.1f);
		}

		BonusGamePresenter.instance.gameEnded();
	}
	private IEnumerator doSwapping()
	{
		while (true)
		{
			if (Time.time > (lastTimeChanged + titleSwapFrequency))
			{
				infoObjectA.SetActive(!infoObjectA.activeInHierarchy);
				infoObjectB.SetActive(!infoObjectB.activeInHierarchy);
				lastTimeChanged = Time.time;
			}

			yield return null;
		}
	}
	#endregion Private Methods
}

