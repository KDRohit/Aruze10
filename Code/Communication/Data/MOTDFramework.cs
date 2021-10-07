using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.EUE;
using Com.Scheduler;

public class MOTDFramework : MonoBehaviour, IResetGame
{
	public const string VIP_ROOM_CALL_TO_ACTION = "vip_room";
	public const string LOZ_LOBBY_CALL_TO_ACTION = "loz_lobby";
	public const string MAX_VOLTAGE_LOBBY_CALL_TO_ACTION = "max_voltage_lobby";
	public const string SLOTVENTURE_LOBBY_CALL_TO_ACTION = "slotventure_lobby";
	public const string CHALLENGE_LOBBY_CALL_TO_ACTION = "challenge_lobby";
	
	// Static Collections for managing/sorting the motds.
	public static Dictionary<string, int> sortingOrder;
	public static List<string> sortedMOTDQueue = new List<string>();
	public static HashSet<string> seenThisSession = new HashSet<string>();


	// Static Queue and bool to pop MOTDs in the correct order regardless of download speed.
	public static Queue<string> motdToShowQueue = new Queue<string>();
	private static bool isTryingToShowMotd = false;
	private static bool showCallWasMade = false;
	private static int lastMotdPopTime = 0;
	private static string lastMotdPoppedKey = "";
	private static float motdTimeoutDuration = 30f;
	private static  SURFACE_POINT lastSurfacePoint;

#if !ZYNGA_PRODUCTION
	// If this is not a production build, maintain a list of passed over MOTDs	
	public static Dictionary<string, string> passedOverThisSession = new Dictionary<string, string>();
	// maintain list of MOTD that returned true in shouldShow yet bailed out in show
	public static List<string> noShowList = new List<string>();
#endif
	
	public static string callToActionGameKey = "";
	
	// Session/Per Visit dialog spawn limits (read from livedata).
	private static int limitSessionRtl = 0;
	private static int limitPerRtl = 0;	
	private static int limitSessionVip = 0;
	private static int limitPerVip = 0;
	private static int limitSessionAppEntry = 0;
	public static int limitSessionSale = 0;

	//	Current dialog spawn counters.
	private static int currentRtlCount = 0;
	private static int currentVipCount = 0;
	private static int currentAppEntryCount = 0;
	public static int currentSaleCount = 0;
	
	private static List<string> blacklist = new List<string>();
	
	// Init Function. Because it has so many variables read in from livedata, pass the JSON into
	// the init funciton so that we can do it here.
	public static void init(LiveData liveData)
	{
		// Setting the constants from livedata
		limitSessionRtl		  = liveData.getInt("MOTD_LIMIT_RTL_SESSION", 0);
		limitSessionVip		  = liveData.getInt("MOTD_LIMIT_VIP_SESSION", 0);
		limitSessionAppEntry  = liveData.getInt("MOTD_LIMIT_APP_ENTRY", 0);
		limitPerRtl			  = liveData.getInt("MOTD_LIMIT_RTL_EVENT", 0);
		limitPerVip			  = liveData.getInt("MOTD_LIMIT_VIP_EVENT", 0);
		limitSessionSale	  = liveData.getInt("MOTD_LIMIT_SALE_SESSION", 0);
		string[] list		  = liveData.getArray("MOTD_BLACKLIST", new string[0]);

		motdTimeoutDuration = 30f;
		// registering the callback for when a new list is sent down from the server.
		Server.registerEventDelegate("motd_new_list", motdListAction, true);	

		blacklist.AddRange(list);
	}

	public static string getMOTDStatusReport()
	{
		string status = " isTryingToShowMotd = " + isTryingToShowMotd + " lastMotdPoppedKey = " + lastMotdPoppedKey;

		return status;
	}


