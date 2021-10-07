using System.Collections;
using UnityEngine;

public class InboxListItemMultiButton : InboxListItem
{
	// item that has multiple primary buttons animations
	[SerializeField] protected AnimationListController.AnimationInformationList multiButtonOffAnimations;
	[SerializeField] protected AnimationListController.AnimationInformationList multiButtonIntroAnimations;
	[SerializeField] protected AnimationListController.AnimationInformationList multiButtonIdleAnimations;
	[SerializeField] protected AnimationListController.AnimationInformationList multiButtonOutroAnimations;
	
	/*=========================================================================================
	MULTIPLE BUTTON ANIMATION HANDLING
	=========================================================================================*/
	protected override IEnumerator waitForListItemAnimThenSetButtonAnimation(ItemAnimations anim)
	{
		yield return StartCoroutine(base.waitForListItemAnimThenSetButtonAnimation(anim));

		switch (anim)
		{
			case ItemAnimations.Idle:
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(multiButtonIdleAnimations));
				break;

			case ItemAnimations.Intro:
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(multiButtonIntroAnimations));
				break;

			case ItemAnimations.Outro:
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(multiButtonOutroAnimations));
				break;

			case ItemAnimations.Off:
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(multiButtonOffAnimations));
				break;
		}
	}
}
