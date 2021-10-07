using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using CustomLog;
using QuestForTheChest;
using UnityEngine;
using Zynga.Core.Platform;

public class Data : IResetGame
{
	public static bool isBasicDataSet;
	public static bool isGlobalDataSet;
	public static bool isPlayerDataSet;

	public static JSON data = null;

	// Shortcut to the liveData values.
	public static LiveData liveData;

	public static string overrideZid = "none";

	// Url to use to get the json containing login urls, global data url, etc
	public static string basicDataUrl = "none";

	// Url of the server being accessed
	public static string serverUrl = "none";

	// which zynga app to log into
	public static string zAppId = "none";

	// Are we in debug mode with debug options?
	public static bool debugMode;

	// Are we showing non-production ready games?
	public static bool showNonProductionReadyGames;

	// release stage for Bugsnag.
	public static string releaseStage = null;

	// Economy debug mode:
	public static bool debugEconomy
	{
		get
		{
			return _debugEconomy;
		}
		set
		{
			_debugEconomy = value;
			if (_debugEconomy == true)
			{
				// We also need to force a few prerequisites on, otherwise logging doesn't work properly:
				debugMode = true;
				//Economy.EconomyManager.GetInstance().EnableLogging();

				// Use preferences to save this so that next time the app is restarted it's turned on before we load to the menu:
				if (PlayerPrefsCache.GetInt(DebugPrefs.ECONOMY_LOG_ENABLED) != 1)
				{
					PlayerPrefsCache.SetInt(DebugPrefs.ECONOMY_LOG_ENABLED, 1);
					PlayerPrefsCache.Save();
				}

				Debug.Log("Data::debugEconomy - Economy debug mode is now enabled and persistent.");
			}
			else if (PlayerPrefsCache.HasKey(DebugPrefs.ECONOMY_LOG_ENABLED))
			{
				// Delete this flag to turn it off:
				PlayerPrefsCache.DeleteKey(DebugPrefs.ECONOMY_LOG_ENABLED);
				PlayerPrefsCache.Save();
				Debug.Log("Data::debugEconomy - Economy debug mode is now disabled. You may need to restart the game to take effect.");
			}
		}
	}

	private static bool _debugEconomy = false;  // Always default to false in case we're not in the editor.

	// which fb app to log into
	public static string fbAppId = "none";

	// Shortcut to the login data JSON object
	public static JSON login;

	// Shortcut to the player data
	public static JSON player { get; private set; }

    // NOTE: we no longer store the global data JSON dict after init, in order to save memory, because it is large

	public static bool IsSandbox;

	public static WebPlatform webPlatform;

	// Canvas-provided objects (Config, BasicData, PlayerData) when running on WebGL Canvas (else null)
	public static JSON canvasBasedConfig;
	public static JSON canvasBasedBasicData;
	public static JSON canvasBasedPlayerData;

	// Due to multiple places that need to read the config JSON due to race conditions,
	// and we only want to actually read it one time, we have this property getter with a cached object.
	public static JSON configJSON
	{
		get
		{
			// Attempt to read the config file to acquire the basic data URL and other stuff.
			// This should only happen once per session.
			if (_configJSON == null)
			{ 
				if (canvasBasedConfig != null)
				{
					// Use canvas-based config if it exists (for WebGL)
					Debug.Log("Using canvas-based Config");
					_configJSON = canvasBasedConfig;
				}
				else
				{
					// Else read from the embedded config file
					string configFilePath = getConfigFilePath();
					Bugsnag.LeaveBreadcrumb(string.Format("Loading configuration from '{0}'", configFilePath));
					TextAsset configFile = Resources.Load(configFilePath) as TextAsset;

					if (configFile != null && configFile.text != null)
					{
						_configJSON = new JSON(configFile.text);
					}
					else
					{
						Debug.LogError("config file failed to read: " + configFilePath);
					}
				}

				Zynga.Zdk.ZyngaConstants.GameSkuVersion = Glb.clientVersion;
			}
			return _configJSON;
		}
	}

	private static JSON _configJSON = null;

	// Returns the path to use for the config file.
	public static string getConfigFilePath()
	{
		return string.Format("Config/{0}",PlayerPrefsCache.GetString(DebugPrefs.EDITOR_CONFIG_FILE + SkuResources.currentSku.ToString(), "config").Replace(".txt", ""));
	}

