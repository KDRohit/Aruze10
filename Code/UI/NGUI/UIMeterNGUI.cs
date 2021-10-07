using UnityEngine;
using System.Collections;

/**
A simple extension of the NGUI UIFilledSprite to be used as a meter where the maximum value is set once
so only the current value needs to be updated in order to change the meter,
and the current value can be specified as an actual value rather than a normalized value.
*/

public class UIMeterNGUI : TICoroutineMonoBehaviour
{
	public enum MeterOrientation
	{
		Horizontal,
		Vertical
	}

	private const float DEFAULT_MAX_TWEEN_DURATION = 3.0f;
	protected const float MIN_TWEEN_DURATION = 1.0f;
	protected float maxTweenDuration = DEFAULT_MAX_TWEEN_DURATION;

	public UISprite sprite;
	public long maximumValue;           // The potential maximum value of the meter.
	public MeterOrientation orientation;

	public bool tweenChanges = false;   // Set to true if you'd like the changes in the meter to be tweened instead of jumping immediately.
										// Warning: Leave this false if the value of the meter is changed very frequently, such as the loading screen meter.

	protected float axisMaxScale = 0;		// Used if the sprite is sliced instead of filled. Sprite must be at full meter width before runtime.
	public bool alwaysDisplay = false;

	// abstracts implementation detail that maxValue must be set before curValue
	public void setState(long curValue, long maxValue, bool doTween=false, float setMaxTweenDuration=DEFAULT_MAX_TWEEN_DURATION)
	{
		maxTweenDuration = setMaxTweenDuration;
		tweenChanges = doTween;
		maximumValue = maxValue;    // this MUST be set before currentValue to avoid a /0 exception
		currentValue = curValue;
	}

	public void setSpriteScale(float newScale)
	{
		if (newScale == 0)
		{
			return;
		}
		
		switch (orientation)
		{
			case MeterOrientation.Vertical:
				axisMaxScale = newScale;
				CommonTransform.setHeight(sprite.transform, 0.0f);
				break;
					
			default:
				axisMaxScale = newScale;
				CommonTransform.setWidth(sprite.transform, 0.0f);
				break;
		}
	}

	public float tweenDuration { get; protected set; }
	
	public virtual long currentValue
	{
		get { return _currentValue; }
		
		set
		{
			_currentValue = value;

			checkForZeroScale();

			if (_currentValue < 0)        
			{
				_currentValue = 0;	// sanity check
			}
			
			if (maximumValue == 0)
			{
				Debug.LogWarning("maximumValue is 0 in UIMeterNGUI. This shouldn't be allowed to happen. Check your data.");
				sprite.fillAmount = 0.0f;
			}
			else
			{
				sprite.fillAmount = (float)_currentValue / maximumValue;
			}

			if (sprite.type == UISprite.Type.Sliced)
			{
				float scale = sprite.fillAmount * axisMaxScale;

				// The minimum width is .5f because some Android devices apparently have
				// problems displaying sliced sprites properly when the scale is 0 in at least one axis.
				scale = Mathf.Max(0.5f, scale);

				// if meterbar is below a min width, just hide it completely, because NGUIMath::CalculateRelativeInnerBounds
				// will not scale sliced sprites below the size of the sliced border, 
				// but we need bar to empty if meter is at 0, instead of a small bar of just 'border'.
				// TODO: override NGUI's scale-minimum for sliced sprites so progress meter can scale properly below sliced-sprite border size
				gameObject.SetActive(alwaysDisplay || scale > 1.0f);

				
				if (scale > 1.0f)
				{
					float duration = tweenChanges ? getTweenDuration(scale) : 0; //only calculate duration if we need to
					updateTransform(scale, duration);
				}
				else
				{
					tweenDuration = 0;
				}
			}
		}
	}

	protected void checkForZeroScale()
	{
		if (axisMaxScale == 0 && sprite.type == UISprite.Type.Sliced)
		{
			switch (orientation)
			{
				case MeterOrientation.Vertical:
					axisMaxScale = sprite.transform.localScale.y;
					CommonTransform.setHeight(sprite.transform, 0.0f);
					break;
					
				default:
					axisMaxScale = sprite.transform.localScale.x;
					CommonTransform.setWidth(sprite.transform, 0.0f);
					break;
			}
		}
	}

	protected void updateTransform(float scale, float tweenDuration)
	{
		if (tweenChanges)
		{
			string parameter = "x";
			switch (orientation)
			{
				case MeterOrientation.Vertical:
					parameter = "y";
					break;
							
				default:
					parameter = "x";
					break;
			}
			iTween.ScaleTo(sprite.gameObject, iTween.Hash(parameter, scale, "time", tweenDuration, "easetype", iTween.EaseType.easeOutQuad));
						
		}
		else
		{
			tweenDuration = 0;
			switch (orientation)
			{
				case MeterOrientation.Vertical:
					CommonTransform.setHeight(sprite.transform, scale);
					break;
							
				default:
					CommonTransform.setWidth(sprite.transform, scale);
					break;
			}
		}
	}
	protected long _currentValue = 0;
	
	// Stole this formula from XP meter tweening for SIR.
	protected float getTweenDuration(float scale)
	{
		float t;

		switch (orientation)
		{
			case MeterOrientation.Vertical:
				t = Mathf.Abs(sprite.transform.localScale.y - scale) / axisMaxScale;
				break;
			
			default:
				t = Mathf.Abs(sprite.transform.localScale.x - scale) / axisMaxScale;
				break;
		}

		float duration = MIN_TWEEN_DURATION * (1.0f - t) + maxTweenDuration * t;

		return duration;
	}

}
