//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Sample script showing how easy it is to implement a standard button that swaps sprites.
/// </summary>

[AddComponentMenu("NGUI/UI/Image Button")]
public class UIImageButton : TICoroutineMonoBehaviour
{
	public UISprite target;
	public string normalSprite;
	public string hoverSprite;
	public string pressedSprite;
	public string disabledSprite;

	// Zynga - Ryan: There are instances where we wish to use the same images but at different sizes for smaller mobile devices,
	// and this making pixel perfect on these images causes unneeded flickering.  This override allows us to change its use 
	// in the script that enforces size, which is SmallDeviceSizeEnforcer.cs
	public bool overrideMakePixelPerfect = false;
	
	public bool isEnabled
	{
		get
		{
			Collider col = GetComponent<Collider>();
			return col && col.enabled;
		}
		set
		{
			Collider col = GetComponent<Collider>();
			if (!col) return;

			if (col.enabled != value)
			{
				col.enabled = value;
				UpdateImage();
			}
		}
	}

	protected override void OnEnable ()
	{
		base.OnEnable();
		
		if (target == null) target = GetComponentInChildren<UISprite>();
		UpdateImage();
	}
	
	protected virtual void UpdateImage()
	{
		if (target != null)
		{
			if (isEnabled)
			{
				target.spriteName = UICamera.IsHighlighted(gameObject) ? hoverSprite : normalSprite;
			}
			else
			{
				target.spriteName = disabledSprite;
			}
			//Zynga - Ryan: optional override.  See above documentation.
			if (!overrideMakePixelPerfect)
			{
				target.MakePixelPerfect();
			}
		}
	}

	protected virtual void OnHover (bool isOver)
	{
		if (isEnabled && target != null)
		{
			target.spriteName = isOver ? hoverSprite : normalSprite;
			//Zynga - Ryan: optional override.  See above documentation.
			if (!overrideMakePixelPerfect)
			{
				target.MakePixelPerfect();
			}
		}
	}

	protected virtual void OnPress (bool pressed)
	{
		if (pressed)
		{
			target.spriteName = pressedSprite;
			//Zynga - Ryan: optional override.  See above documentation.
			if (!overrideMakePixelPerfect)
			{
				target.MakePixelPerfect();
			}
		}
		else UpdateImage();
	}
}
