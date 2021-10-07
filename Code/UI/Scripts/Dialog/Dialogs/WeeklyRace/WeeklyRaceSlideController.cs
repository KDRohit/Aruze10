using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeeklyRaceSlideController : PinnedSlideController
{
	protected override void updateVerticalPin(GameObject item)
	{
		base.updateVerticalPin(item);
		applyEasing(item);
	}

	private void applyEasing(GameObject item)
	{
		Vector3 contentPosition = content.transform.localPosition;
		float bottomBound = -(contentPosition.y + panel.clipRange.w/2);
		float topBound = -(contentPosition.y - panel.clipRange.w/2);

		int index = pinnedItems.IndexOf(item);
		Vector3 itemPos = pinnedItemPositions[index];

		float upperBoundOffset = leftOrTopPinnedItems.Count * pinSize;
		float lowerBoundOffset = (rightOrBottomPinnedItems.Count+1) * pinSize;

		// parametric variables
		float scaleAmount;

		// top bound check
		float itemUpperBound = itemPos.y + upperBoundOffset;
		if (itemUpperBound + pinSize > topBound && itemUpperBound < topBound)
		{
			scaleAmount = getScaleAmount(itemUpperBound, topBound);
			item.transform.localScale = new Vector3(scaleAmount, scaleAmount, 1);
		}

		// bottom bound check
		float itemLowerBound = itemPos.y - lowerBoundOffset;
		if (itemLowerBound - pinSize <= bottomBound && itemLowerBound > bottomBound)
		{
			scaleAmount = getScaleAmount(bottomBound, itemLowerBound);
			item.transform.localScale = new Vector3(scaleAmount, scaleAmount, 1);
		}
		else if (!isItemPinned(item) && item.transform.localScale.x > 1f)
		{
			item.transform.localScale = Vector3.one;
		}
	}

	private float getScaleAmount(float a, float b)
	{
		a = Mathf.Abs(a);
		b = Mathf.Abs(b);
		float p = 1 - (a - b) / pinSize;
		return 1 - p + p * WeeklyRacePlayerListItem.SCALE_SIZE;
	}
}
