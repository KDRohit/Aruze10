using System.Collections;
using UnityEngine;
using System.Collections.Generic;

/*
 * Module for handling reveal and left over reveal of credit picks that use a groupId to differentiate animations
 *
 * Author : Shaun Peoples <speoples@zynga.com>
 * First Use : Orig001
 */
public class PickingGameCreditsWithGroupIdModule : PickingGameRevealModule
{
	[Header("Reveal Settings")]
	[SerializeField] protected AudioListController.AudioInformationList revealAudio;
	[SerializeField] protected AudioListController.AudioInformationList revealVO;
	[SerializeField] protected AudioListController.AudioInformationList revealLeftoverAudio;
	[SerializeField] protected PickItemAnimationProperties[] pickItemAnimationProperties;

	[Header("Particle Trail Settings")]
	[SerializeField] protected AnimatedParticleEffect revealAnimatedParticleEffect;

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// detect pick type & whether to handle with this module
		if (
			(pickData != null) &&
			(pickData.credits > 0) &&
			!pickData.canAdvance &&
			(pickData.additonalPicks == 0) &&
			(pickData.extraRound == 0) &&
			(
				(!pickData.isGameOver) ||
				(
					pickData.isGameOver &&
					(
						roundVariantParent.roundIndex == roundVariantParent.gameParent.pickingRounds.Count-1 ||
						pickingVariantParent.gameParent.getDisplayedPicksRemaining() >= 0
					)
				)
			)
		)
		{
			return true;
		}
		
		return false;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		List<TICoroutine> coroutineList = new List<TICoroutine>();
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
		handleCreditItemPicked(currentPick, pickItem);

		yield return StartCoroutine(base.executeOnItemClick(pickItem));

		if (revealAnimatedParticleEffect != null)
		{
			coroutineList.Add(StartCoroutine(revealAnimatedParticleEffect.animateParticleEffect(pickItem.transform)));
		}

		// rollup with extra animations included
		if (pickingVariantParent.useMultipliedCreditValues)
		{
			coroutineList.Add(StartCoroutine(base.rollupCredits(currentPick.credits)));
		}
		else
		{
			coroutineList.Add(StartCoroutine(base.rollupCredits(currentPick.baseCredits)));
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
	}

	private void handleCreditItemPicked(ModularChallengeGameOutcomeEntry currentPick, PickingGameBasePickItem pickItem)
	{
		if (revealAudio != null)
		{
			// play the associated reveal sound
			AudioListController.playListOfAudioInformation(revealAudio);
		}

		if (revealVO != null)
		{
			// play the associated audio voiceover
			AudioListController.playListOfAudioInformation(revealVO);
		}

		PickItemAnimationProperties animationProperties = getPickItemAnimationProperties(currentPick.groupId);
		if (animationProperties != null)
		{
			pickItem.setRevealAnim(animationProperties.revealAnimationName, animationProperties.animationDurationOverride);
		}

		PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(pickItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);

		// adjust with bonus multiplier if necessary
		if (pickingVariantParent.useMultipliedCreditValues)
		{
			creditsRevealItem.setCreditLabels(currentPick.credits);
		}
		else
		{
			creditsRevealItem.setCreditLabels(currentPick.baseCredits);
		}
	}

	public override IEnumerator executeOnRevealLeftover(PickingGameBasePickItem leftover)
	{
		// set the credit value from the leftovers
		ModularChallengeGameOutcomeEntry leftoverOutcome = pickingVariantParent.getCurrentLeftoverOutcome();

		PickingGameCreditPickItem creditsLeftOver = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(leftover.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Default);

		if (leftoverOutcome != null)
		{
			if (creditsLeftOver != null)
			{
				// adjust with bonus multiplier if necessary
				if (pickingVariantParent.useMultipliedCreditValues)
				{
					creditsLeftOver.setCreditLabels(leftoverOutcome.credits);
				}
				else
				{
					creditsLeftOver.setCreditLabels(leftoverOutcome.baseCredits);
				}

				PickItemAnimationProperties animationProperties = getPickItemAnimationProperties(leftoverOutcome.groupId);
				if (animationProperties != null)
				{
					creditsLeftOver.REVEAL_ANIMATION_GRAY = animationProperties.revealLeftoverAnimationName;
				}
			}
			else
			{
				Debug.LogError("PickingGameCreditsWithGroupIdModule.executeOnRevealLeftover() - leftover item didn't have an attached PickingGameCreditPickItem!");
			}
		}
		
		if (revealLeftoverAudio != null)
		{
			// play the associated leftover reveal sound
			AudioListController.playListOfAudioInformation(revealLeftoverAudio);
		}

		yield return StartCoroutine(base.executeOnRevealLeftover(leftover));
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
