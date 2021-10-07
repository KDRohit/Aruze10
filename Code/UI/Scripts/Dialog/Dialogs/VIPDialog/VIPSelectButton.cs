using UnityEngine;
using System.Collections;

public class VIPSelectButton : TICoroutineMonoBehaviour {

	public UILabel unselectedLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent unselectedLabelWrapperComponent;

	public LabelWrapper unselectedLabelWrapper
	{
		get
		{
			if (_unselectedLabelWrapper == null)
			{
				if (unselectedLabelWrapperComponent != null)
				{
					_unselectedLabelWrapper = unselectedLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_unselectedLabelWrapper = new LabelWrapper(unselectedLabel);
				}
			}
			return _unselectedLabelWrapper;
		}
	}
	private LabelWrapper _unselectedLabelWrapper = null;
	
	public UILabel selectedLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent selectedLabelWrapperComponent;

	public LabelWrapper selectedLabelWrapper
	{
		get
		{
			if (_selectedLabelWrapper == null)
			{
				if (selectedLabelWrapperComponent != null)
				{
					_selectedLabelWrapper = selectedLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_selectedLabelWrapper = new LabelWrapper(selectedLabel);
				}
			}
			return _selectedLabelWrapper;
		}
	}
	private LabelWrapper _selectedLabelWrapper = null;
	

	private UIImageButton uiButton;

	// Use this for initialization
	void Start ()
	{
		this.uiButton = gameObject.GetComponent<UIImageButton>();
	}

	public void Select(bool value)
	{
		if (this.unselectedLabelWrapper != null)
		{
			this.unselectedLabelWrapper.gameObject.SetActive(!value);
		}
		if (this.selectedLabelWrapper != null)
		{
			this.selectedLabelWrapper.gameObject.SetActive(value);
		}

		if (this.uiButton != null)
		{
			// Note that this is reversed, because the selected tab should be un-enabled:
			this.uiButton.isEnabled = !value;
		}
	}
	
}

