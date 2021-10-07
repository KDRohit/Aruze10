using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to play a specific AnimationList on a round start based on the current multiplier value
 */
public class ChallengeGameAnimateOnStartRoundByMultiplierValueModule : ChallengeGameAnimateOnStartRoundModule {

	[SerializeField] private List<ChallengeGameMultiplierHelper.MultiplierAnimationInfoList> transitionByMultiplier;

	// Executes the defined animation list on round start
	public override IEnumerator executeOnRoundStart()
	{
		yield return StartCoroutine(
			AnimationListController.playListOfAnimationInformation(
				ChallengeGameMultiplierHelper.getAnimationInfoForMultiplierInList(roundVariantParent.gameParent.currentMultiplier, transitionByMultiplier)));
	}
}