	// Access the config file and set the static variables that we care about
	public static void loadConfig()
	{
		Debug.Log("Girish: In load config");
		Zynga.Zdk.ZyngaConstants.GameSkuVersion = Glb.clientVersion;

#if !ZYNGA_PRODUCTION
		if (SharedConfig.configJSON != null || configJSON != null)
		{
			JSON config = SharedConfig.configJSON != null ? SharedConfig.configJSON : configJSON;
			//Reset this if we we're using the override version
			Zynga.Core.Util.PreferencesBase preferences = SlotsPlayer.getPreferences();
			preferences.SetString(DebugPrefs.CONFIG_OVERRIDE_PATH, "");
			preferences.Save();
#else
		if (configJSON != null)
		{
			JSON config = configJSON;
#endif			
			basicDataUrl = config.getString("basic_data_url", "none");
			if (basicDataUrl == "none")
			{
				Debug.LogError("Missing Basic Data URL!");
			}

			serverUrl = config.getString("server_url", "none");
			if (serverUrl == "none")
			{
				Debug.LogError("Missing Server URL!");
			}

			zAppId = config.getString("Z_App_ID", "none");
			if (zAppId == "none")
			{
				Debug.LogError("Missing Zynga App ID!");
			}

			fbAppId = config.getString("FB_App_ID", "none");
			if (fbAppId == "none")
			{
				Debug.LogError("Missing FB App ID!");
			}

			overrideZid = config.getString("overrideZid", "none");

			IsSandbox = config.getBool("sandbox", false);

			Zynga.Zdk.ZyngaConstants.UserkeyLabel = config.getString("BUNDLE_ID", "");
			Zynga.Zdk.ZyngaConstants.BundleId = config.getString("BUNDLE_ID", "");

#if UNITY_EDITOR
			// Always allow debug mode in the editor.
			debugMode = true;
#else
			debugMode = config.getBool("debug", false);
#endif
			showNonProductionReadyGames = config.getBool("show_non_production_games", false);

			debugEconomy = config.getBool("debugEconomy", false);

			if (!debugEconomy)
			{
				// Otherwise just use the value stored in our local preferences:
				debugEconomy = (PlayerPrefsCache.GetInt(DebugPrefs.ECONOMY_LOG_ENABLED, 0) == 1) ? true : false;
			}

			if (debugMode && Loading.instance != null)
			{
				// Enable & Show server url on loading screen in debugMode
				Loading.instance.enableStageLabel(true);
				Loading.instance.setStageUrlLabel(serverUrl);
			}

			if (Log.instance != null)
			{
				// Only keep the Log script enabled if in debug mode.
				Log.instance.enabled = debugMode || Log.FORCE_LOG;
			}

			releaseStage = config.getString("releaseStage", "none");
		}
		else
		{
			Debug.LogError("Can't find config.txt file!");
		}

		// Handle any config overrides
		overrideConfigSettingsFromURL();
	}
	// Handle any overrides via URL query string (later to be provided in basic data or via JS call)
	private static void overrideConfigSettingsFromURL()
	{
		Debug.Log("Checking for config setting overrides from URL");

		var urlMgr = URLStartupManager.Instance;
		urlMgr.updateRefValueFromKVP("server_url",     ref serverUrl);
		urlMgr.updateRefValueFromKVP("basic_data_url", ref basicDataUrl);
		urlMgr.updateRefValueFromKVP("FB_App_ID",      ref fbAppId);
		urlMgr.updateRefValueFromKVP("Z_App_ID",       ref zAppId);
		urlMgr.updateRefValueFromKVP("debug",          ref debugMode);
		urlMgr.updateRefValueFromKVP("sandbox",        ref IsSandbox);
		urlMgr.updateRefValueFromKVP("non_prod_games", ref showNonProductionReadyGames);
	}


	// Set the liveData loaded in from LoadBasicData
	public static void setLiveData(JSON data)
	{
		liveData = new LiveData(data);
		Userflows.setSimulatedMode(!liveData.getBool("ENABLE_USERFLOWS", true));
		
		// The sampling presets array is a string array with entries like "spin_wow04:0.5",
		// which indicates that userflows with key "spin_wow04" should be sampled at a rate of 0.5.
		string[] samplingPresets = Data.liveData.getArray("USERFLOW_SAMPLING_PRESETS", null);
		if (samplingPresets != null && samplingPresets.Length > 0)
		{
			foreach (string preset in samplingPresets)
			{
				float samplingRate;
				string[] parts = preset.Split(':');
				if (parts.Length == 2 && float.TryParse(parts[1], out samplingRate))
				{
					Userflows.setFlowSampling(parts[0], samplingRate);
				}
				else
				{
					Debug.LogErrorFormat("Data.setLiveData() : Badly formed userflow sampling preset array item '{0}'.", preset);
				}
			}
		}
	}

	// set all the data from basic_data, before login, before we have global data, this gives us URLs, etc
	public static void setBasicData(JSON data)
	{
		// Initialize the time before doing anything that relies on timers.
		GameTimer.startSession(data.getInt("current_time", 0));

		Glb.stageName = data.getString("stage", "");
		Glb.appNamespace = data.getString("app_namespace", "");
		Glb.appId = data.getString("app_id", "");
		Glb.urlPrefix = data.getString("url_prefix", "");
		Glb.appDomain = data.getString("app_domain", "");
		Glb.dataUrl = Glb.appDomain + data.getString("data_url", "");
		Glb.requestUrl = data.getString("request_url", "");
		Glb.ogObjectBaseUrl = data.getString("og_object_base_url", "");
		Glb.actionUrl = Glb.dataUrl + "game.php";

		// TODO: TRAMP define/handling clean-up
#if ZYNGA_TRAMP
			Glb.logErrorUrl = "https://staging-bravo.hititrich.zynga.com/server/log_error.php";
			Glb.logEventUrl = "https://staging-bravo.hititrich.zynga.com/server/log_event.php";
#else
			Glb.logErrorUrl = Glb.dataUrl + "log_error.php";
			Glb.logEventUrl = Glb.dataUrl + "log_event.php";
#endif

		Glb.loginUrl = data.getString("zid_login_url", "");
		Glb.zidCheckUrl = data.getString("zid_check_url", "");
		Glb.minClientVersion = data.getString("min_client", "");
#if UNITY_ANDROID
		Glb.clientSocialURL = data.getString("google_plus_url", "https://plus.google.com/103674545678348927852/");
#endif

		// Gotta set this (from livedata) before we can use it
		Glb.switchCdnUrl = liveData.getBool ("SWITCH_CDN_URL", false);

		// Retry Zis login when it fails the first time around
		Glb.retryZisLogin = liveData.getBool("RETRY_ZIS_LOGIN", false);

		//Initialize the analytics package
		Glb.initializeAnalytics = liveData.getBool("INITIALIZE_ANALYTICS", true);

		//Special case login for anon users when the clientZid and ServerZid are different and install creds exists
		Glb.loginAnonUser = liveData.getBool("LOGIN_ANON_USER", false);

		//Delete all the webgl accountstore
		Glb.deleteWebglAccountStore = liveData.getBool("DELETE_WEBGL_ACCOUNTSTORE", false);

		// This is how we switch between two different CDN providers (akamai and cloudfront)  :-(
		if (Glb.switchCdnUrl)
		{
			Glb.cdnUrl = data.getStringListList("static_asset_hosts_v2");
			if (Glb.cdnUrl == null || Glb.cdnUrl.Count <= 0)
			{
				Debug.LogError("No CDN url in global data");
				Glb.staticAssetHosts = null;

			}
			else
			{
				Glb.staticAssetHosts = Glb.cdnUrl [1].ToArray();
			}
			Glb.bundleBaseUrl = getFullUrl(data.getString("bundle_base_url_v2", ""));
			Glb.mobileStreamingAssetsUrl = getFullUrl(data.getString("mobile_streaming_assets_base_url_v2", "BAD_URL"));
			SlotGameData.baseUrl = getFullUrl(data.getString ("game_data_base_url_v2", "BAD_URL"));
			LobbyOption.baseUrl = getFullUrl(data.getString("lobby_data_base_url_v2", "BAD_URL"));
			Quest.baseUrl = getFullUrl(data.getString("quest_data_base_url_v2", "BAD_URL"));

		}
		else 
		{
			Glb.staticAssetHosts = data.getStringArray ("static_asset_hosts");
			Glb.bundleBaseUrl = data.getString("bundle_base_url", "");
			Glb.mobileStreamingAssetsUrl = data.getString("mobile_streaming_assets_base_url", "BAD_URL");
			SlotGameData.baseUrl = data.getString ("game_data_base_url", "BAD_URL");
			LobbyOption.baseUrl = data.getString("lobby_data_base_url", "BAD_URL");
			Quest.baseUrl = data.getString("quest_data_base_url", "BAD_URL");
		}

		Debug.Log("URL: bundle_base_url = " + Glb.bundleBaseUrl);  // URL: bundle_base_url = https://socialslots.cdn.zynga.com/ios/bundles/

		// Temp query-string based override so we can run on any env without server-overrides
		URLStartupManager.Instance.updateRefValueFromKVP("bundle_base_url", ref Glb.bundleBaseUrl);

		if (Glb.bundleBaseUrl.Contains("/bundles/"))
		{
			//TODO: need to fix '/bundles/' in basic data server code here:
			//https://github.com/spookycool/casino_server/blob/1bdca1b5289d56093031e32570fbea7c290a47cd/public/server/basic_game_data.php
			//'bundle_base_url' => URL_PREFIX . IOS_BUNDLE_BASE_URL,
			Glb.bundleBaseUrl = Glb.bundleBaseUrl.Replace("/bundles/", "/bundlesv2/");
		}

		if (Glb.switchCdnUrl && liveData.getBool("USE_PARTIAL_URLS", false))
		{
			CarouselData.versionUrl = data.getString("mlcs_url_partial", "BAD_URL");
			CarouselData.versionNumber = data.getString("mlcs_version", "435");
			LoLa.versionUrl = data.getString("lola_url_partial", "BAD_URL");
		}
		else
		{
			CarouselData.versionUrl = data.getString("mlcs_url", "BAD_URL");
			LoLa.versionUrl = data.getString("lola_url", "BAD_URL");
		}

		StreakSaleManager.dataUrl = data.getString("streak_sale_url", "");
		if (StreakSaleManager.dataUrl.Length > 0)
		{
			StreakSaleManager.dataUrl = getFullUrl(StreakSaleManager.dataUrl);
		}

		SlotGameData.dataVersion = data.getString("data_version", "0");



		



#if UNITY_WEBGL
		// So mobile merged in the Glb.switchCdnUrl based CDN switchover code (above) while
		// WebGL fixes URL's in place here; can be cleaned up once the dust settles, it does no harm...
		// (Expecting Akamai CORS permissions to be fixed Monday Jan 29, 2018)

		string overrideWebGLCDN = liveData.getString("WEBGL_CDN_URL_OVERRIDE", "");
		if (overrideWebGLCDN != "")
		{
			Debug.Log("kk Using LiveData WEBGL_CDN_URL_OVERRIDE: " + overrideWebGLCDN);

			Glb.staticAssetHostOldUrls = new string[]
			{ 
				"zdnhir1-a.akamaihd.net/",
				"zdnhir2-a.akamaihd.net/",
				"zdnhir3-a.akamaihd.net/",
				"zdnhir4-a.akamaihd.net/",
				"socialslots.cdn.zynga.com/"
			};

			Glb.staticAssetHostNewUrls = new string[]
			{
				overrideWebGLCDN
			};
		}

		SlotGameData.baseUrl = Glb.fixupStaticAssetHostUrl(SlotGameData.baseUrl);
		LobbyOption.baseUrl = Glb.fixupStaticAssetHostUrl(LobbyOption.baseUrl);
		Quest.baseUrl = Glb.fixupStaticAssetHostUrl(Quest.baseUrl);
		Glb.bundleBaseUrl = Glb.fixupStaticAssetHostUrl(Glb.bundleBaseUrl);
		Glb.mobileStreamingAssetsUrl = Glb.fixupStaticAssetHostUrl(Glb.mobileStreamingAssetsUrl);

		if (Glb.staticAssetHosts != null)
		{
			for(int i=0; i < Glb.staticAssetHosts.Length; i++)
			{
				Glb.staticAssetHosts[i] = Glb.fixupStaticAssetHostUrl(Glb.staticAssetHosts[i]);
			}
		}

		// Separately we need to fixup two lingering S3 URLs
		string overrideWebGLS3URL = liveData.getString("WEBGL_S3_URL_OVERRIDE", "");
		if (overrideWebGLS3URL != "")
		{
			Debug.Log("kk Using LiveData WEBGL_S3_URL_OVERRIDE: " + overrideWebGLS3URL);

			// TEMP - to fix CORS pathing problem (should fix this in server-provided URL once we settle on new domain)
			CarouselData.versionUrl = CarouselData.versionUrl.Replace("socialslots-aws-s3-assets.s3.amazonaws.com/", overrideWebGLS3URL);
			LoLa.versionUrl = LoLa.versionUrl.Replace("socialslots-aws-s3-assets.s3.amazonaws.com/", overrideWebGLS3URL);
		}

		// TEMP - CarouselData URL gets reloaded multiple times, causes caching issues on WebGL (HIR-61440); so bust it here for now
		// New Ticket to properly fix carousel versioning: HIR-62159
		CarouselData.versionUrl += "?buster="+System.DateTime.Now.Ticks;

		Debug.Log("kk CarouselData.versionUrl: " + CarouselData.versionUrl);
		Debug.Log("kk LoLa.versionUrl: new url: " + LoLa.versionUrl);
#endif

		// Old/New CDN values need to be setup before this point...
		LoadGlobalData.setGlobalDataURL(data);


		// soft prompt limiters
		Glb.PUSH_NOTIF_SOFT_PROMPT_THROTTLE = data.getInt("push_notif_soft_prompt_throttle", 48);
		Glb.PUSH_NOTIF_SOFT_PROMPT_CAP = data.getInt("push_notif_soft_prompt_cap", 5);
		Glb.PUSH_NOTIF_SOFT_PROMPT_FREQUENCY = data.getInt("push_notif_soft_prompt_frequency", 4);

		Zynga.Slots.ZyngaConstantsGame.auth_zid = data.getString("auth_zid", "");
		Zynga.Slots.ZyngaConstantsGame.auth_user_id = data.getString("auth_user_id", "");
		Zynga.Slots.ZyngaConstantsGame.auth_user_hash = data.getString("auth_user_hash", "");
		Zynga.Slots.ZyngaConstantsGame.install_credentials = data.getString("install_credentials", "");


		// HEADS UP - These queries to get FB_GRAPH_CALL_xxx are looking for named properties in the BasicData object. There are none here!
		// (though some are defined in BasicGame.mobile_runtimes and BasicData.live_data, and PlayerData.runtime and PlayerData.live_data)
		// As a result, graphVersionString is always blank, but our calls to FacebookMember.getImageUrl( ) still seem to work fine.
		string graphVersionString = "";
#if UNITY_IPHONE
		graphVersionString = data.getString("FB_GRAPH_CALL_IOS", "");
#elif UNITY_ANDROID
		graphVersionString = data.getString("FB_GRAPH_CALL_ANDROID", "");
#elif ZYNGA_KINDLE
		graphVersionString = data.getString("FB_GRAPH_CALL_KINDLE", "");
#elif UNITY_WEBGL
		graphVersionString = data.getString("FB_GRAPH_CALL_UNITYWEB", "");
#elif UNITY_WSA_10_0 && NETFX_CORE //SMP this may need to be WSA specific
		graphVersionString = data.getString("FB_GRAPH_CALL_WINDOWS", "");
#endif
		// Appends a "/" to the version # if it is not empty
		if (!graphVersionString.Equals(""))
		{
			Zynga.Zdk.ZyngaConstants.FbGraphVersion = "v" + graphVersionString + "/";
		}
		else
		{
			Zynga.Zdk.ZyngaConstants.FbGraphVersion = graphVersionString;
		}
		Debug.Log("fb_graph_version: " + Zynga.Zdk.ZyngaConstants.FbGraphVersion);

		// This is the MOTD configuration:
#if UNITY_IPHONE
		Glb.MOTD_KEY = data.getString("ios_motd_key", Glb.MOTD_KEY).Trim();
#elif ZYNGA_KINDLE
		Glb.MOTD_KEY = data.getString("kindle_motd_key", Glb.MOTD_KEY).Trim();
#elif UNITY_ANDROID
		Glb.MOTD_KEY = data.getString("android_motd_key", Glb.MOTD_KEY).Trim();
#elif UNITY_WEBGL
		Glb.MOTD_KEY = data.getString("webgl_motd_key", Glb.MOTD_KEY).Trim();
#elif UNITY_WSA_10_0 && NETFX_CORE //SMP this may need to be WSA specific
		Glb.MOTD_KEY = data.getString("windows_motd_key", Glb.MOTD_KEY).Trim();
#endif

		SlotsPlayer.instance.mergeBonus = data.getLong("merge_bonus", 5000L); // Setting the connect amount
		Glb.mobileStreamingAssetsVersion = liveData.getInt("STREAMING_ASSET_VERSION", 0);
		PlayerPrefsCache.SetInt(Prefs.MOBILE_ASSET_STREAMING_VERSION, Glb.mobileStreamingAssetsVersion); // Set this after loading it
#if RWR
		Glb.RWR_ACTIVE_PROMO                    = liveData.getString("RWR_ACTIVE_PROMO", "");
#endif
		Glb.clientAppstoreURL                   = liveData.getString("CLIENT_APPSTORE_URL", "");
#if ZYNGA_KINDLE
		Glb.clientAppstoreURL 					= "amzn://apps/android?p=com.zynga.hititrich";
#endif
		Glb.wozSlotsClientAppstoreURL           = liveData.getString("WOZ_SLOTS_STORE_URL", "");
		Glb.devicesForceReloadOnResume          = liveData.getArray ("DEVICES_RELOAD_ON_RESUME", LiveData.EMPTY_ARRAY);
		Glb.SHOW_EARLY_ACCESS_TAG               = liveData.getBool  ("SHOW_EARLY_ACCESS_TAG", false);
		Glb.UNLOCK_ALL_GAMES                    = liveData.getBool  ("UNLOCK_ALL_GAMES", false);
		Glb.UNLOCK_ALL_GAMES_END_TIME           = liveData.getInt   ("UNLOCK_ALL_GAMES_END_TS", 0);
		Glb.UNLOCK_ALL_GAMES_START_TIME         = liveData.getInt   ("UNLOCK_ALL_GAMES_START_TS", 0);
		Glb.ENGAGEMENT_REWARD_MULTIPLIER        = liveData.getInt   ("ENGAGEMENT_REWARD_MULTIPLIER", 1);
		Glb.MAX_BET_AMOUNT                      = liveData.getLong  ("MAX_BET_AMOUNT", -1L);
		Glb.JSON_CACHE_FORMAT                   = liveData.getString("JSON_CACHING", "0");
		Glb.NEED_CREDITS_DEFAULT_PACKAGE        = liveData.getString("OOC_FALLBACK_PACKAGE", Glb.NEED_CREDITS_DEFAULT_PACKAGE);
		Glb.mobileBuyTwiceMultiplier            = liveData.getInt   ("REPURCHASE_MULTIPLIER", 1);
		Glb.STARTER_PACK_DEFAULT_PACKAGE        = liveData.getString("STARTER_PACKAGE", Glb.STARTER_PACK_DEFAULT_PACKAGE);
		Glb.STARTER_PACK_DEFAULT_BONUS          = liveData.getInt   ("STARTER_PACKAGE_BONUS", Glb.STARTER_PACK_DEFAULT_BONUS);
		Glb.BUY_PAGE_DEFAULT_PACKAGES           = liveData.getArray ("BUY_PAGE_FALLBACK_PACKAGES", Glb.BUY_PAGE_DEFAULT_PACKAGES);
		Glb.DYNAMIC_MOTD_JSON_STRING            = liveData.getString("DYNAMIC_MOTD_JSON", "");
		Glb.STATS_BYPASS                        = liveData.getBool  ("STATS_BYPASS", false);
		Glb.SHOULD_HIDE_GOOGLE_PLUS             = liveData.getBool  ("HIDE_GOOGLE_PLUS", false);
		LobbyOption.minSpinCountSorting         = liveData.getInt   ("MIN_SPINCOUNT_SORTING", 50);
		SoftwareUpdateDialog.instructionUrl     = liveData.getString("SOFTWARE_UPDATE_URL", "");
		SoftwareUpdateDialog.minVersion         = liveData.getString("SOFTWARE_UPDATE_MIN_VERSION", "");
		Glb.CHECK_PAYMENT                       = "new";//liveData.getString ("CHECK_PAYMENT", "");
		Glb.RANDOM_LOGGING_VALUE                = 1;//liveData.getInt 	("RANDOM_LOGGING_VALUE", 1);
		RateMe.MIN_PROMPT_LEVEL	                = liveData.getInt   ("MIN_RATE_APP_LEVEL", 1);
		Glb.NEW_RETRY_LOGIC						= liveData.getBool ("NEW_RETRY_LOGIC", false);
		Glb.BLACKLIST_GIFTCHEST_GAME_KEYS 		= liveData.getArray("BLACKLIST_GIFTCHEST_GAME_KEYS", Glb.BLACKLIST_GIFTCHEST_GAME_KEYS);

		// SKU-Specific Help dialog links
		Glb.HELP_LINK_SUPPORT                   = liveData.getString("HELP_LINK_SUPPORT", Glb.HELP_LINK_SUPPORT);
		Glb.HELP_LINK_TERMS                     = liveData.getString("HELP_LINK_TERMS",   Glb.HELP_LINK_TERMS);
		Glb.HELP_LINK_PRIVACY                   = liveData.getString("HELP_LINK_PRIVACY", Glb.HELP_LINK_PRIVACY);
		Glb.HELP_LINK_SMS                       = liveData.getString("HELP_LINK_SMS", Glb.HELP_LINK_SMS);

		// Symbol culling flags (master-switch and runtime-switch)
		Glb.enableSymbolCullingSystem           = liveData.getBool  ("MOBILE_SYMBOL_CULLING", true);
		Glb.enableSymbolCulling                 = liveData.getBool  ("MOBILE_SYMBOL_CULLING", true);
		Glb.autoToggleSymbolCulling             = false;

		// Watch To Earn config
		WatchToEarn.samplingThreshold           = liveData.getFloat ("W2E_SAMPLING_THRESHOLD", 1.1f);

		Glb.SUPER_WIN_THRESHOLD                 = liveData.getInt   ("SUPER_WIN_THRESHOLD", 25);
		Glb.MEGA_WIN_THRESHOLD                  = liveData.getInt   ("MEGA_WIN_TRESHOLD", 50);  // [SIC]
		Glb.LIVE_SPIN_SAFETY_TIMEOUT            = liveData.getInt   ("LIVE_SPIN_SAFETY_TIMEOUT", 20000) / 1000.0f;
		Glb.START_TIMER_WEEKLY_RACE				= liveData.getBool	("START_TIMER_WEEKLY_RACE", false);
		Glb.TIMER_INTERVAL_WEEKLY_RACE			= liveData.getLong	("TIMER_INTERVAL_WEEKLY_RACE", 30000);
		Glb.START_SECOND_TIMER_WEEKLY_RACE		= liveData.getBool	("START_SECOND_TIMER_WEEKLY_RACE", false);
		Glb.TIMER_SECOND_INTERVAL_WEEKLY_RACE	= liveData.getLong	("TIMER_SECOND_INTERVAL_WEEKLY_RACE", 15000);
		Glb.ENABLE_MACHINE_SCALER_UPDATE        = liveData.getBool("ENABLE_MACHINE_SCALER_UPDATE", false);

		// Setup Logging/Sampling
		Glb.serverLogLoadTime            = getSampling("Server Load Time Logging",   liveData.getInt("LOG_CLIENT_LOADTIME_RATE", 0));
		Glb.serverLogErrors              = getSampling("Server Error Logging",       liveData.getInt("LOG_CLIENT_ERROR_RATE", 0));
		Glb.serverLogWarnings            = getSampling("Server Warning Logging",     liveData.getInt("LOG_CLIENT_WARNING_RATE", 0));
		Glb.serverLogPurchasablePackages = getSampling("PurchasablePackage Logging", liveData.getInt("LOG_CLIENT_PACKAGES_RATE", 0));
		Glb.serverLogDeviceInfo          = getSampling("Server Device Info Logging", liveData.getInt("LOG_CLIENT_DEVICE_INFO_RATE", 0));
		Glb.serverLogPayments            = liveData.getBool("LOG_CLIENT_PAYMENTS", false);
		Glb.serverLogPushNotifications	 = liveData.getBool("LOG_CLIENT_PUSH_NOTIFICATION", false);
		Glb.logRequestError 			 = liveData.getBool("LOG_REQUEST_ERROR", true);
		Glb.logAnonAuth 				 = liveData.getBool("LOG_ANON_AUTH", false);
		Glb.logAccountStore				 = liveData.getBool("LOG_ACCOUNT_STORE", false);
		Glb.switchFbCall 				 = liveData.getBool("SWITCH_FB_CALL", false);
		Glb.showWebglEmail				 = liveData.getBool("SHOW_WEBGL_EMAIL", true);
		Glb.webglAccountSwitch			 = liveData.getBool("WEBGL_ACCOUNT_SWITCH", false);
		Glb.llapidisconnect				 = liveData.getBool("LLAPI_DISCONNECT", false);
		Glb.showVerifyEmailDialog		 = liveData.getBool("SHOW_VERIFY_EMAIL_DIALOG", false);
		Glb.showEditButton				 = liveData.getBool("SHOW_EDIT_BUTTON", false);
		Glb.migrationCounter			 = liveData.getInt("MIGRATION_COUNTER", 0);
		Glb.zisMigrateRetry				 = liveData.getInt("ZIS_MIGRATE_RETRY", 0);
		Glb.zisMigrateRetryMs			 = liveData.getInt("ZIS_MIGRATE_RETRY_MS", 0);
		Glb.zisMigrateRetryMsMultiplier  = liveData.getInt("ZIS_MIGRATE_RETRY_MS_MULTIPLIER", 0);


		// Control the level of logging we will do to the console, via GameLoginSettings 
		// value if in Editor or using a LiveData value if we are on device.
#if UNITY_EDITOR
		Debug.unityLogger.filterLogType = (LogType)(PlayerPrefsCache.GetInt(DebugPrefs.MIN_LOG_LEVEL, (int)LogType.Log));
#else
		int minLogLevelLiveDataValue = liveData.getInt("LOG_CLIENT_DEVICE_CONSOLE_MIN_LOG_LEVEL", 0);
		Debug.unityLogger.filterLogType = convertLiveDataValueToLogType(minLogLevelLiveDataValue);
#endif

		// It's safe to call this for all SKU's, even if the SKU doesn't use it.
		CollectBonusDialog.setTheme(liveData.getString("DAILY_BONUS_THEME", ""));

		// New MOTD Framework config.
		MOTDFramework.init(liveData);

		// Early access wager set override
		Glb.IS_USING_VIP_EARLY_ACCESS_WAGER_SETS = liveData.getBool("USE_VIP_EARLY_ACCESS_WAGER_SETS", false);
		Glb.IS_USING_VIP_EARLY_ACCESS_WAGER_SETS_VIP_MIN_WAGER = liveData.getBool("USE_VIP_EARLY_ACCESS_WAGER_SETS_VIP_MIN_WAGER", false);
	
		// Init the Level Up Bonus data.
		ModifyLevelupBonusMultiplierBuff.init();

		MysteryGift.init();
		CreditSweepstakes.init();
		
		// Set some Bugsnag data for recording purposes
		if (releaseStage == "none" || string.IsNullOrEmpty(releaseStage))
		{
			// Not found in config, use basic data stage name.
			Bugsnag.ReleaseStage = Glb.stageName;
		}
		Bugsnag.AddToTab("HIR", new Dictionary<string, object> {
				{"stage", Glb.stageName},
				{"server_url", Glb.dataUrl},
				{"build_tag", Glb.buildTag}}
			);

		Data.isBasicDataSet = true;
	}

	// Return full url
	public static string getFullUrl (string url)
	{
		if (!Glb.switchCdnUrl || Glb.cdnUrl == null || Glb.cdnUrl.Count == 0) { return url; }

		List<string> cdnUrl = Glb.cdnUrl[0];
		string fullUrl = null;
		if (url.StartsWith("http"))
		{
			fullUrl = url;
		}
		else
		{
			fullUrl = Glb.urlPrefix + cdnUrl[0] + url;
		}
		return fullUrl;
	}

    // Getting the CDN URL
	public static IEnumerator attemptServerRequest (string url, Dictionary<string,string> elements, string defaultErrorKey,
	string responseKey, bool forceGameRefresh= true, string cacheFileName = "", 
	bool shouldLoadCache=true )
    {
		Data.data = null;
		if (!(url.Contains ("akamai") || url.Contains ("cdn")) || Glb.switchCdnUrl == false) {
			yield return RoutineRunner.instance.StartCoroutine (Server.attemptRequest (url, elements, defaultErrorKey, responseKey, forceGameRefresh, cacheFileName, shouldLoadCache));
		} else {
			List<string> cdnUrl = Glb.cdnUrl [0];
			string fullUrl = getFullUrl (url);
			yield return RoutineRunner.instance.StartCoroutine (Server.attemptRequest (fullUrl, elements, defaultErrorKey, responseKey, forceGameRefresh, cacheFileName, shouldLoadCache));

			Data.data = Server.getResponseData (responseKey);

			if (Data.data == null) {
				if (url.Contains (Glb.urlPrefix + cdnUrl [0])) {
					url = url.Replace (Glb.urlPrefix + cdnUrl [0], "");
				}
				string[] cdnUrlSub = Glb.cdnUrl [1].ToArray ();
				fullUrl = Glb.urlPrefix + cdnUrlSub [0] + url;
				yield return RoutineRunner.instance.StartCoroutine (Server.attemptRequest (fullUrl, elements, defaultErrorKey, responseKey, forceGameRefresh, cacheFileName, shouldLoadCache));														  				   
				Data.data = Server.getResponseData (responseKey);
				yield break;
			} else {
				yield break;
			}
		}
		Data.data = Server.getResponseData(responseKey);
		yield break;
    }

	// The AssetBundleManager will call us when it's ready to use, then we can start caching, playing audio, etc.
	public static void onAssetBundleManagerInitialized()
	{
		// Start downloading/caching some bundles now, loads things in FIFO order
		AssetBundleManager.downloadAndCacheBundle(AssetBundleManager.INITIALIZATION_BUNDLE_NAME, true, blockingLoadingScreen:true);
		AssetBundleManager.downloadAndCacheBundle("main_img_lobby_options", skipMapping:true, blockingLoadingScreen:true);
		AssetBundleManager.downloadAndCacheBundle("main_lobby_basic", true, blockingLoadingScreen:true);

		// Play the lobby music and ambience as soon as possible.
		Audio.playMusic("spookylandingintro");
		Audio.switchMusicKey("spookylandingloop");
		Audio.play("lobbyambienceloop0");
	}

	// This function setters are used to prevent any accidents in setting the login data,
	// and also act as a springboard for any data setting functions that need to be called.
	public static void setLoginData(JSON data)
	{
		if (data == null)
		{
			Debug.LogError("Somehow got to setLoginData with it being null.");
			return;
		}

		Debug.LogFormat("Player JSON: {0}", data.ToString());

		// Adding in apple name and email
		player = data.getJSON("player");
		Debug.LogFormat("Player JSON Only: {0}", player.ToString());
		if (SlotsPlayer.getPreferences().HasKey(SocialManager.appleName))
		{
			//ZisData.AppleName = SlotsPlayer.getPreferences().GetString(SocialManager.appleName);
		}
		else
		{
			//ZisData.AppleName = player.getString("first_name", "") + " " + player.getString("last_name", "");
		}
		if (SlotsPlayer.getPreferences().HasKey(SocialManager.fbName))
		{
			//ZisData.FacebookName = SlotsPlayer.getPreferences().GetString(SocialManager.fbName);
		}
		else
		{
			//ZisData.FacebookName = ZisData.AppleName;
		}
		//ZisData.AppleEmail = player.getString("email", "");
		Debug.LogFormat("AppleLogin: ZisData applename {0} appleemail {1}", ZisData.AppleName, ZisData.AppleEmail);
		Glb.isNew = data.getBool("is_new", false);
		Glb.showEmailOptIn = data.getLong("opt_in_reward", 0);
		// Any first-thing data initialization for login data.
		// Always set the access key, even if player data has already
		// been set by a previous login, since we need this for server requests to work.
		Server.accessKey = data.getString("session.access_key", "");

		AssetBundleManager.setLazyLoadFeaturesData(data.getJSON("lazy_load"));

		if (isPlayerDataSet)
		{
			// Prevent double-logins from both being processed on the client.
			return;
		}

		//Get Streak sale current purchase index.
		JSON streakSaleJson = player.getJSON("streak_sale");
		if (streakSaleJson != null)
		{
			StreakSaleManager.purchaseIndex = streakSaleJson.getInt("current_package",0);
		}

		login = data;

		//Debug.Log("INITIALIZE PLAYER");
		PlayerResource.resetStaticClassData();

#if !ZYNGA_PRODUCTION
		DesyncAction.onLogin();
#endif
		// During transition we want to pull EOS into Experiment Manager as well.
		ExperimentManager.populateEos(login.getJSON("eos"));

		SlotsWagerSets.overwriteWagerData(player.getJsonArray("wager_overrides"));

		isPlayerDataSet = true;

		SlotsPlayer.instance.init();

		Bugsnag.LeaveBreadcrumb("About to load friends list");

		FacebookMember.createPlayerFacebookMember();
		
		JSON royalRushData = login.getJSON("player.royal_rush");
		if (royalRushData != null && ExperimentWrapper.RoyalRush.isInExperiment)
		{
			RoyalRushEvent.init(royalRushData);
		}

		// Register the player as a network user before we send off a request.
		SocialMember.addNetworkUser(SlotsPlayer.instance.networkID, SlotsPlayer.instance.socialMember);

		SocialManager.Instance.CheckEmailVerifiedOnGameLoad();
		SocialManager.Instance.setEmailOptIn();

		ModifyDailyBonusReducedTimerBuff.init();	// moved from setBasicData because this is dependent on ExperimentManager being initialized

		WatchToEarn.initializeAdAgencies(); // We want to experiment gate these so we need to initialize AFTER loading experiments.

		LuckyDealDialog.initLoginData(data);

		//setup network data once slots player has initialized
		setupNetworkProfile();

		//set dialog transitions
		Dialog.setTransitionData(ExperimentWrapper.DialogTransitions.getExperimentData());

		// Tell the Feature Director that we have loaded the data.
		FeatureDirector.recieveLoginData(data);

		// We do not use zRuntime to set the early access game.
		// Set the early access game by setting the lobby game experiment to variant 4.
		// Make sure only one game is set to early access at a time, or the server will pick one randomly.
		// The server finds the early access game and sends the early access data.
		// The server gets the game key, min bet multiplier, and min vip level from the SCAT experiments tab.
		// The client does not actually use the min vip level, this will be removed eventually.
		Glb.EARLY_ACCESS_MIN_BET_MULTIPLIER = login.getLong("vip_early_access_min_bet_multiplier", 1L);
		Glb.EARLY_ACCESS_WAGER_SET = login.getString("vip_early_access_wager_set", "");
		
		LobbyOption.freePreviewLocalizationKey = data.getString("player.free_preview_localization_key", "");

		Bugsnag.LeaveBreadcrumb("About to log our session count");

		// We've just loaded our player data, so technically our game session should begin now:
		StatsManager.Instance.LogCount("start_session", "session_count");
		StatsManager.Instance.LogCount("start_session", "client_version", Glb.clientVersion);

		// Setup some error stuff in advance
		Server.makeErrorBaseStrings();

		// Watch2Earn varible config.
		JSON w2e = data.getJSON("mobile_w2e");
		if (w2e != null)
		{
			WatchToEarn.isServerEnabled = w2e.getBool("enabled", false);
			WatchToEarn.rewardAmount = w2e.getLong("reward_amount", 0);
			WatchToEarn.motdSeen = (w2e.getInt("motd_count", 0) != 0);
			WatchToEarn.inventory = w2e.getInt("inventory", 0);
			//WatchToEarn.maxViewsPlacementIds = w2e.getStringIntDict("inventory_per_source");
		}

		if (w2e != null)
		{
			// Wrapping this with a null check since the ttl value needs to exist or this throws an NRE
			// Using a seperate wrapper so that we can keep all (future) data timer inits in one place.
			RefreshableData.setDataTimer("mobile_w2e_inventory_ttl", w2e.getInt("inventory_ttl", 0), new string[] { "mobile_w2e_inventory", "mobile_w2e_reward_amount", "mobile_w2e_inventory_per_source" });
		}

		//Note: presence of zis block in json does not mean this is apple login
		//fb calls also have this. Should check the flag isAppleLoggedIn or the pref kLoginPreference
        //to check if its apple login.
		if (data.getJSON("zis") != null)
		{
			Debug.LogFormat("AppleLogin: zis data  {0}", data.getJSON("zis").ToString());
			JSON zisData = data.getJSON("zis");
			//ZisData.loadZisData(zisData);
		}
		else
		{
			Debug.Log("AppleLogin: zis data  is empty");
            //if this is empty, the apple login failed. update the global state to reflect it.
			SlotsPlayer.IsAppleLoggedIn = false;
		}

		// Get progressive jackpot blacklist,
		// which effectively ignores the progressive jackpot data
		// from SCAT for any pools in this list.
		ProgressiveJackpot.blacklist = data.getStringArray("pjp_poolkey_blacklist");

		// // Even if no quest is active, we still register for the basic collectible quest delegates,
		// // so we will receive a collectible event if the quest is enabled during this session,
		// // allowing us to refresh the game to make the client recognize the event.
		// TODD: We need a different way to do this now, since we have multiple quest types possible.
		// Quest.registerCollectibleEventDelegates();

		string activeQuestKey = liveData.getString("QUESTS_CURRENT_ACTIVE_QUEST", "");

		// note this section must execute even if quest is expired because there are 
		// 'quest-over' dialogs that must need to be brought up in that case

		switch (activeQuestKey)
		{
			case "daily_challenge":
				if(ExperimentWrapper.DailyChallengeQuest2.isInExperiment || ExperimentWrapper.DailyChallengeQuest.isInExperiment)
				{
					const string questJsonKey = "quests.daily_challenge";
					JSON dailyChallengeJson = data.getJSON(questJsonKey);

					Quest.activeQuest = new DailyChallenge(activeQuestKey, liveData.getInt("QUESTS_ACTIVE_QUEST_END_DATE", 0), dailyChallengeJson);

					if (dailyChallengeJson == null)
					{
						Debug.LogErrorFormat("Can't find {0} in JSON: {1}",questJsonKey,data.ToString());
						break;
					}

					SlotsPlayer.instance.questCollectibles = DailyChallenge.GetCollectibleAmountFromJSON(dailyChallengeJson);
				}
				break;
		}
		
		StarterDialog.saleTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + login.getInt("player.tickers.starter_pack_timer.time_remaining", 0));
		LifecycleDialog.saleTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + login.getInt("player.tickers.lifecycle_sale_timer.time_remaining", 0));

