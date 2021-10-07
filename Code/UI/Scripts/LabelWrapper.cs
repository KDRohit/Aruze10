using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMProExtensions;

/*
Wrapper class for labels so that we can use the same code regardless of what label technology is used.
Only one of the label inspectors should be linked to a component.
*/

[System.Serializable]
public class LabelWrapper
{
	public UILabel ngui;
	public TextMeshPro tmPro;
	
	[System.NonSerialized] public LabelWrapperComponent component = null;	// Set by the component if this object is a property of a wrapper component, for two-way reference.

	public LabelWrapper()
	{}
	
	public LabelWrapper(UILabel ngui)
	{
		this.ngui = ngui;
	}
	
	public LabelWrapper(TextMeshPro tmPro)
	{
		this.tmPro = tmPro;
	}

	// Constructor when you don't know which one will be null and which one won't.
	// Priority is to use TextMeshPro if both are non-null.
	public LabelWrapper(TextMeshPro tmPro, UILabel ngui)
	{
		if (tmPro != null)
		{
			// Just making sure.
			ngui = null;
		}
		this.ngui = ngui;
		this.tmPro = tmPro;
	}

	public bool hasLabelReference
	{
		get { return ngui != null || tmPro != null; }
	}

	// Forces the labels stored in the wrapper to be updated.
	public void forceUpdate()
	{
		if (ngui != null)
		{
			Debug.LogError("Sorry dude, didn't set this for NGUI objects, since you'd need to be able to convernt NGUI font size to tmPro size and vice versa.");
		}
		else if (tmPro != null)
		{
			tmPro.ForceMeshUpdate();
		}
	}
	
	public Transform transform
	{
		get
		{
			if (gameObject != null)
			{
				return gameObject.transform;
			}
			else
			{
				return null;
			}
		}
	}
	
	public GameObject gameObject
	{
		get
		{
			if (ngui != null)
			{
				return ngui.gameObject;
			}
			else if (tmPro != null)
			{
				return tmPro.gameObject;
			}
			return null;
		}
	}

	public virtual string text
	{
		set
		{
			if (ngui != null)
			{
				ngui.text = value;
			}
			else if (tmPro != null)
			{
				tmPro.text = value;
			}
		}
		get
		{
			if (ngui != null)
			{
				return ngui.text;
			}
			else if (tmPro != null)
			{
				return tmPro.text;
			}
			return "";
		}
	}
	
	public Color color
	{
		set
		{
			if (ngui != null)
			{
				ngui.color = value;
			}
			else if (tmPro != null)
			{
				tmPro.color = value;
			}
		}
		get
		{
			if (ngui != null)
			{
				return ngui.color;
			}
			else if (tmPro != null)
			{
				return tmPro.color;
			}
			return Color.black;
		}
	}

	public float alpha
	{
		set
		{
			if (ngui != null)
			{
				ngui.alpha = value;
			}
			else if (tmPro != null)
			{
				Color c = tmPro.color;
				c.a = value;
				tmPro.color = c;
			}
		}
		get
		{
			if (ngui != null)
			{
				return ngui.alpha;
			}
			else if (tmPro != null)
			{
				return tmPro.color.a;
			}
			return 1.0f;
		}
	}
	
	public int boxWidth
	{
		set
		{
			if (ngui != null)
			{
				ngui.lineWidth = value;
			}
			else if (tmPro != null)
			{
				tmPro.textContainer.width = value;
			}
		}
		get
		{
			if (ngui != null)
			{
				return ngui.lineWidth;
			}
			else if (tmPro != null)
			{
				return (int)tmPro.textContainer.width;
			}
			return 0;
		}
	}
	
	public int boxHeight
	{
		set
		{
			if (ngui != null)
			{
				ngui.lineHeight = value;
			}
			else if (tmPro != null)
			{
				tmPro.textContainer.height = value;
			}
		}
		get
		{
			if (ngui != null)
			{
				return ngui.lineHeight;
			}
			else if (tmPro != null)
			{
				return (int)tmPro.textContainer.height;
			}
			return 0;
		}
	}

	public float lineSpacing
	{
		set
		{
			if (ngui != null)
			{
				ngui.lineSpacing = (int)value;
			}
			else if (tmPro != null)
			{
				tmPro.lineSpacing = value;
			}
		}
		get
		{
			if (ngui != null)
			{
				return ngui.lineSpacing;
			}
			else if (tmPro != null)
			{
				return tmPro.lineSpacing;
			}
			return 0;
		}
	}

