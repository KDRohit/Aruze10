//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public class GradientStep
{
	public GradientStep( float l, Color c )
	{
		location = l;
		color = c;
	}
	
	public GradientStep( float l, Color32 c )
	{
		location = l;
		color = c;
	}
	
	public float location;
	public Color color;
}

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Label")]
public class UILabel : UIWidget
{
	public enum Effect
	{
		None,
		Shadow,
		Outline,
	}
	
	public enum ColorMode
	{
		Solid,
		Gradient,
	}

	
	[HideInInspector][SerializeField] protected UIFont mFont;
	[HideInInspector][SerializeField] protected string mText = "";
	[HideInInspector][SerializeField] protected int mMaxLineWidth = 0;
	[HideInInspector][SerializeField] protected int mMaxLineHeight = 0;
	[HideInInspector][SerializeField] protected bool mEncoding = true;
	[HideInInspector][SerializeField] protected int mMaxLineCount = 0; // 0 denotes unlimited
	[HideInInspector][SerializeField] protected bool mPassword = false;
	[HideInInspector][SerializeField] protected bool mShowLastChar = false;
	[HideInInspector][SerializeField] protected Effect mEffectStyle = Effect.None;
	[HideInInspector][SerializeField] protected Color mEffectColor = Color.black;
	[HideInInspector][SerializeField] protected UIFont.SymbolStyle mSymbols = UIFont.SymbolStyle.Uncolored;
	[HideInInspector][SerializeField] protected Vector2 mEffectDistance = Vector2.one;
	[HideInInspector][SerializeField] protected bool mShrinkToFit = false;
	[HideInInspector][SerializeField] protected ColorMode mColorMode = ColorMode.Solid;
	[HideInInspector][SerializeField] protected Color mEndGradientColor = Color.white;
	[HideInInspector][SerializeField] protected List<GradientStep> mGradientSteps = new List<GradientStep>();
	[HideInInspector] protected List<GradientStep> mGradientStepOverrides = new List<GradientStep>(); 	// override used in place of regular gradient steps
	[HideInInspector] protected bool mIsUsingGradientOverride = false;									// flag to tell if the gradient override is being used
	[HideInInspector][SerializeField] protected float mGradientScale = 1.0f;
	[HideInInspector][SerializeField] protected float mGradientOffset = 0.0f;
	
	/// <summary>
	/// Obsolete, do not use. Use 'mMaxLineWidth' instead.
	/// </summary>

	[HideInInspector][SerializeField] protected float mLineWidth = 0;

	/// <summary>
	/// Obsolete, do not use. Use 'mMaxLineCount' instead
	/// </summary>

	[HideInInspector][SerializeField] protected bool mMultiline = true;

	/// <summary>
	/// Defines the max size of the label.
	/// </summary>
	[HideInInspector] public Vector2 maxSize;

	protected bool mShouldBeProcessed = true;
	protected string mProcessedText = null;
	protected Vector3 mLastScale = Vector3.one;
	protected Vector2 mSize = Vector2.zero;
	protected bool mPremultiply = false;

	/// <summary>
	/// Function used to determine if something has changed (and thus the geometry must be rebuilt)
	/// </summary>

	protected bool hasChanged
	{
		get
		{
			return mShouldBeProcessed;
		}
		set
		{
			if (value)
			{
				mChanged = true;
				mShouldBeProcessed = true;
			}
			else
			{
				mShouldBeProcessed = false;
			}
		}
	}

	/// <summary>
	/// Retrieve the material used by the font.
	/// </summary>

	public override Material material 
	{ 
		get 
		{ 
			mMatCached = !System.Object.ReferenceEquals(mFont,null) ? mFont.material : null; 
			return mMatCached;
		} 
	}


	/// <summary>
	/// Set the font used by this label.
	/// </summary>

