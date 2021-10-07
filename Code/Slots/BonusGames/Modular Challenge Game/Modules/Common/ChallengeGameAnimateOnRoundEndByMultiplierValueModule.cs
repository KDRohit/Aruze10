using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to play a collection of animator states concurrently on a target on round end (varying by current multiplier)
 */
public class ChallengeGameAnimateOnRoundEndByMultiplierValueModule : ChallengeGameAnimateOnRoundEndModule
{
	[SerializeField] private List<ChallengeGameMultiplierHelper.MultiplierAnimationInfoList> transitionByMultiplier;


	// Executes the defined animation list on round end
	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		yield return StartCoroutine(
			AnimationListController.playListOfAnimationInformation(
				ChallengeGameMultiplierHelper.getAnimationInfoForMultiplierInList(roundVariantParent.gameParent.currentMultiplier, transitionByMultiplier)));
	}
}