	public TextContainerAnchors pivot
	{
		set
		{
			if (ngui != null)
			{
				switch (value)
				{
					case TextContainerAnchors.TopLeft:
						ngui.pivot = UIWidget.Pivot.TopLeft;
						break;
					case TextContainerAnchors.Top:
						ngui.pivot = UIWidget.Pivot.Top;
						break;
					case TextContainerAnchors.TopRight:
						ngui.pivot = UIWidget.Pivot.TopRight;
						break;
					case TextContainerAnchors.Left:
						ngui.pivot = UIWidget.Pivot.Left;
						break;
					case TextContainerAnchors.Middle:
						ngui.pivot = UIWidget.Pivot.Center;
						break;
					case TextContainerAnchors.Right:
						ngui.pivot = UIWidget.Pivot.Right;
						break;
					case TextContainerAnchors.BottomLeft:
						ngui.pivot = UIWidget.Pivot.BottomLeft;
						break;
					case TextContainerAnchors.Bottom:
						ngui.pivot = UIWidget.Pivot.Bottom;
						break;
					case TextContainerAnchors.BottomRight:
						ngui.pivot = UIWidget.Pivot.BottomRight;
						break;
				}
			}
			else if (tmPro != null)
			{
				// TODO:UNITY2018:obsoleteTextContainer:confirm
				TMProExtensions.TMProExtensionFunctions.SetPivotAndAlignmentFromTextContainerAnchor(tmPro, value);
			}
		}
		get
		{
			if (ngui != null)
			{
				switch (ngui.pivot)
				{
					case UIWidget.Pivot.TopLeft:
						return TextContainerAnchors.TopLeft;
					case UIWidget.Pivot.Top:
						return TextContainerAnchors.Top;
					case UIWidget.Pivot.TopRight:
						return TextContainerAnchors.TopRight;
					case UIWidget.Pivot.Left:
						return TextContainerAnchors.Left;
					case UIWidget.Pivot.Center:
						return TextContainerAnchors.Middle;
					case UIWidget.Pivot.Right:
						return TextContainerAnchors.Right;
					case UIWidget.Pivot.BottomLeft:
						return TextContainerAnchors.BottomLeft;
					case UIWidget.Pivot.Bottom:
						return TextContainerAnchors.Bottom;
					case UIWidget.Pivot.BottomRight:
						return TextContainerAnchors.BottomRight;
				}
				// Shouldn't get here, but we need to return something to satisfy the compiler.
				return TextContainerAnchors.Middle;
			}
			else if (tmPro != null)
			{
				// TODO:UNITY2018:obsoleteTextContainer:confirm
				TMProExtensions.TMProExtensionFunctions.GetAnchorPosition(tmPro);
			}
			return 0;
		}
	}
	
	// We must use string type to specify font since each label type uses different font types.
	// The labelwrapper does not need to set the font of the label, it only needs to hold the font information
	public string font
	{
		get
		{
			if (ngui != null && ngui.font != null)
			{
				return ngui.font.name;
			}
			else if (tmPro != null && tmPro.font != null)
			{
				return tmPro.font.name;
			}
			return "";
		}
	}
	
	public bool isGradient
	{
		set
		{
			if (ngui != null)
			{
				ngui.colorMode = value ? UILabel.ColorMode.Gradient : UILabel.ColorMode.Solid;
			}
			else if (tmPro != null)
			{
				tmPro.enableVertexGradient = value;
			}
		}
		get
		{
			if (ngui != null)
			{
				return (ngui.colorMode == UILabel.ColorMode.Gradient);
			}
			else if (tmPro != null)
			{
				return tmPro.enableVertexGradient;
			}
			return false;
		}
	}
	
	public Color endGradientColor
	{
		set
		{
			if (ngui != null)
			{
				ngui.endGradientColor = value;
			}
			else if (tmPro != null)
			{
				// We want the bottom of the gradient to be the "end".
				tmPro.colorGradient = new VertexGradient(tmPro.color, tmPro.color, value, value);
			}
		}
		get
		{
			if (ngui != null)
			{
				return ngui.endGradientColor;
			}
			else if (tmPro != null)
			{
				return tmPro.colorGradient.bottomLeft;
			}
			return Color.white;
		}		
	}
	
