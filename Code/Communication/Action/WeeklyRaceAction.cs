using UnityEngine;
using System.Collections.Generic;

public class WeeklyRaceAction : ServerAction
{
    // =============================
    // PRIVATE
    // =============================
	private string eventID = "";
    private string raceId = null;

    // =============================
    // CONST
    // =============================
	private const string WEEKLY_RACE_INFO = "get_weekly_race_info";
	private const string COMPLETE_WEEKLY_RACE = "complete_weekly_race";
	private const string CLAIM_REWARD = "claim_weekly_race_chest";
	private const string DAILY_RIVAL_COMPLETE = "complete_daily_rivals";

	private const string EVENT_ID = "event";
	private const string RACE_ID = "race_key";

	private WeeklyRaceAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	public static void completeRace(string eventID = "", EventDelegate callback = null)
	{
		WeeklyRaceAction action = new WeeklyRaceAction(ActionPriority.HIGH, COMPLETE_WEEKLY_RACE);
		action.eventID = eventID;
		if (callback != null)
		{
			Server.registerEventDelegate("weekly_race_complete", callback, false);
		}
		processPendingActions(true);
	}


	public static void claimReward(string eventID = "", EventDelegate callback = null)
	{
		WeeklyRaceAction action = new WeeklyRaceAction(ActionPriority.HIGH, CLAIM_REWARD);
		action.eventID = eventID;
		if (callback != null)
		{
			Server.registerEventDelegate("claim_weekly_race_chest", callback, false);
		}
		processPendingActions(true);
	}

	public static void getInfo(string raceId = "", EventDelegate callback = null)
	{
		WeeklyRaceAction action = new WeeklyRaceAction(ActionPriority.HIGH, WEEKLY_RACE_INFO);
		action.raceId = raceId;
		if (callback != null)
		{
			Server.registerEventDelegate("weekly_race_info", callback, false);
		}
		processPendingActions(true);
	}

	public static void onDailyRivalsComplete(string eventID = "", EventDelegate callback = null)
	{
		WeeklyRaceAction action = new WeeklyRaceAction(ActionPriority.HIGH, DAILY_RIVAL_COMPLETE);
		action.eventID = eventID;
		processPendingActions(true);
	}

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(WEEKLY_RACE_INFO, new string[] {});
				_propertiesLookup.Add(COMPLETE_WEEKLY_RACE, new string[] { EVENT_ID });
				_propertiesLookup.Add(CLAIM_REWARD, new string[] { EVENT_ID });
				_propertiesLookup.Add(DAILY_RIVAL_COMPLETE, new string[] { EVENT_ID });
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
				case EVENT_ID:
					appendPropertyJSON(builder, property, eventID);
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
	}
}
