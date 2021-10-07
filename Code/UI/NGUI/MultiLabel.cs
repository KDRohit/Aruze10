using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
If you have a label with several outline labels and they should all have the same text,
then attach this script so when you change that main label, it automatically changes all those outline labels, too.
*/

[ExecuteInEditMode]
public class MultiLabel : MonoBehaviour
{
	public UILabel mainLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent mainLabelWrapperComponent;

	public LabelWrapper mainLabelWrapper
	{
		get
		{
			if (_mainLabelWrapper == null)
			{
				if (mainLabelWrapperComponent != null)
				{
					_mainLabelWrapper = mainLabelWrapperComponent.labelWrapper;
				}
				else
				{
					if (mainLabel == null)
					{
						mainLabel = GetComponent<UILabel>();
					}

					_mainLabelWrapper = new LabelWrapper(mainLabel);
				}
			}
			return _mainLabelWrapper;
		}
	}
	private LabelWrapper _mainLabelWrapper = null;
	
	public UILabel[] outlineLabels = new UILabel[1];	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] outlineLabelsWrapperComponent = new LabelWrapperComponent[1];

	public List<LabelWrapper> outlineLabelsWrapper
	{
		get
		{
			if (_outlineLabelsWrapper == null)
			{
				_outlineLabelsWrapper = new List<LabelWrapper>();

				if (outlineLabelsWrapperComponent != null && outlineLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in outlineLabelsWrapperComponent)
					{
						if (wrapperComponent != null)
						{
							_outlineLabelsWrapper.Add(wrapperComponent.labelWrapper);
						}
					}
				}
				
				if (outlineLabels != null && outlineLabels.Length > 0)
				{
					foreach (UILabel label in outlineLabels)
					{
						if (label != null)
						{
							_outlineLabelsWrapper.Add(new LabelWrapper(label));
						}
					}
				}
			}
			return _outlineLabelsWrapper;
		}
	}
	private List<LabelWrapper> _outlineLabelsWrapper = null;	
	
	
	private string lastText = "";
	
	void Awake()
	{
		if (Application.isPlaying)
		{
			if (!mainLabelWrapper.hasLabelReference)
			{
				Debug.LogWarning("MultiLabel script has no associated main label.", gameObject);
				enabled = false;
			}
			
			if (outlineLabelsWrapper == null || outlineLabelsWrapper.Count == 0)
			{
				Debug.LogWarning("MultiLabel script has no associated outline labels.", gameObject);
				enabled = false;
			}

		}
	}
	
	void OnEnable()
	{
		Update();   // updated right a way so there is no unset labels rendering for a frame.
	}

	// This function loops through all outline labels and modifies their enable state
	public void setMultiLabelEnabledState(bool state)
	{
		SafeSet.gameObjectActive(mainLabelWrapper.gameObject, state);

		foreach (LabelWrapper outlineLabel in outlineLabelsWrapper)
		{
			SafeSet.gameObjectActive(outlineLabel.gameObject, state);
		}
	}

	public void Update()
	{
		if (mainLabelWrapper.hasLabelReference)
		{
			if (lastText != mainLabelWrapper.text)
			{
				foreach (LabelWrapper outlineLabel in outlineLabelsWrapper)
				{
					if (!outlineLabel.hasLabelReference)
					{
						Debug.LogWarning(string.Format("MultiLabel has a null outline", gameObject));

						if (Application.isPlaying)
						{
							enabled = false;
							return;
						}
					}
					else
					{
						outlineLabel.text = mainLabelWrapper.text;
					}
				}
				
				lastText = mainLabelWrapper.text;
			}
		}
	}
}

