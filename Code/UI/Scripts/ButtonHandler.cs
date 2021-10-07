using UnityEngine;
using System.Collections;
using TMPro;

/*
  Class: ButtonHandler
  Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
  Description: A simple class to setup a button to our coding standards. Adds the required scripts and
  sets up their values properly. If there is more than one sprite beneath the parent object you put this on, 
  you may need to change that link manually.
  This class also handles setting every UI Script on it to be enabled/disabled when you set it's enabled value.

  ** Click delegate has been moved to ClickHandler
*/

[RequireComponent(typeof(UIButtonScale))]
[RequireComponent(typeof(BoxCollider))]
[ExecuteInEditMode]
public class ButtonHandler : ClickHandler
{
	public UIButtonScale buttonScale;
	public UIButton button;
	public BoxCollider spriteCollider;
	public UISprite sprite;
	public UISprite underlay;
	public TextMeshPro label;
	public LabelWrapperComponent labelWrapperComponent;
	public ButtonColorExtended[] colors;


	private bool _enabled;
	public new bool enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			_enabled = value;
			if (button != null)
			{
				button.isEnabled = _enabled;
			}


			spriteCollider.enabled = _enabled;
			buttonScale.enabled = _enabled;

			if (colors != null)
			{
				for (int i = 0; i < colors.Length; i++)
				{
					colors[i].isEnabled = value;
				}
			}
			base.isEnabled = value;
		}
	}

	// Sets/Gets the TMPro label text;
	public string text
	{
		get
		{
			if (labelWrapperComponent != null)
			{
				return labelWrapperComponent.text;
			}

			if (label != null)
			{
				return label.text;
			}
			
			return null;
		}
		set
		{
			if (label != null)
			{
				SafeSet.labelText(label, value);	
			}
			else if (labelWrapperComponent != null)
			{
				SafeSet.labelText(labelWrapperComponent.labelWrapper, value);
			}
		}
	}

	public void setAllAlpha(float alpha)
	{
		Color spriteColor;
		Color labelColor;
		Color underlayColor;

		if (sprite != null)
		{
			spriteColor = sprite.color;
			spriteColor.a = alpha;
			sprite.color = spriteColor;
		}
		if (underlay != null)
		{
			underlayColor = underlay.color;
			underlayColor.a = alpha;
			underlay.color = underlayColor;
		}
		if (label != null)
		{
			labelColor = label.color;
			labelColor.a = alpha;
			label.color = labelColor;
		}
	}

	// Supporting this GameObject call to make conversion easier.
	public void SetActive(bool isEnabled)
	{
		enabled = isEnabled;
	}
}
