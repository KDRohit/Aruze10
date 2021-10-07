using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Attach to a GameObject to be able to scale it when on a small device size according to the parameters set in the script.
It is run once upon Awake, and each time when returning to the game after being paused,
which can cause some screen resolution confusion on the device when restoring.
*/

public class SmallDeviceSpriteScaler : MonoBehaviour
{
	public float scaleX = 1.0f;
	public float scaleY = 1.0f;
	public bool scaleWidthByAspectRatio = false;
	public bool scaleHeightByAspectRatio = false;
	
	private Vector3 originalScale = Vector3.one;
	
	private const float BASE_ASPECT_RATIO = 1.33f;
	private const float GOAL_ASPECT_RATIO = 1.5f;
		 
	void Awake()
	{
		if (MobileUIUtil.isSmallMobile)
		{
			originalScale = transform.localScale;
		}
		else
		{
			// Don't use this script if not on small device.
			Destroy(this);
		}
	}
	
	void Update()
	{
		transform.localScale = Vector3.Scale(originalScale, new Vector3(scaleX, scaleY, 1.0f));

		if (scaleWidthByAspectRatio || scaleHeightByAspectRatio)
		{
			// Set defaults to 1 just in case one of these isn't scaled by aspect ratio.
			float aspectRatioScaleX = 1f;
			float aspectRatioScaleY = 1f;
			
			// Do this adjustment after the normal adjustment, so it can be based on the first adjusted size if necessary.
			if (scaleWidthByAspectRatio)
			{
				// Determine the X scale based on the aspect ratio of the screen.
				// This will always be a lower value, and only used when the aspect ratio
				// is less than 4:3.
				if (NGUIExt.aspectRatio < BASE_ASPECT_RATIO)
				{
					aspectRatioScaleX = NGUIExt.aspectRatio / GOAL_ASPECT_RATIO;
				}
			}
		
			if (scaleHeightByAspectRatio)
			{
				// Determine the Y scale based on the aspect ratio of the screen.
				// This will always be a lower value, and only used when the aspect ratio
				// is less than 4:3.
				if (NGUIExt.aspectRatio < BASE_ASPECT_RATIO)
				{
					aspectRatioScaleY = NGUIExt.aspectRatio / GOAL_ASPECT_RATIO;
				}
			}
			
			transform.localScale = Vector3.Scale(transform.localScale, new Vector3(aspectRatioScaleX, aspectRatioScaleY, 1.0f));
		}
		
		// Don't destroy, so we can re-enable if resolution changes.
		enabled = false;
	}
}