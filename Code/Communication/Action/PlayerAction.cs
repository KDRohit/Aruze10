using UnityEngine;
using System.Collections.Generic;

/**
ServerAction class for handling generic player actions.
*/
public class PlayerAction : ServerAction
{
	public const string GAME_RESET = "game_reset";
	public const string RESET_PLAYER = "reset_player"; // Dev only
	public const string ADD_CREDITS = "add_credits"; // Dev only
	public const string ADD_XP = "add_experience"; // Dev only
	public const string ADD_LEVELS = "add_levels"; // Dev only
	public const string UNLOCK_SLOTS_GAME = "unlock_slots_game";
	public const string DEV_UNLOCK_SLOTS_GAME = "add_game_key"; // Dev only
	public const string ADD_VIP_POINTS = "add_vip_points"; // Dev only
	public const string RESET_PLAYER_VIP = "reset_player_vip"; //Dev only
	public const string TUTORIAL_SEEN = "tutorial_seen";
	public const string TIMED_XP_MULTIPLIER_MOTD_SEEN = "timed_xp_multiplier_motd_seen";
#if RWR
	public const string RWR_PROMO_ENTER = "enter_rwr_promo";
#endif
	public const string ADD_UNLOCK_POINTS = "add_keys";	// Backend is nonconforming with terminology. // Dev only
	public const string DISCLAIMER_READ = "disclaimer_read";
	public const string RESET_PLAYER_REQUESTS = "reset_player_requests"; // Dev only
	public const string RESET_MIGRATION_STATUS = "reset_migration_status";
	public const string CHANGE_TIMESTAMP = "change_timestamp"; // Dev only
	public const string GET_FRIEND_LIST = "get_friend_list";
	public const string ACCEPT_TOS = "tos_accepted";
	public const string CUSTOM_TIMESTAMP = "save_mytimestamp";
	public const string REFRESH_DATA = "refresh_data";
	public const string GET_MOTD_LIST = "motd_get_new_list";
	public const string MARK_MOTD_SEEN = "motd_seen";
	public const string CLEAR_MOTD_SEEN = "motd_clear";
	public const string SELECT_GAME_UNLOCK = "select_game_unlock";
	public const string CHARM_ACTIVATE = "charm_activate";
	public const string GET_RAINY_DAY_DATA = "get_rainy_day_data";
	public const string CREDIT_SWEEPSTAKES_COLLECT = "coin_sweepstakes_collect";
	public const string SELECT_CHARMS = "select_charms";
	public const string CHARM_GRANT_ACCEPT = "one_time_charm_grant_accept";
	public const string PLAY_VIP_GIFTED_FREESPIN_GAME = "play_vip_gifted_freespin_game";
	public const string UPDATE_CURRENCY = "update_currency";

	private long amount = 0;
	private string gameID = "";
	private string tutorial = "";
	private string email = "";
	private string requestType = "";
	private string timeStampKey = "";
	private string timeStampValue = "";
	private string customTimeStampKey = "";
	private string customTimeStampValue = "";
	private List<string> refreshFields;
	private string motdKey = "";
	private string eventID = "";
	private string gameKey = "";
	private string charmType = "";
	private string currency = "";
	private int levels = 0;
	private int creditSweepstakesWinVersion = 0;
	public string charmEventID = "";

	//property names
	private const string AMOUNT = "amount";
	private const string GAME_ID = "slots_game";
	private const string TUTORIAL = "tutorial";
	private const string EMAIL = "email";
	private const string REQUEST_TYPE = "request_type";
	private const string TIMESTAMP_VALUE = "timestamp_value";
	private const string TIMESTAMP_KEY = "timestamp_key";
	private const string CUSTOM_TIMESTAMP_KEY = "mytimestamp_key";
	private const string CUSTOM_TIMESTAMP_VALUE = "mytimestamp_value";
	private const string REFRESH_FIELDS = "fields";
	private const string MOTD_KEY = "motd_key";
	private const string EVENT_ID = "event";
	private const string GAME_KEY = "game_key";
	private const string CHARM_KEY = "charm_key";
	private const string CHARM_KEYS = "charm_keys";
	private const string LEVELS = "levels";
	private const string CURRENCY = "currency";
	private const string CREDIT_SWEEPSTAKES_WIN_VERSION = "coin_sweepstakes_win_version";
	private const string CHARM_EVENT_ID = "eventid";

	private PlayerAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	public static void getFriendsList(EventDelegate callback)
	{
		Server.registerEventDelegate("friend_list", callback, true);
		new PlayerAction(ActionPriority.LOW, GET_FRIEND_LIST);
	}
	
