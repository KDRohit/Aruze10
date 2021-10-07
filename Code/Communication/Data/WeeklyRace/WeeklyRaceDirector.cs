using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Com.Scheduler;

/// <summary>
///   WeeklyRace management system. Handles feeding of events, creation of races, and public access to current races
/// </summary>
public class WeeklyRaceDirector : IResetGame
{
	// =============================
	// PRIVATE
	// =============================
	private List<WeeklyRace> races = new List<WeeklyRace>();
	private List<JSON> pendingCompleteEvents = new List<JSON>();
	private static SmartTimer persistentUpdateTimer = null;
	private static event EventHandler refreshStatsEvent;
	private static bool hasRequestedInfo = false;

	// =============================
	// PUBLIC
	// =============================

	public static SmartTimer rankChangeUpdateTimer = null;

	private static JSON pendingBoostData;
	// =============================
	// CONST
	// =============================
	// server events
	public const string INFO_EVENT = "weekly_race_info";
	public const string SPIN_EVENT = "weekly_race";
	public const string COMPLETE_EVENT = "weekly_race_complete";
	public const string CREDITS_EVENT = "weekly_race_chest";
	public const string REWARDS_EVENT = "weekly_race_reward";
	public const string BOOST_EVENT = "weekly_race_bonus_boost";
	public const string DAILY_RIVAL_COMPLETE = "daily_rivals_complete";

	/*=========================================================================================
	PRIVATE METHODS
	=========================================================================================*/
	private void createRace(JSON data)
	{
		WeeklyRace race = new WeeklyRace(data);
		races.Add(race);
	}

	/*=========================================================================================
	PUBLIC STATIC METHODS
	=========================================================================================*/

	public static void init(JSON data)
	{
		if (data == null ) { return; }

		for (int i = 0; i < data.getKeyList().Count; i++)
		{
			string race = data.getKeyList()[i];
			instance.createRace(data.getJSON(race));
		}

		if (instance.races.Count > 0)
		{
			handlePendingCompleteEvents();
		}

		refreshStats();

		// Check live data to see if it is you want to start the timer 
		if (Glb.START_TIMER_WEEKLY_RACE)
		{
			if (persistentUpdateTimer == null)
			{
				persistentUpdateTimer = new SmartTimer(Glb.TIMER_INTERVAL_WEEKLY_RACE, true, onTimedEvent, "weekly_race_persistent_update");
				persistentUpdateTimer.start();
			}
		}
	}

	/// <summary>
	///   This function is called on an interval to make sure we get the latest race data without overloading the server
	/// </summary>
	public static void onTimedEvent()
	{
		getUpdatedRaceData();

		if (rankChangeUpdateTimer != null)
		{
			rankChangeUpdateTimer.stop();
		}
	}

	/// <summary>
	/// Get stats updates
	/// </summary>
	/// <param name="func"></param>
	public static void registerStatHandler(EventHandler func)
	{
		//prevent duplicates by removing event first
		refreshStatsEvent -= func;
		refreshStatsEvent += func;
	}

	public static void unregisterStatHandler(EventHandler func)
	{
		refreshStatsEvent -= func;
	}

	/// <summary>
	///   Several places throughout the game may need to update their badge/division/ranking. This method
	///   is responsible for calling those refresh stats (originally for the overlay, and bottom overlay)
	/// </summary>
	public static void refreshStats()
	{
		OverlayTopHIRv2 overlay = OverlayTopHIRv2.instance as OverlayTopHIRv2;
		if (overlay!= null)
		{
			if (overlay.weeklyRaceOverlay == null)
			{
				overlay.setupWeeklyRace();
			}
			else
			{
				Scheduler.removeFunction(overlay.weeklyRaceOverlay.refresh);
				Scheduler.addFunction(overlay.weeklyRaceOverlay.refresh);
			}
		}

		EventHandler handler = refreshStatsEvent;
		if (handler != null)
		{
			handler.Invoke(instance, null);
		}
	}

	/*=========================================================================================
	EVENT HANDLING
	=========================================================================================*/
	public static void registerEvents()
	{
		Server.registerEventDelegate(INFO_EVENT, onWeeklyRaceInfo, true);
		Server.registerEventDelegate(REWARDS_EVENT, onRewardReceived, true);
		Server.registerEventDelegate(BOOST_EVENT, onBoostEvent, true);
		Server.registerEventDelegate(DAILY_RIVAL_COMPLETE, onDailyRivalsComplete, true);
		
		// Register with colectables to handle pack drops?
		Collectables.Instance.registerForPackDrop(instance.onCollectablePackDrop, "weekly_race");
	}

