using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Grumpy01Pickem : ChallengeGame {

	public GameObject screen1Objects;									// Parent obj for all the tuna screen
	public GameObject screen2Objects;									// Parent obj for all the second screen

	public GameObject[] tunaButtons;									// Collider objects for the tuna heads
	public Animator[] tunaAnimators;									// Animator objects for the tuna heads
	public UILabel[] tunaRevealPickLabels;						// Labels for the tuna reveal -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] tunaRevealPickLabelsWrapperComponent;						// Labels for the tuna reveal

	public List<LabelWrapper> tunaRevealPickLabelsWrapper
	{
		get
		{
			if (_tunaRevealPickLabelsWrapper == null)
			{
				_tunaRevealPickLabelsWrapper = new List<LabelWrapper>();

				if (tunaRevealPickLabelsWrapperComponent != null && tunaRevealPickLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in tunaRevealPickLabelsWrapperComponent)
					{
						_tunaRevealPickLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in tunaRevealPickLabels)
					{
						_tunaRevealPickLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _tunaRevealPickLabelsWrapper;
		}
	}
	private List<LabelWrapper> _tunaRevealPickLabelsWrapper = null;	
	
	public UILabel[] tunaRevealPickGrayLabels;					// Shadow labels for the tuna reveal -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] tunaRevealPickGrayLabelsWrapperComponent;					// Shadow labels for the tuna reveal

	public List<LabelWrapper> tunaRevealPickGrayLabelsWrapper
	{
		get
		{
			if (_tunaRevealPickGrayLabelsWrapper == null)
			{
				_tunaRevealPickGrayLabelsWrapper = new List<LabelWrapper>();

				if (tunaRevealPickGrayLabelsWrapperComponent != null && tunaRevealPickGrayLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in tunaRevealPickGrayLabelsWrapperComponent)
					{
						_tunaRevealPickGrayLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in tunaRevealPickGrayLabels)
					{
						_tunaRevealPickGrayLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _tunaRevealPickGrayLabelsWrapper;
		}
	}
	private List<LabelWrapper> _tunaRevealPickGrayLabelsWrapper = null;	
	
	
	private bool[] tunaRevealed = new bool[3];						// Bool array to determine if a icon has been revealed.
	private bool[] toyRevealed = new bool[13];							// Bool array to determine if a icon has been revealed.
	
	public GameObject[] toyButtons;									// Collider buttons for the second screen.
	public Animator[] toyAnimators;									// Animators for the second screen.
	public UILabel[] revealNumbers;										// Reveal Numbers on the second screen. -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] revealNumbersWrapperComponent;										// Reveal Numbers on the second screen.

	public List<LabelWrapper> revealNumbersWrapper
	{
		get
		{
			if (_revealNumbersWrapper == null)
			{
				_revealNumbersWrapper = new List<LabelWrapper>();

				if (revealNumbersWrapperComponent != null && revealNumbersWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in revealNumbersWrapperComponent)
					{
						_revealNumbersWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in revealNumbers)
					{
						_revealNumbersWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _revealNumbersWrapper;
		}
	}
	private List<LabelWrapper> _revealNumbersWrapper = null;	
	
	public UILabel[] revealGrayNumbers;								// Reveal shadow numbers for the second screen. -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] revealGrayNumbersWrapperComponent;								// Reveal shadow numbers for the second screen.

	public List<LabelWrapper> revealGrayNumbersWrapper
	{
		get
		{
			if (_revealGrayNumbersWrapper == null)
			{
				_revealGrayNumbersWrapper = new List<LabelWrapper>();

				if (revealGrayNumbersWrapperComponent != null && revealGrayNumbersWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in revealGrayNumbersWrapperComponent)
					{
						_revealGrayNumbersWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in revealGrayNumbers)
					{
						_revealGrayNumbersWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _revealGrayNumbersWrapper;
		}
	}
	private List<LabelWrapper> _revealGrayNumbersWrapper = null;	
	
	public UILabel pickAmountLabel;										// Pick amount on the top left corner -  To be removed when prefabs are updated.
	public LabelWrapperComponent pickAmountLabelWrapperComponent;										// Pick amount on the top left corner

	public LabelWrapper pickAmountLabelWrapper
	{
		get
		{
			if (_pickAmountLabelWrapper == null)
			{
				if (pickAmountLabelWrapperComponent != null)
				{
					_pickAmountLabelWrapper = pickAmountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_pickAmountLabelWrapper = new LabelWrapper(pickAmountLabel);
				}
			}
			return _pickAmountLabelWrapper;
		}
	}
	private LabelWrapper _pickAmountLabelWrapper = null;
	
	public UILabel multiplierLabel;										// Multiplier label in the bottom left corner -  To be removed when prefabs are updated.
	public LabelWrapperComponent multiplierLabelWrapperComponent;										// Multiplier label in the bottom left corner

	public LabelWrapper multiplierLabelWrapper
	{
		get
		{
			if (_multiplierLabelWrapper == null)
			{
				if (multiplierLabelWrapperComponent != null)
				{
					_multiplierLabelWrapper = multiplierLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierLabelWrapper = new LabelWrapper(multiplierLabel);
				}
			}
			return _multiplierLabelWrapper;
		}
	}
	private LabelWrapper _multiplierLabelWrapper = null;
	
	public UILabel winAmountLabel;										// Win amount label in the bottom right corner -  To be removed when prefabs are updated.
	public LabelWrapperComponent winAmountLabelWrapperComponent;										// Win amount label in the bottom right corner

	public LabelWrapper winAmountLabelWrapper
	{
		get
		{
			if (_winAmountLabelWrapper == null)
			{
				if (winAmountLabelWrapperComponent != null)
				{
					_winAmountLabelWrapper = winAmountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winAmountLabelWrapper = new LabelWrapper(winAmountLabel);
				}
			}
			return _winAmountLabelWrapper;
		}
	}
	private LabelWrapper _winAmountLabelWrapper = null;
	
	public UILabelStyle multiplierStyle;								// Mutliplier font to use when revealing a multiplier
	public UILabelStyle multiplierShadowStyle;							// Mutliplier shadow font to use when revealing a multiplier
	public UILabelStyle grayedOutMultiplierStyle;						// Reveal gray font
	public UILabelStyle grayedOutNumberStyle;							// Reveal gray font
	public GameObject sparkleTrail;										// Multiplier trail
	public GameObject sparklePop;										// Multiplier pop for after trail is finished.

	private WheelOutcome wheelOutcome;
	private WheelPick wheelPick;
	
	private PickemOutcome pickemOutcome;
	private PickemPick pickemPick;
	
	private CoroutineRepeater pickemRepeater;
	
	[SerializeField] private List<int> possiblePickCounts = new List<int>();	// Hardcoded list of possible wins, so we can do the reveals.
	
	private long localPickCount = 0;									// Pick count used in the top left label
	private long pickMultiplier = 1;								// Multiplier used in the bottom left label

	private bool screen1Complete = false;
	private bool screen2Complete = false;
	private bool pickMeAllowed = true;
	

	[SerializeField] protected float MIN_TIME_ANIM;
	[SerializeField] protected float MAX_TIME_ANIM;
	[SerializeField] protected float TIME_BETWEEN_REVEALS;
	[SerializeField] protected float PICK_ME_WAIT;						// Used for anim time and time in between.
	[SerializeField] protected float PRE_TUNA_REVEAL_WAIT;
	[SerializeField] protected float POST_TUNA_REVEAL_WAIT;
	[SerializeField] protected float PRE_TOY_REVEAL_WAIT;
	[SerializeField] protected float POST_TOY_REVEAL_WAIT;
	[SerializeField] protected float CREDIT_BUFFER_WAIT;
	[SerializeField] protected float SPARKLE_TRAIL_WAIT;
	[SerializeField] protected float POP_WAIT;

	private SkippableWait revealWait = new SkippableWait();			//Handles skippable reveals
	
	// Sound names
	private const string BONUS_BG = "IdleBonusGrumpyCat";
	private const string BONUS_BG_2 = "BonusBgGrumpyCat";
	private const string INTRO_VO = "BonusIntroVOGrumpyCat";
	private const string PICKME_SOUND = "PickMeBonusGrumpyCat";
	private const string PICK_TUNA_SOUND = "RevealNumberOfPicksGrumpyCat";
	private const string REVEAL_CREDIT = "RevealCreditGrumpyCat";
	private const string TUNA_MMMM = "RevealNumberOfPicksGrumpyCat";
	private const string INTRO_BONUS = "BonusIntroVOGrumpyCat";
	private const string REVEAL_MUTLTIPLIER = "RevealMultiplierGrumpyCat";
	private const string MULTIPLIER_FLY = "MultiplierTravelsGrumpyCat";
	private const string MULTIPLIER_HIT = "MultiplierArrivesGrumpyCat";
	private const string MULTIPLIER_FLY_TO_CREDIT = "MultiplierFliesUpGrumpyCat";
	private const string MUTIPLIER_UPDATES_CREDIT = "MultiplierMultipliesCreditGrumpyCat";
	private const string REVEAL_OTHERS = "reveal_others";

	private static readonly string[] MULTIPLIER_VO_NAMES = 
	{
		"APGoAwayPokey",
		"APOMGPokeyYoure",
		"APScramPokey",
		"APPokeyWhoLet",
		"APPokeyYouThinking",
		"APGetOuttaHere",
		"APThatsMyBrother",
		"APStopHelpingThe",
		"APYouRuinEverything"
	};

	private static readonly string[] REVEAL_CREDIT_VO = 
	{
		"APHmmBoring",
		"APBetterKeepYour",
		"APYouWinAnd",
		"APBoring",
		"APNotVeryGood",
	};


	
	public override void init()
	{
		Audio.switchMusicKeyImmediate(BONUS_BG);
		Audio.play(INTRO_VO, 1.0f, 0.0f, 0.6f);
		
		pickemRepeater = new CoroutineRepeater(MIN_TIME_ANIM, MAX_TIME_ANIM, pickMeCallback);
		wheelOutcome = (WheelOutcome)BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE];
		wheelPick = wheelOutcome.getNextEntry();
		_didInit = true;
	}
	
	protected override void Update()
	{
		base.Update();
		if ((!screen1Complete || !screen2Complete) && pickMeAllowed && _didInit)
		{
			pickemRepeater.update();
		}
	}
	
	// Controls the pickem animations.
	private IEnumerator pickMeCallback()
	{
		if(!screen1Complete)
		{
			int pickemIndex = Random.Range(0,tunaButtons.Length);
			tunaAnimators[pickemIndex].Play("pickme");
			Audio.play(PICKME_SOUND);
			yield return new TIWaitForSeconds(PICK_ME_WAIT);

			if (!screen1Complete)
			{
				tunaAnimators[pickemIndex].Play("default");
			}

		}
		else if (!screen2Complete)
		{
			int pickemIndex = Random.Range(0,toyButtons.Length);
			if (!toyRevealed[pickemIndex])
			{
				toyAnimators[pickemIndex].Play("pickme");
				Audio.play(PICKME_SOUND);
				yield return new TIWaitForSeconds(PICK_ME_WAIT);

				if (!toyRevealed[pickemIndex])
				{
					toyAnimators[pickemIndex].Play("default");
				}

			}

		}
		
		yield return new TIWaitForSeconds(PICK_ME_WAIT);
	}
	
	// Callback from the first screen.
	public void tunaSelected(GameObject tunaHead)
	{
		if (screen1Complete)
		{
			return;
		}
		
		pickMeAllowed = false;
		screen1Complete = true;
		
		Audio.play(PICK_TUNA_SOUND);
		
		int arrayIndex = System.Array.IndexOf(tunaButtons, tunaHead);
		tunaRevealed[arrayIndex] = true;
		
		tunaAnimators[arrayIndex].Play("reveal_picks");
		
		pickemOutcome = new PickemOutcome(SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, wheelPick.bonusGame));
		
		// Remove our found number from the full list.
		possiblePickCounts.Remove(pickemOutcome.entryCount);
		localPickCount = pickemOutcome.entryCount;
		
		tunaRevealPickLabelsWrapper[arrayIndex].text = Localize.text("{0}_picks", pickemOutcome.entryCount);

		pickemPick = pickemOutcome.getNextEntry();
		
		StartCoroutine(revealtunaIcons());
	}
	
	// Reveals the remaining icons on the first screen
	private IEnumerator revealtunaIcons()
	{
		yield return new TIWaitForSeconds(PRE_TUNA_REVEAL_WAIT);

		for (int i = 0; i < tunaRevealed.Length; i++)
		{
			if (!tunaRevealed[i])
			{
				Audio.play(REVEAL_OTHERS);
				UISprite tunaButton = tunaButtons[i].GetComponent<UISprite>();
				if (tunaButton != null)
				{
					tunaButton.color = Color.gray;
				}
				int fakeCount = possiblePickCounts[0];
				possiblePickCounts.RemoveAt(0);
				
				tunaRevealPickGrayLabelsWrapper[i].text = Localize.text("{0}_picks", fakeCount);
				
				tunaAnimators[i].Play("reveal_picks grey");
				tunaRevealed[i] = true;
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}
		
		yield return new TIWaitForSeconds(POST_TUNA_REVEAL_WAIT);
		
		beginSecondScreen();
	}
	
	// Toggles the first screen off, and puts the second screen on.
	private void beginSecondScreen()
	{
		Audio.switchMusicKeyImmediate(BONUS_BG_2);
		screen1Objects.SetActive(false);
		screen2Objects.SetActive(true);
		pickMeAllowed = true;
		pickAmountLabelWrapper.text = Localize.text("{0}_picks", CommonText.formatNumber(localPickCount));
	}
	
	// Callback from the selection in the second screen.
	public void toySelected(GameObject toy)
	{
		int arrayIndex = System.Array.IndexOf(toyButtons, toy);
		if (localPickCount == 0 || toyRevealed[arrayIndex] || pickMeAllowed == false)
		{
			return;
		}
		
		pickMeAllowed = false;
		
		toyRevealed[arrayIndex] = true;
		
		Audio.play(PICK_TUNA_SOUND);
		
		// Let's decrease our pick count.
		localPickCount--;
		pickAmountLabelWrapper.text = Localize.text("{0}_picks", CommonText.formatNumber(localPickCount));
		toyRevealed[arrayIndex] = true;
		
		StartCoroutine(starttoySelectedSequence(arrayIndex));
	}
	
	// Decides whether its just a basic credit, multiplied credit, or multiplier with a credit attached.
	private IEnumerator starttoySelectedSequence(int arrayIndex)
	{
		PickemPick currentPick = pickemPick;
		pickemPick = pickemOutcome.getNextEntry();
		
		if (currentPick.multiplier != 0)
		{
			pickMultiplier += currentPick.multiplier;
		}
		long localPickMultiplier = pickMultiplier;

		if (currentPick.multiplier != 0)
		{
			Audio.play(MULTIPLIER_VO_NAMES[Random.Range(0,(MULTIPLIER_VO_NAMES.Length-1))]);
			Audio.play (REVEAL_MUTLTIPLIER);
			revealNumbersWrapper[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits);
			toyAnimators[arrayIndex].Play("reveal_multiplier");
			// New multiplier is involved, let's do our fly to the box.
			StartCoroutine(multiplierSequence(arrayIndex, currentPick));
		}
		else
		{
			revealNumbersWrapper[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits);
			revealGrayNumbersWrapper[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits);
			toyAnimators[arrayIndex].Play("reveal_credit");
			Audio.play(REVEAL_CREDIT_VO[Random.Range(0,(REVEAL_CREDIT_VO.Length-1))]);
			if (localPickMultiplier == 1)
			{
				// No multiplier locally, and a credit was found. Just show it.
				revealNumbersWrapper[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits * localPickMultiplier);
				revealGrayNumbersWrapper[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits * localPickMultiplier);
				Audio.play(REVEAL_CREDIT);

				endGameCheck();
			}
			else
			{
				// Fly the mutliplier to the credit, and show the new value.
				yield return new TIWaitForSeconds(POST_TOY_REVEAL_WAIT);
				StartCoroutine(startMultipliedCreditSequence(arrayIndex, currentPick, localPickMultiplier));
			}
		}

		if (currentPick.credits != 0)
		{
			long amountWonThisRound = currentPick.credits * localPickMultiplier;
			if(localPickMultiplier != 1 && currentPick.multiplier <= 0)
			{
				yield return new TIWaitForSeconds(SPARKLE_TRAIL_WAIT + CREDIT_BUFFER_WAIT);
			}
			else if(currentPick.multiplier != 0)
			{
				yield return new TIWaitForSeconds(SPARKLE_TRAIL_WAIT*2 + CREDIT_BUFFER_WAIT + POST_TOY_REVEAL_WAIT); //wait twice as long because of 2 sparkle trail sequences
			}
			else
			{
				yield return new TIWaitForSeconds(CREDIT_BUFFER_WAIT);
			}
			StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + amountWonThisRound, winAmountLabelWrapper));
			BonusGamePresenter.instance.currentPayout += amountWonThisRound;
		}
	}
	
	// Trail from the multiplier to the credit.
	private IEnumerator startMultipliedCreditSequence(int arrayIndex, PickemPick currentPick, long multiplierAmount)
	{
		Audio.play(REVEAL_CREDIT);
		sparkleTrail.transform.position = multiplierLabelWrapper.gameObject.transform.position;
		sparkleTrail.SetActive(true);
		Vector3 toyButtonPosition = new Vector3(toyButtons[arrayIndex].transform.position.x, toyButtons[arrayIndex].transform.position.y, 0.0f); //Created this posiiton to prevent the sparkle from moving to the asme Z as the toy
		Audio.play (MULTIPLIER_FLY_TO_CREDIT);

		yield return new TITweenYieldInstruction(iTween.MoveTo(sparkleTrail, iTween.Hash("position", toyButtonPosition, "time", SPARKLE_TRAIL_WAIT, "islocal", false, "easetype", iTween.EaseType.linear)));

		Audio.play(MUTIPLIER_UPDATES_CREDIT);
		sparkleTrail.SetActive(false);
		toyAnimators[arrayIndex].Play("update");
		revealNumbersWrapper[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits * multiplierAmount);
		revealGrayNumbersWrapper[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits * multiplierAmount);
		yield return new TIWaitForSeconds(POP_WAIT);
		endGameCheck();
	}
	
	// Trail from the credit to the multiplier
	private IEnumerator multiplierSequence(int arrayIndex, PickemPick currentPick)
	{
		UILabelStyler labelStyler = revealNumbersWrapper[arrayIndex].gameObject.GetComponent<UILabelStyler>();
		if (labelStyler != null)
		{
			labelStyler.style = multiplierStyle;
			labelStyler.updateStyle();
		}
		labelStyler = revealGrayNumbersWrapper[arrayIndex].gameObject.GetComponent<UILabelStyler>();
		if (labelStyler != null)
		{
			labelStyler.style = multiplierShadowStyle;
			labelStyler.updateStyle();
		}
		revealNumbersWrapper[arrayIndex].text = Localize.text("plus_{0}_X", CommonText.formatNumber(currentPick.multiplier));
		revealGrayNumbersWrapper[arrayIndex].text = Localize.text("plus_{0}_X", CommonText.formatNumber(currentPick.multiplier));
		
		yield return new TIWaitForSeconds(POST_TOY_REVEAL_WAIT);
		
		revealNumbersWrapper[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits);
		revealGrayNumbersWrapper[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits);
		
		sparkleTrail.transform.position = new Vector3 (toyButtons [arrayIndex].transform.position.x, toyButtons [arrayIndex].transform.position.y, 0.0f);
		sparkleTrail.SetActive(true);
		Audio.play(MULTIPLIER_FLY);

		yield return new TITweenYieldInstruction(iTween.MoveTo(sparkleTrail, iTween.Hash("position", multiplierLabelWrapper.gameObject.transform.position, "time", SPARKLE_TRAIL_WAIT, "islocal", false, "easetype", iTween.EaseType.linear)));

		toyAnimators [arrayIndex].Play ("reveal_credit");
		multiplierLabelWrapper.text = Localize.text("X{0}", CommonText.formatNumber(pickMultiplier));
		sparkleTrail.SetActive(false);
		Audio.play(MULTIPLIER_HIT);
		sparklePop.SetActive(true);
		sparklePop.transform.position = multiplierLabelWrapper.gameObject.transform.position;
		revealNumbersWrapper[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits);
		revealGrayNumbersWrapper[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits);

		yield return new TIWaitForSeconds(POP_WAIT);
		toyAnimators [arrayIndex].StopPlayback ();
		sparklePop.SetActive(false);
		StartCoroutine(startMultipliedCreditSequence(arrayIndex, currentPick, pickMultiplier));
	}
	
	private void endGameCheck()
	{
		if (localPickCount == 0 && !screen2Complete)
		{
			screen2Complete = true;
			StartCoroutine(revealAlltoySelections());
		}
		else
		{
			pickMeAllowed = true;
		}
	}
	
	// Our final reveal for the toy.
	private IEnumerator revealAlltoySelections()
	{
		yield return new TIWaitForSeconds(PRE_TOY_REVEAL_WAIT);
	
		revealWait.reset();
		for (int i = 0; i < toyAnimators.Length; i++)
		{
			if (!toyRevealed[i])
			{
				toyRevealed[i] = true;
				pickemPick = pickemOutcome.getNextReveal();
				

				
				if (pickemPick.multiplier != 0)
				{
					UILabelStyler labelStyler = revealNumbersWrapper[i].gameObject.GetComponent<UILabelStyler>();
					if (labelStyler != null)
					{
						labelStyler.style = grayedOutMultiplierStyle;
						labelStyler.updateStyle();
					}
					revealGrayNumbersWrapper[i].text = Localize.text("plus_{0}_X", CommonText.formatNumber(pickemPick.multiplier));
					toyAnimators[i].Play("reveal_multiplier grey");

				}
				else
				{
					UILabelStyler labelStyler = revealNumbersWrapper[i].gameObject.GetComponent<UILabelStyler>();
					if (labelStyler != null)
					{
						labelStyler.style = grayedOutNumberStyle;
						labelStyler.updateStyle();
					}
					revealGrayNumbersWrapper[i].text = CreditsEconomy.convertCredits(pickemPick.credits);
					toyAnimators[i].Play("reveal_credit grey");
				}
				if(!revealWait.isSkipping)
				{
					Audio.play(REVEAL_OTHERS);
				}
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}
		
		yield return new TIWaitForSeconds(POST_TOY_REVEAL_WAIT);
		
		BonusGamePresenter.instance.gameEnded();
	}
}


