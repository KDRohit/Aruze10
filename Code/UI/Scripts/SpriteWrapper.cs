using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Wrapper class for graphical UI elements so that we can use the same code regardless of what UI technology is used.
Only one of the label inspectors should be linked to a component.
*/

[System.Serializable]
public class SpriteWrapper
{
	public UISprite ngui;
	public Renderer renderer;
	
	public GameObject gameObject
	{
		get
		{
			if (ngui != null)
			{
				return ngui.gameObject;
			}
			else if (renderer != null)
			{
				return renderer.gameObject;
			}
			return null;
		}
	}
	
	public Color color
	{
		set
		{
			if (ngui != null)
			{
				ngui.color = value;
			}
			else if (renderer != null)
			{
				CommonMaterial.colorMaterials(rendererMaterials, value);
			}
		}
		get
		{
			if (ngui != null)
			{
				return ngui.color;
			}
			else if (renderer != null)
			{
				if (rendererMaterials != null)
				{
					// Since we don't know which material to use, just use the first one.
					// Most of the time this is probably the only material anyway.
					return rendererMaterials[0].color;
				}
			}
			return Color.black;
		}
	}

	public float alpha
	{
		set
		{
			if (ngui != null)
			{
				ngui.alpha = value;
			}
			else if (renderer != null)
			{
				CommonMaterial.setAlphaOnMaterials(rendererMaterials, alpha);
			}
		}
		get
		{
			if (ngui != null)
			{
				return ngui.alpha;
			}
			else if (renderer != null)
			{
				if (rendererMaterials != null)
				{
					return CommonMaterial.getAlphaOnMaterial(rendererMaterials[0]);
				}
			}
			return 1.0f;
		}
	}
	
	private Material[] rendererMaterials
	{
		get
		{
			if (_rendererMaterials == null)
			{
				// Only do this once, since it makes a copy every time you do.
				_rendererMaterials = renderer.materials;
			}
			
			return _rendererMaterials;
		}
	}
	private Material[] _rendererMaterials = null;

}
