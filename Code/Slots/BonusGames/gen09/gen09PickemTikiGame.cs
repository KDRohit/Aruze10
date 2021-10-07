using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class gen09PickemTikiGame : PickingGameUsingPickemOutcome 
{
	// Tunables
	
	[SerializeField] private float TIME_BEFORE_SUMMARY = 0.5f;
	
	// Game Objects
	
	[SerializeField] private MultiLabel[] titleLabels = new MultiLabel[5];   // Labels at the top of the game, two styles.
	[SerializeField] private UILabel picksRemainingLabel = null;       // Picks remaining text. -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent picksRemainingLabelWrapperComponent = null;       // Picks remaining text.

	public LabelWrapper picksRemainingLabelWrapper
	{
		get
		{
			if (_picksRemainingLabelWrapper == null)
			{
				if (picksRemainingLabelWrapperComponent != null)
				{
					_picksRemainingLabelWrapper = picksRemainingLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_picksRemainingLabelWrapper = new LabelWrapper(picksRemainingLabel);
				}
			}
			return _picksRemainingLabelWrapper;
		}
	}
	private LabelWrapper _picksRemainingLabelWrapper = null;
	
	[SerializeField] private UILabel jackpotBoxAmountText = null;      // Tells how much the jackpot icon is worth. -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent jackpotBoxAmountTextWrapperComponent = null;      // Tells how much the jackpot icon is worth.

	public LabelWrapper jackpotBoxAmountTextWrapper
	{
		get
		{
			if (_jackpotBoxAmountTextWrapper == null)
			{
				if (jackpotBoxAmountTextWrapperComponent != null)
				{
					_jackpotBoxAmountTextWrapper = jackpotBoxAmountTextWrapperComponent.labelWrapper;
				}
				else
				{
					_jackpotBoxAmountTextWrapper = new LabelWrapper(jackpotBoxAmountText);
				}
			}
			return _jackpotBoxAmountTextWrapper;
		}
	}
	private LabelWrapper _jackpotBoxAmountTextWrapper = null;
	
	
	[SerializeField] private GameObject[] jackpotBoxCharacterIcons = new GameObject[5]; // Displays the right icon for the character's game you are playing.
	[SerializeField] private GameObject jackpotWinAnimationObj = null;                  // Animation object for winning the jackpot.

	[SerializeField] private UILabelStyle grayOutStyle = null;

	// Variables
	
	private CharacterEnum selectedCharacter = CharacterEnum.None;
	private long jackpotValueFromWheel = 0; // used to track the jackpot value passed from the wheel which we will display for the tiki jackpot display
	private long baseGameMultiplierAtWheelStart = 0; // Added debug info to try and track down an issue where jackpot values aren't matching what was displayed during the wheel
	
	// Constants
	
	private const string PICKS_REMAINING_LOC_STRING = "{0}_picks_remaining"; // Localization string for "{0} picks remaining"
	private const string ONE_PICK_REMAINING_LOC_STRING = "1_pick_remaining"; // Localization string for "1 pick remaining"
	
	private readonly string[] PICKME_ANIM_NAMES = new string[5]
	{
		"GEN09_WB_PickingObject_Chief_PickMe",
		"GEN09_WB_PickingObject_Larry_PickMe",
		"GEN09_WB_PickingObject_Biff_PickMe",
		"GEN09_WB_PickingObject_Todd_PickMe",
		"GEN09_WB_PickingObject_heatchliff_PickMe"
	};
	private readonly string[] STILL_ANIM_NAMES = new string[5]
	{
		"GEN09_WB_PickingObject_Chief_Still",
		"GEN09_WB_PickingObject_Larry_Still",
		"GEN09_WB_PickingObject_Biff_Still",
		"GEN09_WB_PickingObject_Todd_Still",
		"GEN09_WB_PickingObject_heatchliff_Still"
	};
	
	private readonly string[] REVEAL_ICON_ANIM_NAMES = new string[5]
	{
		"GEN09_WB_PickingObject_Chief_Reveal Chief",
		"GEN09_WB_PickingObject_Larry_Reveal Larry",
		"GEN09_WB_PickingObject_Biff_Reveal Biff",
		"GEN09_WB_PickingObject_Todd_Reveal Todd",
		"GEN09_WB_PickingObject_heatchliff_Reveal heathcliff"
	};
	private readonly string[] REVEAL_NUMBER_ANIM_NAMES = new string[5]
	{
		"GEN09_WB_PickingObject_Chief_Reveal Numbers",
		"GEN09_WB_PickingObject_Larry_Reveal Numbers",
		"GEN09_WB_PickingObject_Biff_Reveal Numbers",
		"GEN09_WB_PickingObject_Todd_Reveal Numbers",
		"GEN09_WB_PickingObject_heatchliff_Reveal Numbers"
	};

	private const string PICKEM_BG_MUSIC = "PickBgTiki";
	private const string ITEM_PICKED_SOUND = "MonkeyPickTiki";
	private const string REVEAL_CHARACTER_ICON_SOUND = "WheelSpinRevealMatchingCharacter";
	private readonly string[] CHARACTER_VO_SOUNDS = new string[5]
	{
		"M1Tiki",
		"M1Tiki",
		"M3Tiki",
		"M4Tiki",
		"M2Tiki"
	};
	private const string REVEAL_CREDITS_SOUND = "WheelSpinRevealCreditTiki";
	private const string REVEAL_OTHER_SOUND_KEY = "reveal_not_chosen";
	
	public enum CharacterEnum
	{
		None = -1,
		WickedWitch = 0,
		Dorothy,
		TinMan,
		Lion,
		Scarecrow
	};
	
/*==========================================================================================================*\
	Pre-Init
\*==========================================================================================================*/
	
	public void setSelectedCharacter(CharacterEnum newCharacter)
	{
		selectedCharacter = newCharacter;
	}
	
	public void setJackpotText(long amount, long baseGameMultiplierAtWheelStart)
	{
		jackpotValueFromWheel = amount;
		this.baseGameMultiplierAtWheelStart = baseGameMultiplierAtWheelStart;
		jackpotBoxAmountTextWrapper.text = CreditsEconomy.convertCredits(jackpotValueFromWheel);
	}
	
/*==========================================================================================================*\
	Init
\*==========================================================================================================*/
	
	public override void init(PickemOutcome passedOutcome)
	{
		setTitleLableText();
		updateJackpotCharacter(); // You have to do this before base.init!
		
		base.init(passedOutcome);
		
		Audio.switchMusicKeyImmediate(PICKEM_BG_MUSIC);
		
		// Update the win total with what was carried over from the wheel game.
		
		currentWinAmountTextWrapperNew.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
		
		// Set initial localalized text.
		
		picksRemainingLabelWrapper.text = Localize.textUpper(PICKS_REMAINING_LOC_STRING, 3);
		BonusGamePresenter.instance.useMultiplier = false;
		
		_didInit = true;
	}

	public void setTitleLableText()
	{
		for (int i = 0; i < titleLabels.Length; i++)
		{
			if (i == (int)selectedCharacter)
			{
				titleLabels[i].setMultiLabelEnabledState(true);
				jackpotBoxCharacterIcons[i].SetActive(true);
			}
			else
			{
				titleLabels[i].setMultiLabelEnabledState(false);
				jackpotBoxCharacterIcons[i].SetActive(false);
			}
		}
	}

	public void updateJackpotCharacter()
	{
		// Usually, each PickGameObject contains a single PickGameButton.
		// For this game, each PickGameObject actually contains five different PickGameButtons (one for each character).
		// Rewire each PickGameObject to point to the right PickGameButton (depending on the character),
		// and disable the unused PickGameObjects.
		
		GameObject[] pickGameObjects = newPickGameButtonRounds[0].pickGameObjects;
		
		for (int j = 0; j < pickGameObjects.Length; j++)
		{
			GameObject tikiObject = pickGameObjects[j];
			tikiObject = tikiObject.transform.GetChild(0).gameObject;
			
			PickGameButton[] tikiPicks = tikiObject.GetComponentsInChildren<PickGameButton>();
			
			for (int i = 0; i < tikiPicks.Length; i++)
			{
				GameObject tikiPick = tikiPicks[i].gameObject;
				
				if (i == (int)selectedCharacter)
				{
					pickGameObjects[j] = tikiPick;
				}
				else
				{
					tikiPick.SetActive(false);
				}
			}
		}
	}
	
/*==========================================================================================================*\
	Pick Me
\*==========================================================================================================*/
	
	protected override IEnumerator pickMeAnimCallback()
	{
		PickGameButtonData tikiPickMe = getRandomPickMe();
		
		if (tikiPickMe != null)
		{
			tikiPickMe.animator.Play(PICKME_ANIM_NAMES[(int)selectedCharacter]);
//			Audio.play(PICKME_SOUND);

			// It has to wait one frame before it can get the duration of the animation.			
			yield return null;
			float dur = tikiPickMe.animator.GetCurrentAnimatorStateInfo(0).length;
			yield return new WaitForSeconds(dur);
			
			if (isButtonAvailableToSelect(tikiPickMe))
			{
				tikiPickMe.animator.Play(STILL_ANIM_NAMES[(int)selectedCharacter]);
			}
		}
	}

/*==========================================================================================================*\
	Pickem Button Pressed
\*==========================================================================================================*/
	
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject tikiButton)
	{
		inputEnabled = false;
		
		int tikiIndex = getButtonIndex(tikiButton);
		removeButtonFromSelectableList(tikiButton);
		PickGameButtonData tikiPick = getPickGameButton(tikiIndex);
		
		// Ensure that a pick click doesn't cause the rollup to skip.
		
		yield return null;

		PickemPick pick = outcome.getNextEntry();
		long creditsWon = pick.credits * BonusGameManager.instance.currentMultiplier;
		
		if (pick.groupId.Length > 0)
		{
			// Play the reveal icon anim.

			// Verify that the amount that is being displayed for the tiki matches the amount we are actually paying out
			// if not log an error with the value differences, as well as some info on the multipliers being used so maybe we can
			// better understand what is causing the amounts to not match up here.
			// Will hopefully shed some light on: https://jira.corp.zynga.com/browse/HIR-84685
			if (jackpotValueFromWheel != creditsWon)
			{
				Debug.LogError("gen09PickemTikiGame.pickemButtonPressedCoroutine() - jackpotValueFromWheel = " + jackpotValueFromWheel 
					+ "; did not match the tiki pick jackpot value creditsWon = " + creditsWon 
					+ "; pick creditsWon used BonusGameManager.instance.currentMultiplier = " + BonusGameManager.instance.currentMultiplier
					+ " and GameState.bonusGameMultiplierForLockedWagers = " + GameState.bonusGameMultiplierForLockedWagers 
					+ "; jackpotValueFromWheel used SlotBaseGame.instance.multiplier = " + SlotBaseGame.instance.multiplier
					+ " or possibly baseGameMultiplierAtWheelStart = " + baseGameMultiplierAtWheelStart);
			}

			tikiPick.animator.Play(REVEAL_ICON_ANIM_NAMES[(int)selectedCharacter]);
			Audio.play(ITEM_PICKED_SOUND);

			// It has to wait one frame before it can get the duration of the animation.
			yield return null;
			float dur = tikiPick.animator.GetCurrentAnimatorStateInfo(0).length;
			yield return new TIWaitForSeconds(dur / 2.0f);
			
			Audio.play(REVEAL_CHARACTER_ICON_SOUND);
			Audio.play(CHARACTER_VO_SOUNDS[(int)selectedCharacter]);
			
			Gen09PickGameButton gen09PickGameButton = tikiPick.go.GetComponent<Gen09PickGameButton>();
			gen09PickGameButton.celebration.SetActive(true);

			if(outcome.entryCount == 1)
			{
				picksRemainingLabelWrapper.text = Localize.textUpper(ONE_PICK_REMAINING_LOC_STRING);
			}
			else
			{
				picksRemainingLabelWrapper.text = Localize.textUpper(PICKS_REMAINING_LOC_STRING, outcome.entryCount);
			}
			yield return new TIWaitForSeconds(dur / 2.0f);

			if (jackpotWinAnimationObj != null)
			{
				jackpotWinAnimationObj.SetActive(true);
			}
			
			// Wait for the rollup.
			
			yield return StartCoroutine(
				animateScore(
					BonusGamePresenter.instance.currentPayout,
					BonusGamePresenter.instance.currentPayout + creditsWon));
					
			BonusGamePresenter.instance.currentPayout += creditsWon;
			
			if (jackpotWinAnimationObj != null)
			{
				jackpotWinAnimationObj.SetActive(false);
			}
		}
		else
		{
			// Play the reveal number anim.
			
			tikiPick.revealNumberLabel.text = CreditsEconomy.convertCredits(creditsWon);
			
			tikiPick.animator.Play(REVEAL_NUMBER_ANIM_NAMES[(int)selectedCharacter]);
			Audio.play(ITEM_PICKED_SOUND);

			// It has to wait one frame before it can get the duration of the animation.
			yield return null;
			float dur = tikiPick.animator.GetCurrentAnimatorStateInfo(0).length;
			yield return new TIWaitForSeconds(dur / 2.0f);
			
			// Play this sound halfway through the animation so it doesn't get lost in the rollup sounds.
			Audio.play(REVEAL_CREDITS_SOUND);
			
			if(outcome.entryCount == 1)
			{
				picksRemainingLabelWrapper.text = Localize.textUpper(ONE_PICK_REMAINING_LOC_STRING);
			}
			else
			{
				picksRemainingLabelWrapper.text = Localize.textUpper(PICKS_REMAINING_LOC_STRING, outcome.entryCount);
			}
			yield return new TIWaitForSeconds(dur / 2.0f);
			
			
			// Ensure that a pick click doesn't cause the rollup to skip.
			
			yield return null;
			
			// Wait for the rollup.
			
			yield return StartCoroutine(
				animateScore(
					BonusGamePresenter.instance.currentPayout,
					BonusGamePresenter.instance.currentPayout + creditsWon));
					
			BonusGamePresenter.instance.currentPayout += creditsWon;
		}
			
		if (outcome.entryCount != 0)
		{
			inputEnabled = true;
		}
		else
		{
			// Done with picks, so do reveals.
			
			yield return StartCoroutine(revealRemainingPicks());
			
			// Game has ended.
			
			yield return new TIWaitForSeconds(TIME_BEFORE_SUMMARY);
			BonusGamePresenter.instance.gameEnded();
		}
	}

	private IEnumerator revealRemainingPicks()
	{
		PickemPick pickemReveal = outcome.getNextReveal();
		
		// Gray-out the leftovers.
		
		for (int pickIndex = 0; pickIndex < this.getButtonLengthInRound(); pickIndex++ )
		{
			PickGameButtonData pickGameButtonData = getPickGameButton(pickIndex);
			
			if (this.isButtonAvailableToSelect(pickGameButtonData))
			{
				pickGameButtonData.animator.StopPlayback();
				yield return null;

				Gen09PickGameButton gen09PickGameButton = pickGameButtonData.go.GetComponent<Gen09PickGameButton>();
				gen09PickGameButton.grayOutRevealCharacter();

				gen09PickGameButton.revealNumberLabel.color = Color.gray;
				UILabelStyler textLabelStyler = gen09PickGameButton.revealNumberLabel.gameObject.GetComponent<UILabelStyler>();
				textLabelStyler.style = grayOutStyle;
				textLabelStyler.updateStyle();
			}
		}
		
		float GRAY_TO_REVEAL_DUR = 1.0f;
		yield return new TIWaitForSeconds(GRAY_TO_REVEAL_DUR);
		
		while (pickemReveal != null)
		{
			StartCoroutine(revealTiki(pickemReveal));
			yield return StartCoroutine(revealWait.wait(revealWaitTime));
			
			pickemReveal = outcome.getNextReveal();
		}
	}
	
	private IEnumerator revealTiki(PickemPick pickemReveal)
	{
		int tikiIndex = getButtonIndex(grabNextButtonAndRemoveIt());
		PickGameButtonData tikiPick = getPickGameButton(tikiIndex);
		
		if (pickemReveal.groupId.Length > 0)
		{
			tikiPick.animator.Play(REVEAL_ICON_ANIM_NAMES[(int)selectedCharacter]);
			if(!revealWait.isSkipping)
			{
				Audio.play(Audio.soundMap(REVEAL_OTHER_SOUND_KEY));
			}
			
			// It has to wait one frame before it can get the duration of the animation.
			yield return null;
			float dur = tikiPick.animator.GetCurrentAnimatorStateInfo(0).length;
			yield return new TIWaitForSeconds(dur);
		}
		else
		{
			long creditsWon = pickemReveal.credits * BonusGameManager.instance.currentMultiplier;
			tikiPick.revealNumberLabel.text = CreditsEconomy.convertCredits(creditsWon);
			
			tikiPick.animator.Play(REVEAL_NUMBER_ANIM_NAMES[(int)selectedCharacter]);
			if(!revealWait.isSkipping)
			{
				Audio.play(Audio.soundMap(REVEAL_OTHER_SOUND_KEY));
			}
			
			// It has to wait one frame before it can get the duration of the animation.
			yield return null;
			float dur = tikiPick.animator.GetCurrentAnimatorStateInfo(0).length;
			yield return new TIWaitForSeconds(dur);
		}
	}
	
/*==========================================================================================================*/
		
}

