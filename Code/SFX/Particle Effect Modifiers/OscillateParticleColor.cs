using UnityEngine;
using System.Collections;

/**
 A particle system effect that oscillates the color of the particle system between two provided colors
 */
public class OscillateParticleColor : Effect
{
	public ParticleSystem particlesToModify; // the particle system containing the shader to be operated on
	public float currentValue; // internal value for current displayed data
	public float rateOfChange; // rate of change 
	public int directionOfChange; // direction of increase.  1 for positive, -1 for negative
	public Color colorOne; // the left color to interpolate between
	public Color colorTwo; // the right color to interpolate between
		
	public void initialize( ParticleSystem passedParticles, 
								Color leftColor,
								Color rightColor,
								float startingValue = 0.0f, 
								float speedOfOscillation = 0.001f, 
								int initialDirection = 1
								)
	{
		particlesToModify = passedParticles;
		currentValue = startingValue;
		rateOfChange = Mathf.Abs(speedOfOscillation);
		directionOfChange = initialDirection;
		colorOne = leftColor;
		colorTwo = rightColor;
		
		//Sanity checking values for sensical behavior
		if (particlesToModify == null)
		{
			Debug.LogWarning("WARNING: No particle system detected on game object passed, or original particle system was null!  This added effect will fail.");
			particlesToModify = null;
		}
		if (directionOfChange != -1 && directionOfChange != 1) 
		{
			Debug.LogWarning ("WARNING: Direction of color change was not -1 or 1!  Converting value, but please fix for clarity and accuracy.");
			if (directionOfChange < 0) 
			{
				directionOfChange = -1;
			}
			else
			{
				directionOfChange = 1;
			}
		}
		
		if (rateOfChange == 0.0f)
		{
			Debug.LogWarning ("WARNING: Specified transition velocity is 0, resulting in a lock! Setting a large value to have the effect be instantaneous and break the lock.");
			rateOfChange = 1.0f;
		}
	}
	
	/// Update the current particle system with a color oscillation
	public override void updateEffect ()
	{
		if (particlesToModify != null)
		{
			ParticleSystem.MainModule particleSystemMainModule = particlesToModify.main;
			particleSystemMainModule.startColor = Color.Lerp(colorOne, colorTwo, currentValue);
			currentValue += (rateOfChange * directionOfChange);
			if (currentValue >= 1.0f) 
			{
				currentValue = 1.0f;
				directionOfChange *= -1;
			}
			else if (currentValue <= 0.0f)
			{
				currentValue = 0.0f;
				directionOfChange *= -1;
			}
		}
	}
		
	/// isFinished always returns false for a properly declared particle system. 
	public override bool isFinished ()
	{
		if (particlesToModify == null)
		{
			return true;
		}
		return false;
	}
}

