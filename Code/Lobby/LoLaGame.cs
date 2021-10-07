using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Class to deal with LoLa game data.
*/

public class LoLaGame : IResetGame
{
	public enum Feature
	{
		NONE,
		EARLY_ACCESS,
		FEATURED,		// Also known as "free preview".
		MYSTERY_GIFT,
		BIG_SLICE,
		STANDARD_PROGRESSIVE,
		MULTI_PROGRESSIVE,
		GIANT_PROGRESSIVE,
		XPROMO_GAME_UNLOCK,
		DOUBLE_FREE_SPINS,
		ROYAL_RUSH,
		REEVALUATOR_PROGRESSIVE,
		STICK_AND_WIN,
		SUPER_FAST_SPINS,
		PROGRESSIVE_FREE_SPINS,
		CASH_CHAIN
	}

	public enum UnlockMode
	{
		SNEAK_PREVIEW,
		COMING_SOON,
		UNLOCK_BY_LEVEL,
		UNLOCK_FOR_ALL,
		LEVEL_LOCK_EXPERIMENT,
		UNLOCK_BY_VIP_LEVEL,
		UNLOCK_BY_GOLD_PASS,
		UNLOCK_BY_SILVER_PASS
	}
	
	public LobbyGame game = null;
	public string rampOverride = "";
	public string wagerSetOverride = "";	// This might have to be changed to an int.
	public Feature feature = Feature.NONE;
	public UnlockMode unlockMode = UnlockMode.UNLOCK_BY_LEVEL;
	public UnlockMode fallbackMode = UnlockMode.UNLOCK_BY_LEVEL;	// Since sneak preview is time-limited, this is the mode to use if time has expired.
	public UnlockMode originalUnlockMode = UnlockMode.UNLOCK_BY_LEVEL;
	public int vipUnlockLevel = 0; // if the unlock mode is set to unlock_by_vip_level, there's an associated vip unlock level
	public bool isLevelLockedWithFeatures = false;	// If true, the game is level locked even when it has a feature like progressives or mystery gift.

	public static Dictionary<string, LoLaGame> all = new Dictionary<string, LoLaGame>();

	public List<LoLaLobbyDisplay> gameDisplays = new List<LoLaLobbyDisplay>();

	public ProgressiveJackpot.Type displayProgressiveType(Feature feature)
	{
		switch (feature)
		{
			case Feature.STANDARD_PROGRESSIVE:
				return ProgressiveJackpot.Type.STANDARD;
					
			case Feature.MULTI_PROGRESSIVE:
				return ProgressiveJackpot.Type.MULTI;
					
			case Feature.GIANT_PROGRESSIVE:
				return ProgressiveJackpot.Type.GIANT;

			case Feature.REEVALUATOR_PROGRESSIVE:
			case Feature.STICK_AND_WIN:
				return ProgressiveJackpot.Type.REEVALUATOR;
		}
		return ProgressiveJackpot.Type.NONE;
	}

	// Returns true if the game has any of the vip features enabled
	public bool hasVIPFeature
	{
		get
		{
			return feature == Feature.DOUBLE_FREE_SPINS; // doublefree spins is a VIP feature
		}
	}

	// Parse all the LoLa data into something useful.
	public static void populateAll(JSON data)
	{
		foreach (JSON json in data.getJsonArray("active_games"))
		{
			new LoLaGame(json);
		}

		if (LoLaLobby.vip != null)
		{
			// Now that the games are populated in all the lobbies they're defined in,
			// do another loop on the games in the VIP lobby to make sure no extra features or unlock modes are used.
			// We just ignore what may have been set in LoLa since they can't be used for VIP games.
			foreach (LoLaLobbyDisplay lolaDisplay in LoLaLobby.vip.displays)
			{
				if (lolaDisplay.game != null)
				{
					lolaDisplay.game.unlockMode = UnlockMode.UNLOCK_BY_LEVEL;

					if (!lolaDisplay.game.hasVIPFeature)
					{
						lolaDisplay.game.feature = Feature.NONE;
					}
				}
			}
		}
	}

	public LoLaGame(JSON json)
	{
		string gameKey = json.getString("game_key", "");
		if (!string.IsNullOrEmpty(gameKey))
		{
			LoLa.registerActiveGame(gameKey);	
		}
		
		game = LobbyGame.find(gameKey);
		
		if (game == null)
		{
			return;
		}

		game.lolaGame = this;	// Set up a two-way reference.
		
		rampOverride = json.getString("ramp_override", "");
		wagerSetOverride = json.getString("wager_set_override", "");
		isLevelLockedWithFeatures = json.getBool("is_locked_with_feature", false);

		if (!Data.liveData.getBool("ENABLE_EARLY_USER_LOBBIES", false))
		{
			string featureString = json.getString("extra_feature", "none").ToUpper();
			string unlockString = json.getString("unlock_mode", "none").ToUpper();
			string fallbackModeString = "";

			if (unlockString.Contains("->"))
			{
				string[] parts = unlockString.Split(new string[] { "->" }, System.StringSplitOptions.RemoveEmptyEntries);
				unlockString = parts[0];
				fallbackModeString = parts[1];
			}

			if (System.Enum.IsDefined(typeof(UnlockMode), unlockString))
			{
				unlockMode = (UnlockMode)System.Enum.Parse(typeof(UnlockMode), unlockString);

				// We use a/the sneak preview game in the MOTD for the feature. Also it's probably just a good idea to keep the keys on hand.
				switch (unlockMode)
				{
					case UnlockMode.SNEAK_PREVIEW:
						// So we don't mess up the MOTD. Also don't keep re-assigning.
						if (LobbyGame.sneakPreviewGame == null && game.unlockLevel > SlotsPlayer.instance.socialMember.experienceLevel)
						{
							LobbyGame.sneakPreviewGame = game;
						}
						break;

					case UnlockMode.UNLOCK_BY_VIP_LEVEL:
						if (game != null)
						{
							vipUnlockLevel = json.getInt( "vip_unlock_level", 0 );
							game.vipLevel = VIPLevel.find(vipUnlockLevel);
						}
						break;
				}
			}
			else
			{
				Debug.LogWarningFormat("LoLaGame unlock_mode {0} is invalid in data.", featureString);
			}
			
			if (fallbackModeString != "")
			{
				if (System.Enum.IsDefined(typeof(UnlockMode), fallbackModeString))
				{
					fallbackMode = (UnlockMode)System.Enum.Parse(typeof(UnlockMode), fallbackModeString);
	//				Debug.LogWarning("sneak preview fallback for " + game.keyName + ": " + sneakPreviewFallback.ToString());
				}
				else
				{
					Debug.LogWarningFormat("LoLaGame sneakPreviewFallback {0} is invalid in data.", fallbackModeString);
				}
			}

			if (System.Enum.IsDefined(typeof(Feature), featureString))
			{
				feature = (Feature)System.Enum.Parse(typeof(Feature), featureString);
			}
			else
			{
				Debug.LogWarningFormat("LoLaGame extra_feature {0} is invalid in data.", featureString);
			}
		}

		foreach (JSON lobbyJson in json.getJsonArray("lobby_display"))
		{
			gameDisplays.Add(new LoLaLobbyDisplay(lobbyJson, this));
		}

		all.Add(game.keyName, this);
	}
		
	// Standard find method.
	public static LoLaGame find(string keyName)
	{
		LoLaGame lolaGame;
		if (all.TryGetValue(keyName, out lolaGame))
		{
			return lolaGame;
		}
		return null;
	}

	public static void resetStaticClassData()
	{
		all = new Dictionary<string, LoLaGame>();
	}

}