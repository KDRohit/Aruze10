using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Class to deal with LoLa lobby data.
*/

public class LoLaLobby : IResetGame
{
	// =============================
	// PUBLIC
	// =============================
	public string keyName 					= "";
	public List<LoLaLobbyDisplay> displays  = new List<LoLaLobbyDisplay>();
	public List<int> pinPositions 			= new List<int>();	// Only worry about the x position on mobile.
	public string lobbyWagerSet				= "default";
	public string backgroundAsset { get; private set; }
	public int lobbyLevel = 0;

	public static LoLaLobby main 			= null;
	public static LoLaLobby mainEarlyUser 	= null;
	public static LoLaLobby vip 			= null;
	public static LoLaLobby vipRevamp 		= null;
	public static LoLaLobby maxVoltage 		= null;
	public static LoLaLobby loz 			= null;
	public static LoLaLobby slotVentures	= null;
	public static LoLaLobby richPass        = null;
	public static JSON[] rawLobbyData		= null;

	// lookups
	public Dictionary<string, LoLaLobbyDisplay> gamesDict = new Dictionary<string, LoLaLobbyDisplay>();
	public static Dictionary<string, LoLaLobby> all = new Dictionary<string, LoLaLobby>();
	public static Dictionary<string, LoLaLobby> eosControlled = new Dictionary<string, LoLaLobby>();
	

	// =============================
	// CONST
	// =============================
	public const string MOBILE_LOZ 				= "mobile_loz";
	public const string MOBILE_VIP 				= "mobile_vip";
	public const string MOBILE_VIP_REVAMP 		= "mobile_vip_revamp";
	public const string MOBILE_MAX_VOLTAGE 		= "mobile_max_voltage";
	public const string MOBILE_MAIN 			= "mobile_main";
	public const string MOBILE_MAIN_EARLY_USER 	= "mobile_main_early_user";
	public const string MOBILE_SIN_CITY 		= "mobile_sin_city";
	public const string MOBILE_SLOTVENTURE  	= "mobile_slotventure";
	public const string MOBILE_RICH_PASS		= "mobile_gold_pass";

	public const string DEFAULT_WAGER_SET 	= "default"; // when this is set, games just use their normal wager set for this lobby
	public const string LOBBY_BG_PATH       = "lobby_backgrounds/{0}";

	// lobby specific click sound overrides
	public const string MAX_VOLTAGE_CLICK_SOUND = "MVSelectGame";

	public LoLaLobby(JSON json)
	{
		backgroundAsset = json.getString("background_art", "");

		if (!string.IsNullOrEmpty(backgroundAsset))
		{
			backgroundAsset = string.Format(LOBBY_BG_PATH, backgroundAsset);
		}

		JSON playerCriteria = json.getJSON("player_criteria");
		if (playerCriteria != null)
		{
			lobbyLevel = playerCriteria.getInt("max_level", 0);
		}
		
		keyName = json.getString("lobby_key", "");

		if (all.ContainsKey(keyName))
		{
			Debug.LogErrorFormat("Duplicate lobby_key in LoLaLobby data: '{0}', 2nd instance skipped", keyName);
			return;
		}
		
		// On mobile, we only pin things to the top row of options, so ignore the y value.
		foreach (JSON pin in json.getJsonArray("pin_positions"))
		{
			pinPositions.Add(pin.getInt("x", -1));
		}

		all.Add(keyName, this);
	}
	
	// Parse all the LoLa data into something useful.
	public static void populateAll(JSON data)
	{
		foreach (JSON json in data.getJsonArray("lobbies"))
		{
			new LoLaLobby(json);
		}

		sortLobbies();
		evalEarlyUser();
	}

	/// <summary>
	///   Adds wager sets that need to be set for this lobby, see scat > common > lobbies > <some lobby> > wager set feature
	/// </summary>
	public static void addWagerSets()
	{
		if (rawLobbyData != null)
		{
			foreach (JSON lobby in rawLobbyData)
			{
				string wagerSet = lobby.getString("wager_set_feature", DEFAULT_WAGER_SET);
				LoLaLobby lolaLobby = LoLaLobby.find(lobby.getString("key_name", ""));

				if (lolaLobby != null && wagerSet != DEFAULT_WAGER_SET)
				{
					lolaLobby.lobbyWagerSet = wagerSet;
				}
			}
		}
	}

