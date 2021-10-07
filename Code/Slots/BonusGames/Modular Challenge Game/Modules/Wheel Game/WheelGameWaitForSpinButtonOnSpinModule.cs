using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
For use in wheel game with more than one wheel where you want the wheels to start spinning at the same time and
one of the wheels is using a WheelGameSpinButtonModule to play some started animations.

Creation Date: 4/4/2018
Original Author: Scott Lepthien
*/
public class WheelGameWaitForSpinButtonOnSpinModule : WheelGameModule 
{
	[SerializeField] private WheelGameSpinButtonModule spinButtonModule = null;

	public override bool needsToExecuteOnSpin()
	{
		return spinButtonModule != null;
	}

	// stop the idle & play the button animation on spin started
	public override IEnumerator executeOnSpin()
	{
		// wait until the linked WheelGameSpinButtonModule has finished its executeOnSpin function
		while (!spinButtonModule.isExecuteOnSpinComplete)
		{
			yield return null;
		}
	}
}