	public UIFont font
	{
		get
		{
			return mFont;
		}
		set
		{
			if (mFont != value)
			{
#if DYNAMIC_FONT
				if (mFont != null && mFont.dynamicFont != null)
					mFont.dynamicFont.textureRebuildCallback -= MarkAsChanged;
#endif
				RemoveFromPanel();
				mFont = value;
				hasChanged = true;
				mMatCached = (!System.Object.ReferenceEquals(mFont,null)) ? mFont.material : null;
#if DYNAMIC_FONT
				if (mFont != null && mFont.dynamicFont != null)
				{
					mFont.dynamicFont.textureRebuildCallback += MarkAsChanged;
					mFont.Request(mText);
				}
#endif
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Text that's being displayed by the label.
	/// </summary>

	public string text
	{
		get
		{
			return mText;
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				if (!string.IsNullOrEmpty(mText)) mText = "";
				hasChanged = true;
			}
			else if (mText != value)
			{
				mText = value;
				hasChanged = true;
#if DYNAMIC_FONT
				if (mFont != null) mFont.Request(value);
#endif
				if (shrinkToFit) MakePixelPerfect();
			}
		}
	}

	/// <summary>
	/// Whether this label will support color encoding in the format of [RRGGBB] and new line in the form of a "\\n" string.
	/// </summary>

	public bool supportEncoding
	{
		get
		{
			return mEncoding;
		}
		set
		{
			if (mEncoding != value)
			{
				mEncoding = value;
				hasChanged = true;
				if (value) mPassword = false;
			}
		}
	}

	/// <summary>
	/// Style used for symbols.
	/// </summary>

	public UIFont.SymbolStyle symbolStyle
	{
		get
		{
			return mSymbols;
		}
		set
		{
			if (mSymbols != value)
			{
				mSymbols = value;
				hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Maximum width of the label in pixels.
	/// </summary>

	public int lineWidth
	{
		get
		{
			return mMaxLineWidth;
		}
		set
		{
			if (mMaxLineWidth != value)
			{
				mMaxLineWidth = value;
				hasChanged = true;
				if (shrinkToFit) MakePixelPerfect();
			}
		}
	}

	/// <summary>
	/// Maximum height of the label in pixels.
	/// </summary>

	public int lineHeight
	{
		get
		{
			return mMaxLineHeight;
		}
		set
		{
			if (mMaxLineHeight != value)
			{
				mMaxLineHeight = value;
				hasChanged = true;
				if (shrinkToFit) MakePixelPerfect();
			}
		}
	}

	/// <summary>
	/// Whether the label supports multiple lines.
	/// </summary>
	
	public bool multiLine
	{
		get
		{
			return mMaxLineCount != 1;
		}
		set
		{
			if ((mMaxLineCount != 1) != value)
			{
				mMaxLineCount = (value ? 0 : 1);
				hasChanged = true;
				if (value) mPassword = false;
			}
		}
	}

	/// <summary>
	/// The max number of lines to be displayed for the label
	/// </summary>

	public int maxLineCount
	{
		get
		{
			return mMaxLineCount;
		}
		set
		{
			if (mMaxLineCount != value)
			{
				mMaxLineCount = Mathf.Max(value, 0);
				if (value != 1) mPassword = false;
				hasChanged = true;
				if (shrinkToFit) MakePixelPerfect();
			}
		}
	}

	/// Zynga - Todd
	/// <summary>
	/// Line spacing for this label
	/// </summary>

	public int lineSpacing
	{
		get
		{
			return mLineSpacing;
		}
		set
		{
			if (mLineSpacing != value)
			{
				mLineSpacing = value;
				hasChanged = true;
				if (shrinkToFit) MakePixelPerfect();
			}
		}
	}
	[HideInInspector][SerializeField] protected int mLineSpacing = 0;
	/// End Zynga

	/// <summary>
	/// Whether the label's contents should be hidden
	/// </summary>

	public bool password
	{
		get
		{
			return mPassword;
		}
		set
		{
			if (mPassword != value)
			{
				if (value)
				{
					mMaxLineCount = 1;
					mEncoding = false;
				}
				mPassword = value;
				hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Whether the last character of a password field will be shown
	/// </summary>

	public bool showLastPasswordChar
	{
		get
		{
			return mShowLastChar;
		}
		set
		{
			if (mShowLastChar != value)
			{
				mShowLastChar = value;
				hasChanged = true;
			}
		}
	}

	/// <summary>
	/// What effect is used by the label.
	/// </summary>

	public Effect effectStyle
	{
		get
		{
			return mEffectStyle;
		}
		set
		{
			if (mEffectStyle != value)
			{
				mEffectStyle = value;
				hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Color used by the effect, if it's enabled.
	/// </summary>

	public Color effectColor
	{
		get
		{
			return mEffectColor;
		}
		set
		{
			if (!mEffectColor.Equals(value))
			{
				mEffectColor = value;
				if (mEffectStyle != Effect.None) hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Effect distance in pixels.
	/// </summary>

	public Vector2 effectDistance
	{
		get
		{
			return mEffectDistance;
		}
		set
		{
			if (mEffectDistance != value)
			{
				mEffectDistance = value;
				hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Whether the label will automatically shrink its size in order to fit the maximum line width.
	/// </summary>

	public bool shrinkToFit
	{
		get
		{
			return mShrinkToFit;
		}
		set
		{
			if (mShrinkToFit != value)
			{
				mShrinkToFit = value;
				hasChanged = true;
			}
		}
	}

	public ColorMode colorMode
	{
		get
		{
			return mColorMode;
		}
		set
		{
			if( mColorMode != value )
			{
				mColorMode = value;
				hasChanged = true;
			}
		}
	}

	public Color endGradientColor
	{
		get
		{
			return mEndGradientColor;
		}
		set
		{
			if( mEndGradientColor != value )
			{
				mEndGradientColor = value;
				hasChanged = true;
			}
		}
	}

	public List<GradientStep> gradientSteps
	{
		get
		{
			return mGradientSteps;
		}
		set
		{
			mGradientSteps = value;
			hasChanged = true;
		}
	}
	
	public float gradientScale
	{
		get
		{
			return mGradientScale;
		}
		set
		{
			if( mGradientScale != value )
			{
				mGradientScale = value;
				hasChanged = true;
			}
		}
	}
	
	public float gradientOffset
	{
		get
		{
			return mGradientOffset;
		}
		set
		{
			if( mGradientOffset != value )
			{
				mGradientOffset = value;
				hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Returns the processed version of 'text', with new line characters, line wrapping, etc.
	/// </summary>

	public string processedText
	{
		get
		{
			if (mLastScale != cachedTransform.localScale)
			{
				mLastScale = cachedTransform.localScale;
				mShouldBeProcessed = true;
			}

			// Process the text if necessary
			if (hasChanged) ProcessText();
			return mProcessedText;
		}
	}

	/// <summary>
	/// Visible size of the widget in local coordinates.
	/// </summary>

	public override Vector2 relativeSize
	{
		get
		{
			if (mFont == null) return Vector3.one;
			if (hasChanged) ProcessText();
			return mSize;
		}
	}

#if DYNAMIC_FONT
	/// <summary>
	/// Register the font texture change listener.
	/// </summary>

	protected override void OnEnable ()
	{
		if (mFont != null && mFont.dynamicFont != null)
			mFont.dynamicFont.textureRebuildCallback += MarkAsChanged;
		base.OnEnable();
	}

	/// <summary>
	/// Remove the font texture change listener.
	/// </summary>

	protected override void OnDisable ()
	{
		if (mFont != null && mFont.dynamicFont != null)
			mFont.dynamicFont.textureRebuildCallback -= MarkAsChanged;
		base.OnDisable();
	}
#endif

	/// <summary>
	/// Determine start-up values.
	/// </summary>

	protected override void OnStart ()
	{
		// Legacy support
		if (mLineWidth > 0f)
		{
			mMaxLineWidth = Mathf.RoundToInt(mLineWidth);
			mLineWidth = 0f;
		}

		if (!mMultiline)
		{
			mMaxLineCount = 1;
			mMultiline = true;
		}

		// Whether this is a premultiplied alpha shader
		mPremultiply = (font != null && font.material != null && font.material.shader.name.Contains("Premultiplied"));

#if DYNAMIC_FONT
		// Request the text within the font
		if (mFont != null) mFont.Request(mText);
#endif
	}

#if UNITY_EDITOR
	/// <summary>
	/// Labels are not resizable using the handles.
	/// </summary>

	public override bool showResizeHandles { get { return false; } }
#endif

	/// <summary>
	/// UILabel needs additional processing when something changes.
	/// </summary>

	public override void MarkAsChanged ()
	{
		hasChanged = true;
		base.MarkAsChanged();
	}

	/// <summary>
	/// Process the raw text, called when something changes.
	/// </summary>

	void ProcessText ()
	{
		mChanged = true;
		hasChanged = false;

		float scale = Mathf.Abs(cachedTransform.localScale.x);

		if (scale > 0f)
		{
			for (;;)
			{
				bool fits = true;

				if (mPassword)
				{
					mProcessedText = "";

					if (mShowLastChar)
					{
						for (int i = 0, imax = mText.Length - 1; i < imax; ++i)
							mProcessedText += "*";
						if (mText.Length > 0)
							mProcessedText += mText[mText.Length - 1];
					}
					else
					{
						for (int i = 0, imax = mText.Length; i < imax; ++i)
							mProcessedText += "*";
					}
					//Since the scale, might not be the same as the font size (in the case for shrinkToFit), determine the y scale based on the percentage the font is scale, and the line spacing.
					float scaleY = scale + lineSpacing * ((float)scale / font.size);
					fits = mFont.WrapText(mProcessedText, out mProcessedText, mMaxLineWidth / scale, mMaxLineHeight / scaleY,
						mMaxLineCount, false, UIFont.SymbolStyle.None);
				}
				else if (mMaxLineWidth > 0 || mMaxLineHeight > 0)
				{
				//Since the scale, might not be the same as the font size (in the case for shrinkToFit), determine the y scale based on the percentage the font is scale, and the line spacing.
					float scaleY = scale + lineSpacing * ((float)scale / font.size);
					fits = mFont.WrapText(mText, out mProcessedText, mMaxLineWidth / scale, mMaxLineHeight / scaleY,
						mMaxLineCount, mEncoding, mSymbols);
				}
				else mProcessedText = mText;

				// Zynga - Todd added mLineSpacing argument.
				mSize = !string.IsNullOrEmpty(mProcessedText) ? mFont.CalculatePrintedSize(mProcessedText, mEncoding, mSymbols, mLineSpacing) : Vector2.one;

				if (mShrinkToFit)
				{
					// We want to shrink the label (when it doesn't fit)
					if (!fits)
					{
						scale = Mathf.Round(scale - 1f);
						if (scale > 1f) continue;
					}


					// Removed by Zynga - Mike Wood (Telos) - Replaced with more accurate calculation below.
					/*if (mMaxLineWidth > 0)
					{
						float maxX = (float)mMaxLineWidth / scale;
						float x = (mSize.x * scale > maxX) ? (maxX / mSize.x) * scale : scale;
						scale = Mathf.Min(x, scale);
					}*/
					// End Zynga

					// Added by Zynga -  Mike Wood (Telos)
					// if we have a max width specified, and our formated text will exceed the bounds, calculate the scale required to make it fit.
					if (mMaxLineWidth > 0 && mMaxLineWidth < mSize.x * scale)
					{
						scale = Mathf.Min(scale, (float)mMaxLineWidth / (mSize.x));
					}
					
					// if we have a max height specified, and our formated text will exceed the bounds, calculate the scale required to make it fit.
					if (mMaxLineHeight > 0 && mMaxLineHeight < mSize.y * scale)
					{
						scale = Mathf.Min(scale, (float)(mMaxLineHeight - lineSpacing) / (mSize.y));
					}
					// End Zynga

					scale = Mathf.Round(scale);
					cachedTransform.localScale = new Vector3(scale, scale, 1f);
				}
				break;
			}
			// Added by Zynga - Mike Wood (Telos) - set the max size, instead of the actual, since the actual can be smaller base on wrapping and skrinking.
			maxSize.x = Mathf.Max(mSize.x, (scale > 0f) ? mMaxLineWidth / scale : 1f);
			maxSize.y = Mathf.Max(mSize.y, (scale > 0f) ? mMaxLineHeight / scale : 1f);
			// End Zynga
		}
		else
		{
			// This should never happen (label should never have a scale of 0) -- but just in case.
			mSize.x = 1f;
			mSize.y = 1f;
			scale = mFont.size;

			cachedTransform.localScale = new Vector3(scale, scale, 1f);
			mProcessedText = "";
		}
	}

	/// <summary>
	/// Text is pixel-perfect when its scale matches the size.
	/// </summary>

	public override void MakePixelPerfect ()
	{
		if (mFont != null)
		{
			float pixelSize = font.pixelSize;
			Vector3 scale = cachedTransform.localScale;
			scale.x = mFont.size * pixelSize;
			scale.y = scale.x;
			scale.z = 1f;
			Vector3 pos = cachedTransform.localPosition;
			pos.x = (Mathf.CeilToInt(pos.x / pixelSize * 4f) >> 2);
			pos.y = (Mathf.CeilToInt(pos.y / pixelSize * 4f) >> 2);
			pos.z = Mathf.RoundToInt(pos.z);

			pos.x *= pixelSize;
			pos.y *= pixelSize;

			cachedTransform.localPosition = pos;
			cachedTransform.localScale = scale;
			
			if (shrinkToFit) ProcessText();
		}
		else base.MakePixelPerfect();
	}

	/// <summary>
	/// Apply a shadow effect to the buffer.
	/// </summary>

	protected void ApplyShadow (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols, int start, int end, float x, float y)
	{
		Color c = mEffectColor;
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
	/// Turn gradient override on and set the override color
	/// </summary>

	public void UseGradientOverride(Color color)
	{
		if (mColorMode == ColorMode.Gradient)
		{
			mIsUsingGradientOverride = true;

			mGradientStepOverrides.Clear();

			for (int i = 0; i < mGradientSteps.Count; ++i)
			{
				// Apply a monochromatic effect (of any color, not just black and white) to the gradient to simulate the color change
				GradientStep step = mGradientSteps[i];
				float monoValue = (step.color.r + step.color.g + step.color.b) / 3.0f;
				Color newColor = new Color(color.r * monoValue, color.g * monoValue, color.b * monoValue, color.a);

				mGradientStepOverrides.Add(new GradientStep(step.location, newColor));
			}

			mChanged = true;
		}
		else
		{
			Debug.LogWarning("Trying to UseGradientOverride() on a UILabel which isn't set to use gradients!");
		}
	}

	/// <summary>
	/// Turn gradient override off and refresh the label
	/// </summary>

	public void DisableGradientOverride()
	{
		mIsUsingGradientOverride = false;
		mChanged = true;
	}

	/// <summary>
	/// Draw the label.
	/// </summary>

	public override void OnFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
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
		switch( mColorMode )
		{
		case ColorMode.Solid:
			mFont.Print(processedText, col, verts, uvs, cols, mEncoding, mSymbols, alignment, lWidth, mPremultiply, mLineSpacing);
			break;
		case ColorMode.Gradient:
			if(mIsUsingGradientOverride)
			{
				mFont.PrintGradient(processedText, col, mEndGradientColor, mGradientStepOverrides, mGradientScale, mGradientOffset, verts, uvs, cols, mEncoding, mSymbols, alignment, lWidth, mPremultiply, mLineSpacing);
			}
			else
			{
				mFont.PrintGradient(processedText, col, mEndGradientColor, mGradientSteps, mGradientScale, mGradientOffset, verts, uvs, cols, mEncoding, mSymbols, alignment, lWidth, mPremultiply, mLineSpacing);
			}
			break;
		}

		// Apply an effect if one was requested
		if (effectStyle != Effect.None)
		{
			int end = verts.size;
			float pixel = 1f / (mFont.size * mFont.pixelSize);

			float fx = pixel * mEffectDistance.x;
			float fy = pixel * mEffectDistance.y;

			ApplyShadow(verts, uvs, cols, offset, end, fx, -fy);

			if (effectStyle == Effect.Outline)
			{
				offset = end;
				end = verts.size;

				ApplyShadow(verts, uvs, cols, offset, end, -fx, fy);

				offset = end;
				end = verts.size;

				ApplyShadow(verts, uvs, cols, offset, end, fx, fy);

				offset = end;
				end = verts.size;

				ApplyShadow(verts, uvs, cols, offset, end, -fx, -fy);
			}
		}
	}
}
