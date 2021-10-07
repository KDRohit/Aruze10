using UnityEngine;
using TMPro;

/**
A compoenent version of the LabelWrapper class, which can hide what the underlying label is
*/
[ExecuteInEditMode]
public class LabelWrapperComponent : TICoroutineMonoBehaviour 
{
	[SerializeField] protected LabelWrapper _labelWrapper = new LabelWrapper();
	public virtual LabelWrapper labelWrapper
	{
		get { return _labelWrapper; }
	}

	protected virtual void Update()
	{
		// only run this in edit mode, to try and grab an initial link to the label component
		if (!Application.isPlaying)
		{
			updateLabelComponentLink();
		}
	}	

	protected virtual void Awake()
	{
		// when awaking if the label isn't set yet, try and set it
		updateLabelComponentLink();
	}

	private void Reset()
	{
		updateLabelComponentLink();
	}

	public UILabel nguiLabel
	{
		get { return labelWrapper.ngui; }
		set { labelWrapper.ngui = value; }
	}

	public TextMeshPro tmProLabel
	{
		get { return labelWrapper.tmPro; }
		set { labelWrapper.tmPro = value; }
	}

	// determine if a label is hooked up to the LabelWrapper yet
	public bool hasLabelReference(bool shouldWarn = true)
	{
		bool hasLabelReference = labelWrapper.hasLabelReference;

		if (!hasLabelReference && shouldWarn)
		{
			Debug.LogWarning("LabelWrapperComponent.hasLabelReference() - Trying to change the property of a label which doesn't have a label reference set yet!  Consider using menu option: \"Zynga/Assets/Add LabelWrapperComponent To All Text Labels\" to update label linkages.");
		}

		return hasLabelReference;
	}

	// try and grab a link to the label component
	private void updateLabelComponentLink()
	{
		labelWrapper.component = this;
		
		if (!hasLabelReference(false))
		{
			// find the type of text object attached
			labelWrapper.tmPro = gameObject.GetComponent<TextMeshPro>();

			if (labelWrapper.tmPro == null)
			{
				// try finding an NGUI label since it didn't have a TextMeshPro
				labelWrapper.ngui = gameObject.GetComponent<UILabel>();
			}

			if (!hasLabelReference(false))
			{
				Debug.LogError("LabelWrapperComponent.updateLabelComponentLink() - Couldn't find a text label object.");
			}
		}
	}

	public void forceUpdate()
	{

		// Only call force update if there's a reference to the label and it's not an NGUI label.
		// This is because NGUI labels don't need to be (and shouldn't be) updated. 
		if (hasLabelReference() && labelWrapper.ngui == null)
		{
			labelWrapper.forceUpdate();
		}
	}

	// the text value of the label
	public virtual string text
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.text = value;
			}
		}
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.text;
			}
			else
			{
				return "";
			}
		}
	}
	
	// the color of the label
	public Color color
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.color = value;
			}
		}
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.color;
			}
			else
			{
				return Color.black;
			}
		}
	}

	// the label alpha
	public float alpha
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.alpha = value;
			}
		}
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.alpha;
			}
			else
			{
				return 1.0f;
			}
		}
	}
	
	// the fitting box width for the label
	public int boxWidth
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.boxWidth = value;
			}
		}
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.boxWidth;
			}
			else
			{
				return 0;
			}
		}
	}
	
	// the fitting box height for the label
	public int boxHeight
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.boxHeight = value;
			}
		}
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.boxHeight;
			}
			else
			{
				return 0;
			}
		}
	}

	// line spacing between text lines of label
	public float lineSpacing
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.lineSpacing = value;
			}
		}
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.lineSpacing;
			}
			else
			{
				return 0;
			}
		}
	}

	// the way the text is anchored
	public TextContainerAnchors pivot
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.pivot = value;
			}
		}
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.pivot;
			}
			else
			{
				return 0;
			}
		}
	}
	
	// We must use string type to specify font since each label type uses different font types.
	// The labelwrapper does not need to set the font of the label, it only needs to hold the font information
	public string font
	{
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.font;
			}
			else
			{
				return "";
			}
		}
	}
	
	// controls if a label is using a gradient as its color
	public bool isGradient
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.isGradient = value;
			}
		}
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.isGradient;
			}
			else
			{
				return false;
			}
		}
	}
	
	// lowest gradient color
	public Color endGradientColor
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.endGradientColor = value;
			}
		}
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.endGradientColor;
			}
			else
			{
				return Color.white;
			}
		}		
	}
	
	// special effect on the label
	public string effectStyle
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.effectStyle = value;
			}
		}
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.effectStyle;
			}
			else
			{
				return "";
			}
		}
	}

	// controls shadow if using shadow effect of label
	public Vector2 shadowOffset
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.shadowOffset = value;
			}
		}
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.shadowOffset;
			}
			else
			{
				return Vector2.zero;
			}
		}
	}

	// adjust outline size
	public float outlineWidth
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.outlineWidth = value;
			}
		}
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.outlineWidth;
			}
			else
			{
				return 0.0f;
			}
		}
	}

	// color of the special effect on the label
	public Color effectColor
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.effectColor = value;
			}
		}

		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.effectColor;
			}
			else
			{
				return Color.black;
			}
		}
	}

	public float fontSize
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.fontSize = value;
			}
		}

		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.fontSize;
			}
			else
			{
				return -1;
			}
		}
	}

	public bool enableAutoSize
	{
		set
		{
			if (hasLabelReference())
			{
				labelWrapper.enableAutoSize = value;
			}
		}
		get
		{
			if (hasLabelReference())
			{
				return labelWrapper.enableAutoSize;
			}
			return false;
		}
	}
}
