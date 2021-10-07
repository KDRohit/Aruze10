using UnityEngine;
using System.Collections;

/// Added by Zynga - Mike Wood (Telos)
/// <summary>
/// Works in conjuction with the existing UIButtonColor system, and the UILabelEffectColorTween class to add
/// the ability to tween the color effects of a label, as well as its tint color.
/// </summary>
public class UIButtonLabelColor : UIButtonColor
{
	public Color effectHover = new Color(0.6f, 1f, 0.2f, 1f);

	public Color effectPressed = Color.grey;

	protected Color mEffectColor;

	protected override void Init ()
	{
		base.Init();

		if (tweenTarget == null) tweenTarget = gameObject;
		UILabel label = tweenTarget.GetComponent<UILabel>();

		mEffectColor = label.effectColor;
	}

	public override void OnPress(bool isPressed)
	{
		base.OnPress(isPressed);

		if (enabled)
		{
			TweenLabelEffectColor.Begin(tweenTarget, duration, isPressed ? effectPressed : (UICamera.IsHighlighted(gameObject) ? effectHover : mEffectColor));
		}
	}
	
	public override void OnHover(bool isOver)
	{
		base.OnHover(isOver);

		if (enabled)
		{
			TweenLabelEffectColor.Begin(tweenTarget, duration, isOver ? effectHover : mEffectColor);
		}
	}
}