	public static IEnumerator showPreLobbyDialog(string message)
	{
		string[] messageSplit = message.Split(':');
		if (messageSplit.Length != 3)
		{
			Debug.LogError("PreLobby Dialog message was incorrectly formatted");
			PlayerPrefsCache.SetString(Prefs.LAST_PRE_LOBBY_MESSAGE, "");
			yield break;
		}
		// The zrt should have a title and body and view count seperated by :
		// If it doesn't, then its not properly formatted.
		string title = messageSplit[0];
		string body = messageSplit[1];
		string showEveryLoad = messageSplit[2];

		if (showEveryLoad == "0")
		{
			// If this message should not show on every load.
			string lastMessage = PlayerPrefsCache.GetString(Prefs.LAST_PRE_LOBBY_MESSAGE, "");
			if (lastMessage == message)
			{
				// If the message is the same as the previous one, then skip it.
				yield break;
			}
			else
			{
				// If not, save this message to PlayerPrefsCache and continue showing it.
				PlayerPrefsCache.SetString(Prefs.LAST_PRE_LOBBY_MESSAGE, message);
			}	
		}
		
		// Attempt to localize them
		title = Localize.textOrUpper(title, title);		
		body = Localize.textOr(body, body);

		// Hide the loading screen visually, but not truly hiding it.
		Loading.instance.setAlpha(0f);

		// Show the dialog.
		GenericDialog.showDialog(
				Dict.create(
					D.TITLE, title,
				D.MESSAGE, body,
				D.SHOULD_HIDE_LOADING, false,
				D.REASON, "pre-lobby-motd-message"
			),
			SchedulerPriority.PriorityType.IMMEDIATE
		);
		
		while (Dialog.instance.isShowing)
		{
			// Wait for the dialog to be clicked through before continuing with the data load.
			yield return null;
		}

		// Re-show the loading screen.
		Loading.instance.setAlpha(1f);
	}
	// Server Event Callback. Basically a wrapper becuase of the required Event Method parameters.
	public static void motdListAction(JSON response)
	{
		processMotdQueue(response.getJsonArray("motd"));
	}
	
	// Processed the passed in JSON list of sorted dialog keys.
	public static void processMotdQueue(JSON[] items)
	{
		sortedMOTDQueue = new List<string>();
		sortingOrder = new Dictionary<string, int>();

		if (ExperimentWrapper.SegmentedDynamicMOTD.isInExperiment && MOTDDialogDataDynamic.instance != null)
		{
			if (MOTDDialogDataDynamic.instance.keyName == null && Data.debugMode)
			{
				Debug.LogError("MOTDFramework::processMotdQueue - The keyname for the instance was null. EOS error?");
			}
			else
			{
				MOTDDialogData.checkForDynamicMotd();
				sortingOrder.Add(MOTDDialogDataDynamic.instance.keyName, MOTDDialogDataDynamic.instance.sortIndex);
				sortedMOTDQueue.Add(MOTDDialogDataDynamic.instance.keyName);
			}
		}

		foreach (JSON json in items)
		{
			string keyName = json.getString("key_name", "");
			int sortOrder = json.getInt("sort_order", -1);

			// MCC -- I was occasionaly seeing a double MOTD view, I believe this was becuase we get the new MOTD list
			// before we actually show the MOTD if that MOTD was bundled (as there is now a delay). The system wasn't designed
			// with that in mind, and we cannot guarantee the order of server action/events. So as soon as we start the process
			// to show a dialog, mark it as "seen" on the client, and never allow that to be seen again this session.
			if (!string.IsNullOrEmpty(keyName) &&
				(seenThisSession == null || !seenThisSession.Contains(keyName)))
			{
				if (sortedMOTDQueue.Contains(keyName))
				{
					// removing duplicate
					sortedMOTDQueue.Remove(keyName);
					sortingOrder.Remove(keyName);
				}
				
				sortingOrder.Add(keyName, sortOrder);
				sortedMOTDQueue.Add(keyName);
			}
		}

		// MCC -- Comment this line out when not testing the test MOTDs.
		//addTestDialogs();
		sortedMOTDQueue.Sort(motdQueueSorter);
	}

