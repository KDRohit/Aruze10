using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/*
Class that handles the expanding wilds that happen on all of the WD symbols in oz00.
*/
public class Oz00 : SlotBaseGame
{	
	/// reelsStoppedCallback - called when all reels have come to a stop.
	protected override void reelsStoppedCallback()
	{	
		if (mutationManager.mutations.Count > 0)
		{
			SlotReel[] reelArray = engine.getReelArray();

			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				//int index = 0;
				foreach (SlotSymbol symbol in engine.getVisibleSymbolsAt(reelIndex))
				{                 
					// This handles mutating WD heads in Oz00. We want to verify its on a payline to be safe as well.
					if (symbol.name.Contains("WD") && !_outcome.isBonus)
					{
						bool mutateFound = false;
						ReadOnlyCollection<SlotOutcome> slotOutcomes = _outcome.getSubOutcomesReadOnly();
						foreach (SlotOutcome outcomeCheck in slotOutcomes)
						{
							int paylineLength = PayTable.find(engine.gameData.basePayTable).lineWins[outcomeCheck.getWinId()].symbolMatchCount;

							if (paylineLength >= reelIndex && !mutateFound)
							{
								// Play I am the great and powerful oz if oz game, vo with a bit of a delay.
								Audio.play("symbol_expwild_fanfare");
								Audio.play("wz_expanding_wild_speech", 1f, 0f, 0.1f);

								mutateFound = true;
							}
						}
						engine.getVisibleSymbolsAt(reelIndex)[0].mutateTo("WD-3A", null, mutateFound);

						// Store the indexes in an array so we can tell later which indexes to not animate.
						engine.wildReelIndexes.Add(reelIndex);
						break;
					}
				}
			}
		}
		base.reelsStoppedCallback();
	}
}
