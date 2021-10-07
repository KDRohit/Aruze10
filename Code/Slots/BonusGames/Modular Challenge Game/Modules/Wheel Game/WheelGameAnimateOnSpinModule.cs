using UnityEngine;
using System.Collections;


/**
 * Module to play a specific animator state on a target on spin action
 */
public class WheelGameAnimateOnSpinModule : WheelGameModule
{
	[SerializeField] private AnimationListController.AnimationInformationList onSpinAnimations;

	// Enable the spin action
	public override bool needsToExecuteOnSpin()
	{
		return true;
	}

	// Executes the defined animation on spin
	public override IEnumerator executeOnSpin()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(onSpinAnimations));
	}
}
