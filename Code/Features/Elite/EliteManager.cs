using UnityEngine;
using System.Collections;
using Com.Rewardables;
using Com.Scheduler;

/// <summary>
/// Manages elite pass durations, points, etc
/// </summary>
public class EliteManager : IResetGame
{
	// =============================
	// PUBLIC
	// =============================
	/// <summary>
	/// Elite pass points accumulated
	/// </summary>
	public static int points { get; private set; }
	
	/// <summary>
	/// Elite pass points needed to acquire the pass
	/// </summary>
	public static int targetPoints { get; private set; }

	/// <summary>
	/// Date timestamp the pass expires
	/// </summary>
	public static int passExpiration { get; private set; }

	/// <summary>
	/// How long the pass lasts (typically 7 days)
	/// </summary>
	public static int passDuration { get; private set; }

	/// <summary>
	/// Current number of passes
	/// </summary>
	public static int passes { get; private set; }

	/// <summary>
	/// Number of spin rewards the player has collected
	/// </summary>
	public static int spinRewards { get; private set; }

	/// <summary>
	/// Number of spins a player has made toward a spin reward
	/// </summary>
	public static int spinsTowardReward { get; private set; }

	/// <summary>
	/// Number of spins needed to get a spin reward
	/// </summary>
	public static int spinRewardThreshold { get; private set; }

	/// <summary>
	/// Maximum number of spin rewards a user can get in a day
	/// </summary>
	public static int maxSpinRewards { get; private set; }

	/// <summary>
	/// Running count of number of spins in a day
	/// </summary>
	public static int dailySpinCount { get; private set; }

	/// <summary>
	/// Qualifying wager for elite points
	/// </summary>
	public static long minQualifyingWager { get; private set; }

	/// <summary>
	/// Max qualifying wager for elite points
	/// </summary>
	public static long maxQualifyingWager { get; private set; }
	
	/// <summary>
	/// Elite points granted for every dollar
	/// </summary>
	public static int elitePointsPerDollar { get; private set; }

	/// <summary>
	/// Daily rollover timer
	/// </summary>
	public static GameTimerRange rolloverTimer { get; private set; }

	/// <summary>
	/// Boolean used to display the elite unlocking animation when the elite dialog
	/// is opened for the first time after unlocking
	/// </summary>
	public static bool showEliteUnlocked = false;

	/// <summary>
	/// time in seconds after midnight pst to rollover elite pass
	/// </summary>
	public static int rolloverTime = 0;

	/// <summary>
	/// Pass expiration timer
	/// </summary>
	public static GameTimerRange expirationTimer { get; private set; }
	
	/// <summary>
	/// Returns true when elite pass was previously inactive, and the user just received one
	/// </summary>
	public static bool showLobbyTransition { get; private set; }
	
	/// <summary>
	/// Event for handling elite points granted
	/// </summary>
	public static event System.Action onElitePointsGranted;

	private static bool debugForceActive;

	/// <summary>
	/// Eliete Member Got Gold Pass From Upgrade
	/// </summary>
	public static bool hasGoldFromUpgrade { get; private set; }
	
	// =============================
	// PRIVATE
	// =============================
	/// <summary>
	/// Event for handling elite pass activation
	/// </summary>
	private static event GenericDelegate onElitePassActive;
	
	
	// =============================
	// CONST
	// =============================
	public const string ELITE_PROGRESS_EVENT = "elite_pass_spin_progress";
	public const string ELITE_SHOW_THANK_GOLD = "rich_pass_for_elite_dialog";
	public const string ELITE_FIRST_ACCESS_ENDED = "elite_first_pass_ended";
	public const string ELITE_PASS_ROLLOVER_TIME_PST = "ELITE_PASS_ROLLOVER_TIME_PST";
	public const string ELITE_LOBBY_MUSIC = "LobbyTuneElite";
	public const string ELITE_STINGER = "StingerElite";
	public const string TRANSITION_BUNDLE_NAME = "features/elite/elite_lobby_transition";
	public const string FEATURE_BUNDLE_NAME = "elite";

