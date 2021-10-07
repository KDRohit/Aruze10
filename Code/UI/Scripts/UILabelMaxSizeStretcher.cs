using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Stretches a UILabel's max width or height to match a sprite's size in a given direction.
*/

[ExecuteInEditMode]

public class UILabelMaxSizeStretcher : TICoroutineMonoBehaviour
{
	public enum Direction
	{
		WIDTH,
		HEIGHT,
		BOTH
	}
	
	public UILabel label;
	public UISprite targetSprite;
	public Direction direction;
	public Vector2 pixelOffset = Vector2.zero;
		
	void Update()
	{
		if (targetSprite == null || label == null)
		{
			return;
		}
				
		if (direction == Direction.WIDTH || direction == Direction.BOTH)
		{
			label.lineWidth = (int)(targetSprite.transform.localScale.x + pixelOffset.x);
		}

		if (direction == Direction.HEIGHT || direction == Direction.BOTH)
		{
			label.lineHeight = (int)(targetSprite.transform.localScale.y + pixelOffset.y);
		}
	}
}
