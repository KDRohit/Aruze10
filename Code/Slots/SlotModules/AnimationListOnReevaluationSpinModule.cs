using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Class for playing an AnimationListController list for reevaulation spins
Originally built for the frame animation in aruze04 Goddesses Hera

Creation Date: December 4, 2017
Original Author: Scott Lepthien
*/
public class AnimationListOnReevaluationSpinModule : SlotModule 
{
	[SerializeField] private AnimationListController.AnimationInformationList animationList;
	[SerializeField] private AnimationListController.AnimationInformationList idleAnimationList; // idle animations to swap back to when the next spin after the respins starts
	[SerializeField] private bool isPlayingOnFirstRespinOnly = true; // controls if the animation happens every respin, or just the first one
	[SerializeField] private bool isResetingToIdleAfterReelStop = false; // controls if the animation will reset to idle when the respin stops, or when the next prespin happens

	private bool isFirstRespinDone = false; // track when the first respin comepletes in case we only want to play the animations for the first respin

// executeOnPreSpin() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		isFirstRespinDone = false;

		if (!isResetingToIdleAfterReelStop && idleAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(idleAnimationList));
		}
		else
		{
			yield break;
		}
	}

// executeOnReevaluationReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		return isResetingToIdleAfterReelStop;
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		if (idleAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(idleAnimationList));
		}
		else
		{
			yield break;
		}
	}

// executeOnReevaluationPreSpin() section
// function in a very similar way to the normal PreSpin hook, but hooks into ReelGame.startNextReevaluationSpin()
// and triggers before the reels begin spinning
	public override bool needsToExecuteOnReevaluationPreSpin()
	{
		if (animationList.Count > 0)
		{
			return (!isPlayingOnFirstRespinOnly || !isFirstRespinDone);
		}
		else
		{
			Debug.LogError("AnimationListOnReevaluationSpinModule.needsToExecuteOnReevaluationPreSpin() - animationList.Count is 0, nothing to animate!");
			return false;
		}
	}

	public override IEnumerator executeOnReevaluationPreSpin()
	{
		isFirstRespinDone = true;
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationList));
	}
}