	/*=========================================================================================
	SETUP
	=========================================================================================*/
	public static void init(JSON data)
	{
		registerEvents();

		points = data.getInt("points", 0);
		targetPoints = data.getInt("visible_total_points", 0);
		passExpiration = data.getInt("pass", 0);
		passDuration = data.getInt("duration", 0);
		spinRewards = data.getInt("spin_rewards", 0);
		spinsTowardReward = data.getInt("spins", 0);
		spinRewardThreshold = data.getInt("spin_reward_threshold", 0);
		maxSpinRewards = data.getInt("max_spin_rewards", 0);
		minQualifyingWager = data.getLong("min_qualifying_bet", 0L);
		maxQualifyingWager = data.getLong("max_qualifying_bet", 0L);
		elitePointsPerDollar = data.getInt("elite_points_per_dollar", 0);
		passes = data.getInt("num_passes", 0);
		dailySpinCount = data.getInt("daily_spin_count", 0);
		
		//Live data value is the number of seconds from midnight Pacific Time
		rolloverTime = Data.liveData.getInt(ELITE_PASS_ROLLOVER_TIME_PST, 0);
		//Convert to the UTC time taking daylight savings into account
		if (CommonText.isPacificDaylightSavingsTime())
		{
			rolloverTime += 7 * Common.SECONDS_PER_HOUR; // 7 hrs in seconds
		}
		else
		{
			rolloverTime += 8 * Common.SECONDS_PER_HOUR; // 8 hrs in seconds
		}
		createTimer();
		createRolloverTimer();

		if (hasActivePass)
		{
			dispatchEvent();
		}
	}

	/*=========================================================================================
	EVENT HANDLING
	=========================================================================================*/
	private static void registerEvents()
	{
		if (!isLevelLocked)
		{
			RewardablesManager.addEventHandler(onRewardGranted);
			Server.registerEventDelegate(ELITE_SHOW_THANK_GOLD,onShowThankYouGold  );
			Server.registerEventDelegate(ELITE_PROGRESS_EVENT, onProgress, true);
			Server.registerEventDelegate(ELITE_FIRST_ACCESS_ENDED, onFirstAccessEnded, true);
		}
		else
		{
			EueFeatureUnlocks.instance.registerForGetInfoEvent("elite_pass", init);
			EueFeatureUnlocks.instance.registerForFeatureLoadEvent("elite_pass", loadBundles);
		}
	}

	private static void onShowThankYouGold(JSON prams)
	{
		hasGoldFromUpgrade = true;
	}
	

	private static void loadBundles()
	{
		if (!AssetBundleManager.isBundleCached(TRANSITION_BUNDLE_NAME))
		{
			AssetBundleManager.downloadAndCacheBundle(TRANSITION_BUNDLE_NAME);
		}

		if (!AssetBundleManager.isBundleCached(FEATURE_BUNDLE_NAME))
		{
			AssetBundleManager.downloadAndCacheBundle(FEATURE_BUNDLE_NAME);
		}
	}

	private static void unregisterEvents()
	{
		RewardablesManager.removeEventHandler(onRewardGranted);
		Server.unregisterEventDelegate(ELITE_SHOW_THANK_GOLD,onShowThankYouGold  );
		Server.unregisterEventDelegate(ELITE_PROGRESS_EVENT, onProgress, true);
		Server.unregisterEventDelegate(ELITE_FIRST_ACCESS_ENDED, onFirstAccessEnded, true);
	}

	/// <summary>
	/// Add handler to the event for when elite pass becomes active
	/// </summary>
	public static void addEventHandler(GenericDelegate onPassActive)
	{
		onElitePassActive -= onPassActive;
		onElitePassActive += onPassActive;
	}

	/// <summary>
	/// Remove handler for event for elite activation
	/// </summary>
	public static void removeEventHandler(GenericDelegate onPassActive)
	{
		onElitePassActive -= onPassActive;
	}

	/// <summary>
	/// Dispatches the event when elite becomes active
	/// </summary>
	private static void dispatchEvent()
	{
		if (onElitePassActive != null)
		{
			onElitePassActive();
		}
	}

	/// <summary>
	/// Dispatches event when points are granted
	/// </summary>
	private static void pointsGranted()
	{
		if (onElitePointsGranted != null)
		{
			onElitePointsGranted();
		}
	}

