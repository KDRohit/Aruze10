using UnityEngine;
using System.Collections;

public class Kendra01Pickem : PickingGame<NewBaseBonusGameOutcome> 
{
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
	
	public Animator[] roundIconAnimators;
	public GameObject winBoxParticles;
	public GameObject intialStagesBGSet;
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
	
	public ParticleSystem[] leftParticles;
	public ParticleSystem[] rightParticles;

	private RoundPicks currentRoundPick;

	private const string PICKME_ANIM = "kendra01_picking object_pick me";
	private const string PICKME_STILL = "kendra01_picking object_Still";
	private const string TYPE_1_REVEAL_END = "kendra01_picking object_reveal end";
	private const string TYPE_1_REVEAL_ADVANCE = "kendra01_picking object_reveal kendra";
	private const string TYPE_1_REVEAL_NUMBER = "kendra01_picking object_reveal number";
	private const string TYPE_1_REVEAL_END_GRAY = "kendra01_picking object_not selected end";
	private const string TYPE_1_REVEAL_ADVANCE_GRAY = "kendra01_picking object_not selected kendra";
	private const string TYPE_1_REVEAL_NUMBER_GRAY = "kendra01_picking object_not selected number";
	private const string FINAL_PICKME_ANIM = "kendra01_final round picking object_pick me";
	private const string FINAL_PICKME_STILL = "kendra01_final round picking object_still";
	private const string ICON_CELEBRATION_1 = "kendra01_first_round_icon_celebration";
	private const string ICON_CELEBRATION_2 = "kendra01_second_round_icon_celebration";
	private const string ICON_CELEBRATION_3 = "kendra01_third_round_icon_celebration";
	private const string ICON_CELEBRATION_4 = "kendra01_final_round_icon_celebration";

	private const string INTRO_VO = "bonus_intro_vo";
	private const string PICKME_1 = "PickMeRound1Kendra";
	private const string PICKME_2 = "PickMeRound2Kendra";
	private const string PICKME_3 = "PickMeRound3Kendra";
	private const string PICKME_4 = "PickMeRound4Kendra";
	private const string REVEAL_SFX = "reveal_others";
	private const string LOSE_SFX = "PickemLoseKendra";
	private const string LOSE_VO = "BonusGameOverVOKendra";
	private const string ADVANCE_SFX_1 = "Pick1Kendra";
	private const string ADVANCE_VO_1 = "BonusRound1VOKendra";
	private const string ADVANCE_SFX_2 = "Pick2Kendra";
	private const string ADVANCE_VO_2 = "BonusRound2VOKendra";
	private const string ADVANCE_SFX_3 = "Pick3Kendra";
	private const string ADVANCE_VO_3 = "BonusRound3VOKendra";
	private const string ADVANCE_SFX_4 = "Pick4Kendra";
	private const string ADVANCE_VO_4 = "BonusRound4VOKendra";
	private const string BG_MUSIC_1 = "BonusBg1Kendra";
	private const string BG_MUSIC_2 = "BonusBg2Kendra";
	private const string BG_MUSIC_3 = "BonusBg3Kendra";
	private const string BG_MUSIC_4 = "BonusBg4Kendra";
	private const string NUMBER_REVEAL_1 = "PickemRound1CreditsKendra";
	private const string NUMBER_REVEAL_2 = "PickemRound2CreditsKendra";
	private const string NUMBER_REVEAL_3 = "PickemRound3CreditsKendra";
	private const string NUMBER_REVEAL_4 = "PickemRound4CreditsKendra";
	private const string MULTIPLIER_REVEAL_SFX = "FinalPickemMultiplierKendra";
	private const string MULTIPLIER_REVEAL_VO = "BonusRound4VOKendra";

	private const float PICKME_TIME = 1.0f;
	private const float PRE_ROLLUP_WAIT = 0.5f;
	private const float PRE_REVEAL_WAIT = 0.5f;
	private const float REVEAL_WAIT = 0.15f;
	private const float POST_REVEAL_WAIT_LONG = 2.0f;
	private const float POST_REVEAL_WAIT_SHORT = 1.0f;

