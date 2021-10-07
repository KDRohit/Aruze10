using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Com.Scheduler;
using Zynga.Core.Util;

/**
Global references to scripts are placed in Scr.
This script contains variables and functions not specific to a game mode or category.
*/

// Daily bonus modes.
// If adding more modes, be sure to increment DAILY_BONUS_MODE_MAX.
public enum DailyBonus
{
	NONE = 0,
	SEVEN_DAY_NONE = 1,
	SEVEN_DAY_PRE = 2,
	SEVEN_DAY_POST = 3,
}

public class Glb : IResetGame
{
	public const int DAILY_BONUS_MODE_MAX = 3;

	public const string STARTUP_SCENE = "Startup";				// The scene asset name for the startup scene
	public const string STARTUP_LOGIC_SCENE = "Startup Logic";	// The scene asset name for the logic startup scene
	public const string LOADING_SCENE = "Loading";				// The scene asset name for the loading scene
	public const string LOBBY_SCENE = "Lobby";					// The scene asset name for the lobby scene
	public const string GAME_SCENE = "Game";					// The scene asset name for the game scene
	public const string RESET_SCENE = "ResetGame";   			// The scene asset name for the reset scene

	public const int SCREEN_WIDTH = 1024;			// Base screen width when on iPad 2.
	public const int SCREEN_HEIGHT = 768;			// Base screen height when on iPad 2.

	public const string DYNAMIC_IMAGE_PATH = "images/";	// Path of dynamically streamed images, relative to gameUrl

#region SCAT_CONSTANTS
	// SCAT-defined constants.
	public static bool			ALLOW_CREDITS_PURCHASE = false;
	public static int			BIG_WIN_THRESHOLD = 5;
	public static int			CHALLENGE_BONUS_CREDITS = 100;
	public static int			CREDIT_SEND_RETURN_AMOUNT = 5;
	public static int			DAILY_BONUS_MAX_CHIP_COUNT = 10000;
	public static DailyBonus	DAILY_BONUS_MODE = DailyBonus.SEVEN_DAY_NONE;
	public static long			DEFAULT_BANKROLL_BET_PCT = 0;
	public static float			DIALOG_ANIM_TIME = 0;
	public static int			GIFTING_CREDITS = 20;
	public static long			INITIAL_CREDITS = 5000;
	public static int			LIKE_DAILY_VIP_POINTS_REWARD = 0;
	public static int			LIKE_INCENTIVE_VIP_POINTS_REWARD = 0;
	public static int			LOW_CHIPS_MULTIPLIER = 4;
	public static float			MOBILE_AUTO_CLOSE_DIALOG_SECONDS = 3;
	public static int			NUMBER_OF_RANDOM_FACTS = 0;
	public static float			ROLLUP_MULTIPLIER = .75f;
	public static bool			SHOW_TUTORIALS = false;
	public static bool			USE_SWRVE_DATA = false;
	public static int			VIP_POINTS_PER_DAY = 0;
	public static int			VIP_POINTS_PER_INVITED_FRIEND = 0;
	public static int			UNLOCK_HIGH_LIMIT_LOBBY = 0;
	public static float			MOBILE_RESET_HOURS = 24f;
	public static int			PROGRESSIVE_JACKPOT_UPDATE = 20;
	public static int 			INFINITE_SPIN_TIME_ALLOWED = 10;
	public static int 			GLOBAL_BASE_WAGER = 100;
	public static int			NETWORK_PROFILE_NEW_MOTD_MIN_LEVEL = 15;
	public static int			NETWORK_PROFILE_MOTD_MIN_LEVEL = 10;
	public static int 			ACHIEVEMENT_MOTD_MIN_LEVEL = 5;
	public static int			NETWORK_FRIENDS_MOTD_MIN_LEVEL = 20;
	public static int			SEND_GIFT_COOLDOWN = 64800;
	
	public static SpecialWinSurfacing SURFACING_QUEST_RACE_TO_RICHES_WIN = SpecialWinSurfacing.POST_NORMAL_OUTCOMES;
	public static SpecialWinSurfacing SURFACING_MYSTERY_GIFT_WIN = SpecialWinSurfacing.POST_NORMAL_OUTCOMES;
	public static SpecialWinSurfacing SURFACING_PROGRESSIVE_WIN = SpecialWinSurfacing.PRE_REEL_STOP;
#endregion

#region ZRT_VALUES
	// ZRuntime defined values
	public static long EARLY_ACCESS_MIN_BET_MULTIPLIER = 1L;
	public static string EARLY_ACCESS_WAGER_SET = "";

	public static int STARTERPACK_SEC_AFTER_INSTALL = -1;
	public static int STARTERPACK_SEC_REPEAT_PERIOD = 172800; // 48 hours.
	public static string STARTER_PACK_DEFAULT_PACKAGE = "coin_package_5"; // The default package we use for starter packs in case of bad data (new system only).
	public static string CHECK_PAYMENT = "new"; // The default whether to use old or new payments
	public static int RANDOM_LOGGING_VALUE = 1; // The default is 1 which means log everything
	public static int STARTER_PACK_DEFAULT_BONUS = 10; // The default bonus we use for starter packs in case of bad data (new system only).
	public static string[] BUY_PAGE_DEFAULT_PACKAGES = 
	{
		"coin_package_2",
		"coin_package_5", 
		"coin_package_25", 
		"coin_package_50", 
		"coin_package_75", 
		"coin_package_100"
	};

