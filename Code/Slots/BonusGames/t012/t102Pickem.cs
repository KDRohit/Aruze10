using UnityEngine;
using System.Collections;

public class t102Pickem : PickingGame<NewBaseBonusGameOutcome> 
{
	public Animator jackpotAnimationWinArea;
	public GameObject initialStagesOverlay;
	public UILabel jackpotLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent jackpotLabelWrapperComponent;

	public LabelWrapper jackpotLabelWrapper
	{
		get
		{
			if (_jackpotLabelWrapper == null)
			{
				if (jackpotLabelWrapperComponent != null)
				{
					_jackpotLabelWrapper = jackpotLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_jackpotLabelWrapper = new LabelWrapper(jackpotLabel);
				}
			}
			return _jackpotLabelWrapper;
		}
	}
	private LabelWrapper _jackpotLabelWrapper = null;
	
	public UILabel finalWinAmountText;	// To be removed when prefabs are updated.
	public LabelWrapperComponent finalWinAmountTextWrapperComponent;

	public LabelWrapper finalWinAmountTextWrapper
	{
		get
		{
			if (_finalWinAmountTextWrapper == null)
			{
				if (finalWinAmountTextWrapperComponent != null)
				{
					_finalWinAmountTextWrapper = finalWinAmountTextWrapperComponent.labelWrapper;
				}
				else
				{
					_finalWinAmountTextWrapper = new LabelWrapper(finalWinAmountText);
				}
			}
			return _finalWinAmountTextWrapper;
		}
	}
	private LabelWrapper _finalWinAmountTextWrapper = null;
	

	private const string ROUND_SELECTION_PREFIX = "round";
	private const string ROUND_PICKME_ANIM_SUFFIX = " Picking Object_Pick Me";
	private const string ROUND_PICKME_STILL_SUFFIX = " Picking Object_still";
	private const string ROUND_REVEAL_END_SUFFIX = " Picking Object_reveal end";
	private const string ROUND_REVEAL_END_MULTIPLIER = " Picking Object_reveal Multiplier";
	private const string ROUND_REVEAL_ADVANCE_SUFFIX = " Picking Object_reveal advances";
	private const string ROUND_REVEAL_NUMBER_SUFFIX = " Picking Object_reveal number";
	private const string ROUND_REVEAL_GRAY_END_SUFFIX = " Picking Object_not selected end";
	private const string ROUND_REVEAL_GRAY_ADVANCE_SUFFIX = " Picking Object_not selected advances";
	private const string ROUND_REVEAL_GRAY_NUMBER_SUFFIX = " Picking Object_not selected number";
	private const string ROUND_REVEAL_GRAY_MULTIPLIER_SUFFIX = " Picking Object_not selected Multiplier";
	private const string JACKPOT_ANIMATE = "celebration meter ani";
	private const string JACKPOT_STILL = "celebration meter idle";

	private const string INTRO_VO = "bonus_intro_vo";
	private const string BG_MUSIC_1 = "BonusBg1TIBB";
	private const string BG_MUSIC_2 = "BonusBg2TIBB";
	private const string BG_MUSIC_3 = "BonusBg3TIBB";
	private const string BG_MUSIC_4 = "BonusBg4TIBB";
	private const string PICKME_1 = "PickMeRound1TIBB";
	private const string PICKME_2 = "PickMeRound2TIBB";
	private const string PICKME_3 = "PickMeRound3TIBB";
	private const string PICKME_4 = "PickMeRound4TIBB";
	private const string ROUND_1_ADVANCE = "PickemRound1AdvanceTIBB";
	private const string ROUND_2_ADVANCE = "PickemRound2AdvanceTIBB";
	private const string ROUND_3_ADVANCE = "PickemRound3AdvanceTIBB";
	private const string ROUND_1_CREDITS = "PickemRound1CreditsTIBB";
	private const string ROUND_2_CREDITS = "PickemRound2CreditsTIBB";
	private const string ROUND_3_CREDITS = "PickemRound3CreditsTIBB";
	private const string ROUND_4_CREDITS = "PickemRound4CreditsTIBB";
	private const string ROUND_1_ADVANCE_VO = "BonusPickAdvanceVOTIBB";
	private const string ROUND_2_ADVANCE_VO = "BonusPickAdvanceVO2TIBB";
	private const string ROUND_3_ADVANCE_VO = "BonusPickAdvanceVO3TIBB";
	private const string MULTIPLIER_VFX = "FinalPickemMultiplierTIBB";
	private const string LOSE_SFX = "PickemLoseTIBB";
	private const string LOSE_VO = "BonusOverVOTIBB";
	private const string REVEAL_SFX = "reveal_others";

