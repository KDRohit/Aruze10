/**
 * @file      NotificationManager.cs
 * @authors     Nick Reynolds <nreynolds@zynga.com>
 * 				Dhashrath Raguraman <draghuraman@zynga.com>
 * <summary>
 * Notification Manager Manages Queueing of Push and Local Notification
 * </summary>
 */


using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zynga.Zdk;
using System.Runtime.InteropServices;
using Com.HitItRich.EUE;
using Com.HitItRich.Feature.VirtualPets;
using Com.Scheduler;
using Zynga.Core.Util;
using Zynga.Core.Net;
using Zynga.Core.Tasks;
using Zynga.Zdk.Services.Common;
using Zynga.Core.Platform;

using Log = Zynga.Core.ZLogger.Log<NotificationManager>;
/// Manager for Local / Push Notifications
public class NotificationManager : IDependencyInitializer, IResetGame
{
	public static NotificationManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new NotificationManager();
				_instance.init();
			}
			return _instance;
		}
	}

	/// <summary>
	/// Data structure to capture results for native PN registration call
	/// </summary>
	private class RegisterPNResult
	{
		public string ErrorMessage;
		public Dictionary<string, object> Data;
	}
	
	private static NotificationManager _instance;
	// preference for knowing if push notifications have been initialized
	public static bool RegisterPNAttempted = false;

	// Override to allow pushNotifications on dev environment
	public static bool AllowEnv3PushNotifs = false;

	private static int notifsAdded = 0;

	public static bool RegisteredForPushNotifications = false;
	private static bool asking = false;
	private static int softStep = 0;

	private const string PushNotifAllowedKey = "CanPushNotif"; 
	private const string PushNotifPromptedKey = "AskedForPushNotif";
	private const string LocalNotifAllowedKey = "CanLocalNotif";
	private const int BADGE_COUNT = 1;
	private const int BATCH_NOTIF_LIMIT = 100;

	private const string NOTIF_REACT = "notif_react";
	public const string NOTIF_HOURLY_BONUS = "notif_hourly_bonus";
	private const string NOTIF_DAILY_STREAK = "notif_daily_streak";
	private const string NOTIF_SUPER_STREAK = "notif_super_streak";
	private const string NOTIF_PET = "notif_pet";

	private RegisterPNResult _registerResult;
	
	//local test
	private Dictionary<NotificationMessage, int> testNotifsToSchedule;

	private bool isInitialized = false;
	private PreferencesBase _prefs;
	private PreferencesBase prefs
	{
		get
		{
			if (_prefs == null)
			{
				_prefs = SlotsPlayer.getPreferences();
			}
			return _prefs;
		}
	}

	#if UNITY_ANDROID && !UNITY_EDITOR
		private AndroidJavaClass pluginBadgeActivityJavaClass;
		private AndroidJavaObject currentActivity;
	#endif

	#if UNITY_IOS && !UNITY_EDITOR
	[DllImport("__Internal")]
	private static extern string UnityGetCachedUserInfo();
	#endif

	public void init() 
	{
		#if UNITY_ANDROID && !UNITY_EDITOR
		AndroidJNI.AttachCurrentThread();
		
		AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
		
		pluginBadgeActivityJavaClass = new AndroidJavaClass ("com.zynga.badgeplugin.BadgeCount");
		#endif

		// Clears out old local notifications upon app start.
		ZyngaLocalNotifications.ClearNotifications();
		ZyngaLocalNotifications.CancelAllLocalNotifications();
	}

	public static bool PushNotifsAllowed
	{
		get
		{
			return Instance.prefs.GetInt(PushNotifAllowedKey, 0) == 1 && LocalNotifsAllowed;
		}
		set
		{
			Instance.prefs.SetInt(PushNotifAllowedKey, value ? 1 : 0);
			Instance.prefs.Save();
		}
	}

	//Currently this is no longer used since we removed the toggle button, so it always defaults to true
	public static bool LocalNotifsAllowed
	{
		get
		{
			return Instance.prefs.GetInt(LocalNotifAllowedKey, 1) == 1;
		}
		set
		{
			Instance.prefs.SetInt(LocalNotifAllowedKey, value ? 1 : 0);
			Instance.prefs.Save();
		}
	}
	
	public static bool PushNotifsAnswered
	{
		get
		{
			bool answer = PushNotifsAllowed || Instance.prefs.GetInt(PushNotifPromptedKey + softStep, 0) == 1;
			return answer;
		}
	}

	public static bool InitialPrompt
	{
		get
		{
			bool answer = Instance.prefs.GetInt(PushNotifPromptedKey, 0) == 0;
			return answer;
		}
	}
	
	public static bool DevicePushNotifsEnabled
	{
		get
		{
#if UNITY_IPHONE
			return (UnityEngine.iOS.NotificationServices.enabledNotificationTypes != UnityEngine.iOS.NotificationType.None);
#elif UNITY_WSA_10_0 && NETFX_CORE //SMP this should probably be WSA specific
			return false;
#else
			// The glass is always half full in Android development.
			return true;
#endif
		}
	}

	public static bool DeviceBadgesEnabled
	{
		get
		{
#if UNITY_IPHONE
			return (UnityEngine.iOS.NotificationServices.enabledNotificationTypes & UnityEngine.iOS.NotificationType.Badge) != 0;
#elif UNITY_WSA_10_0 && NETFX_CORE //SMP this should probably be WSA specific
			return false;
#else
			// The glass is always half full in Android development.
			return true;
#endif
		}
	}

	public static bool DayZero = false;

	private List<NotificationItem> _allSetNotifications = new List<NotificationItem>();
	private List<DateTime> _allLocalNotifTimes = new List<DateTime>();

	private static bool populatedFromScat = false;

	public static string storedDeviceToken = "[REGISTRATION NOT ATTEMPTED]";

	// Implements IResetGame.
	public static void resetStaticClassData()
	{
		RegisterPNAttempted = false;
		AllowEnv3PushNotifs = false;
		notifsAdded = 0;
		RegisteredForPushNotifications = false;
		asking = false;
		softStep = 0;
		DayZero = false;
		populatedFromScat = false;
		storedDeviceToken = "[RESET]";
		NotificationInfo.clearAllInfo();
	}

	private static bool IsValidFacebookSession()
	{
		var session = ZdkManager.Instance.Zsession;
		return (session != null && StringUtil.ParseInt(session.Zid.ToStringInvariant(), 0) != 0 && session.Snid == Snid.Facebook);
	}

	public static void PushNotifSoftPromptAccepted()
	{
		Instance.prefs.SetInt(PushNotifAllowedKey, 1);
		if (PushNotifsAllowed)
		{
			if (Instance != null)
			{
				Instance.RegisterForPushNotifications();
			}
		}
	}

	public static void ShowPushNotifSoftPrompt(bool initialGameLoad = false, bool isInFtue = false)
	{
		
		//don't show if we haven't seen the first login overlay
		if (EUEManager.isEnabled && EUEManager.shouldDisplayFirstLoadOverlay)
		{
			return;
		}
		
		if (LocalNotifsAllowed)
		{
			bool isIOS = false;
#if UNITY_IPHONE || UNITY_EDITOR
			// Since android functionality is silent, assume we are on iOS for this function.
			isIOS = true;
#endif
			if (isIOS)
			{
				if (InitialPrompt || // Always show if this is the first time prompting the user,
					// Otherwise we only want to show this if we are not calling it from initial game load
					// and the trottling is not kicking in.
					(!initialGameLoad && canTriggerSoftPrompt()))
				{
					// Trigger the soft prompt!
					Instance.prefs.SetInt(PushNotifPromptedKey, 1);
					Instance.prefs.Save();

					if (ExperimentWrapper.PushNotifSoftPrompt.isInExperiment)
					{
						StatsManager.Instance.LogCount("dialog", "notifications", "soft_prompt", "", "", "view");
						if (ExperimentWrapper.PushNotifSoftPrompt.isIncentivizedPromptEnabled && IncentivizedSoftPromptDialog.creditAmount > 0)
						{
							
							IncentivizedSoftPromptDialog.showDialog(Com.Scheduler.SchedulerPriority.PriorityType.IMMEDIATE, isInFtue);	
						}
						else
						{
							SoftPromptDialog.showDialog(Com.Scheduler.SchedulerPriority.PriorityType.IMMEDIATE, isInFtue);	
						}
					}
					else
					{
						// Skip showing the soft prompt dialog and proceed to the hard prompt
						StatsManager.Instance.LogCount("dialog", "notifications", "soft_prompt", "", "", "not_shown");
						PushNotifSoftPromptAccepted();
					}
				}
				else if (!initialGameLoad && EUEManager.shouldDisplayGameIntro)
				{
					RoutineRunner.instance.StartCoroutine(showGameIntroAfterDelay(0.5f)); //wait half second for dialogs to close so we don't block (in case eue dialog is already on stack)
				}
			}
			else
			{
				if (initialGameLoad)
				{
					Instance.RegisterForPushNotifications();
				}
				else if (EUEManager.shouldDisplayGameIntro)
				{
					RoutineRunner.instance.StartCoroutine(showGameIntroAfterDelay(0.5f)); //wait half second for dialogs to close so we don't block (in case eue dialog is already on stack)
				}
			} 
		}
	}
	
	private static IEnumerator showGameIntroAfterDelay(float delay)
	{
		yield return new TIWaitForSeconds(delay);
		EUEManager.showGameIntro();
	}
	
	// Checks if the we can show the softPrompt based upon the experiment, the throttle, and the player preference
    private static bool canTriggerSoftPrompt()
	{
		//Only allow reprompt if in the PN soft prompt experiment or push notif permissions have not already been granted
		if (PushNotifsAllowed)
		{
			return false;
		}

		bool canPrompt = false;
		System.DateTime lastPromptDateTime = System.DateTime.MinValue;
		
		string dateTimeString = Instance.prefs.GetString(Prefs.SOFT_NOTIFICATION_PROMPT, null);
		if (string.IsNullOrEmpty(dateTimeString) == false)
		{
			long fileTime = 0;
			if (!long.TryParse(dateTimeString, out fileTime))
			{
				Debug.LogWarning("No soft prompt file");
			}
			lastPromptDateTime = System.DateTime.FromFileTime(fileTime);
		}

		int triggerCounter = Instance.prefs.GetInt(Prefs.SOFT_NOTIFICATION_PROMPT_TRIGGER_COUNTER, 0);
		//Start default value at 1 because we have already seen the 1st soft_prompt upon login.
		int capCounter = Instance.prefs.GetInt(Prefs.SOFT_NOTIFICATION_PROMPT_CAP_COUNTER, 1);

		float hours = (float)System.DateTime.Now.Subtract(lastPromptDateTime).TotalHours;
		bool isValidSurfacingScenario = false;
		if (ExperimentWrapper.PushNotifSoftPrompt.isInExperiment)
		{
			float seconds = (float)System.DateTime.Now.Subtract(lastPromptDateTime).TotalSeconds;	
			isValidSurfacingScenario = capCounter < ExperimentWrapper.PushNotifSoftPrompt.maxViews && // If we havent hit our max cap yet,
			                           seconds >= ExperimentWrapper.PushNotifSoftPrompt.cooldown; // And we arent within the throttle window
			//there is no frequency limit in this variant; ie it shows after every collect
		}
		else
		{
			isValidSurfacingScenario = capCounter < Glb.PUSH_NOTIF_SOFT_PROMPT_CAP && // If we havent hit our max cap yet,
			                           hours >= Glb.PUSH_NOTIF_SOFT_PROMPT_THROTTLE && // And we arent within the throttle window
			                           triggerCounter >= Glb.PUSH_NOTIF_SOFT_PROMPT_FREQUENCY; // And we have tried and failed to trigger it enough times to show again.
		}
		if (isValidSurfacingScenario)
		{			
			Instance.prefs.SetString(Prefs.SOFT_NOTIFICATION_PROMPT, System.DateTime.Now.ToFileTime().ToString());
			Instance.prefs.SetInt(Prefs.SOFT_NOTIFICATION_PROMPT_CAP_COUNTER, capCounter+1);
			Instance.prefs.SetInt(Prefs.SOFT_NOTIFICATION_PROMPT_TRIGGER_COUNTER, 0);
			Instance.prefs.Save();
			canPrompt = true;
		}
		else
		{
			//Increase this counter if we tried to trigger but failed
			Instance.prefs.SetInt(Prefs.SOFT_NOTIFICATION_PROMPT_TRIGGER_COUNTER, triggerCounter+1);
			Instance.prefs.Save();
		}

#if UNITY_EDITOR
		bool shouldLogReason = false; // Change to true to print out reasoning.
		if (!canPrompt && shouldLogReason)
		{
			System.Text.StringBuilder reason = new System.Text.StringBuilder();
			if (capCounter >= Glb.PUSH_NOTIF_SOFT_PROMPT_CAP)
			{
			    reason.AppendLine(string.Format("The cap counter {0} is greater than the cap {1}",
				    capCounter,
					Glb.PUSH_NOTIF_SOFT_PROMPT_CAP));
			}
			if (hours < Glb.PUSH_NOTIF_SOFT_PROMPT_THROTTLE)
			{
				reason.AppendLine( string.Format("The hours {0} are less than the throttle amount {1} so not showing it",
						hours, Glb.PUSH_NOTIF_SOFT_PROMPT_THROTTLE));
			}
			if (triggerCounter < Glb.PUSH_NOTIF_SOFT_PROMPT_FREQUENCY)
			{
			    reason.AppendLine(string.Format("The trigger count {0} is less than the frequency cap {1}.",
					triggerCounter, Glb.PUSH_NOTIF_SOFT_PROMPT_FREQUENCY));
			}
			Debug.Log(reason.ToString());
		}
#endif
		return canPrompt;
	}

	public static void settingsPrompt()
	{
		GenericDialog.showDialog(
			Dict.create(
				D.TITLE, Localize.text("notifications"),
				D.MESSAGE, Localize.text("enable_notifs_in_settings"),
				D.REASON, "notification-manager-settings-prompt"
			),
			SchedulerPriority.PriorityType.IMMEDIATE
		);
	}
	
	/// Populates all notification types from SCAT
	public void populateNotifications()
	{
		if (Data.login != null && populatedFromScat == false)
		{	
			foreach (JSON text in Data.login.getJsonArray("mobile_notifs.properties")) // Something Notif-y
			{
				string notifType = text.getString("notif_type", "");
				string locKey = text.getString("loc_key", "");
				string optionText = text.getString("options", ""); 
				JSON options = null;
				if (string.IsNullOrEmpty(optionText) == false)
				{
					options = new JSON(optionText);
				}

				bool disabled = false;
				if (options != null && options.isValid)
				{
					disabled = options.getBool("disabled", false);
				}

				string notifMessage;
				// Check to make sure this notification's message is valid.
				bool validMessage = NotificationInfo.getNotificationMessage(locKey, out notifMessage);
				if (!validMessage || disabled)
				{
					continue;
				}

				if (notifType.Contains(NOTIF_REACT) ||
					notifType.Contains(NOTIF_HOURLY_BONUS) ||
					notifType.Contains(NOTIF_SUPER_STREAK) || 
					notifType.Contains(NOTIF_DAILY_STREAK) ||
					notifType.Contains(NOTIF_PET))
				{
					NotificationInfo.addInfo(notifType, locKey, notifMessage, options);
				}
			}
			populatedFromScat = true;
		}
	}
	
	void Update()
	{
		if (Instance != null)
		{
			if (asking && PushNotifsAnswered)
			{
				StatsManager.Instance.LogCount("dialog", "notifications", "soft_prompt", PushNotifsAllowed ? "ok" : "not_now", "", "click");
				asking = false;
			}

#if UNITY_IPHONE
			if (UnityEngine.iOS.NotificationServices.localNotificationCount > 0)
			{
				Debug.LogFormat("PN> localNotificationCount: {0}", UnityEngine.iOS.NotificationServices.localNotificationCount);

				foreach(UnityEngine.iOS.LocalNotification local in UnityEngine.iOS.NotificationServices.localNotifications)
				{
					// Track Notifications Received
					Debug.LogFormat("PN> Received Local Notification: {0}", local.alertBody);
					
					if (local.userInfo != null)
					{						
						string subtype = "";
						if (local.userInfo.Contains("subtype"))
						{
							subtype = (string)local.userInfo["subtype"];
						}
						
						string uniqueId = "";
						if (local.userInfo.Contains("uniqueId"))
						{
							uniqueId = (string)local.userInfo["uniqueId"];
						}
						string day = null;
						if (local.userInfo.Contains("day"))
						{
							day = local.userInfo["day"] + "d";
						}
					}
				}
			}
#endif
		}
	}

	/// Logs where the player was when the resumed the application
	private static void logWhereResumed()
	{
		// Check to see if the player is in a game when they resume
		if (SlotBaseGame.instance != null)
		{
			StatsManager.Instance.LogCount("start_session", "game_resume", "game", StatsManager.getGameName(), "", "", 1);
		}
		// Check to see if the player is in the load screen
		else if (Loading.isLoading)
		{
			// downloading a game when they resume
			if (Loading.instance.isDownloading)
			{
				StatsManager.Instance.LogCount("start_session", "game_resume", "game_download", StatsManager.getGameName(), "", "", 1);
			}
			// loading a game when they resume
			else
			{
				StatsManager.Instance.LogCount("start_session", "game_resume", "game_load", StatsManager.getGameName(), "", "", 1);
			}
		}
		// Check to see if the player was in the lobby when they resumed
		else if (MainLobby.instance != null)
		{
			StatsManager.Instance.LogCount("start_session", "game_resume", "lobby", MainLobby.instance.getTrackedScrollPosition().ToString(), "", "", 1);
		}
		else if (VIPLobby.instance != null)
		{
			StatsManager.Instance.LogCount("start_session", "game_resume", "vip_room", VIPLobby.instance.getTrackedScrollPosition().ToString(), "", "", 1);
		}
	}

	private void clearAllNotifications()
	{
		notifsAdded = 0;
		_allSetNotifications.Clear();
		_allLocalNotifTimes.Clear();
#if UNITY_IPHONE
		ZyngaLocalNotifications.CancelAllLocalNotifications();
		UnityEngine.iOS.NotificationServices.ClearLocalNotifications();
#elif UNITY_WSA_10_0 && NETFX_CORE //SMP this should probably be WSA specific
		//Do WSA stuff here
#elif UNITY_ANDROID
		//TODO: Girish
		ZyngaLocalNotifications.ClearNotifications();
		ZyngaLocalNotifications.CancelAllLocalNotifications();
#endif
	}
	
	public void pauseHandler(bool pause)
	{
		// Don't do anything until the Manager actually initializes after first auth
		if (!isInitialized) return;

		DateTime now = DateTime.UtcNow;
		string dateTimeString = " Bad Date Time!";

		dateTimeString = now.ToString();

		if (pause)
		{
			Bugsnag.LeaveBreadcrumb(string.Format("GAME PAUSED -- {0}", dateTimeString));
			
			clearAllNotifications();

			setBadgeNumber();
			if (LocalNotifsAllowed)
			{
				populateNotifications();
				addPriorityNotifications();
				addReactivationNotifications();
				addPetNotifications();
				
				//Should only be able to queue these from the dev panel, but going to still gate these to never be added on Prod builds
#if !ZYNGA_PRODUCTION 
				addTestLocalNotifications();
#endif
			}
		}
		else
		{
			if (WatchToEarn.watchToEarnClick) {
				// TODO: Add watch to earn logging
				WatchToEarn.watchToEarnClick = false;
			} else {
				logWhereResumed ();
			}

			Bugsnag.LeaveBreadcrumb(string.Format("GAME PAUSED -- {0}", dateTimeString));
#if UNITY_IPHONE
			ZyngaLocalNotifications.CancelAllLocalNotifications();
#elif UNITY_ANDROID
			ZyngaLocalNotifications.CancelAllLocalNotifications();
#elif UNITY_WSA_10_0 && NETFX_CORE //SMP this should probably be WSA specific
			//Do WSA stuff here
#endif
			processUnpauseNotifications();
		}
	}

	/// If App was reopened before notifications were sent.
	/// Happens all the time when the app loses focus at login
	/// purchase, etc
	private void processUnpauseNotifications()
	{
		for (int i = 0; i < _allSetNotifications.Count; i++)
		{
			if (_allSetNotifications[i] != null)
			{
				trackLocalNotification(_allSetNotifications[i], true);
			}
		}
		
		_allSetNotifications.Clear();
		_allLocalNotifTimes.Clear();
		clearAllNotifications();
	}
	
	// Most important notifs
	public void addPriorityNotifications()
	{
		if (Data.debugMode)
		{
			Debug.LogFormat("PN> ExperimentWrapper.LocalNotificationHourlyBonus.isInExperiment true");
		}

		//DailyBonus Bonus
		if (SlotsPlayer.instance != null && SlotsPlayer.instance.dailyBonusTimer != null)
		{
			DailyBonusGameTimer dailyBonusGameTimer = SlotsPlayer.instance.dailyBonusTimer;
			if (dailyBonusGameTimer == null)
			{
				return;
			}
			string messageKey = NOTIF_HOURLY_BONUS;
			NotificationMessage nMessage;
			nMessage = NotificationInfo.getRandomMessage(messageKey);
			for (int i = 0; i < _allSetNotifications.Count; i++)
			{
				NotificationItem notif = _allSetNotifications[i];
				if (notif.userDataDict["subtype"] == messageKey)
				{
					Debug.LogWarning("Tried to double add a hourly bonus PN. Skipping");
					return;
				}
			}
			setNotification(nMessage, dailyBonusGameTimer.timeRemaining, "");

			//Set the Daily Streak Ending Notification
			int day = dailyBonusGameTimer.day;
			string notifType = NOTIF_DAILY_STREAK;
			long triggerTimeOffset = ExperimentWrapper.NewDailyBonus.dailyStreakEndingReminder;
			long triggerTime = dailyBonusGameTimer.resetProgressionTimerTimeRemaining - triggerTimeOffset;
			string locKey = "";
			string locKeyTitle = "";
			
			if (triggerTimeOffset >= 0 && triggerTime > 0)
			{
				NotificationMessage notifMessage;
				if (day >= 1 && day < 7)
				{
					locKey = ExperimentWrapper.NewDailyBonus.notifLocKeyDay1To6;
				}
				else if (day >= 7)
				{
					locKey = ExperimentWrapper.NewDailyBonus.notifLocKeyDay7;
				}

				if (string.IsNullOrEmpty(locKey))
				{
					return;
				}

				locKeyTitle = locKey + "_title";
				string title = Localize.text(locKeyTitle);
				notifMessage = NotificationInfo.getMessageOfTypeWithKey(notifType, locKey);
				setNotification(notifMessage, triggerTime, title);
			}
		}
	}
	
	// Mixes up reactivation notifications based on some rules outlined in spec
	// and included in the options JSON. Currently only affect minimum time until can be set 
	// and whether the message is disabled or not
	public void addReactivationNotifications()
	{
		string messageKey = NOTIF_REACT;
		
		List<NotificationMessage> valid_reacts = NotificationInfo.getAllMessagesOfType(messageKey);
		if (valid_reacts.Count == 0)
		{
			Debug.LogWarning("PN> No valid reactivation notifications found.");
			return;
		}
		var reacts = new List<NotificationMessage>();
		foreach (var nm in valid_reacts)
		{
			NotificationMessage message = nm; // copy
			reacts.Add(message);
		}

		int[] days = {0,1,2,7,14,30,60};
		foreach (int day in days)
		{
			if (day == 0 && !DayZero)
			{
				continue;
			}

			// Find a random reaction that works.
			for (int i=0; i < reacts.Count; i++)
			{
				NotificationMessage react = reacts[UnityEngine.Random.Range(0, reacts.Count)];
				int minDay = 0;
				if (react.options != null)
				{
					minDay = react.options.getInt("min", 0);
				}
				if (day >= minDay)
				{
					// Use this one.
					reacts.Remove(react);
					long triggerTime = Common.SECONDS_PER_DAY * day;
					
					// Day Zero Notif
					if (day == 0)
					{
						if (DayZero)
						{
							triggerTime = Common.SECONDS_PER_HOUR * 2;
							DayZero = false;
						}
					}
					setNotification(react, triggerTime, "");
					break;
				}
			}
		}
	}

	private void addPetNotifications()
	{
		if (VirtualPetsFeature.instance != null && VirtualPetsFeature.instance.isEnabled)
		{
			if (VirtualPetsFeature.instance.lowEnergyTimer != null && VirtualPetsFeature.instance.lowEnergyTimer.timeRemaining > 0)
			{
				NotificationMessage lowEnergyMessage = NotificationInfo.getRandomMessage(VirtualPetsFeature.LOW_ENERGY_NOTIF_KEY);
				setNotification(lowEnergyMessage, VirtualPetsFeature.instance.lowEnergyTimer.timeRemaining, "");
			}
			
			if (VirtualPetsFeature.instance.midEnergyTimer != null && VirtualPetsFeature.instance.midEnergyTimer.timeRemaining > 0)
			{
				NotificationMessage midEnergyMessage = NotificationInfo.getRandomMessage(VirtualPetsFeature.MID_ENERGY_NOTIF_KEY);
				setNotification(midEnergyMessage, VirtualPetsFeature.instance.midEnergyTimer.timeRemaining, "");
			}
			
			//Only schedule the daily bonus fetch message if we've reached hyper for the day & haven't hit our limit yet
			DailyBonusGameTimer dailyBonusGameTimer = SlotsPlayer.instance.dailyBonusTimer;
			if (dailyBonusGameTimer != null &&
			    VirtualPetsFeature.instance.timerCollectsUsed < VirtualPetsFeature.instance.hyperMaxTimerCollects &&
			    VirtualPetsFeature.instance.hyperReached)
			{
				NotificationMessage fetchMessage = NotificationInfo.getRandomMessage(VirtualPetsFeature.DB_FETCH_NOTIF_KEY);
				setNotification(fetchMessage, dailyBonusGameTimer.timeRemaining, "");
			}
		}
	}
	
	// Adds a notification to the list of scheduled notifications
	// No 2 notifications within 2 hours of eachother
	// Priority given to non-reactivation
	private long scheduleTime(long seconds)
	{
		if (seconds < 0)
		{
			Debug.LogWarningFormat("NotificationManager.cs -- scheduleTime -- attempting to schedule a notification with negative seconds.");
			return seconds;
		}
		
		DateTime scheduledTime = DateTime.Now.Add(new TimeSpan(0,0,(int)seconds));

		if (scheduledTime.Hour < 7) 
		{
			seconds += (7 - scheduledTime.Hour) * Common.SECONDS_PER_HOUR;
		}
		else if (scheduledTime.Hour >= 23)
		{
			seconds += ((24-scheduledTime.Hour) + 7) * Common.SECONDS_PER_HOUR;
		}
	
		_allLocalNotifTimes.ForEach((time) => 
		{
			TimeSpan span = time.Subtract(scheduledTime);
			if (Math.Abs(span.TotalSeconds) < Common.SECONDS_PER_HOUR * 2)
			{
				seconds = -1;
			}
		});
		
		if (seconds != -1)
		{
			_allLocalNotifTimes.Add(DateTime.Now.Add(new TimeSpan(0,0,(int)seconds)));
		}
		
		return seconds;
	}
	
	// Actually adds the notification on the device
	private bool setNotification(NotificationMessage notifMess, long triggerTimeSeconds, string title)
	{
		Debug.LogFormat("PN> add Local Notification( {0}, {1})", notifMess.key, triggerTimeSeconds);

		if (string.IsNullOrEmpty(notifMess.notifType))
		{
			Debug.LogFormat("NotificationManager.cs -- setNotification -- notifType is null or empty, aborting");
			return false;
		}

		//ScheduleTime checks that multiple notifications aren't set within 2 hours of each other
		// or that they aren't during quiet hours.
		// Returns -1 if notification should be cancelled or pushes it back to a reasonable time
		long originalTriggerTimeSeconds = triggerTimeSeconds;

		
		if (Data.debugMode && (triggerTimeSeconds != originalTriggerTimeSeconds))
		{
			Debug.LogFormat("PN> Rescheduled queued Local Notification {0}:{1}:{2} from [{3}] to [{4}]",
				notifMess.notifType, notifMess.key, notifMess.message,
				originalTriggerTimeSeconds, triggerTimeSeconds);
		}
		if (triggerTimeSeconds < 0)
		{
			Debug.LogFormat("NotificationManager.cs -- setNotification -- triggerTimeSeconds after scheduling is <0, aborting.");
			return false;
		}

		NotificationItem notif = new NotificationItem(notifMess.notifType, notifMess.key, triggerTimeSeconds, notifMess.message, title);
		
		if (!checkIfNotifAlreadyScheduled(notif)) 
		{
			//Preventing Malformed Notif content by preprocessing for weird stuff
			notif.message = notif.message.Replace ("\r\n", string.Empty);
			notif.message = notif.message.Replace ("\n", string.Empty);
			notif.message = System.Text.RegularExpressions.Regex.Replace (notif.message, @"\s+", " ");
			notif.Queue (++notifsAdded);
			_allSetNotifications.Add (notif);
			trackLocalNotification (notif);
#if UNITY_EDITOR
			Debug.LogFormat ("PN> Queued Local Notification {0} : {1} [{2}s]", notifsAdded, notif.message,
				notif.originalTriggerTimeSeconds);
#endif
		}
		return true;
	}


	//Checks to see if the notif is already scheduled at a particular time
	// true if it is
	// false if it is not
	private bool checkIfNotifAlreadyScheduled(NotificationItem notif)
	{
	
		for (int i = 0; i < _allSetNotifications.Count; i++)
		{
			if (_allSetNotifications [i].message == notif.message) 
			{
				return true;
			}
		}
		return false;
	}

	public void setBadgeNumber() 
	{
#if UNITY_IPHONE

		// MCC -- Experiment cleanup, this is being set to a const of 1 and removed from experimentation.int bad
		//HIR-84661 We'd like it to simply have a (1) rather than reflecting the inbox count.
		notifsAdded = BADGE_COUNT;
		UnityEngine.iOS.LocalNotification notif = new UnityEngine.iOS.LocalNotification();
		notif.fireDate = DateTime.Now.AddSeconds(Common.SECONDS_PER_HOUR);
		notif.applicationIconBadgeNumber = BADGE_COUNT;
		UnityEngine.iOS.NotificationServices.ScheduleLocalNotification(notif);
		
#elif UNITY_ANDROID
		// MCC -- Experiment cleanup, this is being set to a const of 1 and removed from experimentation.
		//HIR-84661 We'd like it to simply have a (1) rather than reflecting the inbox count.
		notifsAdded = BADGE_COUNT;
		setAndroidBadgeCount(BADGE_COUNT);
		
#elif UNITY_WSA_10_0 && NETFX_CORE //SMP this should probably be WSA specific
		//Do WSA stuff here
#endif
	}

	//Sets the badge count of android devices (only works for Samsung devices)
	private void setAndroidBadgeCount(int count)
	{
	    // HIR-50347: For some reason we are recently starting to get crashes on Samsung devices (the only ones that
	    // this call has any effect on) from making this call.  Crash manifests as "AndroidJavaException:
	    // java.lang.SecurityException: Permission Denial: writing com.sec.android.provider.badge.BadgeProvider,
	    // requires com.sec.android.provider.badge.permission.WRITE".  Our AndroidManifest.xml has had
	    // com.sec.android.provider.badge.permission.WRITE permission declared for ages.  For now we have no way of
	    // debugging this so we will just disable badging on Android temporarily for release (MAR.1.2017 release).
	    return;
#if UNITY_ANDROID && !UNITY_EDITOR
		if (count > 0) {
			pluginBadgeActivityJavaClass.CallStatic ("setAndroidBadge", currentActivity, count);
		} else 
		{
			pluginBadgeActivityJavaClass.CallStatic ("clearAndroidBadge", currentActivity);
			pluginBadgeActivityJavaClass.CallStatic ("setAndroidBadge", currentActivity, 1);
		}
#endif
	}
	
	// ZTrack
	private void trackLocalNotification(NotificationItem item, bool unPause = false)
	{
		string status = "scheduled";
			
		if (unPause)
		{
			status = (item.triggerTimeSeconds > CurrentUnixSeconds()) ? "canceled" : "sent";
		}

		AnalyticsManager.Instance.LogLocalNotificationScheduled(status, item);
	}
	