	public static string NEED_CREDITS_DEFAULT_PACKAGE = "coin_package_25"; // The default package we use for the Need Credits dialog in case of bad data (new system only).

	public static int GAMECENTER_SEC_AFTER_INSTALL = -1;
	public static int GAMECENTER_SEC_REPEAT_PERIOD = 604800; // 7 days.

	public static string stageName;			/// The server returned stage type
	public static string appNamespace;		/// The facebook app namespace
	public static string appId;				/// The facebook app id

	public static string urlPrefix = "https://";		/// The url prefix [http:// or https://], by default starts out as https:// because of iOS 10
	public static string appDomain;					/// The facebook domain of the app
	
	public static List<List<string>> cdnUrl = null;     // CDN url which is the prefix to all the urls
	public static bool switchCdnUrl = false;	// Switch CDN url to the new url

	public static string accessKey;
	public static string mobileStreamingAssetsUrl;		// The base url for external assets like images.
	public static int mobileStreamingAssetsVersion;		// The version for external assets like images. Added as a querystring to requests to avoid unwanted caching.
	public static string dataUrl;			/// The base url for talking to the server
	public static string requestUrl;		/// The url to the player request script
	public static string gameUrl;			/// The base versioned S3 path
	public static string ogObjectBaseUrl;	/// The base url for OpenGraph stuff
	public static string actionUrl;			/// The url for posting actions
	public static string logErrorUrl;		/// The url for posting errors logged to splunk
	public static string logEventUrl;		/// The url for posting events logged to splunk
	public static string minClientVersion;	/// The minimum client version sent from server
	public static string clientAppstoreURL;	/// The url to goto to update the client.
	public static string wozSlotsClientAppstoreURL;	// The url to goto to download the WOZ Slots app.
	public static string clientSocialURL;	/// The url to go to evangelize the client - eg: Google Plus.
	public static string loginUrl;			/// The url to login with
	public static string zidCheckUrl;		/// The url to check zids with
	public static string bundleBaseUrl;		/// The base url for asset bundles
	public static int mobileBuyTwiceMultiplier; /// The buy-twice multiplier for special sales.
	public static string[] staticAssetHosts; /// List of CDN hosts for external assets
	public static string[] devicesForceReloadOnResume = {};	// If our SystemInfo.deviceModel identifier is on this list, do specific graphic mitigation.

	// (optional) old CDN hosts and new CDN hosts; we temporarily are using this to replacement akamai hosts with cloudfront...
	public static string[] staticAssetHostOldUrls; // Old (akamai) vs new  List of CDN hosts for external assets
	public static string[] staticAssetHostNewUrls; // Old (akamai) vs new  List of CDN hosts for external assets

	public static string HELP_LINK_SUPPORT = "https://www.zyngaplayersupport.com/home/hit-it-rich";
	public static string HELP_LINK_TERMS = "https://m.zynga.com/legal/terms-of-service";
	public static string HELP_LINK_PRIVACY = "https://m.zynga.com/privacy/policy";
	public static string HELP_LINK_SMS = "	https://www.zynga.com/legal/SMS-program";
	public static string FACEBOOK_LINK_APPLICATIONS = "https://www.facebook.com/settings?tab=applications";

	public static int PUSH_NOTIF_SOFT_PROMPT_THROTTLE = 24 * 2; // 2 days.
	public static int PUSH_NOTIF_SOFT_PROMPT_CAP = 5; //hard cap
	public static int PUSH_NOTIF_SOFT_PROMPT_FREQUENCY = 4; //Number of collects before prompting

	public static string MOTD_KEY = "";			// Display this message-of-the-day.
#if RWR		
	public static string RWR_ACTIVE_PROMO = "";
	public static string RWR_SWEEPSTAKES_LINK_LEGAL = "https://zynga.com/games/free-slots/sweepstakes/HIR-January-2015";
#endif
	
	public static bool SHOW_EARLY_ACCESS_TAG = false;
	
	public static bool UNLOCK_ALL_GAMES = false;
	public static int UNLOCK_ALL_GAMES_END_TIME = 0;
	public static int UNLOCK_ALL_GAMES_START_TIME = 0;
	public static int UNLOCK_ALL_GAMES_MOTD_COOL_DOWN = 7;
	public static int RAINY_DAY_MOTD_MIN_LEVEL = 8;

	
	public static int    ENGAGEMENT_REWARD_MULTIPLIER = 2;
	public static long   MAX_BET_AMOUNT = -1L;
	public static string JSON_CACHE_FORMAT = "0";
	public static string DYNAMIC_MOTD_JSON_STRING = "";
	public static bool   STATS_BYPASS = false;

	public static bool SHOULD_HIDE_GOOGLE_PLUS = false;

	public static int LINKED_VIP_MIN_LEVEL = 5;

