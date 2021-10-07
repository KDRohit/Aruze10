using UnityEngine;
using System.Collections;

/**
 * Animation module for standard animation lists that should play only on the first pick in a round
 */
public class PickingGameAnimateOnFirstPick : PickingGameModule 
{
	[SerializeField] protected AnimationListController.AnimationInformationList animationInfoList;

	public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
	{
		// check if we're the first pick for the round
		if (pickingVariantParent.getCurrentRoundOutcome().entries.IndexOf(pickData) == 0)
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
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationInfoList));
	}
}
