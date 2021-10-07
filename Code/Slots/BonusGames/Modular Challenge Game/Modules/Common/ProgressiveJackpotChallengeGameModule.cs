using System.Collections;
using UnityEngine;

//
// This module is used for progressive jackpots on challenge games to populate and
// award progressive jackpots
//
// games : bettie02 wheelgame
// Date : Sep 10th, 2019
// Author : Nick Saito <nsaito@zynga.com>
//
public class ProgressiveJackpotChallengeGameModule : ChallengeGameModule
{
	[SerializeField] private AnimationListController.AnimationInformationList rollupAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList rollupFinishedAnimations;
	[SerializeField] private bool useMultiplier;
	[SerializeField] protected string rollupLoopSoundOverride;
	[SerializeField] protected string rollupEndSoundOverride;
	[Tooltip("Labels to display the currently active progressive jackpot tier value")]
	[SerializeField] private LabelWrapperComponent[] progressiveJackpotValueLabels;
	[Tooltip("List of animations to play to celebrate the jackpot win before the rollup actually happens")]
	[SerializeField] private AnimationListController.AnimationInformationList preRollupCelebrationAnimations;

	private long credits;
	private ProgressiveJackpot progressiveJackpot;

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		credits = round.outcome.progressiveJackpotCredits;

		if (credits > 0)
		{
			// we need to extract the progressive information so we can control labels if they're configured
			BuiltInProgressiveJackpotBaseGameModule.BuiltInProgressiveJackpotTierData progTierData = BuiltInProgressiveJackpotBaseGameModule.getCurrentTierData();
			if (progTierData != null)
			{
				progressiveJackpot = progTierData.getProgressiveJackpot();
			}
		}
	}

	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		if (isEndOfGame && credits > 0)
		{
			return true;
		}

		return false;
	}

	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		BonusGamePresenter.instance.useMultiplier = useMultiplier;
		setProgressiveJackpotValueLabelsToJackpotWinAmount(credits);

		if (preRollupCelebrationAnimations.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(preRollupCelebrationAnimations));
		}
		
		long startValue = BonusGamePresenter.instance.currentPayout;
		long endValue = startValue + credits;
		yield return StartCoroutine(rollupCredits(rollupAnimations, rollupFinishedAnimations, startValue, endValue, true, rollupSoundLoopOverride:rollupLoopSoundOverride, rollupSoundEndOverride:rollupEndSoundOverride));
	}
	
	// Unregister the labels from the progressive jackpot so that they don't update when
	// the value changes anymore
	private void unregisterValueLabelsFromProgressiveJackpot()
	{
		for (int i = 0; i < progressiveJackpotValueLabels.Length; i++)
		{
			if (progressiveJackpot != null)
			{
				progressiveJackpot.unregisterLabel(progressiveJackpotValueLabels[i]);
			}
		}
	}
	
	// Update all of the labels that are showing the progressive jackpot amount to the final payout amount
	private void setProgressiveJackpotValueLabelsToJackpotWinAmount(long amount)
	{
		unregisterValueLabelsFromProgressiveJackpot();
		
		for (int i = 0; i < progressiveJackpotValueLabels.Length; i++)
		{
			progressiveJackpotValueLabels[i].text = CreditsEconomy.convertCredits(amount);
		}
	}
}

