//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Similar to UIButtonColor, but adds a 'disabled' state based on whether the collider is enabled or not.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Button")]
public class UIButton : UIButtonColor
{
	/// <summary>
	/// Color that will be applied when the button is disabled.
	/// </summary>

	public Color disabledColor = Color.grey;

	/// <summary>
	/// If the collider is disabled, assume the disabled color.
	/// </summary>

	protected override void OnEnable ()
	{
		if (isEnabled) base.OnEnable();
		else UpdateColor(false, true);
	}

	public override void OnHover (bool isOver) { if (isEnabled) base.OnHover(isOver); }
	public override void OnPress (bool isPressed) { if (isEnabled) base.OnPress(isPressed); }

	/// <summary>
	/// Whether the button should be enabled.
	/// </summary>

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
				UpdateColor(value, false);
			}
		}
	}

	/// <summary>
	/// Update the button's color to either enabled or disabled state.
	/// </summary>

	public void UpdateColor (bool shouldBeEnabled, bool immediate)
	{
		if (tweenTarget)
		{
			if (!mStarted)
			{
				mStarted = true;
				Init();
			}

			Color color = shouldBeEnabled ? defaultColor : disabledColor;

			setTargetDisabledColor(tweenTarget, color, immediate);
			
			foreach (GameObject target in otherTweenTargets)
			{
				setTargetDisabledColor(target, color, immediate);
			}
		}
	}
	
	private void setTargetDisabledColor(GameObject target, Color color, bool immediate)
	{
		if (target == null)
		{
			return;
		}
		
		TweenColor tc = TweenColor.Begin(target, 0.15f, color);

		if (immediate)
		{
			tc.color = color;
			tc.enabled = false;
		}
	}
}
