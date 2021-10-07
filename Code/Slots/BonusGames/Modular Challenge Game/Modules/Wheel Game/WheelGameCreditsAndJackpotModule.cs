using System.Collections;
using UnityEngine;

/**
 * Module to handle wheel credit rollup and jackpot rollup that has zero credit values
 * and wants to do some type of transition or response animation to the jackpot or credit
 * rollup that are different from eachother
 */
public class WheelGameCreditsAndJackpotModule : WheelGameModule
{
	[SerializeField] protected bool executeOnSpinCompleteWithZeroCredits;
	[SerializeField] protected AnimationListController.AnimationInformationList animationsToPlayOnSpinComplete;
	[SerializeField] protected AnimationListController.AnimationInformationList rollupAnimations;
	[SerializeField] protected AnimationListController.AnimationInformationList jackpotAnimations;
	[SerializeField] protected AnimationListController.AnimationInformationList rollupFinishedAnimations;
	[SerializeField] protected AnimationListController.AnimationInformationList jackpotFinishedAnimations;
	[SerializeField] protected float delayBeforeRollup = 0.0f; // May need to delay the rollup start slightly so that the wheel_stop sound isn't aborted
	private long creditsWon;

	// Enable spin complete callback
	public override bool needsToExecuteOnSpinComplete()
	{
		ModularChallengeGameOutcomeRound round = wheelRoundVariantParent.outcome.getRound(wheelRoundVariantParent.roundIndex);
		
		creditsWon = (round.entries != null && round.entries.Count > 0) ? round.entries[0].credits : 0;
		
		if (round != null)
		{
			if (executeOnSpinCompleteWithZeroCredits || creditsWon > 0)
			{
				return true;
			}
			Debug.LogError("Round entries is null or undefined or executeOnSpinCompleteWithZeroCredits is false");
		}
		else
		{
			Debug.LogError("Attempting to find the round, but no round can be found.");
		}
		return false;
	}

	private bool isJackpot()
	{
		return creditsWon < 1;
	}
	
	// Update the winnings label if we've won credits from this outcome
	public override IEnumerator executeOnSpinComplete()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationsToPlayOnSpinComplete));
		
		yield return new TIWaitForSeconds(delayBeforeRollup);
		
		AnimationListController.AnimationInformationList startAnimations = (isJackpot() && jackpotAnimations != null) ? jackpotAnimations : rollupAnimations;
		AnimationListController.AnimationInformationList finishAnimations = (isJackpot() && jackpotFinishedAnimations != null) ? jackpotFinishedAnimations : rollupFinishedAnimations;

		long startValue = BonusGamePresenter.instance.currentPayout;
		long endValue = BonusGamePresenter.instance.currentPayout + creditsWon;

		yield return StartCoroutine(rollupCredits(startAnimations, finishAnimations, startValue, endValue, true));
	}
}
