using System;
using System.Collections.Generic;
using UnityEngine;

/**
ReelSetData contains the fields from Global Data associated with a particular reelset.
Note: although global data does have a root-level "reel_sets"
JSON array, this does not come from there.  This data is gathered from "slots_games" -> "reel_sets",
which is identical.  Because each game has its own reel_sets lookup,
this class has no static dictionary search capability.
*/

public class ReelSetData
{
	private const int MIN_STRIP_SIZE = 12;		// The smallest reel strip that will work with our symbol buffering

	public string keyName;
	public string payLineSet;
	public bool isIndependentReels;
	public bool isHybrid = false; // This is used to control if we should invert paylines in independent reel games
	public JSON checkpointWagerData;
	public List<string> mutationTypes = new List<string>();
	public List<ReelData> reelDataList = new List<ReelData>();
	
	public ReelSetData (JSON data)
	{
		keyName = data.getString("key_name", "");
		payLineSet = data.getString("pay_line_set", "");
		isIndependentReels = data.getBool("independent_reels", false);
		isHybrid = data.getBool("is_hybrid", false);
		checkpointWagerData = data.getJSON("checkpoint_wager");

		JSON[] mutationJsonArray = data.getJsonArray("mutations");
		if (mutationJsonArray != null)
		{
			foreach (JSON type in mutationJsonArray)
			{
				mutationTypes.Add(type.getString("type", ""));
			}
		}
		
		foreach (JSON reelJson in data.getJsonArray("reel_strips"))
		{
			reelDataList.Add(new ReelData(reelJson));
		}
	}

	// Validate data used from global data is reasonable
	public void validateData()
	{
		foreach (ReelData reelData in reelDataList)
		{
			// ignore some test-ish reels that are showing up and always fail
			if (!reelData.reelStripKeyName.ToLower().Contains("test_reel") &&
				!reelData.reelStripKeyName.ToLower().Contains("debug") &&
				!reelData.reelStripKeyName.ToLower().Contains("wow_reelstrip_fs_force"))
			{
				if (reelData.visibleSymbols == 0)
				{
					Debug.LogError(reelData.reelStripKeyName + " contains a ReelData with no visible symbols!");
				}
				else if (reelData.reelStrip.symbols.Length < reelData.visibleSymbols)
				{
					Debug.LogError(reelData.reelStripKeyName + " has less symbols in the strip (" + reelData.reelStrip.symbols.Length + "), than visible symbols (" + reelData.visibleSymbols + ")!");
				}
				else if (reelData.reelStrip.symbols.Length < MIN_STRIP_SIZE)
				{
					Debug.LogErrorFormat("{0} only contains {1} symbols and is too short! The minimum strip size is {2}.",
						reelData.reelStripKeyName, reelData.reelStrip.symbols.Length, MIN_STRIP_SIZE );
				}
			}
		}
	}

	/// Grab all symbols that can show up on the reels for this game
	public HashSet<string> getUniqueSymbolList()
	{
		HashSet<string> uniqueSymbolList = new HashSet<string>();

		foreach (ReelData reelData in reelDataList)
		{
			foreach (string symbol in reelData.reelStrip.symbols)
			{
				if (!uniqueSymbolList.Contains(symbol))
				{
					uniqueSymbolList.Add(symbol);
				}
			}
		}

		return uniqueSymbolList;
	} 
}