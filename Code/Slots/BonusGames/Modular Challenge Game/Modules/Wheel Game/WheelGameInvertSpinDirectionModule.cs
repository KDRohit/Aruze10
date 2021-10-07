using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Class for reversing the spin directions of wheels 

Creation Date: 2/12/2018
Original Author: Scott Lepthien
*/
public class WheelGameInvertSpinDirectionModule : WheelGameModule 
{
// Hook for when you want to change the spin direction of the wheel
// hooks when a spin has been triggered but before anything has been calculated
	public override bool needsToExecuteOnOverrideSpinDirection(bool isCurrentSpinDirectionClockwise)
	{
		return true;
	}

	public override bool executeOnOverrideSpinDirection(bool isCurrentSpinDirectionClockwise)
	{
		// invert the spin direction
		return !isCurrentSpinDirectionClockwise;
	}
}
