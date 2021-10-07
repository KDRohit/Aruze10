using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
PLEASE USE MasterFader INSTEAD OF THIS CLASS.
THIS WILL BE REMOVED AFTER EXISTING THINGS ARE CONVERTED OVER.
*/

[ExecuteInEditMode]

public class TextMeshProFader : MonoBehaviour
{
	public float alpha = 1.0f;
	public TextMeshPro[] labels;
	
	private float lastAlpha = -1.0f;
	
	void Awake()
	{
		setAlpha();
	}
	
	void Update()
	{
		if (alpha != lastAlpha)
		{
			setAlpha();
		}
	}
	
	private void setAlpha()
	{
		if (labels == null)
		{
			return;
		}
		foreach (TextMeshPro label in labels)
		{
			label.alpha = Mathf.Clamp01(alpha);
		}
		lastAlpha = alpha;
	}
}
