using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to handle revealing unique multipliers during a picking round
 * For this implementation, the multiplier revealed is based on the initial multiplier value at the beginning of the round.
 */
public class PickingGameMultiplierUniqueFromRoundModule : PickingGameMultiplierModule
{
	[SerializeField] private List<ChallengeGameMultiplierHelper.MultiplierAnimationInfoList> individualMultiplierReveals;
	[SerializeField] private List<ChallengeGameMultiplierHelper.MultiplierAnimationInfoList> individualMultiplierGreyReveals;

	[SerializeField] private List<ChallengeGameMultiplierHelper.MultiplierAnimationInfoList> individualMultiplierAudio;
	[SerializeField] private List<ChallengeGameMultiplierHelper.MultiplierAnimationInfoList> individualMultiplierVOAudio;

	private int roundStartMultiplier = 0;

	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		roundStartMultiplier = roundVariantParent.gameParent.currentMultiplier;
		return base.executeOnRoundStart();
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry pickOutcome = (roundVariantParent as ModularPickingGameVariant).getCurrentPickOutcome();

		// play the audio for an individual multiplier value
		string multiplierReveal = ChallengeGameMultiplierHelper.getAudioKeyForMultiplierInList(roundStartMultiplier, individualMultiplierAudio);
		if(!string.IsNullOrEmpty(multiplierReveal))
		{
			Audio.playSoundMapOrSoundKey(multiplierReveal);
		}

		string multiplierVO = ChallengeGameMultiplierHelper.getAudioKeyForMultiplierInList(roundStartMultiplier, individualMultiplierVOAudio);
		if(!string.IsNullOrEmpty(multiplierVO))
		{
			Audio.playSoundMapOrSoundKeyWithDelay(multiplierVO, REVEAL_VO_DELAY);
		}

		// set the individual animation
		pickItem.REVEAL_ANIMATION = ChallengeGameMultiplierHelper.getAnimationNameForMultiplierInList(roundStartMultiplier, individualMultiplierReveals);

		yield return StartCoroutine(base.executeOnRevealPick(pickItem));

		// shoot sparkle trail towards multiplier label
		ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.Multiplier);
		if(particleTrailController != null)
		{
			yield return StartCoroutine(particleTrailController.animateParticleTrail(roundVariantParent.multiplierLabel.gameObject.transform.position, roundVariantParent.gameObject.transform));
		}

		// play any additional animations defined in the per-multiplier list
		AnimationListController.AnimationInformationList additionalAnims = ChallengeGameMultiplierHelper.getAnimationInfoForMultiplierInList(roundStartMultiplier, individualMultiplierReveals);
		if (additionalAnims != null)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(additionalAnims));
		}

		//set the multiplier value within the item and the reveal animation
		PickingGameMultiplierPickItem multiplierPick = pickItem.gameObject.GetComponent<PickingGameMultiplierPickItem>();
		multiplierPick.setMultiplierLabel(pickOutcome.multiplier);

		// update the actual round multiplier
		roundVariantParent.addToCurrentMultiplier(pickOutcome.multiplier);

		// play an animation flourish if we have one
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(multiplierAmbientAnimations));

		// animate credit values
		long creditsMultiplied = BonusGamePresenter.instance.currentPayout * (pickOutcome.multiplier);
		yield return StartCoroutine(rollupCredits(creditsMultiplied));
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularPickingGameVariant itemRoundParent = (roundVariantParent as ModularPickingGameVariant);
		ModularChallengeGameOutcomeEntry leftoverOutcome = itemRoundParent.getCurrentLeftoverOutcome();

		PickingGameMultiplierPickItem multiplierLeftOver = leftover.gameObject.GetComponent<PickingGameMultiplierPickItem>();

		if(leftoverOutcome != null)
		{
			if(multiplierLeftOver != null)
			{
				multiplierLeftOver.setMultiplierLabel(leftoverOutcome.multiplier);

				// set the individual multiplier animation
				multiplierLeftOver.REVEAL_ANIMATION_GRAY = ChallengeGameMultiplierHelper.getAnimationNameForMultiplierInList(roundStartMultiplier, individualMultiplierGreyReveals);
			} else
			{
				Debug.LogError("PickingGameMultiplierModule.executeOnRevealLeftover() - leftover item didn't have an attached PickingGameMultiplierPickItem!");
			}
		}

		// play the associated leftover reveal sound
		Audio.playSoundMapOrSoundKey(REVEAL_LEFTOVER_AUDIO);

		// reveal the leftover with the unique animation
		yield return StartCoroutine(leftover.revealLeftover(leftoverOutcome));
	}
}
