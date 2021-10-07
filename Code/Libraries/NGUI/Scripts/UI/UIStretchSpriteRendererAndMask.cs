using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Allows sprite renderer objects to be used in conjunction with dynamic-stretchable NGUI sprites.
/// Can be used with SpriteMasks to create dynamic-stretchable masks for things like in-game meters
/// </summary>
[ExecuteInEditMode]
public class UIStretchSpriteRendererAndMask : MonoBehaviour
{
	public enum StretchOrientation
	{
		None,
		Horizontal,
		Vertical,
		Both,
	}

	[SerializeField] private Transform mask;
	[SerializeField] private SpriteRenderer spriteRenderer;
	[SerializeField] private Transform stretchWithSprite;
	[SerializeField] private StretchOrientation orientation = StretchOrientation.None;

	private float sizeX;
	private float sizeY;

	private Vector2 minimumSpriteSize;
	private float minX;
	private float minY;

	private void OnEnable()
	{
		sizeX = spriteRenderer.size.x;
		sizeY = spriteRenderer.size.y;

		GetMinimumSpriteSize();
	}
	
	private void Update ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			//run this in editor as well so we can see our updates in real time
			GetMinimumSpriteSize();
		}
#endif

		//The minimum values for the spriteRenderer size (left + right, top + bottom)
		minX = minimumSpriteSize.x / mask.localScale.x;
		minY = minimumSpriteSize.y / mask.localScale.y;

		if (orientation == StretchOrientation.None)
		{
			return;
		}

		switch (orientation)
		{
			case StretchOrientation.Horizontal:
				sizeX = stretchWithSprite.localScale.x / mask.localScale.x;

				if (sizeX < minX)
				{
					sizeX = minX;
				}

				break;

			case StretchOrientation.Vertical:
				sizeY = stretchWithSprite.localScale.y / mask.localScale.y;

				if (sizeY < minY)
				{
					sizeY = minY;
				}

				break;

			case StretchOrientation.Both:
				sizeX = stretchWithSprite.localScale.x / mask.localScale.x;
				sizeY = stretchWithSprite.localScale.y / mask.localScale.y;

				if (sizeX < minX)
				{
					sizeX = minX;
				}

				if (sizeY < minY)
				{
					sizeY = minY;
				}

				break;
		}

		spriteRenderer.size = new Vector2(sizeX, sizeY);
	}

	private void GetMinimumSpriteSize()
	{
		//X=left, Y=bottom, Z=right, W=top.
		float left = spriteRenderer.sprite.border.x;
		float right = spriteRenderer.sprite.border.z;
		float top = spriteRenderer.sprite.border.y;
		float bottom = spriteRenderer.sprite.border.w;

		float spriteWidth = left + right;
		float spriteHeight = top + bottom;

		minimumSpriteSize = new Vector2(spriteWidth, spriteHeight);
	}

}