	public string effectStyle
	{
		set
		{
			value = value.ToLower();
				
			if (ngui != null)
			{
				switch (value)
				{
					case "shadow":
						ngui.effectStyle = UILabel.Effect.Shadow;
						break;
					case "outline":
						ngui.effectStyle = UILabel.Effect.Outline;
						break;
					default:
						ngui.effectStyle = UILabel.Effect.None;
						break;
				}
			}
			else if (tmPro != null)
			{
				switch (value)
				{
					case "shadow":
						tmPro.enableUnderlay(true);
						tmPro.setUnderlayDilate(0.0f);
						break;
					case "outline":
						tmPro.enableUnderlay(true);
						tmPro.setUnderlayOffset(Vector2.zero);
						break;
				}
			}
		}
		get
		{
			if (tmPro.isUnderlayEnabled())
			{
				if (ngui != null)
				{
					switch (ngui.effectStyle)
					{
						case UILabel.Effect.Shadow:
							return "shadow";
						case UILabel.Effect.Outline:
							return "outline";
					}
				}
				else if (tmPro != null)
				{
					Vector2 offset = tmPro.getUnderlayOffset();
					if (offset.x != 0.0f || offset.y != 0.0f)
					{
						return "shadow";
					}
					else if (tmPro.getUnderlayDilate() != 0.0f)
					{
						return "outline";
					}
				}
			}
			return "";
		}
	}

	public Vector2 shadowOffset
	{
		set
		{
			if (ngui != null)
			{
				ngui.effectDistance = value;
			}
			else if (tmPro != null)
			{
				tmPro.setUnderlayOffset(value);
			}
		}
		get
		{
			if (ngui != null)
			{
				return ngui.effectDistance;
			}
			else if (tmPro != null)
			{
				return tmPro.getUnderlayOffset();
			}
			return Vector2.zero;
		}
	}

	public float outlineWidth
	{
		set
		{
			if (ngui != null)
			{
				ngui.effectDistance = Vector2.one * value;
			}
			else if (tmPro != null)
			{
				tmPro.setUnderlayDilate(value);
			}
		}
		get
		{
			if (ngui != null)
			{
				return ngui.effectDistance.x;
			}
			else if (tmPro != null)
			{
				return tmPro.getUnderlayDilate();
			}
			return 0.0f;
		}
	}

	public Color effectColor
	{
		set
		{
			if (ngui != null)
			{
				ngui.effectColor = value;
			}
			else if (tmPro != null)
			{
				tmPro.setUnderlayColor(value);
			}
		}
		get
		{
			if (ngui != null)
			{
				return ngui.effectColor;
			}
			else if (tmPro != null)
			{
				return tmPro.getUnderlayColor();
			}
			return Color.black;
		}
	}

	public float fontSize
	{
		set
		{
			if (ngui != null)
			{
				Debug.LogError("Sorry dude, didn't set this for NGUI objects, since you'd need to be able to convernt NGUI font size to tmPro size and vice versa.");
			}
			else if (tmPro != null)
			{
				tmPro.fontSize = value;
			}
		}
		get
		{
			if (ngui != null)
			{
				Debug.LogError("Sorry dude, didn't set this for NGUI objects, since you'd need to be able to convernt NGUI font size to tmPro size and vice versa.");
			}
			else if (tmPro != null)
			{
				return tmPro.fontSize;
			}
			return -1;
		}
	}

	public bool enableAutoSize
	{
		set
		{
			if (ngui != null)
			{
				ngui.shrinkToFit = value;
			}
			else if (tmPro != null)
			{
				tmPro.enableAutoSizing = value;
			}
		}
		get
		{
			if (ngui != null)
			{
				return ngui.shrinkToFit;
			}
			else if (tmPro != null)
			{
				return tmPro.enableAutoSizing;
			}
			return false;
		}
	}

	public void copySettings(LabelWrapper other)
	{
		if (tmPro != null)
		{
			tmPro.copySettings(other.tmPro);
		}
		else if (ngui != null)
		{
			NGUIExt.copyUILabel(ngui, other.ngui);
		}
	}
	
	// Returns whether this label has a matching internal label.
	public bool matchesLabel(LabelWrapper other)
	{
		if (ngui != null && ngui == other.ngui)
		{
			return true;
		}
		if (tmPro != null && tmPro == other.tmPro)
		{
			return true;
		}
		return false;
	}
}