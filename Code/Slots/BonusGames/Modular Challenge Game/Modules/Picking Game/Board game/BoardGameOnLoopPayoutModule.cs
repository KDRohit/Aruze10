
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module to handle payout when token loops on the board. 
 */
public class BoardGameOnLoopPayoutModule : BoardGameModule
{
	[SerializeField] private LabelWrapperComponent creditsLabel;

	[Tooltip("Payout animation to play when the token passes go")]
	[SerializeField] private AnimationListController.AnimationInformationList boardLoopPayoutAnimations;
	
	public override bool needsToExecuteOnBoardLoop()
	{
		return true;
	}
	
	public override IEnumerator executeOnBoardLoop()
	{
		long loopCredits = boardGameVariantParent.loopCreditsAmount;
		if (loopCredits > 0)
		{
			creditsLabel.text = CreditsEconomy.convertCredits(loopCredits);
			List<TICoroutine> coroutines = new List<TICoroutine>();
			coroutines.Add(StartCoroutine(rollupCredits(loopCredits)));
			coroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(boardLoopPayoutAnimations)));
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutines));
		}
	}
}