//------------ Push Notifications
//------------------------------

	public static void DeRegisterFromPN()
	{
		ZyngaPushNotification.RemoveDeviceContactForZid();
	}

	private void CheckConnectionAndRegisterPN()
	{
		Debug.LogFormat("PN> ====In check connection and register PN");
		//Packages.Net.NetStatus.RunWhenOnline (SendRequest, Packages.Net.Settings.DefaultTimeout);

		PackageProvider.Instance.Net.NetStatus.WaitUntilOnline().Callback(task =>
		{
			connectionWatcherRunning = false;
			if (PackageProvider.Instance.Net.NetStatus.IsOnline)
			{
				ProcessPushNotificationRegistration();	
			}
			else
			{
				RoutineRunner.instance.StartCoroutine(ConnectionWatcher(3));
			}
		});
	}

	bool connectionWatcherRunning = false;
	
	IEnumerator ConnectionWatcher(int delaySeconds = 0)
	{
		if (connectionWatcherRunning)
			yield break;
		
		connectionWatcherRunning = true;
		
		if (delaySeconds > 0)
			yield return new WaitForSeconds(delaySeconds);
		
		while (ZdkManager.Instance.Zsession == null)
			yield return new WaitForSeconds(3);
		
		CheckConnectionAndRegisterPN();
	}
	
	public void RegisterForPushNotifications()
	{
		Debug.LogFormat("PN> ====IN register for push notifications");
		if (Glb.serverLogPushNotifications)
		{
			Server.sendLogInfo("PN", "IN register for push notifications");
		}
		if (!RegisteredForPushNotifications && !RegisterPNAttempted)
		{
			Debug.LogFormat("PN> ====IN register for push notifications before starting connection watcher");
			if (Glb.serverLogPushNotifications)
			{
				Server.sendLogInfo("PN", "IN register for push notifications before starting connection watcher");
			}
			RoutineRunner.instance.StartCoroutine(ConnectionWatcher());
		}
	}
	
	private void ProcessPushNotificationRegistration()
	{
		// TODO: Actually setup some push notifications (none have been specified ??)
		// 1) Use ZyngaPushNotifications.SubscribeToPushNotification to indicate that the client is listening for the given notifications
		// Open question -- is the above method required for all notifs or are they enabled by default
		// 2) Use ZyngaPushNotifications.HandlePushNotificationWithEventId to listen to when user receives (not clicks) the given push notification (iOS only according to Ron Vergis)

		if (!RegisterPNAttempted)
		{
			RegisterPNAttempted = true;
		}
			
		#if UNITY_IPHONE
		//string registrationError = UnityEngine.iOS.NotificationServices.registrationError;
		//bool notificationsEnabled = (registrationError == null || registrationError == "");
		#endif

		bool isKindle = false;
#if ZYNGA_KINDLE
		isKindle = true;
#endif
		if (Data.debugMode)
		{
			Debug.LogFormat("PN> Attempting to register of for Push Notifications");
			if (Glb.serverLogPushNotifications)
			{
				Server.sendLogInfo("PN", "Attempting to register of for Push Notifications");
			}
		}

		ZyngaPushNotification.RegisterForRemoteNotifications(ZyngaConstants.PushNotifSenderId, (data, errorMessage) =>
		{
			RegisterPNResult result = new RegisterPNResult();
			result.ErrorMessage = errorMessage;
			result.Data = data;
			_registerResult = result;
		}, isKindle);
		RoutineRunner.instance.StartCoroutine(onRegisterForRemoteNotifications(0.5f));
	}

	private IEnumerator onRegisterForRemoteNotifications(float delay = 0f)
	{
#if UNITY_WEBGL
		// there is no support for this with WebGL, so we break out now so we don't spam bugsnag with the "Could not find PN registration results" error HIR-75511
		yield break;
#endif


		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}

		int attempts = 10;

		while (_registerResult == null && attempts > 0)
		{
			attempts--;
			yield return new WaitForSeconds(1);
		}

		if (_registerResult == null)
		{
			Debug.LogError("Could not find PN registration results");
			yield break;
        }

		if (_registerResult.ErrorMessage != null)
		{
			Debug.LogFormat("PN> Failure while registering for PN: {0}", _registerResult.ErrorMessage);

			if (Glb.serverLogPushNotifications)
			{
				Server.sendLogInfo("PN", string.Format("Failure while registering for PN: {0}", _registerResult.ErrorMessage));
			}
		}
		checkForExtraData();

		RegisteredForPushNotifications = true;

		if (Data.debugMode)
		{
			string messagingString = "null";

			if (_registerResult.Data != null)
			{
				messagingString = JSON.createJsonString("", _registerResult.Data);
			}

			Debug.LogFormat("PN> Registered for PN successfully: {0}", messagingString);
			if (Glb.serverLogPushNotifications)
			{
				Server.sendLogInfo("PN", string.Format("Registered for PN successfully: {0}", messagingString));
			}
		}
		_registerResult = null;
	}

	public static void PushNotification(List<SocialMember> recipients, NotificationEvents notEvent, string message)
	{
		if (recipients != null)
		{
			foreach (SocialMember recip in recipients)
			{
				Debug.LogFormat("PN> PushNotification {0} {1} {2}", recip.zId, notEvent, message);

				NotificationManager.PushNotification(recip, notEvent, message);
			}
		}
	}
	
	public static void PushNotification(SocialMember recipient, NotificationEvents notEvent, string message)
	{
		if (recipient != null)
		{
			Debug.LogFormat("PN> PushNotification Snid.Facebook {0}", Snid.Facebook);

			NotificationManager.PushNotification(Snid.Facebook, recipient.zId, notEvent, message);
		}
	}
	
	public static void PushNotification(Snid destsnid, string destzid, NotificationEvents notEvent, string message)
	{
		if (Instance != null)
		{
			if (!RegisteredForPushNotifications)
			{
				if (Data.debugMode)
				{
					Debug.LogFormat("PN> Can't send Push Notification {0} - {1}", notEvent, message);
				}
				return;
			}

			int eventType = (int)notEvent;
			Debug.LogFormat("PN> Sending Push Notification {0} {1} {2} {3}", destsnid, destzid, eventType, message);
			Instance.PushNotification(destsnid, destzid, eventType, message);
		}
	}

	public void PushNotification(Snid destsnid, string destzid, int eventType, string message)
	{	
		Debug.LogFormat("PN> PushNotification {0} {1} {2} {3}", destsnid, destzid, eventType, message);

		//TODO: Girish
		ZyngaPushNotification.PushNotification pn = new ZyngaPushNotification.PushNotification();
		pn.sandbox = Data.IsSandbox && !AllowEnv3PushNotifs;
			//pn.message = message;
			Dictionary <string, object> body = new Dictionary<string, object> ();
			Dictionary <string, string> param = new Dictionary<string, string> ();
			param.Add ("contents", message);
			param.Add ("subject", "");
			body.Add ("locale", "en_US");
			body.Add ("template_id", "-1");
			body.Add ("params", param);
			pn.message = body;
			Dictionary<string,string> extraVars = new Dictionary<string, string>();
			extraVars.Add("zid", destzid);
			extraVars.Add("snID", destsnid.ToString());

			//DT_TODO: with the next upcoming version of ZDKUnityBeta this will be deprecated in favor up adding a member pn.title
			extraVars.Add("title", Localize.getGameName());

			pn.appData = extraVars;

			long zid = 0;
			Int64.TryParse (destzid, out zid);

			// Fire the Notification
			ZyngaPushNotification.SendPushNotificationToZid( zid, pn);
	}

	public static void SocialPushNotification(List<SocialMember> recipients, NotificationEvents notEvent)
	{
		if (recipients == null || Instance == null)
		{
			if (Data.debugMode)
			{
				Debug.LogFormat("PN> No recipients, or instance");
			}

			return;
		}

		if (!RegisteredForPushNotifications)
		{
			if (Data.debugMode)
			{
				Debug.LogFormat("PN> Can't send Push Notification {0}", notEvent);
			}

			return;
		}

		List<string> allZids = new List<string>();
		int index = 0;
		foreach (SocialMember recip in recipients)
		{
			//leave breadcrumb and sanitize input
			Bugsnag.LeaveBreadcrumb("NotificationManager.SocialPushNotification() - PN> Sending Push Notification id: " + recip.zId + "; event: " + notEvent);
			if (!string.IsNullOrEmpty(recip.zId) && "-1" != recip.zId)
			{
				allZids.Add(recip.zId);	
			}

			++index;
			if (index >= BATCH_NOTIF_LIMIT)
			{
				index = 0;
				Instance.SendBatchSocialPushNotifications(allZids, notEvent);
				allZids = new List<string>();
			}
		}

		if (index > 0) //edge case check so we don't duplicate send when are reciepient count is a multiple of BATCH_NOTIF_LIMIT
		{
			Instance.SendBatchSocialPushNotifications(allZids, notEvent);
		}
	}


	public ZyngaPushNotification.PushNotification getPushNotifData(string destzid, NotificationEvents notEvent, out string template_id, out string template_name)
	{
		// Payload
		Dictionary <string, object> body = new Dictionary<string, object>();
		body.Add("locale", "en_US");

		// Message params, depends on the event
		Dictionary <string, string> param = new Dictionary<string, string>();
		param.Add("SENDER_NAME", SlotsPlayer.instance.socialMember.fullName);

		template_id = "-1";
		switch (notEvent)
		{
			case NotificationEvents.SendCoins:
				template_id = Data.liveData.getString("SOCIAL_NOTIF_TEMPLATE_SEND_COINS", "-1");
				template_name = "SOCIAL_NOTIF_TEMPLATE_SEND_COINS";
				break;
			case NotificationEvents.SendSpins:
				template_id = Data.liveData.getString("SOCIAL_NOTIF_TEMPLATE_SEND_SPINS", "-1");
				template_name = "SOCIAL_NOTIF_TEMPLATE_SEND_SPINS";
				break;
			case NotificationEvents.Jackpot:
				template_id = Data.liveData.getString("SOCIAL_NOTIF_TEMPLATE_JACKPOT", "-1");
				template_name = "SOCIAL_NOTIF_TEMPLATE_JACKPOT";
				break;
			case NotificationEvents.StartPlaying:
				template_id = Data.liveData.getString("SOCIAL_NOTIF_TEMPLATE_STARTPLAYING", "-1");
				template_name = "SOCIAL_NOTIF_TEMPLATE_STARTPLAYING";
				break;
			case NotificationEvents.RequestCoins:
				template_id = Data.liveData.getString("SOCIAL_NOTIF_TEMPLATE_REQUEST_COINS", "-1");
				template_name = "SOCIAL_NOTIF_TEMPLATE_REQUEST_COINS";
				break;
			case NotificationEvents.RequestCoinsGranted:
				template_id = Data.liveData.getString("SOCIAL_NOTIF_TEMPLATE_REQUEST_COINS_GRANTED", "-1");
				template_name = "SOCIAL_NOTIF_TEMPLATE_REQUEST_COINS_GRANTED";
				break;
			//John Bess : PPU stuff
			case NotificationEvents.PartnerPowerupNudge:
				template_id = Data.liveData.getString("SOCIAL_NOTIF_TEMPLATE_PPU_PLAYER_TO_PLAYER", "-1");
				template_name = "SOCIAL_NOTIF_TEMPLATE_PPU_PLAYER_TO_PLAYER";
				param.Add("user_name", SlotsPlayer.instance.socialMember.firstName);
				param.Add("buddy_name", CampaignDirector.partner.buddyFirstName);
				break;
			case NotificationEvents.PartnerPowerupFBNudge:
				template_id = Data.liveData.getString("SOCIAL_NOTIF_TEMPLATE_PPU_PLAYER_TO_FACEBOOK", "-1");
				template_name = "SOCIAL_NOTIF_TEMPLATE_PPU_PLAYER_TO_FACEBOOK";
				param.Add("user_name", SlotsPlayer.instance.socialMember.firstName);
				param.Add("buddy_name", CampaignDirector.partner.buddyFirstName);
				break;
			case NotificationEvents.PartnerPowerupPlayerComplete:
				template_id = Data.liveData.getString("SOCIAL_NOTIF_TEMPLATE_PPU_PLAYER_TO_PLAYER_COMPLETE", "-1");
				template_name = "SOCIAL_NOTIF_TEMPLATE_PPU_PLAYER_TO_PLAYER_COMPLETE";
				param.Add("user_name", SlotsPlayer.instance.socialMember.firstName);
				param.Add("buddy_name", CampaignDirector.partner.buddyFirstName);
				break;
			case NotificationEvents.PartnerPowerupFBComplete:
				template_id = Data.liveData.getString("SOCIAL_NOTIF_TEMPLATE_PPU_PLAYER_TO_FACEBOOK_COMPLETE", "-1");
				template_name = "SOCIAL_NOTIF_TEMPLATE_PPU_PLAYER_TO_FACEBOOK_COMPLETE";
				param.Add("user_name", SlotsPlayer.instance.socialMember.firstName);
				param.Add("buddy_name", CampaignDirector.partner.buddyFirstName);
				break;
			case NotificationEvents.PartnerPowerupPaired:
				template_id = Data.liveData.getString ("SOCIAL_NOTIF_TEMPLATE_PPU_PAIRED", "-1");
				template_name = "SOCIAL_NOTIF_TEMPLATE_PPU_PAIRED";
				param.Add ("user_name", SlotsPlayer.instance.socialMember.firstName);
				param.Add ("buddy_name", CampaignDirector.partner.buddyFirstName);
				break;
			case NotificationEvents.PartnerPowerupFBPaired:
				template_id = Data.liveData.getString ("SOCIAL_NOTIF_TEMPLATE_PPU_PAIRED_FACEBOOK", "-1");
				template_name = "SOCIAL_NOTIF_TEMPLATE_PPU_PAIRED_FACEBOOK";
				param.Add ("user_name", SlotsPlayer.instance.socialMember.firstName);
				param.Add ("buddy_name", CampaignDirector.partner.buddyFirstName);
				break;
			default:
				template_id =  "-1";
				template_name = "";
				if(Data.debugMode)
				{
					param.Add ("contents", string.Format("ERROR: Missing template for Social Notif {0}", 
					Enum.GetName( typeof(NotificationEvents), notEvent)));
				}
				else
				{
					param.Add ("contents", Localize.getGameName());
				}
				param.Add ("subject", "");
				break;
		}

		body.Add("template_id", template_id.Trim());
		body.Add("params", param);

		// Extra vars - Usually used for tracking and stuff
		Dictionary<string,string> extraVars = new Dictionary<string, string>();
		extraVars.Add("zid", destzid);
		extraVars.Add("snID", Snid.Facebook.ToString());
		extraVars.Add("title", Localize.getGameName());
		extraVars.Add("NotificationEvent", Enum.GetName( typeof(NotificationEvents), notEvent));

		ZyngaPushNotification.PushNotification pn = new ZyngaPushNotification.PushNotification();
		pn.sandbox = Data.IsSandbox && !AllowEnv3PushNotifs;
		pn.message = body;
		pn.appData = extraVars;

		return pn;
	}

	public void SendSocialPushNotification(string destzid, NotificationEvents notEvent)
	{

		string template_id = "";
		string template_name = "";
		ZyngaPushNotification.PushNotification pn = getPushNotifData(destzid, notEvent,out template_id, out template_name);

		long zid = 0;
		Int64.TryParse(destzid, out zid);

		AnalyticsManager.Instance.LogPushNotificationSent(template_id, template_name);

		// Fire the Notification
		ZyngaPushNotification.SendPushNotificationToZid(zid, pn);
	}
	
	public void SendBatchSocialPushNotifications(List<string> destzids, NotificationEvents notEvent)
	{
		if (destzids == null || destzids.Count == 0)
		{
			return;
		}
		
		if (destzids.Count > BATCH_NOTIF_LIMIT)
		{
			Debug.LogWarning("Batch size > 100, only sending the first 100");
			destzids = destzids.GetRange(0, BATCH_NOTIF_LIMIT);
		}
		
		Dictionary<string, ZyngaPushNotification.PushNotification> allNotifs = new Dictionary<string, ZyngaPushNotification.PushNotification>();
		foreach (string zid in destzids)
		{
			string template_id = "";
			string template_name = "";
			allNotifs[zid] = getPushNotifData(zid, notEvent, out template_id, out template_name);
			AnalyticsManager.Instance.LogPushNotificationSent(template_id, template_name);
		}
		
		// Fire the Notifications
		ZyngaPushNotification.BatchSendPushNotifications(allNotifs);
		
	}

	public static void scheduleTestLocalNotifications(string messageKey, int seconds = 0)
	{
		if (Instance != null)
		{
			if (Instance.testNotifsToSchedule == null)
			{
				Instance.testNotifsToSchedule = new Dictionary<NotificationMessage, int>();
			}
			Instance.populateNotifications ();
			
			NotificationMessage nMessage;
			nMessage = NotificationInfo.getRandomMessage(messageKey);
			//Storing these to be scheduled when the game is actually paused
			//All scheduled notifs get cleared onPause, so we can't schedule these test ones right away
			Instance.testNotifsToSchedule[nMessage] = seconds;
		}
	}
	
	
	
	private static void addTestLocalNotifications()
	{
		if (Instance != null && Instance.testNotifsToSchedule != null)
		{
			foreach (KeyValuePair<NotificationMessage, int> kvp in Instance.testNotifsToSchedule)
			{
				bool success = Instance.setNotification (kvp.Key, kvp.Value, "");
				if (kvp.Key.key != null)
				{
					if (Data.debugMode)
					{
						if (!success)
						{
							Debug.LogFormat("PN> test local notif FAILED to be added {0}, reason should be logged above", kvp.Key.key);
						}
						else
						{
							Debug.LogFormat("PN> test local notif added {0}", kvp.Key.key);
						}

					}
				}
				else
				{
					if (Data.debugMode)
					{
						Debug.LogFormat("PN> test local notif unable to be added {0}", kvp.Key.notifType);
					}
				}
			}
			
			Instance.testNotifsToSchedule.Clear();
		}
	}

	public static long CurrentUnixSeconds()
	{
		DateTime Epoch = new DateTime(1970,1,1,0,0,0,0,System.DateTimeKind.Utc);
		TimeSpan delta = DateTime.UtcNow.Subtract(Epoch);
		long returnValue = Convert.ToInt64(delta.TotalSeconds);

		return returnValue;
	}

	// This function checks if extradata is stored in the native code.
	// If it is then it fires off to get the incentivized link
	private void checkForExtraData()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		AndroidJNI.AttachCurrentThread();
		string extraData = "";
		AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
		AndroidJavaClass zyngaUnityActivity = new AndroidJavaClass("com.zynga.ZyngaUnityActivity.ZyngaUnityActivity");
		if (zyngaUnityActivity != null)
		{
			// Grab the extra data from the java class.
			extraData = zyngaUnityActivity.CallStatic<string>("getExtraPushNotificationData");
			// Now that we have it, set it to null.
			zyngaUnityActivity.CallStatic("resetExtraPushNotificationData");
			if (extraData != null && extraData != "")
			{
				GameObject go = Packages.Singleton.ZyngaUnityCallbacks;
				ZyngaUnityCallbacks zyngaUnityCallbacks = go.GetComponent<ZyngaUnityCallbacks>();
				zyngaUnityCallbacks.ReceivePushNotification(extraData);
			}
		} else {
			Debug.Log("*****ZYNGAUNITYACTIVITY extra data IS NULL*****");
		}
