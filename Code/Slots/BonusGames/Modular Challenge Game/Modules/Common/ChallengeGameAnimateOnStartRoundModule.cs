using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to play a collection of animator states concurrently on a target on round start
 */
public class ChallengeGameAnimateOnStartRoundModule : ChallengeGameModule
{
	[SerializeField] protected AnimationListController.AnimationInformationList animationInformation;

	// Enable round start action
	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}
	
	// Executes the defined animation on round start
	public override IEnumerator executeOnRoundStart()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationInformation));
	}
}
