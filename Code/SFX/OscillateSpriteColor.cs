using UnityEngine;
using System.Collections;

/**
 A particle system effect that oscillates the color of a text label between two or more provided colors
 */
public class OscillateSpriteColor : Effect
{
	UISprite spriteToModify; // the particle system containing the shader to be operated on
	float rateOfChange; // rate of change 
	private Color[] colorsToSwitchBetween; // A list of colors to alternate between, going from entry 0 to entry X and wrapping around
	private int currentColorIndex; // The step of where we are in the list
	
	
	public void initialize( UISprite passedSprite, 
	                       Color[] colors,
	                       float speedOfOscillation = 0.001f
	                       )
	{
		spriteToModify = passedSprite;
		rateOfChange = Mathf.Abs(speedOfOscillation);
		colorsToSwitchBetween = colors;
		currentColorIndex = 0;
		
		//Sanity checking values for sensical behavior
		if (spriteToModify == null)
		{
			Debug.LogWarning("WARNING: No particle system detected on game object passed, or original particle system was null!  This added effect will fail.");
			spriteToModify = null;
		}
		
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
		StartCoroutine(oscillateColors());
	}

	private IEnumerator oscillateColors()
	{
		while (true)
		{
			iTween.ValueTo(this.gameObject, iTween.Hash("from", 0.0f, "to", 1.0f, "time", rateOfChange, "easetype", iTween.EaseType.linear, "onupdate", "updateValue"));
			yield return new TIWaitForSeconds(rateOfChange);

			currentColorIndex++;
			if (currentColorIndex == colorsToSwitchBetween.Length)
			{
				currentColorIndex = 0;
			}
		}
	}
	
	public void updateValue(float value)
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

		if (spriteToModify != null)
		{
			spriteToModify.color = Color.Lerp(colorOne, colorTwo, value);
		}
	}
	
	/// Update the current text label with a color oscillation, alternating between colors in an array.
	/**public override void updateEffect ()
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
		if (spriteToModify != null)
		{
			spriteToModify.color = Color.Lerp(colorOne, colorTwo, currentValue);
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
	}*/
	
	/// isFinished always returns false for a properly and still existant UILabel. 
	public override bool isFinished ()
	{
		if (spriteToModify == null)
		{
			return true;
		}
		return false;
	}
}

