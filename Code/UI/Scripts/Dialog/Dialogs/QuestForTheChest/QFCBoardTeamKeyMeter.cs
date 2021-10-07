using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/
namespace QuestForTheChest
{
	public class QFCBoardTeamKeyMeter : MonoBehaviour
	{
		[SerializeField] private UITwoWayMeterNGUI keyMeter;
		[SerializeField] private TextMeshPro meterLabel;

		private int startingKeyAmount;
		private float animationTime;
		private float totalAnimTime;

		public float init(int currentKeyAmount, int maxKeyAmount, bool doTween = false, float maxTweenDuration = 3.0f)
		{
			keyMeter.setState(currentKeyAmount, maxKeyAmount, doTween, maxTweenDuration);
			meterLabel.text = currentKeyAmount.ToString();
			return keyMeter.tweenDuration;
		}


		public float drainMeter(int maxKeyAmount, float maxTweenDuration = 3.0f)
		{
			startingKeyAmount = (int)keyMeter.currentValue;
			keyMeter.setState(0, maxKeyAmount, true, maxTweenDuration);
			animationTime = keyMeter.tweenDuration;
			totalAnimTime = animationTime;
			return animationTime;
		}


		private void Update()
		{
			if (animationTime > 0)
			{
				animationTime -= Time.deltaTime;
				if (animationTime < 0)
				{
					animationTime = 0;
				}

				float amount = startingKeyAmount * ((float) animationTime / (float) totalAnimTime);
				int integerAmount = System.Convert.ToInt32(amount);
				meterLabel.text = integerAmount.ToString();
			}
		}

		public long Value
		{
			get { return keyMeter.currentValue; }
		}
	}
}
