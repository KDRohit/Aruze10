/*
** Class: AnalyticsManager
** Author: Kevin Kralian
** Date: October 14, 2016
** Description: A manager class to handle the initialization and calling of the Zynga.Metrics.Analytics package
**   (See https://github-ca.corp.zynga.com/UnityTech/Zynga.Metrics.Analytics/tree/development)
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zynga.Core.Util;
using Zynga.Metrics.Analytics;
using Zynga.Metrics.ZTrack;
using Zynga.Zdk.Services.Common;

#pragma warning disable 0618 // `Zynga.Core.UnityUtil.DeviceInfo' is obsolete, however we need to be able to set OverrideCurrentMemoryMB

public class AnalyticsManager : IDependencyInitializer , IResetGame
{
	readonly TimeSpan callHeartBeatPeriod = TimeSpan.FromMinutes(0.5);
	DateTime lastHeartBeat = DateTime.MinValue;

	GamePhase gamePhase = GamePhase.InLoading;  // start in loading state
	string gamePhaseInfo = "loading";           // start in loading state
	bool wasLoading = true;                     // previous frame was loading?
	TICoroutine heartbeatCoroutine;

	private static bool analyticsServiceAvailable => (Packages.Analytics != null && Packages.Analytics.AnalyticsService != null);

	private static bool trackDataModelTwo => (Data.liveData != null && Data.liveData.getBool("ANALYTICS_TRACK_DM2", false));

	private bool isGameActive = false; // Used for Visit call.

	private delegate void AnalyticsLogAction();
	private static List<AnalyticsLogAction> pendingCalls = new List<AnalyticsLogAction>();

	// Singleton
	private static AnalyticsManager _instance;
	public static AnalyticsManager Instance
	{
		get
		{
			if (_instance == null) { _instance = new AnalyticsManager(); }
			return _instance;
		}
	}

	private static void TraceAnalyticsEvent(string eventType)
	{
		if (Data.debugMode)
		{
			//czablocki - 2/3/2021 Commenting this out because it spams logs that crash v4.8.4 of Bugsnag when its
			//Notify() hook handles them to leave a Breadcrumb
			//Debug.Log($"--ANALYTICS: {eventType}");
		}
	}

	// Call when the app is paused or resumed
	public static void onAppPausedOrResumed(bool isPaused)
	{
		if (isPaused)
		{
			Instance.OnAppPaused();
		}
		else
		{
			Instance.OnAppResumed();
		}
	}

	// Coroutine so we can be updated every frame via RoutineRunner
	IEnumerator trackFrameTimeAndDoHeartbeatCoroutine()
	{
		// Updates every frame until our package is shutdown
		while (Packages.Analytics != null)
		{
			trackFrameTimeAndDoHeartbeat();
			yield return null;
		}
	}

	// Call every frame; will update frametime metrics & periodically call analytics heartbeat
	void trackFrameTimeAndDoHeartbeat()
	{
		if (!analyticsServiceAvailable)
		{
			return;
		}
		// Handle loading start/stop transitions
		if (Loading.isLoading != wasLoading)
		{
			if (Loading.isLoading)
			{
				OnStopInteractive();
			}
			else
			{
				OnStartInteractive();
			}
			wasLoading = Loading.isLoading;
		}

		// Get GamePhase & PhaseInfo from game current global game states
		GamePhase newPhase;
		string newPhaseInfo;
		getPhaseInfo(out newPhase, out newPhaseInfo);

		if (newPhase != gamePhase || newPhaseInfo != gamePhaseInfo)
		{
			//Debug.Log("AnalPhaseChange:   " + newPhase + ",  " + newPhaseInfo);
			gamePhase = newPhase;
			gamePhaseInfo = newPhaseInfo;
		}

		// Tell Analytics package our memory usage, maybe 0 if profiling is disabled (may need platform-specific impl)
		if (UnityEngine.Profiling.Profiler.usedHeapSizeLong > 0)
		{
			//Zynga.Core.UnityUtil.DeviceInfo.OverrideCurrentMemoryMB = UnityEngine.Profiling.Profiler.usedHeapSizeLong / (1024.0 / 1024.0);
		}

		// Track frame time
		Packages.Analytics.AnalyticsService.DoTrackFrameTime(gamePhase, gamePhaseInfo, Time.deltaTime * 1000.0);

		// Periodically call onHeartbeat
		DateTime utcNow = DateTime.UtcNow;
		if (utcNow.Subtract(lastHeartBeat) > callHeartBeatPeriod)
		{
			Packages.Analytics.AnalyticsService.OnHeartBeat();
			flushPendingCalls();
			lastHeartBeat = utcNow;
		}
	}

	// Determine effective game-state we're in, in this priority order:
	//  Loading Screen
	//  Dialog (which one)
	//  Lobby (Normal or VIP)
	//  BonusGame / GiftingGame / SlotGame (which)
	private void getPhaseInfo(out GamePhase phase, out string phaseExtraInfo)
	{
		// default values...
		phase = GamePhase.InCoreLoop;
		phaseExtraInfo = "undefined";

		// Only want to add this state if we're in a basegame or gifting game
		bool addSpinningOrIdleState = false;

		if (Loading.isLoading)
		{
			phase = GamePhase.InLoading;
			phaseExtraInfo = "loading";
		}
		else if (Dialog.instance.isShowing)
		{
			phase = GamePhase.InDialog;
			phaseExtraInfo = Dialog.instance.currentDialog.type.keyName;
		}
		else if (MainLobby.instance != null)
		{
			phase = GamePhase.InMainMenu;
			phaseExtraInfo = "Lobby";
		}
		else if (VIPLobby.instance != null)
		{
			phase = GamePhase.InMainMenu;
			phaseExtraInfo = "VIPLobby";
		}
		else if (BonusGameManager.isBonusGameActive)
		{
			phase = GamePhase.InGame;
			phaseExtraInfo = BonusGameManager.instance.currentGameKey + "-" + BonusGameManager.instance.currentGameType.ToString().ToLower();

			if (BonusGameManager.instance.currentGameType == BonusGameType.GIFTING)
			{
				addSpinningOrIdleState = true;
			}
		}
		else if (GameState.game != null)
		{
			phase = GamePhase.InGame;
			phaseExtraInfo = GameState.game.keyName;
			addSpinningOrIdleState = true;
		}

		// We only get a single userdata string (phaseExtraInfo), put extra userdata here and separate it in splunk  :-(

		// Add isSpinning/isIdle state to baseslot & gifting games
		if (addSpinningOrIdleState)
		{
			phaseExtraInfo += Glb.spinTransactionInProgress ? " isSpinning" : " isIdle";
		}

		// Track the games target FPS (20/30/60)
#if !UNITY_WEBGL
		phaseExtraInfo += " targetFPS=" + Application.targetFrameRate;
#endif

		// Track the asset bundle variant here too
		phaseExtraInfo += " variant=" + AssetBundleVariants.getActiveVariantName();
	}

#region pending calls management
	// Check for service being available; if not ready, add action to pending calls and return true.
	private bool handleNotReady(string eventName, AnalyticsLogAction logAction)
	{
		if (!analyticsServiceAvailable)
		{
			if (Data.debugMode)
			{
				Debug.Log($"--ANALYTICS: handleNotReady: {eventName}");
			}
			pendingCalls.Add(logAction);
			return true;
		}
		return false;
	}

	private void flushPendingCalls()
	{
		if (!analyticsServiceAvailable)
		{
			return;
		}

		if (pendingCalls.Count > 0)
		{
			foreach (AnalyticsLogAction call in pendingCalls)
			{
				// Call them now.
				call();
			}
			resetPendingCalls();
		}
	}

	private void resetPendingCalls()
	{
		if (pendingCalls.Count > 0)
		{
			pendingCalls = new List<AnalyticsLogAction>();
		}
	}
#endregion pending calls management

	#region Analytics events

	public void OnAppStarted()
	{
		const string eventName = "OnAppStarted";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.OnAppStarted());
			return;
		}

		TraceAnalyticsEvent($"{eventName}");
		Packages.Analytics.AnalyticsService.OnAppStarted();
		TriggerVisit();
	}

	public void OnAppShutdown()
	{
		const string eventName = "OnAppShutdown";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.OnAppShutdown());
			return;
		}

		TraceAnalyticsEvent($"{eventName}");
		Packages.Analytics.AnalyticsService.OnAppShutdown();
	}

	public void OnUserAuthenticated()
	{
		const string eventName = "OnUserAuthenticated";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.OnUserAuthenticated());
			return;
		}

		TraceAnalyticsEvent($"{eventName}");
		Packages.Analytics.AnalyticsService.OnUserAuthenticated(ZdkManager.Instance.Zsession.Snid);
	}

	public void OnAppPaused()
	{
		const string eventName = "OnAppPaused";
		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.OnAppPaused());
			return;
		}

		TraceAnalyticsEvent($"{eventName}");
		Packages.Analytics.AnalyticsService.OnAppPaused();
	}

	public void OnAppResumed()
	{
		const string eventName = "OnAppResumed";
		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.OnAppResumed());
			return;
		}

		TraceAnalyticsEvent($"{eventName}");
		Packages.Analytics.AnalyticsService.OnAppResumed();
		TriggerVisit();
	}

	public void OnStopInteractive()
	{
		isGameActive = false;
		const string eventName = "OnStopInteractive";
		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.OnStopInteractive());
			return;
		}

		TraceAnalyticsEvent($"{eventName}");
		Packages.Analytics.AnalyticsService.OnStopInteractive();
	}

	public void OnStartInteractive()
	{
		isGameActive = true;
		const string eventName = "OnStartInteractive";
		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.OnStartInteractive());
			return;
		}

		TraceAnalyticsEvent($"{eventName}");
		Packages.Analytics.AnalyticsService.OnStartInteractive();
	}

	/// <summary>
	///  Log install if a new user.  Sends Data Model 1 events as well as Data Model 2 events when enabled.
	/// </summary>
	public void CheckLogInstall()
	{
		const string eventName = "LogInstall";

		if (ZdkManager.Instance.Zsession == null)
		{
			handleNotReady(eventName, () => Instance.CheckLogInstall());
			return;
		}

		string installKey = StatsManager.KEY_FIRST_USE_OF_SN + ZdkManager.Instance.Zsession.Snid;
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		if (UnityPrefs.GetInt(installKey, 0) == 0)
		{
			UnityPrefs.SetInt(installKey, 1);
			UnityPrefs.Save();
		}
		else
		{
			// Already installed, nothing to do.
			return;
		}

		StatsManager.Instance.LogInstall();

		TriggerInstall();
	}

	public void LogAdjust(string adjustId)
	{
		const string eventName = "LogAdjust";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.LogAdjust(adjustId));
			return;
		}

		ZTrackAdjustAttributionEvent adjustEvent = new ZTrackAdjustAttributionEvent
		{
			AdjustId = adjustId
		};
		LogAnalyticsDataModelTwoEvent(adjustEvent.ToTrackEvent(), eventName);
	}

	public void LogLocale(string locale)
	{
		const string eventName = "LogLocale";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.LogLocale(locale));
			return;
		}

		ZTrackGameLocaleEvent localeEvent = new ZTrackGameLocaleEvent
		{
			LocaleType = ZTrackGameLocaleEvent.LocaleTypeGameUiLang,
			Locale = locale
		};
		LogAnalyticsDataModelTwoEvent(localeEvent.ToTrackEvent(), eventName);
	}

	public void LogTOSView()
	{
		const string eventName = "LogTOSView";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.LogTOSView());
			return;
		}

		StatsManager.Instance.LogMileStone("tos_dialog_view", 1);

		ZTrackTermsOfServiceAction trackEvent = new ZTrackTermsOfServiceAction
		{
			Action = "view"
		};
		LogAnalyticsDataModelTwoEvent(trackEvent.ToTrackEvent(), eventName);

	}

	public void LogTOSAccept()
	{
		const string eventName = "LogTOSAccept";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.LogTOSAccept());
			return;
		}

		StatsManager.Instance.LogMileStone("tos_dialog_accept", 1);

		ZTrackTermsOfServiceAction trackEvent = new ZTrackTermsOfServiceAction
		{
			Action = "accept"
		};
		LogAnalyticsDataModelTwoEvent(trackEvent.ToTrackEvent(), eventName);
	}

	public void LogLocalNotificationScheduled(string status, NotificationItem item)
	{
		const string eventName = "LogLocalNotificationScheduled";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.LogLocalNotificationScheduled(status, item));
			return;
		}

		if (ZdkManager.Instance.Zsession == null)
		{
			Debug.LogErrorFormat("trackLocalNotification -- ZSession is null so not sending logMessage");
		}
		else
		{
			StatsManager.Instance.LogMessage(
				"local_pn",
				status,
				ZdkManager.Instance.Zsession.Zid,
				item.subtype.ToString(),
				item.uniqueId,
				"",
				"",
				DateTime.Now.AddSeconds((double)item.originalTriggerTimeSeconds).ToShortTimeString());
		}

		ZTrackLocalNotificationAction notifAction = new ZTrackLocalNotificationAction
		{
			SourceChannel = ZTrackDeviceNotificationAction.ChannelPn,
			Status = status
		};
		LogAnalyticsDataModelTwoEvent(notifAction.ToTrackEvent(), eventName);
	}

	public void LogLocalNotificationClicked(string subtype, string uniqueId, string day)
	{
		const string eventName = "LogLocalNotificationClicked";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.LogLocalNotificationClicked(subtype, uniqueId, day));
			return;
		}

		StatsManager.Instance.LogMessage(
			"local_pn",
			"click",
			ZdkManager.Instance.Zsession.Zid,
			subtype,
			uniqueId,
			"",
			"",
			day);

		ZTrackLocalNotificationAction notifAction = new ZTrackLocalNotificationAction
		{
			SourceChannel = ZTrackDeviceNotificationAction.ChannelPn,
			Status = ZTrackDeviceNotificationAction.StatusClicked
		};
		LogAnalyticsDataModelTwoEvent(notifAction.ToTrackEvent(), eventName);
	}

	public void LogPushNotificationSent(string templateId, string templateName = "", string campaignId = "", string campaignName="")
	{
		const string eventName = "LogPushNotificationSent";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.LogPushNotificationSent(templateId));
			return;
		}

		// Ensure parameter values are valid for validation.
		if (string.IsNullOrEmpty(templateId) || templateId == "-1")
		{
			templateId = "unknown";
		}
		if (string.IsNullOrEmpty(templateName) || templateName == "-1")
		{
			templateName = "unknown";
		}
		if (string.IsNullOrEmpty(campaignId) || campaignId == "-1")
		{
			campaignId = "unknown";
		}
		if (string.IsNullOrEmpty(campaignName) || campaignName == "-1")
		{
			campaignName = "unknown";
		}

		ZTrackScheduledNotificationAction notifAction = new ZTrackScheduledNotificationAction
		{
			SourceChannel = ZTrackDeviceNotificationAction.ChannelPn,
			TemplateId = templateId,
			TemplateName = templateName,
			CampaignId = campaignId,
			CampaignName = campaignName,
			Status = ZTrackDeviceNotificationAction.StatusSent
		};
		if (Data.debugMode)
		{
			Debug.Log($"--ANALYTICS: LogPN {templateId}:{templateName} {campaignId}:{campaignName}");
		}
		LogAnalyticsDataModelTwoEvent(notifAction.ToTrackEvent(), eventName);
	}

	public void LogPlayerGiftAction(string giftType, long amount)
	{
		const string eventName = "LogPlayerGiftAction";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.LogPlayerGiftAction(giftType, amount));
			return;
		}

		ZTrackPlayerGiftAction giftAction = new ZTrackPlayerGiftAction
		{
			RelationshipType = "friend",
			Gift = giftType,
			Amount = amount
		};
		LogAnalyticsDataModelTwoEvent(giftAction.ToTrackEvent(), eventName);
	}

	public void LogPlayerGraphAction(string actionName)
	{
		const string eventName = "LogPlayerGraphAction";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.LogPlayerGraphAction(actionName));
			return;
		}

		// Convert our NetworkFriendsAction actionName to ZTrack graph action name.
		string graphActionName = "created";
		switch (actionName)
		{
			case NetworkFriendsAction.INVITE_FRIEND:
				graphActionName = "created";
				break;
			case NetworkFriendsAction.REMOVE_FRIEND:
				graphActionName = "deleted";
				break;
			case NetworkFriendsAction.ACCEPT_INVITE:
				graphActionName = "accepted";
				break;
			default:
				// Ignore
				return;
		}

		ZTrackPlayerGraphAction graphAction = new ZTrackPlayerGraphAction
		{
			Sender = new Pid(),	// This shouldn't have a PlayerId since we don't support the concept on playerId on draft4
			Recipients = new Pid[] {new Pid()}, // This shouldn't have a PlayerId since we don't support the concept on playerId on draft4
			Action = graphActionName,
			Group = "invite"
		};
		LogAnalyticsDataModelTwoEvent(graphAction.ToTrackEvent(), eventName);
	}

	private void TriggerVisit()
	{
		string eventName = "ZTrackPlayerActiveEvent";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.TriggerVisit());
			return;
		}

		// Sends visit.player_active DM2 event.
		ZTrackPlayerActiveEvent visitEvent = new ZTrackPlayerActiveEvent
		{
			Active = isGameActive,
			SNUId = ZdkManager.Instance.Zsession == null ? Snid.Anonymous.ToString() : ZdkManager.Instance.Zsession.Snid.ToString()
		};

		LogAnalyticsDataModelTwoEvent(visitEvent.ToTrackEvent(), eventName);
	}

	private void TriggerInstall()
	{
		string eventName = "ZTrackPlayerAuthEvent";

		if (!analyticsServiceAvailable)
		{
			handleNotReady(eventName, () => Instance.TriggerInstall());
			return;
		}

		// Sends install.player_auth DM2 event.
		ZTrackPlayerAuthEvent installEvent = new ZTrackPlayerAuthEvent
		{
			ClientId = Convert.ToInt64(Zynga.Zdk.ZyngaConstants.ClientId),
			Channel = "facebook" // Seems this should be install channel, but we don't have access to that.
		};

		LogAnalyticsDataModelTwoEvent(installEvent.ToTrackEvent(), eventName);
	}

	private void LogAnalyticsDataModelTwoEvent(Zynga.Zdk.Services.Track.TrackEventBase trackEvent, string eventName)
	{
		if (!trackDataModelTwo || !analyticsServiceAvailable)
		{
			return;
		}

		TraceAnalyticsEvent($"{eventName}");
		Packages.Analytics.AnalyticsService.LogEvent(trackEvent);
	}

	#endregion Analytics events

	#region IDependencyInitializer implementation

	// Initializes this AnalyticsManager
	void IDependencyInitializer.Initialize(InitializationManager initMgr)
	{
		Bugsnag.LeaveBreadcrumb($"AnalyticsManager::Initialize(): DM 2: {trackDataModelTwo}");
		TraceAnalyticsEvent($"Initialize(): DM 2: {trackDataModelTwo}");

		OnAppStarted();

		// AuthManager/SocialManager is already connected at this point
		OnUserAuthenticated();

		// Start the per-frame coroutine update...
		heartbeatCoroutine = RoutineRunner.instance.StartCoroutine(trackFrameTimeAndDoHeartbeatCoroutine());

		// Check for any calls before analytics service was ready.
		flushPendingCalls();

		// We're done initializing
		initMgr.InitializationComplete(this);
	}

	// Returns a list of ZDK packages that we are dependent on
	System.Type[] IDependencyInitializer.GetDependencies()
	{
		return new System.Type[] { typeof(ZdkManager), typeof(AuthManager), typeof(SocialManager), typeof(StatsManager) };
	}
		
	// Returns a short description of this dependency
	string IDependencyInitializer.description()
	{
		return "AnalyticsManager";
	}

	#endregion IDependencyInitializer implementation

	public static void resetStaticClassData()
	{
		// Ensure OnStopInteractive is called on reset for login, if heartbeat coroutine does not call it before
		// analytics service is terminated.
		if (!_instance.wasLoading)
		{
			_instance.OnStopInteractive();
		}

		// Stop our coroutine
		if (_instance != null && _instance.heartbeatCoroutine != null)
		{
			RoutineRunner.instance.StopCoroutine(_instance.heartbeatCoroutine);
			_instance.heartbeatCoroutine = null;
		}

		_instance.resetPendingCalls();

		// Release our singleton
		_instance = null;
	}
}
