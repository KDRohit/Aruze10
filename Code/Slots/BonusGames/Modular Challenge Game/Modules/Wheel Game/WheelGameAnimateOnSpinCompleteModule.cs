using UnityEngine;
using System.Collections;



/**
 * Module to play an animation list when a wheel spin action finishes
 */
public class WheelGameAnimateOnSpinCompleteModule : WheelGameModule
{
	[SerializeField] private AnimationListController.AnimationInformationList onSpinCompleteAnimations;

	// Enable the spin action
	public override bool needsToExecuteOnSpinComplete()
	{
		return onSpinCompleteAnimations != null && onSpinCompleteAnimations.Count > 0;
	}
	
	// Executes the defined animation on spin
	public override IEnumerator executeOnSpinComplete()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onSpinCompleteAnimations));
	}
}
