using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Adjusts the relative stretch toward the goal values based on how close to the target aspect ratio
the device is compared to iPad, which is treated as the basis for UI layout.
*/

public class AspectRatioRelativeStretch : TICoroutineMonoBehaviour
{
	private const float IPAD_ASPECT = 1.33f;
	
	public enum Direction
	{
		HORIZONTAL,
		VERTICAL,
		BOTH
	}

	public Direction direction = Direction.BOTH;
	public UIStretch stretch;
	public Vector2 goalRelativeSize = Vector2.one;
	public Vector2 goalPixelOffset;
	public float goalAspectRatio;
		
	private Vector2 originalRelativeSize;
	private Vector2 originalPixelOffset;

	void Awake()
	{
		if (stretch == null)
		{
			stretch = gameObject.GetComponent<UIStretch>();
		}
		
		if (stretch == null)
		{
			Debug.LogWarning("No UIStretch found for AspectRatioRelativeAnchor on GameObject " + gameObject.name);
			enabled = false;
			return;
		}

		originalRelativeSize = new Vector2(stretch.relativeSize.x, stretch.relativeSize.y);
		originalPixelOffset = new Vector2(stretch.pixelOffset.x, stretch.pixelOffset.y);
	}

	void Update()
	{
		if (goalAspectRatio < IPAD_ASPECT)
		{
			Debug.LogError("Come on, man. You can't specify a goal aspect ratio that's smaller than the baseline iPad aspect.");
		}

		float normalized = CommonMath.normalizedValue(IPAD_ASPECT, goalAspectRatio, NGUIExt.aspectRatio);
		
		float relativeX = originalRelativeSize.x;
		float relativeY = originalRelativeSize.y;
		float pixelX = originalPixelOffset.x;
		float pixelY = originalPixelOffset.y;
		
		if (direction == Direction.BOTH || direction == Direction.HORIZONTAL)
		{
			relativeX = Mathf.Lerp(relativeX, goalRelativeSize.x, normalized);
			pixelX = Mathf.Lerp(pixelX, goalPixelOffset.x, normalized);
		}
		
		if (direction == Direction.BOTH || direction == Direction.VERTICAL)
		{
			relativeY = Mathf.Lerp(relativeY, goalRelativeSize.y, normalized);
			pixelY = Mathf.Lerp(pixelY, goalPixelOffset.y, normalized);
		}
		
		if (direction != Direction.BOTH)
		{
			if (direction != Direction.HORIZONTAL)
			{
				// Use the current X values.
				relativeX = stretch.relativeSize.x;
				pixelX = stretch.pixelOffset.x;
			}
			if (direction != Direction.VERTICAL)
			{
				// Use the current Y values.
				relativeY = stretch.relativeSize.y;
				pixelY = stretch.pixelOffset.y;
			}
		}

		stretch.relativeSize = new Vector2(relativeX, relativeY);
		stretch.pixelOffset = new Vector2(pixelX, pixelY);
		
		// Don't destroy, so we can re-enable if resolution changes.
		enabled = false;
	}
}