	private static void addTestDialogs()
	{
		string[] testKeys =
		{
			"test_timeout_motd"
		};
		
		for (int i = 0; i < testKeys.Length; i++)
		{
			// Add all test keys at 0 sort index for ease of testing.
			sortingOrder.Add(testKeys[i], 0);
			sortedMOTDQueue.Add(testKeys[i]);
		}
	}

	// Public method for spawning an motd from the new system.
	// Takes in the surfacing location where we are trying to spawn the dialog.
	public static void showGlobalMOTD(SURFACE_POINT loc)
	{
		if (!shouldShowStartupDialogs)
		{
			// If we dont meet the base requirements for MOTDs showing, then return automatically here.
			return;
		}
		lastSurfacePoint = loc;
		List<string> keysToRemove = new List<string>();
		for (int i = 0; i < sortedMOTDQueue.Count; i++)
		{
			if (getRemainingDialogs(loc) <= 0)
			{
				Debug.Log("hit the max for this surfacing location");
				break;
			}
			MOTDDialogData data = MOTDDialogData.find(sortedMOTDQueue[i]);
		
			if (data != null && shouldShowDialog(data, loc) && !seenThisSession.Contains(sortedMOTDQueue[i]))
			{
				seenThisSession.Add(sortedMOTDQueue[i]);
				keysToRemove.Add(sortedMOTDQueue[i]);
				addMotdToShowQueue(sortedMOTDQueue[i]);
#if !ZYNGA_PRODUCTION
				// If this is not a production build, maintain a list of passed over MOTDs
				if (passedOverThisSession.ContainsKey(sortedMOTDQueue[i]))
				{
					passedOverThisSession.Remove(sortedMOTDQueue[i]);
				}

#endif
				incrementLocationCount(loc);
			}
#if !ZYNGA_PRODUCTION
			else
			{
				// If this is not a production build, maintain a list of passed over MOTDs
				if (!passedOverThisSession.ContainsKey(sortedMOTDQueue[i]) && !seenThisSession.Contains(sortedMOTDQueue[i]))
				{
					passedOverThisSession.Add(sortedMOTDQueue[i], data.noShowReason);
				}

			}
#endif
		}

		for (int i = 0; i < keysToRemove.Count; i++)
		{
			sortedMOTDQueue.Remove(keysToRemove[i]);
		}

		PlayerAction.getNewMotdList();
	}

	public static void clearAllSeenDialogs()
	{
		foreach (string key in MOTDDialogData.motdKeys)
		{
			PlayerAction.markMotdSeen(key, false);
		}
	}

	public static void checkIfQueueIsStuck()
	{
		if (lastMotdPopTime != 0f)
		{
			// Make sure this doesnt return true before we have even tried to show anything.
			int timeSinceLastPop = GameTimer.currentTime - lastMotdPopTime;
			if (isTryingToShowMotd &&
				!Scheduler.hasTaskWith(lastMotdPoppedKey) &&
				timeSinceLastPop > motdTimeoutDuration)
			{
				Debug.LogErrorFormat("MOTDFramework.cs -- checkIfQueueIsStuck -- queue is stuck, popping next one.");
				/* 
				If we are trying to show an MOTD, are back in the main lobby,
				and the time since we last tried to show it has exceeded our timeout, 
				try to pop another MOTD.
				*/
				handledFailedMOTD(lastMotdPoppedKey);
			}
		}
	}
	
	private static void addMotdToShowQueue(string key)
	{
		motdToShowQueue.Enqueue(key);
		popMotdFromToShowQueue();
	}

	public static void popMotdFromToShowQueue()
	{
		if (!isTryingToShowMotd && motdToShowQueue != null)
		{
			if (motdToShowQueue.Count > 0)
			{
				// If we are not currently trying to show an motd, and there are still motds to show,
				// then try to show one now, and pop it off the queue.
				string key = motdToShowQueue.Dequeue();
				showMOTD(key);
			}
			else
			{
				// Since some MOTD's in the motdToShowQueue may have failed to show,
				// give others in the sorted list a chance since the location counts have been adjusted to account for this.
				// This will add to motdToShowQueue if the location counts allow it and call popMotdFromToShowQueue again.
				showGlobalMOTD(lastSurfacePoint);
			}
		}
	}

