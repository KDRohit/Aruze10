using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
This namespace/class holds extension methods for TextMeshPro class.
In order to use these, you must add "using TMProExtensions;" to the top of the class that uses them.
For functions that are not extension methods, see TMProFunctions.cs.
*/

namespace TMProExtensions
{
	public static class TMProExtensionFunctions
	{
		public static Color getFaceColor(this TextMeshPro tmPro)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.getFaceColor() - fontSharedMaterial is null.");
				return Color.white;
			}
			return tmPro.fontSharedMaterial.GetColor(ShaderUtilities.ID_FaceColor);
		}

		public static void enableUnderlay(this TextMeshPro tmPro, bool value)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.enableUnderlay() - fontSharedMaterial is null.");
				return;
			}
			if (value)
			{
				tmPro.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
			}
			else
			{
				tmPro.fontSharedMaterial.DisableKeyword("UNDERLAY_ON");
			}
			tmPro.UpdateMeshPadding();
		}
		
		public static bool isUnderlayEnabled(this TextMeshPro tmPro)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.isUnderlayEnabled() - fontSharedMaterial is null.");
				return false;
			}
			return tmPro.fontSharedMaterial.IsKeywordEnabled("UNDERLAY_ON");
		}
		
		public static void setUnderlayTypeInner(this TextMeshPro tmPro)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.setUnderlayTypeInner() - fontSharedMaterial is null.");
				return;
			}
			tmPro.fontSharedMaterial.EnableKeyword("UNDERLAY_INNER");
			tmPro.UpdateMeshPadding();
		}
		
		public static void setUnderlayTypeNormal(this TextMeshPro tmPro)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.setUnderlayTypeNormal() - fontSharedMaterial is null.");
				return;
			}
			tmPro.fontSharedMaterial.DisableKeyword("UNDERLAY_INNER");
			tmPro.UpdateMeshPadding();
		}
		
		public static void setUnderlayColor(this TextMeshPro tmPro, Color value)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.setUnderlayColor() - fontSharedMaterial is null.");
				return;
			}
			tmPro.fontSharedMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, value);
		}

		public static Color getUnderlayColor(this TextMeshPro tmPro)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.getUnderlayColor() - fontSharedMaterial is null.");
				return Color.white;
			}
			return tmPro.fontSharedMaterial.GetColor(ShaderUtilities.ID_UnderlayColor);
		}

		public static void setUnderlayOffset(this TextMeshPro tmPro, Vector2 value)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.setUnderlayOffset() - fontSharedMaterial is null.");
				return;
			}
			tmPro.fontSharedMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, value.x);
			tmPro.fontSharedMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, value.y);
			tmPro.UpdateMeshPadding();
		}

		public static Vector2 getUnderlayOffset(this TextMeshPro tmPro)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.getUnderlayOffset() - fontSharedMaterial is null.");
				return Vector2.zero;
			}
			float x = tmPro.fontSharedMaterial.GetFloat(ShaderUtilities.ID_UnderlayOffsetX);
			float y = tmPro.fontSharedMaterial.GetFloat(ShaderUtilities.ID_UnderlayOffsetY);
			return new Vector2(x, y);
		}

		public static void setUnderlayDilate(this TextMeshPro tmPro, float value)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.setUnderlayDilate() - fontSharedMaterial is null.");
				return;
			}
			tmPro.fontSharedMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, value);
			tmPro.UpdateMeshPadding();
		}

		public static float getUnderlayDilate(this TextMeshPro tmPro)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.getUnderlayDilate() - fontSharedMaterial is null.");
				return 0f;
			}
			return tmPro.fontSharedMaterial.GetFloat(ShaderUtilities.ID_UnderlayDilate);
		}

		public static void setUnderlaySoftness(this TextMeshPro tmPro, float value)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.setUnderlaySoftness() - fontSharedMaterial is null.");
				return;
			}
			tmPro.fontSharedMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, value);
			tmPro.UpdateMeshPadding();
		}

		public static float getUnderlaySoftness(this TextMeshPro tmPro)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.getUnderlaySoftness() - fontSharedMaterial is null.");
				return 0f;
			}
			return tmPro.fontSharedMaterial.GetFloat(ShaderUtilities.ID_UnderlaySoftness);
		}
		
		// Make an instance of the material so changing color, etc. won't affect the original material.
		public static void makeMaterialInstance(this TextMeshPro tmPro, Shader shader = null)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.makeMaterialInstance() - fontSharedMaterial is null.");
				return;
			}
			tmPro.fontMaterial = new Material(tmPro.fontSharedMaterial);
			if (shader != null)
			{
				tmPro.fontMaterial.shader = shader;
			}
		}
		
		public static Texture getFaceTexture(this TextMeshPro tmPro)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.getFaceTexture() - fontSharedMaterial is null.");
				return null;
			}
			return tmPro.fontSharedMaterial.GetTexture(ShaderUtilities.ID_FaceTex);
		}

		public static void setFaceTexture(this TextMeshPro tmPro, Texture tex)
		{
			if (tmPro.fontSharedMaterial == null)
			{
				Debug.LogWarning("TMProExtensionFunctions.setFaceTexture() - fontSharedMaterial is null.");
				return;
			}
			tmPro.fontSharedMaterial.SetTexture(ShaderUtilities.ID_FaceTex, tex);
		}
		
		// Based on UIWidget.pivotOffset, calculates the relative offset based on the current anchor position.
		// Assumes the object isn't using a custom anchor position.
		// I have no idea if this is accurate or not for UIInput since it only affect IME input (whatever that is).
		public static Vector2 getAnchorOffset(this TextMeshPro tmPro)
		{
			Vector2 v = Vector2.zero;
			// TODO:UNITY2018:obsoleteTextContainer:confirm
			Vector2 pivot = tmPro.rectTransform.pivot;
			v.x = 0 - pivot.x;
			v.y = 1 - pivot.y;

			return v;
		}

		/// <summary>
		///   Convert old TextContainerAnchors usage, make pivot match alignment.
		/// </summary> 
		public static TextAlignmentOptions convertLegacyTextAlignmentEnumValue(TextAlignmentOptions legacyValue)
		{
			switch ((int)legacyValue)
			{
				case 0:
					return TextAlignmentOptions.TopLeft;
				case 1:
					return TextAlignmentOptions.Top;
				case 2:
					return TextAlignmentOptions.TopRight;
				case 3:
					return TextAlignmentOptions.TopJustified;
				case 4:
					return TextAlignmentOptions.Left;
				case 5:
					return TextAlignmentOptions.Center;
				case 6:
					return TextAlignmentOptions.Right;
				case 7:
					return TextAlignmentOptions.Justified;
				case 8:
					return TextAlignmentOptions.BottomLeft;
				case 9:
					return TextAlignmentOptions.Bottom;
				case 10:
					return TextAlignmentOptions.BottomRight;
				case 11:
					return TextAlignmentOptions.BottomJustified;
				case 12:
					return TextAlignmentOptions.BaselineLeft;
				case 13:
					return TextAlignmentOptions.Baseline;
				case 14:
					return TextAlignmentOptions.BaselineRight;
				case 15:
					return TextAlignmentOptions.BaselineJustified;
				case 16:
					return TextAlignmentOptions.MidlineLeft;
				case 17:
					return TextAlignmentOptions.Midline;
				case 18:
					return TextAlignmentOptions.MidlineRight;
				case 19:
					return TextAlignmentOptions.MidlineJustified;
				case 20:
					return TextAlignmentOptions.CaplineLeft;
				case 21:
					return TextAlignmentOptions.Capline;
				case 22:
					return TextAlignmentOptions.CaplineRight;
				case 23:
					return TextAlignmentOptions.CaplineJustified;
				default:
					// Value was not a legacy value
					return legacyValue;
			}
		}

		// TODO:UNITY2018:obsoleteTextContainer:confirm
		/// <summary>
		///   Set pivot and alignment to match NGUI pivot value.
		/// </summary> 
		public static void SetPivotAndAlignmentFromUIPivot(TextMeshPro tmPro, UIWidget.Pivot nguiPivot)
		{
			switch (nguiPivot)
			{
				case UIWidget.Pivot.TopLeft:
					tmPro.alignment = TextAlignmentOptions.TopLeft;
					break;
				case UIWidget.Pivot.Top:
					tmPro.alignment = TextAlignmentOptions.Top;
					break;
				case UIWidget.Pivot.TopRight:
					tmPro.alignment = TextAlignmentOptions.TopRight;
					break;
				case UIWidget.Pivot.Left:
					tmPro.alignment = TextAlignmentOptions.Left;
					break;
				case UIWidget.Pivot.Center:
					tmPro.alignment = TextAlignmentOptions.Center;
					break;
				case UIWidget.Pivot.Right:
					tmPro.alignment = TextAlignmentOptions.Right;
					break;
				case UIWidget.Pivot.BottomLeft:
					tmPro.alignment = TextAlignmentOptions.BottomLeft;
					break;
				case UIWidget.Pivot.Bottom:
					tmPro.alignment = TextAlignmentOptions.Bottom;
					break;
				case UIWidget.Pivot.BottomRight:
					tmPro.alignment = TextAlignmentOptions.BottomRight;
					break;
			}
			SetPivotFromAlignment(tmPro);
		}

		// TODO:UNITY2018:obsoleteTextContainer:confirm
		/// <summary>
		///   Set pivot and alignment from obsolete TextContainerAnchors value.
		/// </summary> 
		public static void SetPivotAndAlignmentFromTextContainerAnchor(TextMeshPro tmPro, TextContainerAnchors anchor)
		{
			switch (anchor)
			{
				case TextContainerAnchors.TopLeft:
					tmPro.alignment = TextAlignmentOptions.TopLeft;
					break;
				case TextContainerAnchors.Top:
					tmPro.alignment = TextAlignmentOptions.Top;
					break;
				case TextContainerAnchors.TopRight:
					tmPro.alignment = TextAlignmentOptions.TopRight;
					break;
				case TextContainerAnchors.Left:
					tmPro.alignment = TextAlignmentOptions.Left;
					break;
				case TextContainerAnchors.Middle:
					tmPro.alignment = TextAlignmentOptions.Center;
					break;
				case TextContainerAnchors.Right:
					tmPro.alignment = TextAlignmentOptions.Right;
					break;
				case TextContainerAnchors.BottomLeft:
					tmPro.alignment = TextAlignmentOptions.BottomLeft;
					break;
				case TextContainerAnchors.Bottom:
					tmPro.alignment = TextAlignmentOptions.Bottom;
					break;
				case TextContainerAnchors.BottomRight:
					tmPro.alignment = TextAlignmentOptions.BottomRight;
					break;
			}
			SetPivotFromAlignment(tmPro);
		}

		// TODO:UNITY2018:obsoleteTextContainer:confirm
		/// <summary>
		///   Convert old TextContainerAnchors settings.
		/// </summary> 
		public static void SetPivotAndAlignmentFromTextContainer(TextMeshPro tmPro)
		{
			TextContainer textContainer = tmPro.GetComponent<TextContainer>();
			if (textContainer != null)
			{
				RectTransform rectTransform = tmPro.GetComponent<RectTransform>();
				if (rectTransform == null)
				{
					rectTransform = tmPro.gameObject.AddComponent<RectTransform>();
				}
				SetPivotAndAlignmentFromTextContainerAnchor(tmPro, textContainer.anchorPosition);
				if (Application.isPlaying)
				{
					Object.Destroy(textContainer);
				}
				else
				{
					Object.DestroyImmediate(textContainer, true);
				}
			}
		}

		// Check if the TextContainer alignment matches the TextMeshPro.  This is being done to double
		// check and build a conversion output as we remove TextContainer which are no longer used by TextMeshPro
		public static bool isTextContainerAlignmentSameAsTextMeshPro(TextMeshPro tmPro, TextContainer textContainer)
		{
			if (tmPro != null && textContainer != null)
			{
				TextAlignmentOptions tmProAlign = tmPro.alignment;
				TextContainerAnchors containerAlign = textContainer.anchorPosition;

				if ((tmProAlign == TextAlignmentOptions.TopLeft && containerAlign == TextContainerAnchors.TopLeft)
					|| (tmProAlign == TextAlignmentOptions.Top && containerAlign == TextContainerAnchors.Top)
					|| (tmProAlign == TextAlignmentOptions.TopRight && containerAlign == TextContainerAnchors.TopRight)
					|| (tmProAlign == TextAlignmentOptions.Left && containerAlign == TextContainerAnchors.Left)
					|| (tmProAlign == TextAlignmentOptions.Center && containerAlign == TextContainerAnchors.Middle)
					|| (tmProAlign == TextAlignmentOptions.Right && containerAlign == TextContainerAnchors.Right)
					|| (tmProAlign == TextAlignmentOptions.BottomLeft && containerAlign == TextContainerAnchors.BottomLeft)
					|| (tmProAlign == TextAlignmentOptions.Bottom && containerAlign == TextContainerAnchors.Bottom)
					|| (tmProAlign == TextAlignmentOptions.BottomRight && containerAlign == TextContainerAnchors.BottomRight))
				{
					return true;
				}
			}

			return false;
		}

		// TODO:UNITY2018:obsoleteTextContainer:confirm
		/// <summary>
		///   Convert old TextContainerAnchors usage, make pivot match alignment.
		/// </summary> 
		public static void SetPivotFromAlignment(TextMeshPro tmPro)
		{
			RectTransform rectTransform = tmPro.GetComponent<RectTransform>();
			if (rectTransform == null)
			{
				rectTransform = tmPro.gameObject.AddComponent<RectTransform>();
			}
			switch (tmPro.alignment)
			{
				case TextAlignmentOptions.TopLeft:
					rectTransform.pivot = new Vector2(0, 1);
					break;
				case TextAlignmentOptions.Top:
					rectTransform.pivot = new Vector2(0.5f, 1);
					break;
				case TextAlignmentOptions.TopRight:
					rectTransform.pivot = new Vector2(1, 1);
					break;
				case TextAlignmentOptions.Left:
					rectTransform.pivot = new Vector2(0, 0.5f);
					break;
				case TextAlignmentOptions.Center:
					rectTransform.pivot = new Vector2(0.5f, 0.5f);
					break;
				case TextAlignmentOptions.Right:
					rectTransform.pivot = new Vector2(1, 0.5f);
					break;
				case TextAlignmentOptions.BottomLeft:
					rectTransform.pivot = new Vector2(0, 0);
					break;
				case TextAlignmentOptions.Bottom:
					rectTransform.pivot = new Vector2(0.5f, 0);
					break;
				case TextAlignmentOptions.BottomRight:
					rectTransform.pivot = new Vector2(1, 0);
					break;
			}
		}
		
		/// <summary>
		///   Return equivalent TextContainerAnchors value from alignment for compatibility.
		/// </summary>
		public static TextContainerAnchors GetAnchorPosition(TextMeshPro tmPro)
		{
			switch (tmPro.alignment)
			{
				case TextAlignmentOptions.TopLeft:
					return TextContainerAnchors.TopLeft;
				case TextAlignmentOptions.Top:
					return TextContainerAnchors.Top;
				case TextAlignmentOptions.TopRight:
					return TextContainerAnchors.TopRight;
				case TextAlignmentOptions.Left:
					return TextContainerAnchors.Left;
				case TextAlignmentOptions.Center:
					return TextContainerAnchors.Middle;
				case TextAlignmentOptions.Right:
					return TextContainerAnchors.Right;
				case TextAlignmentOptions.BottomLeft:
					return TextContainerAnchors.BottomLeft;
				case TextAlignmentOptions.Bottom:
					return TextContainerAnchors.Bottom;
				case TextAlignmentOptions.BottomRight:
					return TextContainerAnchors.BottomRight;
			}
			return TextContainerAnchors.Middle;
		}

		// Copies all the settings of the source label to the target label.
		public static void copySettings(this TextMeshPro original, TextMeshPro other)
		{
			original.font = other.font;
			original.fontSharedMaterial = other.fontSharedMaterial;
			original.text = other.text;
			original.color = other.color;
			original.textContainer.width = other.textContainer.width;
			original.textContainer.height = other.textContainer.height;
			original.fontStyle = other.fontStyle;	// Handles stuff like bold, italic, underline.
			original.enableAutoSizing = other.enableAutoSizing;
			original.fontSizeMin = other.fontSizeMin;
			original.fontSizeMax = other.fontSizeMax;
			original.fontSize = other.fontSize;
			original.enableVertexGradient = other.enableVertexGradient;
			original.colorGradient = new VertexGradient(other.colorGradient.topLeft, other.colorGradient.topRight, other.colorGradient.bottomLeft, other.colorGradient.bottomRight);
			original.alignment = other.alignment;
			original.characterSpacing = other.characterSpacing;
			original.lineSpacing = other.lineSpacing;
			original.paragraphSpacing = other.paragraphSpacing;
			original.extraPadding = other.extraPadding;
			original.enableKerning = other.enableKerning;
			original.isOrthographic = other.isOrthographic;
			original.isOverlay = other.isOverlay;
		}
	
		//////////////////////////////////////////////////////////////////////////////////////////////////////////
		// Several transformCharacter overloads, for convenience.
		
		// Overload to transform a character by only position.
		public static void transformCharacterPosition(this TextMeshPro tmPro, int charIndex, Vector2 position, bool isRelativePositioning = false)
		{
			tmPro.transformCharacter(charIndex, position, Vector2.one, 0.0f, isRelativePositioning);
		}

		// Overload to transform a character by only scale, where width and height scale are the same.
		public static void transformCharacterScale(this TextMeshPro tmPro, int charIndex, float scale)
		{
			tmPro.transformCharacter(charIndex, Vector2.zero, Vector2.one * scale, 0.0f, true, false);
		}
		
		// Overload to transform a character by only scale, where width and height scale can be different.
		public static void transformCharacterScale(this TextMeshPro tmPro, int charIndex, Vector2 scale)
		{
			tmPro.transformCharacter(charIndex, Vector2.zero, scale, 0.0f, true, false);
		}

		// Overload to transform a character by only rotation.
		public static void transformCharacterRotation(this TextMeshPro tmPro, int charIndex, float rotation)
		{
			tmPro.transformCharacter(charIndex, Vector2.zero, Vector2.one, rotation, true, false);
		}

		// Overload to transform a character by only scale and rotation, where width and height scale are the same..
		public static void transformCharacter(this TextMeshPro tmPro, int charIndex, float scale, float rotation)
		{
			tmPro.transformCharacter(charIndex, Vector2.zero, Vector2.one * scale, rotation, true, false);
		}

		// Overload to transform a character by position, scale and rotation.
		// To be consistent with how Unity transforms work, positive rotation values are counter-clockwise, negative are clockwise.
		public static void transformCharacter(this TextMeshPro tmPro, int charIndex, Vector2 position, Vector2 scale, float rotation, bool isRelativePositioning = false, bool useBaseline = true)
		{
			// Get the index of the mesh used by this character.
			int meshIndex = tmPro.textInfo.characterInfo[charIndex].materialReferenceIndex;

			Vector3[] vertices = tmPro.textInfo.meshInfo[meshIndex].vertices;
		
			TMP_CharacterInfo charInfo = tmPro.textInfo.characterInfo[charIndex];
		
			if (!charInfo.isVisible)
			{
				// This isn't just a performance thing.
				// It's necessary in order to avoid problems with vertex offset,
				// since invisible characters have no vertices in the vertex array.
				return;
			}
		
			int vertexIndex = charInfo.vertexIndex;
		
			// First center the character so we're working with transforms relative to center.
			// Sometimes the vertical alignment is based on the baseline position of each character,
			// other times we want the characters to be vertically centered.
			float yOffset = (useBaseline ? charInfo.baseLine : charInfo.getCharacterCenter().y);
			
			Vector3 offsetToCenter = new Vector3((charInfo.topLeft.x + charInfo.topRight.x) * 0.5f, yOffset, 0.0f);
		
			for (int i = 0; i < 4; i++)
			{
				vertices[vertexIndex + i] += -offsetToCenter;
			}
		
			Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.Euler(0, 0, rotation), new Vector3(scale.x, scale.y, 1.0f));

			for (int i = 0; i < 4; i++)
			{
				vertices[vertexIndex + i] = matrix.MultiplyPoint3x4(vertices[vertexIndex + i]);
			
				if (isRelativePositioning)
				{
					// Offset by the opposite amount needed to make it centered, for the final positioning.
					vertices[vertexIndex + i] += offsetToCenter;
				}
			}
		
			tmPro.UpdateVertexData();
		}
		
		// End of transformCharacter overloads.
		//////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		// Returns the center position of a single character.
		public static Vector2 getCharacterCenter(this TMP_CharacterInfo charInfo)
		{
			return new Vector2(
				(charInfo.topLeft.x + charInfo.topRight.x) * 0.5f,
				(charInfo.topLeft.y + charInfo.bottomLeft.y) * 0.5f
			);
		}
	}
}
