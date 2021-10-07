using UnityEngine;
using System.Collections;

//
// Module to handle revealing credits during a picking round
// and animating a particle trail on the picked item to the win box.
//
// PickingGameCreditsWithTrailModule and PickingGameCreditsModule should inherit from a common base class
// that have shared methods and variables. It's ok like this - just not as perfect as it should be.
//
// Author : nick saito <nsaito@zynga.com>
// Date : March 2018
public class PickingGameCreditsWithTrailModule : PickingGameCreditsModule
{
	[Header("Particle Trail Settings")]
	[SerializeField] protected AnimatedParticleEffect animatedParticleEffect;
	[SerializeField] protected Transform endTargetPosition;

	// Detect pick type & whether to handle with this module
	// Note: We do not use base.shouldHandleOutcomeEntry because it has a groupID check.
	// In some games the groupID will be set in the mutation even with credits and multiplier
	// values. If you need a groupID to be empty, then use PickingGameCreditsModule.
	// Used in marilyn02
	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		if (
			pickData != null &&
			pickData.credits > 0 &&
			!pickData.canAdvance &&
			pickData.additonalPicks == 0 &&
			pickData.extraRound == 0 &&
			(
				!pickData.isGameOver ||
				(
					pickData.isGameOver &&
					(
						roundVariantParent.roundIndex == roundVariantParent.gameParent.pickingRounds.Count - 1 ||
						pickingVariantParent.gameParent.getDisplayedPicksRemaining() >= 0
					)
				)
			)
		)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	// Override executeOnItemClick ourself so we can animate the particle trail at the right time.
	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
		handleCreditItemPicked(currentPick, pickItem);

		// Since we can't call base.base.executeOnItemClick, we just call the executeBasicOnRevealPick directly
		yield return StartCoroutine(executeBasicOnRevealPick(pickItem));
		yield return StartCoroutine(playParticleTrail(pickItem));

		// rollup with extra animations included
		if (!USE_BASE_CREDIT_AMOUNT)
		{
			yield return StartCoroutine(base.rollupCredits(currentPick.credits));
		}
		else
		{
			yield return StartCoroutine(base.rollupCredits(currentPick.baseCredits));
		}
	}

	protected IEnumerator playParticleTrail(PickingGameBasePickItem pickItem)
	{
		if (animatedParticleEffect != null)
		{
			yield return StartCoroutine(animatedParticleEffect.animateParticleEffect(pickItem.transform));
		}
		else
		{
			ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.Multiplier);

			if (particleTrailController != null)
			{
				yield return StartCoroutine(particleTrailController.animateParticleTrail(endTargetPosition.position, roundVariantParent.gameObject.transform));
			}
		}
	}
}