	private const float PICKME_TIME = 3.0f;
	private const float PRE_ROLLUP_WAIT = 0.5f;
	private const float REVEAL_WAIT = 1.0f;
	private const float MID_REVEAL_WAIT = 0.25f;

	private RoundPicks currentRoundPick;

	/// Handle initialization stuff for the game
	public override void init()
	{
		base.init();
		inputEnabled = true;

		Audio.play(Audio.soundMap(INTRO_VO));
		Audio.switchMusicKeyImmediate(BG_MUSIC_1);
		currentRoundPick = outcome.roundPicks[currentStage];
		jackpotLabelWrapper.text = CreditsEconomy.convertCredits(currentRoundPick.getHighestPossibleCreditValue());
	}

	/// Triggered periodically to draw the users eye
	protected override IEnumerator pickMeAnimCallback()
	{	
		PickGameButtonData pickButton = getRandomPickMe();

		if (pickButton != null)
		{		
			pickButton.animator.Play(ROUND_SELECTION_PREFIX + (currentStage+1) + ROUND_PICKME_ANIM_SUFFIX);
			switch (currentStage)
			{
				case 0:
					Audio.play(PICKME_1);
					break;
				case 1:
					Audio.play(PICKME_2);
					break;
				case 2:
					Audio.play(PICKME_3);
					break;
				case 3:
					Audio.play(PICKME_4);
					break;
			}
			
			yield return new TIWaitForSeconds(PICKME_TIME);
				
			if (isButtonAvailableToSelect(pickButton))
			{
				pickButton.animator.Play(ROUND_SELECTION_PREFIX + (currentStage+1) + ROUND_PICKME_STILL_SUFFIX);
			}
		}
		yield return new WaitForSeconds(PICKME_TIME);
	}

	/// Called when a button is pressed
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		inputEnabled = false;
		StartCoroutine(type1ButtonSelected(button));

