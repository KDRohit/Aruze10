using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Plays specific animations based on what the group was for the won wheel wedge, intended
for games with meters that need to play based on a specific wedge landing.

Original Author: Scott Lepthien
Creation Date: 9/5/2018 
*/
public class WheelGameAnimateOnSpinCompleteBasedOnGroup : WheelGameModule 
{
	[SerializeField] private SymbolWheelAnimations[] groupWinAnimations;
	[Tooltip("Used if you want the rollup of the credits to be handled by this module. This will allow you to trigger the rollup during the animations rather than before or after it.")]
	[SerializeField] private bool isDoingRollup = false;
	[Tooltip("Delay before the rollup starts, if this is 0 then the rollup will start at the same time as the animations.")]
	[SerializeField] private float delayBeforeRollup = 0.0f;
	[Tooltip("Delay after the rollup before the outro animaitons play.")]
	[SerializeField] private float delayBeforeOutroAnims = 0.0f;
	
	[System.Serializable]
	public class SymbolWheelAnimations
	{
		public string group;
		public AnimationListController.AnimationInformationList celebrateAnimationList;
		public AnimationListController.AnimationInformationList outroAnimationList;
	}
	
	private SymbolWheelAnimations currentWheelAnimationEntry = null;

	// Execute when the wheel has completed spinning
	public override bool needsToExecuteOnSpinComplete()
	{
		long creditsWon = wheelRoundVariantParent.outcome.getCurrentRound().entries[0].credits;
		string group = wheelRoundVariantParent.outcome.getCurrentRound().entries[0].groupId;
		currentWheelAnimationEntry = getWheelAnimationListEntryForGroup(group);

		if (currentWheelAnimationEntry != null || (isDoingRollup && creditsWon > 0))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	// Rollup the credits after a delay, which will allow it to sync with animations
	private IEnumerator doCreditsRollupAfterDelay(float delay, long creditsWon)
	{
		yield return new TIWaitForSeconds(delay);
		wheelRoundVariantParent.addCredits(creditsWon);
		yield return StartCoroutine(wheelRoundVariantParent.animateScore(0, creditsWon));
	}

	public override IEnumerator executeOnSpinComplete()
	{
		long creditsWon = wheelRoundVariantParent.outcome.getCurrentRound().entries[0].credits;
		List<TICoroutine> routinesRunningWithAnimations = new List<TICoroutine>();

		if (isDoingRollup && creditsWon > 0)
		{
			if (delayBeforeRollup > 0.0f)
			{
				routinesRunningWithAnimations.Add(StartCoroutine(doCreditsRollupAfterDelay(delayBeforeRollup, creditsWon)));
			}
			else
			{
				// Rollup immediatly
				wheelRoundVariantParent.addCredits(creditsWon);
				routinesRunningWithAnimations.Add(StartCoroutine(wheelRoundVariantParent.animateScore(0, creditsWon)));
			}
		}

		if (currentWheelAnimationEntry != null && currentWheelAnimationEntry.celebrateAnimationList.Count > 0)
		{
			// Do celebration animations and wait for the rollup to finish
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentWheelAnimationEntry.celebrateAnimationList));
		}
		
		// Make sure the rollup is done before proceeding
		if (routinesRunningWithAnimations.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(routinesRunningWithAnimations));
		}
		
		if (delayBeforeOutroAnims > 0.0f)
		{
			yield return new TIWaitForSeconds(delayBeforeOutroAnims);
		}
		
		// Play outro animations
		if (currentWheelAnimationEntry != null && currentWheelAnimationEntry.outroAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentWheelAnimationEntry.outroAnimationList));
		}
	}

	private SymbolWheelAnimations getWheelAnimationListEntryForGroup(string group)
	{
		for (int i = 0; i < groupWinAnimations.Length; i++)
		{
			if (group == groupWinAnimations[i].group)
			{
				return groupWinAnimations[i];
			}
		}

		Debug.LogError("WheelGameAnimateOnSpinCompleteBasedOnGroup.getWheelAnimationListEntryForBonusName() - Unable to find entry for group = " + group);
		return null;
	}
}
