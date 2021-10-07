using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/** 
 * Reveal module to play a set of animationInfos directly prior to revealing an item.
 * Used for performing a sort of "pseudo-reveal" prior to the main item reveal.
 * Includes sparkle trail element that flies to the selected "master" pick.
 * Design requirements from "skee01" picking game.
 */
public class PickingGamePreRevealAnimationWithTrailModule : PickingGameModule 
{
	[SerializeField] private string ORIGINAL_PICK_ANTICIPATION;

	[System.Serializable]
	private class LinkedRevealMapping
	{
		public PickingGameCreditPickItem.CreditsPickItemType creditRevealType;
		public bool playRandomAnimation = false;	// choose the animation from the list randomly?
		public AnimationListController.AnimationInformationList[] animationList;
	}
	[SerializeField] private LinkedRevealMapping[] preRevealAnimationGroups;
	[SerializeField] private LinkedRevealMapping[] revealAnimationGroups;

	public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
	{
		return true;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

		// play anticipation animation for the original pick, while we do the random pre-reveal ones
		pickItem.pickAnimator.Play(ORIGINAL_PICK_ANTICIPATION);

		// determine reveal type from pick properties
		PickingGameCreditPickItem.CreditsPickItemType revealType = getRevealTypeForOutcome(currentPick);
		LinkedRevealMapping targetLinkedPreRevealMapping = getLinkedRevealMappingForRevealType(preRevealAnimationGroups, revealType);
		LinkedRevealMapping targetLinkedRevealMapping = getLinkedRevealMappingForRevealType(revealAnimationGroups, revealType);

		AnimationListController.AnimationInformationList[] targetRevealList = targetLinkedRevealMapping.animationList;
		AnimationListController.AnimationInformationList[] targetPreRevealList = targetLinkedPreRevealMapping.animationList;
		int revealTypeIndex = (int)revealType;

		// determine our target index based on rank or random
		int targetIndex = 0;

		if (targetLinkedPreRevealMapping.playRandomAnimation)
		{
			// choose and play a random pre-reveal animation
			targetIndex = Random.Range(0, targetPreRevealList.Length);
		}
		else
		{
			// get the index based on credit rank
			targetIndex = pickingVariantParent.getCurrentRoundOutcome().getRankIndexForCreditValue(currentPick.credits);
		}

		// play the pre-reveal animation first
		AnimationListController.AnimationInformationList targetList = targetPreRevealList[targetIndex];
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(targetList));

		// look up and set data for this pre-reveal pick label
		PickingGameCreditPickItem creditsRevealItem = 
			PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(
				targetRevealList[targetIndex].animInfoList[0].targetAnimator.gameObject, 
				revealAnimationGroups[revealTypeIndex].creditRevealType);

		creditsRevealItem.setCreditLabels(currentPick.credits);

		// play the appropriate pre-reveal animation & trail
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(targetRevealList[targetIndex]));

		// get the position of the pre-reveal animator
		Vector3 trailStartPos = targetRevealList[targetIndex].animInfoList[0].targetAnimator.gameObject.transform.position;

		// sparkle trail from preReveal to actual reveal
		ParticleTrailController particleTrailController = ParticleTrailController.getParticleTrailControllerForType(pickItem.gameObject, ParticleTrailController.ParticleTrailControllerType.Default);
		if (particleTrailController != null)
		{
			yield return StartCoroutine(particleTrailController.animateParticleTrail(trailStartPos, pickItem.gameObject.transform.position, roundVariantParent.gameObject.transform));
		}

		// actual master pick animation revealed by the appropriate module
	}

	// Retrieve the appropriate animation list for the type of reveal provided
	private AnimationListController.AnimationInformationList[] getListForRevealType(LinkedRevealMapping[] targetMap, PickingGameCreditPickItem.CreditsPickItemType revealType)
	{
		LinkedRevealMapping linkedReveal = getLinkedRevealMappingForRevealType(targetMap, revealType);
		if (linkedReveal != null)
		{
			return linkedReveal.animationList;
		}
		else
		{
			return null;	
		}
	}

	// Retrieve the appropriate reveal mapping for a reveal type
	private LinkedRevealMapping getLinkedRevealMappingForRevealType(LinkedRevealMapping[] targetMap, PickingGameCreditPickItem.CreditsPickItemType revealType)
	{
		return System.Array.Find(targetMap, revealAnimation => (revealAnimation.creditRevealType == revealType));
	}

	// Return a reveal type based on outcome criteria
	private static PickingGameCreditPickItem.CreditsPickItemType getRevealTypeForOutcome(ModularChallengeGameOutcomeEntry currentPick)
	{
		// determine reveal type from pick properties
		PickingGameCreditPickItem.CreditsPickItemType revealType = PickingGameCreditPickItem.CreditsPickItemType.Default;

		if (currentPick.credits > 0)
		{
			revealType = PickingGameCreditPickItem.CreditsPickItemType.Default;
		}
		if (currentPick.additonalPicks > 0)
		{
			revealType = PickingGameCreditPickItem.CreditsPickItemType.IncreasePicks;
		}
		if (currentPick.canAdvance)
		{
			revealType = PickingGameCreditPickItem.CreditsPickItemType.Advance;
		}

		return revealType;
	}
}
