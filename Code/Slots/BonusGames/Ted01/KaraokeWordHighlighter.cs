using UnityEngine;
using System.Collections;

public class KaraokeWordHighlighter : MonoBehaviour {

	public Color initialColor;
	public Color highlightColor;
	public UILabel label;	// To be removed when prefabs are updated.
	public LabelWrapperComponent labelWrapperComponent;

	public LabelWrapper labelWrapper
	{
		get
		{
			if (_labelWrapper == null)
			{
				if (labelWrapperComponent != null)
				{
					_labelWrapper = labelWrapperComponent.labelWrapper;
				}
				else
				{
					_labelWrapper = new LabelWrapper(label);
				}
			}
			return _labelWrapper;
		}
	}
	private LabelWrapper _labelWrapper = null;
	

	void OnTriggerEnter(Collider other) 
	{
		labelWrapper.color = highlightColor;
	}

	void OnTriggerExit(Collider other)
	{
		labelWrapper.color = initialColor;
	}
}

