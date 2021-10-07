using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostPurchaseChallengeItem : MonoBehaviour
{
	[SerializeField] private AnimationListController.AnimationInformationList idleAnimInfo;
	[SerializeField] private AnimationListController.AnimationInformationList preWinAnimInfo;
	[SerializeField] private AnimationListController.AnimationInformationList winAnimInfo;
	[SerializeField] private AnimationListController.AnimationInformationList loseAnimInfo;
	
	public void playIdleAnimation()
	{
		StartCoroutine(AnimationListController.playListOfAnimationInformation(idleAnimInfo));
	}

	public void playPreWinAnimation()
	{
		StartCoroutine(AnimationListController.playListOfAnimationInformation(preWinAnimInfo));
	}

	public IEnumerator playWinSequence()
	{
		yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(winAnimInfo));
	}
	
	public IEnumerator playLoseSequence()
	{
		yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(loseAnimInfo));
	}
	
}
