using System.Collections;
using UnityEngine;

/**
 * Module created for elvira07 where stick and win in free spins would trigger this pick game to handle
 * 1: revealing multipliers/credits (supports displaying using multiplier labels or credits labels)
 * 2: revealing how many extra stick and win spins you will get
 * 
 * Original Author: Xueer Zhu <xzhu@zynga.com>
 * Date: 06/11/2021
 */
public class PickingGameMultiplierAndIncreaseSNWCountModule : PickingGameRevealModule
{
	[Header("Reveal Multiplier Settings"), Space(5)] [SerializeField]
	protected PickingGameMultiplierAnimationProperties[] multiplierAnimationProperties;

	[SerializeField] protected string REVEAL_ANIMATION_NAME = "revealMultiplier";
	[SerializeField] protected string REVEAL_GRAY_ANIMATION_NAME = "revealMultiplierGray";
	[SerializeField] protected string REVEAL_AUDIO = "pickem_multiplier_pick";
	[SerializeField] protected string REVEAL_VO_AUDIO = "pickem_multiplier_vo_pick";
	[SerializeField] protected float REVEAL_VO_DELAY = 0.2f; // delay before playing the VO clip
	[SerializeField] protected float REVEAL_ANIMATION_DURATION_OVERRIDE = -1.0f;
	[SerializeField] protected string REVEAL_LEFTOVER_AUDIO = "reveal_not_chosen";
	[SerializeField] protected AnimationListController.AnimationInformationList multiplierAmbientAnimations;

	[Header("Reveal SNW Extra Spins Settings"), Space(5)]
	[Tooltip("Use this if every snw spin count value that can be revealed has a different animation that needs to be played.")]
	[SerializeField] protected AnimationDataBySpinCountWon[] revealSpinCountAnimDataByValueArray;

	[SerializeField] protected string REVEAL_SPINS_ANIMATION_NAME = "revealSNWSpinCount";
	[SerializeField] protected string REVEAL_SPINS_GRAY_ANIMATION_NAME = "revealSNWSpinCountGray";
	[SerializeField] protected float REVEAL_SPINS_ANIMATION_DURATION_OVERRIDE = -1.0f;

	[Tooltip("Particle effect(s) played when a spin value amount is revealed.")] [SerializeField]
	protected AnimatedParticleEffect spinCountRevealedParticleEffect = null;

	[SerializeField] protected AnimatedParticleEffect animatedParticleEffect;
	
	[Header("Data Settings")] private const string INCREASE_SNW_PICK_GROUP_KEY = "SNW";

	private int snwCount = 0;
	private JSON freeSpinMeterJson;

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		
		// We'll handle both the added spins and the reveal multiplier pick (which sits in side of the groupKey info) 
		if ((pickData != null && ((pickData.spins > 0) || (!string.IsNullOrEmpty(pickData.groupId)))))
		{
			return true;
		}
		
		return false;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPickOutcome = pickingVariantParent.getCurrentPickOutcome();

		if (currentPickOutcome == null || pickItem == null)
		{
			yield break;
		}
		
