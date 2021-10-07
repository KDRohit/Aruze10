using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Advertisements;
using Com.HitItRich.IDFA;

public class WatchToEarn : MonoBehaviour
{
	// Rewarded Ad object 
	//private static RewardedAd rewarded;

	// Watch To Earn variables from the server used for display logic.
	public static bool isServerEnabled = false; // Whether Watch to Earn is enabled from the HIR server.
	public static bool isAdAvailable          // Whether ad provider has any ads available.
	{
		get
		{
#if UNITY_ADS
			return UnityAdsManager.isAdAvailable(currentPlacementId);
#else
			return false;
#endif
		}
	}

	public static bool watchToEarnClick = false; // Check to see if the watch to earn button is clicked
	public static long rewardAmount = 0; // The amount of credits (coins) that the user will receive after watching an ad.
	public static bool motdSeen = false; // The number of times the user has seen the watch to earn motd.
	public static int inventory = 0; // The number of "inventory" that the user has left from the server. This is the maximum number of times left in this time period that they can watch and collect credits for watching an ad.
	public static UnityAdsManager.PlacementId currentPlacementId = UnityAdsManager.PlacementId.VIDEO;
	public static bool isInitialized // Whether ad provider was properly initialized at startup.
	{
		get
		{
#if UNITY_ADS
			return UnityAdsManager.instance != null && UnityAdsManager.instance.isInitialized;
#else
			//we don't support any other ad providers at this time
			return false;
#endif
		}
	}

	public static bool shouldServerLog = false;
	public static float samplingThreshold = 0.0f;
	public static string lastKnownSrc = "";

	public static event System.Action adInitialized;

	private static bool isWaitingForCollectDialog = false;
	private static bool _isAdAvailable = false;
	private const float WELCOME_DIALOG_SPAWN_DELAY = 2.0f; // Amount of time to wait before spawning the Welcome Back dialog.
	private const float COINS_COMING_DIALOG_SPAWN_DELAY = 10.0f; // Amount of time to wait before spawning the your coins are coming dialog
	private static int retriesRemaining = 3; // Number of retries allowed for failed ads.

	
	
	// Public method to initialize all the server agencies that we use to populate ads.
	// WatchToEarn is the common class that reads them so putting this initialization function in here.
	public static void initializeAdAgencies()
	{
		float randomSamplingNumber = UnityEngine.Random.Range(0, 1.0f);
		shouldServerLog = (randomSamplingNumber > samplingThreshold);

		Server.unregisterEventDelegate("w2e_reward_grant");
		Server.registerEventDelegate("w2e_reward_grant", collectCredits);
	}

	//Function to log stats 
	public static void logStat(bool isFailure = false)
	{
		StatsManager.Instance.LogCount(
			"watch_to_earn",
			string.Format("initialized_{0}", (isInitialized ? "yes" : "no")),
			string.Format("inventory_{0}", (isAdAvailable ? "yes" : "no")),
			string.Format("server_inventory_{0}", inventory),
			string.Format("ad_provider_{0}", "unity_ads"),
			string.Format("is_failure_{0}", isFailure)
		);
	}

	// Check to see if the feature is enabled
	public static bool isEnabled
	{
		get
		{
#if ZYNGA_KINDLE
			return false;
#else

			if (isServerEnabled &&
					isInitialized && 
#if UNITY_ADS
					ExperimentWrapper.WatchToEarn.useUnityAds &&
					UnityAdsManager.instance != null && 
					!string.IsNullOrEmpty(UnityAdsManager.instance.gameId) &&
#endif
					isAdAvailable 
#if !UNITY_EDITOR
					&& (inventory > 0)
#endif
					)
			{
				return true;
			}
			else
			{
				return false;
			}
#endif
		}
	}

