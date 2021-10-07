using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to play a collection of animator states concurrently on a target on round end
 */
public class ChallengeGameAnimateOnRoundEndModule : ChallengeGameModule
{
	[SerializeField] private AnimationListController.AnimationInformationList animationInformation;
	[SerializeField] private bool shouldPlayOnAdvanceRound = true;
	[SerializeField] private bool shouldPlayOnEndGame = true;

	// Enable round end action
	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		if (isEndOfGame)
		{
			return shouldPlayOnEndGame;
		}
		else
		{
			return shouldPlayOnAdvanceRound;
		}
	}
	
	// Executes the defined animation on round end
	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationInformation));
	}
}