		if (!string.IsNullOrEmpty(currentPickOutcome.groupId))
		{
			string pickItemGroupKey = currentPickOutcome.groupId;
			if (pickItemGroupKey == INCREASE_SNW_PICK_GROUP_KEY)
			{
				yield return StartCoroutine(revealSNWSpinCountPick(pickItem, currentPickOutcome));
			}
			else
			{
				int multiplierValue = currentPickOutcome.multiplier;
				long credits = currentPickOutcome.credits * multiplierValue;
				yield return StartCoroutine(revealMultiplierPick(pickItem, multiplierValue, credits));
			}
		}
	}

	protected virtual IEnumerator revealSNWSpinCountPick(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry currentPick)
	{
		AnimationDataBySpinCountWon animData = getAnimationDataForSpinCount(currentPick.spins);

		// Play the associated reveal sounds
		if (animData != null && animData.revealSpinsAudio.Count > 0)
		{
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(animData.revealSpinsAudio));
		}

		// Set the spin count value within the item and the reveal animation
		PickingGameSpinCountPickItem spinCountPick = pickItem.gameObject.GetComponent<PickingGameSpinCountPickItem>();

		if (spinCountPick != null)
		{
			spinCountPick.setSpinCountLabel(currentPick.spins);
		}

		if (animData != null)
		{
			pickItem.setRevealAnim(animData.REVEAL_SPINS_ANIMATION_NAME,
				animData.REVEAL_SPINS_ANIMATION_DURATION_OVERRIDE);
		}
		else
		{
			pickItem.setRevealAnim(REVEAL_SPINS_ANIMATION_NAME, REVEAL_SPINS_ANIMATION_DURATION_OVERRIDE);
		}

		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		// Play a reveal particle effect if one is setup
		if (spinCountRevealedParticleEffect != null)
		{
			yield return StartCoroutine(spinCountRevealedParticleEffect.animateParticleEffect(pickItem.gameObject.transform));
		}
	}

	protected virtual IEnumerator revealMultiplierPick(PickingGameBasePickItem pickItem, int multiplierValue, long credits = 0)
	{
		// Play the associated reveal sound
		Audio.playSoundMapOrSoundKey(REVEAL_AUDIO);

		if (!string.IsNullOrEmpty(REVEAL_VO_AUDIO))
		{
			// play the associated audio voiceover
			Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_VO_AUDIO, REVEAL_VO_DELAY);
		}
		
		// Set the credits/multiplier value within the item and the reveal animation
		PickingGameCreditPickItem creditPick = pickItem.gameObject.GetComponent<PickingGameCreditPickItem>();
		if (creditPick != null)
		{
			creditPick.setCreditLabels(credits); 
		}
		
		PickingGameMultiplierPickItem multiplierPick = pickItem.gameObject.GetComponent<PickingGameMultiplierPickItem>();
		if (multiplierPick != null)
		{
			multiplierPick.setMultiplierLabel(multiplierValue);
		}

		PickingGameMultiplierAnimationProperties animationProperties = getMultiplierAnimationProperties(multiplierValue);
		if (animationProperties != null)
		{
			pickItem.setRevealAnim(animationProperties.REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(animationProperties.REVEAL_SOUNDS));
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
				yield return StartCoroutine(particleTrailController.animateParticleTrail(
					roundVariantParent.multiplierLabel.gameObject.transform.position,
					roundVariantParent.gameObject.transform));
			}
		}

		// update the actual round multiplier
		roundVariantParent.addToCurrentMultiplier(multiplierValue);

		// play an animation flourish if we have one
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(multiplierAmbientAnimations));

		// animate credit values
		yield return StartCoroutine(rollupCredits(credits));
	}
	
	public override IEnumerator executeOnRevealPick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(base.executeOnRevealPick(pickItem));
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftoverItem)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
		
		if (leftoverOutcome == null || leftoverItem == null)
		{
			yield break;
		}
		
		if (!string.IsNullOrEmpty(leftoverOutcome.groupId))
		{
			string leftoverOutcomeGroupKey = leftoverOutcome.groupId;
			if (leftoverOutcomeGroupKey == INCREASE_SNW_PICK_GROUP_KEY)
			{
				yield return StartCoroutine(revealSNWSpinCountLeftover(leftoverItem, leftoverOutcome));
			}
			else
			{
				int multiplierValue = leftoverOutcome.multiplier;
				long credits = leftoverOutcome.credits * multiplierValue;
				yield return StartCoroutine(revealMultiplierLeftover(leftoverItem, multiplierValue, credits));
			}
		}
	}
	
	protected virtual IEnumerator revealSNWSpinCountLeftover(PickingGameBasePickItem leftoverItem, ModularChallengeGameOutcomeEntry leftoverOutcome)
	{
		// Set the spin count value within the item and the leftover animation
		PickingGameSpinCountPickItem spinCountPick = leftoverItem.gameObject.GetComponent<PickingGameSpinCountPickItem>();

		if (spinCountPick != null)
		{
			spinCountPick.setSpinCountLabel(leftoverOutcome.spins);
		}

		AnimationDataBySpinCountWon animData = getAnimationDataForSpinCount(leftoverOutcome.spins);
		
		if (animData != null)
		{
			leftoverItem.REVEAL_ANIMATION_GRAY = animData.REVEAL_SPINS_GRAY_ANIMATION_NAME;
		}
		else
		{
			leftoverItem.REVEAL_ANIMATION_GRAY = REVEAL_SPINS_GRAY_ANIMATION_NAME;
		}
		
		// play the associated leftover reveal sound
		Audio.play(Audio.soundMap(REVEAL_LEFTOVER_AUDIO));
		yield return StartCoroutine(base.executeOnRevealLeftover(leftoverItem));
	}
	
	protected virtual IEnumerator revealMultiplierLeftover(PickingGameBasePickItem leftoverItem, int multiplierValue, long credits = 0)
	{
		// Set the credit/multiplier value within the item and the reveal animation
		PickingGameCreditPickItem creditPick = leftoverItem.gameObject.GetComponent<PickingGameCreditPickItem>();
		if (creditPick != null)
		{
			creditPick.setCreditLabels(credits); 
		}
		
		PickingGameMultiplierPickItem multiplierLeftover = leftoverItem.gameObject.GetComponent<PickingGameMultiplierPickItem>();

		if (multiplierLeftover != null)
		{
			multiplierLeftover.setMultiplierLabel(multiplierValue);
		}

		PickingGameMultiplierAnimationProperties animationProperties = getMultiplierAnimationProperties(multiplierValue);
		if (animationProperties != null)
		{
			leftoverItem.REVEAL_ANIMATION_GRAY = animationProperties.REVEAL_GRAY_ANIMATION_NAME;
			yield return StartCoroutine(AudioListController.playListOfAudioInformation(animationProperties.REVEAL_SOUNDS));
		}
		else
		{
			leftoverItem.REVEAL_ANIMATION_GRAY = REVEAL_GRAY_ANIMATION_NAME;
		}
		
		// play the associated leftover reveal sound
		Audio.play(Audio.soundMap(REVEAL_LEFTOVER_AUDIO));
		yield return StartCoroutine(base.executeOnRevealLeftover(leftoverItem));
	}
	
	protected AnimationDataBySpinCountWon getAnimationDataForSpinCount(int spinCount)
	{
		foreach (AnimationDataBySpinCountWon animData in revealSpinCountAnimDataByValueArray)
		{
			if (animData.spinCount == spinCount)
			{
				return animData;
			}
		}
		return null;
	}

	protected PickingGameMultiplierAnimationProperties getMultiplierAnimationProperties(int multiplier)
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

	[System.Serializable]
	protected class AnimationDataBySpinCountWon
	{
		[Tooltip("The amount of spins which this set of animation data should be used for")] [SerializeField]
		public int spinCount = -1;

		[Tooltip("The animation that will be played")] [SerializeField]
		public string REVEAL_SPINS_ANIMATION_NAME = "revealSNWSpinCount";
		public string REVEAL_SPINS_GRAY_ANIMATION_NAME = "revealSNWSpinCountGray";

		[Tooltip("Sounds to accompany the animation")] [SerializeField]
		public AudioListController.AudioInformationList revealSpinsAudio;

		[Tooltip("Override for how long the animation is.")] [SerializeField]
		public float REVEAL_SPINS_ANIMATION_DURATION_OVERRIDE = -1.0f;
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