	public static string getNotAvailableReason()
	{
		StringBuilder sb = new StringBuilder();
		if (!isServerEnabled)
		{
			sb.AppendLine("Server is not enabled.");
		}

		if (!isInitialized)
		{
			sb.AppendLine("Not initialized");
		}
#if UNITY_ADS
		if (!ExperimentWrapper.WatchToEarn.useUnityAds)
		{
			sb.AppendLine("Unity ads not turned on in experiment");
		}

		if (UnityAdsManager.instance == null)
		{
			sb.AppendLine("Unity ad manager not initialized");
		}
		else if (string.IsNullOrEmpty(UnityAdsManager.instance.gameId))
		{
			sb.AppendLine("Game id is not valid");
		}
#endif

		if (!isAdAvailable)
		{
			sb.AppendLine("No ad available");
		}
		
#if !UNITY_EDITOR
		if (inventory <= 0)
		{
			sb.AppendLine("No inventory");
		}
#endif
		return sb.ToString();
	}

	//Function logs events to splunk
	private static void logSplunk(string message)
	{
		Dictionary<string, string> extraFields = new Dictionary<string, string>();
		extraFields.Add("error", message);
		string eventType =
#if UNITY_ADS
			"UnityAds"
#else
			"NoAdProvider"
#endif
		;
		SplunkEventManager.createSplunkEvent(eventType, "watchtoearn-logs", extraFields);

		w2eLog(message);
	}

	private static void w2eLog(string message)
	{
		if (Data.debugMode)
		{
			DevGUIMenuAdServices.w2eLog(message);
		}
	}

	//Function to lod the ad 3 times after the ad has failed to load
	private static void retryAdRequest ()
	{
		if (retriesRemaining == 0 || !isAdAvailable)
		{
			logSplunk("Failed to load the ad 3 times");
		}  
		else 
		{
			//attempt again with same parameters, (don't play audio)
			watchVideo(lastKnownSrc, false, currentPlacementId);
			retriesRemaining--;
		}
	}

	private static void handleAdDisplayed(object sender, System.EventArgs args)
	{
		logSplunk("OnAdDisplayed");
	}

	private static void handleUnityAdResult(object sender, UnityAdsManager.UnityAdEventArgs args)
	{
		restoreGameAudioAfterAd();
		switch(args.result)
		{
			case ShowResult.Failed:
				logSplunk("HandleAdFailedToDisplay");
				retryAdRequest();
				break;

			case ShowResult.Finished:
				{
					logSplunk("HandleAdDismissed with credit");
					markVideoWatched();
					updateSurfacing();
				}
				break;

			case ShowResult.Skipped:
				logSplunk("HandleAdSkipped");
				break;
		}
			
	}

	private static void muteGameAudioDuringAd()
	{
		Audio.tempMuted = true;
	}

	private static void restoreGameAudioAfterAd()
	{
		Audio.tempMuted = false;
	}

	private static void updateSurfacing()
	{
		w2eLog("updateSurfacing");
		LobbyCarousel.checkWatchToEarn();
		if (adInitialized != null)
		{
			adInitialized();
		}
	}

	private static void displayVideo(string src = "", bool playAudio = false, UnityAdsManager.PlacementId placementId = UnityAdsManager.PlacementId.VIDEO)
	{
		currentPlacementId = placementId;
		if (!isEnabled)
		{
			return;
		}

		lastKnownSrc = src;     // so collect dialog knows how to track the stat correctly

		if (playAudio)
		{
			Audio.play("W2EWatchMoreButton");
		}

		// Leaving this log here so that we can have some feedback in editor, as UnityAds only runs on mobile.
		Debug.Log("Spawning Ads Video from: " + src);
		w2eLog("Starting w2e video from : " + src);

#if UNITY_ADS
		if (isAdAvailable)
		{
			watchToEarnClick = true;
			//rewarded.DidPrompt();
			muteGameAudioDuringAd();
			UnityAdsManager.showRewardVideo(currentPlacementId, handleAdDisplayed, handleUnityAdResult);
		}
		else
		{
			watchToEarnClick = false;
			//rewarded.DidNotOfferAd();
			LobbyCarousel.checkWatchToEarn();
		}

		if (MainLobbyBottomOverlay.instance != null)
		{
			// Make sure that we update whether watch to earn is showing in the overlay.
			MainLobbyBottomOverlay.instance.setUpdateFlag();
		}
#endif
		//if we're valid log that the video has started
		if (watchToEarnClick)
		{
			WatchToEarnAction.statsCall("started", currentPlacementId); // Tell the server that we are starting the video.	
		}
	}

