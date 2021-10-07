using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bev02Pickem : ChallengeGame
{
	public GameObject screen1Objects;									// Parent obj for all the jethro screen
	public GameObject screen2Objects;									// Parent obj for all the second screen

	public GameObject[] jethroButtons;									// Collider objects for the jethro heads
	public Animator[] jethroAnimators;									// Animator objects for the jethro heads
	public UILabel[] jethroRevealPickLabels;						// Labels for the jethro reveal -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] jethroRevealPickLabelsWrapperComponent;						// Labels for the jethro reveal

	public List<LabelWrapper> jethroRevealPickLabelsWrapper
	{
		get
		{
			if (_jethroRevealPickLabelsWrapper == null)
			{
				_jethroRevealPickLabelsWrapper = new List<LabelWrapper>();

				if (jethroRevealPickLabelsWrapperComponent != null && jethroRevealPickLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in jethroRevealPickLabelsWrapperComponent)
					{
						_jethroRevealPickLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in jethroRevealPickLabels)
					{
						_jethroRevealPickLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _jethroRevealPickLabelsWrapper;
		}
	}
	private List<LabelWrapper> _jethroRevealPickLabelsWrapper = null;	
	
	public UILabel[] jethroRevealPickShadowLabels;					// Shadow labels for the jethro reveal -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] jethroRevealPickShadowLabelsWrapperComponent;					// Shadow labels for the jethro reveal

	public List<LabelWrapper> jethroRevealPickShadowLabelsWrapper
	{
		get
		{
			if (_jethroRevealPickShadowLabelsWrapper == null)
			{
				_jethroRevealPickShadowLabelsWrapper = new List<LabelWrapper>();

				if (jethroRevealPickShadowLabelsWrapperComponent != null && jethroRevealPickShadowLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in jethroRevealPickShadowLabelsWrapperComponent)
					{
						_jethroRevealPickShadowLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in jethroRevealPickShadowLabels)
					{
						_jethroRevealPickShadowLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _jethroRevealPickShadowLabelsWrapper;
		}
	}
	private List<LabelWrapper> _jethroRevealPickShadowLabelsWrapper = null;	
	

	private bool[] jethroRevealed = new bool[3];						// Bool array to determine if a icon has been revealed.
	private bool[] foodRevealed = new bool[15];							// Bool array to determine if a icon has been revealed.

	public GameObject[] foodButtons;									// Collider buttons for the second screen.
	public Animator[] foodAnimators;									// Animators for the second screen.
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
	
	public UILabel[] revealShadowNumbers;								// Reveal shadow numbers for the second screen. -  To be removed when prefabs are updated.
	public LabelWrapperComponent[] revealShadowNumbersWrapperComponent;								// Reveal shadow numbers for the second screen.

	public List<LabelWrapper> revealShadowNumbersWrapper
	{
		get
		{
			if (_revealShadowNumbersWrapper == null)
			{
				_revealShadowNumbersWrapper = new List<LabelWrapper>();

				if (revealShadowNumbersWrapperComponent != null && revealShadowNumbersWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in revealShadowNumbersWrapperComponent)
					{
						_revealShadowNumbersWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in revealShadowNumbers)
					{
						_revealShadowNumbersWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _revealShadowNumbersWrapper;
		}
	}
	private List<LabelWrapper> _revealShadowNumbersWrapper = null;	
	
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

	private List<int> possiblePickCounts = new List<int>();	// Hardcoded list of possible wins, so we can do the reveals.

	private long localPickCount = 0;									// Pick count used in the top left label
	private long pickMultiplier = 1;								// Multiplier used in the bottom left label

	private bool screen1Complete = false;
	private bool screen2Complete = false;
	private bool pickMeAllowed = true;

	private const float MIN_TIME_ANIM = 2.0f;
	private const float MAX_TIME_ANIM = 5.0f;
	private const float TIME_BETWEEN_REVEALS = 0.25f;
	private const float PICK_ME_WAIT = 1.0f;						// Used for anim time and time in between.
	private const float PRE_JETHRO_REVEAL_WAIT = 1.5f;
	private const float INNER_JETHRO_REVEAL_WAIT = 0.5f;
	private const float POST_JETHRO_REVEAL_WAIT = 1.0f;
	private const float INNER_FOOD_REVEAL_WAIT = 0.25f;
	private const float POST_FOOD_REVEAL_WAIT = 1.0f;
	private const float CREDIT_BUFFER_WAIT = 0.1f;
	private const float SPARKLE_TRAIL_WAIT = 1.0f;
	private const float POP_WAIT = 0.5f;
	
	private SkippableWait revealWait = new SkippableWait();			//Handles skippable reveals

	// Sound names
	private const string BONUS_BG = "IdleBonusBev02";
	private const string BONUS_BG_2 = "BonusBgBev02";
	private const string INTRO_VO = "IntroBonusBev02VO";
	private const string PICKME_SOUND = "PickMeBonusBev02";
	private const string PICK_JETHRO_SOUND = "RevealNumJethrosDing";
	private const string REVEAL_CREDIT = "RevealCreditBev02";
	private const string JETHRO_MMMM = "JethroMmmMM";
	private const string INTRO_BONUS = "IntroBonusBev02";
	private const string REVEAL_MUTLIPLIER = "RevealMultiplierBev02";
	private const string MULTIPLIER_VO = "JBHotDawg";
	private const string MULTIPLIER_FLY = "MultiplierSlidesDownBev02";
	private const string MULTIPLIER_HIT = "MultiplierLandsBev02";
	private const string MULTIPLIER_FLY_TO_CREDIT = "MultiplierFliesUpBev02";
	private const string MUTIPLIER_UPDATES_CREDIT = "MultiplierMultipliesCreditBev02";
	private const string REVEAL_OTHERS = "reveal_others";

	public override void init()
	{
		Audio.switchMusicKeyImmediate(BONUS_BG);
		Audio.play(INTRO_VO, 1, 0, 0.6f);

		// Adding in our possible counts
		possiblePickCounts.Add(5);
		possiblePickCounts.Add(7);
		possiblePickCounts.Add(9);

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
		if (!screen1Complete)
		{
			int pickemIndex = Random.Range(0,jethroButtons.Length);
			jethroAnimators[pickemIndex].Play("bev02_PB_JETHRO_Icon_PickMe");
			Audio.play(PICKME_SOUND);
			yield return new TIWaitForSeconds(PICK_ME_WAIT);
			if (!screen1Complete)
			{
				jethroAnimators[pickemIndex].Play("bev02_PB_JETHRO_Icon_reset");
			}
		}
		else if (!screen2Complete)
		{
			int pickemIndex = Random.Range(0,foodButtons.Length);
			if (!foodRevealed[pickemIndex])
			{
				foodAnimators[pickemIndex].Play("bev02_PB_PickingObject_PickMe");
				Audio.play(PICKME_SOUND);
				yield return new TIWaitForSeconds(PICK_ME_WAIT);
				if (!foodRevealed[pickemIndex])
				{
					foodAnimators[pickemIndex].Play("bev02_PB_PickingObject_Reveal_reset");
				}
			}
		}

		yield return new TIWaitForSeconds(PICK_ME_WAIT);
	}

	// Callback from the first screen.
	public void jethroSelected(GameObject jethroHead)
	{
		if (screen1Complete)
		{
			return;
		}

		pickMeAllowed = false;
		screen1Complete = true;

		Audio.play(PICK_JETHRO_SOUND);

		int arrayIndex = System.Array.IndexOf(jethroButtons, jethroHead);
		jethroRevealed[arrayIndex] = true;

		jethroAnimators[arrayIndex].Play("bev02_PB_JETHRO_Icon_Reveal");

		pickemOutcome = new PickemOutcome(SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, wheelPick.bonusGame));

		// Remove our found number from the full list.
		possiblePickCounts.Remove(pickemOutcome.entryCount);
		localPickCount = pickemOutcome.entryCount;

		jethroRevealPickLabelsWrapper[arrayIndex].text = Localize.text("{0}_picks", pickemOutcome.entryCount);
		jethroRevealPickShadowLabels[arrayIndex].text = Localize.text("{0}_picks", pickemOutcome.entryCount);

		pickemPick = pickemOutcome.getNextEntry();

		StartCoroutine(revealJethroIcons());
	}

	// Reveals the remaining icons on the first screen
	private IEnumerator revealJethroIcons()
	{
		yield return new TIWaitForSeconds(PRE_JETHRO_REVEAL_WAIT);

		Audio.switchMusicKeyImmediate(INTRO_BONUS);

		for (int i = 0; i < jethroRevealed.Length; i++)
		{
			if (!jethroRevealed[i])
			{
				Audio.play(REVEAL_OTHERS);
				UISprite jethroButton = jethroButtons[i].GetComponent<UISprite>();
				if (jethroButton != null)
				{
					jethroButton.color = Color.gray;
				}
				int fakeCount = possiblePickCounts[0];
				possiblePickCounts.RemoveAt(0);

				jethroRevealPickLabelsWrapper[i].text = Localize.text("{0}_picks", fakeCount);
				jethroRevealPickShadowLabels[i].text = Localize.text("{0}_picks", fakeCount);

				jethroAnimators[i].Play("bev02_PB_JETHRO_Icon_RevealnotSelect");
				jethroRevealed[i] = true;
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}

		yield return new TIWaitForSeconds(POST_JETHRO_REVEAL_WAIT);

		beginSecondScreen();
	}

	// Toggles the first screen off, and puts the second screen on.
	private void beginSecondScreen()
	{
		Audio.switchMusicKeyImmediate(BONUS_BG_2);
		screen1Objects.SetActive(false);
		screen2Objects.SetActive(true);
		pickMeAllowed = true;
		pickAmountLabel.text = CommonText.formatNumber(localPickCount);
	}

	// Callback from the selection in the second screen.
	public void foodSelected(GameObject food)
	{
		int arrayIndex = System.Array.IndexOf(foodButtons, food);

		if (localPickCount == 0 || foodRevealed[arrayIndex] || pickMeAllowed == false)
		{
			return;
		}

		pickMeAllowed = false;

		foodRevealed[arrayIndex] = true;

		Audio.play(PICK_JETHRO_SOUND);

		// Let's decrease our pick count.
		localPickCount--;
		pickAmountLabel.text = CommonText.formatNumber(localPickCount);
		foodRevealed[arrayIndex] = true;
		
		StartCoroutine(startFoodSelectedSequence(arrayIndex));
	}

	// Decides whether its just a basic credit, multiplied credit, or multiplier with a credit attached.
	private IEnumerator startFoodSelectedSequence(int arrayIndex)
	{
		PickemPick currentPick = pickemPick;
		pickemPick = pickemOutcome.getNextEntry();

		if (currentPick.multiplier != 0)
		{
			pickMultiplier += currentPick.multiplier;
		}
		long localPickMultiplier = pickMultiplier;

		if (currentPick.credits != 0)
		{
			long amountWonThisRound = currentPick.credits * localPickMultiplier;
			yield return new TIWaitForSeconds(CREDIT_BUFFER_WAIT);
			StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + amountWonThisRound, winAmountLabel));
			BonusGamePresenter.instance.currentPayout += amountWonThisRound;
		}

		foodAnimators[arrayIndex].Play("bev02_PB_PickingObject_Reveal_Number");

		if (currentPick.multiplier != 0)
		{
			Audio.play(MULTIPLIER_VO);
			// New multiplier is involved, let's do our fly to the box.
			StartCoroutine(multiplierSequence(arrayIndex, currentPick));
		}
		else
		{
			revealNumbers[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits);
			revealShadowNumbers[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits);
			if (localPickMultiplier == 1)
			{
				// No multiplier locally, and a credit was found. Just show it.
				revealNumbers[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits * localPickMultiplier);
				revealShadowNumbers[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits * localPickMultiplier);
				Audio.play(REVEAL_CREDIT);
				Audio.play(JETHRO_MMMM, 1, 0, 0.4f);

				endGameCheck();
			}
			else
			{
				// Fly the mutliplier to the credit, and show the new value.
				StartCoroutine(startMultipliedCreditSequence(arrayIndex, currentPick, localPickMultiplier));
			}
		}
	}

	// Trail from the multiplier to the credit.
	private IEnumerator startMultipliedCreditSequence(int arrayIndex, PickemPick currentPick, long multiplierAmount)
	{
		Audio.play(MULTIPLIER_FLY_TO_CREDIT);
		Audio.play(REVEAL_CREDIT);
		Audio.play(JETHRO_MMMM, 1, 0, 0.4f);
		sparkleTrail.transform.position = multiplierLabel.gameObject.transform.position;
		sparkleTrail.SetActive(true);
		iTween.MoveTo(sparkleTrail, iTween.Hash("position", foodButtons[arrayIndex].transform.position, "time", 1.0f, "islocal", false, "easetype", iTween.EaseType.linear));
		yield return new TIWaitForSeconds(SPARKLE_TRAIL_WAIT);
		Audio.play(MUTIPLIER_UPDATES_CREDIT);
		sparkleTrail.SetActive(false);
		revealNumbers[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits * multiplierAmount);
		revealShadowNumbers[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits * multiplierAmount);

		endGameCheck();
	}

	// Trail from the credit to the multiplier
	private IEnumerator multiplierSequence(int arrayIndex, PickemPick currentPick)
	{
		UILabelStyler labelStyler = revealNumbers[arrayIndex].gameObject.GetComponent<UILabelStyler>();
		if (labelStyler != null)
		{
			labelStyler.style = multiplierStyle;
			labelStyler.updateStyle();
		}
		labelStyler = revealShadowNumbers[arrayIndex].gameObject.GetComponent<UILabelStyler>();
		if (labelStyler != null)
		{
			labelStyler.style = multiplierShadowStyle;
			labelStyler.updateStyle();
		}
		revealNumbers[arrayIndex].text = Localize.text("plus_{0}_X", CommonText.formatNumber(currentPick.multiplier));
		revealShadowNumbers[arrayIndex].text = Localize.text("plus_{0}_X", CommonText.formatNumber(currentPick.multiplier));

		yield return new TIWaitForSeconds(1.0f);

		revealNumbers[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits);
		revealShadowNumbers[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits);

		sparkleTrail.transform.position = foodButtons[arrayIndex].transform.position;
		sparkleTrail.SetActive(true);
		iTween.MoveTo(sparkleTrail, iTween.Hash("position", multiplierLabel.gameObject.transform.position, "time", 1.0f, "islocal", false, "easetype", iTween.EaseType.linear));

		Audio.play(MULTIPLIER_FLY);
		yield return new TIWaitForSeconds(SPARKLE_TRAIL_WAIT);
		multiplierLabel.text = CommonText.formatNumber(pickMultiplier);
		sparkleTrail.SetActive(false);
		Audio.play(MULTIPLIER_HIT);
		sparklePop.SetActive(true);
		sparklePop.transform.position = multiplierLabel.gameObject.transform.position;
		revealNumbers[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits * pickMultiplier);
		revealShadowNumbers[arrayIndex].text = CreditsEconomy.convertCredits(currentPick.credits * pickMultiplier);
		yield return new TIWaitForSeconds(POP_WAIT);
		sparklePop.SetActive(false);

		endGameCheck();
	}

	private void endGameCheck()
	{
		if (localPickCount == 0 && !screen2Complete)
		{
			screen2Complete = true;
			StartCoroutine(revealAllFoodSelections());
		}
		else
		{
			pickMeAllowed = true;
		}
	}

	// Our final reveal for the food.
	private IEnumerator revealAllFoodSelections()
	{
		revealWait.reset();
		for (int i = 0; i < foodAnimators.Length; i++)
		{
			if (!foodRevealed[i])
			{
				foodRevealed[i] = true;
				pickemPick = pickemOutcome.getNextReveal();

				foodAnimators[i].Play("bev02_PB_PickingObject_Reveal_Number");

				if (pickemPick.multiplier != 0)
				{
					UILabelStyler labelStyler = revealNumbers[i].gameObject.GetComponent<UILabelStyler>();
					if (labelStyler != null)
					{
						labelStyler.style = grayedOutMultiplierStyle;
						labelStyler.updateStyle();
					}
					revealNumbers[i].text = Localize.text("plus_{0}_X", CommonText.formatNumber(pickemPick.multiplier));
					revealShadowNumbers[i].text = "";
				}
				else
				{
					UILabelStyler labelStyler = revealNumbers[i].gameObject.GetComponent<UILabelStyler>();
					if (labelStyler != null)
					{
						labelStyler.style = grayedOutNumberStyle;
						labelStyler.updateStyle();
					}
					revealNumbers[i].text = CreditsEconomy.convertCredits(pickemPick.credits);
					revealShadowNumbers[i].text = "";
				}
				if(!revealWait.isSkipping)
				{
					Audio.play(REVEAL_OTHERS);
				}
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}

		yield return new TIWaitForSeconds(POST_FOOD_REVEAL_WAIT);

		BonusGamePresenter.instance.gameEnded();
	}
}

