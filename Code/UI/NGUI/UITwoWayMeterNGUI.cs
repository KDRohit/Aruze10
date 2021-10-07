using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
overriding the behaviour of the ui meter to be able to tween to 0 with a non-zero time duration
*/
public class UITwoWayMeterNGUI : UIMeterNGUI
{

	public override long currentValue
	{
		get { return base.currentValue; }

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

				tweenDuration = tweenChanges ? getTweenDuration(scale) : 0; //only calculate duration if we need to

				// if meterbar is below a min width, just hide it completely, because NGUIMath::CalculateRelativeInnerBounds
				// will not scale sliced sprites below the size of the sliced border,
				// but we need bar to empty if meter is at 0, instead of a small bar of just 'border'.
				// TODO: override NGUI's scale-minimum for sliced sprites so progress meter can scale properly below sliced-sprite border size
				if (scale < 1.0f && !alwaysDisplay)
				{
					StartCoroutine(turnOffAfterTime(gameObject, tweenDuration));
				}
				else
				{
					gameObject.SetActive(true);
				}

				updateTransform(scale, tweenDuration);
			}
		}
	}

	private static IEnumerator turnOffAfterTime(GameObject obj, float fTime)
	{
		yield return new WaitForSeconds(fTime);
		obj.SetActive(false);
	}

}
