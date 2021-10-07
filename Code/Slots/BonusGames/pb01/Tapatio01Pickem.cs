using UnityEngine;
using System.Collections;

public class Tapatio01Pickem : PickingGame<NewBaseBonusGameOutcome> 
{
	[SerializeField] private Animator endBonusAnimator;
	[SerializeField] private Vector3 finalWinValuePosition;
	[SerializeField] private int gameOverLimit = 3;
	private int gameOverCount = 0;
	[SerializeField] private bool hideEndAnimatorOnLastRound = true;

	private RoundPicks currentRoundPicks;
	private BasePick currentPick;

	private const string PICK_ADVANCE_ANIMATION_NAME = "Pick Advance";
	private const string REVEAL_ADVANCE_ANIMATION_NAME = "Reveal Advance";
	private const string PICK_END_ANIMATION_NAME = "Pick End";
	private const string REVEAL_END_ANIMATION_NAME = "Reveal End";
	private const string PICK_CREDITS_ANIMATION_NAME = "Pick Credits";
	private const string REVEAL_CREDITS_ANIMATION_NAME = "Reveal Credits";
	private const string PICK_MULTIPLIER_ANIMATION_NAME = "Pick Multiplier";
	private const string REVEAL_MULTIPLIER_ANIMATION_NAME = "Reveal Multiplier";
	
	private const string SHOW_NUMBER_GAMEOVERS_ANIMATION = "Show {0} Game Over";

	// Constants
	private const string INTRO1_VO = "PizzaPickVOTapatio"; //"BonusIntroVOTapatio"; I think this gets played on web during the intro screen.
	private const string INTRO2_VO = "TacoPickVOTapatio";
	private const string INTRO3_VO = "DrinkPickMeVOTapatio";
		// Background tunes
		private const string BACKGROUND_MUSIC_1 = "BonusBgTapatioV1";
		private const string BACKGROUND_MUSIC_2 = "BonusBgTapatioV2";
		private const string BACKGROUND_MUSIC_3 = "BonusBgTapatioV3";

		// Pickme Sounds
		private const string PICKME1 = "PizzaPickMeTapatio";
		private const string PICKME2 = "TacoPickMeTapatio";
		private const string PICKME3 = "DrinkPickMeTapatio";
		// Pick Sounds
		private const string PICK1 = "PizzaPickASlice";
		private const string PICK2 = "TacoPickATaco";
		private const string PICK3 = "DrinkPickADrink";
		// Pick VO Sounds
		private const string PICK_VO1 = "PizzaPickVOTapatio";
		private const string PICK_VO2 = "TacoPickATaco";
		private const string PICK_VO3 = "DrinkPickMeVOTapatio";
		// Reveal Bad Sounds
		private const string PICK_BAD = "PickRevealBottle";
		// Reveal Advance Sounds
		private const string PICK_ADVANCE1 = "PizzaRevealLogo1";
		private const string PICK_ADVANCE2 = "PizzaRevealLogo2";
		// Reveal Credits sounds
		private const string PICK_CREDIT3 = "DrinkRevealCredit";
		// Reveal Multiplier
		private const string PICK_MULTIPLIER = "DrinkRevealMultiplier";
		// Reveal Other sounds
		private const string REVEAL_OTHER3 = "DrinkRevealOtherTapatio"; // Reveal sound for the last round.




	/// Handle initialization stuff for the game
	public override void init()
	{
		base.init();
		if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
		{
			BonusGameManager.instance.wings.forceShowChallengeWings(true);
		}
		currentRoundPicks = outcome.roundPicks[currentStage];
		currentPick = currentRoundPicks.getNextEntry();
		Audio.switchMusicKeyImmediate(BACKGROUND_MUSIC_1);
		Audio.play(INTRO1_VO, 1.0f, 0.0f, 0.7f);
		pickMeSoundName = ""; // We don't want to play this sound. It needs to be custom.
	}

	protected override IEnumerator pickMeAnimCallback()
	{
		if (inputEnabled)
		{
			switch (currentStage)
			{
				case 0:
					Audio.play(PICKME1);
					break;
				case 1:
					Audio.play(PICKME2);
					break;
				case 2:
					Audio.play(PICKME3);
					break;
			}
		}
		yield return StartCoroutine(base.pickMeAnimCallback());
	}