	private static void sortLobbies()
	{
		foreach (KeyValuePair<string, LoLaLobby> lobby in all)
		{
			switch (lobby.Key)
			{
				case MOBILE_MAIN:
					main = lobby.Value;
					break;

				case MOBILE_MAIN_EARLY_USER:
					mainEarlyUser = lobby.Value;
					break;

				case MOBILE_VIP:
					vip = lobby.Value;
					break;

				case MOBILE_LOZ:
					if (ExperimentWrapper.LOZChallenges.isInExperiment)
					{
						loz = find(MOBILE_LOZ);
						eosControlled.Add(MOBILE_LOZ, loz);
					}
					break;

				case MOBILE_VIP_REVAMP:
					if (ExperimentWrapper.VIPLobbyRevamp.isInExperiment)
					{
						vipRevamp = find(MOBILE_VIP_REVAMP);
						eosControlled.Add(MOBILE_VIP_REVAMP, vipRevamp);
					}
					break;

				case MOBILE_MAX_VOLTAGE:
					maxVoltage = find(MOBILE_MAX_VOLTAGE);
					eosControlled.Add(MOBILE_MAX_VOLTAGE, maxVoltage);
					break;

				case MOBILE_SLOTVENTURE:
					if (ExperimentWrapper.Slotventures.isInExperiment)
					{
						slotVentures = find(MOBILE_SLOTVENTURE);
						eosControlled.Add(MOBILE_SLOTVENTURE, slotVentures);
					}
					break;
				
				case MOBILE_RICH_PASS:
					if (ExperimentWrapper.RichPass.isInExperiment)
					{
						richPass = find(MOBILE_RICH_PASS);
						eosControlled.Add(MOBILE_RICH_PASS, richPass);
					}
					break;

				default:
					eosControlled.Add(lobby.Key, lobby.Value);
					break;
			}
		}
	}

	public static bool shouldUseEarlyUserLobby()
	{
		bool isEnabled = Data.liveData.getBool("ENABLE_EARLY_USER_LOBBIES", false);
		if (isEnabled && SlotsPlayer.instance != null && SlotsPlayer.instance.socialMember != null)
		{
			if (mainEarlyUser != null && SlotsPlayer.instance.socialMember.experienceLevel < mainEarlyUser.lobbyLevel)
			{
				return true;
			}
		}

		return false;
	}

	public static void evalEarlyUser()
	{
		if (shouldUseEarlyUserLobby())
		{
			main = mainEarlyUser;
		}
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	public LoLaLobbyDisplay findGame(string gameKey)
	{
		LoLaLobbyDisplay lobbydisplay;
		if (gamesDict.TryGetValue(gameKey, out lobbydisplay))
		{
			return lobbydisplay;
		}
		return null;
	}
	
	public static LoLaLobbyDisplay findGameInEOS(string gameKey)
	{
		foreach (KeyValuePair<string, LoLaLobby> entry in eosControlled)
		{
			if (entry.Value == null)
			{
				continue;
			}
			
			LoLaLobbyDisplay display = entry.Value.findGame(gameKey);
			if (display != null)
			{
				return display;
			}
		}

		return null;
	}
		
	// Standard find method is not used outside of initialization.
	// Use LoLaLobby.main and LoLaLobby.vip instead.
	public static LoLaLobby find(string keyName)
	{
		LoLaLobby lobby;
		if (all.TryGetValue(keyName, out lobby))
		{
			return lobby;
		}
		return null;
	}

	public static string findKeyByLobbyInfo(LobbyInfo.Type type)
	{
		switch(type)
		{
			case LobbyInfo.Type.LOZ:
				return MOBILE_LOZ;
			case LobbyInfo.Type.RICH_PASS:
			case LobbyInfo.Type.MAIN:
				if (LoLaLobby.main == LoLaLobby.mainEarlyUser)
				{
					return MOBILE_MAIN_EARLY_USER;
				}
				return MOBILE_MAIN;
			case LobbyInfo.Type.MAX_VOLTAGE:
				return MOBILE_MAX_VOLTAGE;
			case LobbyInfo.Type.SIN_CITY:
				return MOBILE_SIN_CITY;
			case LobbyInfo.Type.SLOTVENTURE:
				return MOBILE_SLOTVENTURE;
			case LobbyInfo.Type.VIP_REVAMP:
			case LobbyInfo.Type.VIP:
				return MOBILE_VIP_REVAMP;
		}

		return "";
	}

	// Searches only lobbies that are controlled via eos also
	public static LoLaLobby findEOSLobby(string keyName)
	{
		LoLaLobby lobby;
		if (eosControlled.TryGetValue(keyName, out lobby))
		{
			return lobby;
		}
		return null;
	}

	/// <summary>
	///   Returns a lola lobby instance that contains the specified game key
	/// </summary>
	public static LoLaLobby findWithGame(string gameKey)
	{
		foreach (KeyValuePair<string, LoLaLobby> entry in all)
		{
			if (entry.Value.findGame(gameKey) != null)
			{
				return entry.Value;
			}
		}
		return null;
	}

	/// <summary>
	/// Finds the EOS with game.
	/// </summary>
	/// <returns>The EOS controlled LoLaLobby that contains the specified gamekey</returns>
	/// <param name="gameKey">Game key.</param>
	public static LoLaLobby findEOSWithGame(string gameKey)
	{
		foreach (KeyValuePair<string, LoLaLobby> entry in eosControlled)
		{
			if (entry.Value.findGame(gameKey) != null)
			{
				return entry.Value;
			}
		}
		return null;
	}

	public string getClickOverride()
	{
		if (this == maxVoltage)
		{
			return MAX_VOLTAGE_CLICK_SOUND;
		}

		if (EliteManager.hasActivePass)
		{
			return EliteManager.ELITE_STINGER;
		}
		
		return null;
	}
		
	public static void resetStaticClassData()
	{
		all = new Dictionary<string, LoLaLobby>();
		eosControlled = new Dictionary<string, LoLaLobby>();
		main = null;
		vip = null;
		loz = null;
		vipRevamp = null;
		maxVoltage = null;
	}
}
