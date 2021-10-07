using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SlotAction : ServerAction
{
	public enum ProgressBonusGameAccumulativeChoiceEnum
	{
		UNDEFINED = -1,
		PROGRESS = 0,
		COMPLETE
	};

	public const string SLOTS_SPIN = "slots_spin15";
	public const string SLOTS_SPIN_DEV = "slots_spin_dev";
	public const string SLOTS_START_GAME = "slots_start_game";
	public const string PLAY_BONUS_GAME = "play_bonus_game5";
	public const string COMPLETED_BONUS_GAME = "complete_bonus_game3";
	public const string SEEN_BONUS_SUMMARY_SCREEN = "bonus_game_summary_seen";
	public const string PLAY_CHOSEN_BONUS_GAME = "play_bonus_choice_game2";
	public const string ACCEPT_BONUS_GAME_CREDITS = "accept_bonus_game_credits";
	public const string PROGRESS_BONUS_GAME_ACCUMULATIVE = "progress_bonus_game_accumulative";

	private string gameId = "";
	private int betMultiplier = 0;
	private int wagerAmount = 0;
	private long flatWagerAmount = 0;
	private int tierId = -1;
	private int[] slotStops = null;
	private Dictionary<string, Dictionary<string, string>> layerToReelToStop = null;
	private Dictionary<string, int>[] independentStops = null;
	private string eventId = "";
	private bool shouldAwardXPMultiplier = false;
	private int forcedMysteryGiftWin = 0;
	private string serverCheatKey = ""; // Cheat key defined in SCAT under the game in the Scripted Results tab
	private string currentLobby = "";

	// The following are used for choosing bonus games
	private string gameKey = "";
	private string paytableSet = "";
	private string bonusGame = "";
	private string bonusGameGroup = "";

	// This is used when the player has a choice in bonus game outcomes, Example: Pawn01 Picking game or tpir01 Cliffhanger (although exact message sent is slightly different between these two)
	private int choiceIndex = -1;
	private ProgressBonusGameAccumulativeChoiceEnum accumulativeChoice = ProgressBonusGameAccumulativeChoiceEnum.UNDEFINED;

	// property names
	private const string GAME_ID = "game";
	private const string BET_MULTIPLIER = "wager_multiplier";
	private const string WAGER_AMOUNT = "wager_amount";
	private const string WAGER = "wager";
	private const string TIER_ID = "tier_id";
	private const string STOPS = "stops";	// When all the stops are sent as an map.
	private const string FORCED_STOPS = "forced_stops";	// The way to control stops for an independent end reel game.
	private const string SLOT_STOP_0 = "stop_0";
	private const string SLOT_STOP_1 = "stop_1";
	private const string SLOT_STOP_2 = "stop_2";
	private const string SLOT_STOP_3 = "stop_3";
	private const string SLOT_STOP_4 = "stop_4";
	private const string SLOT_STOP_5 = "stop_5";
	private const string EVENT_ID = "event";
	private const string BONUS_EVENT_ID = "bonus_game_key";	// The backend was written as bonus_game_key, but it's really an event id.
	private const string GAME_KEY = "slots_game";
	private const string BONUS_GAME_KEY = "bonus_game";
	private const string PAYTABLE_SET = "paytable_set";
	private const string BONUS_GAME_GROUP = "group";
	private const string XP_MULTIPLIER_ON = "xp_multiplier_on";
	private const string FORCE_MYSTERY_GIFT = "force_mystery_gift";
	private const string LOLA_VERSION = "lola_version";
	private const string CHOICE_INDEX = "choice_index";
	private const string SERVER_CHEAT_KEY = "cheat_key";
	private const string LOBBY_KEY = "lobby_key";
	private const string LAUNCH_METHOD = "launch_method";
	private const string PIN_POSITION = "pin_position";
	private const string PICK_INDEX = "pick_index";
	private const string ACCUMLATIVE_CHOICE_KEY = "player_choice";

	// Set by dev panel when the next spin should force a mystery gift, then reset to 0.
	// Values: 1 = random, 2 = wheel, 3 = scratch card, 4 = double bet
	public static int forceMysteryGiftWin = 0;
	public static string launchMethod = "";
	public static int pinPosition = -1;

	/// <summary>
	/// Server requires additional statistics on where the user launched the game from during each spin.
	/// Call this set the launch method to motd, lobby option, or challenge
	/// and the associated pin position in the lobby if the user is launching from the lobby card
	/// </summary>
	/// <param name="method"></param>
	/// <param name="lobbyPosition"></param>
	public static void setLaunchDetails(string method, int lobbyPosition = -1)
	{
		launchMethod = method;
		pinPosition = lobbyPosition;
	}

	public static void spin(string gameId, int betMultiplier, int wagerAmount, long flatWagerAmount, bool shouldAwardXPMultiplier, EventDelegate callback)
	{
		spin(SLOTS_SPIN, gameId, betMultiplier, wagerAmount, flatWagerAmount, shouldAwardXPMultiplier, "", -1, null, null, null, callback);
	}

	public static void forceServerCheat(string gameId, int betMultiplier, int wagerAmount, long flatWagerAmount, bool shouldAwardXPMultiplier, string serverCheatKey, EventDelegate callback)
	{
		spin(SLOTS_SPIN, gameId, betMultiplier, wagerAmount, flatWagerAmount, shouldAwardXPMultiplier, serverCheatKey, -1, null, null, null, callback);
	}

	public static void forceOutcome(string gameId, int betMultiplier, int wagerAmount, long flatWagerAmount, bool shouldAwardXPMultiplier, int tierId, int[] slotStops, EventDelegate callback)
	{
		spin(SLOTS_SPIN, gameId, betMultiplier, wagerAmount, flatWagerAmount, shouldAwardXPMultiplier, "", tierId, slotStops, null, null, callback);
	}

	public static void forceOutcome(string gameId, int betMultiplier, int wagerAmount, long flatWagerAmount, bool shouldAwardXPMultiplier, int tierId, int[] slotStops, Dictionary<string, Dictionary<string, string>> layerToReelToStop, EventDelegate callback)
	{
		spin(SLOTS_SPIN, gameId, betMultiplier, wagerAmount, flatWagerAmount, shouldAwardXPMultiplier, "", tierId, slotStops, layerToReelToStop, null, callback);
	}

	public static void forceOutcome(string gameId, int betMultiplier, int wagerAmount, long flatWagerAmount, bool shouldAwardXPMultiplier, int tierId, int[] slotStops, Dictionary<string, int>[] independentStops, EventDelegate callback)
	{
		spin(SLOTS_SPIN, gameId, betMultiplier, wagerAmount, flatWagerAmount, shouldAwardXPMultiplier, "", tierId, slotStops, null, independentStops, callback);
	}
	
	private static void spin
	(
		string actionType,
		string gameId,
		int betMultiplier,
		int wagerAmount,
		long flatWagerAmount,
		bool shouldAwardXPMultiplier,
		string serverCheatKey,
		int tierId,
		int[] slotStops,
		 Dictionary<string, Dictionary<string, string>> layerToReelToStop,
		Dictionary<string, int>[] independentStops,
		EventDelegate callback
	)
	{
#if ZYNGA_TRAMP
		AutomatedPlayer.spinRequested();
#endif
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.timeOutTimer = Time.time;
		}

		if (forceMysteryGiftWin > 0)
		{
			// Change this here since this kind of spin uses the normal spin() function.
			actionType = SLOTS_SPIN_DEV;
		}
		
		SlotAction action = new SlotAction(ActionPriority.IMMEDIATE, actionType);
		action.gameId = gameId;
		action.betMultiplier = betMultiplier;
		action.wagerAmount = wagerAmount;
		action.flatWagerAmount = flatWagerAmount;
		action.tierId = tierId;
		action.slotStops = slotStops;
		action.layerToReelToStop = layerToReelToStop;
		action.independentStops = independentStops;
		action.shouldAwardXPMultiplier = shouldAwardXPMultiplier;
		action.forcedMysteryGiftWin = forceMysteryGiftWin;
		action.serverCheatKey = serverCheatKey;
		action.currentLobby = getLobbyKey(gameId);

		Server.registerEventDelegate("slots_outcome", callback);
		
		// Resets with each spin.
		forceMysteryGiftWin = 0;
	}

	private static string getLobbyKey(string gameId)
	{
		if (CampaignDirector.richPass != null && CampaignDirector.richPass.isActive && RichPassCampaign.goldGameKeys.Contains(gameId))
		{
			return LobbyInfo.typeToString(LobbyInfo.Type.RICH_PASS, true);
		}
		
		if (LoLaLobby.vipRevamp != null && LoLaLobby.vipRevamp.gamesDict.ContainsKey(gameId))
		{
			return LobbyInfo.typeToString(LobbyInfo.Type.VIP_REVAMP, true);
		}
		
		return LobbyInfo.typeToString(LobbyLoader.lastLobby, true);
	}

	public static void startGame(string gameId, EventDelegate callback)
	{
		Server.registerEventDelegate("slots_game_started", callback);
		SlotAction action = new SlotAction(ActionPriority.HIGH, SLOTS_START_GAME);
		action.gameId = gameId;
		action.currentLobby = getLobbyKey(gameId);
		ServerAction.processPendingActions(true);

		//Send a push notif to friends saying you played this game
		//9/2/14 dtruong: Per Braxton holding off on sending this push notification
		//NotificationAction.sendStartPlayingNotifications(GameState.currentStateName);                                             
	}

	public static void playBonusGame(string eventId, string gameId)
	{
		SlotAction action = new SlotAction(ActionPriority.HIGH, PLAY_BONUS_GAME);
		action.eventId = eventId;
		action.gameId = gameId;
		ServerAction.processPendingActions(true);
	}

	public static void chooseBonusGame(string gameKey, string paytableSet, string bonusGame, string bonusGameGroup, int betMultiplier, EventDelegate callback)
	{
		SlotAction action = new SlotAction(ActionPriority.IMMEDIATE, PLAY_CHOSEN_BONUS_GAME);
		action.gameKey = gameKey;
		action.bonusGame = bonusGame;
		action.paytableSet = paytableSet;
		action.bonusGameGroup = bonusGameGroup;
		action.betMultiplier = betMultiplier;
		action.eventId = BonusGameManager.instance.challengeProgressEventId;
		action.currentLobby = getLobbyKey(gameKey);
		Server.registerEventDelegate("slots_outcome", callback);
		ServerAction.processPendingActions(true);
	}

	public static void bonusGameCompleted(string eventId, string gameId)
	{
		SlotAction action = new SlotAction(ActionPriority.IMMEDIATE, COMPLETED_BONUS_GAME);
		action.eventId = eventId;
		action.gameId = gameId;
	}

	//Action: accept_bonus_game_credits	 
	//A Pickem Game with a user choice to "accept" or "reject" the results of a pickem round (implemented as a wheel game) will award zero credits from the Bonus Game during the actual spin.
	//The client must then query the player who should make the choice and the client can then use this action to accept the credits from a particular round. 
	//The client must supply the event_id, the slots_game key name, and the choice_index of the round the user accepted. The event_id comes from the BonusGameChoiceEvent (bonus_game_choice)
	//Args example: { "event":"*******", "slots_game":"pawn01", "choice_index":"1"}	
	public static void acceptBonusGameCredits(string eventId, string gameKey, int choiceIndex)
	{
		SlotAction action = new SlotAction(ActionPriority.IMMEDIATE, ACCEPT_BONUS_GAME_CREDITS);
		action.eventId = eventId;
		action.gameKey = gameKey;
		action.choiceIndex = choiceIndex;
	}

	// Action: progress_bonus_game_accumulative
	public static void progressBonusGameAccumulative(string eventId, string gameKey, ProgressBonusGameAccumulativeChoiceEnum choiceEnum, int pickIndex)
	{
		SlotAction action = new SlotAction(ActionPriority.IMMEDIATE, PROGRESS_BONUS_GAME_ACCUMULATIVE);
		action.eventId = eventId;
		action.gameKey = gameKey;
		action.accumulativeChoice = choiceEnum;
		action.choiceIndex = pickIndex;
	}

	public static void seenBonusSummaryScreen(string eventId)
	{
		SlotAction action = new SlotAction(ActionPriority.IMMEDIATE, SEEN_BONUS_SUMMARY_SCREEN);
		action.eventId = eventId;
	}

	private SlotAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	private static void initPropertiesLookup()
	{
		// Multiple spin actions use the same set of properties, so define it once.
		string[] spinProperties = new string[]
		{
			GAME_ID,
			BET_MULTIPLIER,
			WAGER_AMOUNT,
			WAGER,
			SERVER_CHEAT_KEY,
			TIER_ID,
			XP_MULTIPLIER_ON,
			FORCE_MYSTERY_GIFT,
			STOPS,
			SLOT_STOP_0,
			SLOT_STOP_1,
			SLOT_STOP_2,
			SLOT_STOP_3,
			SLOT_STOP_4,
			SLOT_STOP_5,
			FORCED_STOPS,
			LOLA_VERSION,
			LOBBY_KEY,
			LAUNCH_METHOD,
			PIN_POSITION
		};
		
		_propertiesLookup = new Dictionary<string, string[]>();
		_propertiesLookup.Add(SLOTS_SPIN, spinProperties);
		_propertiesLookup.Add(SLOTS_SPIN_DEV, spinProperties);
		_propertiesLookup.Add(SLOTS_START_GAME, new string[] { GAME_ID, LOBBY_KEY });
		_propertiesLookup.Add(PLAY_BONUS_GAME, new string[] { EVENT_ID, GAME_ID, LOBBY_KEY });
		_propertiesLookup.Add(COMPLETED_BONUS_GAME, new string[] { EVENT_ID, GAME_ID });
		_propertiesLookup.Add(SEEN_BONUS_SUMMARY_SCREEN, new string[] { EVENT_ID});
		_propertiesLookup.Add(PLAY_CHOSEN_BONUS_GAME, new string[] { EVENT_ID, GAME_KEY, LOBBY_KEY, BONUS_GAME_KEY, PAYTABLE_SET, BET_MULTIPLIER, BONUS_GAME_GROUP});
		_propertiesLookup.Add(ACCEPT_BONUS_GAME_CREDITS, new string[] { EVENT_ID, GAME_KEY, CHOICE_INDEX});
		_propertiesLookup.Add(PROGRESS_BONUS_GAME_ACCUMULATIVE, new string[] { EVENT_ID, GAME_KEY, PICK_INDEX, ACCUMLATIVE_CHOICE_KEY });
	}

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				initPropertiesLookup();
			}
			return _propertiesLookup;
		}
	}
	private static Dictionary<string, string[]> _propertiesLookup = null;

	/// Appends all the specific action properties to json
	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		if (!propertiesLookup.ContainsKey(type))
		{
			Debug.LogError("No properties defined for action: " + type);
			return;
		}

		foreach (string property in propertiesLookup[type])
		{
			switch (property)
			{
				case GAME_ID:
					appendPropertyJSON(builder, property, gameId);
					break;
				case LOBBY_KEY:
					if (!string.IsNullOrEmpty(currentLobby) && Data.liveData.getBool("ENABLE_EARLY_USER_LOBBIES", false))
					{
						appendPropertyJSON(builder, property, currentLobby);
					}
					break;
				case LAUNCH_METHOD:
					appendPropertyJSON(builder, property, launchMethod);
					break;
				case PIN_POSITION:
					appendPropertyJSON(builder, property, pinPosition);
					break;
				case BET_MULTIPLIER:
					appendPropertyJSON(builder, property, betMultiplier);
					break;
				case WAGER_AMOUNT:
					appendPropertyJSON(builder, property, wagerAmount);
					break;
				case WAGER:
					appendPropertyJSON(builder, property, flatWagerAmount);
					break;
				case SERVER_CHEAT_KEY:
					if (serverCheatKey != "")
					{
						appendPropertyJSON(builder, property, serverCheatKey);
					}
					break;
				case TIER_ID:
					if (tierId != -1)
					{
						appendPropertyJSON(builder, property, tierId);
					}
					break;
				case CHOICE_INDEX:
					if (choiceIndex != -1)
					{
						appendPropertyJSON(builder, property, choiceIndex);
					}
					break;
				case PICK_INDEX:
					if (choiceIndex != -1)
					{
						appendPropertyJSON(builder, property, choiceIndex);
					}
					break;
				case ACCUMLATIVE_CHOICE_KEY:
					string choiceStr = convertProgressBonusGameAccumulativeChoiceEnumToString();
					if (!string.IsNullOrEmpty(choiceStr))
					{
						appendPropertyJSON(builder, property, choiceStr);
					}
					break;
				case EVENT_ID:
					appendPropertyJSON(builder, property, eventId);
					break;
				case GAME_KEY:
					appendPropertyJSON(builder, property, gameKey);
					break;
				case BONUS_GAME_KEY:
					appendPropertyJSON(builder, property, bonusGame);
					break;
				case PAYTABLE_SET:
					appendPropertyJSON(builder, property, paytableSet);
					break;
				case BONUS_GAME_GROUP:
					appendPropertyJSON(builder, property, bonusGameGroup);
					break;
				case XP_MULTIPLIER_ON:
					appendPropertyJSON(builder, property, shouldAwardXPMultiplier);
					break;				
				case FORCE_MYSTERY_GIFT:
					appendPropertyJSON(builder, property, forcedMysteryGiftWin);
					break;
				case LOLA_VERSION:
					appendPropertyJSON(builder, property, LoLa.version);
					break;
				case SLOT_STOP_0:
					if (slotStops != null && slotStops.Length > 0)
					{
						appendPropertyJSON(builder, property, slotStops[0]);
					}
					break;
				case STOPS:
					if (layerToReelToStop != null)
					{
						appendPropertyJSON(builder, property, layerToReelToStop);
					}
					break;
				case FORCED_STOPS:
					if (independentStops != null && independentStops.Length > 0)
					{
						appendPropertyJSON(builder, property, independentStops);
					}
					break;
				case SLOT_STOP_1:
					if (slotStops != null && slotStops.Length > 1)
					{
						appendPropertyJSON(builder, property, slotStops[1]);
					}
					break;
				case SLOT_STOP_2:
					if (slotStops != null && slotStops.Length > 2)
					{
						appendPropertyJSON(builder, property, slotStops[2]);
					}
					break;
				case SLOT_STOP_3:
					if (slotStops != null && slotStops.Length > 3)
					{
						appendPropertyJSON(builder, property, slotStops[3]);
					}
					break;
				case SLOT_STOP_4:
					if (slotStops != null && slotStops.Length > 4)
					{
						appendPropertyJSON(builder, property, slotStops[4]);
					}
					break;
				case SLOT_STOP_5:
					if (slotStops != null && slotStops.Length > 5)
					{
						appendPropertyJSON(builder, property, slotStops[5]);
					}
					break;
				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}
	}

	private string convertProgressBonusGameAccumulativeChoiceEnumToString()
	{
		switch (accumulativeChoice)
		{
			case ProgressBonusGameAccumulativeChoiceEnum.COMPLETE:
				return "complete";
			case ProgressBonusGameAccumulativeChoiceEnum.PROGRESS:
				return "progress";
			default:
				return "";
		}
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		//ServerAction.resetStaticClassData(); NEVER CALL THE BASE CLASS'S RESET!
		_propertiesLookup = null;
	}
}
