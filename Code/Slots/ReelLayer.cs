using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/**
SlidingLayer
*/
[System.Serializable()]
public class ReelLayer
{
	public GameObject parent;						// The parent of all the reels.
	public GameObject[] reelRoots;					// Each individual reel object.
	public int layer = 0;							// The depth that this layer is on.
	public bool hasReplacementSymbolDataReady = false;	// Tells if this layer is ready to refresh with reel data, avoids an issue with replacement symbols 
	[HideInInspector] public ReelGame reelGame;
	public ReelSetData reelSetData
	{
		get
		{
			return _reelSetData;
		}
		set
		{
			refreshReelSets = true;
			_reelSetData = value;
		}
	}
	public ReelSetData _reelSetData = null;

	// getReelArray override's can be costly functions; do NOT call repeatedly in tight loops
	public SlotReel[] getReelArray()
	{
		if (refreshReelSets)
		{
			if (reelSetData == null)
			{
				//Debug.LogError("There is no reelSetData for this layer!");
				return _reelArray;
			}

			refreshReelSets = false;

			if (_reelArray == null)
			{
				// Create a new reelArray.
				_reelArray = new SlotReel[reelSetData.reelDataList.Count];
				for (int reelIndex = 0; reelIndex < _reelArray.Length; reelIndex++)
				{
					_reelArray[reelIndex] = new SpinReel(reelGame);
					_reelArray[reelIndex].isLayeringOverlappingSymbols = reelGame.isLayeringOverlappingSymbols;
				}
			}
			// Set the reels with the reel data.
			for (int reelIndex = 0; reelIndex < _reelArray.Length; reelIndex++)
			{
				int reelID = reelIndex + 1;
				if (reelSetData.isIndependentReels)
				{
					reelID = reelSetData.reelDataList[reelIndex].reelID;
				}

				_reelArray[reelIndex].setReelDataWithoutRefresh(reelSetData.reelDataList[reelIndex], reelID, layer); // 1 based.
			}

			// refresh all the reels with the data that was loaded above
			for (int reelIndex = 0; reelIndex < _reelArray.Length; reelIndex++)
			{
				_reelArray[reelIndex].refreshReelWithReelData(hasReplacementSymbolDataReady);
			}
		}
		return _reelArray;
	}
	private SlotReel[] _reelArray = null;

	// Independent reels need to be stored a little differently.
	protected List<List<SlotReel>> independentReelArray 
	{ 
		get
		{
			if (_independentReelArray == null)
			{
				populateIndependentReelArray();
			}
			return _independentReelArray;
		}
		private set {}
	}
	private List<List<SlotReel>> _independentReelArray;

	// Get all the reels on this layer
	public virtual List<SlotReel> getAllReels()
	{
		List<SlotReel> allReelsForLayer = new List<SlotReel>();

		if (reelSetData != null && reelSetData.isIndependentReels)
		{
			if (_independentReelArray != null)
			{
				// grab the independent reels
				foreach (List<SlotReel> slotReelList in _independentReelArray)
				{
					foreach (SlotReel reel in slotReelList)
					{
						allReelsForLayer.Add(reel);
					}
				}
			}
		}
		else
		{
			if (_reelArray != null)
			{
				foreach (SlotReel reel in _reelArray)
				{
					allReelsForLayer.Add(reel);
				}
			}
		}

		return allReelsForLayer;
	}

	public float symbolVerticalSpacing = 0.0f; // Public so copy constructor can get it.
	protected Dictionary<int,int> reelToStopDict = new Dictionary<int, int>();
	private GenericDelegate _reelsSpinEndingDelegate;
	private bool refreshReelSets = true;

	// Constants
	protected const int BASE_LAYER = 0;

	public float getSymbolVerticalSpacingAt(int reelID)
	{
		return symbolVerticalSpacing;
	}

	public int getReelRootsLength()
	{
		if (reelSetData != null && reelSetData.isIndependentReels)
		{
			return independentReelArray.Count;
		}
		else
		{
			return reelRoots.Length;
		}
	}

