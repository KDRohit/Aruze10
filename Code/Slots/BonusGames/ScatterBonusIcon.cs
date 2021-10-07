using UnityEngine;
using System.Collections;

//Basic class meant to hold the different components that might be modified in a Scatter Symbol
//Set these values in the inspector

public class ScatterBonusIcon : MonoBehaviour
{
	public GameObject textObject;
	public UILabelStyler winAmountTextStyler;
	public UILabel textLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent textLabelWrapperComponent;

	public LabelWrapper textLabelWrapper
	{
		get
		{
			if (_textLabelWrapper == null)
			{
				if (textLabelWrapperComponent != null)
				{
					_textLabelWrapper = textLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_textLabelWrapper = new LabelWrapper(textLabel);
				}
			}
			return _textLabelWrapper;
		}
	}
	private LabelWrapper _textLabelWrapper = null;
	
	public UILabel textShadow;	// To be removed when prefabs are updated.
	public LabelWrapperComponent textShadowWrapperComponent;

	public LabelWrapper textShadowWrapper
	{
		get
		{
			if (_textShadowWrapper == null)
			{
				if (textShadowWrapperComponent != null)
				{
					_textShadowWrapper = textShadowWrapperComponent.labelWrapper;
				}
				else
				{
					_textShadowWrapper = new LabelWrapper(textShadow);
				}
			}
			return _textShadowWrapper;
		}
	}
	private LabelWrapper _textShadowWrapper = null;
	
	public UILabel textOutline;	// To be removed when prefabs are updated.
	public LabelWrapperComponent textOutlineWrapperComponent;

	public LabelWrapper textOutlineWrapper
	{
		get
		{
			if (_textOutlineWrapper == null)
			{
				if (textOutlineWrapperComponent != null)
				{
					_textOutlineWrapper = textOutlineWrapperComponent.labelWrapper;
				}
				else
				{
					_textOutlineWrapper = new LabelWrapper(textOutline);
				}
			}
			return _textOutlineWrapper;
		}
	}
	private LabelWrapper _textOutlineWrapper = null;
	
	public UILabel textGray;	// To be removed when prefabs are updated.
	public LabelWrapperComponent textGrayWrapperComponent;

	public LabelWrapper textGrayWrapper
	{
		get
		{
			if (_textGrayWrapper == null)
			{
				if (textGrayWrapperComponent != null)
				{
					_textGrayWrapper = textGrayWrapperComponent.labelWrapper;
				}
				else
				{
					_textGrayWrapper = new LabelWrapper(textGray);
				}
			}
			return _textGrayWrapper;
		}
	}
	private LabelWrapper _textGrayWrapper = null;
	
	public LabelWrapperComponent creditsTextLabelWrapper;
	public Animator iconAnimator;
	public string PATH_TO_GLOW = ""; //For scatter games with different colored glows like Ghostbusters01
	[HideInInspector] public string iconName;
}

