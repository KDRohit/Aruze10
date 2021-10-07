using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*

Adjusts the relative anchor toward the goal values based on how close to the target aspect ratio
the device is compared to iPad, which is treated as the basis for UI layout.
*/

public class AspectRatioRelativeAnchor : TICoroutineMonoBehaviour
{
	private const float IPAD_ASPECT = 1.33f;
	
	public enum Direction
	{
		HORIZONTAL,
		VERTICAL,
		BOTH
	}

	public Direction direction = Direction.BOTH;
	public UIAnchor anchor;
	public Vector2 goalRelativeOffset;
	public Vector2 goalPixelOffset;
	public float goalAspectRatio;
	
	private Vector2 originalRelativeOffset;
	private Vector2 originalPixelOffset;
	
	void Awake()
	{
		if (anchor == null)
		{
			anchor = gameObject.GetComponent<UIAnchor>();
		}
		
		if (anchor == null)
		{
			Debug.LogWarning("No UIAnchor found for AspectRatioRelativeAnchor on GameObject " + gameObject.name);
			enabled = false;
			return;
		}
		
		originalRelativeOffset = new Vector2(anchor.relativeOffset.x, anchor.relativeOffset.y);
		originalPixelOffset = new Vector2(anchor.pixelOffset.x, anchor.pixelOffset.y);
	}
	
	void Update()
	{
		if (goalAspectRatio < IPAD_ASPECT)
		{
			Debug.LogError("Come on, man. You can't specify a goal aspect ratio that's smaller than the baseline iPad aspect.");
		}

		float normalized = CommonMath.normalizedValue(IPAD_ASPECT, goalAspectRatio, NGUIExt.aspectRatio);
		
		float relativeX = originalRelativeOffset.x;
		float relativeY = originalRelativeOffset.y;
		float pixelX = originalPixelOffset.x;
		float pixelY = originalPixelOffset.y;
		
		if (direction == Direction.BOTH || direction == Direction.HORIZONTAL)
		{
			relativeX = Mathf.Lerp(relativeX, goalRelativeOffset.x, normalized);
			pixelX = Mathf.Lerp(pixelX, goalPixelOffset.x, normalized);
		}
		
		if (direction == Direction.BOTH || direction == Direction.VERTICAL)
		{
			relativeY = Mathf.Lerp(relativeY, goalRelativeOffset.y, normalized);
			pixelY = Mathf.Lerp(pixelY, goalPixelOffset.y, normalized);
		}
		
		if (direction != Direction.BOTH)
		{
			if (direction != Direction.HORIZONTAL)
			{
				// Use the current X values.
				relativeX = anchor.relativeOffset.x;
				pixelX = anchor.pixelOffset.x;
			}
			if (direction != Direction.VERTICAL)
			{
				// Use the current Y values.
				relativeY = anchor.relativeOffset.y;
				pixelY = anchor.pixelOffset.y;
			}
		}
		
		anchor.relativeOffset = new Vector2(relativeX, relativeY);
		anchor.pixelOffset = new Vector2(pixelX, pixelY);
		
		// Don't destroy, so we can re-enable if resolution changes.
		if (!anchor.enabled)
		{
			anchor.enabled = true;
			anchor.reposition();
		}
		enabled = false;
	}
}
