﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Holds data about the new VIP Levels.
*/

// These must be defined to match the levelNumber of each VIPLevel object.
public enum VIPLevelEnum
{
	SAPPHIRE = 0,
	EMERALD,
	GOLD,
	PLATINUM,
	RUBY,
	DIAMOND,
	BLACK_DIAMOND,
	YELLOW_DIAMOND,
	BLUE_DIAMOND
}

public class VIPLevel : IResetGame
{
	public string keyName;
	public int levelNumber;
	public string name;
	public string trackingName;
	public List<LobbyGame>games = new List<LobbyGame>();	// Games unlocked at this level.
	
	// Properties that are directly displayed on the VIP dialog.
	public int vipPointsRequired;		// VIP points required to reach this level.
	public int purchaseBonusPct;		// Percent extra credits from purchasing.
	public int dailyBonusPct;			// Percent extra credits from daily bonus game.
	public int receiveGiftBonusPct;		// Percent extra credits received from friends.
	public int sendGiftBonusPct;		// Percent extra credits sent to friends.
	public int freeSpinLimit;			// Number of gifted free spins per day.
	public int creditsGiftLimit;		// Number of acceptable gifts per day.
	public int rareCharmPct;			// Bonus percent chance of winning a rare charm when getting a charm.
	
	// These are hard-coded since they aren't part of SCAT.
	public bool dedicatedAccountManager;
	public bool invitationToSpecialEvents;
		
	public static List<LobbyGame> allGames = new List<LobbyGame>();	// All VIP games
	
	public static VIPLevel maxLevel = null;
	public static VIPLevel earlyAccessMinLevel = null;	// The minimum level required for early access games.

	private static Dictionary<int, VIPLevel> all = new Dictionary<int, VIPLevel>();
	private static GameObject cachedVIPGemSprite;
	private static GameObject cachedVIPCard;

	public static void populateAll(JSON[] levels)
	{
		foreach (JSON level in levels)
		{
			new VIPLevel(level);
		}
	}

	// Sets the maximum level the client should read based on livedata keys and experiment values.
	public static void setMaxLevel()
	{
		int maxLevelNumber = Data.liveData.getInt("VIP_MAX_DISPLAYED_LEVEL", 0);

		maxLevel = find(maxLevelNumber);

		if (maxLevel == null)
		{
			// The max level wasnt found, so lets find the largest one we have.
			Debug.LogErrorFormat("VIPLevel.cs -- setMaxLevel -- the live data key for max vip level is {0}, but that doesn't exist on the client, finding fallback value from existing data.", maxLevelNumber);
			foreach (VIPLevel level in all.Values)
			{
				if (maxLevel == null || level.levelNumber > maxLevel.levelNumber)
				{
					maxLevel = level;
				}
			}
		}
	}
	
	public VIPLevel(JSON data) 
	{
		keyName = data.getString("key_name", "");
		levelNumber = data.getInt("level_number", 0);
		name = data.getString("name", "");

		// PM's want the unlocalized level name for stat tracking, so we have to parse that from the key
		// since it doesn't get provided in the global data.
		string prefix = string.Format("vip_new_{0}_", levelNumber);
		trackingName = keyName.Replace(prefix, "");

		vipPointsRequired = data.getInt("vip_points_required", 0);
		purchaseBonusPct = data.getInt("purchase_bonus_pct", 0);
		dailyBonusPct = data.getInt("daily_bonus_pct", 0);
		receiveGiftBonusPct = data.getInt("receive_gift_bonus_pct", 0);
		sendGiftBonusPct = data.getInt("send_gift_bonus_pct", 0);
		freeSpinLimit = data.getInt("free_spin_bonus_limit", 0);
		creditsGiftLimit = data.getInt("credit_gift_bonus_limit", 0);
		rareCharmPct = data.getInt("rare_charm_percent", 0);
				
		string gameKeysString = data.getString("unlock_game_keys", "");
		
		if (gameKeysString != "")
		{
			string[] keysArray = gameKeysString.Split(',');
			
			foreach (string gameKey in keysArray)
			{
				LobbyGame game = LobbyGame.find(gameKey.Trim());
				if (game != null)
				{
					// Set up a two-way reference.
					game.vipLevel = this;
					games.Add(game);
				}
			}
		}

		// Manually massage some of the vip_levels data for stuff that isn't currently SCAT-driven,
		// but we still want to be code-driven for the UI.
		switch (levelNumber)
		{
			case 0:
				dedicatedAccountManager = false;
				invitationToSpecialEvents = false;
				break;
			case 1:
				dedicatedAccountManager = false;
				invitationToSpecialEvents = false;
				break;
			case 2:
				dedicatedAccountManager = false;
				invitationToSpecialEvents = false;
				break;
			case 3:
				dedicatedAccountManager = false;
				invitationToSpecialEvents = false;
				break;
			case 4:
				dedicatedAccountManager = true;
				invitationToSpecialEvents = true;
				break;
			case 5:
				dedicatedAccountManager = true;
				invitationToSpecialEvents = true;
				break;
			case 6:
				dedicatedAccountManager = true;
				invitationToSpecialEvents = true;
				break;
			case 7:
				dedicatedAccountManager = true;
				invitationToSpecialEvents = true;
				break;
			case 8:
				dedicatedAccountManager = true;
				invitationToSpecialEvents = true;
				break;
		}
		
		if (levelNumber == LoLa.earlyAccessMinLevel)
		{
			earlyAccessMinLevel = this;
		}
		
		all[levelNumber] = this;
	}

