using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PinnedSlideController : SlideController
{
	// =============================
	// PROTECTED
	// =============================
	protected List<GameObject> pinnedItems = new List<GameObject>(); // objects to pin when they go out of view
	protected List<GameObject> leftOrTopPinnedItems = new List<GameObject>(); // objects currently being pinned
	protected List<GameObject> rightOrBottomPinnedItems = new List<GameObject>(); // objects currently being pinned
	protected List<Vector3> pinnedItemPositions = new List<Vector3>();

	[SerializeField] protected GameObject leftOrTopPinParent;
	[SerializeField] protected GameObject rightOrBottomPinParent;
	[SerializeField] protected UIPanel panel;

	[Tooltip("Spacing between pinned objects")]
	[SerializeField] protected float pinPadding = 0.0f;

	[Tooltip("Either the width or height of your pinned object, depending on the slide controller orientation")]
	[SerializeField] protected float pinSize = 0.0f;

	// =============================
	// PUBLIC
	// =============================
	public delegate void PinDelegate(GameObject item);
	public event PinDelegate onUnpin;
	public event PinDelegate onPin;

	public void addPinnedItem(GameObject pinnedItem)
	{
		if (!pinnedItems.Contains(pinnedItem) && pinnedItem != null)
		{
			pinnedItems.Add(pinnedItem);
			pinnedItemPositions.Add(pinnedItem.transform.localPosition);
		}
	}

	protected override void setContentPosition(float newValue)
	{
		base.setContentPosition(newValue);
		updatePinning();
	}

	public override void Update()
	{
		base.Update();

		float mouseScroll = -Input.mouseScrollDelta.y * mouseScrollSpeed;
		bool isMouseScrolling = Mathf.Abs(mouseScroll) > 0.5f && currentScrollingSlider == this && shouldUseMouseScroll;

		if (!isAnimation && isMouseScrolling)
		{
			updatePinning();
		}
		else if (!isAnimation && TouchInput.isDragging && (swipeArea == null || TouchInput.swipeArea == swipeArea))
		{
			updatePinning();
		}
	}

	public void updatePinning()
	{
		GameObject item = null;
		for (int i = 0; i < pinnedItems.Count; ++i)
		{
			item = pinnedItems[i];

			if (item != null)
			{
				if (currentOrientation == Orientation.HORIZONTAL)
				{
					//updateHorizontalPin(item);
				}
				else
				{
					updateVerticalPin(item);
				}
			}
		}
	}

	protected virtual void updateVerticalPin(GameObject item)
	{
		Vector3 contentPosition = content.transform.localPosition;
		float bottomBound = -(contentPosition.y + panel.clipRange.w/2);
		float topBound = -(contentPosition.y - panel.clipRange.w/2);

		int index = pinnedItems.IndexOf(item);
		Vector3 itemPos = pinnedItemPositions[index];

		float upperBoundOffset = leftOrTopPinnedItems.Count * pinSize;
		float lowerBoundOffset = (rightOrBottomPinnedItems.Count+1) * pinSize;

		if (!leftOrTopPinnedItems.Contains(item) && !rightOrBottomPinnedItems.Contains(item))
		{
			// top bound check
			if (itemPos.y + upperBoundOffset >= topBound)
			{
				pin(item, leftOrTopPinParent, new Vector3(itemPos.x, -(leftOrTopPinnedItems.Count * pinSize), itemPos.z));
				leftOrTopPinnedItems.Add(item);
				return;
			}

			// bottom bound check
			if (itemPos.y - lowerBoundOffset <= bottomBound)
			{
				pin(item, rightOrBottomPinParent, new Vector3(itemPos.x, rightOrBottomPinnedItems.Count * pinSize, itemPos.z));
				rightOrBottomPinnedItems.Add(item);
				return;
			}
		}

		// removal check
		if
		(
			(rightOrBottomPinnedItems.Contains(item) && itemPos.y - (lowerBoundOffset - pinSize) > bottomBound) ||
			(leftOrTopPinnedItems.Contains(item) && itemPos.y + (upperBoundOffset - pinSize) < topBound)
		)
		{
			unpin(item);
		}
	}

	// This is commented out, as it is untested/unfinished. Leaving the function here in case this
	// becomes desired functionality at some point. This function should more or less be correct
	// except for possibly the left/right bound calculation
	/*protected virtual void updateHorizontalPin(GameObject item)
	{
		Vector3 contentPosition = content.transform.localPosition;
		float leftBound = -(contentPosition.x - panel.clipRange.z/2);
		float rightBound = -(contentPosition.x + panel.clipRange.z/2);

		int index = pinnedItems.IndexOf(item);
		Vector3 itemPos = pinnedItemPositions[index];

		float leftBoundOffset = leftOrTopPinnedItems.Count * pinSize;
		float rightBoundOffset = (rightOrBottomPinnedItems.Count+1) * pinSize;

		if (!leftOrTopPinnedItems.Contains(item) && !rightOrBottomPinnedItems.Contains(item))
		{
			// left bound check
			if (itemPos.x - leftBoundOffset <= leftBound)
			{
				pin(item, leftOrTopPinParent, new Vector3(-(leftOrTopPinnedItems.Count * pinSize), itemPos.y, itemPos.z));
				leftOrTopPinnedItems.Add(item);
				return;
			}

			// right bound check
			if (itemPos.x + rightBoundOffset >= rightBound)
			{
				pin(item, rightOrBottomPinParent, new Vector3(rightOrBottomPinnedItems.Count * pinSize, itemPos.y, itemPos.z));
				rightOrBottomPinnedItems.Add(item);
				return;
			}
		}

		// removal check
		if
		(
			(rightOrBottomPinnedItems.Contains(item) && itemPos.x - (leftBoundOffset - pinSize) > leftBoundOffset) ||
			(leftOrTopPinnedItems.Contains(item) && itemPos.x + (rightBoundOffset - pinSize) < rightBoundOffset)
		)
		{
			unpin(item);
		}
	}*/

	protected virtual void pin(GameObject item, GameObject parent, Vector3 pos)
	{
		if (item != null)
		{
			Transform t = item.transform;
			t.SetParent(parent.transform, false);

			t.localPosition = pos;
			item.layer = parent.layer;
			NGUITools.MarkParentAsChanged(item);

			onPinned(item);
		}
	}

	protected virtual void onPinned(GameObject item)
	{
		if (onPin != null)
		{
			onPin(item);
		}
	}

	protected virtual void unpin(GameObject item)
	{
		if (item != null)
		{
			int index = pinnedItems.IndexOf(item);
			item.transform.parent = content.transform;

			NGUITools.MarkParentAsChanged(item);
			item.transform.localPosition = pinnedItemPositions[index];

			rightOrBottomPinnedItems.Remove(item);
			leftOrTopPinnedItems.Remove(item);

			onUnpinned(item);
		}
	}

	protected virtual void onUnpinned(GameObject item)
	{
		if (onUnpin != null)
		{
			onUnpin(item);
		}
	}

	public override void safleySetXLocation(float x)
	{
		base.safleySetXLocation(x);
		updatePinning();
	}

	public override void safleySetYLocation(float y)
	{
		base.safleySetYLocation(y);
		updatePinning();
	}

	public void removeAllPins()
	{
		int i = 0;
		for (i = 0; i < rightOrBottomPinnedItems.Count; ++i)
		{
			unpin(rightOrBottomPinnedItems[i]);
		}

		for (i = 0; i < leftOrTopPinnedItems.Count; ++i)
		{
			unpin(leftOrTopPinnedItems[i]);
		}

		rightOrBottomPinnedItems.Clear();
		leftOrTopPinnedItems.Clear();
		pinnedItems.Clear();
		pinnedItemPositions.Clear();
	}

	public bool isItemPinned(GameObject item)
	{
		return rightOrBottomPinnedItems.Contains(item) || leftOrTopPinnedItems.Contains(item);
	}
}