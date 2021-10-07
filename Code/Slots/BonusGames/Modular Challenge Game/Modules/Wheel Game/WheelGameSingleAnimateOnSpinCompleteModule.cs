using UnityEngine;
using System.Collections;



/**
 * Module to play a specific animator state on a target on spin action finished
 */
public class WheelGameSingleAnimateOnSpinCompleteModule : WheelGameModule
{
	public Animator targetAnimator;
	public string	ANIMATION_NAME = "anim";

	// Enable the spin action
	public override bool needsToExecuteOnSpinComplete()
	{
		return true;
	}
	
	// Executes the defined animation on spin
	public override IEnumerator executeOnSpinComplete()
	{
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(targetAnimator, ANIMATION_NAME));
	}
}