	public static void watchVideoClickHandler(string kingdom, string gameKey, string statSrc)
	{
		// stat tracking
		StatsManager.Instance.LogCount("dialog", kingdom, "", gameKey, "w2e", "click");

		watchVideo(statSrc, true);
	}

	public static void watchVideo(string src = "", bool playAudio = false, UnityAdsManager.PlacementId placementId = UnityAdsManager.PlacementId.VIDEO)
	{
#if (UNITY_IPHONE || UNITY_IOS) && !UNITY_EDITOR
// Display IDFA prompt(s) if necessary
  		IDFASoftPromptManager.displayIDFADialog(IDFASoftPromptManager.SurfacePoint.W2E, () => { displayVideo(src, playAudio, placementId);});
 #else
		displayVideo(src, playAudio, placementId);
#endif
	}

	public static void markVideoWatched()
	{
		Bugsnag.LeaveBreadcrumb("WatchToEarn markVideoWatched");
		// Send server action
		inventory--;
		Server.unregisterEventDelegate("w2e_reward_grant");
		Server.registerEventDelegate("w2e_reward_grant", collectCredits);
		WatchToEarnAction.statsCall("completed", currentPlacementId); // Tell the server that the video was successfully finished.
		w2eLog("Video marked as watched");

		RoutineRunner.instance.StartCoroutine(waitForCollectDialog());

	}

	// waits to see if collect dialog shows within COINS_COMING_DIALOG_SPAWN_DELAY, if not show coins coming dialog
	public static IEnumerator waitForCollectDialog()
	{
		w2eLog("waiting for collect dialog");

		isWaitingForCollectDialog = true;

		if (Data.debugMode && DevGUIMenuAdServices.useOneSecondCoinsComingDelay)
		{
			// so we can test on device if supersonic is responding quicly.
			yield return new WaitForSeconds(1.0f);
		}
		else
		{
			yield return new WaitForSeconds(COINS_COMING_DIALOG_SPAWN_DELAY);
		}

		// are we still waiting for the collect dialog? Then show coins are coming dialog
		if (isWaitingForCollectDialog)
		{
			w2eLog("wait time exceeded showing coins will be coming dialog");
			WatchToEarnNotifyDialog.showThanks();
		}

		isWaitingForCollectDialog = false;

		updateSurfacing();
	}

	public static bool initWatchToEarnUI(GameObject watchToEarnButton, GameObject collectButton, TextMeshPro watchToEarnLabel, bool shouldShow, UnityAdsManager.PlacementId placementRewardId)
	{
		if (watchToEarnButton != null &&
			isEnabled &&
			shouldShow)
		{
			currentPlacementId = placementRewardId;
			SafeSet.gameObjectActive(watchToEarnButton, true);
			SafeSet.labelText(watchToEarnLabel, Localize.text("w2e_get_coins", CreditsEconomy.multiplyAndFormatNumberAbbreviated(WatchToEarn.rewardAmount)));
			return true;
		}
		else
		{
			SafeSet.gameObjectActive(watchToEarnButton, false);

			// center the collect button since it is all that remains
			if (collectButton != null)
			{
				CommonTransform.setX(collectButton.transform, 0.0f);
			}
			return false;
		}
	}

	public static void collectCredits(JSON data)
	{
		string eventId = data.getString("event", "");
		long creditAmount = data.getLong("creditAmount", 0L);


		isWaitingForCollectDialog = false;      // will stop coins are coming dialog if this happens in time

		w2eLog("collect credits received " + creditAmount.ToString());

#if UNITY_ADS
		// Grant credits to the player.
		if (creditAmount > 0L)
		{
			WatchToEarnCollectDialog.showDialog(creditAmount, eventId);
		}
#else
		Debug.LogError("collectCredits - UnityAds not enabled for this build!");
#endif
	}
}
