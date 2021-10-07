using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TumbleSlotEngine : SlotEngine
{
	public HashSet<SlotSymbol> previousWinningSymbols;		// Keeps track of the winning symbols that need to be cleared out before the next tumble

	public TumbleSlotEngine(ReelGame reelGame, string freeSpinsPaytableKey = "") : base(reelGame, freeSpinsPaytableKey)
	{
	}

	public override void setReelSetData(ReelSetData reelSetData, GameObject[] reelRoots, Dictionary<string, string> normalReplacementSymbolMap, Dictionary<string, string> megaReplacementSymbolMap)
	{
		this.reelSetData = reelSetData;
		SlotReel[] reelArray = getReelArray();

		if (reelArray == null)
		{
			if (reelRoots != null)
			{
				// Set the reel roots for the game.
				this.reelRoots = reelRoots;
			}

			// Set the reelArray to tumble reels
			reelArray = new TumbleReel[reelSetData.reelDataList.Count];
			setReelArray(reelArray);

			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				reelArray[reelIndex] = new TumbleReel(_reelGame);
				reelArray[reelIndex].setReelDataWithoutRefresh(reelSetData.reelDataList[reelIndex], reelIndex + 1);

				// control if we want symbols to force layering
				reelArray[reelIndex].isLayeringOverlappingSymbols = _reelGame.isLayeringOverlappingSymbols;
				reelArray[reelIndex].setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: false);
			}

			// refresh all the reels with the data that was loaded above
			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				reelArray[reelIndex].refreshReelWithReelData();
			}

			if (_reelGame.isLayeringOverlappingSymbols)
			{
				forceUpdatedRenderQueueLayering();
			}

			isStopOrderDone = new bool[_reelGame.stopOrder.Length];
			resetReelStoppedFlags();

		}
		else
		{
			// Reset all the flags which track if the symbol position for a reel has been calculated, this will be used to ensure linked reels remained lined up
			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				reelArray[reelIndex].resetIsRefreshedPositionSet();
			}

			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				int reelID = reelIndex + 1;
				if (reelSetData.isIndependentReels)
				{
					reelID = reelSetData.reelDataList[reelIndex].reelID; 
				}
				reelArray[reelIndex].setReelDataWithoutRefresh(reelSetData.reelDataList[reelIndex], reelID);
			}

			// refresh all the reels with the data that was loaded above
			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				reelArray[reelIndex].refreshReelWithReelData();
			}

			resetReelStoppedFlags();
		}

		// After we swap out and change the reels, update the list of reels linked through the reel data
		updateDataLinkedReelList();

#if UNITY_EDITOR
		Debug.Log("Symbols used by engine: " + getSymbolList());
#endif
	}

	public override void setOutcome(SlotOutcome slotOutcome)
	{
		_slotOutcome = slotOutcome;

		// Add tumbleOutcomes received from data to reevaluation spins
		_reelGame.reevaluationSpins = _slotOutcome.getTumbleOutcomesAsSlotOutcomes();
		_reelGame.reevaluationSpinsRemaining = _reelGame.reevaluationSpins.Count;

		updateReelsWithOutcome(_slotOutcome);
	}

	public override void spinReevaluatedReels(SlotOutcome spinData)
	{
		previousWinningSymbols = getPreviousWinningSymbols();

		isReevaluationSpin = true;
		_slotOutcome = spinData;
		updateReelsWithOutcome(_slotOutcome);
		resetSlamStop();

		SlotReel[] reelArray = getReelArray();

		// reset which reels have already been stopped since they should all be spinnin now
		resetReelStoppedFlags();

		// Set reel state to Tumbling when spinning reevaluation(tumble) outcomes
		for (int i = 0; i < reelArray.Length; i++)
		{
			(reelArray[i] as TumbleReel).setReelTumbling();
		}
	}

	public override int getStopIndexForReel(SlotReel reel)
	{
		if (reel != null && reel.reelID - 1 < _reelStops.Length)
		{
			return _reelStops[reel.reelID - 1];
		}
		return -1;
	}

	private HashSet<SlotSymbol> getPreviousWinningSymbols()
	{
		SlotOutcome previousOutcome;
		int currentReevaluationSpinIndex = _reelGame.reevaluationSpins.Count - _reelGame.reevaluationSpinsRemaining;

		if (currentReevaluationSpinIndex > 0)
		{
			previousOutcome = _reelGame.reevaluationSpins[currentReevaluationSpinIndex - 1];
		}
		else
		{
			previousOutcome = _reelGame.outcome;
		}

		HashSet<SlotSymbol> winningSymbols = _reelGame.outcomeDisplayController.getSetOfWinningSymbols(previousOutcome);

		return winningSymbols;
	}
}
