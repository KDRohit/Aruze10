using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// If the art uses Text Mesh Pro, then you should use PickGameButtonNew.
// You will probably have to make new versions of whatever code uses the new pick game buttons, too.
// For example, PickAndAddWildsToReelsModules and PickAndAddWildsToReelsNewModule. */

public class PickGameButton : TICoroutineMonoBehaviour
{
	public GameObject button;
	public Animator animator;
	
	public UILabel revealNumberLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent revealNumberWrapper;
	public UILabel revealNumberOutlineLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent revealNumberOutlineLabelWrapperComponent;

	public LabelWrapper revealNumberOutlineLabelWrapper
	{
		get
		{
			if (_revealNumberOutlineLabelWrapper == null)
			{
				if (revealNumberOutlineLabelWrapperComponent != null)
				{
					_revealNumberOutlineLabelWrapper = revealNumberOutlineLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_revealNumberOutlineLabelWrapper = new LabelWrapper(revealNumberOutlineLabel);
				}
			}
			return _revealNumberOutlineLabelWrapper;
		}
	}
	private LabelWrapper _revealNumberOutlineLabelWrapper = null;
	
	public UILabel revealGrayNumberLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent revealGrayNumberWrapper;
	public UILabel revealGrayNumberOutlineLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent revealGrayNumberOutlineLabelWrapperComponent;

	public LabelWrapper revealGrayNumberOutlineLabelWrapper
	{
		get
		{
			if (_revealGrayNumberOutlineLabelWrapper == null)
			{
				if (revealGrayNumberOutlineLabelWrapperComponent != null)
				{
					_revealGrayNumberOutlineLabelWrapper = revealGrayNumberOutlineLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_revealGrayNumberOutlineLabelWrapper = new LabelWrapper(revealGrayNumberOutlineLabel);
				}
			}
			return _revealGrayNumberOutlineLabelWrapper;
		}
	}
	private LabelWrapper _revealGrayNumberOutlineLabelWrapper = null;
	
	public UILabel multiplierLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent multiplierLabelWrapperComponent;

	public LabelWrapper multiplierLabelWrapper
	{
		get
		{
			if (_multiplierLabelWrapper == null)
			{
				if (multiplierLabelWrapperComponent != null)
				{
					_multiplierLabelWrapper = multiplierLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierLabelWrapper = new LabelWrapper(multiplierLabel);
				}
			}
			return _multiplierLabelWrapper;
		}
	}
	private LabelWrapper _multiplierLabelWrapper = null;
	
	public UILabel multiplierOutlineLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent multiplierOutlineLabelWrapperComponent;

	public LabelWrapper multiplierOutlineLabelWrapper
	{
		get
		{
			if (_multiplierOutlineLabelWrapper == null)
			{
				if (multiplierOutlineLabelWrapperComponent != null)
				{
					_multiplierOutlineLabelWrapper = multiplierOutlineLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierOutlineLabelWrapper = new LabelWrapper(multiplierOutlineLabel);
				}
			}
			return _multiplierOutlineLabelWrapper;
		}
	}
	private LabelWrapper _multiplierOutlineLabelWrapper = null;
	
	public UILabel extraLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent extraLabelWrapperComponent;

	public LabelWrapper extraLabelWrapper
	{
		get
		{
			if (_extraLabelWrapper == null)
			{
				if (extraLabelWrapperComponent != null)
				{
					_extraLabelWrapper = extraLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_extraLabelWrapper = new LabelWrapper(extraLabel);
				}
			}
			return _extraLabelWrapper;
		}
	}
	private LabelWrapper _extraLabelWrapper = null;
	
	public UILabel extraOutlineLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent extraOutlineLabelWrapperComponent;

	public LabelWrapper extraOutlineLabelWrapper
	{
		get
		{
			if (_extraOutlineLabelWrapper == null)
			{
				if (extraOutlineLabelWrapperComponent != null)
				{
					_extraOutlineLabelWrapper = extraOutlineLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_extraOutlineLabelWrapper = new LabelWrapper(extraOutlineLabel);
				}
			}
			return _extraOutlineLabelWrapper;
		}
	}
	private LabelWrapper _extraOutlineLabelWrapper = null;
	
	
	public GameObject extraGo;
	
	public UISprite imageReveal;
	public UISprite[] multipleImageReveals;
	public Material material;
	public string pickMeSoundName;
	
	public MeshRenderer[] glowList;
	public MeshRenderer[] glowShadowList;
}