	public static int POPCORN_SALE_RTL_SHOW_COOLDOWN = 1;
	public static int MAX_VOLTAGE_MIN_LEVEL = 25;
#endregion

#region LIVEDATA_VALUES
	public static bool IS_USING_VIP_EARLY_ACCESS_WAGER_SETS = false;	// Tells if the wager set of an early access game can be overridden via an experiment
	public static bool IS_USING_VIP_EARLY_ACCESS_WAGER_SETS_VIP_MIN_WAGER = false;	// Tells if vip_progressive_minimum in SlotsWagerSets is used for progressives on early access games
	public static int SUPER_WIN_THRESHOLD = 25;     // threshold for super win in multiples of the bet amount
	public static int MEGA_WIN_THRESHOLD = 50;      // threshold for mega win in multiples of the bet amount
	public static float LIVE_SPIN_SAFETY_TIMEOUT = 20000.0f; // value here and/or data from LIVEDATA will come down as miliseconds and be converted to seconds
	public static bool NEW_RETRY_LOGIC = false; // value determining the retry logic for FB 
	public static bool START_TIMER_WEEKLY_RACE = false; // value that determines whether to start timer that pings server every 30 seconds to get updated weekly race data
	public static long TIMER_INTERVAL_WEEKLY_RACE = 30000; // value that sets the timer interval for weekly race
	public static bool START_SECOND_TIMER_WEEKLY_RACE = false; // value that determines whether to start the second timer that pings the server in 15 seconds to get updated weekly race data
	public static long TIMER_SECOND_INTERVAL_WEEKLY_RACE = 15000; // value that sets the second timer interval for weekly race
	public static string[] BLACKLIST_GIFTCHEST_GAME_KEYS = null; //Value that sets the blacklist for gifted freespin games
	public static bool ENABLE_MACHINE_SCALER_UPDATE = false; //Boolean used to check if the new machine scaling with the features panel overlap should be enabled
#endregion
	
	public static bool inCleanupMode = false;
	public static bool isResetting { get; private set; }
	public static bool isQuitting = false;			///< Is set to true when the game is quitting, since there is no built-in global for it.
	public static float memoryMBAtLastCleanup;	// samples memory in use during each cleanupMemoryAsync

	public const int SERVER_LOG_SAMPLE_RANGE = 10000;
	public static bool serverLogErrors = false;
	public static bool serverLogWarnings = false;
	public static bool serverLogPayments = false;
	public static bool serverLogLoadTime = false;
	public static bool serverLogDeviceInfo = false;
	public static bool serverLogPurchasablePackages = false;
	public static bool serverLogPushNotifications = false;
	public static bool showVerifyEmailDialog = true;
	public static bool showEditButton = true;
	public static bool logRequestError = true;
	public static bool logAnonAuth = false;
	public static bool logAccountStore = false;
	public static bool loginAnonUser = false;
	public static bool deleteWebglAccountStore = false;
	public static bool showWebglEmail = false;
	public static bool webglAccountSwitch = false;
	public static int migrationCounter = 0;
	public static int zisMigrateRetry = 0;
	public static int zisMigrateRetryMs = 0;
	public static int zisMigrateRetryMsMultiplier = 0;
	public static bool llapidisconnect = false;
	public static bool retryZisLogin = false;
	public static bool switchFbCall = false; // Switch fb to make server authoritative for getting the photo, names etc
	public static bool isNew = false; // Check to see if it is a new social connection
	public static long showEmailOptIn = 0L; // Check to see if on load to show email optin or not
	public static bool initializeAnalytics = true; // Initialize the analytics package
	public static bool appStartIncrimented  { get; private set; } ///< True is the appStartCount has been incrimented in this session

	public static bool loadOptimizedFlatSymbolsOvertime = false;

	// Control for custom symbol culling
	public static bool enableSymbolCullingSystem = true;  // kill switch for the whole system (from live data)
	public static bool enableSymbolCulling = true;        // diagnostic control (requires cullingSystem to be enabled)
	public static bool autoToggleSymbolCulling = false;   // diagnostic toggle

	public static JSON [] popcornSalePackages = null;
	public static JSON[] richPassPackages = null;
	public static JSON[] bonusGamePackages = null;
	public static JSON[] premiumSlicePackages = null;
	public static JSON [] progressiveJackpots = null;

	[System.Obsolete("Wagers now come down as part of the wager set (reprice 2019 changes)")]
	public static Dictionary<string, JSON[]> wagerUnlockData; //The different unlock levels for the wagers based on the "global_max_wager" experiment.

#if UNITY_EDITOR
	private static bool debugOptionalLogLargeJSONMessagesToFile = false; // <- Always FALSE when checking-in to source control!!!
#endif

	private static bool? _isUpdateAvailable = null;
	private static string[] blacklistVersions = null;

	// initially 0, updated if/when the game is restarted
	private static float lastRestartTimestamp = 0.0f;
	private static PreferencesBase _pref = null;
	private static PreferencesBase preferences
	{
		get
		{
			if (_pref == null)
			{
				_pref = SlotsPlayer.getPreferences();
			}

			return _pref;
		}
	}

	public static JSON[] allPackages
	{
		get
		{
			List<JSON> allPackages = new List<JSON>();
			if (popcornSalePackages != null)
			{
				allPackages.AddRange(popcornSalePackages);
			}
			if (richPassPackages != null)
			{
				allPackages.AddRange(richPassPackages);
			}
			if (premiumSlicePackages != null)
			{
				allPackages.AddRange(premiumSlicePackages);
			}
			if (bonusGamePackages != null)
			{
				allPackages.AddRange(bonusGamePackages);
			}
			return allPackages.ToArray();
		}
	}


