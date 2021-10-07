using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BeaverBonusPickem : ChallengeGame
{
	private const float WATER_HIGH_Y = 100f;
	private const float WATER_LOW_Y = 0f;
	private const float TIME_BETWEEN_REVEALS = 0.25f;
	
	public GameObject[] buttonSelections;				// Buttons that can be selected
    public Animation[] buttonPickMeAnimations;			// Attraction animations for the user to pick the button
	public Animation[] duckReflections;					// Used when the duck bobs
	public int[] reversedDucks;							// Hardcoded array of the indices of reversed ducks
	public UILabel[] revealTexts; 						// labels that reveal value of each pick -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] revealTextsWrapperComponent; 						// labels that reveal value of each pick

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
	
	public GameObject[] pickedBeavers; // game objects that have beaver sprites attached. Use for reveal and displaying in box (with beaver indents)
	private Dictionary<GameObject, GameObject> buttonsHoldingBeavers = new Dictionary<GameObject, GameObject>();
	private Dictionary<GameObject, LabelWrapper> buttonsHoldingText = new Dictionary<GameObject, LabelWrapper>();
	private GameObject buttonHoldingSi;
	public GameObject pickedSi; // game objects that have Si sprites attached for reveal 
	public GameObject[] revealSis;
	public GameObject[] beaverPips; // game objects of the beaver indents
	public GameObject waterPlane;
	public GameObject itemText;				// Text used for the shuffle effect
	public GameObject reshuffleText;		// Text used for the shuffle effect
	public GameObject[] itemPoints; 		// locations used to move "ITEM" text around
	public GameObject[] reshufflePoints; 	// locations used to move "RESHUFFLED" text around
	public GameObject reshuffleBackdrop;	// Background used during a reshuffle

	public GameObject gameLabel;			// Part of label shown during game over
	public GameObject overLabel;			// Part of label shown during game over

	public UILabel selectDuckLabel;			// Instruction text telling the user to select a duck -  To be removed when prefabs are updated.
	public LabelWrapperComponent selectDuckLabelWrapperComponent;			// Instruction text telling the user to select a duck

	public LabelWrapper selectDuckLabelWrapper
	{
		get
		{
			if (_selectDuckLabelWrapper == null)
			{
				if (selectDuckLabelWrapperComponent != null)
				{
					_selectDuckLabelWrapper = selectDuckLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_selectDuckLabelWrapper = new LabelWrapper(selectDuckLabel);
				}
			}
			return _selectDuckLabelWrapper;
		}
	}
	private LabelWrapper _selectDuckLabelWrapper = null;
	
	public UILabel findSiLabel;				// Instruction text telling the user to find Si -  To be removed when prefabs are updated.
	public LabelWrapperComponent findSiLabelWrapperComponent;				// Instruction text telling the user to find Si

	public LabelWrapper findSiLabelWrapper
	{
		get
		{
			if (_findSiLabelWrapper == null)
			{
				if (findSiLabelWrapperComponent != null)
				{
					_findSiLabelWrapper = findSiLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_findSiLabelWrapper = new LabelWrapper(findSiLabel);
				}
			}
			return _findSiLabelWrapper;
		}
	}
	private LabelWrapper _findSiLabelWrapper = null;
	
	public UILabel callDucksLabel;			// Instuction text telling the user that ducks are being called -  To be removed when prefabs are updated.
	public LabelWrapperComponent callDucksLabelWrapperComponent;			// Instuction text telling the user that ducks are being called

	public LabelWrapper callDucksLabelWrapper
	{
		get
		{
			if (_callDucksLabelWrapper == null)
			{
				if (callDucksLabelWrapperComponent != null)
				{
					_callDucksLabelWrapper = callDucksLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_callDucksLabelWrapper = new LabelWrapper(callDucksLabel);
				}
			}
			return _callDucksLabelWrapper;
		}
	}
	private LabelWrapper _callDucksLabelWrapper = null;
	

	public UILabel winLabel;				// Won value text -  To be removed when prefabs are updated.
	public LabelWrapperComponent winLabelWrapperComponent;				// Won value text

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
	
	private List<int> animationIndicies = new List<int>(); // list of indicies to use for picking random shot glass to animate
	private int previousAnimationIndex = -1; // previously selected index [so we don't select it again]

    public GameObject sparkleTrail; // prefab for trail from pick to beaver head
	public GameObject pickDuckEffect; // prefab for star burst when duck is picked

	public UISprite[] beaverParts;		// The parts of the beaver for fading.
	public Animator damBuildingBeaver;	// beaver that builds the dam
	public UISprite[] logs;				// The three logs that appear when the beaver builds the dam.
	public GameObject flyingDuckTemplate; // template to clone for ducks flying in
	
	private const int NUM_BEAVERS_NEEDED = 3;	// number needed to ADVANCE
	private int numBeaversPicked = 0;			// beavers found so far

	private PickemOutcome pickemOutcome;				// Stores the outcome information sent from the server
	private List<int> pickedButtons = new List<int>();	// Tracks the buttons that have already been picked
	private SkippableWait revealWait = new SkippableWait();
	
	private float waterBaseY = WATER_HIGH_Y;

	public override void init() 
	{
		pickemOutcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];

		BonusGamePresenter.instance.useMultiplier = true;
		
		Audio.switchMusicKey("BonusBgDuck01");
		Audio.stopMusic();

		startDucksBobbing();
		StartCoroutine("alternateLabels", alternateLabels());
		Audio.play("BeaverBonusVO");

		_didInit = true;
	}
	
	protected override void Update()
	{
		base.Update();
		
		// Make the 3D water level slowly go up and down for effect.
		CommonTransform.setY(waterPlane.transform, CommonEffects.pulsateBetween(waterBaseY - 2f, waterBaseY + 2f, 2f));
	}

	private IEnumerator alternateLabels()
	{
		bool selectIsActive = true;
		while (true)
		{
			yield return new WaitForSeconds(7.0f);
			selectIsActive = !selectIsActive;
			selectDuckLabelWrapper.gameObject.SetActive(selectIsActive);
			findSiLabelWrapper.gameObject.SetActive(!selectIsActive);
		}
	}

	private void startDucksBobbing()
	{
		for (int i = 0; i < buttonPickMeAnimations.Length; i++)
		{

			buttonPickMeAnimations[i]["dd_duckFloat"].time = .15f * i;
			buttonPickMeAnimations[i].Play();
			duckReflections[i]["dd_duckReflectBounce"].time = .15f * i;
			duckReflections[i].Play();

			animationIndicies.Add(i);
		}

		StartCoroutine("pickDuckToAnimate");
	}
	
	/// randomly pick a shot (that wasnt the previously picked shot) and play 2 animations to get the player's attention
	private IEnumerator pickDuckToAnimate()
	{
		bool reAddIndex;
		int randomIndex, buttonIndex;

		while (true)
		{
			reAddIndex = false;
			if (animationIndicies.Contains(previousAnimationIndex))
			{
				// don't let it pick the same object twice, so remove the previously selected one temporarily (if it hasn't been destroyed)
				animationIndicies.Remove(previousAnimationIndex); 
				reAddIndex  = true;
			}
			randomIndex = UnityEngine.Random.Range(0, animationIndicies.Count);
			buttonIndex = animationIndicies[randomIndex];
			if (animationIndicies.Contains(buttonIndex))
			{
				buttonPickMeAnimations[buttonIndex].Play("dd_duckPickme");
			}
			if (reAddIndex)
			{
				animationIndicies.Add(previousAnimationIndex);
			}

			previousAnimationIndex = buttonIndex;

			yield return new WaitForSeconds(1.0f);

			// Restart duck bobbing
			buttonPickMeAnimations[buttonIndex].Play("dd_duckFloat");
			duckReflections[buttonIndex].Play();

			yield return new WaitForSeconds(.8f);
		}
	}

	/// When a button is selected, 
	public void pickemButtonPressed(GameObject button)
	{
		PickemPick pick = pickemOutcome.getNextEntry();
		NGUIExt.disableAllMouseInput();
	
		// Let's find which button index was clicked on.	
		int index = System.Array.IndexOf(buttonSelections, button);

		// NGUITools.SetActive(buttonPickMeAnimations[index], false); ///< Lets turn off the flashy pickme animation
		// animationIndicies.Remove(index);
		revealTextsWrapper[index].gameObject.SetActive(true);
		revealTextsWrapper[index].text = "";//pick.pick;

		button.GetComponent<Collider>().enabled = false;

		Audio.play("BBPickDuck");

		StartCoroutine(revealPick(pick, button, index));
	}
	
	private IEnumerator showPopulateAnimation(GameObject button)
	{
		GameObject populateAnim = CommonGameObject.instantiate(pickDuckEffect) as GameObject;
		populateAnim.transform.parent = gameObject.transform;
		populateAnim.transform.position = button.transform.position + new Vector3(0.0f, 0.0f, -1.0f); // put animation in front of duck
		populateAnim.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
		populateAnim.SetActive(true);
		yield return new WaitForSeconds(1.5f);
		Destroy (populateAnim);
	}
	
	private IEnumerator revealPick(PickemPick pick, GameObject button, int index)
	{
		pickedButtons.Add(index);
		StartCoroutine(showPopulateAnimation(button));
		yield return new WaitForSeconds(.4f);
		button.SetActive(false);
		yield return new WaitForSeconds(.1f);
		if (pick.pick == "1")
		{
			Audio.play("RevealBeaver");
			pickedBeavers[numBeaversPicked].transform.parent = button.transform.parent;
			pickedBeavers[numBeaversPicked].transform.localPosition = Vector3.zero;


			GameObject trail = CommonGameObject.instantiate(sparkleTrail) as GameObject;
			trail.transform.parent = button.transform.parent;
			trail.transform.localScale = sparkleTrail.transform.localScale;
			trail.transform.localPosition = new Vector3(0, 0, -100f);
			
			float tweenTime = 1.5f;
			Vector2 destination = NGUIExt.localPositionOfPosition(button.transform.parent, beaverPips[numBeaversPicked].transform.position);
			iTween.MoveTo(trail, iTween.Hash("x", destination.x, "y", destination.y, "time", tweenTime, "islocal", true));
			
			yield return new WaitForSeconds(tweenTime + 0.2f);
			Destroy(trail);

			beaverPips[numBeaversPicked].GetComponent<UISprite>().spriteName = "beaver_on";
			buttonsHoldingBeavers.Add(button,pickedBeavers[numBeaversPicked]);
			numBeaversPicked++;

			Audio.play("BeaverSwims");

			// Always show the beaver animation and the fading log and rocks,
			// no matter how many beavers have been picked so far.
			damBuildingBeaver.Play(string.Format("dd_beaverSwim{0}", numBeaversPicked), -1, 0f);
			
			// Let the animation reset before setting the alpha back to 1 so we don't see it at the old position for 1 frame.
			yield return null;
			yield return null;
			setBeaverAlpha(1f);
			yield return new WaitForSeconds(1f);
			yield return StartCoroutine(fadeLog());
			yield return StartCoroutine(adjustWaterLevel());

			if (numBeaversPicked == 3)
			{
				// Picked the third beaver, so the game is ending.
				selectDuckLabelWrapper.gameObject.SetActive(false);
				findSiLabelWrapper.gameObject.SetActive(false);
				callDucksLabelWrapper.gameObject.SetActive(false);
				StopCoroutine("alternateLabels");
				yield return new WaitForSeconds(1f);
				yield return StartCoroutine(revealAllPicks());
				yield return StartCoroutine(showGameOver());
				
				BonusGamePresenter.instance.gameEnded();
			}

			NGUIExt.enableAllMouseInput();

			yield return new WaitForSeconds(.3f);
		}		
		else if (pick.pick == "2")
		{
			selectDuckLabelWrapper.gameObject.SetActive(false);
			findSiLabelWrapper.gameObject.SetActive(false);
			callDucksLabelWrapper.gameObject.SetActive(true);
			StopCoroutine("alternateLabels");
			Audio.play("BBRevealSi");
			pickedSi.transform.parent = button.transform.parent;
			pickedSi.transform.localPosition = Vector3.zero;
			buttonHoldingSi = button;
			yield return new WaitForSeconds(.3f);
			yield return StartCoroutine(bringNewDucks());
			selectDuckLabelWrapper.gameObject.SetActive(true);
			findSiLabelWrapper.gameObject.SetActive(false);
			callDucksLabelWrapper.gameObject.SetActive(false);

			NGUIExt.enableAllMouseInput();
		}
		else
		{
			Audio.play("BBRevealCredit");
			long credits = long.Parse(pick.pick) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			revealTextsWrapper[index].text = CreditsEconomy.convertCredits(credits);
			buttonsHoldingText.Add(button,revealTextsWrapper[index]);
			long previousPayout = BonusGamePresenter.instance.currentPayout;
			BonusGamePresenter.instance.currentPayout += credits;
			
			NGUIExt.enableAllMouseInput();
			yield return StartCoroutine(SlotUtils.rollup(previousPayout, BonusGamePresenter.instance.currentPayout, winLabelWrapper));
		}
	}

	private IEnumerator showGameOver()
	{
		gameLabel.SetActive(true);
		gameLabel.GetComponent<TweenAlpha>().Play(true);
		yield return new WaitForSeconds(.2f);

		overLabel.SetActive(true);
		overLabel.GetComponent<TweenAlpha>().Play(true);
		yield return new WaitForSeconds(2.0f);

		gameLabel.GetComponent<TweenColor>().Play(true);
		overLabel.GetComponent<TweenColor>().Play(true);

		Audio.play("BBEndBonus");
		yield return new WaitForSeconds(2.0f);
	}


	private IEnumerator adjustWaterLevel()
	{
		Audio.play("BeaverRevealVO");
		float desiredTideNormalized = (float)numBeaversPicked / (float)NUM_BEAVERS_NEEDED;
		float waterY = Mathf.Lerp(WATER_HIGH_Y, WATER_LOW_Y, desiredTideNormalized);
		iTween.ValueTo(this.gameObject, iTween.Hash("from", waterBaseY, "to", waterY, "time", 1f, "onupdate", "updateWaterBaseY", "easeType", iTween.EaseType.easeInOutQuad));
		yield return new WaitForSeconds(1f);
	}
	
	// iTween callback.
	private void updateWaterBaseY(float newY)
	{
		waterBaseY = newY;	
	}

	// Fade in a log.
	private IEnumerator fadeLog()
	{
		logs[numBeaversPicked - 1].alpha = 0;
		logs[numBeaversPicked - 1].gameObject.SetActive(true);
		iTween.ValueTo(this.gameObject, iTween.Hash("from", 0f, "to", 1f, "time", 1.0f, "onupdate", "updateLog"));
		yield return new WaitForSeconds(1f);
	}

	// Update a fading-in log.
	private void updateLog(float alphaValue)
	{
		logs[numBeaversPicked - 1].alpha = alphaValue;

		// Also fade out the beaver.
		setBeaverAlpha(1f - alphaValue);
	}
	
	// Set the alpha value of the beaver.
	private void setBeaverAlpha(float alpha)
	{
		foreach (UISprite part in beaverParts)
		{
			part.alpha = alpha;
		}		
	}

	private IEnumerator bringNewDucks()
	{
		Audio.play("PerfectDuckCall");
		Audio.play("RevealSiVO", 1, 0, 0.5f);
		bool shouldFlyFromLeft = true;
		foreach (int i in pickedButtons)
		{
			shouldFlyFromLeft = true;
			foreach (int j in reversedDucks)
			{
				// in Unity, objects are 1-indexed, so adjust for the fact that the array is 0-indexed
				if ((j-1) == i)
				{
					shouldFlyFromLeft = false;
				}
			}
			StartCoroutine(flyInNewDuck(i, shouldFlyFromLeft));
			yield return new WaitForSeconds(.4f);
		}
		yield return new WaitForSeconds((2.6f)); // wait until all ducks are finished being replaced
		yield return StartCoroutine(reshuffleDucks());
	}

	private IEnumerator flyInNewDuck(int buttonIndex, bool shouldFlyFromLeft)
	{
		Audio.play("DuckFlockFlap");
		GameObject button = buttonSelections[buttonIndex];
		GameObject flyingDuck = CommonGameObject.instantiate(flyingDuckTemplate) as GameObject;
		flyingDuck.transform.parent = gameObject.transform;
		flyingDuck.transform.localScale = new Vector3(.4f,.4f,.4f);

		if (shouldFlyFromLeft)	
		{
			flyingDuck.transform.position = new Vector3(-3f, 0f, 0f) + button.transform.position; 
		}
		else
		{
			flyingDuck.transform.localScale = new Vector3(flyingDuck.transform.localScale.x * -1.0f, flyingDuck.transform.localScale.y, flyingDuck.transform.localScale.z);
			flyingDuck.transform.position = new Vector3(3f, 0f, 0f) + button.transform.position; 
		}
		flyingDuck.SetActive(true);
		iTween.MoveTo(flyingDuck, iTween.Hash("position",button.transform.position,"isLocal",false,"time",2.5f,"easeType",iTween.EaseType.linear));
		yield return new WaitForSeconds(2f);
		flyingDuck.GetComponent<Animator>().Play("duck_land");
		yield return new WaitForSeconds(1.0f);
		Audio.play("DuckSplash");
		Destroy (flyingDuck);
		button.SetActive(true);
		button.GetComponent<BoxCollider>().enabled = true;
		
		if (buttonsHoldingBeavers.ContainsKey(button))
		{
			buttonsHoldingBeavers[button].transform.parent = gameObject.transform.Find("beavers");
			buttonsHoldingBeavers[button].transform.localPosition = Vector2.zero;
			buttonsHoldingBeavers.Remove(button);
		}
		else if(buttonHoldingSi == button)
		{
			pickedSi.transform.parent = gameObject.transform.Find ("Si");
			pickedSi.transform.localPosition = Vector3.zero;
		}
		else
		{
			buttonsHoldingText[button].text = "";
			buttonsHoldingText.Remove(button);
		}

		animationIndicies.Add(buttonIndex);
		buttonPickMeAnimations[buttonIndex].Play("dd_duckFloat");
		duckReflections[buttonIndex].Play();
	}

	private IEnumerator reshuffleDucks()
	{
		// show reshuffle text
		StartCoroutine(shuffleItemsReshuffledText());

		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < buttonPickMeAnimations.Length; j++)
			{
				buttonSelections[j].gameObject.transform.localScale = new Vector3(-1.0f*buttonSelections[j].gameObject.transform.localScale.x, buttonSelections[j].gameObject.transform.localScale.y, buttonSelections[j].gameObject.transform.localScale.z);
			}
			yield return new WaitForSeconds(.3f);
		}
		yield return new WaitForSeconds(2.0f); // wait until text animation is done too, plus a tiny bit extra time
	}

	private IEnumerator shuffleItemsReshuffledText()
	{
		itemText.gameObject.SetActive(true);
		reshuffleText.gameObject.SetActive(true);
		reshuffleBackdrop.SetActive(true);
		yield return new WaitForSeconds(1.0f);
		itemText.transform.Find("itemsLabel").GetComponent<TweenColor>().Play(true);
		reshuffleText.transform.Find("reshuffleLabel").GetComponent<TweenColor>().Play(true);
		for (int i=0; i < 4; i++)
		{
			iTween.MoveTo(itemText, iTween.Hash("position", itemPoints[i].transform.position, "time", .5f, "islocal", false, "easeType", iTween.EaseType.easeInOutCubic));
			iTween.MoveTo(reshuffleText, iTween.Hash("position", reshufflePoints[i].transform.position, "time", .5f, "islocal", false, "easeType", iTween.EaseType.easeInOutCubic));
			yield return new WaitForSeconds(.5f);
		}

		iTween.MoveTo(itemText, iTween.Hash("position", itemPoints[4].transform.position, "time", .25f, "islocal", false, "easeType", iTween.EaseType.easeInOutCubic));
		iTween.MoveTo(reshuffleText, iTween.Hash("position", reshufflePoints[4].transform.position, "time", .25f, "islocal", false, "easeType", iTween.EaseType.easeInOutCubic));
		yield return new WaitForSeconds(1.0f);

		itemText.gameObject.SetActive(false);
		reshuffleText.gameObject.SetActive(false);
		reshuffleBackdrop.gameObject.SetActive(false);

		// Set text back to original color just in case they ever decide that we can reshuffle more than once
		itemText.transform.Find("itemsLabel").GetComponent<TweenColor>().Play(false);
		reshuffleText.transform.Find("reshuffleLabel").GetComponent<TweenColor>().Play(false);
	}
	
	public IEnumerator revealAllPicks()
	{
		StopCoroutine("pickDuckToAnimate");
		selectDuckLabelWrapper.gameObject.SetActive(false);
		findSiLabelWrapper.gameObject.SetActive(false);
		Color disabledColor = new Color(0.25f, 0.25f, 0.25f);
		PickemPick reveal = pickemOutcome.getNextReveal();
		int beaverIndex = numBeaversPicked;
		int siIndex = 0;
		
		yield return new WaitForSeconds(.5f);

		int duckIndex;
		GameObject button;
		while (reveal != null)
		{
			duckIndex = -1;
			button = null;
			while(button == null || !button.activeSelf)
			{
				duckIndex++;
				button = buttonSelections[duckIndex];
			}
			Destroy (button);
			if (reveal.pick == "1")
			{
				pickedBeavers[beaverIndex].transform.parent = button.transform.parent;
				pickedBeavers[beaverIndex].transform.localPosition = Vector3.zero;
				pickedBeavers[beaverIndex].GetComponent<UISprite>().color = Color.gray;
				beaverIndex++;
			}
			else if (reveal.pick == "2")
			{
				revealSis[siIndex].transform.parent = button.transform.parent;
				revealSis[siIndex].transform.localPosition = Vector3.zero;
				revealSis[siIndex].GetComponent<UISprite>().color = Color.gray;
				siIndex++;
			}
			else
			{
				long revealCredits = long.Parse(reveal.pick) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
				revealTextsWrapper[duckIndex].gameObject.GetComponent<UILabelStyler>().style = null; // don't style anymore
				revealTextsWrapper[duckIndex].text = CreditsEconomy.convertCredits(revealCredits);
				revealTextsWrapper[duckIndex].color = disabledColor;
				revealTextsWrapper[duckIndex].effectStyle = "none";
				revealTextsWrapper[duckIndex].isGradient = false;
				revealTextsWrapper[duckIndex].gameObject.SetActive(true);
			}
			if(!revealWait.isSkipping)
			{
				Audio.play("BBRevealOtherDucks");
			}
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			reveal = pickemOutcome.getNextReveal();
		} 
		yield return new WaitForSeconds(0.5f);
	}

}



