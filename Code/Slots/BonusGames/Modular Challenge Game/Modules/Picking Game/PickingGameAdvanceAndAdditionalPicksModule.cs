using UnityEngine;
using System.Collections;

/**
 * Module to handle revealing Advacne outcomes during a picking round
 * Also includes awarding extra picks
 */
public class PickingGameAdvanceAndAdditionalPicksModule : PickingGameAdvanceModule
{
	[SerializeField] private bool shouldUpdatePicksRemainingImmediately = false; //Sometimes we want to update our picks remaining label immediately instead of waiting for the next round to start
	[SerializeField] private float DELAY_AFTER_INCREASE_PICKS_PARTICLE_TRAIL_DONE = 0.0f; // use this if you need to introduce a small delay after the particle trail wraps up so that sounds can finish before rollup

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// detect pick type & whether to handle with this module
		return pickData != null && pickData.canAdvance && pickData.additonalPicks > 0;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(base.executeOnItemClick(pickItem));
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

		// reward additional picks if included
		if (currentPick.additonalPicks > 0)
		{
			// play a particle trail if one exists for adding picks
			ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.IncreasePicks);
			if (particleTrailController != null)
			{
				yield return StartCoroutine(particleTrailController.animateParticleTrail(pickingVariantParent.picksRemainingLabel.gameObject.transform.position, roundVariantParent.gameObject.transform));
			}

			yield return StartCoroutine(pickingVariantParent.gameParent.increasePicks(currentPick.additonalPicks));

			if (shouldUpdatePicksRemainingImmediately)
			{
				pickingVariantParent.updatePicksRemainingLabel();
			}

			if (DELAY_AFTER_INCREASE_PICKS_PARTICLE_TRAIL_DONE > 0.0f)
			{
				yield return new TIWaitForSeconds(DELAY_AFTER_INCREASE_PICKS_PARTICLE_TRAIL_DONE);
			}
		}
	}
}
