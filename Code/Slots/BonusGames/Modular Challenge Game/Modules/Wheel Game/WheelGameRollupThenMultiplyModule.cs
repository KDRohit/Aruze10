using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to roll up the win amount and then play an animation 
 */
public class WheelGameRollupThenMultiplyModule : NewWheelGameCreditsModule
{
	[SerializeField] protected ParticleTrailController sparkleTrailMultiplierOrigin;
	[SerializeField] protected GameObject sparkleTrailTarget;
	[SerializeField] protected AnimationListController.AnimationInformationList animationsToPlayAfterRollup;
	[SerializeField] protected float delayBeforeMultiply = 0.0f;
	[SerializeField] protected bool isRollingUpToMultipliedValue = false; // this controls if the credit value jumps to the new credit value, or rolls up to it
	
	// Update the winnings label if we've won credits from this outcome
	public override IEnumerator executeOnSpinComplete()
	{
		// Roll up the base amount.
		yield return StartCoroutine(base.executeOnSpinComplete());
		long multiplier = 1;
		if (ReelGame.activeGame != null)
		{
			multiplier = ReelGame.activeGame.relativeMultiplier;
		}
		if (multiplier > 1)
		{
			List<TICoroutine> runningAnimations = new List<TICoroutine>();
			runningAnimations.Add(StartCoroutine(playMultiplyOnDelay(delayBeforeMultiply)));
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationsToPlayAfterRollup, runningAnimations));
		}
	}

	protected IEnumerator playParticleTrail()
	{
		yield return StartCoroutine(sparkleTrailMultiplierOrigin.animateParticleTrail(sparkleTrailTarget.transform.position, sparkleTrailTarget.transform));
	}

	private IEnumerator playMultiplyOnDelay(float delay)
	{
		if (delay > 0.0f)
		{
			yield return new TIWaitForSeconds(delay);
		}
		long creditsWon = wheelRoundVariantParent.outcome.getRound(wheelRoundVariantParent.roundIndex).entries[0].credits;
		long multiplier = 1;
		if (ReelGame.activeGame != null)
		{
			multiplier = ReelGame.activeGame.relativeMultiplier;
		}
		creditsWon = creditsWon * (multiplier - 1);

		if (sparkleTrailMultiplierOrigin != null && sparkleTrailTarget != null)
		{
			yield return StartCoroutine(playParticleTrail());
		}

		if (isRollingUpToMultipliedValue)
		{
			yield return StartCoroutine(wheelRoundVariantParent.animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + creditsWon));
		}
		else
		{
			wheelRoundVariantParent.winLabel.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout + creditsWon);
		}
	}
}