	public static bool isUpdateAvailable
	{
		get
		{
			if (_isUpdateAvailable == null)
			{
				int currentVersion = CommonText.parseVersionString(clientVersion);
		
				if (currentVersion == 0)
				{
					// This is a dev build
					_isUpdateAvailable = false;
				}
				else
				{
					int requiredVersion = CommonText.parseVersionString(minClientVersion);

					if (string.IsNullOrEmpty(clientAppstoreURL))
					{
						Debug.LogWarning("Client version check skipped because clientAppstoreURL is empty or null.");
						_isUpdateAvailable = false;
					}
					else if (currentVersion < requiredVersion)
					{
						_isUpdateAvailable = true;
						Bugsnag.LeaveBreadcrumb("Client version " + clientVersion + " less than minimum version " + minClientVersion + ", forcing update.");
					}
					else
					{
						blacklistVersions = Data.liveData.getArray("MOBILE_VERSION_BLACKLIST", new string[0]);
						if (System.Array.IndexOf(blacklistVersions, clientVersion) != -1)
						{
							_isUpdateAvailable = true;
							Bugsnag.LeaveBreadcrumb("Client version " + clientVersion + " is blacklisted, forcing update.");
						}
						else
						{
							_isUpdateAvailable = false;
						}
					}
				}
			}
			return (_isUpdateAvailable == true);
		}
	}
	
	/// The number of times the application has been started.  This does not count
	/// seamless application resumes.
	public static int appStartCount
	{
		get
		{
			return preferences.GetInt(DebugPrefs.APP_START_COUNT, 1);
		}
	}

	/// Increments the Glb::appStartCount if Glb::appStartIncrimented is false.
	/// This should only be called once per application session and will throw a warning and do nothing
	/// if it is called multiple times in a session.
	public static void incrementAppStartCount()
	{
		if (!appStartIncrimented)
		{
			preferences.SetInt(DebugPrefs.APP_START_COUNT, appStartCount + 1);
			preferences.Save();
			appStartIncrimented = true;
		}
		else
		{
			Debug.LogWarning("Glb::incrementAppStartCount() called mutliple times in one session.  No Action taken.");
		}
	}

	public static System.DateTime installDateTime
	{
		get
		{
			//attempt player prefs cache lookup
			string installDateTimeString = preferences.GetString(Prefs.FIRST_APP_START_TIME, null);
			if (installDateTimeString == null)
			{
				//if lookup fails use local storage on webgl

				//Represents a time that we have installed the game.  Log it.
				return System.DateTime.Now;
			}
			else
			{
				//Debug.Log("instalDateTime = " + installDateTimeString);
				long fileTime = long.Parse(installDateTimeString);
				return System.DateTime.FromFileTime(fileTime);
			}
		}
		set
		{
			if (installDateTime != value)
			{
				//save to player prefs cache
				preferences.SetString(Prefs.FIRST_APP_START_TIME, value.ToFileTime().ToString());
				preferences.Save();
			}
		}
	}

	public static string ogObjectBaseURL
	{
		get
		{
			return string.Format("https://apps.facebook.com/{0}/og/", Glb.appNamespace);
		}
	}

	public static bool isNothingHappening
	{
		get
		{
			return (
				isNothingExceptDialogsHappening &&
				(Dialog.instance == null || !Dialog.instance.isShowing)
				// Don't add new conditions here. Put them in isNothingExceptDialogsHappening below.
			);
		}
	}

	/// Returns whether nothing is happening, without regards to whether a dialog is open,
	/// to indicate that something special is allowed to happen now.
	public static bool isNothingExceptDialogsHappening
	{
		get
		{
			return (
				!Packages.PaymentsPurchaseInProgress() &&
				!Loading.isLoading &&
				!(SlotBaseGame.instance != null && SlotBaseGame.instance.isGameBusy) &&
				!(SlotBaseGame.instance != null && SlotBaseGame.instance.isBigWinBlocking) &&
				!(Dialog.instance != null && Dialog.instance.isShowing && Dialog.instance.currentDialog.type.keyName == "daily_bonus") &&
				!(ChallengeGame.instance != null) &&
				!(FreeSpinGame.instance != null) &&
				!(Overlay.instance != null && Overlay.instance.topHIR != null && Overlay.instance.topHIR.levelUpSequence != null)
				// Add more conditions here.
			);
		}
	}

	// The current version of the client running.  The format looks like '1.0.1'.
	public static string clientVersion
	{
		get
		{
#if UNITY_EDITOR
			_clientVersion = "dev";
#else
			// Attempt to read the version from the version file created during
			// the build job.
			if (_clientVersion == null)
			{
				TextAsset versionFile = Resources.Load("Config/version") as TextAsset;
				if (versionFile != null)
				{
					_clientVersion = versionFile.text;
				}
				if (string.IsNullOrEmpty(_clientVersion))
				{
					if (Data.debugMode)
					{
						// Fall back to a version of "dev" if something went wrong on a non-production build.
						_clientVersion = "dev";
					}
					else
					{
						// Otherwise fallback to a version of "1.0.0" if something went wrong on a production build.
						// This way we can make the build work if we need to via zruntime, but by default it won't work.
						_clientVersion = "1.0.0";
					}
				}
			}
#endif
			return _clientVersion;
		}
	}
	private static string _clientVersion = null;

	public static string buildTag
	{
		get
		{
			if (_buildTag == null)
			{
				if (Application.isPlaying && Application.isEditor)
				{
					_buildTag = "Editor";
				}
				else
				{
					TextAsset buildTagFile = Resources.Load("Config/build_tag") as TextAsset;
					if (buildTagFile != null)
					{
						_buildTag = buildTagFile.text;
					}
				}

				if (string.IsNullOrEmpty(_buildTag))
				{
					_buildTag = "";
				}
			}
			return _buildTag;
		}
	}
	private static string _buildTag = null;

