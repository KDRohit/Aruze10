//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Label With Multiple Effects")]
public class UILabelMultipleEffects : UILabel
{
	[HideInInspector][SerializeField] Effect mEffectStyle2 = Effect.None;
	[HideInInspector][SerializeField] Color mEffectColor2 = Color.black;
	[HideInInspector][SerializeField] Vector2 mEffectDistance2 = Vector2.one;

	/// <summary>
	/// What effect is used by the label.
	/// </summary>
	public Effect effectStyle2
	{
		get { return mEffectStyle2; }
		set
		{
			if (mEffectStyle2 != value)
			{
				mEffectStyle2 = value;
				hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Color used by the effect, if it's enabled.
	/// </summary>
	public Color effectColor2
	{
		get { return mEffectColor2; }
		set
		{
			if (!mEffectColor2.Equals(value))
			{
				mEffectColor2 = value;
				if (mEffectStyle2 != Effect.None) hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Effect distance in pixels.
	/// </summary>
	public Vector2 effectDistance2
	{
		get { return mEffectDistance2; }
		set
		{
			if (mEffectDistance2 != value)
			{
				mEffectDistance2 = value;
				hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Apply a shadow effect to the buffer.
	/// </summary>

	protected void ApplyShadow(BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols, int start, int end, float x, float y, Color color)
	{
		Color c = color;
		c.a *= alpha * mPanel.alpha;
		Color32 col = (font.premultipliedAlpha) ? NGUITools.ApplyPMA(c) : c;

		for (int i = start; i < end; ++i)
		{
			verts.Add(verts.buffer[i]);
			uvs.Add(uvs.buffer[i]);
			cols.Add(cols.buffer[i]);

			Vector3 v = verts.buffer[i];
			v.x += x;
			v.y += y;
			verts.buffer[i] = v;
			cols.buffer[i] = col;
		}
	}

	/// <summary>
	/// Draw the label.
	/// </summary>

	public override void OnFill(BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		if (mFont == null) return;
		Pivot p = pivot;
		int offset = verts.size;

		Color col = color;
		col.a *= mPanel.alpha;
		if (font.premultipliedAlpha) col = NGUITools.ApplyPMA(col);

		UIFont.Alignment alignment = UIFont.Alignment.Left;
		int lWidth = 0;

		// Print the text into the buffers
		if (p == Pivot.Left || p == Pivot.TopLeft || p == Pivot.BottomLeft)
		{
			alignment = UIFont.Alignment.Left;
			lWidth = 0;
		}
		else if (p == Pivot.Right || p == Pivot.TopRight || p == Pivot.BottomRight)
		{
			alignment = UIFont.Alignment.Right;
			lWidth = Mathf.RoundToInt(relativeSize.x * mFont.size);
		}
		else
		{
			alignment = UIFont.Alignment.Center;
			lWidth = Mathf.RoundToInt(relativeSize.x * mFont.size);
		}

		// print based on color mode
		switch (mColorMode)
		{
			case ColorMode.Solid:
				mFont.Print(processedText, col, verts, uvs, cols, mEncoding, mSymbols, alignment, lWidth, mPremultiply, mLineSpacing);
				break;
			case ColorMode.Gradient:
				mFont.PrintGradient(processedText, col, mEndGradientColor, mGradientSteps, mGradientScale, mGradientOffset, verts, uvs, cols, mEncoding, mSymbols, alignment, lWidth, mPremultiply, mLineSpacing);
				break;
		}

		ApplyEffect(effectStyle, effectDistance, effectColor, verts, uvs, cols, offset);
		ApplyEffect(effectStyle2, effectDistance2, effectColor2, verts, uvs, cols, offset);
	}

	private void ApplyEffect(Effect effectStyle, Vector2 effectDistance, Color effectColor, BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols, int offset)
	{
		// Apply an effect if one was requested
		if (effectStyle != Effect.None)
		{
			int end = verts.size;
			float pixel = 1f / (mFont.size * mFont.pixelSize);

			float fx = pixel * effectDistance.x;
			float fy = pixel * effectDistance.y;

			ApplyShadow(verts, uvs, cols, offset, end, fx, -fy, effectColor);

			if (effectStyle == Effect.Outline)
			{
				offset = end;
				end = verts.size;

				ApplyShadow(verts, uvs, cols, offset, end, -fx, fy, effectColor);

				offset = end;
				end = verts.size;

				ApplyShadow(verts, uvs, cols, offset, end, fx, fy, effectColor);

				offset = end;
				end = verts.size;

				ApplyShadow(verts, uvs, cols, offset, end, -fx, -fy, effectColor);
			}
		}
	}
}
