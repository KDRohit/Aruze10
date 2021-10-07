using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Class for getting the stop angle for a wheel that spins and stops on a specific location based
on the number of spins the user will be given in the freespin game (read from a field added to the root bonus outcome)

Creation Date: 2/12/2018
Original Author: Scott Lepthien
*/
public class WheelGameCustomAngleFromFreespinSpinCountModule : WheelGameModule 
{
	[SerializeField] private SpinCountWheelIndex[] spinCountWheelIndices;

	[System.Serializable]
	public class SpinCountWheelIndex
	{
		public int spinsWonCount;
		public int wheelIndex;
	} 

	public override bool needsToSetCustomAngleForFinalStop(ModularChallengeGameOutcomeRound currentRound)
	{
		return true;
	}

	public override float executeSetCustomAngleForFinalStop(ModularChallengeGameOutcomeRound currentRound)
	{
		// Look at the root bonus outcome so we can determine which major symbol we should rotate the wheel to
		SpinCountWheelIndex spinCountIndexEntry = getWheelIndexEntryForSpinCount(FreeSpinGame.instance.payTableFreeSpinCount);

		if (spinCountIndexEntry != null)
		{
			return (360.0f / wheelParent.getNumberOfWheelSlices()) * spinCountIndexEntry.wheelIndex;
		}
		else
		{
			return -1;
		}
	}

	private SpinCountWheelIndex getWheelIndexEntryForSpinCount(int spinsWonCount)
	{
		for (int i = 0; i < spinCountWheelIndices.Length; i++)
		{
			if (spinsWonCount == spinCountWheelIndices[i].spinsWonCount)
			{
				return spinCountWheelIndices[i];
			}
		}

		Debug.LogError("WheelGameCustomAngleFromFreespinSpinCountModule.getWheelIndexEntryForSpinCount() - Unable to find entry for spinsWonCount = " + spinsWonCount);
		return null;
	}
}
