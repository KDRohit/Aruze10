using UnityEngine;
using System.Collections;

/// Added by Zynga - Mike Wod (Telos)
/// <summary>
/// Works in conjunction with UIButtonLabelTween to added color tweening
/// functionality to the effect color of a label.
/// </summary>
public class TweenLabelEffectColor : UITweener
{
	public Color from = Color.white;
	public Color to = Color.white;

	UILabel mLabel;

	/// <summary>
	/// Current effect color.
	/// </summary>
	
	public Color color
	{
		get
		{
			if (mLabel != null) return mLabel.effectColor;
			return Color.black;
		}
		set
		{
			if (mLabel != null) mLabel.effectColor = value;
		}
	}

	/// <summary>
	/// Find all needed components.
	/// </summary>
	
	void Awake ()
	{
		mLabel = GetComponentInChildren<UILabel>();
	}

	/// <summary>
	/// Interpolate and update the color.
	/// </summary>
	
	protected override void OnUpdate(float factor, bool isFinished) { color = Color.Lerp(from, to, factor); }
	
	/// <summary>
	/// Start the tweening operation.
	/// </summary>
	
	static public TweenLabelEffectColor Begin(GameObject go, float duration, Color color)
	{
		TweenLabelEffectColor comp = UITweener.Begin<TweenLabelEffectColor>(go, duration);
		comp.from = comp.color;
		comp.to = color;
		
		if (duration <= 0f)
		{
			comp.Sample(1f, true);
			comp.enabled = false;
		}
		return comp;
	}
}
