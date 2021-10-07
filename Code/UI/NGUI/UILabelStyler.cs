using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Attach to an object with a UILabel to allow you to select a common style and have the label styled automatically.
THIS WILL BE OBSOLETE when we are completely switched to TextMeshPro.
Instead, use style materials for TextMeshPro.
*/

[ExecuteInEditMode]
public class UILabelStyler : TICoroutineMonoBehaviour
{
	public UILabelStyle style;
	public UILabel label;	// To be removed when prefabs are updated.
	public LabelWrapperComponent labelWrapperComponent;

	public LabelWrapper labelWrapper
	{
		get
		{
			if (_labelWrapper == null)
			{
				if (labelWrapperComponent != null)
				{
					_labelWrapper = labelWrapperComponent.labelWrapper;
				}
				else
				{
					_labelWrapper = new LabelWrapper(label);
				}
			}
			return _labelWrapper;
		}
	}
	private LabelWrapper _labelWrapper = null;
	
	public bool dimIfNotFacebook = false;	// If not logged into facebook, should this label be dimmed?

#if UNITY_EDITOR
	protected UILabelStyle styleLastFrame;
#endif

	void Awake()
	{
		if (label == null)
		{
			label = gameObject.GetComponent<UILabel>();
		}

		updateStyle();
	}

#if UNITY_EDITOR
	void Update()
	{
		// Added styleLastFrame check because Update was still executing every frame in which something changed
		// in the editor, so if the code modified the color of text for say fading from white to blue and back,
		// the UILabelStyler would instantly reset the color back to the style default color while in the editor.
		// This way the developer may swap styles at runtime while in the editor and see the updated style in
		// place instead of having to restart without interfereing with normal runtime color swapping.
		if (label == null || style == null || (style == styleLastFrame && Application.isPlaying))
		{
			return;
		}

		updateStyle();
	}
#endif
	
	public void updateStyle(UILabelStyle style = null)
	{
		if (style == null)
		{
			style = this.style;
		}
		else if (style == this.style)
		{
			return;
		}
		else
		{
			this.style = style;
		}

		if (labelWrapper == null || style == null)
		{
			return;
		}
		
		// It's common that the alpha will be animated for fading effects,
		// so store the alpha temporarily and restore it after setting the color.
		// This really only matters in the editor at design time, since this script
		// doesn't constantly update the style at runtime.
		// Necessary for animating alpha values using the Animator tool.
		float alpha = labelWrapper.alpha;
		
		// Apply the style to the label.
		labelWrapper.isGradient = (style.colorMode == UILabel.ColorMode.Gradient);
		labelWrapper.endGradientColor = style.endGradientColor;
		labelWrapper.color = style.color;
		labelWrapper.effectStyle = style.effect.ToString();
		labelWrapper.shadowOffset = new Vector2(style.effectDistanceX, style.effectDistanceY);
		labelWrapper.outlineWidth = style.effectDistanceX;
		labelWrapper.effectColor = style.effectColor;

		if (!Application.isPlaying)
		{
			labelWrapper.alpha = alpha;
		}
		else if (dimIfNotFacebook && !SlotsPlayer.isFacebookUser)
		{
			float gray = Color.gray.r;
			labelWrapper.color = new Color(style.color.r * gray, style.color.g * gray, style.color.b * gray, alpha * 0.75f);
		}

#if UNITY_EDITOR
		styleLastFrame = style;
#endif
	}
}