#endif

#if UNITY_IOS && !UNITY_EDITOR
		string extraData = UnityGetCachedUserInfo();
		if (!extraData.IsNullOrWhiteSpace())
		{
			GameObject go = Packages.Singleton.ZyngaUnityCallbacks;
			if (go != null)
			{	
				ZyngaUnityCallbacks zyngaUnityCallbacks = go.GetComponent<ZyngaUnityCallbacks>();
				zyngaUnityCallbacks.ReceivePushNotification(extraData);
			}
		}
#endif
	}

#region IDependencyInitializer implementation

	/// Get managers that the NotificationManager is dependent on
	public Type[] GetDependencies() {
		return new Type[] {
			typeof(AuthManager), typeof(SocialManager)
		};
	}

	/// Initialization routine... empty at the moment. Will have things when push notifs are ready
	public void Initialize(InitializationManager mgr) 
	{
		if (PushNotifsAllowed)
		{
			if (Data.debugMode)
			{
				Debug.LogWarning("PN> Registering for push notifications in initialize");
				if (Glb.serverLogPushNotifications)
				{
					Server.sendLogInfo("PN", "Registering for push notifications in initialize");
				}
			}
			RegisterForPushNotifications();
		}
	
		StatsManager.Instance.LogCount("pn_enablement", (DevicePushNotifsEnabled ? "Enabled" : "Disabled"));
		
		if (Instance.prefs.GetInt("LastNotifValue", -1) != (DevicePushNotifsEnabled ? 1 : 0))
		{
			//Turning off per Anshul's request
			//StatsManager.Instance.LogCount("notification", "game", "push_notif", DevicePushNotifsEnabled ? "turned_on" : "turned_off");
			Instance.prefs.SetInt("LastNotifValue", (DevicePushNotifsEnabled ? 1 : 0));
			Instance.prefs.Save();
		}

		mgr.InitializationComplete(this);
		isInitialized = true;
	}
	
	// short description of this dependency for debugging purposes
	public string description()
	{
		return "NotificationManager";
	}
