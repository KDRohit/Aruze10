using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Keeps track of game-specific info for the user, such as number of spins, whether it's unlocked, etc.
*/

public class GameExperience : IResetGame
{
	public bool isVisible = true;           // False means do not show it in the lobby.
	public int spinCount = 0;				// Number of spins the player has for this game.
	public bool isPermanentUnlock = false;	// Whether the player has explicitly and permanently unlocked this game.
											// Only useful when games are unlocked out of level order,
											// such as a granted game unlock. Otherwise we just look at unlock level and player's level.
	public bool isPendingPlayerUnlock = false;	// Set to true if the player chose this game to unlock, but an unlock event hasn't yet been received to unlock it.
	#if RWR
	public long rwrSweepstakesMeterCount = 0;			// Real world rewards meter count.
	#endif
	
	public string unlockType = null;       // How do you unlock this game?  For example, is it an xpromo event like playing WoZ?
	public string xpromoTarget = null;     // What sku do you have to play to unlock this game?
	public string xpromoUrl = null;        // The link to the sku.
	public bool isSkuGameUnlock = false;
	
	public bool didUnlockInGame = false;	// We need to distinguish whether a premium game was unlocked in game vs. customer service tool.
	
	private static Dictionary<string, GameExperience> all = new Dictionary<string, GameExperience>();
	public static int totalSpinCount = 0;
	
	public GameExperience(LobbyGame game, JSON gameData)
	{
		if (gameData != null)	// It is legit to pass in null.
		{
			isVisible = gameData.getBool("is_visible", true);
			spinCount = gameData.getInt("spin_count", 0);
			isPermanentUnlock = gameData.getBool("is_permanent_unlock", false);
			
			unlockType = gameData.getString("unlock_type", "");
			xpromoTarget = gameData.getString("xpromo_target", "");
			xpromoUrl = CommonText.appendQuerystring(gameData.getString("xpromo_url", ""), Zynga.Slots.ZyngaConstantsGame.advertisingTrackSuffix);
			
			isSkuGameUnlock =
				(unlockType == "xpromo" && xpromoTarget != "" && xpromoUrl != "") &&
				game.isEnabledForLobby;
			
			if (isSkuGameUnlock)
			{
				// LoLa has to have this game set up as a XPROMO_GAME_UNLOCK feature in order to show it.
				isSkuGameUnlock = (game.lolaGame != null && game.lolaGame.feature == LoLaGame.Feature.XPROMO_GAME_UNLOCK);
			}
#if RWR
			JSON rwrJSON = gameData.getJSON("rwr_meter_count");
			if (rwrJSON != null)
			{
				JSON packageJSON = rwrJSON.getJSON(Glb.RWR_ACTIVE_PROMO);
				if (packageJSON != null)
				{
					rwrSweepstakesMeterCount = packageJSON.getLong("count", 0L);
				}
			}		
#endif
		}
		
		totalSpinCount += spinCount;
	}
	
	/// Used in global data init, to make sure every game has a GameExperience object,
	/// even if not provided in the player data.
	/// All other code should use find() instead.
	public static GameExperience findOrCreate(LobbyGame game)
	{
		GameExperience gameExp;
		if (all.TryGetValue(game.keyName, out gameExp))
		{
			return gameExp;
		}
		else
		{
			GameExperience gameXp = new GameExperience(game, null);
			all.Add(game.keyName, gameXp);
			return gameXp;
		}
	}

	public static GameExperience find(string keyName)
	{
		if (all.ContainsKey(keyName))
		{
			return all[keyName];
		}
		Debug.LogWarning("cannot find GameExperience for game:" + keyName);
		return null;
	}

	/// Populate all from player data.
	public static void populateAll(JSON[] array)
	{
		foreach (JSON gameJson in array)
		{
			LobbyGame game = LobbyGame.find(gameJson.getString("slots_game", ""));
			if (game == null)
			{
				continue;
			}
			GameExperience gameExperience = new GameExperience(game, gameJson);
		
#if UNITY_EDITOR
	#if (false)
			// Test SKU Game Unlock functionality.
			if (game.keyName == "osa04")
			{
				gameExperience.unlockType = "xpromo";
				gameExperience.xpromoTarget = "woz";
				gameExperience.xpromoUrl = "https://www.appstore.com";
				gameExperience.isSkuGameUnlock = true;
			}
	#endif
#endif

#if UNITY_EDITOR
			// Handy code for testing "free preview" functionality.
			// if (keyName == "t101")
			// {
			// 	gameExperience.isUnlocked = true;
			// }
#endif
	
			if (all.ContainsKey(game.keyName))
			{
				Debug.LogWarning("Duplicate GameExperience key: " + game.keyName);
			}
			else
			{
				all.Add(game.keyName, gameExperience);
			}
		}

		if (totalSpinCount == 0)
		{
			// If the player has not spun at all, then it's a new player.
			// Reset this counter to make sure the ask dialog comes up after spinning and returning to the lobby.
			PlayerPrefsCache.SetInt(Prefs.ASK_FOR_COINS_LAST_TIME, 0);
		}
	}

	/// Adds to the spin count for this game, and for the total spin count.
	public static void addSpin(string keyName)
	{
		//for things that happen "sometime before 2nd spin"
		int uaWrapperFTUEComplete = PlayerPrefsCache.GetInt(Prefs.UA_FTUE_COMPLETED);
		if (uaWrapperFTUEComplete == 1) //are we telling UA Wrapper?
		{
			if(1 == totalSpinCount)
			{
				PlayerPrefsCache.SetInt(Prefs.UA_FTUE_COMPLETED, 0);
				PlayerPrefsCache.Save();
				UAWrapper.Instance.OnFTUECompleted();
			}
		}

		GameExperience gameExperience = find(keyName);
		if (gameExperience != null)
		{
			gameExperience.spinCount++;
		}
		totalSpinCount++;

		UAWrapper.Instance.OnSpin(totalSpinCount);

		// Note: SlotsPlayer.instance.questCollectibles++ is not strictly needed for DailyChallenge v2 or PathToRiches,
		//       because they send a quest_update action to server before displaying the quest status dialog
		//       (technically we could avoid that request for spins-quests-only using the local counting below, but I 
		//        didnt bother).   Really this code only exists for DailyChallenge v1, which will soon be obsolete
		if ((Quest.activeQuest is DailyChallenge) && (DailyChallenge.gameKey == keyName))
		{
			if (DailyChallenge.challengeTask == "spin")
			{
				// Add a spin for the daily challenge quest.
				SlotsPlayer.instance.questCollectibles++;
			}
		}
	}
		
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		all = new Dictionary<string, GameExperience>();
		totalSpinCount = 0;
	}
}