	public static int getEventAdjustedLevel()
	{
		int modifiedLevel = (VIPStatusBoostEvent.isEnabled() || PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_VIP_BOOSTS_KEY)) ? VIPStatusBoostEvent.fakeLevel : 0;
		return Mathf.Min(maxLevel.levelNumber, SlotsPlayer.instance.vipNewLevel + modifiedLevel);
	}

	// Must call this after everything is populated and experiment data is available.
	public static void defineAvailableGames()
	{
		// This is the ONLY place LoLa.earlyAccessGameKey should be used directly.
		// The rest of the program should check LobbyGame.vipEarlyAccessGame to see if
		// an early access game is defined, and LobbyGame.vipEarlyAccessGame.keyName to get the key if it is.
		string gameKey = LoLa.earlyAccessGameKey;
				
		if (gameKey != "")
		{
			LobbyGame.vipEarlyAccessGame = LobbyGame.find(gameKey);
		}
		
		if (LobbyGame.vipEarlyAccessGame != null)
		{
			// If there is a VIP early access game specified,
			// put it at the beginning of the all games list,
			// since it needs to be in the first slot in the lobby.
			allGames.Add(LobbyGame.vipEarlyAccessGame);
			
			// Also, set the vip level to the first level that allows free previews as a benefit.
			// This also makes it so the game shows up in the VIP room instead of the main lobby.
			LobbyGame.vipEarlyAccessGame.vipLevel = earlyAccessMinLevel;
		}
		else
		{
			// If there is no VIP early access game,
			// then insert a null in that spot so that the positioning
			// of the rest of the VIP options still works.
			allGames.Add(null);
		}

		foreach (KeyValuePair<int, VIPLevel> kvp in all)
		{
			foreach (LobbyGame game in kvp.Value.games)
			{
				allGames.Add(game);
			}
		}
		
		// Also, if there is a VIP Progressive Jackpot, then link it to the VIP games.
		if (ProgressiveJackpot.vipJackpot != null)
		{
			foreach (LobbyGame game in allGames)
			{
				if (game != null)
				{
					ProgressiveJackpot.vipJackpot.setGame(game, true);
				}
			}
		}
	}
	
	/// Return a particular set of VIPLevel data.
	/// Feature string is for the VIPLevelUp event.
	public static VIPLevel find(int levelNumber, string featureString = "")
	{
		if (!string.IsNullOrEmpty(featureString) && VIPStatusBoostEvent.isEnabled())//if (VIPStatusBoostEvent.featureList != null && VIPStatusBoostEvent.featureList.Contains(featureString))
		{
			levelNumber += VIPStatusBoostEvent.fakeLevel;
		}

		// With all the VIP level modifying going on, lets not screw up.
		if (maxLevel != null && levelNumber > maxLevel.levelNumber)
		{
			levelNumber = maxLevel.levelNumber;
		}

		if (all.ContainsKey(levelNumber))
		{
			return all[levelNumber];
		}
		return null;
	}

	// Loads/Returns a single VIP card for you to do whatever with.
	public static GameObject loadVIPCard(GameObject parent = null, VIPNewIcon iconToLinkTo = null)
	{
		GameObject loadedObject = loadItem ("VIP Card", parent);
		return loadedObject;
	}

	// Loads/Returns a single VIP gem sprite for you to do whatever with. 
	public static GameObject loadVIPGem(GameObject parent = null, VIPNewIcon iconToLinkTo = null)
	{
		GameObject loadedObject = loadItem ("VIP Gem", parent);
		return loadedObject;
	}

	// Loads the non loaylty lounge version. Don't cache since this should basically never need to happen.
	public static GameObject loadOldVIPCard (GameObject parent = null, VIPNewIcon iconToLinkTo = null)
	{
		GameObject loadedObject = loadItem("Linked VIP Icon", parent);
		return loadedObject;
	}

	// Add the loaded item to the parent here. Return it anyway in case the dev wanted to do something special.
	private static GameObject loadItem(string objectName, GameObject parentObject = null)
	{
		string prefabPath = "Prefabs/Misc/" + objectName;
		GameObject loadedObject = SkuResources.loadSkuSpecificResourcePrefab (prefabPath);

		if(loadedObject != null)
		{
			if (parentObject != null)
			{
				loadedObject = NGUITools.AddChild(parentObject, loadedObject, true);
			}
		} 
		else
		{
			Debug.LogError ("VIPLevel::loadItem - Failed to load " + prefabPath + " Even though it exists in the project?");
		}
	

		return loadedObject;
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		all = new Dictionary<int, VIPLevel>();
		allGames = new List<LobbyGame>();
		maxLevel = null;
		earlyAccessMinLevel = null;
	}
}