	/// Handle initialization stuff for the game
	public override void init()
	{
		base.init();

		Audio.play(Audio.soundMap(INTRO_VO));
		Audio.switchMusicKeyImmediate(BG_MUSIC_1);
		currentRoundPick = outcome.roundPicks[currentStage];
		foreach (Animator roundIconAnimator in roundIconAnimators)
		{
			roundIconAnimator.StopPlayback();
		}
		jackpotLabelWrapper.text = CreditsEconomy.convertCredits(currentRoundPick.getHighestPossibleCreditValue());
	}

	/// Triggered periodically to draw the users eye
	protected override IEnumerator pickMeAnimCallback()
	{	
		PickGameButtonData pickButton = getRandomPickMe();

		if (pickButton != null)
		{		
			if (currentStage == 3)
			{
				pickButton.animator.Play(FINAL_PICKME_ANIM);
			}
			else
			{
				pickButton.animator.Play(PICKME_ANIM);
			}
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
				if (currentStage == 3)
				{
					pickButton.animator.Play(FINAL_PICKME_STILL);
				}
				else
				{
					pickButton.animator.Play(PICKME_STILL);
				}
			}
		}
		yield return new WaitForSeconds(PICKME_TIME);
	}

	/// Called when a button is pressed
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		inputEnabled = false;
		switch (currentStage)
		{
			case 0:
				StartCoroutine(type1ButtonSelected(button));
				break;
			case 1:
				StartCoroutine(type1ButtonSelected(button));
				break;
			case 2:
				StartCoroutine(type1ButtonSelected(button));
				break;
			case 3:
				StartCoroutine(type1ButtonSelected(button));
				break;
		}

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
			pickButtonData.animator.Play(TYPE_1_REVEAL_END);
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
					Audio.play(ADVANCE_SFX_1);
					Audio.play(ADVANCE_VO_1);
					break;
				case 1:
					Audio.play(ADVANCE_SFX_2);
					Audio.play(ADVANCE_VO_2);
					break;
				case 2:
					Audio.play(ADVANCE_SFX_3);
					Audio.play(ADVANCE_VO_3);
					break;
				case 3:
					Audio.play(ADVANCE_SFX_4);
					Audio.play(ADVANCE_VO_4);
					break;
			}

			pickButtonData.animator.Play(TYPE_1_REVEAL_ADVANCE);
			winBoxParticles.SetActive(true);
			leftParticles[currentStage].gameObject.SetActive(true);
			rightParticles[currentStage].gameObject.SetActive(true);
			switch (currentStage)
			{
				case 0:
					roundIconAnimators[currentStage].Play(ICON_CELEBRATION_1);
					break;
				case 1:
					roundIconAnimators[currentStage].Play(ICON_CELEBRATION_2);
					break;
				case 2:
					roundIconAnimators[currentStage].Play(ICON_CELEBRATION_3);
					break;
				case 3:
					roundIconAnimators[currentStage].Play(ICON_CELEBRATION_4);
					break;	
			}
			yield return new TIWaitForSeconds(PRE_ROLLUP_WAIT);
			BonusGamePresenter.instance.currentPayout += currentPick.credits;
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentPick.credits, BonusGamePresenter.instance.currentPayout));
			yield return StartCoroutine(endStage());
			winBoxParticles.SetActive(false);
			switch (currentStage)
			{
				case 0:
					Audio.switchMusicKeyImmediate(BG_MUSIC_2);
					break;
				case 1:
					Audio.switchMusicKeyImmediate(BG_MUSIC_3);
					break;
				case 2:
					Audio.switchMusicKeyImmediate(BG_MUSIC_4);
					break;
			}

			continueToNextStage();
			currentRoundPick = outcome.roundPicks[currentStage];
			if (currentStage < 3)
			{
				leftParticles[currentStage].Stop();
				leftParticles[currentStage].Clear();
				rightParticles[currentStage].Stop();
				rightParticles[currentStage].Clear();
				leftParticles[currentStage].gameObject.SetActive(false);
				rightParticles[currentStage].gameObject.SetActive(false);
			}
			if (currentStage != 3)
			{
				jackpotLabelWrapper.text = CreditsEconomy.convertCredits(currentRoundPick.getHighestPossibleCreditValue());
			}
			else
			{
				intialStagesBGSet.SetActive(false);
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
						Audio.play(NUMBER_REVEAL_1);
						break;
					case 1:
						Audio.play(NUMBER_REVEAL_2);
						break;
					case 2:
						Audio.play(NUMBER_REVEAL_3);
						break;
					case 3:
						Audio.play(NUMBER_REVEAL_4);
						break;
				}
				pickButtonData.animator.Play(TYPE_1_REVEAL_NUMBER);
				pickButtonData.revealNumberLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
				pickButtonData.revealNumberOutlineLabel.text = CreditsEconomy.convertCredits(currentPick.credits);
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
				Audio.play(MULTIPLIER_REVEAL_SFX);
				Audio.play(MULTIPLIER_REVEAL_VO);
				pickButtonData.animator.Play(TYPE_1_REVEAL_ADVANCE);
				pickButtonData.revealNumberLabel.text = Localize.text("{0}X", CommonText.formatNumber(currentPick.multiplier));
				pickButtonData.revealNumberOutlineLabel.text = Localize.text("{0}X", CommonText.formatNumber(currentPick.multiplier));
				yield return new TIWaitForSeconds(PRE_ROLLUP_WAIT);
				BonusGamePresenter.instance.currentPayout *= currentPick.multiplier;
				yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout/currentPick.multiplier, BonusGamePresenter.instance.currentPayout));
				if (currentStage == 3)
				{
					yield return StartCoroutine(endStage(true));
				}
				else
				{
					inputEnabled = true;
				}
			}
		}
	}

	private IEnumerator endStage(bool endGame = false)
	{
		revealWait.reset ();
		yield return new TIWaitForSeconds(PRE_REVEAL_WAIT);
		BasePick revealPick = currentRoundPick.getNextReveal();
		int revealCount = 0;
		while (revealPick != null)
		{
			revealCount++;
			revealIcon(revealPick);
			yield return StartCoroutine(revealWait.wait(REVEAL_WAIT));
			revealPick = currentRoundPick.getNextReveal();
		}

		// If there were reveals, give a little extra time so that we can see the results for longer.
		if (revealCount > 0)
		{
			yield return new TIWaitForSeconds(POST_REVEAL_WAIT_LONG);
		}
		else
		{
			yield return new TIWaitForSeconds(POST_REVEAL_WAIT_SHORT);
		}

		if (endGame)
		{
			//Audio.play("BonusSummaryVOPbride", 1, 0, POST_REVEAL_WAIT_LONG);
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
			if(!revealWait.isSkipping){
				Audio.play(REVEAL_SFX);
			}

			if (revealPick.isGameOver)
			{
				pickButtonData.animator.Play(TYPE_1_REVEAL_END_GRAY);
			}
			else if (revealPick.canAdvance)
			{
				pickButtonData.animator.Play(TYPE_1_REVEAL_ADVANCE_GRAY);
			}
			else
			{
				if (revealPick.multiplier == 0)
				{
					pickButtonData.animator.Play(TYPE_1_REVEAL_NUMBER_GRAY);
					pickButtonData.extraLabel.text = CreditsEconomy.convertCredits(revealPick.credits);
				}
				else
				{
					pickButtonData.animator.Play(TYPE_1_REVEAL_ADVANCE_GRAY);
					//pickButtonData.extraLabel.text = Localize.text("{0}X", CommonText.formatNumber(revealPick.multiplier));
				}
			}
		}
	}
}

