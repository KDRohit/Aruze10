using UnityEngine;

/*
Class Name: ButtonColorExtended.cs
Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
Description: Extending UIButtonColor to be able to have a disabled color.
Feature-flow: Stick it on a button, hook it up to button handler, enjoy!
*/
public class ButtonColorExtended : MonoBehaviour
{
	// Target with a widget, renderer, or light that will have its color tweened.
	public GameObject tweenTarget;
	// Zynga/Todd: Added to allow multiple tween targets in a single script.	
	public GameObject[] otherTweenTargets;	

	public Color hover = new Color(0.6f, 1f, 0.2f, 1f);
	public Color pressed = Color.grey;
	public Color disabled = Color.grey;
	public float duration = 0.2f;

	protected Color mColor;
	protected bool mStarted = false;
	protected bool mHighlighted = false;

	private bool _isEnabled = true;
	public bool isEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			_isEnabled = value;
			setEnabledColoring();
		}
	}
	// UIButtonColor's default (starting) color. It's useful to be able to change it, just in case.
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

	void Start()
	{
		if (!mStarted)
		{
			Init();
			mStarted = true;
		}
	}

	protected void OnEnable()
	{
		if (mStarted && mHighlighted) OnHover(UICamera.IsHighlighted(gameObject));
	}

	protected void OnDisable()
	{
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

	public void setEnabledColoring()
	{
		if (enabled)
		{
			if (!mStarted) Start();
			targetTween(tweenTarget, isEnabled ? mColor : disabled);
			foreach (GameObject target in otherTweenTargets)
			{
				targetTween(target, isEnabled ? mColor : disabled);
			}
		}
	}
}
