//----------------------------------------------
//			NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Simple example script of how a button can be colored when the mouse hovers over it or it gets pressed.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Button Color")]
public class UIButtonColor : TICoroutineMonoBehaviour
{
	/// <summary>
	/// Target with a widget, renderer, or light that will have its color tweened.
	/// </summary>

	public GameObject tweenTarget;
	public GameObject[] otherTweenTargets;	// Zynga/Todd: Added to allow multiple tween targets in a single script.

	/// <summary>
	/// Color to apply on hover event (mouse only).
	/// </summary>

	public Color hover = new Color(0.6f, 1f, 0.2f, 1f);

	/// <summary>
	/// Color to apply on the pressed event.
	/// </summary>

	public Color pressed = Color.grey;

	/// <summary>
	/// Duration of the tween process.
	/// </summary>

	public float duration = 0.2f;

	protected Color mColor;
	protected bool mStarted = false;
	protected bool mHighlighted = false;

	/// <summary>
	/// UIButtonColor's default (starting) color. It's useful to be able to change it, just in case.
	/// </summary>

	public Color defaultColor
	{
		get
		{
			Start();
			return mColor;
		}
		set
		{
			Start();
			mColor = value;
		}
	}

	void Start ()
	{
		if (!mStarted)
		{
			Init();
			mStarted = true;
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		
		if (mStarted && mHighlighted) OnHover(UICamera.IsHighlighted(gameObject));
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		
		if (mStarted)
		{
			setTargetDisabledColor(tweenTarget);
			
			foreach (GameObject target in otherTweenTargets)
			{
				setTargetDisabledColor(target);
			}
		}
	}
	
	private void setTargetDisabledColor(GameObject target)
	{
		if (target == null)
		{
			return;
		}
		
		TweenColor tc = tweenTarget.GetComponent<TweenColor>();

		if (tc != null)
		{
			tc.color = mColor;
			tc.enabled = false;
		}
	}

	// Added by Zynga - Mike Wood (Telos) made function virtual to allow it to be overwridden.
	protected virtual void Init ()
	{
		if (tweenTarget == null) tweenTarget = gameObject;
		UIWidget widget = tweenTarget.GetComponent<UIWidget>();

		if (widget != null)
		{
			mColor = widget.color;
		}
		else
		{
			// Added by Todd Gillissie / Zynga... TextMeshPro support.
			TMPro.TextMeshPro tmpro = tweenTarget.GetComponent<TMPro.TextMeshPro>();
			
			if (tmpro != null)
			{
				mColor = tmpro.color;
			}
			else
			{
				Renderer ren = tweenTarget.GetComponent<Renderer>();

				if (ren != null && ren.material.HasProperty("_Color"))
				{
					mColor = ren.material.color;
				}
				else
				{
					Light lt = tweenTarget.GetComponent<Light>();

					if (lt != null)
					{
						mColor = lt.color;
					}
					else
					{
						Debug.LogWarning(NGUITools.GetHierarchy(gameObject) + " has nothing for UIButtonColor to color", this);
						enabled = false;
					}
				}
			}
		}

		hover = mColor;	// Zynga / Todd - Since we don't use "hover" state on mobile, set the hover color to the same as the default color.
		
		OnEnable();
	}

	public virtual void OnPress (bool isPressed)
	{
		if (enabled)
		{
			if (!mStarted) Start();
			targetTween(tweenTarget, isPressed ? pressed : (UICamera.IsHighlighted(gameObject) ? hover : mColor));
			foreach (GameObject target in otherTweenTargets)
			{
				targetTween(target, isPressed ? pressed : (UICamera.IsHighlighted(gameObject) ? hover : mColor));
			}
		}
	}
	
	private void targetTween(GameObject target, Color color)
	{
		if (target == null)
		{
			return;
		}
		TweenColor.Begin(target, duration, color);
	}

	public virtual void OnHover (bool isOver)
	{
		if (enabled)
		{
			if (!mStarted) Start();
			targetTween(tweenTarget, isOver ? hover : mColor);
			foreach (GameObject target in otherTweenTargets)
			{
				targetTween(target, isOver ? hover : mColor);
			}
			mHighlighted = isOver;
		}
	}
}