	// This gets the SlotReel based off the postion that it's at in the game.
	// So if a reel looks like it's at postion 2, then this will give you that reel on this layer.
	// Returns null if there isn't a reel at that ID.
	public virtual SlotReel getSlotReelAt(int reelID, int row = -1)
	{
		if (reelID < 0)
		{
			return null;
		}
		if (reelSetData != null && reelSetData.isIndependentReels)
		{
			if (row == -1)
			{
				return getReelArray()[reelID];
			}
			if (independentReelArray.Count > reelID && independentReelArray[reelID].Count > row)
			{
				SlotReel reel = independentReelArray[reelID][row];
				if (reel.reelData.reelID - 1 == reelID && reel.reelData.position == row)
				{
					return reel;
				}
			}
		}
		else 
		{
			SlotReel[] reelArray = getReelArray();
			if (reelArray != null)
			{
				for (int index = 0; index < reelArray.Length; index++)
				{
					if ((reelArray[index].reelID - 1) == reelID)	// Comparing one based and zero based.
					{
						return reelArray[index];
					}
				}
			}
		}
		return null;
	}	

	public virtual bool hasSlotReelAt(int reelID, int row = -1)
	{
		if (reelSetData != null && reelSetData.isIndependentReels)
		{
			if (row == -1)
			{
				return getReelArray()[reelID] != null;
			}
			if (independentReelArray.Count > reelID && independentReelArray[reelID].Count > row)
			{
				SlotReel reel = independentReelArray[reelID][row];
				if (reel.reelData.reelID - 1 == reelID && reel.reelData.position == row)
				{
					return true;
				}
			}
		}
		else 
		{
			SlotReel[] reelArray = getReelArray();
			if(reelArray!=null)
			{
				for (int index = 0; index < reelArray.Length; index++)
				{
					if ((reelArray[index].reelID - 1) == reelID)	// Comparing one based and zero based.
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	/// Returns the reel root for the passed in reelID
	public GameObject getReelRootAt(int reelID, int row = -1)
	{
		// Make sure everything is happening as expected.
		if (reelRoots == null)
		{
			Debug.LogError("Trying to get the reel roots Length before they are set in the engine.");
			return null;
		}
		// Layers don't mean anything for the base engine, and rows are not yet implemented.
		if (reelSetData.isIndependentReels)
		{
			return reelRoots[getRawReelID(reelID, row)];
		}
		else if (getReelArray() != null)
		{
			int rawReelID = getRawReelID(reelID, row);

			if (rawReelID != -1)
			{
				return reelRoots[rawReelID];
			}
		}
		return null;
	}

	/// Some data is sending raw ids, that need to be converted back to standard reel ids to work with some of our code
	/// If this isn't an inpendant reel game then we'll just return the rawReelID back
	/// Note returned reelID is ZERO based
	public void rawReelIDToStandardReelID(int rawReelID, out int reelID, out int row)
	{
		if (reelSetData.isIndependentReels)
		{
			// We need to use the row to find out what we should be doing here.
			reelID = 0;
			row = 0;

			for (int id = 0; id < independentReelArray.Count; id++)
			{
				if (rawReelID >= independentReelArray[id].Count)
				{
					rawReelID -= independentReelArray[id].Count;
					reelID++;
				}
				else if(rawReelID == 0)
				{
					// no remainder, and reelID should be correct
					row = 0;
					return;
				}
				else
				{
					// we have a remainder, store it in row, reelID should be correct at this point
					row = rawReelID;
					return;
				}
			}

			Debug.LogError("rawReelIDToStandardReelId() - Conversion failed!");
			return;
		}
		else
		{
			reelID = rawReelID;
			row = -1;
		}
	}

	/// Grabs a raw reelID which can be one of the independant reels if we're dealing with an independant reel game, otherwise it is just the passed reelID
	public int getRawReelID(int reelID, int row, bool isIndpendentSequentialIndex = false)
	{
		if (reelSetData == null)
		{
			return -1;
		}
		else
		{
			if (reelSetData.isIndependentReels)
			{
				// We need to use the row to find out what we should be doing here.
				int reelPosition = 0;
				List<List<SlotReel>> theIndependentReelArray = independentReelArray;
				for (int id = 0; id < reelID; id++)
				{
					reelPosition += theIndependentReelArray[id].Count;
				}

				int targetRow = row;
				int offset = 0;

				List<SlotReel> independentReelArray_reelID = theIndependentReelArray[reelID];

				// Now we have the position.
				//Debug.LogError("Getting reelID = "  + reelID);
				//Debug.LogError("Getting reels at " + (reelPosition + independentReelArray[reelID].Count - 1 - row));
				if (isIndpendentSequentialIndex)
				{
					// some stuff like sounds need to get sequential indexs instead of how independent reels are normally indexed which is bottom being the low number
					// i.e. Non-sequential is:		VS 			Sequential:
					// [5]	[10] [15] [20] [25]					[1] [6]  [11] [16] [21]
					// [4] 	[9]	 [14] [19] [24]					[2] [7]  [12] [17] [22]
					// [3] 	[8]	 [13] [18] [23]					[3] [8]  [13] [18] [23]
					// [2] 	[7]	 [12] [17] [22]					[4]	[9]  [14] [19] [24]
					// [1] 	[6]  [11] [16] [21]					[5]	[10] [15] [20] [25]
					//Debug.LogError("ReelID = " + reelID + " row = " + row + " Raw reelID = " + (reelPosition + row));

					for (int i = 0; i < independentReelArray_reelID.Count; i++)
					{
						SlotReel reel = independentReelArray_reelID[i];
						targetRow -= reel.reelData.visibleSymbols;
						if (targetRow >= 0)
						{
							offset++;
						}
					}

					return reelPosition + offset;
				}
				else
				{
					//Debug.LogError("ReelID = " + reelID + " row = " + row + " Raw reelID = " + (reelPosition + independentReelArray[reelID].Count - 1 - row));
					for (int i = independentReelArray_reelID.Count - 1; i >= 0; i--)
					{
						SlotReel reel = independentReelArray_reelID[i];
						targetRow -= reel.reelData.visibleSymbols;
						if (targetRow >= 0)
						{
							offset++;
						}
					}

					return reelPosition + independentReelArray_reelID.Count - 1 - offset;
				}
			}
			else
			{
				SlotReel[] reelArray = getReelArray();
				for (int index = 0; index < reelArray.Length; index++)
				{
					if ((reelArray[index].reelID - 1) == reelID)	// Comparing one based and zero based.
					{
						
						//Debug.LogError("ReelID = " + reelID + " row = " + row + " Raw reelID = " + index);
						return index;
					}
				}
			}

			// failed to get an ID
			return -1;
		}
	}

	public virtual int getVisibleSymbolsCountAt(int reelID)
	{
		// old non-optimized way
		// SlotSymbol[] slotsymbols = getVisibleSymbolsAt(reelID);
		// return 	(slotsymbols!=null) ? slotsymbols.Length : 0;

		// Optimized: calculate count (as per getVisibleSymbolsAt) without actually allocating list
		int count = 0;

		if (reelSetData != null && reelSetData.isIndependentReels)
		{
			foreach (SlotReel reel in getReelArray())
			{
				if (reel.reelData.reelID == reelID + 1)
				{
					count += reel.visibleSymbols.Length;
				}
			}
		}
		else
		{
			SlotReel reel = getSlotReelAt(reelID);
			if (reel != null)
			{
				count = reel.visibleSymbols.Length;
			}
		}

#if UNITY_EDITOR
		// Verify optimized code matches old results
		if (!UnityEngine.Profiling.Profiler.enabled)
		{
			SlotSymbol[] slotSymbols = getVisibleSymbolsAt(reelID);
			int oldCount = (slotSymbols != null) ? slotSymbols.Length : 0;
			if (oldCount != count) { Debug.LogError("getVisibleSymbolsCountAt problem:" + count + "!=" + oldCount); }
		}
#endif
	
		return count;
	}


	public virtual SlotSymbol[] getVisibleSymbolsAt(int reelID)
	{
		if (reelSetData != null && reelSetData.isIndependentReels)
		{
			List<SlotReel> independentReelsAtID = new List<SlotReel>();
			foreach (SlotReel reel in getReelArray())
			{
				if (reel.reelData.reelID == reelID + 1)
				{
					independentReelsAtID.Add(reel);
				}
			}
			// Sort all of the reel by their positions.
			independentReelsAtID.Sort(delegate(SlotReel reel1, SlotReel reel2) 
				{
					return reel1.reelData.position - reel2.reelData.position;
				}
			);
			// Go through the list of indepentReels and make a list symbols.
			List<SlotSymbol> visibleSymbols = new List<SlotSymbol>();
			foreach (SlotReel reel in independentReelsAtID)
			{
				foreach (SlotSymbol symbol in reel.visibleSymbols)
				{
					visibleSymbols.Add(symbol);
				}
			}
			return visibleSymbols.ToArray();
		}
		else
		{
			SlotReel reel = getSlotReelAt(reelID);
			if (reel != null)
			{
				return reel.visibleSymbols;
			}
			return null;
		}
	}

	public virtual bool stopReelSpinAt(int reelID)
	{
		SlotReel reel = getSlotReelAt(reelID);
		if (reel != null)
		{
			reel.stopReelSpin(reelToStopDict[reelID]);
			reelGame.onSpecificReelStopping(reel);
			return true;
		}
		return false;
	}

	// Get the list of independent reels associated with a reelID
	public List<SlotReel> getIndependentReelListAt(int reelID)
	{
		if (independentReelArray == null)
		{
			return null;
		}
		else
		{
			if (reelID >= 0 && reelID < independentReelArray.Count)
			{
				return independentReelArray[reelID];
			}
			else
			{
				return null;
			}
		}
	}

	// Sets up the independentReelArray
	protected void populateIndependentReelArray()
	{
		_independentReelArray = new List<List<SlotReel>>();
		int numberOfReels = 0;
		SlotReel[] reelArray = getReelArray();
		foreach (SlotReel reel in reelArray)
		{
			if (reel.reelData.reelID > numberOfReels)
			{
				numberOfReels = reel.reelData.reelID;
			}
		}
		// Now we've got all the reels that we need so we want to go through each reel and set up the list.
		for (int reelID = 0; reelID < numberOfReels; reelID++)
		{
			List<SlotReel> reelsAtID = new List<SlotReel>();
			// Get all of the reels that have
			foreach (SlotReel reel in reelArray)
			{
				if (reel.reelData.reelID - 1 == reelID)
				{
					reelsAtID.Add(reel);
				}
			}
			// Sort the list based off position(row)
			reelsAtID.Sort(
			delegate(SlotReel reel1, SlotReel reel2)
			{
				return reel1.reelData.position - reel2.reelData.position;
			});
			_independentReelArray.Add(reelsAtID);
		}
	}

	// Returns the stop position for this reel.
	private int getReelStopAt(int reelID)
	{
		if (reelToStopDict == null || reelToStopDict.Count == 0)
		{
			Debug.LogError("Trying to get reel stop before data has been set.");
			return -1;
		}
		if (getSlotReelAt(reelID) != null)
		{
			return reelToStopDict[reelID];
		}
		return -1;
	}

	public virtual int getStopIndexForReel(SlotReel reel)
	{
		int reelStopIndex = -1;
		if (reelSetData.isIndependentReels)
		{
			int reelStopPosition = 0; 
			// since the data is sent down in an array when we calculate the spot in the array that matches the reelID
			foreach (SlotReel prevReel in getReelArray())
			{
				if (prevReel.reelData.reelID < reel.reelData.reelID)
				{
					reelStopPosition += 1;
				}
			}
			foreach (SlotReel prevReel in independentReelArray[reel.reelData.reelID - 1])
			{
				if (prevReel.reelData.position > reel.reelData.position)
				{
					reelStopPosition += 1;
				}
			}
			
			if (reelToStopDict.ContainsKey(reelStopPosition))
			{
				reelStopIndex = reelToStopDict[reelStopPosition];
			}
			else
			{
				// If we can't look it up we return -1 which if used
				// for a reelStopIndex will force the reels to stop at
				// the next position.
				reelStopIndex = -1;
			}
		}
		else
		{
			reelStopIndex = getReelStopAt(reel.reelID - 1);
		}
		return reelStopIndex;
	}

	public virtual void setReelInfo(SlotOutcome slotOutcome)
	{
		// Check if we need to load a new foreground reel_set here
		if (layer != BASE_LAYER)
		{
			string foreground_reel_set = slotOutcome.getForegroundReelSet();
			// Avoid setting the reelset to the same thing
			if (!string.IsNullOrEmpty(foreground_reel_set) && (reelSetData == null || reelSetData.keyName != foreground_reel_set))
			{
				// We have a foreground reel set to handle setting it here
				reelSetData = reelGame.slotGameData.findReelSet(foreground_reel_set);
			}
		}
		
		setReelStops(slotOutcome);
		setReplacementSymbols(slotOutcome);
	}

	// Sets the stops given the mutation info.
	protected virtual void setReelStops(SlotOutcome slotOutcome)
	{
		// Clear the stops dict since we are going to get new stops
		reelToStopDict.Clear();
		
		if (layer == BASE_LAYER)
		{
			int[] stopArray = slotOutcome.getReelStops();
			for (int i = 0; i < stopArray.Length; i++)
			{
				reelToStopDict[i] = stopArray[i];
			}
		}
		else
		{
			// First check if we have reel stops in the base part of the outcome
			// because it might be stored there rather than in an a mutation
			int[] foregroundReelStops = slotOutcome.getForegroundReelStops();

			if (foregroundReelStops != null && foregroundReelStops.Length > 0)
			{
				// Going to use the foreground reel stops in the outcome itself
				for (int i = 0; i < foregroundReelStops.Length; i++)
				{
					reelToStopDict[i] = foregroundReelStops[i];
				}
			}
			else
			{
				// Going to search for and use the foreground reel stops in a mutation
				JSON[] mutationInfo = slotOutcome.getMutations();
				// For now we will just grab the first variant of the mutations array.
				if (mutationInfo.Length > 0)
				{
					JSON info = mutationInfo[0];
					reelToStopDict = info.getIntIntDict("foreground_reel_stops");
				}
			}
		}
	}

	protected virtual void setReplacementSymbols(SlotOutcome slotOutcome)
	{
		JSON[] mutationInfo = slotOutcome.getMutations();

		Dictionary<string, string> normalReplacementSymbolMap = new Dictionary<string, string>();
		Dictionary<string, string> megaReplacementSymbolMap = new Dictionary<string, string>();

		// Check all mutations for possible replace symbols
		for (int i = 0; i < mutationInfo.Length; i++)
		{
			JSON info = mutationInfo[i];
			JSON replaceData = info.getJSON("replace_symbols");

			if (replaceData != null)
			{
				foreach(KeyValuePair<string, string> megaReplaceInfo in replaceData.getStringStringDict("mega_symbols"))
				{
					megaReplacementSymbolMap.Add(megaReplaceInfo.Key, megaReplaceInfo.Value);
				}

				foreach(KeyValuePair<string, string> normalReplaceInfo in replaceData.getStringStringDict("normal_symbols"))
				{
					if (!megaReplacementSymbolMap.ContainsKey(normalReplaceInfo.Key))
					{
						// Check and see if mega and normal have the same values.
						normalReplacementSymbolMap.Add(normalReplaceInfo.Key, normalReplaceInfo.Value);
					}
					else
					{
						Debug.LogError("The replacement info should not contain duplicate mappings. Investigate!");
					}
				}
			}
		}

		if (normalReplacementSymbolMap.Count > 0 || megaReplacementSymbolMap.Count > 0)
		{
			setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow:true);
		}
	}
	
	public void setReplacementSymbols(Dictionary<string, string> normalReplacementSymbolMap, Dictionary<string, string> megaReplacementSymbolMap)
	{
		SlotReel[] reelArray = getReelArray();
		if (reelArray != null)
		{
			foreach (SlotReel reel in reelArray)
			{
				reel.setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow: true);
			}
		}
	}

	public void setReplacementSymbolMap(Dictionary<string, string> normalReplacementSymbolMap, Dictionary<string, string> megaReplacementSymbolMap, bool isApplyingNow)
	{
		SlotReel[] reelArray = getReelArray();
		if (reelArray != null)
		{
			for (int i = 0; i < reelArray.Length; i++)
			{
				reelArray[i].setReplacementSymbolMap(normalReplacementSymbolMap, megaReplacementSymbolMap, isApplyingNow);
			}
		}
	}

	public void setStartingIndex(int index)
	{
		SlotReel[] reelArray = getReelArray();
		
		foreach (SlotReel reel in reelArray)
		{
			reel.reelID += index;
		}
	}

	public virtual void update()
	{
		UnityEngine.Profiling.Profiler.BeginSample("ReelLayer.update");  // for t102, has 25 items in reel array
		SlotReel[] reelArray = getReelArray();
		
		if (reelArray != null)
		{
			foreach (SlotReel reel in reelArray)
			{
				reel.frameUpdate();
			}
		}
		UnityEngine.Profiling.Profiler.EndSample();
	}

	public bool isEveryReelBlank()
	{
		SlotReel[] reelArray = getReelArray();
		if (reelArray != null)
		{
			for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
			{
				SlotReel reel = reelArray[reelIndex];

				if (reel != null && reel.visibleSymbols != null)
				{
					for (int symbolIndex = 0; symbolIndex < reel.visibleSymbols.Length; symbolIndex++)
					{
						if (!reel.visibleSymbols[symbolIndex].isBlankSymbol)
						{
							return false;
						}
					}
				}
			}
		}

		return true;
	}
}
