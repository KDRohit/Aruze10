using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module that handles a wheel that only awards multipliers.  Will apply multiplier to current win amount.
Will support a particle trail controller if you want a particle trail to go from the multiplier to the win box.

Creation Date: 8/30/2018
Original Author: Scott Lepthien
*/
public class WheelGameMultiplierModule : WheelGameModule 
{
	[SerializeField] protected ParticleTrailController sparkleTrailMultiplierOrigin;
	[SerializeField] protected GameObject sparkleTrailTarget;
	[SerializeField] protected AnimationListController.AnimationInformationList animationsToPlayAfterRollup;
	
	// Enable spin complete callback
	public override bool needsToExecuteOnSpinComplete()
	{
		return true;
	}
	
	// Update the winnings label if we've won credits from this outcome
	public override IEnumerator executeOnSpinComplete()
	{
		yield return StartCoroutine(awardMultiplier());
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationsToPlayAfterRollup));
	}

	protected IEnumerator playParticleTrail()
	{
		yield return StartCoroutine(sparkleTrailMultiplierOrigin.animateParticleTrail(sparkleTrailTarget.transform.position, sparkleTrailTarget.transform));
	}

	private IEnumerator awardMultiplier()
	{
		long creditsWon = wheelRoundVariantParent.outcome.getCurrentRound().entries[0].credits;
		long multiplier = 1;
		if (ReelGame.activeGame != null)
		{
			multiplier = wheelRoundVariantParent.outcome.getCurrentRound().entries[0].multiplier + 1;
		}
		creditsWon = creditsWon * multiplier;

		if (sparkleTrailMultiplierOrigin != null && sparkleTrailTarget != null)
		{
			yield return StartCoroutine(playParticleTrail());
		}

		yield return StartCoroutine(wheelRoundVariantParent.animateScore(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + creditsWon));

		BonusGamePresenter.instance.currentPayout += creditsWon;
	}
}
