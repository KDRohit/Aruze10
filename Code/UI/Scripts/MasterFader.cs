using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Use this script as a master alpha control for multiple TextMeshPro and/or UISprite and/or Renderer objects.
*/

[ExecuteInEditMode]

public class MasterFader : MonoBehaviour
{
	public float alpha = 1.0f;
	public TextMeshPro[] labels;
	public UISprite[] sprites;
	public Renderer[] renderers;
	
	private float lastAlpha = -1.0f;
	
	void Awake()
	{
		setAlpha(alpha);
	}
	
	void Update()
	{
		if (alpha != lastAlpha)
		{
			setAlpha(alpha);
		}
	}
	
	// This may be called directly instead of setting the alpha value
	// if you need it to set immediately instead of waiting for the next frame.
	public void setAlpha(float newAlpha)
	{
		alpha = newAlpha;
		
		if (labels != null)
		{
			foreach (TextMeshPro label in labels)
			{
				label.alpha = Mathf.Clamp01(alpha);
			}
		}

		if (sprites != null)
		{
			foreach (UISprite sprite in sprites)
			{
				sprite.alpha = Mathf.Clamp01(alpha);
			}
		}
		
		if (renderers != null)
		{
			foreach (Renderer rend in renderers)
			{
				CommonRenderer.alphaRenderer(rend, alpha);
			}
		}
		
		lastAlpha = alpha;
	}
}
