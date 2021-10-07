using UnityEngine;
using System.Collections;

/* Module to handle increasing available picks when revealing a specific symbol */
public class PickingGameIncreasePicksModule : PickingGameRevealModule
{
	[SerializeField] protected string REVEAL_ANIMATION_NAME = "reveal_credits_plus_1";
	[SerializeField] protected string REVEAL_GRAY_ANIMATION_NAME = "reveal_credits_plus_1_grey";
	[SerializeField] protected string REVEAL_AUDIO = "pickem_increase_pick";
	[SerializeField] protected string REVEAL_VO_AUDIO = "pickem_increase_vo_pick";
	[SerializeField] protected float  REVEAL_AUDIO_DELAY = 0.0f; // delay before playing the reveal clip
	[SerializeField] protected float 	REVEAL_VO_DELAY = 0.2f; // delay before playing the VO clip
	[SerializeField] protected string REVEAL_LEFTOVER_AUDIO = "reveal_not_chosen";
	[SerializeField] private bool shouldUpdatePicksRemainingImmediately = false; //Sometimes we want to update our picks remaining label immediately instead of waiting for the next round to start
	[SerializeField] private float DELAY_AFTER_INCREASE_PICKS_PARTICLE_TRAIL_DONE = 0.0f; // use this if you need to introduce a small delay after the particle trail wraps up so that sounds can finish before rollup

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// detect pick type & whether to handle with this module
		if ((pickData != null) && (!pickData.canAdvance) && (pickData.additonalPicks > 0))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
		
	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

		PickingGameIncreasePicksPickItem increasePickItem = pickItem.GetComponent<PickingGameIncreasePicksPickItem>();

		// play the associated reveal sound
		Audio.playWithDelay(Audio.soundMap(REVEAL_AUDIO), REVEAL_AUDIO_DELAY);

		if (!string.IsNullOrEmpty(REVEAL_VO_AUDIO))
		{
			// play the associated audio voiceover
			Audio.playWithDelay(Audio.soundMap(REVEAL_VO_AUDIO), REVEAL_VO_DELAY);
		}

		if (increasePickItem != null)
		{
			// set the quantity of picks on the item
			if (currentPick.additonalPicks > 0)
			{
				increasePickItem.setPicksAwarded(currentPick.additonalPicks);
			}
			else
			{
				increasePickItem.setPicksAwarded(currentPick.extraRound);
			}
		}

		// set the increase value within the item and the reveal animation
		if (currentPick.wheelExtraData != "" && pickItem.pickAnimator.HasState(0, Animator.StringToHash(REVEAL_ANIMATION_NAME + currentPick.wheelExtraData)))
		{
			pickItem.REVEAL_ANIMATION = REVEAL_ANIMATION_NAME + currentPick.wheelExtraData;
		}
		else
		{
			pickItem.REVEAL_ANIMATION = REVEAL_ANIMATION_NAME;
		}
		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.IncreasePicks);
		if (particleTrailController != null)
		{
			yield return StartCoroutine(particleTrailController.animateParticleTrail(pickingVariantParent.picksRemainingLabel.gameObject.transform.position, roundVariantParent.gameObject.transform));
		}

		// award the picks revealed
		if (currentPick.additonalPicks > 0)
		{
			yield return StartCoroutine(pickingVariantParent.gameParent.increasePicks(currentPick.additonalPicks));
		}

		if (currentPick.extraRound > 0)
		{
			yield return StartCoroutine(pickingVariantParent.gameParent.increasePicks(currentPick.extraRound));
		}

		if (shouldUpdatePicksRemainingImmediately)
		{
			pickingVariantParent.updatePicksRemainingLabel();
		}

		if (DELAY_AFTER_INCREASE_PICKS_PARTICLE_TRAIL_DONE > 0.0f)
		{
			yield return new TIWaitForSeconds(DELAY_AFTER_INCREASE_PICKS_PARTICLE_TRAIL_DONE);
		}
	}


	public override IEnumerator executeOnRevealPick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(base.executeOnRevealPick(pickItem));
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the pick quantity value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
		if (leftoverOutcome != null)
		{
			PickingGameIncreasePicksPickItem leftoverItem = leftover.GetComponent<PickingGameIncreasePicksPickItem>();

			// set picks awarded value
			if (leftoverOutcome.additonalPicks > 0 && leftoverItem != null)
			{
				leftoverItem.setPicksAwarded(leftoverOutcome.additonalPicks);
			}
		
			if (leftoverOutcome.wheelExtraData != "" && leftoverItem != null && leftoverItem.pickAnimator.HasState(0, Animator.StringToHash(REVEAL_GRAY_ANIMATION_NAME + leftoverOutcome.wheelExtraData)))
			{
				leftover.REVEAL_ANIMATION_GRAY = REVEAL_GRAY_ANIMATION_NAME + leftoverOutcome.wheelExtraData;
			}
			else
			{
				leftover.REVEAL_ANIMATION_GRAY = REVEAL_GRAY_ANIMATION_NAME;
			}
		}

		// play the associated leftover reveal sound
		Audio.play(Audio.soundMap(REVEAL_LEFTOVER_AUDIO));

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
	}

}
