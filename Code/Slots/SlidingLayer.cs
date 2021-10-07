using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/**
SlidingLayer
*/
[System.Serializable()]
public class SlidingLayer : ReelLayer
{
	public SlidingLayer(ReelLayer other)
	{
		Debug.Log("SlidingLayer Copy Constructor.");
		this.parent = other.parent;
		this.reelRoots = other.reelRoots;
		this.layer = other.layer;
		this.reelGame = other.reelGame;
		this.reelSetData = other.reelSetData;
		this.symbolVerticalSpacing = other.symbolVerticalSpacing;
		// this.reelArray = other.reelArray; // Gets set when it's needed.
	}

	public override void setReelInfo(SlotOutcome slotOutcome)
	{
		base.setReelInfo(slotOutcome);
		if (layer != BASE_LAYER)
		{
			// The base layer doesn't get set with this information because it doesn't move...yet
			// Basically if we want the base layer to move we need to get smarter information from the server.
			// This may not come down such as in a game like got01 where only one layer's worth of data comes
			// down depending on what mode the game is in.
			List<KeyValuePair<int, string>> foregroundReelStripData = slotOutcome.getForegroundReelStrips();
			if (foregroundReelStripData != null && foregroundReelStripData.Count > 0)
			{
				setReelPosAndStrips(slotOutcome.getForegroundReelStrips());
			}
		}
	}

	private void setReelPosAndStrips(List<KeyValuePair<int, string>> newData)
	{
		SlotReel[] reelArray = getReelArray();
		if (reelArray == null || newData == null || newData.Count != reelArray.Length)
		{
			Debug.LogError("Not enough information given to setReelPosAndStrips.  Layer = " + layer);
			return;
		}

		// first change all the reelIDs so that things will function correctly
		// if we need to check something involving these reels when setting the
		// replacement strip
		int reelArrayIndex = 0;
		foreach (KeyValuePair<int, string> kvp in newData)
		{
			reelArray[reelArrayIndex].reelID = kvp.Key;
			reelArrayIndex++;
		}

		// Now handle the replacement strips
		reelArrayIndex = 0;
		foreach (KeyValuePair<int, string> kvp in newData)
		{
			ReelStrip newReelStrip = ReelStrip.find(kvp.Value);
			if (newReelStrip != null)
			{
				reelArray[reelArrayIndex].setReplacementStrip(newReelStrip);
			}
			else
			{
				Debug.LogError("ReelStrip name " + kvp.Value + " doesn't exist.");
			}
			reelArrayIndex++;
		}
	}

	// For now sliding layers don't handle their reel stops as sanely as layered reels too.
	// Hopefully in the future only one unified method can be used to get the stops on both.
	// Which should allow us to have layered(with BL symbols) and sliding games.
	protected override void setReelStops(SlotOutcome slotOutcome)
	{
		base.setReelStops(slotOutcome);
		
		// If we didn't already get data for this layer from the base call which might have
		// found layer specific stops (for games like got01), then just assign this layer the base layer stops
		// which is what we do for standard sliding games
		if (reelToStopDict.Count == 0)
		{
			int[] stopArray = slotOutcome.getReelStops();
			reelToStopDict = new Dictionary<int, int>();
			for (int i = 0; i < stopArray.Length; i++)
			{
				reelToStopDict[i] = stopArray[i];
			}
		}

		if (layer == BASE_LAYER)
		{
			//no need for the sorted one
			//List<KeyValuePair<int, string>> forgroundReelStrips = slotOutcome.getForegroundReelStrips();

			Dictionary<int,string> forgroundReelStrips = slotOutcome.getForegroundReelStripsDictionary();

			int reelToStopDict_Count = reelToStopDict.Count;

			foreach (int key in forgroundReelStrips.Keys)
			{
				if (key <= 0 || key > reelToStopDict_Count)
				{
					Debug.LogError("Trying to set a reel stop for a key that's out of our range. " + key);
				}
				else
				{
					reelToStopDict[key - 1] = -1;
				}
			}
		}
	}
}
