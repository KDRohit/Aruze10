using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/**
MultiSlotLayer
This class is an extension of ReelLayer that processes outcomes in a slightly different way when setting reel stops
Author: Nick Reynolds
*/
[System.Serializable()]
public class MultiSlotLayer : ReelLayer
{
	public MultiSlotLayer(ReelLayer other)
	{
		Debug.Log("MultiSlotLayer Copy Constructor.");
		this.parent = other.parent;
		this.reelRoots = other.reelRoots;
		this.layer = other.layer;
		this.reelGame = other.reelGame;
		this.reelSetData = other.reelSetData;
		this.symbolVerticalSpacing = other.symbolVerticalSpacing;
		// this.reelArray = other.reelArray; // Gets set when it's needed.
	}

	// Sets the stops given the mutation info.
	protected override void setReelStops(SlotOutcome slotOutcome)
	{
		JSON[] mutationInfo = slotOutcome.getMutations();
		JSON[] reevalInfo = slotOutcome.getArrayReevaluations();
		// For now we will just grab the first variant of the mutations array.
		if (mutationInfo.Length > 0)
		{
			JSON info = mutationInfo[0];
			// Set the reel stops from the SlotOutcome
			if (layer == BASE_LAYER)
			{
				int[] stopArray = slotOutcome.getReelStops();
				reelToStopDict = new Dictionary<int, int>();
				for (int i = 0; i < stopArray.Length; i++)
				{
					reelToStopDict[i] = stopArray[i];
				}
			}
			else
			{
				// Get the foreground reels stops.
				reelToStopDict = info.getIntIntDict("foreground_reel_stops");
			}
		}

		// games like hi03 use reevaluations for the outcome info for "foreground" reels
		if (reevalInfo.Length > 0)
		{
			// hi03 has crappy, deprecated outcomes. Just let it do it's own thing. Too complicated to make it "good" right now.
			if (GameState.isDeprecatedMultiSlotBaseGame())
			{
				if(layer == BASE_LAYER)
				{
					int[] stopArray = slotOutcome.getReelStops();
					reelToStopDict = new Dictionary<int, int>();
					for (int i = 0; i < stopArray.Length; i++)
					{
						reelToStopDict[i] = stopArray[i];
					}
				}
				else
				{
					// Get the foreground reels stops.
					int[] stopArray = reevalInfo[0].getIntArray("reel_stops." + layer);
					reelToStopDict = new Dictionary<int, int>();
					for (int i = 0; i < stopArray.Length; i++)
					{
						reelToStopDict[i] = stopArray[i];
					}
				}
			}
			else
			{
				JSON gameJson = reevalInfo[0].getJsonArray("games")[layer];
				int[] stopArray = gameJson.getIntArray("reel_stops");
				reelToStopDict = new Dictionary<int, int>();
				for (int i = 0; i < stopArray.Length; i++)
				{
					reelToStopDict[i] = stopArray[i];
				}
			}
		}
	}
}
