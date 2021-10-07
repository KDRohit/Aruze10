using UnityEngine;
using System.Collections;

/**
 A particle system effect that oscillates the color of a text label between two or more provided colors
 */
public class OscillateTextColor : Effect
{
	LabelWrapper labelToModify; // the particle system containing the shader to be operated on
	float currentValue; // internal value for current displayed data
	float rateOfChange; // rate of change 
	private Color[] colorsToSwitchBetween; // A list of colors to alternate between, going from entry 0 to entry X and wrapping around
	private int currentColorIndex; // The step of where we are in the list

		
	public void initialize( LabelWrapper passedLabel, 
								Color[] colors,
								float speedOfOscillation = 0.001f
								)
	{
		labelToModify = passedLabel;
		rateOfChange = Mathf.Abs(speedOfOscillation);
		colorsToSwitchBetween = colors;
		currentValue = 0;
		currentColorIndex = 0;
		
		//Sanity checking values for sensical behavior
		if (rateOfChange == 0.0f)
		{
			Debug.LogWarning ("WARNING: Specified transition velocity is 0, resulting in a lock! Setting a large value to have the effect be instantaneous and break the lock.");
			rateOfChange = 1.0f;
		}
		
		if (colorsToSwitchBetween.Length < 2)
		{
			Debug.LogWarning ("WARNING: The number of colors passed does not meet the minimum requirements.  At least two colors must be passed.  Creating a default ugly color scheme.");
			colorsToSwitchBetween = new Color[] {Color.black, Color.white};
		}

		readyToUpdate = true;
	}
	
	/// Update the current text label with a color oscillation, alternating between colors in an array.
	public override void updateEffect ()
	{
		Color colorOne = colorsToSwitchBetween[currentColorIndex];
		Color colorTwo = Color.white;
		if ((currentColorIndex + 1) == colorsToSwitchBetween.Length)
		{
			colorTwo = colorsToSwitchBetween[0];
		}
		// If not at the end of the array, grab the next value
		else
		{
			colorTwo = colorsToSwitchBetween[currentColorIndex + 1];
		}
		if (labelToModify != null)
		{
			labelToModify.color = Color.Lerp(colorOne, colorTwo, currentValue);
			currentValue += (rateOfChange);
			//Reset if we have gone above the current maximum and move to the next color
			if (currentValue > 1.0f) 
			{
				currentValue = 0.0f;
				currentColorIndex++;
				if (currentColorIndex == colorsToSwitchBetween.Length)
				{
					currentColorIndex = 0;
				}
			}
		}
	}
		
	/// isFinished always returns false for a properly and still existant UILabel. 
	public override bool isFinished ()
	{
		if (labelToModify == null)
		{
			return true;
		}
		return false;
	}
}

