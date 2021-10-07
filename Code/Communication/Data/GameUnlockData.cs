using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Data structure for storing information about what games unlock at what levels, based on experiment variant.
This class may become obsolete after experiments are no longer used with unlock levels.
*/

public class GameUnlockData : IResetGame
{
	public static int maxUnlockLevel = 0;	// The highest level that a game unlocks at.
	
	// The dictionary is a two-dimensional dictionary, indexed on variant then level.
	private static Dictionary<int, Dictionary<int, List<string>>> all = new Dictionary<int, Dictionary<int, List<string>>>();
		
	public static void populateAll(JSON[] dataArray)
	{
		foreach (JSON data in dataArray)
		{
			int variant = data.getInt("variant", 0);
			int level = data.getInt("xp_level", 0);
			string gameKeysString = data.getString("unlock_game_keys", "");
			if (gameKeysString == "")
			{
				Debug.LogWarning("GameUnlockData: No unlock_game_keys were provided for variant " + variant + ", level " + level);
				continue;
			}
			
			Dictionary<int, List<string>> levels = null;
			
			if (!all.ContainsKey(variant))
			{
				levels = new Dictionary<int, List<string>>();
				all.Add(variant, levels);
			}
			else
			{
				levels = all[variant];
			}
						
			if (!levels.ContainsKey(level))
			{
				string[] keysArray = gameKeysString.Split(',');
				List<string> gameKeys = new List<string>();
				
				foreach (string gameKey in keysArray)
				{
					string sanitizedGameKey = gameKey;
					if (sanitizedGameKey != null)
					{
						sanitizedGameKey = sanitizedGameKey.Trim();
					}
					// "high_limit" is not actually a game. It's obsolete data from the old high limit room days.
					// Let's just ignore it.
					if (sanitizedGameKey != "high_limit")
					{
						LobbyGame game = LobbyGame.find(sanitizedGameKey);
						if (game != null)
						{
							gameKeys.Add(sanitizedGameKey);
							// Add a reverse-lookup of unlock level by game.
							game.addUnlockLevel(variant, level);
						}
					}
				}

				levels.Add(level, gameKeys);
				
				if (gameKeys.Count > 0)
				{
					maxUnlockLevel = Mathf.Max(maxUnlockLevel, level);
				}
			}
			else
			{
				Debug.LogError("GameUnlockData: levels Dictionary already contains entry for variant " + variant + ", level " + level);
			}
		}
		
		// // Let's see what we got.
		// foreach (KeyValuePair<int, Dictionary<int, List<string>>> kvp in all)
		// {
		// 	Debug.LogWarning("GameUnlockData variant: " + kvp.Key);
		// 	
		// 	foreach (KeyValuePair<int, List<string>> kvp2 in kvp.Value)
		// 	{
		// 		Debug.LogWarning("GameUnlockData level: " + kvp2.Key + ", game count: " + kvp2.Value.Count);
		// 	}
		// }
	}
	
	// Returns a list of game keys that unlock at the given level for the current variant.
	public static List<string> findUnlockedGamesForLevel(int level)
	{
		if (all.ContainsKey(ExperimentWrapper.LockedLobby.variant))
		{
			if (all[ExperimentWrapper.LockedLobby.variant].ContainsKey(level))
			{
				List<string> unlockedGameKeys = all[ExperimentWrapper.LockedLobby.variant][level];
				List<string> filteredGamesList = new List<string>();
				
				//Go through the unlocked games list and remove any we don't have map data for
				//Map data will be null if the game doesn't exist in the resource map or
				//if its non_production_ready and we're not supposed to be showing in progress games
				for (int i = 0; i < unlockedGameKeys.Count; i++)
				{
					//check if the game is in a lobby.  Ignore it if the user doesn't have access
					if (LoLa.doesGameExistInLobby(unlockedGameKeys[i]))
					{
						SlotResourceData mapData = SlotResourceMap.getData(unlockedGameKeys[i]);
						if (mapData != null)
						{
							filteredGamesList.Add(unlockedGameKeys[i]);
						}	
					}
				}
				return filteredGamesList;
			}
		}
		return null;
	}
		
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		all = new Dictionary<int, Dictionary<int, List<string>>>();
	}

}
