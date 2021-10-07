using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/** 
 * Module to play a specific animation / audio list when a desired number of picks remaining is reached
 * Has a toggle for picks decreasing to support "last pick" effects while avoiding +1 picks on final displayed
 */
public class PickingGameAnimateOnPicksRemainingModule : PickingGameModule 
{
	[SerializeField] private List<int> targetPickCounts;  // specific pick values to trigger on.
	[SerializeField] private bool onPicksDecreasing = true;
	[SerializeField] protected AnimationListController.AnimationInformationList animationInfoList;

	public override bool needsToExecuteOnAdvancePick()
	{
		if (targetPickCounts.Contains(pickingVariantParent.gameParent.getDisplayedPicksRemaining()))
		{
			if (onPicksDecreasing && pickingVariantParent.gameParent.isPicksDecreasing())
			{
				return true;
			}
			else if (!onPicksDecreasing && !pickingVariantParent.gameParent.isPicksDecreasing())
			{
				return true;
			}
		}
		return false;
	}

	public override IEnumerator executeOnAdvancePick()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationInfoList));
	}
}
