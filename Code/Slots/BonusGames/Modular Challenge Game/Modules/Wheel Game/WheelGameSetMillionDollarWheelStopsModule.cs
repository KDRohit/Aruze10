using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * A module for setting the wheel angle stops for wheel games that have a 'Million Dollar wheel.'
 * A wheel that has equidistant big slices that are 2x the size of the small slices. 
 */

public class WheelGameSetMillionDollarWheelStopsModule : WheelGameSetCustomWheelStopsModule
{
	[SerializeField] private float DEGREES_PER_SLICE = 0.0f;
	[SerializeField] private int NUMBER_BIG_SLICES = 0;

	private float getAngleForWheelStopID(int wheelStopID)
	{
		float extraDegreesFromPreviousBigSlices = (wheelStopID / NUMBER_BIG_SLICES) * DEGREES_PER_SLICE; // The big slices are 2x as big as the small slices
		float angle = wheelStopID * DEGREES_PER_SLICE + extraDegreesFromPreviousBigSlices;
		if (wheelStopID % NUMBER_BIG_SLICES != 0)
		{
			angle += DEGREES_PER_SLICE / 2; // We want to stop in the middle of the slice.
		}
		return angle;
	}

	///// Editor Update Loops ///////
	private void OnValidate()
	{
		for (int i = 0; i < customAngles.Count; i++)
		{
			customAngles[i] = getAngleForWheelStopID(i);
		}
	}
}