#endregion

}

// Actual Message Format
public struct NotificationMessage
{
	public string notifType;
	public string key;
	public string message;
	public JSON options;
}

// Info Sent from SCAT groups messages by type
public class NotificationInfo
{
	public static List<NotificationInfo> _allNotifications = new List<NotificationInfo>();
	
	public string notifType;
	public List<NotificationMessage> messages = new List<NotificationMessage>(); 
	
	NotificationInfo(string type)
	{
		notifType = type;
	}
	
	public bool hasMessage(string key)
	{
		return messages.FindIndex(
		
		(message) => {
			return message.key == key;
		}) != -1;
	}
	
	public static void clearAllInfo()
	{
		_allNotifications.Clear();
	}
	
	public static void addInfo(string nType, string nKey, string nMessage, JSON nOptions)
	{
		NotificationInfo notifInfo = findByType(nType);
		// If new
		if (notifInfo == null)
		{
			notifInfo = new NotificationInfo(nType);
			_allNotifications.Add(notifInfo);
		}
		
		if (!notifInfo.hasMessage(nKey))
		{
			
			notifInfo.messages.Add( new NotificationMessage
			{ 
				notifType=nType, key=nKey, message=nMessage, options=nOptions
			}
			);
		}
	}
	
	public static NotificationInfo findByType(string nType)
	{
		for (int i = 0; i< _allNotifications.Count; i++)
		{
			if (_allNotifications[i].notifType == nType)
			{
				return _allNotifications[i];
			}
		}
		return null;
	}
	