	private static Camera recentMainCamera = null;

	/// Gets a reference to the main camera, may be set more intelligently in the future.
	/// This is preferred to using Camera.main directly because it is faster and more flexible.
	/// Note: Camera.main will return null if there are no enabled cameras.
	public static Camera mainCamera
	{
		get
		{
			if (recentMainCamera == null)
			{
				recentMainCamera = Camera.main;
			}
			return recentMainCamera;
		}
		set
		{
			recentMainCamera = value;
		}
	}
	
	/// Performs routine memory cleanup appropriate to the runtime mode.
	/// Since Destroy() calls are applied after LateUpdate() we do this via a Coroutine so we can defer
	/// execution to a later frame. Unity only unloads Assets if there are no live references to them, so if
	/// we call UnloadUnusedAssets() immediately after Destroy(), the object's Assets will not actually be cleaned up.
	public static void cleanupMemoryAsync()
	{
		RoutineRunner.instance.StartCoroutine(cleanupMemoryDeferred());
	}

	private static IEnumerator cleanupMemoryDeferred()
	{
		yield return null;
		yield return null;

		// Might be more to this later...
		Resources.UnloadUnusedAssets();

		//micmurphy -- UnloadUnused assets calls System.GC.Collect directly. There is no point in calling again, it just forces the cpu to waste cyclels on another pass
		//czablocki - there's conflicting info about whether GC.Collect() gets called, it might have changed between versions:
		//https://answers.unity.com/questions/910845/unloadunusedassets-what-exactly-does-unused-mean.html
		//https://forum.unity.com/threads/resources-unloadunusedassets-vs-gc-collect.358597/
		//Setting breakpoints in GC.cs indicates it does not get called by UnloadUnusedAssets in 2018.4 though
		System.GC.Collect();
		
		// Checking free memory can be slow (esp. on Android, 30+ ms) so sample it here, whenever we do cleanupMemoryAsync
		memoryMBAtLastCleanup = (float)Zynga.Core.Platform.DeviceInfo.CurrentMemoryMB;
	}