	public static void getFriendsListAgain(System.Action preActionCallback)
	{
		if (preActionCallback != null)
		{
			preActionCallback();
		}
		new PlayerAction(ActionPriority.HIGH, GET_FRIEND_LIST);
		ServerAction.processPendingActions(true);
	}
	
	public static void seeTutorial(string tutorial)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, TUTORIAL_SEEN);
		action.tutorial = tutorial;
		ServerAction.processPendingActions(true);
	}

	public static void updateCurency(string currencyCode)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, UPDATE_CURRENCY);
		action.currency = currencyCode;
		ServerAction.processPendingActions(true);
	}

	public static void seeXPMultiplierDialog()
	{
		new PlayerAction(ActionPriority.HIGH, TIMED_XP_MULTIPLIER_MOTD_SEEN);
		ServerAction.processPendingActions(true);
	}

#if RWR
	public static void enterRWRPromo(string email)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, RWR_PROMO_ENTER);
		action.email = email;
		ServerAction.processPendingActions(true);
	}
#endif
	
	public static void resetPlayer()
	{
		Debug.Log("Resetting Player");
		new PlayerAction(ActionPriority.HIGH, RESET_PLAYER);
		ServerAction.processPendingActions(true);
	}

	public static void resetVIPStatus()
	{
		Debug.Log("Resetting vip status");
		new PlayerAction(ActionPriority.HIGH, RESET_PLAYER_VIP);
		ServerAction.processPendingActions(true);
	}

	public static void getRainyDayData()
	{
		new PlayerAction(ActionPriority.HIGH, GET_RAINY_DAY_DATA);
	}
	
	public static void collectCreditSweepstakes(int version)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, CREDIT_SWEEPSTAKES_COLLECT);
		action.creditSweepstakesWinVersion = version;
		ServerAction.processPendingActions(true);
	}

	public static void resetRequests()
	{
		Debug.Log("Resetting Player requests");
		// Ben made this reset action so it takes a single type of action to reset,
		// but we want to just reset them all for now.
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, RESET_PLAYER_REQUESTS);
		action.requestType = "beta_invite";

		action = new PlayerAction(ActionPriority.HIGH, RESET_PLAYER_REQUESTS);
		action.requestType = "challenge_result";

		action = new PlayerAction(ActionPriority.HIGH, RESET_PLAYER_REQUESTS);
		action.requestType = "invite";

		action = new PlayerAction(ActionPriority.HIGH, RESET_PLAYER_REQUESTS);
		action.requestType = "send_challenge_bonus";

		action = new PlayerAction(ActionPriority.HIGH, RESET_PLAYER_REQUESTS);
		action.requestType = "send_credits";

		action = new PlayerAction(ActionPriority.HIGH, RESET_PLAYER_REQUESTS);
		action.requestType = "send_credits_sends";

		action = new PlayerAction(ActionPriority.HIGH, RESET_PLAYER_REQUESTS);
		action.requestType = "send_gift_bonus";

		ServerAction.processPendingActions(true);
	}

	public static void addCredits(long amt)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, ADD_CREDITS);
		action.amount = amt;
		ServerAction.processPendingActions(true);
	}

	public static void addXP(long amt)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, ADD_XP);
		action.amount = amt;
		ServerAction.processPendingActions(true);
	}

	public static void addLevels(int levels)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, ADD_LEVELS);
		action.levels = levels;
		ServerAction.processPendingActions(true);
	}

	public static void addUnlockPoints(long amt)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, ADD_UNLOCK_POINTS);
		action.amount = amt;
		ServerAction.processPendingActions(true);
	}

	/// This is a dev panel-only function.
	/// DO NOT USE FOR ADDING POINTS FOR LEGIT ACTIONS.
	public static void addVIPPoints(long amt)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, ADD_VIP_POINTS);
		action.amount = amt;
		ServerAction.processPendingActions(true);
	}

	public static void unlockGame(string gameID)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, UNLOCK_SLOTS_GAME);
		action.gameID = gameID;
		ServerAction.processPendingActions(true);
	}

	// Indicate to the server that the disclaimer has been read.
	// This will ensure that the player data "has_read_disclaimer" will be true on the next load.
	public static void disclaimerRead()
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, DISCLAIMER_READ);
		action.type = DISCLAIMER_READ;
		ServerAction.processPendingActions(true);
	}
	
	// Reset migration status so we can test the 30K migration popup.
	public static void resetMigrationStatus()
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, RESET_MIGRATION_STATUS);
		action.type = RESET_MIGRATION_STATUS;
		ServerAction.processPendingActions(true);
	}
	
	// For now, instead of dynamically, we set the start date of the project to sometime in 2013 for testing purposes.
	public static void changeTimeStamp()
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, CHANGE_TIMESTAMP);
		action.type = CHANGE_TIMESTAMP;
		action.timeStampKey = "started_playing_at";
		action.timeStampValue = "2013-10-01 16:00:00";
		ServerAction.processPendingActions(true);
	}
	
	// Tell the server that the user has accepted the new terms of service.
	public static void acceptTermsOfService()
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, ACCEPT_TOS);
		action.type = ACCEPT_TOS;
		ServerAction.processPendingActions(true);
	}
	
	// Use the custom timestamp functionality to store a timestamp for a keyname decided on the client
	// This will then be sent down with the player data on future logins
	public static void saveCustomTimestamp(string name, string timestamp = "")
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, CUSTOM_TIMESTAMP);
		action.type = CUSTOM_TIMESTAMP;
		action.customTimeStampKey = name;
		action.customTimeStampValue = timestamp;
	}

	// Sending the refresh data to the server, telling it with fields we want to refresh.
	public static void refreshData(List<string> fields)
	{
		Server.registerEventDelegate(REFRESH_DATA, RefreshableData.onRefreshData);
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, REFRESH_DATA);
		action.refreshFields = new List<string>(fields);
	}
	
	// Request a new sorted motd priority list from the server.
	public static void getNewMotdList()
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, GET_MOTD_LIST);
		action.type = GET_MOTD_LIST;
		ServerAction.processPendingActions(true);
	}
	
	public static void markMotdSeen(string keyName, bool isSeen = true)
	{
		if (string.IsNullOrEmpty(keyName))
		{
			// Dont try to send up an action without and MOTD key.
			return;
		}
		bool isProduction = false;
#if ZYNGA_PRODUCTION
		isProduction = true;
#endif
		if (isProduction || PlayerPrefsCache.GetInt(DebugPrefs.MARK_MOTDS_SEEN, 1) == 1)
		{
			string actionType = isSeen ? MARK_MOTD_SEEN : CLEAR_MOTD_SEEN;
			PlayerAction action = new PlayerAction(ActionPriority.LOW, actionType);
			action.type = actionType;
			action.motdKey = keyName;
		}
	}

	public static void selectGameUnlock(string gameKey, string eventID)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, SELECT_GAME_UNLOCK);
		action.gameKey = gameKey;
		action.eventID = eventID;
		ServerAction.processPendingActions(true);
	}

	public static void devGameUnlock(string gameKey)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, DEV_UNLOCK_SLOTS_GAME);
		action.gameKey = gameKey;
		ServerAction.processPendingActions(true);
	}

	public static void selectGameUnlock(List<string> gameKeys, string eventID)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, SELECT_GAME_UNLOCK);
		string gameList = "";
		for (int i = 0; i < gameKeys.Count; i++)
		{
			gameList += gameKeys[i];
			if (i < gameKeys.Count - 1 )
			{
				// If this is not the last value in the array, then add a comma.
				gameList += ",";
			}
		}
		action.gameKey = gameList;
		action.eventID = eventID;
		ServerAction.processPendingActions(true);
	}

	public static void activateCharm(string charmType)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, CHARM_ACTIVATE);
		action.charmType = charmType;
		ServerAction.processPendingActions(true);
	}

	public static void selectCharms(string eventID, string charmType)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, SELECT_CHARMS);
		action.charmType = charmType;
		action.eventID = eventID;
		ServerAction.processPendingActions(true);
	}

	public static void acceptCharmGrant(string eventID)
	{
		PlayerAction action = new PlayerAction(ActionPriority.HIGH, CHARM_GRANT_ACCEPT);
		action.eventID = eventID;
		ServerAction.processPendingActions(true);
	}
	
	public static void playGiftedVipFreeSpins()
	{
		#pragma warning disable 219 // The variable 'action' is assigned but its balue is never used (CS0219)
		PlayerAction action = new PlayerAction(ActionPriority.IMMEDIATE, PLAY_VIP_GIFTED_FREESPIN_GAME);
		ServerAction.processPendingActions(true);
		#pragma warning restore 219
	}
	////////////////////////////////////////////////////////////////////////

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(RESET_PLAYER, new string[] {});
				_propertiesLookup.Add(GAME_RESET, new string[] {});
				_propertiesLookup.Add(RESET_PLAYER_REQUESTS, new string[] {REQUEST_TYPE});
				_propertiesLookup.Add(DISCLAIMER_READ, new string[] {});
				_propertiesLookup.Add(ADD_CREDITS, new string[] {AMOUNT});
				_propertiesLookup.Add(ADD_XP, new string[] {AMOUNT});
				_propertiesLookup.Add(ADD_LEVELS, new string[] {LEVELS});
				_propertiesLookup.Add(ADD_UNLOCK_POINTS, new string[] {AMOUNT});
				_propertiesLookup.Add(ADD_VIP_POINTS, new string[] {AMOUNT});
				_propertiesLookup.Add(UNLOCK_SLOTS_GAME, new string[] {GAME_ID});
				_propertiesLookup.Add(DEV_UNLOCK_SLOTS_GAME, new string[] {GAME_KEY});
				_propertiesLookup.Add(TUTORIAL_SEEN, new string[] {TUTORIAL});
				_propertiesLookup.Add(TIMED_XP_MULTIPLIER_MOTD_SEEN, new string[] {});
