using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuestForTheChest
{
	public class QFCCoinBurst : TICoroutineMonoBehaviour
	{

		[SerializeField] private Transform coinTarget;
		[SerializeField] private AnimationListController.AnimationInformationList burstAnimation;

		public void setTarget(Transform targetPosition)
		{
			coinTarget.position = targetPosition.position;
		}
		
		public IEnumerator playBurstAnimation()
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(burstAnimation));
		}
	}
}

