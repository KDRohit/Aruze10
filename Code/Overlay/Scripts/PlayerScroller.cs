using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Handles dynamically sizing a PageScroller used for friends on the bottom part of the overlay.
*/

public class PlayerScroller : MonoBehaviour
{
	public delegate void PageScrollerInitDelegate(int visiblePanels);

	private const float BASE_ASPECT_RATIO = 1.33f;				// 1.33 is the aspect ratio for iPad 3:2
	private const float SCROLLER_BASE_WIDTH = 1430.0f;			// The base width for iPad.
	private const float SMALL_DEVICE_BACKGROUND_HEIGHT = 418.0f;

	public PageScroller pageScroller;
	public GameObject background;
	public Transform[] lowerRightButtons;
	public Transform bottomRightButtonSizer;
	public GameObject loadingLabel;
		
	public void updateSize(PageScrollerInitDelegate pageScrollerInitCallback)
	{
		if (MobileUIUtil.isSmallMobile)
		{
			CommonTransform.setHeight(background.transform, SMALL_DEVICE_BACKGROUND_HEIGHT);
		}

		// In order to extend this panel along the bottom part of the screen, we need to know the X extent.
		// Use the anchor that is in the bottom-left corner of the screen as a starting point:
		
		// Set the default new friends width based on the iPad aspect ratio,
		// so if we're on a large device that's wider aspect ratio,
		// it will give us more space for the friends, proportionally.
		float widthMultiplier = NGUIExt.aspectRatio / BASE_ASPECT_RATIO;
		float newWidth = SCROLLER_BASE_WIDTH * widthMultiplier;
		
		// Use the effective screen width (the width NGUI sees).
		float fullWidth = NGUIExt.effectiveScreenWidth;

		// Assuming the left edge is enough for the left arrow by default.
		float arrowButtonSpace = background.transform.localPosition.x * 2.0f;

		// How much space in between the buttons in the lower right area?
		// Compare the two buttons in the same sizer parent since they're always relatively the same,
		// regardless of whether the parent was sized and positioned for small devices.
		float lowerRightButtonSpacing = Mathf.Abs(lowerRightButtons[0].localPosition.x - lowerRightButtons[1].localPosition.x);

		if (MobileUIUtil.isSmallMobile)
		{
			// Make some spacing bigger for small devices, because we scale these buttons up.
			SmallDeviceSpriteScaler scaler = pageScroller.decrementButton.GetComponent<SmallDeviceSpriteScaler>();
			if (scaler != null)
			{
				arrowButtonSpace *= scaler.scaleX;
			}
			scaler = bottomRightButtonSizer.GetComponent<SmallDeviceSpriteScaler>();
			if (scaler != null)
			{
				lowerRightButtonSpacing *= scaler.scaleX;
			}
		}

		float rightButtonsSpace = lowerRightButtonSpacing * lowerRightButtons.Length;
				
		newWidth = fullWidth		
			- arrowButtonSpace							// Space for the Left/Right arrows.
			- rightButtonsSpace;						// Move over to accomodate the invite button.
		
		CustomLog.Log.log("PlayerScroller.fullWidth: " + fullWidth + ", newWidth: " + newWidth + ", arrowButtonSpace: " + arrowButtonSpace + ", rightButtonsSpace: " + rightButtonsSpace);

		// Background panel:
		int visiblePanels = (int)(newWidth / pageScroller.effectiveSpacingX);	// How many friends should fit into our new panel.
		int extraSpace = MobileUIUtil.isSmallMobile ? 24 : 16;					// Add a bit of spacing for left and right of friend panels.
		newWidth = visiblePanels * pageScroller.effectiveSpacingX + extraSpace;

		CommonTransform.setWidth(background.transform, newWidth);							// Set the width of our friends panel.
		
		// Adjust the bar position to center in the available space between the bar and the right buttons,
		// which also gives us more room for the left/right arrows.
		float offset = Mathf.Floor((fullWidth - newWidth - arrowButtonSpace - rightButtonsSpace) * .5f);
		
		CommonTransform.setX(transform, Mathf.Max(0, offset));
		
		loadingLabel.SetActive(true);
		
		pageScrollerInitCallback(visiblePanels);
	}
}
