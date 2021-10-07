using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Script is attached to each page indicator button for a page scroller setup.
*/

public class PageIndicator : TICoroutineMonoBehaviour
{
	public UIImageButton button;	///< The button that visually represents the indicator and is clicked to navigate to a page.
	
	[HideInInspector] public int page;						///< The page that this indicator represents [0-based].
	[HideInInspector] public BasePageScroller pageScroller;		///< The PageScroller that controls this indicator.
	
	/// NGUI button callback.
	public void OnClick()
	{
		pageScroller.clickPageIndicator(page);
	}
}
