using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Stretches a UISprite relative to a given TextMeshPro label.
*/

[ExecuteInEditMode]
public class UIStretchTextMeshPro : TICoroutineMonoBehaviour
{
	public enum Direction
	{
		WIDTH,
		HEIGHT,
		BOTH
	}

	public UISprite sprite;
	public TextMeshPro label;
	public List<TextMeshPro> labelsToCompare;
	public Direction direction = Direction.BOTH;
	public Vector2 pixelOffset = new Vector2(130, 130);
	public Vector2 maxSize = Vector2.zero;  // If non-zero in each direction, will limit the sprite size to this.
	public Vector2 minSize = Vector2.zero;
	[Tooltip("Anchors listed here will be updated when the text updates")]
	public List<UIAnchor> dependentAnchors;
	[Tooltip("List of UIStretch components that will be updated when the text updates")]
	[SerializeField] private UIStretch[] dependentStretches;

	private TextMeshPro largestLabel;
	
	private string lastText = "";
	private Vector2 lastpixelOffset = Vector2.zero;
	private Vector2 lastMaxSize = Vector2.zero;
	public Vector2 lastMinSize = Vector2.zero;
	public Vector2 relativeSize = Vector2.one;


	private string labelText
	{
		get
		{
			if (largestLabel != null)
			{
				return largestLabel.text;
			}
			return "";
		}
	}
	
	void Awake()
	{
		largestLabel = label;
		if (Application.isPlaying && sprite == null)
		{
			Debug.LogWarning("LabelShadow script has no associated UISprite.", gameObject);
			enabled = false;
			return;
		}

		if (dependentAnchors != null && dependentAnchors.Count > 0 && sprite.panel != null)
		{
			sprite.panel.onChange += updateDependentAnchors;
		}
	}

	void Update()
	{
		if (largestLabel != null && sprite != null)
		{
			if (lastText != labelText ||
				lastpixelOffset.x != pixelOffset.x ||
				lastpixelOffset.y != pixelOffset.y ||
				lastMaxSize.x != maxSize.x ||
				lastMaxSize.y != maxSize.y ||
				lastMinSize.x != minSize.x ||
				lastMinSize.y != minSize.y
				)
			{
				refresh();
			}
		}
	}

	public void refresh()
	{
		sprite.enabled = (labelText != "");

		Vector2 size = Vector2.zero;
		
		largestLabel = label;
		
		if (largestLabel != null)
		{
			largestLabel.ForceMeshUpdate(); // Force the bounds to be updated immediately after text changes.
			size = new Vector2(largestLabel.bounds.size.x, largestLabel.bounds.size.y);
		}
		
		// Only works for width
		for (int i = 0; i < labelsToCompare.Count; i++)
		{
			labelsToCompare[i].ForceMeshUpdate();
			if (largestLabel != null)
			{
				if (labelsToCompare[i].bounds.size.x > largestLabel.bounds.size.x)
				{
					largestLabel = labelsToCompare[i];
					size = new Vector2(largestLabel.bounds.size.x, largestLabel.bounds.size.y);
				}
			}
			else
			{
				largestLabel = labelsToCompare[i];
			}
		}

		size.x += pixelOffset.x;
		size.y += pixelOffset.y;
		
		if (maxSize.x > 0.0f)
		{
			size.x = Mathf.Min(size.x, maxSize.x);
		}
		if (minSize.x > 0.0f)
		{
			size.x = Mathf.Max(size.x, minSize.x);
		}
		if (maxSize.y > 0.0f)
		{
			size.y = Mathf.Min(size.y, maxSize.y);
		}

		if (minSize.y > 0.0f)
		{
			size.y = Mathf.Min(size.y, minSize.y);
		}
		
		if (direction == Direction.BOTH || direction == Direction.WIDTH)
		{
			CommonTransform.setWidth(sprite.transform, size.x * relativeSize.x);
		}

		if (direction == Direction.BOTH || direction == Direction.HEIGHT)
		{
			CommonTransform.setHeight(sprite.transform, size.y * relativeSize.y);
		}
		
		lastText = labelText;
		lastpixelOffset.x = pixelOffset.x;
		lastpixelOffset.y = pixelOffset.y;
		lastMaxSize.x = maxSize.x;
		lastMaxSize.y = maxSize.y;
		lastMinSize.x = minSize.x;
		lastMinSize.y = minSize.y;

		if (dependentStretches != null)
		{
			for (int i = 0; i < dependentStretches.Length; i++)
			{
				dependentStretches[i].enabled = true;
			}
		}
	}

	private void updateDependentAnchors()
	{
		for (int i = 0; i < dependentAnchors.Count; i++)
		{
			if (dependentAnchors[i] != null)
			{
				dependentAnchors[i].enabled = true;
			}
		}
	}

	private void OnDestroy()
	{
		if (sprite != null && sprite.panel != null)
		{
			sprite.panel.onChange -= updateDependentAnchors;
		}
	}
}
