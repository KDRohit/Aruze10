using System.Collections;
using UnityEngine;

/*
 * Represents individual pip on the progress meter.
 */
public class BoardGameProgressMeterPip : TICoroutineMonoBehaviour
{
	[SerializeField] private AnimationListController.AnimationInformationList onAnimations;
	[SerializeField] private AnimationListController.AnimationInformationList offAnimations;

	public IEnumerator playAnimation(bool isOn)
	{
		if (isOn)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onAnimations));
		}
		else
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(offAnimations));
		}
	}
}