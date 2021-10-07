using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tanker : ChallengeGame 
{
	private const float TIME_BETWEEN_REVEALS = 0.5f;

	public GameObject[] buttonSelections;
	public GameObject[] buttonPickMeAnimations;
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
	

	//Holds all the objects that will need to be turned on for the pipe bomb win animations, mainly particle effects
	public GameObject[] pipeBombSequenceObjects;
	public Animation tankerDriveAway;
	public Animation roadShake;
	public Animation lampsFade;
	public GameObject pipeBombWinPopup;
	public GameObject coinSpinSparkleTrail;

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
	
	public UILabel bigWinText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent bigWinTextWrapperComponent;

	public LabelWrapper bigWinTextWrapper
	{
		get
		{
			if (_bigWinTextWrapper == null)
			{
				if (bigWinTextWrapperComponent != null)
				{
					_bigWinTextWrapper = bigWinTextWrapperComponent.labelWrapper;
				}
				else
				{
					_bigWinTextWrapper = new LabelWrapper(bigWinText);
				}
			}
			return _bigWinTextWrapper;
		}
	}
	private LabelWrapper _bigWinTextWrapper = null;
	
	public UILabel messageBoxLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent messageBoxLabelWrapperComponent;

	public LabelWrapper messageBoxLabelWrapper
	{
		get
		{
			if (_messageBoxLabelWrapper == null)
			{
				if (messageBoxLabelWrapperComponent != null)
				{
					_messageBoxLabelWrapper = messageBoxLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_messageBoxLabelWrapper = new LabelWrapper(messageBoxLabel);
				}
			}
			return _messageBoxLabelWrapper;
		}
	}
	private LabelWrapper _messageBoxLabelWrapper = null;
	
	public string collectAllSpriteName;
	public string gameEndSpriteName;  
	
	private PickemOutcome outcome;
	private PlayingAudio ambientTankerSound;
	private bool collectAllCredits = false;
	private bool finishedRevealing = false; ///< if you don't check to make sure revealing is completed before showing the results the prize amount displayed could be incorrect.
	private SkippableWait revealWait = new SkippableWait();

	public override void init() 
	{
		outcome = (PickemOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		
		foreach (LabelWrapper revealText in revealTextsWrapper)
		{
			revealText.alpha = 0;
		}
		
		ambientTankerSound = Audio.play("AmbienceTankerBg",1,0,0,float.PositiveInfinity);
		_didInit = true;
	}
	
	private void replaceRevealWithCA(LabelWrapper revealText, int index, bool gameOverInstead = false, bool greyOutSprite = false)
	{
		revealText.gameObject.SetActive(false);
		buttonSelections[index].SetActive(true);
		UISprite uiSprite = buttonSelections[index].GetComponent<UISprite>();
		if (uiSprite)
		{
			if (gameOverInstead)
			{
				uiSprite.spriteName = gameEndSpriteName;
			}
			else
			{
				uiSprite.spriteName = collectAllSpriteName;
			}
			uiSprite.MarkAsChanged();
		}

		if (greyOutSprite)
		{
			uiSprite.color = Color.gray;
		}
	}
	
	public void onPickSelected(GameObject button)
	{
		NGUITools.SetActive(button, false);
	
		// Let's find which button index was clicked on.	
		int index = System.Array.IndexOf(buttonSelections, button);
		NGUITools.SetActive(buttonPickMeAnimations[index], false); ///< Let's turn off the flashy pickme animation
		PickemPick pick = outcome.getNextEntry();
		revealTextsWrapper[index].alpha = 1;

		if (!pick.isGameOver)
		{
			GameObject coin = CommonGameObject.instantiate(this.coinSpinSparkleTrail, Vector3.zero, Quaternion.identity) as GameObject;
			coin.transform.parent = button.transform.parent.transform;
			TweenPosition.Begin(coin, 0.0f, Vector3.zero);
			coin.transform.parent = this.winLabelWrapper.transform.parent.transform;

			float tweenDistance = Vector3.Distance(coin.transform.localPosition, this.winLabelWrapper.transform.localPosition);
			float tweenVelocity = 1250.0f;
			float tweenTime = (tweenDistance / tweenVelocity);

			TweenPosition tw = TweenPosition.Begin(coin, tweenTime, this.winLabelWrapper.transform.localPosition);

			StartCoroutine(turnOffCoin((tweenTime + 0.5f), coin));

			//Removing a warning in the console since tween finishes itself
			tw.ToString();
		}

		// Let's disable all the button colliders if necessary.
		if (pick.isCollectAll || pick.isGameOver)
		{
			for (int i = 0; i < buttonSelections.Length; i++)
			{
				buttonSelections[i].GetComponent<Collider>().enabled = false;
			}
		}
		
		if (pick.isCollectAll)
		{
			collectAllCredits = true;
			replaceRevealWithCA(revealTextsWrapper[index],index);
			StartCoroutine("revealRemainingPicks");
			startAnimation(); ///< Moved this from the coroutine, since it doesn't need to be called over and over
			Audio.play("PipeBombTankerTruck");
		}
		else if (pick.isGameOver)
		{		
			replaceRevealWithCA(revealTextsWrapper[index],index,true); 
			StartCoroutine("revealRemainingPicks");
			if (ambientTankerSound!= null && ambientTankerSound.isPlaying)
			{
				Audio.stopSound(ambientTankerSound);
			}
			Audio.play("RevealBadBonusT101");
			Audio.play("A1GetOut", 1, 0, 2);
		}
		else
		{
			long initialCredits = BonusGamePresenter.instance.currentPayout;
			NGUITools.SetActive(revealTextsWrapper[index].gameObject, true);
			BonusGamePresenter.instance.currentPayout += pick.credits;
			revealTextsWrapper[index].text = CreditsEconomy.convertCredits(pick.credits);
			
			StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, updateCreditsRoll));
			
			Audio.play("RevealCreditTerminator");
		}		
	}
	
	/// Rollup callback function.	
	private void updateCreditsRoll(long value)
	{
		winLabelWrapper.text = CreditsEconomy.convertCredits(value);
	}
		
	private IEnumerator revealRemainingPicks()
	{
		// Add a slight delay before revealing all, to give time for the player
		// to see what happened, and also to prevent automatically skipping
		// the remaining reveals.
		yield return new WaitForSeconds(0.5f);
		
		messageBoxLabelWrapper.text = collectAllCredits ? Localize.text("congratulations_ex") : Localize.text("game_over_2");
		PickemPick pick;
		List<PickemPick> remainingReveals = new List<PickemPick>();
		long totalRevealedCredits = 0;
		
		// Just cached the reveals for use further down.
		PickemPick revealedPick = null;
		do
		{
			revealedPick = outcome.getNextReveal();
			if (revealedPick != null)
			{
				remainingReveals.Add(revealedPick);
				totalRevealedCredits += revealedPick.credits;
			}
		}
		while (revealedPick != null);
		
		if (collectAllCredits)
		{
			long initialCredits = BonusGamePresenter.instance.currentPayout;
			BonusGamePresenter.instance.currentPayout += totalRevealedCredits;
			bigWinTextWrapper.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
			StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, updateCreditsRoll));
		}
		
		int revealIndex = 0;
		
		for (int i = 0; i < revealTextsWrapper.Count; i++)
		{
			if (revealTextsWrapper[i].alpha == 0)
			{
				pick = remainingReveals[revealIndex];
				revealIndex++;
				if (pick != null)
				{
					NGUITools.SetActive(buttonSelections[i], false);
					NGUITools.SetActive(buttonPickMeAnimations[i], false);///< Turn off the remaining pickme animations as we reveal the distractors
					revealTextsWrapper[i].alpha = 1;
					
					if (!revealWait.isSkipping)
					{
						Audio.play(Audio.soundMap("reveal_not_chosen"));
					}
					
					if (pick.credits != 0)
					{
						if (!collectAllCredits)
						{
							revealTextsWrapper[i].color = Color.gray;
						}
						revealTextsWrapper[i].text = CreditsEconomy.convertCredits(pick.credits);
					}
					else if (pick.isCollectAll)
					{
						replaceRevealWithCA(revealTextsWrapper[i], i, false, true);
					}
					else
					{
						replaceRevealWithCA(revealTextsWrapper[i], i, true, true);
					}

					yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
				}
			}
		}
		
		if (collectAllCredits == false)
		{
			StartCoroutine("endGame", 0.5f);
		}
		finishedRevealing = true;
	}
	
	void startAnimation()
	{
		//Make sure the proper object are all turned on
		foreach (GameObject go in this.pipeBombSequenceObjects)
		{
			go.SetActive(true);
		}

		//Start the animations
		this.tankerDriveAway.Play();
		this.roadShake.Play();
		this.lampsFade.Play();

		//Temp for testing
		Invoke("waitToEnd", 3.0f);
	}

	private IEnumerator turnOffCoin(float waitTime, GameObject coin)
	{
		yield return new WaitForSeconds(waitTime);
		Destroy(coin);
	}

	private void waitToEnd()
	{
		foreach (GameObject go in this.pipeBombSequenceObjects)
		{
			go.SetActive(false);
		}
		this.pipeBombWinPopup.SetActive(true);
		bigWinTextWrapper.gameObject.SetActive(true);
		StartCoroutine("endGame", 1.5f);
	}
	private IEnumerator endGame(float delay)
	{
		yield return new WaitForSeconds(delay);

		while (!finishedRevealing)
		{
			yield return new WaitForSeconds(0.1f);
		}
		
		if (ambientTankerSound != null && ambientTankerSound.isPlaying)
		{
			Audio.stopSound(ambientTankerSound);
		}
		BonusGamePresenter.instance.gameEnded();
	}
}

