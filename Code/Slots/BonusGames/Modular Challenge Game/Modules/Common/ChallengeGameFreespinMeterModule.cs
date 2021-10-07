using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// This module is used for setting the values for freespin meters in a challenge game. This is almost entirely copied from
// FreepinMeterCollectModule.
//
// games : bettie02 wheelgame
// Date : Aug 28th, 2019
// Author : Nick Saito <nsaito@zynga.com>
//
public class ChallengeGameFreespinMeterModule : ChallengeGameModule
{
	// List of the meterAnimation data for the freespin meters
	[SerializeField] private List<FreespinMeterCollectModule.FreespinMeterAnimationData> meterAnimations;

	// The reevaluation data
	private JSON[] arrayReevaluations;

	public override bool needsToExecuteOnRoundInit()
	{
		arrayReevaluations = ReelGame.activeGame.outcome.getArrayReevaluations();
		return arrayReevaluations != null && arrayReevaluations.Length > 0;
	}

	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		ReevaluationFreespinMeter freespinMeterReevaluation = getFreespinMeterReevaluation();

		if (freespinMeterReevaluation == null)
		{
			return;
		}

		foreach (FreespinMeterValue meterValue in freespinMeterReevaluation.meterValues)
		{
			FreespinMeterCollectModule.FreespinMeterAnimationData freespinMeterAnimationData =
				getAnimationDataForTier(meterValue.tier);

			if (freespinMeterAnimationData != null)
			{
				initializeFreespinMeter(freespinMeterAnimationData, meterValue);
			}
		}
	}

	// Get personal jackpot data from reevaluations.
	private ReevaluationFreespinMeter getFreespinMeterReevaluation()
	{
		for (int i = 0; i < arrayReevaluations.Length; i++)
		{
			string reevalType = arrayReevaluations[i].getString("type", "");
			if (reevalType == "free_spin_meter")
			{
				return new ReevaluationFreespinMeter(arrayReevaluations[i]);
			}
		}

		return null;
	}

	// Initialize the freespin meters with the players saved totals
	private void initializeFreespinMeter(
		FreespinMeterCollectModule.FreespinMeterAnimationData freespinMeterAnimationData, FreespinMeterValue meterValue)
	{
		freespinMeterAnimationData.freespinCount = meterValue.freeSpins;
		freespinMeterAnimationData.textLabel.text = CommonText.formatNumber(freespinMeterAnimationData.freespinCount);

		if (freespinMeterAnimationData.freespinCount >= freespinMeterAnimationData.hotLevelThreshold)
		{
			// make sure we don't play audio for freespin meters that are already activates at the start.
			freespinMeterAnimationData.didPlayHotLevelAudioActivated = true;
		}
	}

	private FreespinMeterCollectModule.FreespinMeterAnimationData getAnimationDataForTier(string tier)
	{
		foreach (FreespinMeterCollectModule.FreespinMeterAnimationData animationData in meterAnimations)
		{
			if (animationData.tier == tier)
			{
				return animationData;
			}
		}

		return null;
	}
}

