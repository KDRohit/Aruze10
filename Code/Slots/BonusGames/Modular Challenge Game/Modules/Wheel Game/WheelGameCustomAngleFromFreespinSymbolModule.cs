using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Class for getting the stop angle for a wheel that spins and stops on a specific location based
on the name of the symbol that the freespin game is featuring (derived from the freespins bonus name)

Creation Date: 2/12/2018
Original Author: Scott Lepthien
*/
public class WheelGameCustomAngleFromFreespinSymbolModule : WheelGameModule 
{
	[SerializeField] private SymbolWheelIndex[] symbolWheelIndices;

	[System.Serializable]
	public class SymbolWheelIndex
	{
		public string symbolName;
		public int wheelIndex;
	} 

	public override bool needsToSetCustomAngleForFinalStop(ModularChallengeGameOutcomeRound currentRound)
	{
		return true;
	}

	public override float executeSetCustomAngleForFinalStop(ModularChallengeGameOutcomeRound currentRound)
	{
		// Look at the root bonus outcome so we can determine which major symbol we should rotate the wheel to
		SymbolWheelIndex symbolIndexEntry = getWheelIndexEntryForBonusName(BonusGameManager.instance.bonusGameName);

		if (symbolIndexEntry != null)
		{
			return (360.0f / wheelParent.getNumberOfWheelSlices()) * symbolIndexEntry.wheelIndex;
		}
		else
		{
			return -1;
		}
	}

	private SymbolWheelIndex getWheelIndexEntryForBonusName(string bonusName)
	{
		for (int i = 0; i < symbolWheelIndices.Length; i++)
		{
			if (bonusName.Contains(symbolWheelIndices[i].symbolName))
			{
				return symbolWheelIndices[i];
			}
		}

		Debug.LogError("WheelGameCustomAngleFromFreespinSymbolModule.getWheelIndexEntryForBonusName() - Unable to find entry for bonusName = " + bonusName);
		return null;
	}
}
