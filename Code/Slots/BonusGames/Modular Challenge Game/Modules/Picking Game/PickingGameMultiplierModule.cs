using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

/**
 * Module to handle revealing multipliers during a picking round
 */
public class PickingGameMultiplierModule : PickingGameRevealModule
{
	[Header("Reveal Settings")]
	[SerializeField] protected string REVEAL_ANIMATION_NAME = "revealMultiplier";
	[SerializeField] protected string REVEAL_GRAY_ANIMATION_NAME = "revealMultiplierGray";
	[SerializeField] protected string REVEAL_AUDIO = "pickem_multiplier_pick";
	[SerializeField] protected string REVEAL_VO_AUDIO = "pickem_multiplier_vo_pick";
	[SerializeField] protected float REVEAL_VO_DELAY = 0.2f; // delay before playing the VO clip
	[SerializeField] protected float REVEAL_ANIMATION_DURATION_OVERRIDE = -1.0f;
	[SerializeField] protected string REVEAL_LEFTOVER_AUDIO = "reveal_not_chosen";

	// We have this so we can define specific animations for each multiplier. So a 2x multiplier and use
	// different animations from a 3x multiplier. What this means is we have two different ways of defining
	// animations which is not ideal. This should be refactored. What should happen is this should moved 
	// into its own class that inherits from a base class. The base class should contain all the common
	// functionality from this class and this class and the new class should have their own ways of doing
	// the multiplier animations.
	// Used in marilyn02.
	[SerializeField] protected PickingGameMultiplierAnimationProperties[] multiplierAnimationProperties;

	[Header("Particle Trail Settings")]
	[SerializeField] protected AnimatedParticleEffect animatedParticleEffect;
	[FormerlySerializedAs("PlayParticlesFromMultiplierToTarget")]
	[SerializeField] protected bool playParticlesFromMultiplierToTarget = false;
	[FormerlySerializedAs("EndTargetPosition")]
	[SerializeField] protected Transform endTargetPosition;

	[Header("Ambient Animations")]
	[SerializeField] protected AnimationListController.AnimationInformationList multiplierAmbientAnimations;

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// Make sure this pick has a multiplier
		if ((pickData != null) && (pickData.multiplier > 0))
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
		
		if (currentPick == null || pickItem == null)
		{
			yield break;
		}

		// Play the associated reveal sound
		Audio.playSoundMapOrSoundKey(REVEAL_AUDIO);

		if (!string.IsNullOrEmpty(REVEAL_VO_AUDIO))
		{
			// play the associated audio voiceover
			Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_VO_AUDIO, REVEAL_VO_DELAY);
		}

		// Set the multiplier value within the item and the reveal animation
		PickingGameMultiplierPickItem multiplierPick = pickItem.gameObject.GetComponent<PickingGameMultiplierPickItem>();

		// Some multiplier picks are using art which uses static art for the multiplier number instead of a modifiable number, so only set labels if it has the item attached
		if (multiplierPick != null)
		{
			multiplierPick.setMultiplierLabel(currentPick.multiplier);
		}

		PickingGameMultiplierAnimationProperties animationProperties = getMultiplierAnimationProperties(currentPick.multiplier);
		if (animationProperties != null)
		{
			pickItem.setRevealAnim(animationProperties.REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);
			StartCoroutine(AudioListController.playListOfAudioInformation(animationProperties.REVEAL_SOUNDS));
		}
		else
		{
			pickItem.setRevealAnim(REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);
		}

		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		if (animatedParticleEffect != null)
		{
			yield return StartCoroutine(animatedParticleEffect.animateParticleEffect(pickItem.transform));
		}
		else
		{
			// shoot sparkle trail towards multiplier label
			ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.Multiplier);

			if (particleTrailController != null && roundVariantParent != null)
			{
				//checks whether or not to use custom targets for the start and end positions of the particle trail
				if (playParticlesFromMultiplierToTarget)
				{
					if (endTargetPosition != null)
					{
						yield return StartCoroutine(particleTrailController.animateParticleTrail(endTargetPosition.transform.position, roundVariantParent.gameObject.transform));
					}
				}
				else
				{
					yield return StartCoroutine(particleTrailController.animateParticleTrail(roundVariantParent.multiplierLabel.gameObject.transform.position, roundVariantParent.gameObject.transform));
				}
			}
		}

		// update the actual round multiplier
		roundVariantParent.addToCurrentMultiplier(currentPick.multiplier);

		// play an animation flourish if we have one
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(multiplierAmbientAnimations));

		// animate credit values
		
		int multiplier = currentPick.multiplier > 1 ? currentPick.multiplier : roundVariantParent.gameParent.currentMultiplier;

		yield return StartCoroutine(rollupCredits(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout * multiplier));
	}

	private PickingGameMultiplierAnimationProperties getMultiplierAnimationProperties(int multiplier)
	{
		foreach (PickingGameMultiplierAnimationProperties animationProperties in multiplierAnimationProperties)
		{
			if (animationProperties.multiplierValue == multiplier)
			{
				return animationProperties;
			}
		}

		return null;
	}

	public override IEnumerator executeOnRevealPick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(base.executeOnRevealPick(pickItem));
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
			}

			PickingGameMultiplierAnimationProperties animationProperties = getMultiplierAnimationProperties(leftoverOutcome.multiplier);
			if (animationProperties != null)
			{
				leftover.REVEAL_ANIMATION_GRAY = animationProperties.REVEAL_GRAY_ANIMATION_NAME;
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

	[System.Serializable]
	public class PickingGameMultiplierAnimationProperties
	{
		public int multiplierValue;
		public string REVEAL_ANIMATION_NAME = "revealMultiplier";
		public string REVEAL_GRAY_ANIMATION_NAME = "revealMultiplierGray";
		public AudioListController.AudioInformationList REVEAL_SOUNDS;
	}
}
