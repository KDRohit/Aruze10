using UnityEngine;
using System.Collections;

public class ToggleHandler : ClickHandler
{
	public UISprite targetSprite;
	public TMPro.TextMeshPro targetLabel;
	public string toggleOnSprite;
	public string toggleOffSprite;

	public bool isToggled = false;

	private ToggleManager manager;
	public int index;


	public enum TextStyling
	{
		STYLE,
		COLOR,
		HIDE,
		NONE
	}
	public TextStyling stylingMethod;

	public Material toggleOnFontStyle;
	public Material toggleOffFontStyle;
	public Color toggleOnFontColor;
	public Color toggleOffFontColor;


	public string text
 	{
		get
		{
			return targetLabel != null ? targetLabel.text : "";
		}
		set
		{
			if (targetLabel != null)
			{
			    targetLabel.text = value;
			}
		}
	}

	public void init(onClickDelegate callback, Dict args = null, bool overrideDefaultCallback = false)
	{
		if (callback != null)
		{
			registerEventDelegate(callback, args);
		}
		if (!overrideDefaultCallback)
		{
			registerEventDelegate(onClick, args);
		}
	}

	
	public void init(ToggleManager manager, int index)
	{
		if (manager != null && index >= 0)
		{
			// If we are setting this up as a linked toggle button then set these;
			this.manager = manager;
			this.index = index;
		}
		registerEventDelegate(onClick);
	}

	public void onClick(Dict args = null)
	{	
		if (manager != null)
		{
			// If this toggle has a manager, then let it handle any turning on/off of the linked buttons.
			manager.toggle(this);
		}
		else
		{
			// Otherwise just toggle it on.
			setToggle(!isToggled);
		}
	}
	
	public void setToggle(bool toggle)
	{
		isToggled = toggle;
		if (toggle)
		{
			targetSprite.spriteName = toggleOnSprite;
		}
		else
		{
			targetSprite.spriteName = toggleOffSprite;
		}

		if (targetLabel != null)
		{
			toggleLabel(toggle);
		}
	}

	private void toggleLabel(bool isToggled)
	{
		switch (stylingMethod)
		{
			case TextStyling.STYLE:
				if (isToggled && toggleOnFontStyle != null)
				{
					targetLabel.fontMaterial = toggleOnFontStyle;
				}
				else if (!isToggled && toggleOffFontStyle != null)
				{
					targetLabel.fontMaterial = toggleOffFontStyle;
				}
				break;
			case TextStyling.COLOR:
				targetLabel.color = isToggled ? toggleOnFontColor : toggleOffFontColor;
				break;
			case TextStyling.HIDE:
				targetLabel.gameObject.SetActive(isToggled);
				break;
		}		
	}
}