	public void onCollectablePackDrop(JSON data)
	{
		// Do nothing. Evidently we get the pack drop from collectables and from the reward drop for 
		// weekly race
	}
	
	public static void unregisterEvents()
	{
		// Unregister with colectables to handle pack drops?
		Server.unregisterEventDelegate(INFO_EVENT, onWeeklyRaceInfo);
	}

	/// <summary>
	///   Handles clearing out any pending completed weekly race events
	/// </summary>
	public static void handlePendingCompleteEvents()
	{
		if (instance.pendingCompleteEvents.Count > 0)
		{
			for (int i = instance.pendingCompleteEvents.Count-1; i >= 0; i--)
			{
				if (getWeeklyRaceForEvent(instance.pendingCompleteEvents[i]) != null)
				{
					onWeeklyRaceComplete(instance.pendingCompleteEvents[i]);
				}
				else
				{
					WeeklyRaceAction.completeRace(instance.pendingCompleteEvents[i].getString("event", ""));
				}
			}
		}

		instance.pendingCompleteEvents = new List<JSON>();
	}

	/// <summary>
	///   This event correlates to a "buff" which will reduce the time between collecting daily bonuses
	/// </summary>
	public static void onBoostEvent(JSON data)
	{
		pendingBoostData = data;
	}

	/// <summary>
	/// This appears when the daily rivals portion of weekly race has completed
	/// </summary>
	public static void onDailyRivalsComplete(JSON data)
	{
		long rewardAmount = data.getLong("reward_amount", 0);

		if (CustomPlayerData.getBool(CustomPlayerData.HAS_SEEN_RIVAL_LOST, false) && rewardAmount <= 0)
		{
			/* HIR-91922: Removing toaster notifications
			SocialMember rivalMember = CommonSocial.findOrCreate("", data.getString("rival_zid", ""));
			WeeklyRaceAlertDirector.showRivalWon(rivalMember, data.getString("rival_name", "Anonymous Racer"));
			*/
			string eventId = data.getString("event", "");
			if (!string.IsNullOrEmpty(eventId))
			{
				WeeklyRaceAction.onDailyRivalsComplete(eventId);
			}
		}
		else
		{
			DailyRivalsCompleteDialog.showDialog(data);
		}

		getUpdatedRaceData();
	}

	/// <summary>
	///   Public method for gathering the latest race data
	/// </summary>
	public static void getUpdatedRaceData()
	{
		if (!hasRequestedInfo)
		{
			WeeklyRaceAction.getInfo();

			hasRequestedInfo = true;
		}
	}

	public static void onWeeklyRaceComplete(JSON data)
	{
		WeeklyRace race = getWeeklyRaceForEvent(data);
		if (race != null)
		{
			WeeklyRaceAction.completeRace(data.getString("event", ""));
			race.onRaceComplete(data);
		}
		else
		{
			instance.pendingCompleteEvents.Add(data);
		}

		refreshStats();
	}

	/// <summary>
	///   Server sends an event for a generic reward, and client sends out an action to claim it
	/// </summary>
	public static void onClaimReward(JSON data)
	{
		WeeklyRace race = getWeeklyRaceForEvent(data);
		if (race != null)
		{
			race.onClaimReward(data);
		}
		else if (currentRace != null)
		{
			currentRace.onClaimReward(data);
		}
		else
		{
			WeeklyRaceAction.claimReward(data.getString("event", ""));
		}
	}

	/// <summary>
	///   Handling the reward received event, which happens after we claim a reward.
	/// </summary>
	public static void onRewardReceived(JSON data)
	{
		string cardPackKey = "";
		JSON rewardData = null;
		JSON cardPack = null;
		JSON[] grantedEvents = null;

		// this is a top level id for the chest, not sure why it's formatted this way, but ok
		int chestId = data.getInt("chest_id", 1);

		// we can now replace the data with the relevant stuff under "reward_data" field
		data = data.getJSON("reward_data");

		if (data == null)
		{
			return;
		}
		
		string featureName = data.getString("feature_name", "");
		if (featureName.Contains("weekly_race"))
		{
			long coins = 0;
			grantedEvents = data.getJsonArray("granted_events");
			if (grantedEvents.Length > 0)
			{
				rewardData = grantedEvents[0];
				coins = rewardData.getLong("added_value", 0);
				if (grantedEvents.Length == 2)
				{
					cardPack = grantedEvents[1];
				}
			}
			
			//call pending credits to prevent desync
			if (coins > 0)
			{
				Server.handlePendingCreditsCreated(WeeklyRaceRewards.REWARD_SOURCE, coins);
			}

			WeeklyRaceRewards.showDialog
			(
				Dict.create
				(
					D.AMOUNT, coins,
					D.INDEX, chestId,
					D.DATA, cardPack
				)
			);

			// show the boost dialog after if there is one
			if (pendingBoostData != null)
			{
				WeeklyRaceBoost.showDialog
				(
					Dict.create
					(
						D.TIME,
						pendingBoostData.getInt("frequency", 30),
						D.START_TIME,
						pendingBoostData.getInt("start_time", 0),
						D.END_TIME,
						pendingBoostData.getInt("end_time", 0)
					)
				);

				pendingBoostData = null;
			}
			else if (WeeklyRaceResults.package != null)
			{
				WeeklyRaceResults.package.removeTask("weekly_race_boost");
			}
		}
	}