	public static bool shouldShowStartupDialogs
	{
		get
		{
			return
				// If adding conditions to this, also add them to noShowStartupDialogsReason.
#if UNITY_EDITOR
				// Editor only, a login setting allows us to suppress the startup dialogs
				// to streamline continuous testing of a feature without being slowed down each time we launch.
				(PlayerPrefsCache.GetInt(DebugPrefs.SHOW_STARTUP_MOTDS, 1) == 1) &&
#endif
				// If this is the user's first time in the game, don't spawn any MOTDs (or similar dialogs).
				(GameExperience.totalSpinCount > 0) &&

				//don't show motds until the user has completed ftue steps
				(!EUEManager.isEnabled || (
					(!EUEManager.shouldDisplayFirstLoadOverlay &&
					 !EUEManager.shouldDisplayBonusCollect &&
					 !EUEManager.shouldDisplayGameIntro)));
		}
	}

	public static string noShowStartupDialogsReason
	{
		get
		{
			string reason = "";
#if UNITY_EDITOR
			if (PlayerPrefsCache.GetInt(DebugPrefs.SHOW_STARTUP_MOTDS, 1) != 1)
			{
				reason += "MOTD's are disabled in the editor.\n";
			}
#endif
			if (GameExperience.totalSpinCount == 0)
			{
				reason += "No spins yet - startup dialogs suppressed.\n";
			}
			return reason;
		}
	}

	public static bool isValidWithSeenList(string[] segregated)
	{
		// If we have some MOTDs that have shown this session, make sure none of them invalidate showing this one.		
		if (seenThisSession != null && seenThisSession.Count > 0)
		{
			for (int i = 0; i < segregated.Length; i++)
			{
				if (seenThisSession.Contains(segregated[i]))
				{
					return false;
				}
			}
		}
		return true;
	}

	public static string findNoShowReason(string motdKey)
	{
		if (string.IsNullOrEmpty(motdKey))
		{
			// Bail out if the motdkey is null
			return "";
		}
		string result = "";
		if (blacklist != null && blacklist.Contains(motdKey))
		{
			result += "Key is blacklisted.\n";
		}
		if (!sortingOrder.ContainsKey(motdKey))
		{
			result += "Data did not come down from the server for this key. \nYou should check the cooldown and platform specific settings in SCAT->UI->MOTDs.\n";
		}
		return result;
	}
	
	// Enumerator for the different surfacing points for MOTD Dialogs.
	public enum SURFACE_POINT
	{
		RTL,
		APP_ENTRY,
		VIP
	}
	
	// Sorting function for the MOTD list.
	private static int motdQueueSorter(string one, string two)
	{
		return sortingOrder[one].CompareTo(sortingOrder[two]);
	}
	
	public static void showMOTD(string key)
	{
		MOTDDialogData data = MOTDDialogData.find(key);

		if (data == null)
		{
		// If we failed to find this data, pop the next one.
			popMotdFromToShowQueue();
			return;
		}
		isTryingToShowMotd = true;
		lastMotdPoppedKey = key;
		lastMotdPopTime = GameTimer.currentTime;

		showCallWasMade = false;

		data.show();

		// wait to see if the dialog actually gets added to the todo list with addDialog or showDialogAfterDownloadingTextures
		// if not we need to unblock by setting isTryingToShowMotd to false and popping another motd
		RoutineRunner.instance.StartCoroutine(waitForMOTDShowcall(key));
	}

	// called when addDialog or showDialogAfterDownloadingTextures gets called so we can verify right away the dialog didn't bail out in the showDialog call and thus block the queue
	public static void notifyOnShow(Dict args)
	{
		if (args != null && lastMotdPoppedKey.Equals((string)args.getWithDefault(D.MOTD_KEY, "")))
		{
			showCallWasMade = true;
		}
	}

