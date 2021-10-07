using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Data structure to hold information about groups of games in the lobby. Not for actually playing games.
*/

public class LobbyGameGroup : IResetGame
{
	public string keyName;
	public string name;
	public string description;
	public string license;
	public bool isActive;
	public string clickSound;
	public List<LobbyGame> games = new List<LobbyGame>();

	private static string[] blacklist = null;	// Any game in this list should not show up in the game at all.

	public LobbyGameGroup()
	{
		
	}
	
	public static void populateAll(JSON[] array)
	{
		blacklist = Data.liveData.getArray("GAMES_BLACKLIST", new string[0]);

	#if RWR
		LobbyGame.rwrSweepstakesGames = Data.liveData.getString("RWR_PROMO_GAMES", "").Split(',');
	#endif

		foreach (JSON groupJson in array)
		{
			createGameGroup(groupJson);
		}
		
		// Create fake test games too.
		TextAsset textAsset = Resources.Load("Test Data/LobbyGameGroup") as TextAsset;
		if (textAsset != null)
		{
			createGameGroup(new JSON(textAsset.text));
		}
	}
		
	private static void createGameGroup(JSON groupJson)
	{
		LobbyGameGroup group = new LobbyGameGroup();
		
		group.keyName = groupJson.getString("key_name", "");
		group.license = groupJson.getString("license", "");
		group.clickSound = groupJson.getString("click_sound", "");
		group.isActive = groupJson.getBool("is_active", false);
				
		// See if this group's license is allowed for the country.
		bool isGroupLicensed = SlotLicense.isLicenseAllowed(group.license);
				
		foreach (JSON gameJson in groupJson.getJsonArray("slots_games"))
		{
			if (System.Array.IndexOf(blacklist, gameJson.getString("key_name", "")) == -1)
			{
				new LobbyGame(group, isGroupLicensed, gameJson);
			}
		}		
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		blacklist = null;
	}
}