	public static List<NotificationMessage> getAllMessagesOfType(string nType)
	{
		NotificationInfo notifInfo = findByType(nType);
		if (notifInfo != null)
		{
			return notifInfo.messages;
		}
		
		return new List<NotificationMessage>();
	}

	public static NotificationMessage getMessageOfTypeWithKey(string nType, string messageKey)
	{
		if (string.IsNullOrEmpty(nType) || string.IsNullOrEmpty(messageKey))
		{
			return new NotificationMessage();
		}
		//Ensure that the notifications are loaded before getting a random message.
		if (NotificationManager.Instance != null)
		{
			NotificationManager.Instance.populateNotifications();
		}
		NotificationInfo notifInfo = findByType(nType);
		if (notifInfo != null)
		{
			foreach (var msg in notifInfo.messages)
			{
				if (msg.key.Equals(messageKey))
				{
					return msg;
				}
			}
		}
		
		return new NotificationMessage();
	}
	
	// Gets a random message by type
	public static NotificationMessage getRandomMessage(string nType) 
	{
		//Ensure that the notifications are loaded before getting a random message.
		if (NotificationManager.Instance != null)
		{
			NotificationManager.Instance.populateNotifications();
		}
		NotificationInfo notif = NotificationInfo.findByType(nType);

		if (notif != null)
		{
			return notif.messages[UnityEngine.Random.Range(0, notif.messages.Count)];
		}
		
		return new NotificationMessage();
	}
					