		CreditsEconomy.setMultiplier(login.getLong("player.economy_multiplier", -1));

		// Update player identification data for Bugsnag
		if (SlotsPlayer.instance != null && SlotsPlayer.instance.socialMember != null)
		{
			SocialMember m = SlotsPlayer.instance.socialMember;
			Bugsnag.SetUser(m.zId, m.fullName, SlotsPlayer.instance.networkID);
			Bugsnag.AddToTab("HIR", new Dictionary<string, object> {
					{"coins", SlotsPlayer.creditAmount.ToString()},
					{"xp", SlotsPlayer.instance.xp.amount.ToString()},
					{"level", m.experienceLevel.ToString()},
					{"vip_level", m.vipLevel.ToString()},
					{"vip_tier", SlotsPlayer.instance.vipSpendTier.ToString()}});
		}
	}

	public static void setupNetworkProfile()
	{
		if (NetworkProfileFeature.instance.isEnabled && SlotsPlayer.instance.socialMember != null)
		{
		    NetworkProfileFeature.instance.getPlayerProfile(SlotsPlayer.instance.socialMember);
		}

		if (NetworkAchievements.isEnabled)
		{
			JSON[] levels = login.getJsonArray("achievement_levels");
			if (levels != null && levels.Length > 0)
			{
				AchievementLevel.populateAll(levels);
			}

			JSON achievementJSON = login.getJSON("achievements");
			if (achievementJSON == null)
			{
				Debug.LogErrorFormat("Data.cs -- setLoginData -- achievementJSON was null.");
				// Forcing the feature off. If we didnt get any achievements then the feature is useless and should be off.
				NetworkAchievements.forceOff = true; 
				// MCC -- returning here as the stuff further down requires achievements to be setup.
				return; 
				
			}
			else
			{
				NetworkAchievements.populateAll(achievementJSON);
				NetworkAchievements.registerEventDelegates();
				long achievementScore = login.getLong("achievement_score", 0L);
				SlotsPlayer.instance.socialMember.setupAchievements(achievementJSON, achievementScore);
			}
			
			if (NetworkAchievements.rewardsEnabled)
			{
				JSON[] rarities = login.getJsonArray("achievement_rarities");
				if (rarities != null && rarities.Length > 0)
				{
					NetworkAchievements.populateRarities(rarities);
				}
				
				JSON rewardsCollected = login.getJSON("achievement_rewards_collected");
				if (rewardsCollected != null)
				{
					NetworkAchievements.populateCollectedAchievements(rewardsCollected);
				}

				int backfillAmount = login.getInt("achievement_backfill_amount", 0);
				if (backfillAmount > 0)
				{
					NetworkAchievements.setBackfillAmount(backfillAmount);
				}
			}
		}
	}

	// This function setters are used to prevent any accidents in setting the login data,
	// and also act as a springboard for any data setting functions that need to be called.
	public static void setGlobalData(JSON data)
	{
		if (isGlobalDataSet)
		{
			// Prevent double-logins from both being processed on the client.
			return;
		}

		Data.isGlobalDataSet = true;

		// Any first-thing data initialization for global data.
		Localize.populateAll(data.getJsonArray("messages"));
		Collectables.populateAll(data.getJSON("collectibles"));
		Collectables.populateChallengePackData(data.getJSON("collection_pack_details"));
		PowerupsManager.populateStaticData(data.getJsonArray("powerups"));
		setConstant(ref Glb.DIALOG_ANIM_TIME, data, "dialog_anim_time");
		setConstant(ref Glb.VIP_POINTS_PER_INVITED_FRIEND, data, "vip_points_per_invited_friend");
		setConstant(ref Glb.VIP_POINTS_PER_DAY, data, "vip_points_per_day");
		setConstant(ref Glb.BIG_WIN_THRESHOLD, data, "big_win_threshold");
		setConstant(ref Glb.CHALLENGE_BONUS_CREDITS, data, "challenge_bonus_credits");
		setConstant(ref Glb.GIFTING_CREDITS, data, "gifting_credits");
		setConstant(ref Glb.DAILY_BONUS_MAX_CHIP_COUNT, data, "bonus_game_menu_max_credits");
		setConstant(ref Glb.CREDIT_SEND_RETURN_AMOUNT, data, "credit_send_return_amount");
		setConstant(ref Glb.LOW_CHIPS_MULTIPLIER, data, "low_chips_threshold");
		setConstant(ref Glb.ROLLUP_MULTIPLIER, data, "rollup_multiplier");
		setConstant(ref Glb.INITIAL_CREDITS, data, "initial_credits");
		setConstant(ref Glb.NUMBER_OF_RANDOM_FACTS, data, "number_of_random_facts");
		setConstant(ref Glb.INFINITE_SPIN_TIME_ALLOWED, data, "default_mobile_spin_wait_time");
		setConstant(ref Glb.GLOBAL_BASE_WAGER, data, "global_base_wager");
		setConstant(ref Glb.NETWORK_PROFILE_NEW_MOTD_MIN_LEVEL, data, "network_profile_motd_new_min_level");
		setConstant(ref Glb.NETWORK_FRIENDS_MOTD_MIN_LEVEL, data, "network_friends_motd_min_level");
		setConstant(ref Glb.NETWORK_PROFILE_MOTD_MIN_LEVEL, data, "network_profile_motd_min_level");
		setConstant(ref Glb.ACHIEVEMENT_MOTD_MIN_LEVEL, data, "achievements_motd_min_level");
		setConstant(ref Glb.MOBILE_AUTO_CLOSE_DIALOG_SECONDS, data, "mobile_auto_close_dialog_seconds");
		
		// Since this variable is of an enum type, we can't use the setConstant function to set it directly.
		int dailyBonusMode = 0;
		setConstant(ref dailyBonusMode, data, "daily_bonus_mode");
		if (dailyBonusMode < 0 || dailyBonusMode > Glb.DAILY_BONUS_MODE_MAX)
		{
			dailyBonusMode = 0;
		}
		Glb.DAILY_BONUS_MODE = (DailyBonus)dailyBonusMode;  // Cast it to the enum value.

		setConstant(ref Glb.SHOW_TUTORIALS, data, "show_tutorials");
		setConstant(ref Glb.USE_SWRVE_DATA, data, "use_swrve_data");
		setConstant(ref Glb.ALLOW_CREDITS_PURCHASE, data, "allow_credits_purchase");

		setConstant(ref Glb.LIKE_DAILY_VIP_POINTS_REWARD, data, "like_daily_vip_points_reward");
		setConstant(ref Glb.LIKE_INCENTIVE_VIP_POINTS_REWARD, data, "like_daily_vip_points_reward");
		setConstant(ref Glb.DEFAULT_BANKROLL_BET_PCT, data, "default_bankroll_bet_pct");
		setConstant(ref Glb.UNLOCK_HIGH_LIMIT_LOBBY, data, "unlock_high_limit_lobby");
		setConstant(ref Glb.MOBILE_RESET_HOURS, data, "mobile_reset_hours");
		setConstant(ref Glb.PROGRESSIVE_JACKPOT_UPDATE, data, "progressive_jackpot_update");

		setConstant(ref Glb.STARTERPACK_SEC_AFTER_INSTALL, data, "starterpack_sec_after_install");
		setConstant(ref Glb.STARTERPACK_SEC_REPEAT_PERIOD, data, "starterpack_sec_repeat_period");

		setConstant(ref Glb.GAMECENTER_SEC_AFTER_INSTALL, data, "gamecenter_sec_after_install");
		setConstant(ref Glb.GAMECENTER_SEC_REPEAT_PERIOD, data, "gamecenter_sec_repeat_period");

		setConstant(ref Glb.SURFACING_QUEST_RACE_TO_RICHES_WIN, data, "surfacing_quest_race_to_riches_win");
		setConstant(ref Glb.SURFACING_MYSTERY_GIFT_WIN, data, "surfacing_mystery_gift_win");
		setConstant(ref Glb.SURFACING_PROGRESSIVE_WIN, data, "surfacing_progressive_win");

		setConstant(ref Glb.UNLOCK_ALL_GAMES_MOTD_COOL_DOWN, data, "unlock_all_games_motd_cool_down_in_days");
		setConstant(ref Glb.RAINY_DAY_MOTD_MIN_LEVEL, data, "rainy_day_motd_min_level");
		setConstant(ref Glb.LINKED_VIP_MIN_LEVEL, data, "linked_vip_min_level");
		setConstant(ref Glb.SEND_GIFT_COOLDOWN, data, "send_gift_cooldown");
		setConstant(ref Glb.POPCORN_SALE_RTL_SHOW_COOLDOWN, data, "popcorn_sale_rtl_show_cooldown");
		setConstant(ref Glb.MAX_VOLTAGE_MIN_LEVEL, data, "max_voltage_min_level");

		Glb.popcornSalePackages = data.getJsonArray("popcorn_sale_packages");  // for EconomyManager
		Glb.richPassPackages = data.getJsonArray("rich_pass_packages");
		Glb.premiumSlicePackages = data.getJsonArray("premium_slice_packages");
		Glb.bonusGamePackages = data.getJsonArray("bonus_game_packages");
		Glb.progressiveJackpots = data.getJsonArray("progressive_jackpots");    // for GameLoader.cs

		InviteRewards.populateAll(data.getJsonArray("invite_reward_tiers"));
		MOTDDialogData.populateAll(data.getJsonArray("new_game_dialogues"));

		TimerCredits.populateAll(data.getJsonArray("timers"));

		GlobalTimer.populateAll(data.getJsonArray("timers"));

		SlotResourceMap.populateAll();  // Must be done before populating LobbyGameGroups.

		SlotLicense.populateAll(data.getJsonArray("licenses"));
		LobbyGameGroup.populateAll(data.getJsonArray("slots_game_groups"));
		GameUnlockData.populateAll(data.getJsonArray("variant_based_xp_levels")); // Must be done after populating LobbyGameGroups.

		VIPLevel.populateAll(data.getJsonArray("vip_new_levels"));

		BonusGamePaytable.populateAll(data.getJsonArray("bonus_game_pay_tables"));
		SlotsWagerMultiplier.populateAll(data.getJsonArray("slots_wager_multipliers"));

		Glb.wagerUnlockData = new Dictionary<string, JSON[]>();

		//Special wager unlock levels for certain EOS experiment variants
		//MMURPHY -- global wager data is obsolete with 2019 reprice.  This code remains for fallback purposes
		JSON[] experimentGlobalMaxWagers = data.getJsonArray("global_max_wagers");
		for (int i = 0; i < experimentGlobalMaxWagers.Length; i++)
		{
			string variantName = experimentGlobalMaxWagers[i].getString("variant", "");
			if (!Glb.wagerUnlockData.ContainsKey(variantName))
			{
				Glb.wagerUnlockData.Add(variantName, experimentGlobalMaxWagers[i].getJsonArray("wagers"));
			}
			else
			{
				Debug.LogErrorFormat("Trying to double add this global wager variant {0}", variantName);
			}
		}

		//Still want to grab "global_wagers" which is the old data we've always grabbed from
		Glb.wagerUnlockData.Add("defaultWagers", data.getJsonArray("global_wagers"));

		SlotsWagerSets.populateAllWagerSets(data.getJsonArray("wager_sets"));


		// This data is paired with "True Vegas" data so we need the right one.
		foreach (JSON test in data.getJsonArray("collect_bonus"))
		{
			if (test.getString("key_name", "none") == "sir_bonus")
			{
				CollectBonusDialog.setBonusData(test);
			}
		}

		Payline.populateAll(data.getJsonArray("pay_lines"));
		PaylineSet.populateAll(data.getJsonArray("pay_line_sets"));

		SlotSymbolData.populateAll(data.getJsonArray("slots_symbols"));

		AudioChannel.populateAll(data.getJsonArray("audio_tags"));
		SlotGameData.populateDefaultSoundMap(data.getJSON("audio_defaults"));
		AudioInfo.populateAll(data.getJsonArray("audio"));
		PlaylistInfo.populateAll(data.getJsonArray("audio_lists"));

		BonusPool.populateAll(data.getJsonArray("bonus_pools"));

		GlobalSkuData.populateAll(data.getJsonArray ("sku"));

		CustomPlayerData.populateFields(data.getJsonArray("custom_data_fields"));
    
		Audio.setup();
		
		LoLaLobby.rawLobbyData = data.getJsonArray("lobbies");

		if (!MobileUIUtil.isAllowedDevice)
		{
			// If the device isn't allowed, show a friendly message and quit the app.
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, Localize.text("incompatible_title"),
					D.MESSAGE, Localize.text("incompatible_description_{0}", CommonDeviceInfo.deviceName),
					D.REASON, "invalid-device",
					D.IS_WAITING, true  // Don't show any buttons to close it.
				),
				SchedulerPriority.PriorityType.BLOCKING
			);
			return;
		}

		Com.HitItRich.Feature.VirtualPets.VirtualPetsFeature.populateTreats(data.getJsonArray("virtual_pet_treats"));
	}

	// Sets a int constant
	private static void setConstant(ref int constant, JSON jsonData, string jsonName)
	{
		constant = jsonData.getInt("constants." + jsonName, constant);
	}

	// Sets a float constant
	private static void setConstant(ref float constant, JSON jsonData, string jsonName)
	{
		constant = jsonData.getFloat("constants." + jsonName, constant);
	}

	// Sets a long constant
	private static void setConstant(ref long constant, JSON jsonData, string jsonName)
	{
		constant = jsonData.getLong("constants." + jsonName, constant);
	}

	// Sets a bool constant
	private static void setConstant(ref bool constant, JSON jsonData, string jsonName)
	{
		constant = jsonData.getBool("constants." + jsonName, constant);
	}

	// Sets a string constant
	private static void setConstant(ref string constant, JSON jsonData, string jsonName)
	{
		constant = jsonData.getString("constants." + jsonName, constant);
	}

	// Sets a SpecialWinSurfacing constant
	private static void setConstant(ref SpecialWinSurfacing constant, JSON jsonData, string jsonName)
	{
		int val = -1;
		val = jsonData.getInt("constants." + jsonName, val);
		if (System.Enum.IsDefined(typeof(SpecialWinSurfacing), val))
		{
			constant = (SpecialWinSurfacing)val;
		}
	}

	// Sets a string init
	private static void setInit(ref string init, JSON jsonData, string jsonName)
	{
		init = jsonData.getString("init." + jsonName, init);
	}

	// This function will show a visible message on screen on non-production stages only.
	// It should only be called when there is a data problem, explaining what the problem is
	// and how the problem will adversely affect the game. This will help QA understand how to
	// properly report an issue instead of reporting the symptoms as a result of bad data.
	// Note that the message does not have to be localized since it is only for dev/QA purposes.
	public static void showIssue(string message, bool shouldShowDialogInEditor = true, string option2 = "", DialogBase.AnswerDelegate callback = null)
	{
		Debug.LogError("Data Problem: " + message);
		if (!debugMode || (Application.isEditor && !shouldShowDialogInEditor))
		{
			// Silently log an error on production, just in case this happens there.
			return;
		}

		Dict dict = Dict.create(
				D.TITLE, "Data Problem",
				D.MESSAGE, message,
				D.REASON, "data-problem"
			);

		if (!string.IsNullOrEmpty(option2))
		{
			dict.merge(D.OPTION2, option2);
		}
		if (callback != null)
		{
			dict.merge(D.CALLBACK, callback);
		}

		GenericDialog.showDialog(
			dict,
			SchedulerPriority.PriorityType.IMMEDIATE
		);
	}

	// Helper function to determine if a switch should be enabled, based on probability.
	public static bool getSampling(string description, int level)
	{
		bool sampling;
		int instanceLevel = 0;
		if (level == 0)
		{
			sampling = false;
		}
		else if (level >= Glb.SERVER_LOG_SAMPLE_RANGE)
		{
			sampling = true;
		}
		else
		{
			// Statistically sample our server logging, so we can get info from a fraction of our users:
			instanceLevel = Random.Range(0, Glb.SERVER_LOG_SAMPLE_RANGE);
			sampling = (instanceLevel < level);
		}

		string msg = string.Format("Data::getSampling - {0} level: {1} Instance: {2} Logging: {3}", description, level, instanceLevel, sampling.ToString());
		Bugsnag.LeaveBreadcrumb(msg);
		return sampling;
	}
	
	// Convert the logging value from live data into a LogType
	private static LogType convertLiveDataValueToLogType(int liveDataValue)
	{
		switch (liveDataValue)
		{
			case 0:
				return LogType.Log;
			case 1:
				return LogType.Warning;
			case 2:
				return LogType.Error;
		}

		Debug.LogWarning("Data.convertLiveDataValueToLogType() - Unknown value liveDataValue = " + liveDataValue + "; falling back to all logs!");
		return LogType.Log;
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	public static bool hasPlayerData
	{
		get { return player != null; }
	}

	public static bool hasLoginData
	{
		get { return login != null; }
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		login = null;
		player = null;
		_configJSON = null;
		isPlayerDataSet = false;
		isGlobalDataSet = false;
		isBasicDataSet = false;
		canvasBasedConfig = null;
		canvasBasedBasicData = null;
		canvasBasedPlayerData = null;
	}
}