	// wait for 1 second in case we have a dialog that is waiting for event data or something
	// if it takes longer we will pop another dialog so we don't block too long
	// if the data does eventually show up the dialog we where waiting on will still pop, just not in exact order desired
	// that's what it gets for lagging
	private static IEnumerator waitForMOTDShowcall(string key)
	{
		float timePassed = 0.0f;
		float timeLimit = Data.liveData.getFloat("MOTD_SHOW_ATTEMPT_TIMEOUT", 1.0f);
		while (timePassed < timeLimit)
		{
			if (showCallWasMade)
			{
				yield break;
			}
			yield return null;
			timePassed += Time.deltaTime;
		}

		if (!showCallWasMade)
		{
#if !ZYNGA_PRODUCTION
			Debug.LogError(lastMotdPoppedKey + " was blocking the popMotdFromToShowQueue because it didn't show the dialog on its showDialog call, you should consider fixing shouldShow for this motd");
			// If this is not a production build, maintain a list of non showing MOTDs
			noShowList.Add(key);
#endif
			handledFailedMOTD(key);
		}
	}

	private static void handledFailedMOTD(string key)
	{
		MOTDDialogData data = MOTDDialogData.find(key);

		// fix up the show counts since we never showed this one and give others in the sorted list a chance
		if (data != null)
		{
			if (data.shouldShowRTL && lastSurfacePoint == SURFACE_POINT.RTL)
			{
				decrementLocationCount(SURFACE_POINT.RTL);
			}
			if (data.shouldShowVip && lastSurfacePoint == SURFACE_POINT.VIP)
			{
				decrementLocationCount(SURFACE_POINT.VIP);
			}
			if (data.shouldShowAppEntry && lastSurfacePoint == SURFACE_POINT.APP_ENTRY)
			{
				decrementLocationCount(SURFACE_POINT.APP_ENTRY);
			}
		}

		isTryingToShowMotd = false;
		popMotdFromToShowQueue();
	}
	
	// Boolean method to 
	private static bool shouldShowDialog(MOTDDialogData data, SURFACE_POINT loc)
	{
		if (data == null)
		{
			Debug.LogError("data is null");
			return false;
		}
		
		// Check the blacklist from livedata, which allows us to disable any troublesome MOTD without a SCAT data push.
		if (blacklist.IndexOf(data.keyName) > -1)
		{
			return false;
		}

		if (data.keyName == null)
		{
			return false;
		}

		if (!data.shouldShow)
		{
			return false;
		}
		
		switch (loc)
		{
			case SURFACE_POINT.RTL:
				return data.shouldShowRTL;
			case SURFACE_POINT.VIP:
				return data.shouldShowVip;
			case SURFACE_POINT.APP_ENTRY:
				return data.shouldShowAppEntry;
			default:
				return false;
		}
	}
	
	// Returns the number of dialogs that can be spanwed at the specified location.
	private static int getRemainingDialogs(SURFACE_POINT loc)
	{
		switch (loc)
		{
			case SURFACE_POINT.RTL:
				return Mathf.Min((limitSessionRtl - currentRtlCount), limitPerRtl);
			case SURFACE_POINT.VIP:
				return Mathf.Min((limitSessionVip - currentVipCount), limitPerVip);
			case SURFACE_POINT.APP_ENTRY:
				return limitSessionAppEntry - currentAppEntryCount;
			default:
				return 0;
		}
	}
	
	// Convenience function for handling the incrementing of the spawn dialog
	// counters specific to each location, rather than having a switch statement everywhere.
	private static void incrementLocationCount(SURFACE_POINT loc)
	{
		switch (loc)
		{
			case SURFACE_POINT.RTL:
				currentRtlCount++;
				break;
			case SURFACE_POINT.VIP:
				currentVipCount++;
				break;
			case SURFACE_POINT.APP_ENTRY:
				currentAppEntryCount++;
				break;
		}
	}

