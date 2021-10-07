using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Attach to an object with a UILabel to allow you to select a common style and have the label styled automatically.
*/

[ExecuteInEditMode]
public class UILabelMultipleEffectsStyler : TICoroutineMonoBehaviour
{
	public UILabelMultipleEffectsStyle style;
	public UILabelMultipleEffects label;

#if UNITY_EDITOR
	protected UILabelMultipleEffectsStyle styleLastFrame;
#endif

	void Awake()
	{
		if (label == null)
		{
			label = gameObject.GetComponent<UILabelMultipleEffects>();
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

	public void updateStyle(UILabelMultipleEffectsStyle style = null)
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

		if (label == null || style == null)
		{
			return;
		}

		// Apply the style to the label.
		label.colorMode = style.colorMode;
		label.gradientSteps = new List<GradientStep>(style.gradientSteps);
		label.endGradientColor = style.endGradientColor;
		label.color = style.color;
		label.effectStyle = style.effect;
		label.effectDistance = new Vector2(style.effectDistanceX, style.effectDistanceY);
		label.effectColor = style.effectColor;
		label.effectStyle2 = style.effect2;
		label.effectDistance2 = new Vector2(style.effectDistanceX2, style.effectDistanceY2);
		label.effectColor2 = style.effectColor2;
		label.font = style.font;
		label.MakePixelPerfect();

#if UNITY_EDITOR
		styleLastFrame = style;
#endif
	}
}
