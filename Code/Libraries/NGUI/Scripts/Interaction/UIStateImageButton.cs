using UnityEngine;
using System.Collections;

[AddComponentMenu("NGUI/UI/Image Button")]
public class UIStateImageButton : UIImageButton
{
	public string selectedSprite;

	protected override void UpdateImage()
	{
		/*if (target != null)
		{
			if (isEnabled)
			{
				target.spriteName = selectedSprite;
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
		}*/
	}

	public void SetSelected(bool isSelected = false)
	{
		if (isSelected)
		{
			target.spriteName = selectedSprite;
		}
		else
		{
			target.spriteName = disabledSprite;
		}
	}

	protected override void OnHover(bool isOver)
	{
		
	}

	protected virtual void OnPress(bool pressed)
	{
		base.OnPress(pressed);

		SetSelected(pressed);
	}

	protected virtual void OnSelect(bool isSelected)
	{
		if (isSelected)
		{
			SetSelected(isSelected);
		}
	}

	protected virtual void OnDrop(GameObject obj)
	{
		// doesn't matter, user just tried to drag this button so it's still selected as per the state design
		if (obj == gameObject)
		{
			SetSelected(true);
		}
	}
}