	// Convenience function for handling the decrementing of the spawn dialog
	// counters specific to each location, rather than having a switch statement everywhere.
	private static void decrementLocationCount(SURFACE_POINT loc)
	{
		switch (loc)
		{
			case SURFACE_POINT.RTL:
				if (currentRtlCount > 0)
				{
					currentRtlCount--;
				}
				break;
			case SURFACE_POINT.VIP:
				if (currentVipCount > 0)
				{
					currentVipCount--;
				}
				break;
			case SURFACE_POINT.APP_ENTRY:
			if (currentAppEntryCount > 0)
				{
					currentAppEntryCount--;
				}
				break;
		}
	}
	
	// Queues up a call to action for either launching a game or going to the VIP room from the lobby.
	// This could be called multiple times when there are multiple MOTD's that allow it,
	// but we only honor the latest one.
	public static void queueCallToAction(string key, Dict args = null)
	{
		callToActionGameKey = key;
		queueCallToActionFunction(args);
	}

	public static void queueCallToActionFunction(Dict args = null)
	{	
		if (!Scheduler.hasTaskWith<SchedulerDelegate>(callToAction))
		{
			// Only add it if it's not already in the queue,
			// since we only want to trigger it once, no matter
			// how many call to action buttons were touched on MOTD's.
			Scheduler.addFunction(callToAction, args, SchedulerPriority.PriorityType.IMMEDIATE);
		}
	}

	public static bool isSubLobbyAction(string actionKey)
	{
		return actionKey == VIP_ROOM_CALL_TO_ACTION ||
			actionKey == LOZ_LOBBY_CALL_TO_ACTION ||
			actionKey == MAX_VOLTAGE_LOBBY_CALL_TO_ACTION ||
			actionKey == SLOTVENTURE_LOBBY_CALL_TO_ACTION ||
			actionKey == CHALLENGE_LOBBY_CALL_TO_ACTION;
	}

	// Do the last MOTD call to action touched on the stack of MOTD's.
	// We only do the last one instead of each one that may have been touched,
	// to avoid conflicting navigation, and we only do this when nothing is happening,
	// so all the MOTD's are closed before doing the call to action.
	// This is called by the Scheduler, which requires the Dict args.
	public static void callToAction(Dict args)
	{
		Scheduler.removeFunction(callToAction);

		if (MainLobby.instance == null)
		{
			// These calls should only happen on startup MOTD's,
			// which relies on the lobby to be loaded.
			return;
		}

		if (MainLobby.instance != null && isSubLobbyAction(callToActionGameKey))
		{
			if (callToActionGameKey == VIP_ROOM_CALL_TO_ACTION)
			{
				//MainLobby.instance.StartCoroutine(MainLobby.instance.transitionToVIPLobby());
				MainLobby.hirV3.transitionToLobby(LobbyLoader.instance.createVIPLobby, LobbyInfo.Type.VIP.ToString() + "-lobby", "SelectPremiumAction");
			}
			else if (callToActionGameKey == LOZ_LOBBY_CALL_TO_ACTION)
			{
				if (shouldResetAndLoad(LOZLobby.isBeingLazilyLoaded, new List<string>() { LOZLobby.BUNDLE }, LOZLobby.onReload, callToActionGameKey))
				{
					return;
				}

				RoutineRunner.instance.StartCoroutine(MainLobby.instance.transitionToLOZLobby());
			}
			else if (callToActionGameKey == MAX_VOLTAGE_LOBBY_CALL_TO_ACTION)
			{
				if (shouldResetAndLoad(MaxVoltageLobbyHIR.isBeingLazilyLoaded, new List<string>() { MaxVoltageLobbyHIR.BUNDLE_NAME }, MaxVoltageLobbyHIR.onReload, callToActionGameKey))
				{
					return;
				}

				MainLobby.instance.StartCoroutine(MainLobby.instance.transitionToMaxVoltageLobby());
			}
			else if (callToActionGameKey == SLOTVENTURE_LOBBY_CALL_TO_ACTION)
			{
				if (shouldResetAndLoad(SlotventuresLobby.isBeingLazilyLoaded, new List<string>() { SlotventuresLobby.COMMON_BUNDLE_NAME, SlotventuresLobby.THEMED_BUNDLE_NAME, SlotventuresLobby.COMMON_BUNDLE_NAME_SOUNDS }, SlotventuresLobby.onReload, callToActionGameKey))
				{
					return;
				}

				MainLobby.instance.StartCoroutine(MainLobby.instance.transitionToSlotventureLobby());
			}
			else if (callToActionGameKey == CHALLENGE_LOBBY_CALL_TO_ACTION)
			{
				LobbyLoader.onLobbyLoad actionToUse = null;
				if (args.containsKey(D.ANSWER))
				{
					actionToUse = (LobbyLoader.onLobbyLoad)args[D.ANSWER];
				}

				if (shouldResetAndLoad((bool)args.getWithDefault(D.IS_WAITING, false), new List<string>() { (string)args.getWithDefault(D.DATA, "") }, actionToUse, callToActionGameKey))
				{
					return;
				}

				MainLobby.instance.StartCoroutine(MainLobby.instance.transitionToChallengeLobby((string)args.getWithDefault(D.CAMPAIGN_NAME, "")));
			}
		}
		else if (callToActionGameKey != "")
		{
			LobbyGame game = LobbyGame.find(callToActionGameKey);
			if (game != null)
			{
				SlotAction.setLaunchDetails("motd");
				// This shouldn't be null since validation on that was already done
				// before setting the string variable, but checking just in case.
				game.askInitialBetOrTryLaunch();
			}
		}

		callToActionGameKey = "";
	}

