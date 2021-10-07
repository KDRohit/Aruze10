using UnityEngine;
using System.Collections;

/**
 An effect to reduce the saturation to -0.1f over time for a shader that includes the saturation property
 */
public class ToGrey : Effect
{
	MeshRenderer meshWithShader; ///< the rendering mesh containing the shader to be operated on
	float currentValue; ///< internal value for current displayed data
	float rateOfChange; ///< rate of change 
	
	bool finished = false;

	public void initialize (MeshRenderer materialWithHue, 
				float startingValue = 0.0f, 
				float speedOfDecline = 0.001f
				) 
	{
		meshWithShader = materialWithHue;
		currentValue = startingValue;
		rateOfChange = Mathf.Abs(speedOfDecline);
		
		//Sanity checking values for sensical behavior
		if (materialWithHue == null)
		{
			Debug.LogWarning("WARNING: No mesh renderer detected on game object passed, or original mesh renderer was null!  This added effect will fail.");
			meshWithShader = null;
		}
		if (currentValue <= -0.1f)
		{
			Debug.LogWarning ("WARNING: Specified starting value is outside the target point! Setting it to the minimum barrier and terminating effect.");
			currentValue = -0.1f;
		}
		if (speedOfDecline == 0.0f)
		{
			Debug.LogWarning ("WARNING: Specified decline velocity is 0, resulting in a lock! Setting a large value to have the effect be instantaneous and break the lock.");
			speedOfDecline = 1.0f;
		}
	}
	
	///Implementation of graphical update loop, following a linear saturation progression downwards to -0.1f
	public override void updateEffect() 
	{
		bool hasSaturation = false;
		if (meshWithShader != null) 
		{
			foreach (Material itemToInspect in meshWithShader.materials) 
			{
				if (itemToInspect.HasProperty("_Saturation")) 
				{
					hasSaturation = true;
					if (currentValue <= -1.0f)
					{
						//When we are here, we are finished. Update one last time.
						currentValue = -1.0f;
						finished = true;
					}
					itemToInspect.SetFloat("_Saturation", currentValue);
					currentValue -= rateOfChange;
				}
			}
		}
		finished |= !hasSaturation;
	}
	
	public override bool isFinished()
	{
		if (meshWithShader == null)
		{
			return true;
		}
		return finished;
	}
}