	/// <summary>
	///   Basic update event handling
	/// </summary>
	private static void onWeeklyRaceInfo(JSON data)
	{
		hasRequestedInfo = false;

		WeeklyRace race = getWeeklyRaceForEvent(data);
		JSON weeklyRaces = data.getJSON("weekly_race");

		if (race != null)
		{
			if (race.hasRankChange)
			{
				startRefreshTimer();
			}
			race.onEventUpdated(weeklyRaces.getJSON(race.raceName));
		}
		else
		{
			instance.createRace(weeklyRaces.getJSON(weeklyRaces.getKeyList()[0]));
		}

		refreshStats();
	}

	private static void startRefreshTimer()
	{
		if (Glb.START_SECOND_TIMER_WEEKLY_RACE)
		{
			if (rankChangeUpdateTimer == null)
			{
				rankChangeUpdateTimer = new SmartTimer(Glb.TIMER_SECOND_INTERVAL_WEEKLY_RACE, true, WeeklyRaceDirector.onTimedEvent, "weekly_race_rank_change_update");
				rankChangeUpdateTimer.start();
			}
		}
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	/// <summary>
	///   Searches the event data for a weekly race key that matches any of the existing races going on
	/// </summary>
	private static WeeklyRace getWeeklyRaceForEvent(JSON data)
	{
		for (int i = 0; i < instance.races.Count; ++i)
		{
			WeeklyRace race = instance.races[i];
			JSON weeklyRaces = data.getJSON("weekly_race");

			if (weeklyRaces == null) { weeklyRaces = data; }

			// fast check for data that has the race key in it
			if (weeklyRaces.getString("race_key", "") == race.raceName)
			{
				return race;
			}

			// thorough check for the race name driven by the key for the weekly race instance
			for (int k = 0; k < weeklyRaces.getKeyList().Count; k++)
			{
				string raceKey = weeklyRaces.getKeyList()[k];
				if (raceKey == race.raceName)
				{
					return race;
				}
			}
		}
		
		return null;
	}

	/*=========================================================================================
	GETTERS
	=========================================================================================*/
	public static bool hasActiveRace
	{
		get
		{
			if (instance.races.Count > 0)
			{
				for (int i = instance.races.Count - 1; i >= 0; --i)
				{
					if (instance.races[i].isActive)
					{
						return true;
					}
				}
			}
			
			return false;
		}
	}

	/// <summary>
	///   	returns the first active race it finds, this may need to be removed it we ever want to support
	///		multiple races 
	/// </summary>
	public static WeeklyRace currentRace
	{
		get
		{
			if (instance.races.Count > 0)
			{
				for (int i = instance.races.Count - 1; i >= 0; --i)
				{
					if (instance.races[i].isActive)
					{
						return instance.races[i];
					}
				}
			}

			// last ditch effort, grab the last race that was completed
			if (instance.races.Count > 0)
			{
				return instance.races[instance.races.Count-1];
			}

			return null;
		}
	}


	/// <summary>
	/// Returns true if any race exists
	/// </summary>
	public static bool hasRace
	{
		get
		{
			return instance.races.Count > 0;
		}
	}
	
	/*=========================================================================================
	SINGLETON
	=========================================================================================*/		
	private static WeeklyRaceDirector _instance;
	private static WeeklyRaceDirector instance { get { if (_instance == null) {	_instance = new WeeklyRaceDirector(); } return _instance; } }

	public static void resetStaticClassData()
	{
		_instance = null;
		pendingBoostData = null;
		persistentUpdateTimer = null;
		rankChangeUpdateTimer = null;
	}
}
