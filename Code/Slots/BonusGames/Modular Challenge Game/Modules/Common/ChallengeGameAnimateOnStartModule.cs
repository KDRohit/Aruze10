using UnityEngine;
using System.Collections;


/**
 * Module to play a specific animator state on a target on round start
 */
public class ChallengeGameAnimateOnStartModule : ChallengeGameModule
{
	public Animator targetAnimator;
	public string	ANIMATION_NAME = "intro";

	// Enable round start action
	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}
	
	// Executes the defined animation on round start
	public override IEnumerator executeOnRoundStart()
	{
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(targetAnimator, ANIMATION_NAME));
	}

}
