using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Stretches a TextMeshPro component's max width or height to match a sprite's size in a given direction.
*/

[ExecuteInEditMode]

public class TextMeshProMaxSizeStretcher : MonoBehaviour
{
	public enum Direction
	{
		WIDTH,
		HEIGHT,
		BOTH
	}
	
	public TextMeshPro label;
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
			// TODO:UNITY2018:obsoleteTextContainer:confirm
			label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (targetSprite.transform.localScale.x + pixelOffset.x));
		}

		if (direction == Direction.HEIGHT || direction == Direction.BOTH)
		{
			// TODO:UNITY2018:obsoleteTextContainer:confirm
			label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (targetSprite.transform.localScale.y + pixelOffset.y));
		}
	}
}
