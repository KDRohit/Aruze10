using UnityEngine;
using System.Collections;

/**
 * Module to play a specific animation based on the specific pick item selected
 */
public class PickingGameAnimateByPickClickModule : PickingGameModule 
{
	[System.Serializable]
	public class LinkedPickAnimation 
	{
		public PickingGameBasePickItem pickItem;
		public AnimationListController.AnimationInformationList animationList;
	}

	[SerializeField] public LinkedPickAnimation[] linkedPickAnimations;


	public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
	{
		return true;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		AnimationListController.AnimationInformationList linkedAnimationList = getListForPickItem(pickItem);
		// play target animations from array
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(linkedAnimationList));
	}

	// Look up a matching animation list from our array
	private AnimationListController.AnimationInformationList getListForPickItem(PickingGameBasePickItem pickItem)
	{
		LinkedPickAnimation linkedPickAnim = System.Array.Find(linkedPickAnimations, pickAnim => (pickAnim.pickItem == pickItem));
		if (linkedPickAnim != null)
		{
			AnimationListController.AnimationInformationList linkedAnimations = linkedPickAnim.animationList;
			return linkedAnimations;
		}
		else
		{
			Debug.LogWarning("Failed to find a linked pick animation for pickItem: " + NGUITools.GetHierarchy(pickItem.gameObject) + " - return null!");
			return null;
		}
	}
}