	private static bool shouldResetAndLoad(bool isBeingLazyLoaded, List<string> bundles, LobbyLoader.onLobbyLoad action, string actionName)
	{
		if (isBeingLazyLoaded)
		{
			Glb.resetGameAndLoadBundles(string.Format("Load {0} now", actionName), bundles, action);
			return true;
		}

		return false; 
	}

	public static void markMotdSeen(Dict args)
	{
		isTryingToShowMotd = false; // Mark this false now that we have shown that MOTD.
		string key = (string)args.getWithDefault(D.MOTD_KEY, "");

		MOTDDialogData data = MOTDDialogData.find(key);
		
		if (data != null)
		{
			data.markSeen();
		}
	}

	// To avoid MOTD dialogs that were not setup properly blocking the system
	// (ie. they forgot to mark themselves as seen) the Scheduler just tells us when it is
	// trying to launch a dialog. We can then check that dialog for an motd key, and if it has one
	// we know it is going to get shown shortly and can thus start the next download.
	public static void alertToDialogShowCall(Dict args)
	{
		if (args != null)
		{
			string motdKey = (string)args.getWithDefault(D.MOTD_KEY, "notyet");
			if (!string.IsNullOrEmpty(motdKey) && motdKey != "notyet")
			{
				isTryingToShowMotd = false;
				popMotdFromToShowQueue();
			}
		}
	}
	
	public static void resetStaticClassData()
	{
		// Ensure that these reset when the user does a re-login
		currentAppEntryCount = 0;
		currentRtlCount = 0;
		currentVipCount = 0;
		currentSaleCount = 0;
		limitSessionRtl = 0;
		limitSessionVip = 0;
		limitSessionAppEntry = 0;
		limitPerRtl = 0;
		limitPerVip = 0;
		limitSessionSale = 0;
		blacklist = new List<string>();
		callToActionGameKey = "";
		seenThisSession = new HashSet<string>();

#if !ZYNGA_PRODUCTION
		noShowList = new List<string>();
		passedOverThisSession = new Dictionary<string, string>();
#endif

		motdToShowQueue = new Queue<string>();
		isTryingToShowMotd = false;
		lastMotdPopTime = 0;
		lastMotdPoppedKey = "";
		motdTimeoutDuration = 30f;		
	}
}