	public static void emptyGameSymbolCache()
	{
		// If we have a memory warning and we're in a reelGame we should clear the symbol cache asap
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame baseGame = SlotBaseGame.instance;
			baseGame.clearSymbolCache();
			baseGame.clearSymbolMap();
			baseGame.clearSymbolBoundsCache();
		}
		if (FreeSpinGame.instance != null)
		{
			FreeSpinGame freeSpinGame = FreeSpinGame.instance;
			freeSpinGame.clearSymbolCache();
			freeSpinGame.clearSymbolMap();
			freeSpinGame.clearSymbolBoundsCache();
		}
	}

	public static void resetGameAndLoadBundles(string reason, List<string> bundlesToLoad, LobbyLoader.onLobbyLoad onLobbyLoadEvent = null)
	{
		if (bundlesToLoad != null)
		{
			for (int i = 0; i < bundlesToLoad.Count; i++)
			{
				// Since we're going to reset the game, we need to make sure that we're not going to
				// send up this bundle as one we have to lazy load. So to avoid a race condition where we
				// have to load a bundle before we hit the code that checks for lazy loaded bundles,
				// remove this bundle from that mapping if it exists in there.
				if (AssetBundleManager.lazyLoadedBundleToFeatureMap.ContainsKey(bundlesToLoad[i]))
				{
					AssetBundleManager.lazyLoadedBundleToFeatureMap.Remove(bundlesToLoad[i]);
				}
			}

			if (onLobbyLoadEvent != null)
			{
				LobbyLoader.lobbyLoadEvent += onLobbyLoadEvent;
			}
		}

		resetGame(reason);
	}

	/// Resets the game and puts you in the give scene
	public static void resetGame(string reason)
	{
		isResetting = true;

#if UNITY_EDITOR && !ZYNGA_PRODUCTION
		if (Zap.Automation.ZyngaAutomatedPlayer.hasBeenSetup)
		{
			Zap.Automation.ZyngaAutomatedPlayer.instance.setupForResume();
		}
#endif
        
		Userflows.flowEnd("run_time");
		Scheduler.dump();
		RoutineRunner.instance.StopAllCoroutines();
		// update the "restart" timestamp so loadtime metrics make sense
		lastRestartTimestamp = Time.realtimeSinceStartup;
			
		Server.unregisterEventDelegate("slots_outcome");

		if (BonusGameWings.instance != null)
		{
			BonusGameWings.instance.hide();
		}
		
		Loading.show(Loading.LoadingTransactionTarget.LOBBY);
		
#if UNITY_WEBGL && !UNITY_EDITOR
		// WebGL reloads will reload the entire webpage
		Application.ExternalEval("window.reloadGame()");
#else
		// Make sure input is restored just in case it got disabled before the reset.
		NGUIExt.enableAllMouseInput();

		Bugsnag.LeaveBreadcrumb("Beginning to reinitialize game because: " + reason);
		preferences.SetInt(Prefs.REINITIALIZE_GAME, 1);
		preferences.Save();
		
		RoutineRunner.instance.StartCoroutine(loadScene(RESET_SCENE), false);
#endif

#if ZYNGA_TRAMP
		if (AutomatedPlayer.instance != null)
		{
			AutomatedPlayer.instance.restartAutomation(true);
		}
#endif
	}

	/// Searches for and calls a reset function for every class that
	/// implements IResetGame and has a static resetStaticClassData()
	/// function.  This is so that the game can reinitialize safely.
	public static void reinitializeGame()
	{
		isResetting = false;
		SlotsPlayer.isLoggedIn = false;

		
		List<System.Type> types = Common.getAllClassTypes("IResetGame");
		
		// Look through every IResetGame class and call the magic method.
		foreach (var type in types)
		{
			FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
			bool hasStaticVariables = false;
			foreach (FieldInfo field in fields)
			{
				if (!field.IsLiteral)
				{
					hasStaticVariables = true;
					break;
				}
			}

			MethodInfo mi = type.GetMethod("resetStaticClassData");
			if (mi != null)
			{
				try
				{
					mi.Invoke(null, null);
				}
				catch (System.Exception ex)
				{
					Debug.LogErrorFormat("Glb.reinitializeGame() : Exception during reset of {0} : {1}", type.Name, ex.ToString());
					// czablocki - 2/2020: We have SIGABRT errors occurring that have some of these exceptions in the breadcrumbs. 
					// Log to Splunk so we have more data available
					var extraFields = new Dictionary<string, string>
					{
						{"type", type.Name},
						{"message", ex.Message}
					};
					SplunkEventManager.createSplunkEvent("Glb reinitializeGame Error", "resetStaticClassData-failed", extraFields);
				}
			}
			else
			{
				// czablocki - 2/2020: temporarily commenting out since it creates a lot of noise in BugSnag breadcrumbs
				// and doesn't provide much information. We have many classes that inherit from IResetGame that don't bother
				// to implement resetStaticClassData() since the interface is actually empty.
				if (hasStaticVariables)
				{
					// Debug.LogError("A call to resetStaticClassData was made on " + type.Name + ", but has not yet been implemented and there are static variables!");
				}
				else
				{
					// Debug.LogWarning("A call to resetStaticClassData was made on " + type.Name + ", but has not yet been implemented!");
				}
			}
		}

		// We just reset a bunch of stuff, so time to GC.
		cleanupMemoryAsync();
	}

	/// Loads a given scene using the inbetween loading scene
	public static void loadLobby()
	{
		// Whenever returning to the lobby in any way, make sure the jackpot overlay is hidden.
		if (Overlay.instance != null && Overlay.instance.jackpotMystery != null)
		{
			Overlay.instance.jackpotMystery.hide();
		}

		SlotBaseGame.instance = null;

		DisposableObject.sceneCleanup();
		InbetweenSceneLoader.nextScene = LOBBY_SCENE;
		//Google Nexus 10 sometimes has issues closing this login screen.  Put in this failsafe to help out with it.
		if (Login.instance != null)
		{
			Login.instance.gameObject.SetActive(false);
		}
		RoutineRunner.instance.StartCoroutine(loadScene(LOADING_SCENE));
	}

	/// Loads a given scene using the inbetween loading scene
	public static void loadGame()
	{
		if (MainLobby.instance != null)
		{
			// Loading a different scene causes the lobby to get destroyed, so manually clean it up first.
			MainLobby.instance.cleanupBeforeDestroy();
		}

		// Make sure we end any active spin transactions in case we are transitioning away from another game during a spin
		// otherwise the transaction will be incomplete and complain when they spin in the new game they are loading
		if (Glb.spinTransactionInProgress)
		{
			Glb.endSpinTransaction();
		}

		DisposableObject.sceneCleanup();
		InbetweenSceneLoader.nextScene = GAME_SCENE;
		
		RoutineRunner.instance.StartCoroutine(prepGameLoad());
	}
	
	// Prepares a game to be loaded by making sure the game's data is available
	// before loading the game scene.
	private static IEnumerator prepGameLoad()
	{
		SlotGameData slotGameData = SlotGameData.find(GameState.game.keyName);
		if (slotGameData == null)
		{
			// Need to request game data.
			JSON jsonData = null;
			string url = "";
			
			const string RESPONSE_KEY = "LoadGameData";
			url = SlotGameData.getDataUrl(GameState.game.keyName);
	
			yield return RoutineRunner.instance.StartCoroutine(Data.attemptServerRequest(url, null, "error_failed_to_load_data", RESPONSE_KEY, false, "_game_" + GameState.game.keyName));
			jsonData = Data.data;

			// Very rarely, a very fast tap into a game crashes it because one of these things is null,
			// so we wait for these to get un-null.
			float waitStartTime = Time.realtimeSinceStartup;
			while (SlotsPlayer.instance == null)
			{
				yield return null;
				
				if (Time.realtimeSinceStartup - waitStartTime > 30f)
				{
					Debug.LogError("Player data incomplete during attempted game load for: " + url);
					yield break;
				}
			}

			// make sure this is initialized before we load data into it
			if (SlotsPlayer.instance.progressivePools == null)
			{
				SlotsPlayer.instance.progressivePools = new ProgressivePools();
			}
			
			if (jsonData != null)
			{
				SlotsPlayer.instance.progressivePools.bindStaticData(jsonData.getJsonArray("progressive_pools"));
				BonusGame.populateAll(jsonData.getJsonArray("bonus_games_data"));
				BonusGamePaytable.populateAll(jsonData.getJsonArray("bonus_game_pay_tables"));
				PayTable.populateAll(jsonData.getJsonArray("pay_tables"));
				ReelStrip.populateAll(jsonData.getJsonArray("reel_strips"));		// ReelStrips must populate before SlotGameData
				ThresholdLadderGame.populateAll(jsonData.getJsonArray("threshold_ladder_games"));
				slotGameData = SlotGameData.populateGame(jsonData);
				if (slotGameData == null)
				{
					Debug.LogError("Slot game data is broken for: " + url);
					yield break;
				}
			}
			else
			{
				// Failed to get the data, so go back to the lobby after showing an error.
				Debug.LogError("Unable to get any game-specific json data from: " + url);
				Loading.hide(Loading.LoadingTransactionResult.FAIL);
				GenericDialog.showDialog(
					Dict.create(
						D.TITLE, Localize.text("check_connection_title"),
						D.REASON, "game-data-download-error",
						D.MESSAGE, Localize.text("download_error_message") + "\n\n" + "Glb.prepGameLoad: " + GameState.game.keyName,
						D.CALLBACK, new DialogBase.AnswerDelegate((args) => { GameState.pop(); Loading.show(Loading.LoadingTransactionTarget.LOBBY); loadLobby(); })
					),
					SchedulerPriority.PriorityType.IMMEDIATE
				);
				yield break;
			}
		}

		// Update the GameState.BonusGameNameData now that we are sure the SlotGameData is loaded
		GameState.BonusGameNameData bonusGameNameData = GameState.bonusGameNameData;
		if (bonusGameNameData != null && GameState.game != null)
		{
			bonusGameNameData.populateBonusGameNames(GameState.game.keyName);
		}
		
		// Don't set this until we know we're leaving the lobby for a game.
		// We can't set it when loading the main lobby because the player may
		// go to the VIP lobby then back to the main lobby without launching a game,
		// which would treat the main lobby the same as returning from a game.
		MainLobby.isFirstTime = false;
		MainLobby.didLaunchGameSinceLastLobby = true;

		RoutineRunner.instance.StartCoroutine(loadScene(LOADING_SCENE));
	}
	
	/// Let's a couple frames go by before loading a scene, to give time for the loading screen to show first.
	/// Otherwise we see blank for a split second (or more) before seeing the loading screen.
	public static IEnumerator loadScene(string sceneName)
	{
		//one frame is enough
		yield return null;
		
		SceneManager.LoadScene(sceneName);
	}

	/// Writes optional debug info to the editor log only
	public static void editorLog(params string[] textItems)
	{
#if UNITY_EDITOR
		if (Data.debugMode && PlayerPrefsCache.GetInt(DebugPrefs.OPTIONAL_LOGS, 0) != 0)
		{
			string text = System.String.Join(" ", textItems);
			
			// Unity's log system truncates on Lengths > 16300
			const int MAX_STRING_SIZE = 16300;
			if (text.Length <= MAX_STRING_SIZE)
			{
				Debug.Log(text);
			}
			else
			{
				if (debugOptionalLogLargeJSONMessagesToFile)
				{					
					string filePath = "Assets/-Temporary Storage-/JSON Messages/json_msg.txt";

					// Create the directory if it does not exist
					string directory = System.IO.Path.GetDirectoryName(filePath);
					if (!System.IO.Directory.Exists(directory))
					{
						System.IO.Directory.CreateDirectory(directory);
					}

					// Make sure the filename is unique i.e. if json_msg.txt exist it will try json_msg 1.txt,
					// if json_msg 1.txt exists it will try json_msg 2.txt and so on.
					filePath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(filePath);

					System.IO.File.AppendAllText(filePath, text);

					// Import the new asset so Unity is aware of it
					UnityEditor.AssetDatabase.ImportAsset(filePath);

					// Get the asset so the Console can highlight the output file in the project when you click the log message
					Object context = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(filePath);

					Debug.LogFormat(context, "<color=yellow>JSON Message too large, {0} \nWritten to file: {1}</color>", text.Substring(0, 150), filePath );
				}
				else
				{
					Debug.Log("[TRUNCATED] " + text.Substring(0, MAX_STRING_SIZE));
				}
			}
		}
#endif
		if (Data.debugMode && DevGUIMenuTools.customLogEditor)
		{
			string text = System.String.Join(" ", textItems);
			CustomLog.Log.log(text, Color.cyan);
		}
	}
	
	// Returns the name of the store for the current device.
	public static string storeName
	{
		get
		{
#if UNITY_IPHONE
			return Localize.text("ios_store");
#elif ZYNGA_KINDLE
			return Localize.text("kindle_store");
#elif ZYNGA_GOOGLE
			return Localize.text("android_store");
#elif UNITY_WSA_10_0 && NETFX_CORE //SMP this should probably be WSA specific
			return Localize.text("windows_store");
#elif UNITY_WEBGL
			return "FACEBOOK PAYMENTS SYSTEM";
#else
			return "ONLINE STORE";
#endif
		}
	}

	// Are we recording spins?
	private static bool isRecordingSpinTransactions
	{
		get
		{
			return GameState.game != null;
		}
	}

	private static string spinTransactionName = "";
	private static int spinTransactionCount = 0;

	public static bool spinTransactionInProgress { get; private set; }

	// Begins a spin transaction
	public static void beginSpinTransaction(long wagerAmount)
	{
		if (spinTransactionInProgress)
		{
			failSpinTransaction("Overlapping spin transaction detected.");
		}
		
		string newSpinTransactionName = "slot-" + GameState.game.keyName;
		if (newSpinTransactionName != spinTransactionName)
		{
			// If we are spinning a new game, reset the name and count
			spinTransactionName = newSpinTransactionName;
			spinTransactionCount = 0;
		}
	
		if (isRecordingSpinTransactions)
		{
			spinTransactionInProgress = true;
			Userflows.flowStart(spinTransactionName);
			Userflows.addExtraFieldToFlow(spinTransactionName, "wager", wagerAmount.ToString());
			Userflows.addExtraFieldToFlow(spinTransactionName, "lobby", LobbyInfo.currentTypeToString);
		}
	}

	// Allow spin credit info to be appended to each transaction for desync checking
	public static void addCreditDataToSpinTransaction(long clientAmount, long expectedServerAmount, bool isDesync, PlayerResource.DesyncCoinFlow flow = null)
	{
		if (isRecordingSpinTransactions && spinTransactionInProgress)
		{
			Dictionary<string, string> extraFields = new Dictionary<string, string>();
			extraFields.Add("client_credits", clientAmount.ToString());

			if (isDesync)
			{
				extraFields.Add("server_expected_credits", expectedServerAmount.ToString());
				extraFields.Add("desync_client_overpaid_credits", (expectedServerAmount - clientAmount).ToString());
				extraFields.Add("suggested_source", flow != null ? flow.source : "unknown");
			}

			Userflows.addExtraFieldsToFlow(spinTransactionName, extraFields);
		}
	}

	// Ends a spin transaction as a "success"
	public static void endSpinTransaction()
	{
		if (isRecordingSpinTransactions)
		{
			if (!spinTransactionInProgress)
			{
#if UNITY_EDITOR
				Debug.LogError("Ended spin transaction without starting one.");
#endif
				return;
			}

			long winAmount = 0;
			long spinPayoutAmount = 0;
			long featureWinAmount = 0;
			if (SlotBaseGame.instance != null)
			{
				winAmount = SlotsPlayer.creditAmount - SlotBaseGame.instance.spinsStartingCredits;
				spinPayoutAmount = SlotBaseGame.instance.getRunningPayoutRollupAlreadyPaidOut();
				featureWinAmount = winAmount - spinPayoutAmount;
			}

			Userflows.addExtraFieldToFlow(spinTransactionName, "win_amount", winAmount.ToString());
			Userflows.addExtraFieldToFlow(spinTransactionName, "spin_payout", spinPayoutAmount.ToString());

			Userflows.flowEnd(spinTransactionName);
			spinTransactionCount++;
			spinTransactionInProgress = false;
		}
	}

	// Ends a spin transaction as a "failure"
	public static void failSpinTransaction(string errorMsg, string reason = "failed")
	{		
		string msg = string.Format("Transaction fail on '{0}' with message {1}", spinTransactionName, errorMsg);

		if (isRecordingSpinTransactions)
		{
			if (!spinTransactionInProgress)
			{
#if UNITY_EDITOR
				Debug.LogError("Failed spin transaction without starting one.");
#endif
				return;
			}
		
			Debug.LogError(msg);	// LogError will leave a breadcrumb

			Userflows.flowEnd(spinTransactionName, false, reason);
			
			spinTransactionCount++;
			spinTransactionInProgress = false;
		}
		else
		{
			// still log the error message so that it can be added to the Userflow
			Debug.LogError(msg);
		}
	}
	
	// Wraps a breadcrumb in the same logic that throttles spin transactions
	public static void leaveSpinGameBreadcrumb(string msg)
	{
		if (isRecordingSpinTransactions)
		{
			// Commented out for spam protection
			//Bugsnag.LeaveBreadcrumb(msg);
		}
	}
	
	// Restores whatever music should be playing, whether in the lobby or in a game.
	// Usually called when closing a dialog that has special music.
	public static IEnumerator restoreMusic(float delay = 0.0f)
	{
		yield return new WaitForSeconds(delay);
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.playBgMusic();
		}
		else
		{
			MainLobby.playLobbyMusic();
		}	
	}

	// loadtime metrics need to know the time since startup or last soft-restart
	public static float timeSinceStartOrRestart
	{
		get
		{
			return Time.realtimeSinceStartup - lastRestartTimestamp;
		}
	}


	// Fixes any URL, replacing old CDN paths (like akamai) with new CDN paths (like cloudfront)
	// This is a temporary hack for WebGL to get running on cloudfront
	public static string fixupStaticAssetHostUrl(string url)
	{
		if (staticAssetHostOldUrls != null && staticAssetHostNewUrls != null)
		{
			foreach (string oldUrl in staticAssetHostOldUrls)
			{
				if (url.Contains(oldUrl))
				{
					var fixedUrl = url.Replace(oldUrl, staticAssetHostNewUrls[0]);
					Debug.Log("kk replacing " + url + " with " + fixedUrl );
					return fixedUrl;
				}
			}
		}

		// No match? return original URL
		return url;
	}


	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		inCleanupMode = false;
		isQuitting = false;
		spinTransactionName = "";
		spinTransactionCount = 0;
		spinTransactionInProgress = false;
		_isUpdateAvailable = null;
		blacklistVersions = null;
	}
}

