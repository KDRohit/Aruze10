using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Controls a soft shadow sprite that goes behind a label, so that the size of the shadow is sized to fit the label.
THIS IS OBSOLETE. PLEASE USE UIStretchTextMeshPro INSTEAD.
*/

[ExecuteInEditMode]
public class LabelShadow : TICoroutineMonoBehaviour
{
	public UILabel label;
	public TextMeshPro tmPro;
	public Vector2 padding = new Vector2(130, 68);
	
	private UIWidget _shadow;
	private string _lastText = "";
	private Vector2 _lastPadding = Vector2.zero;
	
	private string labelText
	{
		get
		{
			if (label != null)
			{
				return label.text;
			}
			else if (tmPro != null)
			{
				return tmPro.text;
			}
			return "";
		}
	}
	
	void Awake()
	{
		_shadow = GetComponent<UISprite>();
		if (_shadow == null)
		{
			_shadow = GetComponent<UITexture>();
		}
		
		if (Application.isPlaying && _shadow == null)
		{
			Debug.LogErrorFormat("LabelShadow error: object {0} has no associated UISprite/UITexture for shadow.", gameObject);
			enabled = false;
		}
	}
	
	void Update()
	{
		if (label != null || tmPro != null)
		{
			if (_lastText != labelText ||
				_lastPadding.x != padding.x ||
				_lastPadding.y != padding.y
				)
			{
				refresh();
			}
		}
	}
	
	public void refresh()
	{
		_shadow.enabled = (labelText != "");

		Vector2 size = Vector2.zero;
		
		if (label != null)
		{
			size = NGUIExt.getLabelPixelSize(label);
		}
		else if (tmPro != null)
		{
			tmPro.ForceMeshUpdate();	// Force the bounds to be updated immediately after text changes.
			size = new Vector2(tmPro.bounds.size.x, tmPro.bounds.size.y);
		}
		
		_shadow.transform.localScale = new Vector3(size.x + padding.x, size.y + padding.y, 1);
		_lastText = labelText;
		_lastPadding.x = padding.x;
		_lastPadding.y = padding.y;
	}
}
