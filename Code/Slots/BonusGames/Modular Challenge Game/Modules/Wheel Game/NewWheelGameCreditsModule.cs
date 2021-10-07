using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to populate a wheel with credit values, Calling it a NEW becuase old games are using a different way of populating the values, but this is prefered.
 */
public class NewWheelGameCreditsModule : WheelGameModule
{
	[SerializeField] protected AnimationListController.AnimationInformationList animationsToPlayOnSpinComplete;
	[SerializeField] protected AnimationListController.AnimationInformationList rollupAnimations;
	[SerializeField] protected AnimationListController.AnimationInformationList rollupFinishedAnimations;
	[SerializeField] protected float delayBeforeRollup = 0.0f; // May need to delay the rollup start slightly so that the wheel_stop sound isn't aborted
	[SerializeField] protected string rollupLoopSoundOverride;
	[SerializeField] protected string rollupEndSoundOverride;

	private ModularChallengeGameOutcomeRound round = null;
	protected bool isPersonalJackpotOutcome;

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	public override void executeOnRoundInit(ModularWheelGameVariant round, ModularWheel wheel)
	{
		base.executeOnRoundInit(round, wheel);

		// If there is a special key for personal jackpot credit outcome, we should bail out because it
		// has special handling in PersonalJackpotsChallengeGameModule.
		isPersonalJackpotOutcome = round.outcome.outcomeContainsPersonalJackpotOutcome;
	}

	// Enable spin complete callback
	public override bool needsToExecuteOnSpinComplete()
	{
		if (isPersonalJackpotOutcome)
		{
			return false;
		}

		round = wheelRoundVariantParent.outcome.getRound(wheelRoundVariantParent.roundIndex);
		if (round != null)
		{
			if (round.entries != null && round.entries.Count > 0)
			{
				return round.entries[0].credits > 0; // We won some sort of credits.
			}
			else
			{
				Debug.LogError("Round.entries not defined.");
			}
		}
		else
		{
			Debug.LogError("Attempting to find the round, but no round can be found.");
		}

		return false;
	}
	
	// Update the winnings label if we've won credits from this outcome
	public override IEnumerator executeOnSpinComplete()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationsToPlayOnSpinComplete));

		wheelRoundVariantParent.overrideRollupSounds(rollupLoopSoundOverride, rollupEndSoundOverride);
		
		yield return new TIWaitForSeconds(delayBeforeRollup);

		long creditsWon = round.entries[0].credits;

		yield return StartCoroutine(rollupCredits(creditsWon));
	}

	// Execute a rollup with optional animations on elements
	protected IEnumerator rollupCredits(long startValue, long endValue, bool addCredits = true)
	{
		yield return StartCoroutine(rollupCredits(rollupAnimations, rollupFinishedAnimations, startValue, endValue, addCredits));
	}

	// Execute a rollup with optional animations on elements
	protected IEnumerator rollupCredits(long credits, bool addCredits = true)
	{
		yield return StartCoroutine(rollupCredits(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + credits, addCredits));
	}
}