	/// Called when a button is pressed
	protected override IEnumerator pickemButtonPressedCoroutine(GameObject button)
	{
		inputEnabled = false;
		removeButtonFromSelectableList(button);
		int pickIndex = getButtonIndex(button);
		PickGameButtonData pickButtonData = getPickGameButton(pickIndex);

		BonusGamePresenter.instance.currentPayout += currentPick.credits;

		// Play the right sound
		switch (currentStage)
		{
			case 0:
				Audio.play(PICK1);
				//Audio.play(PICK_VO1);
				break;
			case 1:
				Audio.play(PICK2);
				Audio.play(PICK_VO2);
				break;
			case 2:
				Audio.play(PICK3);
				Audio.play(PICK_VO3);
				break;
		}

		if (currentPick.isGameOver)
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickButtonData.animator, PICK_END_ANIMATION_NAME));
			Audio.play(PICK_BAD + (gameOverCount + 1));
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentPick.credits, BonusGamePresenter.instance.currentPayout));
			gameOverCount++;
			if (endBonusAnimator != null)
			{
				StartCoroutine(CommonAnimation.playAnimAndWait(endBonusAnimator, string.Format(SHOW_NUMBER_GAMEOVERS_ANIMATION, gameOverCount)));
			}

			if (gameOverCount == gameOverLimit)
			{
				yield return StartCoroutine(endStage(true));
			}
		}
		else if (currentPick.canAdvance)
		{
			switch (currentStage)
			{
				case 0:
					Audio.play(PICK_ADVANCE1);
					break;
				case 1:
					Audio.play(PICK_ADVANCE2);
					break;
			}
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickButtonData.animator, PICK_ADVANCE_ANIMATION_NAME));
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentPick.credits, BonusGamePresenter.instance.currentPayout));
			currentRoundPicks = outcome.roundPicks[currentStage];
		}
		else if (currentPick.multiplier > 1)
		{
			changeLabel(pickButtonData.multiplierLabel, Localize.text("{0}X", CommonText.formatNumber(currentPick.multiplier)));
			Audio.play(PICK_MULTIPLIER);
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickButtonData.animator, PICK_MULTIPLIER_ANIMATION_NAME));
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout * (currentPick.multiplier)));
			BonusGamePresenter.instance.currentPayout *= (currentPick.multiplier);
		}
		else if (currentPick.credits > 0)
		{
			switch (currentStage)
			{
				case 2:
					Audio.play(PICK_CREDIT3);
					break;
			}
			changeLabel(pickButtonData.revealNumberLabel, CreditsEconomy.convertCredits(currentPick.credits));
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickButtonData.animator, PICK_CREDITS_ANIMATION_NAME));
			yield return StartCoroutine(animateScore(BonusGamePresenter.instance.currentPayout - currentPick.credits, BonusGamePresenter.instance.currentPayout));
		}
		else
		{
			Debug.LogError("No sure how to process pick.");
		}

		currentPick = currentRoundPicks.getNextEntry();

		if (currentPick == null) /* We have finished this round */
		{
			yield return StartCoroutine(endStage(true)); // Ends game.
		}
		else
		{
			inputEnabled = true;
		}
	}

	private IEnumerator endStage(bool endGame = false)
	{
		// First we need to reveal all of the remaining picks.
		BasePick revealPick = currentRoundPicks.getNextReveal();
		revealWait.reset ();
		int revealCount = 0;
		while (revealPick != null)
		{
			revealCount++;
			revealIcon(revealPick);
			yield return StartCoroutine(revealWait.wait(revealWaitTime));
			revealPick = currentRoundPicks.getNextReveal();
		}

		yield return new TIWaitForSeconds(1.0f);
		
		// Now we want to see if we should end the game of advance. 
		if (outcome.roundPicks.ContainsKey(currentStage + 1))
		{
			continueToNextStage(); // Advances the current stage counter.
			// Set the wings.
			switch (currentStage)
			{
				case 0:
					if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
					{
						BonusGameManager.instance.wings.forceShowChallengeWings(true);
					}
					break;
				case 1:
					Audio.switchMusicKeyImmediate(BACKGROUND_MUSIC_2);
					Audio.play(INTRO2_VO, 1.0f, 0.0f, 0.7f);
					if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
					{
						BonusGameManager.instance.wings.forceShowSecondaryChallengeWings(true);
					}
					break;
				case 2:
					Audio.switchMusicKeyImmediate(BACKGROUND_MUSIC_3);
					Audio.play(INTRO3_VO, 1.0f, 0.0f, 0.7f);
					if (BonusGameManager.instance != null && BonusGameManager.instance.wings != null)
					{
						BonusGameManager.instance.wings.forceShowThirdChallengeWings(true);
					}
					break;
			}
			inputEnabled = true;
			currentRoundPicks = outcome.roundPicks[currentStage];
			currentPick = currentRoundPicks.getNextEntry();
			if (currentStage == stageObjects.Length - 1)
			{
				currentWinAmountTextWrapperNew.transform.localPosition = finalWinValuePosition;
				if (hideEndAnimatorOnLastRound && endBonusAnimator != null)
				{
					endBonusAnimator.gameObject.SetActive(false);
				}
			}
		}
		else
		{
			BonusGamePresenter.instance.gameEnded();
		}
	}

	private void revealIcon(BasePick revealPick)
	{
		if (anyButtonsAvailableToSelect())
		{
			PickGameButtonData pickButtonData = removeNextPickGameButton();

			switch (currentStage)
			{
				case 0:
				case 1:
					if(!revealWait.isSkipping)
					{
						Audio.play(Audio.soundMap("reveal_not_chosen"));
					}
					break;
				case 2:
					if(!revealWait.isSkipping)
					{
						Audio.play(REVEAL_OTHER3);
					}
					break;
			}
			if (revealPick.isGameOver)
			{
				pickButtonData.animator.Play(REVEAL_END_ANIMATION_NAME);
			}
			else if (revealPick.canAdvance)
			{
				pickButtonData.animator.Play(REVEAL_ADVANCE_ANIMATION_NAME);
			}
			else if (revealPick.multiplier > 1)
			{
				pickButtonData.animator.Play(REVEAL_MULTIPLIER_ANIMATION_NAME);
			}
			else
			{
				pickButtonData.animator.Play(REVEAL_CREDITS_ANIMATION_NAME);

				if (currentStage != 2)
				{
					changeLabel(pickButtonData.revealNumberLabel, CreditsEconomy.convertCredits(revealPick.credits));
				}
				else
				{
					changeLabel(pickButtonData.revealGrayNumberLabel, CreditsEconomy.convertCredits(revealPick.credits));
				}
			}
		}
	}

	// Changes a text field and all of it's Mutilabel childen in one pass.
	// It's possible that this should go into the parent class, but I think we're holding off until a rework.
	private void changeLabel(UILabel label, string value)
	{
		if (label != null)
		{
			label.text = value;
			MultiLabel multiLabel = label.GetComponent<MultiLabel>();
			if (multiLabel != null)
			{
				multiLabel.Update();
			}
		}
	}
}
