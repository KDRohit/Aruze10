using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to handle revealing simple picks and locking reels if the group code matches
 */
public class PickingGameRevealPickedModule : PickingGameRevealModule
{
	private enum AmbientAnimationPlayDuring
	{
		OnReveal,
		OnPick
	}
	
	[Header("Reveal Settings")] 
	[SerializeField] AudioListController.AudioInformationList revealPickedSounds = new AudioListController.AudioInformationList("pickem_reveal_win");
	[SerializeField] AudioListController.AudioInformationList revealLeftoverSounds = new AudioListController.AudioInformationList("reveal_not_chosen");
	[SerializeField] AudioListController.AudioInformationList revealPickedVO = new AudioListController.AudioInformationList("pickem_reveal_win_vo", 0.2f);
	
	[Header("Ambient Animations to play on Pick Or Reveal")]
	[SerializeField] protected PickItemAnimationProperties[] pickItemAnimationProperties;
	[Tooltip("When to play the group triggered ambient animations")] 
	[SerializeField] private AmbientAnimationPlayDuring groupTriggeredAmbientAnimationPlayDuring;
	[SerializeField] protected AnimationListController.AnimationInformationList groupTriggeredAmbientAnimations;
	[SerializeField] protected PickItemAnimationProperties[] groupTriggeredAmbientAnimationProperties;

	[Header("Particle Trail Settings")]
	[SerializeField] protected AnimatedParticleEffect animatedParticleEffect;

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		return getPickItemAnimationProperties(pickData.groupId) != null;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

		if (currentPick == null || pickItem == null)
		{
			yield break;
		}

		PickItemAnimationProperties animationProperties = getPickItemAnimationProperties(currentPick.groupId);

		if (animationProperties == null)
		{
			Debug.LogWarning("animationProperties are null for picked item: " + pickItem.name);
			yield break; 
		}
		
		pickItem.setRevealAnim(animationProperties.revealAnimationName, animationProperties.animationDurationOverride);

		// Play the associated reveal sound
		List<TICoroutine> coroutineList = new List<TICoroutine>();
		coroutineList.Add(StartCoroutine(AudioListController.playListOfAudioInformation(revealPickedSounds)));
		coroutineList.Add(StartCoroutine(AudioListController.playListOfAudioInformation(revealPickedVO)));
		coroutineList.Add(StartCoroutine(base.executeOnItemClick(pickItem)));

		if (animatedParticleEffect != null)
		{
			coroutineList.Add(StartCoroutine(animatedParticleEffect.animateParticleEffect(pickItem.transform)));
		}

		if (groupTriggeredAmbientAnimationPlayDuring == AmbientAnimationPlayDuring.OnPick)
		{
			coroutineList.Add(StartCoroutine(playAmbientAnimations()));
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
	}
	
	public override IEnumerator executeOnRevealPick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(base.executeOnRevealPick(pickItem));

		if (groupTriggeredAmbientAnimationPlayDuring == AmbientAnimationPlayDuring.OnReveal)
		{
			yield return StartCoroutine(playAmbientAnimations());
		}
	}

	private IEnumerator playAmbientAnimations()
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
		PickItemAnimationProperties animationProperties = getGroupTriggeredAmbientAnimationProperties(currentPick.groupId);
	
		if (animationProperties == null)
		{
			yield break;
		}
		
		foreach (AnimationListController.AnimationInformation animationInfo in groupTriggeredAmbientAnimations.animInfoList)
		{
			animationInfo.ANIMATION_NAME = animationProperties.revealAnimationName;
		}
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(groupTriggeredAmbientAnimations));
	}
	
	private PickItemAnimationProperties getPickItemAnimationProperties(string groupId)
	{
		foreach (PickItemAnimationProperties animationProperties in pickItemAnimationProperties)
		{
			if (animationProperties.groupId == groupId)
			{
				return animationProperties;
			}
		}

		return null;
	}
	
	private PickItemAnimationProperties getGroupTriggeredAmbientAnimationProperties(string groupId)
	{
		foreach (PickItemAnimationProperties animationProperties in groupTriggeredAmbientAnimationProperties)
		{
			if (animationProperties.groupId == groupId)
			{
				return animationProperties;
			}
		}

		return null;
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// Set the reveal animation from the groupId
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();
		PickItemAnimationProperties animationProperties = getPickItemAnimationProperties(leftoverOutcome.groupId);
		leftover.REVEAL_ANIMATION_GRAY = animationProperties.revealLeftoverAnimationName;

		// play reveal effects
		List<TICoroutine> coroutineList = new List<TICoroutine>();
		coroutineList.Add(StartCoroutine(AudioListController.playListOfAudioInformation(revealLeftoverSounds)));
		coroutineList.Add(StartCoroutine(base.executeOnRevealLeftover(leftover)));
		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
	}

	[System.Serializable]
	public class PickItemAnimationProperties
	{
		[Tooltip("The groupId is from server data. Each groupId maps to a pick like a multiplier,credits, or special pick")]
		public string groupId; //note that this maps to group_code in pick data

		[Tooltip("The animation to play when player picks")]
		public string revealAnimationName;

		[Tooltip("Duration of the reveal animation")]
		public float animationDurationOverride;

		[Tooltip("The gray animation for the leftover animation")]
		public string revealLeftoverAnimationName;
	}
}
