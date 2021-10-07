using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
for handling Land of Oz server actions.
*/

public class RoyalRushAction : ServerAction
{
	//Server Action Types are public so we can reg for them without passing in a callback

	// Gets info for a specific rush via event ID?
	public const string GET_RUSH_INFO = "get_royal_rush_info";

	// Submits a user's score for a rush
	public const string SUBMIT_SCORE = "end_royal_rush";

	// Starts a rush for a given game
	public const string START_SPRINT = "start_royal_rush";

	// Finishes an event so you can get a reward
	public const string COMPLETE_EVENT = "complete_royal_rush";
	
	//Unpauses the RR timer after a level_up animation
	public const string UNPAUSE_LEVEL_UP = "xp_level_up_seen";
	public const string UNPAUSE_QFC = "qfc_key_drop_seen";

	private string gameKey = "";
	private string eventKey = "";

	//property names
	private const string GAME = "game_key";
	private const string EVENT = "event";


	private RoyalRushAction(ActionPriority priority, string type) : base(priority, type)
	{
		
	}

	// Get a progress update for a specific game. Passing in a blank string gets complete info for all games.
	public static void getUpdate(string gameKey = "", EventDelegate callback = null, bool shouldPersist = false)
	{
		RoyalRushAction action = new RoyalRushAction(ActionPriority.HIGH, RoyalRushAction.GET_RUSH_INFO);
		action.gameKey = gameKey;

		if (callback != null)
		{
			Server.registerEventDelegate(GET_RUSH_INFO, callback);
		}

		ServerAction.processPendingActions(true);
	}

	// Get a progress update for all game in the land of oz
	public static void submitScore(string gameKey, EventDelegate callback = null, bool shouldPersist = false)
	{
		RoyalRushAction action = new RoyalRushAction(ActionPriority.HIGH, RoyalRushAction.SUBMIT_SCORE);
		action.gameKey = gameKey;

		if (callback != null)
		{
			Server.registerEventDelegate(SUBMIT_SCORE, callback);
		}

		ServerAction.processPendingActions(true);
	}

	// Pass a specific game to be completed
	public static void startSprint(string gameKey, EventDelegate callback = null, bool shouldPersist = false)
	{
		RoyalRushAction action = new RoyalRushAction(ActionPriority.IMMEDIATE, RoyalRushAction.START_SPRINT);
		action.gameKey = gameKey;

		if (callback != null)
		{
			Server.registerEventDelegate(START_SPRINT, callback);
		}

		ServerAction.processPendingActions(true);
	}

	// Pass a specific game to be completed
	public static void completeEvent(string eventKey, EventDelegate callback = null, bool shouldPersist = false)
	{
		RoyalRushAction action = new RoyalRushAction(ActionPriority.HIGH, RoyalRushAction.COMPLETE_EVENT);
		action.eventKey = eventKey;

		if (callback != null)
		{
			Server.registerEventDelegate(COMPLETE_EVENT, callback);
		}

		ServerAction.processPendingActions(true);
	}

	public static void unPauseLevelUpEvent(string gameKey)
	{
		RoyalRushAction action = new RoyalRushAction(ActionPriority.IMMEDIATE, RoyalRushAction.UNPAUSE_LEVEL_UP);
		action.gameKey = gameKey;
	}

	public static void unPauseQFCEvent(string gameKey)
	{
		RoyalRushAction action = new RoyalRushAction(ActionPriority.IMMEDIATE, RoyalRushAction.UNPAUSE_QFC);
		action.gameKey = gameKey;
	}

	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(GET_RUSH_INFO, new string[] { GAME });
				_propertiesLookup.Add(SUBMIT_SCORE, new string[] { GAME });
				_propertiesLookup.Add(START_SPRINT, new string[] { GAME });
				_propertiesLookup.Add(UNPAUSE_LEVEL_UP, new string[] { GAME });
				_propertiesLookup.Add(UNPAUSE_QFC, new string[] { GAME });
				_propertiesLookup.Add(COMPLETE_EVENT, new string[] { EVENT });
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
			case EVENT:
				appendPropertyJSON(builder, property, eventKey);
				break;

			case GAME:
				if (!string.IsNullOrEmpty(gameKey))
				{
					appendPropertyJSON(builder, property, gameKey);
				}
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
	}
}