	/// <summary>
	/// Handles elite progress events
	/// </summary>
	private static void onProgress(JSON data)
	{
		spinRewards = data.getInt("spin_reward_count", 0);
		spinsTowardReward = data.getInt("spin_count", 0);
		dailySpinCount = data.getInt("daily_spin_count", 0);
	}
	
	/// <summary>
	/// Handles elite rewardables
	/// </summary>
	private static void onRewardGranted(Rewardable rewardable)
	{
		RewardElitePassPoints elitePassPoints = rewardable as RewardElitePassPoints;

		RewardElitePass elitePass = rewardable as RewardElitePass;
		
		RewardRichPass richPassEliteReward = rewardable as RewardRichPass;

		if (elitePassPoints != null)
		{
			points = elitePassPoints.totalPoints;
			pointsGranted();
		}

		if (elitePass != null)
		{
			bool passWasActive = hasActivePass;
			passDuration = elitePass.duration;
			passes = elitePass.newPassCount;
			passExpiration = elitePass.expiration;
			//Use the old pass count to determine lobby transition animation so that it plays correctly when 2 passes
			//are unlocked at once (can happen if the wager amount is huge)
			
			//Don't check the oldPassCount if we're set to show the transition but haven't shown it yet. Can happen if you
			//make enough back-to-back purchases to gain multiple passes before the transition has had a chance to play
			if (!showLobbyTransition)
			{
				showLobbyTransition = elitePass.oldPassCount == 0;
			}

			showEliteUnlocked = showLobbyTransition;
			
			createTimer();
			createRolloverTimer();
			//Call getInboxItems here so that by the time the transition completes and the Elite dialog opens
			//we have the latest elite items in the inbox.
			InboxAction.getInboxItems();
			
			EliteAccessState accessState = passWasActive ? EliteAccessState.SECOND_ACCESS : EliteAccessState.FIRST_ACCESS;
			Dict args = Dict.create(D.STATE, accessState);
			//HIR-84245 Workaround to add a little delay so any other incoming events like QFC can be processed and shown before this dialog
			RoutineRunner.instance.StartCoroutine(DelayEliteAccessDialog(args));

			StatsElite.logMilestone("elite_active", passes);
		}

		if (richPassEliteReward != null)
		{
			JSON data = richPassEliteReward.data;
			if (data != null)
			{
				string rewardType = data.getString("reward_type", "");
				if (rewardType == "elite_pass_points")
				{
					points = data.getInt("new_points", 0);
					pointsGranted();
				}
			}
		}

		if (elitePass != null || elitePassPoints != null || richPassEliteReward != null)
		{
			rewardable.consume();	
		}
	}

	private static void onFirstAccessEnded(JSON data)
	{
		points = data.getInt("points", 0);
		passes = data.getInt("num_passes", 0);
	}

	private static IEnumerator DelayEliteAccessDialog(Dict args)
	{
		yield return new WaitForSeconds(0.5f);
		EliteAccessDialog.showDialog(args, SchedulerPriority.PriorityType.LOW);
	}

	private static void onPassExpired(Dict args = null, GameTimerRange originalTimer = null)
	{
		showLobbyTransition = true;
		//Reset points only if the player has 2 or more passes when it expired in the same session.
		if (passes >= 2)
		{
			points = 0;
		}

		if (Dialog.instance.currentDialog != null && Dialog.instance.currentDialog.type.keyName == EliteDialog.DIALOG_KEY)
		{
			Dialog.close();
		}
		
		Dict dialogArgs = Dict.create(D.STATE, EliteAccessState.EXPIRED);
		EliteAccessDialog.showDialog(dialogArgs);
		StatsElite.logMilestone("elite_deactivated", passes);
	}

	/*=========================================================================================
    ANCILLARY
    =========================================================================================*/
	/// <summary>
	/// Returns true if the user has an active elite pass
	/// </summary>
	public static bool hasActivePass
	{
		get
		{
			return 
				(Debug.isDebugBuild && debugForceActive) ||
				(expirationTimer != null && !expirationTimer.isExpired);
		}
	}

	public static void debugToggleActive(bool status)
	{
#if !ZYNGA_PRODUCTION
		if (!Debug.isDebugBuild)
		{
			return;
		}
		
		debugForceActive = status;
#endif
	}