	// Gets Notification Message based on some special key words. May move into Filter special words in Localize.cs
	public static bool getNotificationMessage(string keyName, out string message)
	{
		if (string.IsNullOrEmpty(keyName) == true) 
		{
			message = "";
			return false;
		}
		
		message = Localize.text(keyName);

		if (message.Contains("(last_slot)"))
		{
			string lastSlotKey = SlotsPlayer.getPreferences().GetString(Prefs.LAST_SLOT_GAME, "");
			LobbyGame lastPlayed = LobbyGame.find(lastSlotKey);
			
			if (string.IsNullOrEmpty(lastSlotKey) == false && lastPlayed != null)
			{
				message = message.Replace("(last_slot)", lastPlayed.name);
			}
			else
			{
				return false;
			}
		}
		
		if (message.Contains("(random_friend)"))
		{
			int friends = SocialMember.friendPlayers.Count;
			
			if (friends > 0)
			{
				string friendName = SocialMember.friendPlayers[UnityEngine.Random.Range(0,friends)].firstName;
				message = message.Replace("(random_friend)", friendName);
			}
			else
			{
				return false;
			}
		}
		
		if (message.Contains ("(multiplier)"))
		{
			int bonusDay = SlotsPlayer.instance.dailyBonusTimer.day + 1;
			
			if (bonusDay <= 1) bonusDay = 2;
			
			message = message.Replace("(multiplier)", bonusDay.ToString());
		}
		
		if (message.Contains ("(days_left)"))
		{
			int bonusDay = 7 - SlotsPlayer.instance.dailyBonusTimer.day;
			
			if (bonusDay <= 2) bonusDay = 2;
			
			message = message.Replace("(days_left)", bonusDay.ToString());
		}
		
		if (message.Contains ("(pet_name)"))
		{
			if (VirtualPetsFeature.instance != null)
			{
				message = message.Replace("(pet_name)", VirtualPetsFeature.instance.petName);
			}
			else
			{
				return false;
			}
			
		}
		
		return true;
	}
}

