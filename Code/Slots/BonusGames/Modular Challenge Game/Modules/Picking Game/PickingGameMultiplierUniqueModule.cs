using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to handle revealing unique multipliers during a picking round
 * For this implementation, each level of multiplier has a unique animation / audio / VO to be played
 */
public class PickingGameMultiplierUniqueModule : PickingGameMultiplierModule 
{
	[SerializeField] private List<ChallengeGameMultiplierHelper.MultiplierAnimationInfoList> individualMultiplierReveals;
	[SerializeField] private List<ChallengeGameMultiplierHelper.MultiplierAnimationInfoList> individualMultiplierGreyReveals;

	[SerializeField] private List<ChallengeGameMultiplierHelper.MultiplierAnimationInfoList> individualMultiplierAudio;
	[SerializeField] private List<ChallengeGameMultiplierHelper.MultiplierAnimationInfoList> individualMultiplierVOAudio;


	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

		// play the audio for an individual multiplier value
		string multiplierReveal = ChallengeGameMultiplierHelper.getAudioKeyForMultiplierInList(currentPick.multiplier, individualMultiplierAudio);
		if (!string.IsNullOrEmpty(multiplierReveal))
		{
			Audio.playSoundMapOrSoundKey(multiplierReveal);
		}

		string multiplierVO = ChallengeGameMultiplierHelper.getAudioKeyForMultiplierInList(currentPick.multiplier, individualMultiplierVOAudio);
		if (!string.IsNullOrEmpty(multiplierVO))
		{
			Audio.playSoundMapOrSoundKeyWithDelay(multiplierVO, REVEAL_VO_DELAY);
		}
	
		// set the individual animation
		pickItem.REVEAL_ANIMATION = ChallengeGameMultiplierHelper.getAnimationNameForMultiplierInList(currentPick.multiplier, individualMultiplierReveals);

		yield return StartCoroutine(base.executeOnRevealPick(pickItem));

		// shoot sparkle trail towards multiplier label
		ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.Multiplier);
		if (particleTrailController != null)
		{
			yield return StartCoroutine(particleTrailController.animateParticleTrail(roundVariantParent.multiplierLabel.gameObject.transform.position, roundVariantParent.gameObject.transform));
		}

		//set the multiplier value within the item and the reveal animation
		PickingGameMultiplierPickItem multiplierPick = pickItem.gameObject.GetComponent<PickingGameMultiplierPickItem>();
		multiplierPick.setMultiplierLabel(currentPick.multiplier - 1);

		// update the actual round multiplier
		roundVariantParent.addToCurrentMultiplier(currentPick.multiplier - 1);

		// play an animation flourish if we have one
		AnimationListController.playListOfAnimationInformation(multiplierAmbientAnimations);

		// animate credit values
		long creditsMultiplied = BonusGamePresenter.instance.currentPayout * (currentPick.multiplier - 1);
		yield return StartCoroutine(rollupCredits(creditsMultiplied));
	}
		
	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();

		PickingGameMultiplierPickItem multiplierLeftOver = leftover.gameObject.GetComponent<PickingGameMultiplierPickItem>();

		if (leftoverOutcome != null)
		{
			if (multiplierLeftOver != null)
			{
				multiplierLeftOver.setMultiplierLabel(leftoverOutcome.multiplier);

				// set the individual multiplier animation
				multiplierLeftOver.REVEAL_ANIMATION_GRAY = ChallengeGameMultiplierHelper.getAnimationNameForMultiplierInList(leftoverOutcome.multiplier, individualMultiplierGreyReveals);
			}
			else
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
