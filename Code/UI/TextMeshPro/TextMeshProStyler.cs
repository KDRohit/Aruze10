using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Attach to an object with a TextMeshPro to allow you to select a common gradient style and have the label styled automatically.
*/

[ExecuteInEditMode]
public class TextMeshProStyler : TICoroutineMonoBehaviour
{
	public GradientStepStyle style;
	public TextMeshPro label;

#if UNITY_EDITOR
	protected GradientStepStyle styleLastFrame;
#endif

	void Awake()
	{
		if (label == null)
		{
			label = gameObject.GetComponent<TextMeshPro>();
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
	
	public void updateStyle(GradientStepStyle style = null)
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
		
		// TODO: Apply style.gradientSteps to the TextMeshPro object when it supports it.

#if UNITY_EDITOR
		styleLastFrame = style;
#endif
	}
}
