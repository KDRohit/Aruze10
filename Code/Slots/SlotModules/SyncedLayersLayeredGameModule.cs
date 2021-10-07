using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
This module was made for aruze04 in order to have a layered games where both layers start and stop together.
And symbols which are covered by the top layer during a spin will be hidden.

Original Author: Scott Lepthien
Creation Date: November 8, 2017
*/
public class SyncedLayersLayeredGameModule : SlotModule 
{
	private LayeredSlotEngine layeredEngine = null;

// executeOnSlotGameStartedNoCoroutine() section
// executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		layeredEngine = reelGame.engine as LayeredSlotEngine;

		// Need to sync the reel stops between the layers
		reelGame.stopOrder = new ReelGame.StopInfo[][] 
		{
			new ReelGame.StopInfo[] {new ReelGame.StopInfo(0, 0, 0), new ReelGame.StopInfo(0, 0, 1)},
			new ReelGame.StopInfo[] {new ReelGame.StopInfo(1, 0, 0), new ReelGame.StopInfo(1, 0, 1)},
			new ReelGame.StopInfo[] {new ReelGame.StopInfo(2, 0, 0), new ReelGame.StopInfo(2, 0, 1)},
			new ReelGame.StopInfo[] {new ReelGame.StopInfo(3, 0, 0), new ReelGame.StopInfo(3, 0, 1)},
			new ReelGame.StopInfo[] {new ReelGame.StopInfo(4, 0, 0), new ReelGame.StopInfo(4, 0, 1)},
		};

		// Need to ensure that the initial hiding of stacked layers is done,
		// since we normally only do that when the reels move, but they haven't
		// moved yet
		checkForHiddenSymbols();
	}

// Module hook for handling stuff after the reels have advanced in SlotEngine in SlotEngine.frameUpdate()
// Not there is no real concept of blocking in that area, so anything you want to do here must be accomplished
// in a single frame or handled in some non blocking coroutine that you spawn
	public override bool needsToExecuteOnSlotEngineFrameUpdateAdvancedSymbols()
	{
		return true;
	}

	public override void executeOnSlotEngineFrameUpdateAdvancedSymbols()
	{
		checkForHiddenSymbols();
	}

	// Go through each bottom layer reel and determine if anything is covering it
	// on a higher layer
	private void checkForHiddenSymbols()
	{
		List<List<SlotSymbol>> allOverlappingSymbols = layeredEngine.getListsOfOverlappingSymbols();

		// go through all of the overlapping lists and correct any issues we find
		for (int i = 0; i < allOverlappingSymbols.Count; i++)
		{
			List<SlotSymbol> overlapSet = allOverlappingSymbols[i];

			// loop through once to determine if a symbol is covered
			// or if everything is blank (this can happen due to splicing)
			bool isSymbolLocationTotallyBlank = true;
			SlotSymbol baseLayerSymbol = null;
			bool areSymbolsCovered = false;
			int highestCoveringLayer = 0;
			for (int k = 0; k < overlapSet.Count; k++)
			{
				SlotSymbol currentSymbol = overlapSet[k];

				if (currentSymbol.reel.layer == 0)
				{
					baseLayerSymbol = currentSymbol;
				}

				if (!currentSymbol.isBlankSymbol)
				{
					isSymbolLocationTotallyBlank = false;

					if (currentSymbol.reel.layer > 0)
					{
						areSymbolsCovered = true;
						if (currentSymbol.reel.layer > highestCoveringLayer)
						{
							highestCoveringLayer = currentSymbol.reel.layer;
						}
					}
				}
			}

			if (isSymbolLocationTotallyBlank && baseLayerSymbol != null)
			{
				// the location is now totally blank, this should only happen
				// due to splicing and needs to be fixed, as we can't leave a blank
				// here
				string randomReplacementName = baseLayerSymbol.reel.getRandomClobberReplacementSymbol();
				baseLayerSymbol.mutateTo(randomReplacementName, null, false, false, false);
			}
			else if (areSymbolsCovered)
			{
				for (int k = 0; k < overlapSet.Count; k++)
				{
					SlotSymbol currentSymbol = overlapSet[k];
					if (currentSymbol.reel.layer < highestCoveringLayer && !currentSymbol.isBlankSymbol)
					{
						// this symbol is covered, so just mutate it to a BL so it doesn't display
						currentSymbol.mutateTo("BL", null, false, false, false);
					}
				}
			}
		}
	}
}