		yield return null;
	}

	private IEnumerator type1ButtonSelected(GameObject button)
	{
		removeButtonFromSelectableList(button);
		int pickIndex = getButtonIndex(button);
		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);
		BasePick currentPick = currentRoundPick.getNextEntry();

		if (currentPick.isGameOver)
		{
			Audio.play(LOSE_SFX);
			Audio.play(LOSE_VO);
			pickButtonData.animator.Play(ROUND_SELECTION_PREFIX + (currentStage+1) + ROUND_REVEAL_END_SUFFIX);
			yield return new TIWaitForSeconds(PRE_ROLLUP_WAIT);
			BonusGamePresenter.instance.currentPayout += currentPick.credits;
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentPick.credits, BonusGamePresenter.instance.currentPayout));

			yield return StartCoroutine(endStage(true));
		}
		else if (currentPick.canAdvance)
		{
			switch (currentStage)
			{
				case 0:
					Audio.play(ROUND_1_ADVANCE);
					Audio.play(ROUND_1_ADVANCE_VO, 1, 0, 0.8f);
					break;
				case 1:
					Audio.play(ROUND_2_ADVANCE);
					Audio.play(ROUND_2_ADVANCE_VO, 1, 0, 0.8f);
					break;
				case 2:
					Audio.play(ROUND_3_ADVANCE);
					Audio.play(ROUND_3_ADVANCE_VO, 1, 0, 0.8f);
					break;
			}
			jackpotAnimationWinArea.Play(JACKPOT_ANIMATE);
			pickButtonData.animator.Play(ROUND_SELECTION_PREFIX + (currentStage+1) + ROUND_REVEAL_ADVANCE_SUFFIX);
			yield return new TIWaitForSeconds(PRE_ROLLUP_WAIT);
			BonusGamePresenter.instance.currentPayout += currentPick.credits;
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentPick.credits, BonusGamePresenter.instance.currentPayout));
			yield return StartCoroutine(endStage(false));
			continueToNextStage();

			switch (currentStage)
			{
				case 1:
					Audio.switchMusicKeyImmediate(BG_MUSIC_2);
					if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
					{
						BonusGameManager.instance.wings.forceShowSecondaryChallengeWings(true);
					}
					break;
				case 2:
					Audio.switchMusicKeyImmediate(BG_MUSIC_3);
					if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
					{
						BonusGameManager.instance.wings.forceShowThirdChallengeWings(true);
					}
					break;
				case 3:
					Audio.switchMusicKeyImmediate(BG_MUSIC_4);
					if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
					{
						BonusGameManager.instance.wings.forceShowFourthChallengeWings(true);
					}
					break;
			}

			currentRoundPick = outcome.roundPicks[currentStage];
			jackpotAnimationWinArea.Play(JACKPOT_STILL);

			if (currentStage != 3)
			{
				jackpotLabelWrapper.text = CreditsEconomy.convertCredits(currentRoundPick.getHighestPossibleCreditValue());
			}
			else
			{
				initialStagesOverlay.SetActive(false);
				currentWinAmountTextWrapperNew = finalWinAmountTextWrapper;
				currentWinAmountTextWrapperNew.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
			}
			inputEnabled = true;
		}
		else
		{
			if (currentPick.multiplier == 0)
			{
				switch (currentStage)
				{
					case 0:
						Audio.play(ROUND_1_CREDITS);
						break;
					case 1:
						Audio.play(ROUND_2_CREDITS);
						break;
					case 2:
						Audio.play(ROUND_3_CREDITS);
						break;
					case 3:
						Audio.play(ROUND_4_CREDITS);
						break;
				}

				pickButtonData.animator.Play(ROUND_SELECTION_PREFIX + (currentStage+1) + ROUND_REVEAL_NUMBER_SUFFIX);
				pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
				pickButtonData.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
				if (currentStage == 3)
				{
					pickButtonData.multiplierLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
				}
				yield return new TIWaitForSeconds(PRE_ROLLUP_WAIT);
				BonusGamePresenter.instance.currentPayout += currentPick.credits;
				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentPick.credits, BonusGamePresenter.instance.currentPayout));
				if (currentStage == 3)
				{
					yield return StartCoroutine(endStage(true));
				}
				else
				{
					inputEnabled = true;	
				}
			}
			else
			{
				Audio.play(MULTIPLIER_VFX);
				pickButtonData.animator.Play(ROUND_SELECTION_PREFIX + (currentStage+1) + ROUND_REVEAL_END_MULTIPLIER);
				pickButtonData.revealNumberLabel.text = Localize.text("{0}X", CommonText.formatNumber(currentPick.multiplier));
				pickButtonData.revealNumberOutlineLabel.text = Localize.text("{0}X", CommonText.formatNumber(currentPick.multiplier));
				yield return new TIWaitForSeconds(PRE_ROLLUP_WAIT);
				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout * currentPick.multiplier));
				BonusGamePresenter.instance.currentPayout *= currentPick.multiplier;
				//if (currentStage == 3)
				//{
					yield return StartCoroutine(endStage(true));
				//}
				//else
				//{
				//	inputEnabled = true;
				//}
			}
		}
	}

	private IEnumerator endStage(bool endGame = false)
	{
		yield return new TIWaitForSeconds(REVEAL_WAIT);
		BasePick revealPick = currentRoundPick.getNextReveal();
		while (revealPick != null)
		{
			revealIcon(revealPick);
			yield return StartCoroutine(revealWait.wait(MID_REVEAL_WAIT));
			revealPick = currentRoundPick.getNextReveal();
		}

		yield return new TIWaitForSeconds(REVEAL_WAIT);

		if (endGame)
		{
			// See HIR-17162
			//Audio.play(Audio.soundMap("bonus_summary_vo"));
			BonusGamePresenter.instance.gameEnded();
		}
	}

	private void revealIcon(BasePick revealPick)
	{
		if (anyButtonsAvailableToSelect())
		{
			GameObject button = grabNextButtonAndRemoveIt(currentStage);
			int pickIndex = getButtonIndex(button);
			PickGameButtonData pickButtonData = getPickGameButton(pickIndex);
			if(!revealWait.isSkipping)
			{
				Audio.play(REVEAL_SFX);
			}

			if (revealPick.isGameOver)
			{
				pickButtonData.animator.Play(ROUND_SELECTION_PREFIX + (currentStage+1) + ROUND_REVEAL_GRAY_END_SUFFIX);
			}
			else if (revealPick.canAdvance)
			{
				pickButtonData.animator.Play(ROUND_SELECTION_PREFIX + (currentStage+1) + ROUND_REVEAL_GRAY_ADVANCE_SUFFIX);
			}
			else
			{
				if (revealPick.multiplier == 0)
				{
					pickButtonData.animator.Play(ROUND_SELECTION_PREFIX + (currentStage+1) + ROUND_REVEAL_GRAY_NUMBER_SUFFIX);
					pickButtonData.extraLabel.text = CreditsEconomy.convertCredits(revealPick.credits);
					pickButtonData.extraOutlineLabel.text = CreditsEconomy.convertCredits(revealPick.credits);
				}
				else
				{
					pickButtonData.animator.Play(ROUND_SELECTION_PREFIX + (currentStage+1) + ROUND_REVEAL_GRAY_MULTIPLIER_SUFFIX);
					//pickButtonData.extraLabel.text = Localize.text("{0}X", CommonText.formatNumber(revealPick.multiplier));
					//pickButtonData.extraOutlineLabel.text = Localize.text("{0}X", CommonText.formatNumber(revealPick.multiplier));
				}
			}
		}
	}

}

