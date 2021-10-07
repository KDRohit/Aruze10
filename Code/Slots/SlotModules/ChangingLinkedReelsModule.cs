using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// This module checks for linked reels data in the freespins outcome and makes appropriate visual updates
public class ChangingLinkedReelsModule : SlotModule 
{
	[SerializeField] private GameObject[] linkedReelFrames;
	[SerializeField] private GameObject[] reelMarkers;

	private int[] linkedReelsData;
	private GameObject visibleReelFrame;

	private const string LINKED_REEL_REVEAL_SOUND_KEY = "linked_reel_reveal";

	public override bool needsToExecuteOnPreSpin()
	{
		if (FreeSpinGame.instance != null && FreeSpinGame.instance.peekNextOutcome() != null)
		{
			SlotOutcome nextOutcome = FreeSpinGame.instance.peekNextOutcome();
			if (nextOutcome != null)
			{
				linkedReelsData = nextOutcome.getLinkedReelsAsArray();
				if (linkedReelsData != null && linkedReelsData.Length > 0)
				{
					return true;
				}
			}
		}
		return false;
	}
	
	public override IEnumerator executeOnPreSpin()
	{
		if (visibleReelFrame != null)
		{
			visibleReelFrame.SetActive(false);
		}

		if (linkedReelFrames != null && linkedReelFrames.Length > linkedReelsData.Length)
		{
			visibleReelFrame = linkedReelFrames[linkedReelsData.Length];
			if (visibleReelFrame != null)
			{
				visibleReelFrame.transform.position = reelMarkers[linkedReelsData[0]].transform.position;
				visibleReelFrame.transform.localScale = reelMarkers[linkedReelsData[0]].transform.localScale;
				Audio.tryToPlaySoundMap(LINKED_REEL_REVEAL_SOUND_KEY);
				visibleReelFrame.SetActive(true);

				// Refresh all symbols on each reel using the new reel strips before we spin again
				Dictionary<int, string> reelStrips = FreeSpinGame.instance.peekNextOutcome().getReelStrips();

				SlotReel[] reelArray = reelGame.engine.getReelArray();

				// First go through the reels and fix anything that is going to break by 
				// swapping in the new reel strips
				for (int i = 0; i < reelArray.Length; i++)
				{
					SlotReel reel = reelArray[i];

					if (reelStrips.Keys.Contains(reel.reelID))
					{
						// Make sure that if we are dealing with the first or last linked reel
						// we split any straddled mega symbols so that they aren't broken by the reel
						// symbol change we are going to do below.
						if (!reelStrips.ContainsKey(reel.reelID - 1))
						{
							SlotReel previousReel = reelGame.engine.getSlotReelAt((reel.reelID - 1) - 1);
							if (previousReel != null)
							{
								List<SlotSymbol> symbolList = previousReel.symbolList;
								for (int k = 0; k < symbolList.Count; k++)
								{
									SlotSymbol currentSymbol = symbolList[k];
									int symbolWidth = (int)currentSymbol.getWidthAndHeightOfSymbol().x;
									if (currentSymbol.getColumn() != symbolWidth)
									{
										// use random symbols because the 1x1's for gen35 aren't actually 1x1's
										currentSymbol.splitSymbolToRandomSymbols();
									}
								}
							}
						}
						else if (!reelStrips.ContainsKey(reel.reelID + 1))
						{
							SlotReel nextReel = reelGame.engine.getSlotReelAt((reel.reelID - 1) + 1);
							if (nextReel != null)
							{
								List<SlotSymbol> symbolList = nextReel.symbolList;
								for (int k = 0; k < symbolList.Count; k++)
								{
									SlotSymbol currentSymbol = symbolList[k];
									
									if (currentSymbol.getColumn() != 1)
									{
										// use random symbols because the 1x1's for gen35 aren't actually 1x1's
										currentSymbol.splitSymbolToRandomSymbols();
									}
								}
							}
						}
					}
				}

				// Now that we've corrected any issues the reels would have let us actually do the swap
				// We don't want to do the swap in the code above since that could break mega symbols
				// and cause them to not be split correctly
				for (int i = 0; i < reelArray.Length; i++)
				{
					SlotReel reel = reelArray[i];

					if (reelStrips.Keys.Contains(reel.reelID))
					{
						ReelStrip reelStrip = ReelStrip.find(reelStrips[reel.reelID]);
						reel.setSymbolsToReelStripIndex(reelStrip, reelStrip.symbols.Length - 1);
					}
				}

				reelGame.engine.resetLinkedReelPositions();
			}
			else
			{
				Debug.LogError("The reel frame defined for linked reels in ChangingLinkedReelsModule is null for index: " + linkedReelsData.Length);
			}
		}
		else
		{
			Debug.LogError("No reel frames defined for linked reels in ChangingLinkedReelsModule");
		}

		yield break;
	}
}
