using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VIPBenefitsRow : TICoroutineMonoBehaviour
{
	public UILabel titleLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent titleLabelWrapperComponent;

	public LabelWrapper titleLabelWrapper
	{
		get
		{
			if (_titleLabelWrapper == null)
			{
				if (titleLabelWrapperComponent != null)
				{
					_titleLabelWrapper = titleLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_titleLabelWrapper = new LabelWrapper(titleLabel);
				}
			}
			return _titleLabelWrapper;
		}
	}
	private LabelWrapper _titleLabelWrapper = null;
	
	public UILabel[] labels;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] labelsWrapperComponent;

	public List<LabelWrapper> labelsWrapper
	{
		get
		{
			if (_labelsWrapper == null)
			{
				_labelsWrapper = new List<LabelWrapper>();

				if (labelsWrapperComponent != null && labelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in labelsWrapperComponent)
					{
						_labelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in labels)
					{
						_labelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _labelsWrapper;
		}
	}
	private List<LabelWrapper> _labelsWrapper = null;	
	
	public GameObject[] checks;
	public GameObject[] plusses;
	public string anonymousLocalizationKey;

	public void greyOut()
	{
		// Go through all our contents and color disabled:
		if (checks != null && plusses != null)
		{
			int maxItems = Mathf.Max(labelsWrapper.Count, Mathf.Max(checks.Length, plusses.Length));
			for (int i = 0; i < maxItems; i++)
			{
				if (i < labelsWrapper.Count && labelsWrapper[i].hasLabelReference)
				{
					labelsWrapper[i].color = Color.grey;
				}
				if (i < checks.Length && checks[i] != null)
				{
					UISprite sprite = checks[i].GetComponent<UISprite>();
					if (sprite != null)
					{
						sprite.color = Color.grey;
					}
				}
				if (i < plusses.Length && plusses[i] != null)
				{
					UISprite sprite = plusses[i].GetComponent<UISprite>();
					if (sprite != null)
					{
						sprite.color = Color.grey;
					}
				}
			}
		}

		if (titleLabelWrapper.gameObject != null)
		{
			UILabelStaticText st = titleLabelWrapper.gameObject.GetComponent<UILabelStaticText>();
			if (st != null)
			{
				st.enabled = false;
			}
			if (anonymousLocalizationKey != "")
			{
				titleLabelWrapper.text = Localize.textUpper(anonymousLocalizationKey);
			}

			titleLabelWrapper.color = Color.grey;
		}
	}
}

