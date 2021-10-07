using UnityEngine;
using System.Collections;

/**
 A SFX that can be applied to any UIPanel to reduce it's alpha over a specified duration. 
 */
public class UIPanelReduceAlpha : Effect
{
	UIPanel panelToReduce; // the panel whose alpha should be reduced to 0
	float currentValue; // internal value for current displayed data
	float rateOfChange; // rate of change 
	float targetReduction; // the rate to reduce the alpha to
	float originalValue; // used only if the the panel oscillates back to its original value, this marks the starting point.
	
	bool finished = false;
	bool oscillate = false;

	public void initialize (UIPanel targetPanel, 
				float speedOfDecline = 0.001f,
				float valueToReduceTo = 0.0f,
				bool oscillateEffect = false
				) 
	{
		panelToReduce = targetPanel;
		rateOfChange = Mathf.Abs(speedOfDecline);
		targetReduction = valueToReduceTo;
		oscillate = oscillateEffect;
		//Sanity checking values for sensical behavior
		if (targetPanel == null)
		{
			Debug.LogError("ERROR: No panel detected on game object passed, or original panel was null!  This added effect will fail.");
			panelToReduce = null;
		}
		if (rateOfChange == 0.0f)
		{
			Debug.LogWarning ("WARNING: Specified decline velocity is 0, resulting in a lock! Setting a large value to have the effect be instantaneous and break the lock.");
			rateOfChange = 1.0f;
		}
		if (valueToReduceTo < 0.0f || valueToReduceTo > 1.0f || valueToReduceTo > panelToReduce.alpha)
		{
			Debug.LogError("ERROR: Value to reduce the alpha to is nonsensical given boundaries or passed start value!  This effect will not do anything.");
		}
		if (oscillate)
		{
			originalValue = panelToReduce.alpha;
		}
	}
	
	///Implementation of graphical update loop, linearly reducing alpha
	public override void updateEffect() 
	{
		if (panelToReduce.alpha == targetReduction)
		{
			if (oscillate)
			{
				panelToReduce.alpha = originalValue;
			}
			else
			{
				finished = true;
			}
		}
		else
		{
			panelToReduce.alpha -= rateOfChange;
			if(panelToReduce.alpha < targetReduction)
			{
				panelToReduce.alpha = targetReduction;
			}
		}
	}
	
	public override bool isFinished()
	{
		if (panelToReduce == null)
		{
			return true;
		}
		return finished;
	}

}

