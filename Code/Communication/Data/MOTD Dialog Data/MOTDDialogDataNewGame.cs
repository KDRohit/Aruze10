using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MOTDDialogDataNewGame : MOTDDialogData
{
	// new game constants
	public const string MOTD_KEY = "new_game_placeholder";
	public const string DEFAULT_NEW_GAME_IMAGE_BACKGROUND_FORMAT = "motd/MOTD_{0}.jpg";
	public const string DEFAULT_NEW_GAME_IMAGE_BACKGROUND_FORMAT_PNG = "motd/MOTD_{0}.png";
	public const string NEW_GAME_MOTD_JPG = "motd/new_game/MOTD_{0}.jpg";
	public const string NEW_GAME_MOTD_PNG = "motd/new_game/MOTD_{0}.png";
	
	private const string DEFAULT_NEW_GAME_LOC_TITLE_STRING = "new_game";
	private const string DEFAULT_NEW_GAME_LOC_SUBHEADING_FORMAT = "motd_{0}_body_title";
	private const string DEFAULT_NEW_GAME_LOC_BODY_TEXT_FORMAT = "motd_{0}_body_text";
	private const string DEFAULT_NEW_GAME_LOC_ACTION_1_STRING = "play_now";
	private const string DEFAULT_NEW_GAME_COMMAND_ACTION_1_FORMAT = "game:{0}";
	private const int MAX_RECENTLY_SEEN_GAMES = 3;

	private bool hasInitialized = false;
	public List<string> recentlySeenNewGames = new List<string>();
	
	public override bool shouldShow
	{
		get
		{
			if (LoLaLobby.main == LoLaLobby.mainEarlyUser)
			{
				return false;
			}
			
			LobbyGame game = action1Game;
			if (game == null)
			{
				return false;
			}
			if (recentlySeenNewGames.Contains(game.keyName))
			{
				return false;
			}

			if (isLevelLocked)
			{
				return false;
			}

#if ZYNGA_PRODUCTION
			if (isNonProductionReadyGame(game.keyName))
			{
				return false;
			}
#endif

			return true;
		}
	}

	private bool isNonProductionReadyGame(string gameKey)
	{
		bool nonProductionReadyGameMOTD = false;
		SlotResourceData mapData = SlotResourceMap.getData(gameKey);
		if (mapData == null || mapData.gameStatus == SlotResourceData.GameStatus.NON_PRODUCTION_READY) //Don't show new game MOTDs for games that aren't production ready, or not in the build at all
		{
			nonProductionReadyGameMOTD = true;
			Debug.LogWarning("Suppressing a new game MOTD for a non production ready game.");
		}

		//Only don't want to show non production ready new game MOTDs on production builds
		return nonProductionReadyGameMOTD;
	}
	
	private bool isLevelLocked
	{
		get
		{
			return ExperimentWrapper.NewGameMOTDDialogGate.isInExperiment &&
				SlotsPlayer.instance.socialMember.experienceLevel < ExperimentWrapper.NewGameMOTDDialogGate.unlockLevel;
		}
	}
	
	public override string noShowReason
	{
		get
		{
			string reason = base.noShowReason;
			if (LoLaLobby.main == LoLaLobby.mainEarlyUser)
			{
				reason += "Player in mobile_main_early_user\n";
			}
			
			LobbyGame game = action1Game;
			if (game == null)
			{
				reason += "Game is null\n";
			}

			if (game != null && recentlySeenNewGames.Contains(game.keyName))
			{
				reason += "Game is in the recently seen list: {";
				foreach (string key in recentlySeenNewGames)
				{
					reason += key + ",";
				}
				reason += "}\n";
			}
			
			if (isLevelLocked)
			{
				reason += "In the new game motd gate exp and below level: " + ExperimentWrapper.NewGameMOTDDialogGate.unlockLevel + "\n";
			}
#if ZYNGA_PRODUCTION
			if (isNonProductionReadyGame(game.keyName))
			{
				reason += "New Game is not ready for production: " + game.keyName + "\n";;
			}
#endif			
			return reason;
		}
	}

	public override void setData(JSON item)
	{
		hasInitialized = true;
		this.keyName = item.getString("key_name", "");
		this.sortIndex = item.getInt("sort_index", int.MaxValue);
		this.argument = item.getString("argument", "");

		this.appearance = item.getString("loc_key_appearance", "");
		this.maxViews = item.getInt("max_views", 1);
		this.statName = item.getString("stat_name", "");

		// New MOTD Framework Setup
		this.shouldShowAppEntry = item.getBool("show_location_entry", false);
		this.shouldShowRTL = item.getBool("show_location_rtl", false);
		this.shouldShowVip = item.getBool("show_location_vip", false);
		this.shouldShowPreLobby = item.getBool("show_location_prelobby", false);
		
		// Get game key from lola.
		string newGameKey = LoLa.newGameMotdKey;
		if (!string.IsNullOrEmpty(newGameKey))
		{
			setNewGameData(newGameKey);
		}
	}

	public void handleLolaNewGame(string gameKey)
	{
		setNewGameData(gameKey);

		string seenListString = PlayerPrefsCache.GetString(CustomPlayerData.RECENTLY_VIEWED_NEW_GAME_MOTD_MOBILE, "");
		if (seenListString != "")
		{
			recentlySeenNewGames.AddRange(seenListString.Split(','));
		}
	}

	public void markNewGameSeen(LobbyGame game)
	{
		// Mark this new game MOTD as being seen.
		recentlySeenNewGames.Add(game.keyName);
		while (recentlySeenNewGames.Count > MAX_RECENTLY_SEEN_GAMES)
		{
			recentlySeenNewGames.RemoveAt(0);
		}
		PlayerPrefsCache.SetString(CustomPlayerData.RECENTLY_VIEWED_NEW_GAME_MOTD_MOBILE, string.Join(",", recentlySeenNewGames.ToArray()));
	}	

	public override bool show()
	{
		NewGameMOTD.showDialog(this);
		return true;
	}

	private void setNewGameData(string key)
	{
		if (!hasInitialized)
		{
			// If we haven't initialized yet then back out and we will have the data
			// from LoLa when we initialize.
			return;
		}
		
		string newGameTitle = DEFAULT_NEW_GAME_LOC_TITLE_STRING;
		string newGameImageBackground = string.Format(NEW_GAME_MOTD_PNG, key);
		string newGameSubheading = string.Format(DEFAULT_NEW_GAME_LOC_SUBHEADING_FORMAT, key);
		string newGameBodyText = string.Format(DEFAULT_NEW_GAME_LOC_BODY_TEXT_FORMAT, key);
		string newGameAction1 = DEFAULT_NEW_GAME_LOC_ACTION_1_STRING;
		string newGameCommandAction1 = string.Format(DEFAULT_NEW_GAME_COMMAND_ACTION_1_FORMAT, key);
		this.imageBackground = newGameImageBackground;

		this.locTitle = newGameTitle;
		this.locSubheading = newGameSubheading;
		this.locBodyTitle = "";
		this.locBodyText = newGameBodyText;
		this.locAction1 = newGameAction1;
		this.locAction2 = "";
		this.commandAction1 = newGameCommandAction1;
		this.commandAction2 = "";
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{

	}
}
