using UnityEngine;
using System.Collections.Generic;

// SlotSymbolData class - each instance contains the necessary info defining each symbol.
public class SlotSymbolData : IResetGame
{
	public string keyName;
	public string[] wildMatches;
	public int multiplier;
	
	private static Dictionary<string,SlotSymbolData> _all = new Dictionary<string,SlotSymbolData>();
	
	public SlotSymbolData(string key, JSON data)
	{
		keyName = key;
		wildMatches = data.getStringArray("wild_matches");
		multiplier = data.getInt("multiplier", 1);
	}
	
	// isAMatch - tests to see whether there is a match from one symbol to another.  Note that this is one-directional... a WD (wild) on the reel
	// can be part of an (e.g.) M1 symbol win.  An M1 on a reel does not match a WD win, as there are paylines defined that are specifically
	// looking for 3 WD symbols.
	public static bool isAMatch(string symbolName, string reelSymbolName, int column = -1, int row = -1)
	{
		// Use the base symbol names so we will find a match when comparing
		// something like M1 to M1-2A.
		symbolName = SlotUtils.getBaseSymbolName(symbolName);
		reelSymbolName = SlotUtils.getBaseSymbolName(reelSymbolName);
		
		if (symbolName == reelSymbolName)
		{
			return true;
		}
		
		// In Duck Dynasty, we use a fake icon to show a wild symbol. Let's revert it to normal here for normal processing.
		if (reelSymbolName == "TWWD")
		{
			reelSymbolName = "TW";
		}
		
		// Doing a mutation lookup now...
		ReelGame activeGame = ReelGame.activeGame;
		
		if (activeGame != null)
		{
			if (activeGame.mutationManager.mutations.Count > 0)
			{
				StandardMutation currentMutation = activeGame.mutationManager.mutations[0] as StandardMutation;
				if (currentMutation != null)
				{
					if (currentMutation.triggerSymbolNames != null && currentMutation.triggerSymbolNames.GetLength(0) != 0)
					{
						//Let's check the two types of mutations now...
						for (int i = 0; i < currentMutation.triggerSymbolNames.GetLength(0); i++)
						{
							for (int j = 0; j < currentMutation.triggerSymbolNames.GetLength(1); j++)
							{
								if ("WD" == currentMutation.triggerSymbolNames[i, j] && i == column && j == row)// Needs to be the same column/row as well...
								{
									return true;
								}
							}
						}
					}
				}
				// TODO - Add mutations here as needed.

				// restricting location based wild mutation checks to just zombies right now in-case it could cause an issue in another game
				if (GameState.game.keyName.Contains("zom01"))
				{
					if (null != currentMutation.singleSymbolLocations && currentMutation.singleSymbolLocations.Count > 0)
					{
						if (currentMutation.singleSymbolLocations.ContainsKey(column + 1))
						{
							foreach (int mutatedLocation in currentMutation.singleSymbolLocations[column + 1])
							{
								if (mutatedLocation == row + 1)
								{
									// only check for wilds right now, but doing it this way in case a future game has symbols that mutate into other symbols or something
									if ("WD" == currentMutation.replaceSymbol)
									{
										return true;
									}
								}
							}
						}
					}
				}
			}

			// Checking to see during certain games if we have a normal symbol that needs to be used as a wild.
			if (activeGame.permanentWildReels.Count != 0 && column > 0)
			{
				foreach (string matchSymbolName in activeGame.permanentWildReels)
				{
					if (reelSymbolName == matchSymbolName)
					{
						return true;
					}
				}
			}
		}
		
		SlotSymbolData reelSymbolData = find(reelSymbolName);
		if (reelSymbolData != null)
		{
			foreach (string matchSymbolName in reelSymbolData.wildMatches)
			{
				if (symbolName == matchSymbolName)
				{
					return true;
				}
			}
		}
		
		return false;
	}
	
	public static void populateAll(JSON[] symbolsJsonData)
	{
		foreach (JSON data in symbolsJsonData)
		{
			string key = data.getString("key_name", "");
			
			if (key == "")
			{
				Debug.LogWarning("SlotSymbolData::populateAll - Cannot process empty symbols key");
				continue;
			}
			else if (_all.ContainsKey(key))
			{
				Debug.LogWarning("SlotSymbolData::populateAll - SlotSymbolData already exists for key " + key);
				continue;
			}
			
			_all[key] = new SlotSymbolData(key, data);

			// and prime the SlotSymbol cache best we can
			SlotSymbol.getOrCreateCacheItem(key);
		}

		// and pre-populate an empty-string record
		SlotSymbol.getOrCreateCacheItem(""); 
	}
	
	public static SlotSymbolData find(string keyName)
	{
		if (_all.ContainsKey(keyName))
		{
			return _all[keyName];
		}
		
		Debug.LogError("SlotSymbolData::find - Failed to find SlotSymbolData for key " + keyName);
		return null;
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		_all = new Dictionary<string,SlotSymbolData>();
	}
}
