using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AmbientMultipleAnimationsEffectModule : AmbientEffectModule {

	[SerializeField] private List<AnimationListController.AnimationInformationList> ambientAnimationList;
	[SerializeField] private bool playRandom = false;

	private int index = 0;

	protected override IEnumerator animationCallback ()
	{
		if (playRandom == true)
		{
			index = Random.Range(0, ambientAnimationList.Count);
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(ambientAnimationList[index]));
		}
		else
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(ambientAnimationList[index]));
			index++;
			index = index % ambientAnimationList.Count;
		}
	}
}
	
