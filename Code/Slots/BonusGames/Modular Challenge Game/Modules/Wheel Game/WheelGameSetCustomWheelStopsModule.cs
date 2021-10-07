using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * A module for setting the wheel angle stops for each wheel index. Useful if you need to override what's normally calculated (360.0f / NUMBER_OF_SLICES)
 */
public class WheelGameSetCustomWheelStopsModule : WheelGameModule
{
	[SerializeField] protected List<float> customAngles;

	public override bool needsToSetCustomAngleForFinalStop(ModularChallengeGameOutcomeRound currentRound)
	{
		return true;
	}

	public override float executeSetCustomAngleForFinalStop(ModularChallengeGameOutcomeRound currentRound)
	{
		ModularChallengeGameOutcomeEntry modularChallengeGameOutcome = currentRound.entries[0];
		if (modularChallengeGameOutcome.wheelWinIndex < customAngles.Count && modularChallengeGameOutcome.wheelWinIndex >= 0)
		{
			return customAngles[modularChallengeGameOutcome.wheelWinIndex];
		}
		else
		{
			Debug.LogErrorFormat("modularChallengeGameOutcome.wheelWinIndex ({0}) is out of range in customAngles ({1})",
				modularChallengeGameOutcome.wheelWinIndex,
				customAngles.Count);
			return -1;
		}
	}

	public override bool needsToExecuteOnNumberOfWheelSlicesChanged(int newSize)
	{
		return true;
	}

	public override void executeOnNumberOfWheelSlicesChanged(int newSize)
	{
		while (newSize >= customAngles.Count)
		{
			customAngles.Add(0); // Add an element to the end of the list.
		}
		while (newSize < customAngles.Count)
		{
			customAngles.RemoveAt(customAngles.Count - 1); // Remove the last element.
		}
	}
}
