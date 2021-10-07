/* HIR-91922: removing toasters. File will be deleted as part of this ticket HIR-92857
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Com.Scheduler;

/// <summary>
///   Handles summoning of weekly race alerts. Weekly race alerts will appear when the user has entered or exited
///   promotion/relegation zones, 1st, 2nd, and 3rd place. It will also appear when the race is nearing the time limit,
///   and when the race has ended.
/// </summary>
public class WeeklyRaceAlertDirector : IResetGame
{
	public static bool isShowingAlerts { get; private set; }

	// =============================
	// PRIVATE
	// =============================
	private static bool hasShownRaceTime = false;
	private static bool hasShownRaceEnd = false;
	private static bool hasShownRivalEnding = false;
	private static List<KeyValuePair<string, GenericDelegate>> alerts = new List<KeyValuePair<string, GenericDelegate>>();
	private static WeeklyRaceAlert currentAlert = null;

	// =============================
	// CONST
	// =============================
	private const string PREFAB_PATH = "Features/Weekly Race/Prefabs/Weekly Race Toaster";
	private const string LEADER = "leader";
	private const string LEADER_LOST = "leader_lost";
	private const string DROP_ZONE = "drop_zone";
	private const string DROP_ZONE_EXIT = "drop_zone_exit";
	private const string PROMOTION_ZONE = "promotion_zone";
	private const string PROMOTION_ZONE_EXIT = "promotion_zone_exit";
	private const string RACE_ENDING = "race_ending";
	private const string RIVAL_LEAD = "rival_lead";
	private const string PLAYER_LEAD = "player_lead";
	private const string RIVAL_ENDING = "rival_ending";
	private const string RIVAL_PAIRED = "rival_paired";
	private const string RIVAL_WON = "rival_won";

	/*=========================================================================================
	LOADING
	=========================================================================================#1#
	private void loadAlert(AssetLoadDelegate callback, Dict args = null)
	{
		AssetBundleManager.load(this, PREFAB_PATH, callback, onLoadFailed, args);
	}

	private void onLoadFailed(string path, Dict args = null)
	{
		Debug.LogError("WeeklyRaceAlertDirector: Failed to load weekly race alert");
	}

	/*=========================================================================================
	STATE HANDLING
	=========================================================================================#1#
	public static void showAlerts()
	{
		if (!isShowingAlerts)
		{
			isShowingAlerts = true;
			RoutineRunner.instance.StartCoroutine(instance.popAlertQueue());
		}
	}

	private IEnumerator popAlertQueue()
	{
		if (alerts.Count > 0)
		{
			while (Loading.isLoading || Dialog.instance.isOpening || !Glb.isNothingHappening)
			{
				yield return null;
			}

			pruneAlerts();

			// if after sorting, we still have alerts to show
			if (alerts.Count > 0)
			{
				ToasterManager.instance.toggleCamera(true);
				KeyValuePair<string, GenericDelegate> alert = alerts[0];

				int delay = alert.Key == RIVAL_PAIRED ? 2 : 0;
				WeeklyRaceAlertTask task = new WeeklyRaceAlertTask(delegate(Dict args) { alert.Value(); }, null, delay);

				Scheduler.addTask(task);
			}
		}
	}

	public void onAlertComplete()
	{
		ToasterManager.instance.toggleCamera(false);

		isShowingAlerts = false;

		if (!AssetBundleManager.isResourceInInitializationBundle(PREFAB_PATH))
		{
			string assetBundleName = AssetBundleManager.getBundleNameForResource(PREFAB_PATH);
			AssetBundleManager.unloadBundle(assetBundleName);
		}
		
		if (alerts.Count > 0)
		{
			showAlerts();
		}
	}

	/*=========================================================================================
	PRIVATE ALERT CALLS
	=========================================================================================#1#
	private void onPromotionZone(string path, Object obj, Dict args)
	{
		WeeklyRaceAlert alert = getAlertFromAsset(obj);
		alert.setupPromotionZone();
		show(alert, PROMOTION_ZONE);
	}

	private void onPromotionZoneExit(string path, Object obj, Dict args)
	{
		WeeklyRaceAlert alert = getAlertFromAsset(obj);
		alert.setupPromotionZone(true);
		show(alert, PROMOTION_ZONE_EXIT);
	}

	private void onDropZone(string path, Object obj, Dict args)
	{
		WeeklyRaceAlert alert = getAlertFromAsset(obj);
		alert.setupDropZone();
		show(alert, DROP_ZONE);
	}

	private void onDropZoneExit(string path, Object obj, Dict args)
	{
		WeeklyRaceAlert alert = getAlertFromAsset(obj);
		alert.setupDropZone(true);
		show(alert, DROP_ZONE_EXIT);
	}

	private void onLeaderAlert(string path, Object obj, Dict args)
	{
		WeeklyRaceAlert alert = getAlertFromAsset(obj);
		alert.setupLeaderAlert();
		show(alert, LEADER);
	}

	private void onLeaderDownAlert(string path, Object obj, Dict args)
	{
		WeeklyRaceAlert alert = getAlertFromAsset(obj);
		alert.setupLeaderDownAlert();
		show(alert, LEADER_LOST);
	}

	private void onRaceEnding(string path, Object obj, Dict args)
	{
		WeeklyRaceAlert alert = getAlertFromAsset(obj);
		alert.setupRaceEnding();
		show(alert, RACE_ENDING);
	}

	/*=========================================================================================
	DAILY RIVAL ALERTS
	=========================================================================================#1#
	private void onRivalLead(string path, Object obj, Dict args)
	{
		WeeklyRaceAlert alert = getAlertFromAsset(obj);
		alert.setupRivalAlert(true);
		show(alert, RIVAL_LEAD);
	}

	private void onRivalPassed(string path, Object obj, Dict args)
	{
		WeeklyRaceAlert alert = getAlertFromAsset(obj);
		alert.setupRivalAlert(false);
		show(alert, PLAYER_LEAD);
	}

	private void onRivalEnding(string path, Object obj, Dict args)
	{
		WeeklyRaceAlert alert = getAlertFromAsset(obj);
		alert.setupRivalEnding();
		show(alert, RIVAL_ENDING);
	}

	private void onRivalPaired(string path, Object obj, Dict args)
	{
		WeeklyRaceAlert alert = getAlertFromAsset(obj);
		alert.setupRivalPaired();
		show(alert, RIVAL_PAIRED);
	}

	private void onRivalWon(string path, Object obj, Dict args)
	{
		WeeklyRaceAlert alert = getAlertFromAsset(obj);
		SocialMember rival = args.getWithDefault(D.DATA, null) as SocialMember;
		string rivalName = (string)args.getWithDefault(D.PLAYER, "Anonymous Racer");
		alert.setupRivalWins(rival, rivalName);
		show(alert, RIVAL_WON);
	}

	/*=========================================================================================
	ALERT MANAGEMENT
	=========================================================================================#1#
	private void show(WeeklyRaceAlert alert, string alertToRemove)
	{
		if (currentAlert == null || !currentAlert.isShowing)
		{
			currentAlert = alert;
			alert.show(onAlertComplete);
			remove(alertToRemove);
		}
	}

	private void remove(string alertToRemove)
	{
		for (int i = 0; i < alerts.Count; ++i)
		{
			if (alerts[i].Key == alertToRemove)
			{
				alerts.RemoveAt(i);
				break;
			}
		}
	}

	private WeeklyRaceAlert getAlertFromAsset(Object obj)
	{
		GameObject go = (GameObject)CommonGameObject.instantiate(obj, Vector3.zero, Quaternion.identity);
		WeeklyRaceAlert alert = go.GetComponent<WeeklyRaceAlert>();

		go.transform.parent = ToasterManager.instance.toasterParentObject;
		go.transform.localScale = Vector3.one;
		go.transform.localPosition = new Vector3(800, 400, 0);

		return alert;
	}

	/// <summary>
	///   This method goes through some basic logic for the alerts since now they're tied to the game state. What that means
	///   is some alerts could be "dated". For example, if we have a "demotion zone" in the front of the list, and
	///   a promotion zone in the back of the list...we know that it's moot information to display "demotion zone" alerts
	/// </summary>
	private void pruneAlerts()
	{
		// 1. first remove duplicates
		// 2. remove logic based issues
		List<KeyValuePair<string, GenericDelegate>> singleList = new List<KeyValuePair<string, GenericDelegate>>();

		int i = 0;
		for (i = 0; i < alerts.Count; ++i)
		{
			if (!hasAlertByKey(singleList, alerts[i].Key))
			{
				singleList.Add(alerts[i]);
			}
			else
			{
				int index = findAlertIndex(singleList, alerts[i].Key);
				singleList.RemoveAt(index);
				singleList.Add(alerts[i]);
			}
		}

		alerts = singleList;

		// remove promotion zone alerts when the state is old
		prune(PROMOTION_ZONE, PROMOTION_ZONE_EXIT, DROP_ZONE, DROP_ZONE_EXIT);

		// remove demotion zone alerts when the state is old
		prune(DROP_ZONE, PROMOTION_ZONE, PROMOTION_ZONE_EXIT, DROP_ZONE_EXIT);

		// remove promotion zone exit when the state is old
		prune(PROMOTION_ZONE_EXIT, DROP_ZONE, DROP_ZONE_EXIT, PROMOTION_ZONE);

		// remove demotion zone exit when the state is old
		prune(DROP_ZONE_EXIT, PROMOTION_ZONE, PROMOTION_ZONE_EXIT, DROP_ZONE);

		// remove showing the lost lead alert if user has taken the lead
		prune(LEADER_LOST, LEADER);

		// remove showing the lead alert if user has lost the lead
		prune(LEADER, LEADER_LOST);
	}

	private void prune(string alertToCheck, params string[] breakingConditions)
	{
		int validIndex = -1;
		int i = 0;

		KeyValuePair<string, GenericDelegate> alert = new KeyValuePair<string, GenericDelegate>();
		for (i = 0; i < alerts.Count; i++)
		{
			alert = alerts[i];
			if (alert.Key == alertToCheck)
			{
				validIndex = i;
			}
		}

		bool shouldRemove = false;
		if (validIndex >= 0)
		{
			for (i = validIndex; i < alerts.Count; i++)
			{
				alert = alerts[i];
				if(System.Array.IndexOf(breakingConditions, alert.Key) >= 0)
				{
					shouldRemove = true;
					break;
				}
			}
		}

		if (shouldRemove)
		{
			alerts.RemoveAt(validIndex);
		}
	}

	/*=========================================================================================
	STATIC CALLS TO DISPLAY ALERTS
	=========================================================================================#1#
	private static void addAlert(string type, GenericDelegate callback)
	{
		alerts.Add(new KeyValuePair<string, GenericDelegate>(type, callback));
		instance.pruneAlerts();
		showAlerts();

	}
	public static void showPromotionZone()
	{
		addAlert(PROMOTION_ZONE, delegate { instance.loadAlert(instance.onPromotionZone); });
	}

	public static void showPromotionZoneExit()
	{
		addAlert(PROMOTION_ZONE_EXIT, delegate { instance.loadAlert(instance.onPromotionZoneExit); });
	}

	public static void showDropZone()
	{
		addAlert(DROP_ZONE, delegate { instance.loadAlert(instance.onDropZone); });
	}

	public static void showDropZoneExit()
	{
		addAlert(DROP_ZONE_EXIT, delegate { instance.loadAlert(instance.onDropZoneExit); });
	}

	public static void showLeaderAlert()
	{
		addAlert(LEADER, delegate { instance.loadAlert(instance.onLeaderAlert); });
	}

	public static void showLeaderDownAlert()
	{
		addAlert(LEADER_LOST, delegate { instance.loadAlert(instance.onLeaderDownAlert); });
	}

	public static void showRaceEnding()
	{
		WeeklyRace race = WeeklyRaceDirector.currentRace;

		if (race != null && GameState.game != null)
		{
			if (race.timeRemaining > 0 && !hasShownRaceTime)
			{
				hasShownRaceTime = true;
				addAlert(RACE_ENDING, delegate { instance.loadAlert(instance.onRaceEnding); });
			}
			else if (race.timeRemaining <= 0 && !hasShownRaceEnd)
			{
				hasShownRaceEnd = true;
				addAlert(RACE_ENDING, delegate { instance.loadAlert(instance.onRaceEnding); });
			}
		}
	}

	/*=========================================================================================
	DAILY RIVAL ALERTS
	=========================================================================================#1#
	public static void showRivalLead()
	{
		addAlert(RIVAL_LEAD, delegate { instance.loadAlert(instance.onRivalLead); });
	}

	public static void showRivalPassed()
	{
		addAlert(PLAYER_LEAD, delegate { instance.loadAlert(instance.onRivalPassed); });
	}

	public static void showRivalPairing(bool forceShow = false)
	{
		int dailyRivalSeen = CustomPlayerData.getInt(CustomPlayerData.DAILY_RIVALS_LAST_SEEN, 0);

		if (WeeklyRaceDirector.currentRace != null &&
		    WeeklyRaceDirector.currentRace.rivalTimer != null &&
		    (WeeklyRaceDirector.currentRace.rivalTimer.startTimestamp != dailyRivalSeen || dailyRivalSeen == 0 || forceShow)
		    )
		{
			CustomPlayerData.setValue(CustomPlayerData.DAILY_RIVALS_LAST_SEEN, WeeklyRaceDirector.currentRace.rivalTimer.startTimestamp);
			addAlert(RIVAL_PAIRED, delegate { instance.loadAlert(instance.onRivalPaired); });
		}
	}

	public static void showRivalWon(SocialMember rival, string name)
	{
		addAlert(RIVAL_WON, delegate
		{
			instance.loadAlert(instance.onRivalWon, Dict.create(D.DATA, rival, D.PLAYER, name));
		});
	}

	public static void showRivalEnding()
	{
		if (!hasShownRivalEnding)
		{
			addAlert(RIVAL_ENDING, delegate { instance.loadAlert(instance.onRivalEnding); });
			hasShownRivalEnding = true;
		}
	}

	/*=========================================================================================
	ALERT ISSUED ON RANK CHANGES
	=========================================================================================#1#
	public static void handleRankChange()
	{
		WeeklyRace race = WeeklyRaceDirector.currentRace;

		// rival toasters
		if (race.hasRival)
		{
			if (race.hasRankChange)
			{
				// users rank changed, and is now passed their rival
				if (race.previousRank >= race.rivalsRank && race.competitionRank < race.rivalsRank)
				{
					showRivalPassed();
				}
				// users rank changed, and is now behind their rival
				else if (race.previousRank <= race.rivalsPreviousRank && race.competitionRank > race.rivalsRank)
				{
					showRivalLead();
				}
			}
		}

		// users moving up!
		if (race.competitionRank < race.previousRank)
		{
			if (race.competitionRank <= 2)
			{
				showLeaderAlert();
			}
			else if (race.hasPromotion && !race.isRankWithinPromotion(race.previousRank))
			{
				showPromotionZone();
			}
			else if (!race.hasRelegation && race.isRankWithinRelegation(race.previousRank))
			{
				showDropZoneExit();
			}
		}
		// users going down!
		else
		{
			if (race.competitionRank <= 2)
			{
				showLeaderDownAlert();
			}
			else if (!race.hasPromotion && race.isRankWithinPromotion(race.previousRank))
			{
				showPromotionZoneExit();
			}
			else if (race.hasRelegation && !race.isRankWithinRelegation(race.previousRank))
			{
				showDropZone();
			}
		}
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================#1#
	public static string currentAlertQueue
	{
		get
		{
			string alertNames = "";
			string delimiter = "";
			for (int i = 0; i < alerts.Count; ++i)
			{
				alertNames += delimiter + alerts[i].Key;
				delimiter = ",";
			}

			return alertNames;
		}
	}

	private static bool hasAlertByKey(List<KeyValuePair<string, GenericDelegate>> list, string keyName)
	{
		for (int i = 0; i < list.Count; ++i)
		{
			if (list[i].Key == keyName)
			{
				return true;
			}
		}

		return false;
	}

	private static int findAlertIndex(List<KeyValuePair<string, GenericDelegate>> list, string keyName)
	{
		for (int i = 0; i < list.Count; ++i)
		{
			if (list[i].Key == keyName)
			{
				return i;
			}
		}

		return -1;
	}

	/*=========================================================================================
	RESET
	=========================================================================================#1#
	public static void reset()
	{
		hasShownRaceEnd = false;
		hasShownRaceTime = false;
		hasShownRivalEnding = false;
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		hasShownRaceEnd = false;
		hasShownRaceTime = false;
		hasShownRivalEnding = false;
		isShowingAlerts = false;
		alerts = new List<KeyValuePair<string, GenericDelegate>>();
		currentAlert = null;
	}

	/*=========================================================================================
	SINGLETON
	=========================================================================================#1#
	private static WeeklyRaceAlertDirector _instance;
	private static WeeklyRaceAlertDirector instance { get { if (_instance == null) {	_instance = new WeeklyRaceAlertDirector(); } return _instance; } }
}
*/
