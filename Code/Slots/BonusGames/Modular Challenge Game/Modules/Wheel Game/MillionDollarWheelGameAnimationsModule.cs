using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module to roll up the win amount and then play an animation 
 */
public class MillionDollarWheelGameAnimationsModule : WheelGameModule
{
	[SerializeField] protected AnimationListController.AnimationInformationList normalSliceAnimations;
	[SerializeField] protected AnimationListController.AnimationInformationList bigSliceAnimations;

	[SerializeField] private List<int> bigSliceStops;

	public override bool needsToExecuteOnSpinComplete()
	{
		return true; // We won some sort of credits.
	}

	// Update the winnings label if we've won credits from this outcome
	public override IEnumerator executeOnSpinComplete()
	{
		yield return StartCoroutine(base.executeOnSpinComplete());
		int wheelIndex = wheelRoundVariantParent.outcome.getRound(wheelRoundVariantParent.roundIndex).entries[0].wheelWinIndex;
		// Roll up the base amount.
		if (bigSliceStops.Contains(wheelIndex))
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(bigSliceAnimations));
		}
		else
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(normalSliceAnimations));
		}
	}
}