// Notification object for ZDK use
public class NotificationItem
{
	/// This is the number of seconds from unix epoch time
	
	public string subtype = null;
	public long triggerTimeSeconds;
	public long originalTriggerTimeSeconds;
	public string message = null;
	public string uniqueId = null;
	public string title = null;
	public Dictionary<string,string> userDataDict = new Dictionary<string, string>();
	
	// all notifications require a unique identifier on android
	#if UNITY_ANDROID && !UNITY_EDITOR
	private static int androidUniqueId = 1;
	private readonly int _androidNotifId = androidUniqueId++;
	#endif

	private NotificationItem(){}

	public NotificationItem(string subtype, string uniqueId, long triggerTimeSeconds, string message, string title)
	{
		this.originalTriggerTimeSeconds = triggerTimeSeconds;
		this.triggerTimeSeconds = NotificationManager.CurrentUnixSeconds() + triggerTimeSeconds;
		this.message = message;
		this.uniqueId = uniqueId;
		this.subtype = subtype;
		this.title = title;
		this.userDataDict.Add("subtype", subtype);
		this.userDataDict.Add("uniqueId", uniqueId);
		this.userDataDict.Add("sender", (PackageProvider.Instance.ServicesCommon.Client.Session != null) ? PackageProvider.Instance.ServicesCommon.Client.Session.Zid.ToString() : "");
		this.userDataDict.Add("day", (originalTriggerTimeSeconds / (Common.SECONDS_PER_DAY-1)).ToString());
	}
	
