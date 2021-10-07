using UnityEngine;
using System.Collections;

//A super simple class to play one of the new animation infor on slot game start
public class PlayAnimationOnSlotGameStartModule : SlotModule
{
	[SerializeField] private AnimationListController.AnimationInformation animationInfo;

	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		yield return StartCoroutine(AnimationListController.playAnimationInformation(animationInfo));
	}
}