	/// <summary>
	/// Returns true if the feature is active
	/// </summary>
	public static bool isActive
	{
		get
		{
			return
				(Debug.isDebugBuild && debugForceActive) ||
				(ExperimentWrapper.ElitePass.isInExperiment && !isLevelLocked);
		}
	}

	public static bool isLevelLocked
	{
		get
		{
			return ExperimentWrapper.EUEFeatureUnlocks.isInExperiment &&
			       !EueFeatureUnlocks.isFeatureUnlocked("elite_pass");
		}
	}

	public static int timeRemainingInDays
	{
		get
		{
			int daysRemaining = Mathf.CeilToInt((float)expirationTimer.timeRemaining / Common.SECONDS_PER_DAY);
			int passDurationInDays = Mathf.CeilToInt((float)passDuration / Common.SECONDS_PER_DAY);
			return Mathf.Clamp(daysRemaining, 0, passDurationInDays * passes);
		}
	}

	/// <summary>
	/// Returns true if the wager is >= the qualifying amount for elite
	/// </summary>
	public static bool isQualifyingBet(long wager)
	{
		return wager >= minQualifyingWager;
	}

	/// <summary>
	/// Returns true when user has reached the maximum number of spins for the day towards their elite pass
	/// </summary>
	public static bool hasReachedSpinCap
	{
		get { return spinsTowardReward == spinRewardThreshold; }
	}

	/// <summary>
	/// For debugging/testing purposes only
	/// </summary>
	/// <param name="status"></param>
	public static void forceLobbyTransition(bool status)
	{
#if !ZYNGA_PRODUCTION
		showLobbyTransition = status;
#endif
	}

	public static void onLobbyTransitionComplete()
	{
		showLobbyTransition = false;
	}

	/// <summary>
	/// Creates the expiration timer if values are set accordingly
	/// </summary>
	private static void createTimer()
	{
		if (passExpiration > 0)
		{
			if (expirationTimer == null)
			{
				System.DateTime endTime = Common.convertFromUnixTimestampSeconds(passExpiration);
				System.DateTime startTime = Common.convertFromUnixTimestampSeconds(GameTimer.currentTime);
				expirationTimer = GameTimerRange.createWithTimeRemaining((int)(endTime - startTime).TotalSeconds - Data.liveData.getInt("CLIENT_TIMER_EXPIRATION_BUFFER", 0));
			}
			else
			{
				System.DateTime endTime = Common.convertFromUnixTimestampSeconds(passExpiration);
				System.DateTime startTime = Common.convertFromUnixTimestampSeconds(GameTimer.currentTime);
				expirationTimer.updateEndTime((int)(endTime - startTime).TotalSeconds - Data.liveData.getInt("CLIENT_TIMER_EXPIRATION_BUFFER", 0));
			}

			expirationTimer.registerFunction(onPassExpired);
		}
	}

	private static void createRolloverTimer()
	{
		System.DateTime current = Common.convertFromUnixTimestampSeconds(GameTimer.currentTime);
		System.DateTime today = current.Date;
		int secs = (int)current.TimeOfDay.TotalSeconds;
		
		if (secs <= rolloverTime)
		{
			rolloverTimer = GameTimerRange.createWithTimeRemaining(rolloverTime - secs);
		}
		else
		{
			System.DateTime tomorrow = today.AddDays(1).AddSeconds(rolloverTime);
			int secondsToReset = (int) (tomorrow - current).TotalSeconds;
			rolloverTimer = GameTimerRange.createWithTimeRemaining(secondsToReset);
		}
	}
	
	public static void showVideo()
	{
		VideoDialog.showDialog(
			ExperimentWrapper.ElitePass.videoUrl, 
			"", 
			"Check it out!", 
			summaryScreenImage: ExperimentWrapper.ElitePass.videoSummaryPath, 
			autoPopped: false,
			statName:"elite"
		);
	}

	/// <summary>
	/// Implements IResetGame
	/// </summary>
	public static void resetStaticClassData()
	{
		unregisterEvents();
		expirationTimer = null;
		points = 0;
		targetPoints = 0;
		passExpiration = 0;
		passDuration = 0;
		spinRewards = 0;
		spinsTowardReward = 0;
		spinRewardThreshold = 0;
		maxSpinRewards = 0;
		showLobbyTransition = false;
		hasGoldFromUpgrade = false;
	}
}