#if RWR
				_propertiesLookup.Add(RWR_PROMO_ENTER, new string[] {EMAIL});
#endif
				_propertiesLookup.Add(RESET_MIGRATION_STATUS, new string[] {});
				_propertiesLookup.Add(CHANGE_TIMESTAMP, new string[] {TIMESTAMP_KEY, TIMESTAMP_VALUE});
				_propertiesLookup.Add(GET_FRIEND_LIST, new string[] {});
				_propertiesLookup.Add(ACCEPT_TOS, new string[] {});
				_propertiesLookup.Add(CUSTOM_TIMESTAMP, new string[] {CUSTOM_TIMESTAMP_KEY, CUSTOM_TIMESTAMP_VALUE});
				_propertiesLookup.Add(REFRESH_DATA, new string[]{REFRESH_FIELDS});
				_propertiesLookup.Add(GET_MOTD_LIST, new string[]{});
				_propertiesLookup.Add(MARK_MOTD_SEEN, new string[]{MOTD_KEY});
				_propertiesLookup.Add(CLEAR_MOTD_SEEN, new string[]{MOTD_KEY});
				_propertiesLookup.Add(SELECT_GAME_UNLOCK, new string[]{GAME_KEY, EVENT_ID});
				_propertiesLookup.Add(CHARM_ACTIVATE, new string[] {CHARM_KEY});
				_propertiesLookup.Add(GET_RAINY_DAY_DATA, new string[] {});
				_propertiesLookup.Add(CREDIT_SWEEPSTAKES_COLLECT, new string[] {CREDIT_SWEEPSTAKES_WIN_VERSION});
				_propertiesLookup.Add(SELECT_CHARMS, new string[] {CHARM_KEYS, EVENT_ID});
				_propertiesLookup.Add(CHARM_GRANT_ACCEPT, new string[] {EVENT_ID});
				_propertiesLookup.Add(PLAY_VIP_GIFTED_FREESPIN_GAME, new string[] {});
				_propertiesLookup.Add(UPDATE_CURRENCY, new string[] {CURRENCY});
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
				case AMOUNT:
					appendPropertyJSON(builder, property, amount);
					break;
				case GAME_ID:
					appendPropertyJSON(builder, property, gameID);
					break;
				case TUTORIAL:
					appendPropertyJSON(builder, property, tutorial);
					break;
				case EMAIL:
					appendPropertyJSON(builder, property, email);
					break;
				case REQUEST_TYPE:
					appendPropertyJSON(builder, property, requestType);
					break;
				case TIMESTAMP_VALUE:
					appendPropertyJSON(builder, property, timeStampValue);
					break;
				case TIMESTAMP_KEY:
					appendPropertyJSON(builder, property, timeStampKey);
					break;
				case CUSTOM_TIMESTAMP_KEY:
					appendPropertyJSON(builder, property, customTimeStampKey);
					break;
				case CUSTOM_TIMESTAMP_VALUE:
					appendPropertyJSON(builder, property, customTimeStampValue);
					break;
				case REFRESH_FIELDS:
					appendPropertyJSON(builder, property, refreshFields);
					break;
				case MOTD_KEY:
					appendPropertyJSON(builder, property, motdKey);
					break;
				case GAME_KEY:
					appendPropertyJSON(builder, property, gameKey);
					break;
				case EVENT_ID:
					appendPropertyJSON(builder, property, eventID);
					break;
				case CHARM_KEY:
				case CHARM_KEYS:
					appendPropertyJSON(builder, property, charmType);
					break;
				case LEVELS:
					appendPropertyJSON(builder, property, levels);
					break;
				case CREDIT_SWEEPSTAKES_WIN_VERSION:
					appendPropertyJSON(builder, property, creditSweepstakesWinVersion);
					break;
				case CHARM_EVENT_ID:
					appendPropertyJSON(builder, property, charmEventID);
					break;
				case CURRENCY:
					appendPropertyJSON(builder, property, currency);
					break;
				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		//ServerAction.resetStaticClassData(); NEVER CALL THE BASE CLASS'S RESET!
		_propertiesLookup = null;
	}
}
