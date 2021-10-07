using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Plays a series of animations based on what wheel slice is landed on.  Useful if each slice has a specific animation it might need
to play when it is landed.

Original Author: Scott Lepthien
Creation Date: 9/10/2018 
*/
public class WheelGameAnimateOnSpinCompleteBasedOnWheelWinIndex : WheelGameModule 
{
	[SerializeField] private SymbolWheelAnimations[] winIndexWinAnimations;
	[Tooltip("Delay after playing the animations before continuing")]
	[SerializeField] private float delayAfterAnims = 0.0f;
	
	[System.Serializable]
	public class SymbolWheelAnimations
	{
		public long winIndex;
		public AnimationListController.AnimationInformationList celebrateAnimationList;
	}
	
	private SymbolWheelAnimations currentWheelAnimationEntry = null;

	// Execute when the wheel has completed spinning
	public override bool needsToExecuteOnSpinComplete()
	{
		int winIndex = wheelRoundVariantParent.outcome.getCurrentRound().entries[0].wheelWinIndex;
		currentWheelAnimationEntry = getWheelAnimationListEntryForWinIndex(winIndex);

		if (currentWheelAnimationEntry != null)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public override IEnumerator executeOnSpinComplete()
	{
		if (currentWheelAnimationEntry != null && currentWheelAnimationEntry.celebrateAnimationList.Count > 0)
		{
			// Do celebration animations and wait for the rollup to finish
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(currentWheelAnimationEntry.celebrateAnimationList));
		}
		
		if (delayAfterAnims > 0.0f)
		{
			yield return new TIWaitForSeconds(delayAfterAnims);
		}
	}

	private SymbolWheelAnimations getWheelAnimationListEntryForWinIndex(int winIndex)
	{
		for (int i = 0; i < winIndexWinAnimations.Length; i++)
		{
			if (winIndex == winIndexWinAnimations[i].winIndex)
			{
				return winIndexWinAnimations[i];
			}
		}

		Debug.LogError("WheelGameAnimateOnSpinCompleteBasedOnGroup.WheelGameAnimateOnSpinCompleteBasedOnWheelWinIndex() - Unable to find entry for winIndex = " + winIndex);
		return null;
	}
}