	public void Queue(int counter = 0) 
	{
		//TODO: Girish
		Zynga.Zdk.ZyngaLocalNotifications.UILocalNotification ln = new Zynga.Zdk.ZyngaLocalNotifications.UILocalNotification();

		ln.firedateunixts = triggerTimeSeconds.ToString();
		ln.alertBody = string.IsNullOrEmpty (message) ? "empty message" : message;
		ln.alertLaunchImage = "";
		ln.userInfo = userDataDict;

#if UNITY_IPHONE && !UNITY_EDITOR
		if (!string.IsNullOrEmpty(title))
		{
			ln.alertTitle = title;
		}
		ln.applicationIconBadgeNumber = counter.ToString();
		ln.alertAction = Localize.text("play");
		ln.hasAction = false;
		ln.soundName = ""; // "ui_quest_initiated.caf";
#elif UNITY_ANDROID && !UNITY_EDITOR
		if (!string.IsNullOrEmpty(title))
		{
			ln.alertTitle = title;
		}
		else
		{
			ln.alertTitle = Localize.getGameName();
		}
		ln.ticker = Localize.getGameName();
		ln.id = _androidNotifId.ToString();
		string iconName = "notification_icon";
		ln.alertLaunchImage = iconName;
#endif

		Debug.LogFormat("PN> Queue {0}: {1}", ln.alertTitle, ln.alertBody );
		ZyngaLocalNotifications.QueueNotification(ln);
	}
	
	public override string ToString()
	{
		return "triggerTimeSeconds: " + this.triggerTimeSeconds + Environment.NewLine +
			"originalTriggerTimeSeconds: " + this.originalTriggerTimeSeconds + Environment.NewLine +
			"message: " + this.message + Environment.NewLine +
			"uniqueId: " + this.uniqueId + Environment.NewLine +
			"subtype: " + this.subtype;
	}
}

public enum SoftPromptEvents
{
	Other = 0,
	Payment, 
	HourlyBonus

}

// Ludington: I'm not sure what these numbers mean, and no one else seems to either.
// So I added RequestCoins = 1 and RequestCoinsGranted = 2 assuming the number don't matter
// anymore.
public enum NotificationEvents
{
	RequestCoins = 1,
	RequestCoinsGranted = 2,
	SendChallenge=501157, 
	CompletedChallenge=501180,
	SendCoins=501182,
	SendSpins=501183,
	Jackpot=501184,
	StartPlaying=503738,
	PartnerPowerupNudge=999,
	PartnerPowerupFBNudge=888,
	PartnerPowerupFBComplete = 777,
	PartnerPowerupPlayerComplete = 666,
	PartnerPowerupFBPaired = 111,
	PartnerPowerupPaired = 222

	// For testing in different environments
	// Ludington: I'm taking these out because the system doesn't do notfs 
	// based on env any more (I'm pretty sure.)
	// Sandbox=500867,
	// JackPotENV3= 502510,
	// JackPotStaging=502688,
	// StartPlayingENV3=503773,
	// StartPlayingStaging=503772
	// Testing=39009,
	// Other=0,
}

