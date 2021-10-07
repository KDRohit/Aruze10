//
//  AssetBundleManager.cs
//
//  Created by Niklas Borglund
//  http://github.com/NiklasBorglund/AssetBundleCreator
// 	MIT License
//
// This class keeps track of all the downloaded asset bundles. 
// It contains functions to add, destroy and unload a bundle


using System.Diagnostics;
using Zynga.Core.Util;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable 0618 // Not gonna use the bundle hash 128 method
using Com.Scheduler;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using System.Collections;

public delegate void AssetLoadDelegate(string assetPath, Object obj, Dict data = null);
public delegate void AssetFailDelegate(string assetPath, Dict data = null);

// Feature classes can add a delegate to the lazy loaded callback list for any special logic needed per bundle
// e.g. for path_to_riches, it's likely we'll just want to cache the bundles specific to the theme
public delegate void LoadMissingBundle();

public class AssetBundleManager : TICoroutineMonoBehaviour, IResetGame
{
	[SerializeField]
	private bool downloadsEnabled = true;

	[SerializeField]
	private string bundleManifestName;
	
	[SerializeField]
	private bool verbose = true; // Whether to emit base level of logging messages.

	[SerializeField]
	private bool extraVerbose = false; // Whether to emit extra verbose level of logging messages.
	
	// Bundle version to use when one doesn't yet exist.
	private const int BASE_VERSION = 0;
	private const int MAX_CONCURRENT_DOWNLOADS = 4;
	
	private const float UNUSED_CHECK_TIME = 30.0f;

	private const string VERSION_PREF = "AssetBundleVersion";
	private const string LAZY_LOAD_LIST = "lazyloadlist";

#if UNITY_ANDROID
	public const string PLATFORM = "android";
#elif UNITY_IOS
	public const string PLATFORM = "ios";
#elif UNITY_WSA_10_0
    public const string PLATFORM = "wsaplayer";
#elif UNITY_WEBGL
    public const string PLATFORM = "webgl";
#else
    public const string PLATFORM = "Expected iOS, Android, Windows, or WebGL platform!";
#endif

	private static bool isCancelled = false;

	// How many premature errors we should report to crittercism before we shut up
	private static int numPrematureErrorsToReport = 10;
	//Used for tracking download time of a full game
	private static int currentTrackStep = 0;

	private static bool readSlotventureThemes = false;
	
	private static JSON lazyLoadServerResponse = null;
	
	private static object fakeCallerObject = new object(); // used when no caller object provided to load() function
	
	// list of features that need to be loaded immediately, and not lazily (as removed from the lazy loaded list)
	// this is ZRT controlled, and server authoritative
	private static List<string> lazyLoadedOverrides = new List<string> { };
	//this list is added to by the server, not overwritten
	private static List<string> missingFeaturesToLazyLoad =  new List<string>{};
	private static List<string> lazyBundlesReadyForNextSession = new List<string>();
	private static List<string> missingBundles = new List<string>();
	private static List<BundleType> assetBundleTypes = new List<BundleType>();
	
	private static Dictionary<string, LoadMissingBundle> lazyBundleHandlers = new Dictionary<string, LoadMissingBundle>()
	{
		{ "network_achievement", NetworkAchievements.onLoadBundleRequest },
		{ "sin_city_strip", SinCityLobby.onLoadBundleRequest },
		{ "land_of_oz", LOZLobby.onLoadBundleRequest },
		{ "max_voltage", MaxVoltageLobbyHIR.onLoadBundleRequest },
		{ "vip_lobby", VIPLobbyHIRRevamp.onLoadBundleRequest },
		{ "royal_rush", RoyalRushEvent.onLoadBundleRequest }
	};
	
	// we embed the bundle filesize into its filename with a -sz prefix, like "somebundle-hd-8d142345-sz10420.bundlev2"
	public const string bundleSizePrefix = "-sz";
	public const string INITIALIZATION_BUNDLE_NAME = "initialization"; // Special-purpose "initialization" bundle
	public const string bundleV2Extension = ".bundlev2";
	public const string EMBEDDED_BUNDLES_PATH = "Data/embedded_bundles";

	public const int MAX_RETRY_COUNT = 3;
	
	public static AssetBundleManifest manifestV2; 
	
	public static string BundleManifestName => Instance.bundleManifestName;
	
	public static bool useAssetBundles = true;
	public static bool useLocalBundles = false;
	public static bool inMemoryDanger = false; //Shut off for the current session for lazy loading bundles if we're close to crashing due to memory
	
	// dictionary of bundle names associated with a feature name. server receives the list of features that
	// haven't been cached, to determine what needs to be lazily loaded
	public static Dictionary<string, string> lazyLoadedBundleToFeatureMap = new Dictionary<string, string>()
	{
		{ "race_to_riches_50s_drive_win"			, 		"path_to_riches_50s_drive_win" },
		{ "race_to_riches_derby"					, 		"path_to_riches_derby" },
		{ "race_to_riches_martian_mayhem"			, 		"path_to_riches_martian_mayhem" },
		{ "race_to_riches_new_years"				, 		"path_to_riches_new_years" },
		{ "race_to_riches_spring_fling"				, 		"path_to_riches_spring_fling" },
		{ "race_to_riches_st_patrick"				, 		"path_to_riches_st_patrick" },
		{ "race_to_riches_valentine"				, 		"path_to_riches_valentine" },
		{ "race_to_riches_vault"					, 		"path_to_riches_vault" },
		{ "race_to_riches_xmas"						, 		"path_to_riches_xmas" },
		{ "network_achievement"						,		"network_achievement" },
		{ "feature_land_of_oz"						, 		"land_of_oz" },
		{ "sin_city_strip"							, 		"sin_city_strip" },
		{ "partner_powerup"							, 		"partner_powerup" },
		{ "ticket_tumbler"							, 		"ticket_tumbler" },
		{ "max_voltage"								,		"max_voltage" },
		{ "main_snd_max_voltage"					,		"max_voltage" },
		{ "vip_revamp_token_ui"						,		"vip_lobby" },
		{ "royal_rush"								,		"royal_rush" },
		{ "network_profile"							,		"network_profile" },
		{ "network_profile_lobby"					,		"network_profile"},
		{ "robust_challenges"						,		"robust_challenges"},
		{ "robust_challenges_slots_ui"				,		"robust_challenges_slots_ui"},
		{ "slotventures_common"           			,       "slotventures_common" },
		{ "slotventures_common_audio"           	,		"slotventures_common_audio" },
		{ "lucky_deal"								,		"lucky_deal"}
	};

	// Returns a list of bundles that are embedded.
	public static string[] embeddedBundlesList
	{
		get
		{
			if (_embeddedBundlesList == null)
			{
				TextAsset embeddedBundleFile = SkuResources.loadSkuSpecificEmbeddedResourceText(EMBEDDED_BUNDLES_PATH);
				if (embeddedBundleFile != null && embeddedBundleFile.text != null)
				{
					JSON embeddedBundleJSON = new JSON(embeddedBundleFile.text);

					if (embeddedBundleJSON == null || !embeddedBundleJSON.isValid)
					{
						Debug.LogWarning("AssetBundleDownloader.embeddedBundlesList found invalid data in " + EMBEDDED_BUNDLES_PATH);
						_embeddedBundlesList = new string[0];
					}
					else
					{
						_embeddedBundlesList = embeddedBundleJSON.getStringArray("embedded");
					}
				}
				else
				{
					Debug.LogWarning("AssetBundleDownloader.embeddedBundlesList cannot find file at " + EMBEDDED_BUNDLES_PATH);
					_embeddedBundlesList = new string[0];
				}
			}
			return _embeddedBundlesList;
		}
	}
	
	private static string[] _embeddedBundlesList = null;

	private float trackStartTime = 0f;
	
	private bool assetManifestV2Ready = false;
	private bool downloadingLazyLoadBundle = false; //Used to control only downloading one lazy bundle at a time for memory management

	private Dictionary<string, AssetBundleContainer> loadedAssetBundles = new Dictionary<string, AssetBundleContainer>();
	private Dictionary<string, int> assetBundleToHostIndexMap = new Dictionary<string, int>();
	
	// list of queued downloads in user-requested FIFO order; dependencies will be queued before parent to avoid deadlocks
	private List<AssetBundleDownloader> assetBundlesToDownload = new List<AssetBundleDownloader>();
	private List<string> failedAssetBundleDownloads = new List<string>();
	
	// Rotating CDN hosts.
	private string[] bundleBaseUrlsList = null;
	
	public bool isDownloading = false;
	
	public ulong bytesDownloadedThisSession = 0; // reset to 0 when game resets
	public ulong bytesDownloadedAllSessions = 0; // does not reset
	public int LoadedBundleCount => loadedAssetBundles.Count;
	private static int maxConcurrents;

	#region Singleton

	private static AssetBundleManager instance = null;
	public static AssetBundleManager Instance 
	{
		get 
		{
			// If we haven't been externally initialized via the InitializationManager, log each premature access
			if (Application.isPlaying && !AssetBundleManagerInit.Instance.hasInitialized)
			{
				Debug.LogError("Premature usage of AssetBundleManager.Instance!");
				if (!Application.isEditor && numPrematureErrorsToReport > 0)
				{
					try { throw new System.Exception("Premature usage of AssetBundleManager.Instance!"); }
					catch(System.Exception e) { Bugsnag.Notify(e); }
					numPrematureErrorsToReport--;
				}
			}

			// but we still have to run, so create the instance (it won't support ZID-based variant whitelisting thoughs)
			if (instance == null) { createInstance(); }

			return instance;
		}
	}

	public static bool hasInstance()
	{
		return instance != null;
	}

	// This singleton is supposed to be created via AssetBundleManagerInit, instead of on demand
	// but a premature usage of the instance can still cause this to happen
	public static void createInstance()
	{
		Bugsnag.LeaveBreadcrumb("AssetBundleManager::CreateInstance()");

		// instance already exists? return...
		if (instance != null)
		{
			Debug.LogError("AssetBundleManager.instance already exists");
			return;
		}

		GameObject go = new GameObject("AssetBundleManager");
		instance = go.AddComponent<AssetBundleManager>();
		if (Application.isPlaying)
		{
			DontDestroyOnLoad(go);
		}

		if (Application.isEditor)
		{
			// These prefs should only exist while playing through the Editor.
			useAssetBundles = PlayerPrefsCache.GetInt(DebugPrefs.USE_ASSET_BUNDLES, 0) != 0;
			useLocalBundles = PlayerPrefsCache.GetInt(DebugPrefs.USE_LOCAL_BUNDLES, 0) != 0;
		}

		// livedata check for concurrent overrides
		maxConcurrents = Data.liveData != null ? Data.liveData.getInt("BUNDLE_CONCURRENT_DOWNLOAD_LIMIT", 4) : MAX_CONCURRENT_DOWNLOADS;

		// in case someone borks it to 0
		maxConcurrents = Mathf.Max(1, maxConcurrents);

		// load our bundle-content-manifest (includes bundles -> asset mappings)
		instance.loadAssetToBundleMapV2();

		instance.assetBundlesToDownload.Clear();
		instance.loadedAssetBundles.Clear();

		invalidateOnBundleVersion();

		// Check for unused AssetBundles every few seconds
		instance.InvokeRepeating("unloadUnusedBundles", UNUSED_CHECK_TIME, UNUSED_CHECK_TIME);

		// Now that we've processed manifests & whitelists, our user code can start caching...
		Data.onAssetBundleManagerInitialized();
	}

	private string getBundleListReport(List<string> bundleList, string listName)
	{
		string status = "";
		if (bundleList != null)
		{
			status += listName + " count : " + bundleList.Count + "\n";
			status += string.Join("\n", bundleList.ToArray());
		}
		else
		{
			status += listName + " is NULL.";
		}
		status += "\n\n";

		return status;
	}

	public string getStatusReport()
	{
		StringBuilder status = new StringBuilder("", 32000);

		status.Append(getBundleListReport(missingBundles, "missingBundles"));

		status.Append(getBundleListReport(missingFeaturesToLazyLoad, "missingFeaturesToLazyLoad"));

		status.Append(getBundleListReport(lazyBundlesReadyForNextSession, "lazyBundlesReadyForNextSession"));

		status.Append(getBundleListReport(failedAssetBundleDownloads, "failedAssetBundleDownloads"));

		status.Append(getBundleListReport(lazyLoadedOverrides, "lazyLoadedOverrides"));

		if (lazyLoadServerResponse != null)
		{
			status.Append("lazyLoadServerResponse : \n" + lazyLoadServerResponse.ToString() + "\n\n");
		}
		else
		{
			status.Append("lazyLoadServerResponse is NULL and has not been recieved.\n");
		}

		if (manifestV2 != null && manifestV2.baseBundleNameToFullBundleNameDict != null)
		{
			status.Append("Doing Cache Check\n");
			foreach (string bundleName in manifestV2.baseBundleNameToFullBundleNameDict.Keys)
			{
				status.Append(bundleName + " isCached = " + isBundleCached(bundleName)  + "\n");
			}
		}
		else
		{
			status.Append("manifestV2 is NULL. No bundles for you.\n\n");
		}

		return status.ToString();
	}

	public static void invalidateOnBundleVersion()
	{
		// Clean up the Unity cache if the client version is new
		if (PlayerPrefsCache.GetString(VERSION_PREF, Glb.clientVersion) != Glb.clientVersion)
		{
			clearUnityAssetBundleCache();
			PlayerPrefsCache.SetString(VERSION_PREF, Glb.clientVersion);
		}
	}

#endregion //Singleton

#if UNITY_EDITOR
#region TestInterface
	/// Lightweight init for testing purposes.
	public static void initForTesting(bool useABundles = true, bool useLBundles = false)
	{
		Debug.Log("initForTesting");
		useAssetBundles = useABundles;
		useLocalBundles = useLBundles;
		Instance.loadAssetToBundleMapV2();
	}

	/// Testing wrappers
	public static string[] testGetAllUrlsForBundle(string bundleName)
	{
		return Instance.getAllUrlsForBundle(bundleName);
	}

	public static string testGetUrlForBundle(string bundleName)
	{
		return Instance.getUrlForBundle(bundleName);
	}

	public static string[] testGetBundleBaseUrlsList()
	{
		return Instance.bundleBaseUrlsList;
	}

	public static void testDestroyInstance()
	{
		instance = null;
	}
#endregion
#endif

#region Debugging
	/// <summary>
	/// Private method to wrap Debug.Log to enable/disable local logging verbosity.
	/// </summary>
	private void Log(string msg)
	{
		bool testing = Application.isEditor && !Application.isPlaying;
		if (testing || useAssetBundles && Data.debugMode && verbose)
		{
			Debug.Log(msg);
		}
	}

	/// <summary>
	/// Private method to wrap Debug.Log to enable/disable local logging verbosity.
	/// </summary>
	private void LogExtra(string msg)
	{
		bool testing = Application.isEditor && !Application.isPlaying;
		if (testing || useAssetBundles && Data.debugMode && verbose && extraVerbose)
		{
			Debug.Log(msg);
		}
	}
#endregion

	private static void addSlotventureBundlesToLazyLoad()
	{
		if (readSlotventureThemes)
		{
			return;
		}
			
		//add slotventure bundles to lazy load list
		string[] bundleNames = Data.liveData.getArray("SLOTVENTURES_AVAILABLE_THEMES", new string[] {});
		foreach (string bundleName in bundleNames)
		{
			if (string.IsNullOrEmpty(bundleName))
			{
				continue;
			}
			string bundle = "slotventures_" + bundleName.ToLower().Trim();
			lazyLoadedBundleToFeatureMap.Add(bundle, bundle);
		}

		readSlotventureThemes = true;
	}

	private void loadAssetToBundleMapV2()
	{
#if !UNITY_EDITOR && UNITY_WEBGL
		// WebGL stores bundles in the browser indexDB cache, which has an async interface,
		// which means we cannot do a synchronous check on if anything is cached - so instead:
		// We check every asset bundle to see if it is cached so that the plugin cache we are
		// using will probably have the correct cached status by the time the application
		// asks for it. And if not, a value of false is still fine as a fallback.
		
		if (manifestV2 != null && manifestV2.baseBundleNameToFullBundleNameDict != null)
		{
			Instance.LogExtra("Asset bundle caching pre-check for WebGL.");
			foreach (string bundleName in manifestV2.baseBundleNameToFullBundleNameDict.Keys)
			{
				// The result for this first call is meaningless, but it will queue up an
				// async process to cache the appropriate cached state in subsequent calls.
				// See CachedXHRExtensions.jspre for more, including the storage of async results.
				isBundleCached(bundleName);
			}
		}
#endif

		assetManifestV2Ready = true;
	}

	/// <summary>
	/// Private method to get bundle name for a resource.  Returns empty string if no mapping is found.
	/// </summary>
	private string _getBundleNameForResource(string resourcePath)
	{
		if (!assetManifestV2Ready)
		{
			Debug.LogWarning($"Asking for asset {resourcePath} but asset manifest is not yet ready");
		}

		// Check V2 manifest
		if (manifestV2 != null)
		{
			string bundle = manifestV2.getBundleNameForResource( resourcePath );
			if (!string.IsNullOrEmpty(bundle))
			{
				return bundle;
			}
		}
		
#if UNITY_EDITOR
		if (!useAssetBundles && !string.IsNullOrEmpty(resourcePath))
		{
			string projectRelativePath = getProjectRelativePathFromResourcePath(resourcePath);
			if (AssetDatabase.LoadAssetAtPath(projectRelativePath, typeof(UnityEngine.Object)) != null)
			{
				return AssetDatabase.GetImplicitAssetBundleName(projectRelativePath);	
			}
		}
#endif

		// got nothing, return empty string
		return "";
	}

	/// <summary>
	/// Get bundle name in which <paramref name="resourcePath"/> is found.
	/// </summary>
	public static string getBundleNameForResource(string resourcePath)
	{
		return Instance._getBundleNameForResource(resourcePath);
	}

	private void initBundleBaseUrls()
	{
		if (bundleBaseUrlsList == null)
		{
			LogExtra("initBundleBaseUrls");

			if (!Data.isBasicDataSet)
			{
				Bugsnag.LeaveBreadcrumb("Trying to init bundle base urls before basic data is set");
			}

			string baseUrl = Glb.bundleBaseUrl;
			if (string.IsNullOrEmpty(baseUrl))
			{
				baseUrl = NGUILoader.HARDCODED_CDN;
			}
			
#if UNITY_ANDROID
			// Note: 'bundle_base_url' in this server php file hardcodes 'ios' for the platform, here:
			//https://github.com/spookycool/casino_server/blob/1bdca1b5289d56093031e32570fbea7c290a47cd/public/server/basic_game_data.php
			//'bundle_base_url' => URL_PREFIX . IOS_BUNDLE_BASE_URL,
			baseUrl = baseUrl.Replace("ios", "android");
#elif UNITY_WSA_10_0
			baseUrl = baseUrl.Replace("ios", "windows");
#elif UNITY_WEBGL
			baseUrl = baseUrl.Replace("ios", "webgl");
#endif
			bundleBaseUrlsList = Server.findAllStaticUrls(baseUrl);
		}
	}

	private string[] getAllUrlsForBundle(string bundleName)
	{

		if (!bundleName.EndsWith(bundleV2Extension))
		{
			Debug.LogErrorFormat("Bundle {0} does not have correct file extension", bundleName);
			return new string[1] { "" };
		}

		initBundleBaseUrls();

		if (bundleName.Contains('/'))
		{
			bundleName = bundleName.Replace('/', '_');
		}

		// If using local bundles, force a local filesystem URL to our bundles...
		if (useLocalBundles)
		{
			// compose a URL in the form of:  "file:///Users/kkralian/HIR-mobile/build/bundlesv2/ios/bundlename.bundlev2"
			return new string[] { "file://" + Application.dataPath + "/../../build/bundlesv2/" + PLATFORM + "/" + bundleName };
		}

		string[] bundleUrlsList = new string[bundleBaseUrlsList.Length];
	    
		for (int i = 0; i < bundleBaseUrlsList.Length; i++)
		{
			bundleUrlsList[i] = bundleBaseUrlsList[i] + bundleName;
		}
		
		return bundleUrlsList;
	}

	private string getUrlForBundle(string bundleName)
	{
		if (!bundleName.EndsWith(bundleV2Extension))
		{
			return "";
		}
		
		string[] urls = getAllUrlsForBundle(bundleName);

		// Check if have already have loaded this bundle from a specific URL; if so, re-use it
		int hostIndex = -1;
		if (assetBundleToHostIndexMap.TryGetValue(bundleName, out hostIndex) && hostIndex != -1)
		{
			LogExtra($"Bundle {bundleName} Using associated url {urls[hostIndex]}");
			return urls[hostIndex];
		}

		// Check if any of the bundle URL's are already cached, if so, use it
		int bundleVersion = getBundleVersion(bundleName);
		for (int i=0; i < urls.Length; i++)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			if (CachedXHRExtensions.IsCached(urls[i]))
			{
#else
			if (Caching.IsVersionCached(urls[i], bundleVersion))
			{
#endif
				LogExtra($"Bundle {bundleName} Using cached url {urls[i]}");
				assetBundleToHostIndexMap[bundleName] = i;
				return urls[i];
			}
		}

		// Otherwise, start with a deterministic (not random) URL based on the bundle name.
		// We use the same URL for a given bundle unless errors force a URL to rotate (important for WebGL IndexedDB caching)
		hostIndex = Mathf.Abs(bundleName.GetHashCode()) % urls.Length;
		assetBundleToHostIndexMap[bundleName] = hostIndex;
		
		LogExtra($"Bundle {bundleName} Using rotating url {urls[hostIndex]}");
		return urls[hostIndex];
	}

	// Gets the next URL for a bundle, given it's previously used URL
	private string getNextUrlForBundle(string bundleName, string previousUrl)
	{
		// Invalidate cache for this bundle, so that new host for retry will be used.
		incrementBundleVersion(bundleName);

		// get this list of URLs for this bundle and return the URL following 'previousUrl' (wrap as needed)
		string[] urls = getAllUrlsForBundle(bundleName);
		int prevIndex = System.Array.FindIndex(urls, url => url == previousUrl);
		int newIndex = (prevIndex + 1) % urls.Length;
		string newUrl = urls[newIndex];

		// Track associated host index.
		assetBundleToHostIndexMap[bundleName] = newIndex;

		return newUrl;
	}

	/// <summary>
	/// Get locally-stored bundle version.
	/// </summary>
	public static int getBundleVersion(string bundleName)
	{
		string key = bundleName + "_version";
		int version = BASE_VERSION;
		if (PlayerPrefsCache.HasKey(key))
		{
			version = PlayerPrefsCache.GetInt(key, version);
		}
		else
		{
			PlayerPrefsCache.SetInt(key, version);
			PlayerPrefsCache.Save();
		}
		return version;
	}

	/// <summary>
	/// Increment locally-stored bundle version to force re-download.
	/// </summary>
	private static void incrementBundleVersion(string bundleName)
	{
		string key = bundleName + "_version";
		int version = BASE_VERSION;
		if (PlayerPrefsCache.HasKey(key))
		{
			version = PlayerPrefsCache.GetInt(key, version) + 1;
		}
		PlayerPrefsCache.SetInt(key, version);
		Instance.LogExtra($"Inc {bundleName} bundle version to {version}");
		PlayerPrefsCache.Save();
	}

	/// <summary>
	/// Clears Unity's file caches (both Native and WebGL implementations)
	/// </summary>
	public static void clearUnityAssetBundleCache()
	{
		Caching.ClearCache(); // Clears native Unity cache
#if UNITY_WEBGL
		CachedXHRExtensions.CleanCache(); // Clears WebGL's IndexedDB caches
#endif
	}

	public static void downloadAndCacheBundleWithCallback(string bundleName, bool keepLoaded = false, bool lazyLoaded = false, AssetLoadDelegate successCallback = null, AssetFailDelegate failCallback = null, Dict data = null, bool isSkippingMapping = false, bool blockingLoadingScreen = false)
	{
#if UNITY_EDITOR
		if(bundleName.FastEndsWith(".bundle"))
		{
			Debug.LogErrorFormat("Bundle names ending in .bundle are no longer valid; instead use lowercase basename without extension: {0}",bundleName);
			return;
		}

		// caching only works with bundles enabled...
		if (!useAssetBundles)
		{
			return;
		}
#endif
		
		Instance.LogExtra($"AssetBundleManager.downloadAndCacheBundle {bundleName}");

		if (manifestV2 != null)
		{
			bundleName = manifestV2.getFullBundleNameFromBaseBundleName( bundleName ) ?? bundleName;
		}

		// Download and/or load cached asset bundle, and (optionally) keep memory resident
		AssetBundleDownloader download = Instance.downloadAssetBundle(
			bundleName,
			successCallback:successCallback,
			failCallback:failCallback,
			data:data,
			isSkippingMapping:isSkippingMapping,
			blockingLoadingScreen:blockingLoadingScreen);
		
		//downloadAssetBundle() can return an existing download, so we are potentially overwriting
		//the original settings here or setting them needlessly.
		if (download != null)
		{
			download.keepLoaded = keepLoaded;
			download.lazyLoaded = lazyLoaded;
		}
	}
	
	/// <summary>
	/// Downloads and/or loads the bundle.
	/// </summary>
	/// <param name="bundleName">should be the base bundle name, lowercase, without any file extension</param>
	/// <param name="keepLoaded">causes forLevel of the AssetBundleContainer to be set to -1</param>
	/// <param name="lazyLoaded">if this download is to pre-warm the feature cache for next session</param>
	/// <param name="skipMapping">if false, will load ALL assets from the bundle and create a lookup table in AssetBundleMapping</param>
	public static void downloadAndCacheBundle(string bundleName, bool keepLoaded = false, bool lazyLoaded = false, bool skipMapping = false, bool blockingLoadingScreen = false)
	{
	#if UNITY_EDITOR
		if(bundleName.FastEndsWith(".bundle"))
		{
			Debug.LogErrorFormat("Bundle names ending in .bundle are no longer valid; instead use lowercase basename without extension: {0}",bundleName);
			return;
		}

		// caching only works with bundles enabled...
		if (!useAssetBundles)
		{
			return;
		}
	#endif
		
		Instance.LogExtra($"AssetBundleManager.downloadAndCacheBundle {bundleName}");

		if (lazyLoadedBundleToFeatureMap.ContainsKey(bundleName))
		{
			string storedLazyList = PlayerPrefsCache.GetString(LAZY_LOAD_LIST, "" );
			string featureName = lazyLoadedBundleToFeatureMap[bundleName];
						
			if (storedLazyList.Contains(featureName))
			{
				StatsManager.Instance.LogCount("deferred_loading", featureName, "feature_downloaded");
				storedLazyList = storedLazyList.Replace(featureName, "");
				PlayerPrefsCache.SetString(LAZY_LOAD_LIST, storedLazyList);
			}
		}

		if (manifestV2 != null)
		{
			bundleName = manifestV2.getFullBundleNameFromBaseBundleName( bundleName ) ?? bundleName;
		}

		// Download and/or load cached asset bundle, and (optionally) keep memory resident
		AssetBundleDownloader download = Instance.downloadAssetBundle(bundleName, isLazyLoaded: lazyLoaded, isSkippingMapping: skipMapping, blockingLoadingScreen: blockingLoadingScreen);
		
		//downloadAssetBundle() can return an existing download, and isSkippingMapping is set when new downloads
		//are created, so we are potentially overwriting the original settings here or setting them needlessly.
		if (download != null)
		{
			download.keepLoaded = keepLoaded;
			download.lazyLoaded = lazyLoaded;
			download.isSkippingMapping = skipMapping;
		}
	}

	/// <summary>
	/// Returns true if bundle is cached or local.
	/// </summary>
	public static bool isBundleCached(string bundleName)
	{
		bool isCached = false;
		if (useLocalBundles || !useAssetBundles)
		{
			isCached = true;
		}
		else
		{
			if (manifestV2 != null)
			{
				bundleName = manifestV2.getFullBundleNameFromBaseBundleName( bundleName ) ?? bundleName;
			}

			string url = Instance.getUrlForBundle(bundleName);
			if (string.IsNullOrEmpty(url))
			{
				// If there is no valid url to download the bundle from, let's return true, in the sense that there is
				// nothing to go download.
				isCached = true;
			}
			else
			{
#if UNITY_WEBGL && !UNITY_EDITOR
				isCached = CachedXHRExtensions.IsCached(url);
#else
				isCached = Caching.IsVersionCached(url, getBundleVersion(bundleName));
#endif
			}
		}

		Instance.LogExtra($"AssetBundleManager.isCached {bundleName}: {isCached}");
		return isCached;
	}

	public static bool isValidBundle(string bundleName)
	{
		string qualifiedBundle = "";
		if (manifestV2 != null)
		{
			qualifiedBundle = manifestV2.getFullBundleNameFromBaseBundleName( bundleName );
		}
		
#if UNITY_EDITOR
		//Since the manifest won't be read in when playing in the editor with bundles off, check if the bundle exists in the project itself
		if (!useAssetBundles)
		{
			string[] assetsInBundle = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
			return assetsInBundle.Length > 0;
		}
#endif

		if (string.IsNullOrEmpty(qualifiedBundle))
		{
			return false;
		}

		return true;
	}

	/// <summary>
	///   Returns a list of all the current cached bundles based on the ASYNC bundle loading
	/// </summary>
	public static List<string> getLoadedBundles()
	{
		addSlotventureBundlesToLazyLoad();
		
		missingBundles = new List<string>();

		foreach (KeyValuePair<string, string> bundleEntry in lazyLoadedBundleToFeatureMap)
		{
			if (!isBundleCached(bundleEntry.Key) && !missingBundles.Contains(bundleEntry.Value))
			{
				missingBundles.Add(bundleEntry.Value);
			}
		}

		if (missingBundles.Count > 0)
		{
			Server.registerEventDelegate("load_features", setLazyLoadFeaturesData, true);
		}
		return missingBundles;
	}

	/// <summary>
	///   Server handler for loading missing feature bundles
	/// </summary>
	public static void loadMissingFeatures()
	{
		if (!Data.liveData.getBool("LAZY_LOAD_BUNDLES", false) || !ExperimentWrapper.LazyLoadBundles.isInExperiment)
		{
			//If we're not using lazy loading then just return immediately
			return;
		}

		if (missingFeaturesToLazyLoad == null || lazyLoadedOverrides == null)
		{
			Debug.LogError("load missing features exited missingFeaturesToLazyLoad or lazyLoadedOverrides is null");
			return;
		}

		logMissedDownloads();

		List<string> loadingFeatures = new List<string>();
		
		foreach (string feature in missingFeaturesToLazyLoad)
		{
			if (!lazyLoadedBundleToFeatureMap.ContainsValue(feature))
			{
				continue;
			}

			if (!loadingFeatures.Contains(feature))
			{
				loadingFeatures.Add(feature);
			}

			if (lazyBundleHandlers.ContainsKey(feature))
			{
				lazyBundleHandlers[feature]();

				if (!lazyBundlesReadyForNextSession.Contains(feature))
				{
					lazyBundlesReadyForNextSession.Add(feature);
				}
			}
			else
			{
				foreach (KeyValuePair<string, string> bundleEntry in lazyLoadedBundleToFeatureMap)
				{
					if (bundleEntry.Value != feature)
					{
						continue;
					}

					if (!lazyBundlesReadyForNextSession.Contains(feature)) 
					{
						lazyBundlesReadyForNextSession.Add(feature);
					}
					
					downloadAndCacheBundle(bundleEntry.Key, false, true);
				}
			}
		}

		PlayerPrefsCache.SetString(LAZY_LOAD_LIST, string.Join(",", loadingFeatures.ToArray()));
	}

	private static void logMissedDownloads()
	{
		// log the stat if there were any features that were supposed to be downloaded before this happened
		string[] missedLoads = PlayerPrefsCache.GetString(LAZY_LOAD_LIST, "").Split(',');
		for (int i = 0; i < missedLoads.Length; ++i)
		{
			string feature = missedLoads[i].Replace(",", "");
			if (lazyLoadedBundleToFeatureMap.ContainsValue(feature.Replace(" ", "")))
			{
				StatsManager.Instance.LogCount("deferred_loading", missedLoads[i], "feature_not_downloaded");
			}
		}
	}

	public static void setLazyLoadFeaturesData(JSON response)
	{
		if (Data.debugMode)
		{
			lazyLoadServerResponse = response;
		}

		if (response == null)
		{
			return;
		}

		string[] nextSess = response.getStringArray("next_session");
		string[] currSess = response.getStringArray("current_session");

		int index = 0;

		while(currSess != null && index < currSess.Length)
		{
			if (!lazyLoadedOverrides.Contains(currSess[index]))
			{
				lazyLoadedOverrides.Add(currSess[index]);
			}
			++index;
		}

		index = 0;
		while (nextSess != null && index < nextSess.Length)
		{
			if (!missingFeaturesToLazyLoad.Contains(nextSess[index]))
			{
				missingFeaturesToLazyLoad.Add(nextSess[index]);
			}
			++index;
		}
	}

	public static bool shouldLazyLoadBundle(string bundleName)
	{
		if
		(
			lazyLoadingExperimentHasBundle(bundleName) &&
			lazyLoadedBundleToFeatureMap.ContainsKey(bundleName) &&
 			missingFeaturesToLazyLoad != null &&
			missingFeaturesToLazyLoad.Contains(lazyLoadedBundleToFeatureMap[bundleName])
		)
		{
			return true;
		}
		return false;
	}

	private static bool lazyLoadingExperimentHasBundle(string bundleName)
	{
		return ExperimentWrapper.LazyLoadBundles.isInExperiment && ExperimentWrapper.LazyLoadBundles.hasBundle(bundleName);
	}
	
	/// <summary>
	///   Returns true if bundle name should be lazily loaded
	/// </summary>
	public static bool hasLazyBundle(string bundleName)
	{ 
		Regex reg = new Regex(@"([a-zA-Z_]+)\-.*");
		bundleName = reg.Replace(bundleName, "$1");
		return lazyLoadedBundleToFeatureMap.ContainsKey(bundleName);
	}

	public static bool hasLazyOverride(string bundleName)
	{
		if (lazyLoadedOverrides != null)
		{
			return lazyLoadedOverrides.Contains(bundleName);
		}
		return false;
	}

	/// <summary>
	///   Returns true by default. However if lazy load bundles is enabled, and the bundle is in the lazy
	///   load list, it will return true if the bundle is cached already. Otherwise the bundle is not ready,
	///   and needs to be downloaded
	/// </summary>
	public static bool isBundleReady(string bundleName)
	{
		if (Data.liveData.getBool("LAZY_LOAD_BUNDLES", false) && hasLazyBundle(bundleName))
		{
			// we just lazy loaded this, it won't be ready until next reload //SMP huh?
			if (lazyBundlesReadyForNextSession.Contains(bundleName))
			{
				return false;
			}

			return isBundleCached(bundleName);
		}
		return true;
	}
	
	public static bool isResourceInInitializationBundle(string resourcePath)
	{
		if (string.IsNullOrEmpty(resourcePath))
		{
			return false;
		}
		
		// Check manifest for asset bundle containing resourcePath.
		string bundleName = getBundleNameForResource(resourcePath);
		if (string.IsNullOrEmpty(bundleName))
		{
			return false;
		}

		bool isInitializationBundle = bundleName.FastStartsWith(INITIALIZATION_BUNDLE_NAME);
		return isInitializationBundle;
	}

	/// <summary>
	/// Monolithic callback-based routine for loading arbitrary resources, including Textures and Prefabs
	/// 1) Using the manifest, try to look up a bundle that has the resourcePath
	/// 2) If the bundle starts with "initialization" load the resource from the Mega Bundle
	/// 3) If the bundle is NOT already loaded, delegate to downloadAssetBundle() to load the resource
	/// 4) If the bundle IS loaded but has null mappings, remove it and reload it via downloadAssetBundle()
	/// 5) If the bundle has mappings, but they weren't populated and we're not skipping mapping, rebuild them
	/// 6) Attempt to load the resource from the bundle via its assetBundleMapping
	/// 7) If the asset was loaded and we have a success callback, call it and "touch" the bundle so we know it's in use
	/// 8) In all other cases either fallback to loading from Resources or fail
	/// </summary>
	/// <param name="caller"></param>
	/// <param name="resourcePath"></param>
	/// <param name="successCallback"></param>
	/// <param name="failCallback"></param>
	/// <param name="data"></param>
	/// <param name="isLazy"></param>
	/// <param name="isSkippingMapping"></param>
	/// <param name="fileExtension"></param>
	public static void load(object caller, string resourcePath, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data = null, bool isLazy = false, bool isSkippingMapping = false, string fileExtension = "", bool blockingLoadingScreen = false)
	{
		Instance.LogExtra($"AssetBundleManager.load {resourcePath}");

		if (string.IsNullOrEmpty(resourcePath))
		{
			Debug.LogWarning("AssetBundleManager.load: passed in empty resourcePath to load");
			if (failCallback != null)
			{
				failCallback(resourcePath, data);
			}
			return;
		}

		// Check manifest for asset bundle containing resourcePath.
		string bundleName = getBundleNameForResource(resourcePath);
		if (string.IsNullOrEmpty(bundleName))
		{
			// Asset is not in a bundle, so load it from Resources.
			fallbackToLoadFromResources(resourcePath, successCallback, failCallback, data);
			return;
		}

		// Check if the mega initialization bundle is being used, and 
		// log a warning if we are trying to load from it here, since
		// we should be using SkuResources.getObjectFromMegaBundle instead
		if (bundleName.FastStartsWith(INITIALIZATION_BUNDLE_NAME))
		{
			Debug.LogWarning("AssetBundleManager.load() - resourcePath = " + resourcePath + "; is part of the INITIALIZATION BUNDLE, you should use SkuResources.getObjectFromMegaBundle to load this instead. "
			                 + "If this is a dynamic path that could come from a specific bundle or the initialization bundle you should first check using AssetBundleManager.isResourceInInitializationBundle first.  Loading the asset correctly "
			                 + "but the calling code should be cleaned up.");
				
			if (successCallback == null)
			{
				Instance.Log(
					$"AssetBundleManager.load() - resourcePath = {resourcePath}: failed, bundle = {bundleName}; successCallback is NULL!");
			}
			else
			{
				Object megaBundleObj = SkuResources.getObjectFromMegaBundle<Object>(resourcePath);
				successCallback(resourcePath, megaBundleObj, data);
			}

			return;
		}
		
		// Check in list of currently loaded bundles to see if bundle containing resourcePath is already loaded.
		// If not, start loading/downloading the asset bundle.
		AssetBundleContainer bundleContainer;
		if (!Instance.loadedAssetBundles.TryGetValue(bundleName, out bundleContainer))
		{
			Instance.downloadAssetBundle(bundleName, resourcePath, successCallback, failCallback, data, caller, isSkippingMapping:isSkippingMapping, fileExtension:fileExtension, blockingLoadingScreen:blockingLoadingScreen);
			return;
		}

		//Value from Dictionary can be null even if key exists
		if (bundleContainer == null)
		{
#if UNITY_EDITOR
			// Surface these in the editor more aggressively
			Debug.LogError(
				$"Unexpected failure to get bundled asset {resourcePath + fileExtension}, bundle {bundleName} is null.");
#endif
			
			fallbackToLoadFromResources(resourcePath, successCallback, failCallback, data);
			return;
		}
		
		//Reload the bundle if the assetBundleMapping is null
		if (bundleContainer.assetBundleMapping == null) 
		{
			Instance.loadedAssetBundles.Remove(bundleName);
			Instance.downloadAssetBundle(bundleName, resourcePath, successCallback, failCallback, data, caller, isSkippingMapping, fileExtension, blockingLoadingScreen);
			return;
		}

		//Build our bundle map for the already loaded bundle if it was previously loaded but skipped building the asset map
		if (bundleContainer.assetBundleMapping.isSkippingLoadAllAssets && !isSkippingMapping)
		{
			if(Data.debugMode) {
				Debug.LogWarning($"AssetBundleManager::load() called with isSkippingMapping=false on existing unmapped bundle={bundleContainer.bundleName} for resource={resourcePath}");
			}
			
			bundleContainer.assetBundleMapping.rebuildBundleMap();
		}
		
		Object obj = bundleContainer.assetBundleMapping.getAsset(resourcePath, fileExtension);
		if (obj == null)
		{
#if UNITY_EDITOR
			// Surface these in the editor more aggressively
			Debug.LogError(
				$"Unexpected failure to get bundled asset {resourcePath + fileExtension} in bundle {bundleName}.");
#endif
			Instance.Log($"AssetBundleManager.load {resourcePath}: warning, bundle {bundleName} doesn't contain object");
			fallbackToLoadFromResources(resourcePath, successCallback, failCallback, data);
			return;
		}
		
		if (successCallback != null && caller != null)
		{
			Instance.LogExtra($"AssetBundleManager.load {resourcePath}: success, bundle {bundleName} already loaded");
			successCallback(resourcePath, obj, data);
			bundleContainer.touch(); //bundle is still in use...
			return;
		}

		Instance.Log(
			caller == null
				? $"AssetBundleManager.load {resourcePath}: failed, bundle {bundleName} object with callback is null!"
				: $"AssetBundleManager.load {resourcePath}: failed, bundle {bundleName} callback is null!");
	}

	/// <summary>
	/// Load a resource, either from an asset bundle or from Resources, downloading the required asset bundle if
	/// necessary.
	/// </summary>
	/// <param name="successCallback">Delegate called once the resource is found and available.</param>
	/// <param name="failCallback">Delegate called if the resource cannot be found or loaded or on an error.</param>
	public static void load(string resourcePath, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data = null, bool isLazy = false, bool isSkippingMapping = false, string fileExtension = "", bool blockingLoadingScreen = false)
	{
		// Since caller didn't provide a caller object, use a substitute one
		load(fakeCallerObject, resourcePath, successCallback, failCallback, data, isLazy, isSkippingMapping, fileExtension, blockingLoadingScreen);
	}

	// Attempt to load the object immediately, return null if that fails.
	public static Object loadImmediately(string resourcePath, string fileExtension = "")
	{
		Instance.LogExtra($"AssetBundleManager.loadImmediately {resourcePath}, {fileExtension}");

		if (string.IsNullOrEmpty(resourcePath))
		{
			Debug.LogWarning("AssetBundleManager.loadImmediately: passed in empty resourcePath to load");
			return null;
		}

		// Check manifest for asset bundle containing resourcePath (if we've been properly initialized)
		if (hasInstance())
		{
			string bundleName = getBundleNameForResource(resourcePath);
			if (!string.IsNullOrEmpty(bundleName))
			{
				// Check in list of currently loaded bundles to see if bundle containing resourcePath is already loaded.
				AssetBundleContainer bundleContainer;
				if (Instance.loadedAssetBundles.TryGetValue(bundleName, out bundleContainer))
				{
					Object asset = bundleContainer.assetBundleMapping.getAsset(resourcePath, fileExtension);
					if (asset==null)
					{
						Instance.Log(
							$"AssetBundleManager.loadImmediately {resourcePath}: failed, bundle {bundleName} doesn't contain object");	
					}
					else
					{
						Instance.LogExtra(
							$"AssetBundleManager.loadImmediately {resourcePath}: in already loaded bundle {bundleName}");
						return asset;
					}
				}
			}
		}

		if (NGUILoader.instance != null && NGUILoader.instance.initialBundle != null)
		{
			Object asset = NGUILoader.instance.initialBundle.LoadAsset(resourcePath);
			if (asset != null)
			{
				return asset;
			}
		}
		
		return Resources.Load(resourcePath);
	}

	public static void fallbackToLoadFromResources(string resourcePath, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data)
	{
		if (string.IsNullOrEmpty(resourcePath))
		{
			Instance.LogExtra("AssetBundleManager.load: passed in empty resourcePath to load, this is normal when caching");
			if (failCallback != null)
			{
				failCallback(resourcePath, data);
			}
			return;
		}

		if (NGUILoader.instance != null && NGUILoader.instance.initialBundle != null)
		{
			Object asset = NGUILoader.instance.initialBundle.LoadAsset(resourcePath);
			if (asset != null)
			{
				Instance.LogExtra($"AssetBundleManager.load {resourcePath}: success, found in Initialization bundle");
				if (successCallback != null)
				{
					successCallback(resourcePath, asset, data);
					return;
				}
			}
		}

		// Attempt standard Resources loading...
		Object obj = Resources.Load(resourcePath);
		if (obj != null)
		{
			Instance.LogExtra($"AssetBundleManager.load {resourcePath}: success, found in Resources");
			if (successCallback != null)
			{
				successCallback(resourcePath, obj, data);
				return;
			}
		}

#if UNITY_EDITOR
		// Attempt to load non-resource assets that normally get bundled (in-editor only)
		// Best that we only do this while bundles are disabled, so we don't confuse bundled vs non-bundled modes
		if (!useAssetBundles)
		{
			obj = loadLooseMarkedForBundleAsset(resourcePath);
			if (obj != null)
			{
				Instance.LogExtra($"AssetBundleManager.load {resourcePath}: success, found 'ToBundle' asset: ");
				if (successCallback != null)
				{
					successCallback(resourcePath, obj, data);
					return;
				}
			}
		}
#endif

		// Couldn't find the asset
		Instance.LogExtra(
			$"AssetBundleManager.load {resourcePath}: failed: not found in Resources, ToBundle, or any bundle");
		
		if (failCallback != null)
		{
			failCallback(resourcePath, data);
		}
	}

#if UNITY_EDITOR
	// Try to load a loose asset that's intended to be bundled, but for convenience (in-editor) we can load it directly.
	// This respects BundleTags & SKU labels - we will only load what's properly tagged/labeled to best reproduce bundled-behavior.
	private static Object loadLooseMarkedForBundleAsset(string resourcePath)
	{

		// Is this resource defined in our dictionary? If so, load it...
		string projectRelativePath = getProjectRelativePathFromResourcePath(resourcePath);
		if (!string.IsNullOrEmpty(resourcePath))
		{
			return AssetDatabase.LoadAssetAtPath(projectRelativePath, typeof(Object));
		}
		
		// else nothing found
		return null;
	}

	public static string getProjectRelativePathFromResourcePath(string resourcePath)
	{
		string projectRelativePath;
		
		if (resourcePathToProjectRelativePath != null)
		{
			resourcePathToProjectRelativePath.TryGetValue(resourcePath.ToLower(), out projectRelativePath);
			return projectRelativePath;
		}
		
		// First time initialization (takes a few seconds)...
		// Build dictionary for quick lookup of short bundle-relative paths to full project-relative paths,
		// so we can quickly convert from a bundle-relative path and load an asset via LoadAssetAtPath(...) 
		// ie: "Games/rambo/rambo01/rambo01 BaseGame"  ==> "Assets/Assets Games/rambo/TopBundle/Games/rambo/rambo01/rambo01 BaseGame"
		Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

		// Get all label paths 
		string SKU = SkuResources.skuString.ToUpper();
		string[] labelRootPaths = AssetDatabase.FindAssets("l:"+SKU).Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray(); // 131 roots

		// Get all tagged assetbundle root paths, keep only what is SKU-appropriate (300+)
		string[] bundleRootPaths = AssetDatabase.FindAssets("b:").Select(guid => AssetDatabase.GUIDToAssetPath(guid))
			.Where(bundleRoot => labelRootPaths.Any(labelRoot => bundleRoot.FastStartsWith(labelRoot))).ToArray();

		// Prep bundleRootPaths to be used by a binary search (for performance reasons)
		// Add a '/' suffix to each path (unless it's a filepath), then sort...
		string[] orderedBundlePaths = bundleRootPaths
			.Select(path => path.Contains('.') ? path : path + "/")
			.OrderBy(path => path, System.StringComparer.Ordinal).ToArray();

		// Get assetpaths that belong to our sku-specific bundles (ignores all other assetpaths) in an optimal manner
		string[] assetPaths = AssetDatabase.GetAllAssetPaths();
		assetPaths = assetPaths.Where(path => path.Contains('.') && pathIsInOrderedRootPaths(path, orderedBundlePaths)).ToArray(); 

		// Populate resource -> project path dictionary
		resourcePathToProjectRelativePath = new Dictionary<string,string>();
		HashSet<string> setOfProjectPathConflicts = new HashSet<string>();
		StringBuilder fbxAssets = new StringBuilder();
		bool hasFbxAssets = false;
		foreach (string longPath in assetPaths)
		{
			string shortPath = AssetBundleMapping.longAssetPathToShortPath(longPath);
			if (resourcePathToProjectRelativePath.ContainsKey(shortPath))
			{
				// Conflict! Add this new asset & the pre-existing asset to our error set
				setOfProjectPathConflicts.Add( longPath );
				setOfProjectPathConflicts.Add( resourcePathToProjectRelativePath[shortPath] );
			}
			else
			{
				resourcePathToProjectRelativePath[ shortPath ] = longPath;
			}
			
			if (longPath.ToLowerInvariant().FastEndsWith(".fbx"))
			{
				fbxAssets.AppendLine(longPath);
				hasFbxAssets = true;
			}
		}

		// Were there asset-path conflicts? If so, it is a bundle-breaking issue, so inform the user
		if( setOfProjectPathConflicts.Count >= 1 )
		{
			string[] sortedConflicts = setOfProjectPathConflicts
				.Select( longPath => AssetBundleMapping.longAssetPathToShortPath(longPath) + "\t\tFrom: " + longPath )
				.OrderBy( str => str )
				.ToArray();

			TextWindow.Show( 
				"Asset-Path Conflicts!", 
				"There are asset conflicts that will break bundle-building! Short-paths must be unique.\n" +
				"Change their paths and/or basenames to fix (extensions get dropped):\n\n" +
				string.Join("\n", sortedConflicts) );
		}

		// Check if any FBX files are directly included in bundles, inform user if so
		if (hasFbxAssets)
		{
			TextWindow.Show(
				"Detected FBX files!", 
				"FBX files should not directly be included in asset bundles! Files Found: \n  " +
				fbxAssets.ToString());
		}
		
		// All sku-labels must be at/above the AssetBundleTag items; they don't work within bundles
		// Look for and warn about labels underneath bungletags...
		string[] allBundlePaths = AssetDatabase.FindAssets("b:").Distinct().Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();
		string[] labelsUnderneathBundles = labelRootPaths
			.Where(labelPath => allBundlePaths.Any(bundlePath => labelPath != bundlePath && labelPath.FastStartsWith(bundlePath) )).ToArray();
		if (labelsUnderneathBundles.Length >= 1)
		{
			TextWindow.Show(
				"Found Labels underneath AssetBundle Tags!", 
				"SKU Labels should be at/above any AssetBundleTags; they won't work within bundles. \n" +
				labelsUnderneathBundles.Length + " labels found underneath bundle tags. Paths = \n" +
				string.Join("\n", labelsUnderneathBundles));
		}

		// This should take less than 1 second
		Debug.Log("getProjectRelativePathFromResourcePath Found " + assetPaths.Length + " bundled assetpaths in: " + stopwatch.Elapsed.TotalSeconds + " sec"); 
		
		resourcePathToProjectRelativePath.TryGetValue(resourcePath.ToLower(), out projectRelativePath);

		return projectRelativePath;
	}

	// An optimized test to see if a path exists in an (ordered) list of root paths
	//
	// In-editor game startup was taking too long (7+ seconds) as we determined which of our 90,000+ assetpaths
	// were in sku-appropriate bundle paths. We now do a binary search against the (ordered) bundle paths.
	// (reduces search time from 10+ seconds to 0.5)
	static bool pathIsInOrderedRootPaths(string path, string[] rootPaths)
	{
		// returns index if found match, else negative value (a bitwise complement of next-largest item index)
		int index = System.Array.BinarySearch(rootPaths, path, System.StringComparer.Ordinal);

		// Positive index? We have an exact string match
		if (index >= 0)
		{
			return true;
		}

		// No match, complement to get index of next-largest item
		index = ~index; 

		// First item is already greater than desired path, no match
		if (index == 0)
		{
			return false;
		}

		// We only have to test against this one string
		return path.FastStartsWith(rootPaths[index-1]);
	}
	
	// Resource (or "ToBundle") relative paths TO project relative paths
	static Dictionary<string,string> resourcePathToProjectRelativePath = null;
#endif
	
	/// <summary>Unloads an asset bundle from memory, if it is loaded.</summary>
	public static void unloadBundle(string bundleName, bool unLoadLoadedObjects = false)
	{
		if (bundleName.FastStartsWith(INITIALIZATION_BUNDLE_NAME))
		{
			Debug.LogWarning("An attempt was made to unload the initialization bundle");
			return;
		}
		
		// If bundle bundleName is currently loaded, unload it and remove it from loadedAssetBundles map.
		AssetBundleContainer bundleContainer;
		if (Instance.loadedAssetBundles.TryGetValue(bundleName, out bundleContainer))
		{
			Instance.loadedAssetBundles.Remove(bundleName);
			Debug.Log($"AssetBundleManager unloadBundle id={bundleContainer.bundle.GetInstanceID()} name={bundleContainer.bundle.name}");
			bundleContainer.bundle.Unload(unLoadLoadedObjects); // Don't unload earlier, will break dependencies
			bundleContainer.bundle = null;
			Destroy(bundleContainer.gameObject);
		}
	}

	public static void unloadBundleImmediately(string bundleName)
	{
		if (manifestV2 != null)
		{
			bundleName = manifestV2.getFullBundleNameFromBaseBundleName(bundleName) ?? bundleName;
		}
		
		// If bundle bundleName is currently loaded, unload it and remove it from loadedAssetBundles map.
		AssetBundleContainer bundleContainer;
		if (Instance.loadedAssetBundles.TryGetValue(bundleName, out bundleContainer))
		{
			bundleContainer.forLevel = -2;
			unloadBundle(bundleName, unLoadLoadedObjects: false);
		}
	}

	/// <returns>True if the asset <paramref name="resourcePath"/> is available without downloading anything new.</returns>
	public static bool isAvailable(string resourcePath, string fileExtension = "")
	{
		bool isAvailable = false;

		string bundleName = getBundleNameForResource(resourcePath);
		if (!string.IsNullOrEmpty(bundleName))
		{
			AssetBundleContainer bundleContainer;
			if (Instance.loadedAssetBundles.TryGetValue(bundleName, out bundleContainer))
			{
				if (bundleContainer != null)
				{
					if (bundleContainer.assetBundleMapping.hasAsset(resourcePath, fileExtension))
					{
						isAvailable = true;
					}
				}
			}
			else
			{
				// The containing bundle is not currently loaded, but is it cached?
				isAvailable = isBundleCached(bundleName);
			}
		}
		else
		{
			// If the asset is not in the assetToBundleMap then it wasn't built into an asset bundle, so either it is
			// immediately available in Resources or not at all.  Unfortunately Unity doesn't provide any way to
			// positively test for an asset being in Resources without actually trying to load it.
			isAvailable = true;
		}

		Instance.LogExtra($"AssetBundleManager.isAvailable {resourcePath}: {isAvailable}");
		return isAvailable;
	}

	public static BundleType findBundleType(string bundleName)
	{
		return assetBundleTypes.FirstOrDefault(type => type.bundleName == bundleName);
	}

	/// <returns>Value between 0.0 and 1.0 denoting average progress of all current downloads.</returns>
	/// <remarks>Returns 1.0 if nothing is currently being downloaded.</remarks>
	private static float loadProgress()
	{
		float loadProgress = 1.0f;
		if (!Instance.isDownloading)
		{
			return loadProgress;
		}
		
		int downloadCount = Instance.assetBundlesToDownload.Count;
		if (downloadCount <= 0)
		{
			return loadProgress;
		}
		
		float totalProgress = 0.0f;
		foreach (AssetBundleDownloader download in Instance.assetBundlesToDownload)
		{
			totalProgress += download.loadProgress();
		}			
		loadProgress = totalProgress / downloadCount;
		
		return loadProgress;
	}

	/// <returns>Value between 0.0 and 1.0 denoting progress of downloading bundle <paramref name="bundleName"/>.</returns>
	/// <remarks>Returns 1.0 if it is not currently being downloaded.</remarks>
	public static float loadProgress(string bundleName)
	{
		float loadProgress = 1.0f;
		if (!Instance.isDownloading)
		{
			return loadProgress;
		}
		
		foreach (AssetBundleDownloader download in Instance.assetBundlesToDownload)
		{
			if (download.bundleName == bundleName)
			{
				loadProgress = download.loadProgress();
			}
		}
		
		return loadProgress;
	}

	private static void cancelDownloads()
	{
		if (!hasInstance())
		{
			return;
		}
		
		foreach (AssetBundleDownloader download in Instance.assetBundlesToDownload)
		{
			download.cancelDownload();
		}

		Instance.trackCancel();
	}
	
	public static void cancelDownloadsOnClick(List<string> downloadBundleNames)
	{
		cancelDownloads();

		foreach (AssetBundleDownloader download in Instance.assetBundlesToDownload)
		{
			if (downloadBundleNames.Contains(download.bundleName))
			{
				Instance.Log("User manually cancelled download: " + download.bundleName);
				// Rotate the url for the active downloads being canceled.
				download.bundleUrl = Instance.getNextUrlForBundle(download.bundleName, download.bundleUrl);
			}
		}
	}

	// Queues up a bundle to be downloaded/loaded-from-cache.
	// Does nothing if the bundle is already loaded.
	//
	// Returns: New or existing AssetBundleDownloader component if downloading, else null if no bundle or error
	private AssetBundleDownloader downloadAssetBundle(string bundleName, string resourcePath = null, AssetLoadDelegate successCallback = null, AssetFailDelegate failCallback = null, Dict data = null, object caller = null, bool isSkippingMapping = false, string fileExtension = "", bool isLazyLoaded = false, bool blockingLoadingScreen = false)
	{
		if (loadedAssetBundles.ContainsKey(bundleName))
		{
			LogExtra($"AssetBundleManager.downloadAssetBundle {bundleName} for {resourcePath}: bundle already loaded");
			return null;
		}
		
		if (!string.IsNullOrEmpty(resourcePath))
		{
			LogExtra($"AssetBundleManager.downloadAssetBundle {bundleName} for {resourcePath}");
		}
		else
		{
			LogExtra($"AssetBundleManager.downloadAssetBundle {bundleName} to cache");
		}

		bool bundleRequiresDownload = !(useLocalBundles || isBundleCached(bundleName));
		bool bundleUrlOk = useLocalBundles || bundleName.EndsWith(bundleV2Extension);
		bool bundleHasFailed = Data.debugMode && failedAssetBundleDownloads.Contains(bundleName);

		//
		//Attempt to load from Resources if loading via bundle isn't possible
		//
		if (!bundleUrlOk || bundleHasFailed || bundleRequiresDownload && !downloadsEnabled)
		{
			if (!bundleUrlOk)
			{
				LogExtra(
					$"AssetBundleManager.downloadAssetBundle {bundleName} for {resourcePath}: no url mapping found");
			}

			if (!downloadsEnabled)
			{
				Log($"AssetBundleManager.downloadAssetBundle {bundleName} for {resourcePath}: downloads disabled");
			}

			if (bundleHasFailed)
			{
				Log(
					$"AssetBundleManager.downloadAssetBundle {bundleName} for {resourcePath}: previous download failed, skipping");
			}

			fallbackToLoadFromResources(resourcePath, successCallback, failCallback, data);
			return null;
		}

		//
		// Check whether bundle is already downloading or queued up to be downloaded.
		//
		foreach (AssetBundleDownloader existingDownload in assetBundlesToDownload)
		{
			if (existingDownload.bundleName != bundleName)
			{
				continue;
			}
				
			if (!string.IsNullOrEmpty(resourcePath))
			{
				if (existingDownload.lazyLoaded != isLazyLoaded)
				{
					Debug.LogWarningFormat("Bundle {0} is being loaded multiple times with different lazy loading settings.", bundleName);
					existingDownload.lazyLoaded = false;
				}
						
				LogExtra(
					$"AssetBundleManager.downloadAssetBundle {bundleName} for {resourcePath}: already being downloaded");
				
				existingDownload.addNotify(resourcePath, successCallback, failCallback, data, caller, fileExtension);
			}

			if (existingDownload.cancelled || existingDownload.paused)
			{
				existingDownload.cancelled = false;
				existingDownload.paused = false;

				LogExtra(
					$"AssetBundleManager.downloadAssetBundle {bundleName} for {resourcePath}: was paused/cancelled, resuming download");
			}

			return existingDownload;
		}

		//
		// Prepare to download new bundle...
		//
		AssetBundleDownloader mainLoader = gameObject.AddComponent<AssetBundleDownloader>();
		mainLoader.bundleName = bundleName;
		mainLoader.bundleUrl = getUrlForBundle(bundleName);
		mainLoader.isSkippingMapping = isSkippingMapping;
		mainLoader.blockLoadingScreen = blockingLoadingScreen;
			
		if (!string.IsNullOrEmpty(resourcePath)) 
		{
			mainLoader.addNotify(resourcePath, successCallback, failCallback, data, caller, fileExtension); 
		}

		// Recursively check for/queue any bundle dependencies BEFORE pushing main bundle loader (dependencies should load first)
		if (bundleName.FastEndsWith(bundleV2Extension) && manifestV2 != null)
		{
			string[] bundleDependencies = manifestV2.getBundleDependencies(bundleName);
			if (bundleDependencies != null)
			{
				foreach(string bundleDependency in bundleDependencies)
				{
					// Is the bundle already downloaded?
					AssetBundleContainer depBundle = null;
					if (loadedAssetBundles.TryGetValue(bundleName, out depBundle))
					{
						depBundle.referencedBy.Add( mainLoader );
					}
					else // Get downloader for new or in-progress download
					{
						//RECURSIVE
						AssetBundleDownloader subLoader = downloadAssetBundle(bundleDependency, blockingLoadingScreen:mainLoader.blockLoadingScreen);
							
						if (subLoader == null)
						{
							continue;
						}
							
						mainLoader.dependsOn.Add( subLoader );
						subLoader.referencedBy.Add( mainLoader );
					}
				}
			}
		}

		// After queuing any dependencies, we can now queue the main bundle loader
		assetBundlesToDownload.Add(mainLoader);
		
		// Sort in the order
		// 1. dependency first
		// 2. blocking screen bundle next
		// 3. other bundles
		assetBundlesToDownload.Sort((a, b) =>
		{
			if (!a.dependsOn.IsEmpty() && a.dependsOn.Contains(b))
			{
				return 1;
			}
			else if(!b.dependsOn.IsEmpty() && b.dependsOn.Contains(a))
			{
				return -1;
			}
			else if(a.blockLoadingScreen && !b.blockLoadingScreen)
			{
				return -1;
			}
			else if (!a.blockLoadingScreen && b.blockLoadingScreen)
			{
				return 1;
			}
			else
			{
				return 0;
			}
		});

		addToHIRSettingsDialog(mainLoader);

		return mainLoader;
	}

	private void _handleDownloadError(AssetBundleDownloader download, string error)
	{
		string errorMsg = $"Error downloading {download.bundleName}:{download.bundleUrl}: {error}";
		StatsManager.Instance.LogCount("debug", "error", "failed_asset_download", download.bundleName, download.bundleUrl, error);
		
		if (Data.debugMode)
		{
			// This is a retry, so the message is a warning
			Debug.LogWarning(errorMsg);
		}
		else
		{
			// Always log akamai failures, even for retries
			Server.sendLogError(errorMsg, "AssetBundleManager._handleDownloadError()");
		}
		
		download.pause(error);
		
		// Rotate download host for retry.
		download.bundleUrl = Instance.getNextUrlForBundle(download.bundleName, download.bundleUrl);
		
		if (SlotsPlayer.isLoggedIn && download.tryCount >= MAX_RETRY_COUNT)
		{
			// Cancel download, we'll be returning/reloading the lobby after the dialog.
			download.cancelDownload();
			
			Loading.hide(Loading.LoadingTransactionResult.FAIL);
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, Localize.textOr("check_connection_title", "Check Connection"),
					D.MESSAGE, Localize.textOr("download_error_message", "Error downloading") + "\n\nAssetBundleManager: " + download.bundleName,
					D.OPTION1, Localize.toUpper(Localize.textOr("dismiss", "DISMISS")),
					D.REASON, "bundle-download-error",
					D.CALLBACK, new DialogBase.AnswerDelegate(_downloadErrorCallback)
				),
				SchedulerPriority.PriorityType.IMMEDIATE
			);
		}
		else
		{
			Debug.LogWarningFormat("Asset bundle download error on {0}. Retrying...", download.bundleName);
			download.paused = false;
		}
	}

	private void _downloadErrorCallback(Dict answerArgs)
	{
		Debug.LogWarning("Asset bundle download error: Going back to lobby.");
		cancelDownloads();
		Loading.show(Loading.LoadingTransactionTarget.LOBBY);
		GameState.pop();
		Glb.loadLobby();
	}

	/// <summary>Manages bundle downloads in flight.</summary>
	public void Update()
	{
		if (assetBundlesToDownload.Count <= 0)
		{
			return;
		}
		
		int downloadsInProgress = 0;

		List<AssetBundleDownloader> toRemove = new List<AssetBundleDownloader>();
		// check download progress; iterate in reverse order for easy deletion
		for(int i = assetBundlesToDownload.Count - 1; i >= 0; i--)
		{
			AssetBundleDownloader download = assetBundlesToDownload[i];
			if (!download.downloadStarted)
			{
				continue;
			}

			downloadsInProgress++;

			//Handle unfinished download and continue early
			if (!download.isDone)
			{
				if (download.uwr == null)
				{
					continue;
				}
				
				if (!string.IsNullOrEmpty(download.uwr.error))
				{
					_handleDownloadError(download, download.uwr.error);

					// In debug mode, don't keep trying to download a bundle we know won't work.
					if (Data.debugMode && !failedAssetBundleDownloads.Contains(download.bundleName))
					{
						failedAssetBundleDownloads.Add(download.bundleName);
					}
				}
				else if (download.isStalled())
				{
					_handleDownloadError(download, $"Stalled progress on bundle '{download.bundleName}'");
					AssetBundleDownloader.increaseStallTimeout();
				}

				continue;
			}
			
			//Handle finished download
			if (download.bundle != null && !download.failed)
			{
				GameObject go = new GameObject(); // track this AssetBundle in our AssetBundleManager object
				go.name = download.bundleName;
				go.transform.parent = gameObject.transform;

				AssetBundleContainer bundleContainer = go.AddComponent<AssetBundleContainer>();
				bundleContainer.skippedBundleMapping = download.isSkippingMapping;
				bundleContainer.bundle = download.bundle;
				bundleContainer.bundleName = download.bundleName;
				bundleContainer.assetBundleMapping = download.assetBundleMapping;
				bundleContainer.forLevel = download.keepLoaded
					? -1
					: UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
				bundleContainer.referencedBy = download.referencedBy;

				// Add the bundle to the loaded bundle list.
				loadedAssetBundles.Add(bundleContainer.bundleName, bundleContainer);

				// stats
				bytesDownloadedThisSession += download.bytesDownloaded;
				bytesDownloadedAllSessions += download.bytesDownloaded;

				if (lazyLoadedBundleToFeatureMap.ContainsKey(download.bundleName))
				{
					string storedLazyList = PlayerPrefsCache.GetString(LAZY_LOAD_LIST, "");
					string featureName = lazyLoadedBundleToFeatureMap[download.bundleName];

					if (storedLazyList.Contains(featureName))
					{
						StatsManager.Instance.LogCount("deferred_loading", featureName, "feature_downloaded");
						storedLazyList = storedLazyList.Replace(featureName, "");
						PlayerPrefsCache.SetString(LAZY_LOAD_LIST, storedLazyList);
					}
				}
			}

			if (download.failed)
			{
				// In debug mode, don't keep trying to download a bundle we know won't work.
				if (Data.debugMode && !failedAssetBundleDownloads.Contains(download.bundleName))
				{
					failedAssetBundleDownloads.Add(download.bundleName);
				}
			}

			//Remove the bundle from the loading list.
			if (download.lazyLoaded)
			{
				//Go ahead and clean up any memory taken up from the bundle.
				//If its lazy loaded then we wont be using any assets from it this session
				markBundleForUnloading(download.bundleName, true);
				downloadingLazyLoadBundle = false;
			}

			toRemove.Add(download);
		}

		foreach (var download in toRemove)
		{
			assetBundlesToDownload.Remove(download);
			
			// If download was initiated for a particular resource, notify caller.
			download.notifyCallersThatResourceIsReady();

			// Remove and destroy the downloader component.
			Destroy(download); 

			downloadsInProgress--;
		}
		toRemove.Clear();

		for(int i = 0; i < assetBundlesToDownload.Count; i++)
		{
			if (downloadsInProgress >= maxConcurrents)
			{
				break;
			}

			AssetBundleDownloader download = assetBundlesToDownload[i];
			if (download.lazyLoaded) 
			{
				if (downloadingLazyLoadBundle)
				{
					continue;
				}

				//Remove any lazy loaded bundles if we're in memory danger
				if (MemoryHelper.inMemoryDanger())
				{
					toRemove.Add(download);
				}
				else if (download.isReadyToStart())
				{
					download.startDownload();
					downloadsInProgress++;
					downloadingLazyLoadBundle = true;
				}
			}
			else if (download.isReadyToStart())
			{
				download.startDownload();
				downloadsInProgress++;
			}
		}
		
		foreach (var download in toRemove)
		{
			assetBundlesToDownload.Remove(download);
			Destroy(download);
		}
		
		toRemove.Clear();
		
		if (downloadsInProgress == 0)
		{
			if (isDownloading)
			{
				trackComplete();
			}
				
			isDownloading = false;
		}
		else
		{
			if (!isDownloading)
			{
				trackStart();
			}
			isDownloading = true;
			// Debug.LogExtra(string.Format("Asset download progress: {0}", loadProgress()));
		}
			
		trackProgress();
	}
	
	//Mark bundles that can be unloaded outside of our normal scene-change logic
	public void markBundleForUnloading(string bundleName, bool isFullName = false)
	{
		if (!useAssetBundles)
		{
			return;
		}

		if (!isFullName)
		{
			bundleName = manifestV2.getFullBundleNameFromBaseBundleName(bundleName);
		}

		AssetBundleContainer bundleContainer;
		if (bundleName != null && loadedAssetBundles.TryGetValue(bundleName, out bundleContainer))
		{
			bundleContainer.unTouch();
		}
	}

	/// <summary>
	/// Remove and unload unused asset bundle container objects every 5 seconds (Invoked in Awake())
	/// </summary>
    public void unloadUnusedBundles()
    {
	    if (loadedAssetBundles.Count <= 0)
	    {
		    return;
	    }
	    
	    List<string> unusedBundles = new List<string>();
		foreach (KeyValuePair<string, AssetBundleContainer> kvp in loadedAssetBundles)
		{
			AssetBundleContainer bundleContainer = kvp.Value;
			if (!bundleContainer.isInUse())
			{
				unusedBundles.Add(kvp.Key);
			}
		}
		
		foreach (string bundleName in unusedBundles)
		{
			unloadBundle(bundleName, false);
		}
		
		Glb.cleanupMemoryAsync();
    }
	
	public void unloadAllBundles()
	{
		List<string> bundleNames = loadedAssetBundles.Keys.ToList();
		foreach (string bundleName in bundleNames)
		{
			unloadBundle(bundleName, false);
		}
	}
	
	//Used for debugging on device to get a list of all the bundles currently loaded in the asset bundle manager
	public string getLoadedBundlesStr()
	{
		StringBuilder result = new StringBuilder(512);
		result.AppendLine($"Loaded Bundles: {loadedAssetBundles.Count}");

		var ourLoadedBundleNames = loadedAssetBundles.Keys.OrderBy(key => key).ToList();
		foreach(string bundleName in ourLoadedBundleNames)
		{
			result.AppendLine($"Bundle: {bundleName}");
		}

		result.AppendLine();

		List<string> allUnityLoadedBundleNames = 
			AssetBundle.GetAllLoadedAssetBundles().Select(bundle => bundle.name).OrderBy(bundleName => bundleName).ToList();
		result.AppendLine($"Unity AssetBundle.GetAllLoadedAssetBundles() sanity check: {allUnityLoadedBundleNames.Count}");
		foreach (string bundleName in allUnityLoadedBundleNames)
		{
			result.AppendLine($"UnityBundle: {bundleName}");
		}
		
		return result.ToString();
	}
	
	public static List<string> getAllBundleNames()
	{
		List<string> allBundleNames = new List<string>();
		if (manifestV2 != null)
		{
			foreach (KeyValuePair<string, string> kvp in manifestV2.baseBundleNameToFullBundleNameDict)
			{
				allBundleNames.Add(kvp.Key);
			}
		}
		return allBundleNames;
	}

	private void trackStart()
	{
		trackStartTime = Time.realtimeSinceStartup;
		currentTrackStep = 0;
		isCancelled = false;
		StatsManager.Instance.LogCount("game_download", StatsManager.getGameTheme(), "0", "","","", 0);
	}

	private void addToHIRSettingsDialog(AssetBundleDownloader bundle)
	{
		string bundleName = bundle.bundleName.Split('-')[0];
		if (hasLazyBundle(bundleName))
		{
			HelpDialogHIR.addDownload(bundle.bundleName);
		}
	}
	
	private void trackProgress()
	{
		if (!isDownloading || isCancelled)
		{
			return;
		}
		
		int currentStep = (int)(loadProgress() * 100) / 25;
		//Only track 25% at a time
		if (currentStep <= currentTrackStep || currentStep >= 4)
		{
			return;
		}
		
		StatsManager.Instance.LogCount("game_download", StatsManager.getGameTheme(), (currentStep * 25).ToString(), "","","",(int)((Time.realtimeSinceStartup - trackStartTime) * 1000));
		currentTrackStep = currentStep;
	}
	
	private void trackComplete()
	{
		if (isCancelled)
		{
			return;
		}
		
		if (StatsManager.Instance != null)
		{
			StatsManager.Instance.LogCount("game_download",
				StatsManager.getGameTheme(),
				"100",
				loadProgress() * 100 + "%","","",
				(int)((Time.realtimeSinceStartup - trackStartTime) * 1000));
		}
	} 

	private void trackCancel()
	{
		if (isCancelled)
		{
			return;
		}
		
		isCancelled = true;
		if(StatsManager.Instance != null)
		{
			StatsManager.Instance.LogCount("game_download",
				StatsManager.getGameTheme(),
				"download_cancelled",
				loadProgress() * 100 + "%","","",
				(int)((Time.realtimeSinceStartup - trackStartTime) * 1000));
		}
	}
	
	public void loadManifest(string manifestsToLoad)
	{
		StartCoroutine(loadManifestRoutine(manifestsToLoad));
	}

	private IEnumerator loadManifestRoutine(string manifestsToLoad)
	{
		string[] manifests = manifestsToLoad.Split(',');
		foreach (string manifest in manifests)
		{
			string fullManifestUrl = Glb.mobileStreamingAssetsUrl + "Manifests/" + manifest + "_" + PLATFORM + ".txt";
			using (UnityWebRequest uwr = UnityWebRequest.Get(fullManifestUrl))
			{
				yield return uwr.SendWebRequest();
				if (!string.IsNullOrEmpty(uwr.error))
				{
					Debug.LogErrorFormat("Error {0} downloading manifest {1}",uwr.error, fullManifestUrl);
				}
				else
				{
					manifestV2.readExtraManifest(uwr.downloadHandler.text);
				}
			}
		}
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		cancelDownloads();
		if (instance != null)
		{
			instance.bytesDownloadedThisSession = 0;
		}

		lazyBundlesReadyForNextSession = new List<string>();
		missingFeaturesToLazyLoad.Clear();
		inMemoryDanger = false;
		missingFeaturesToLazyLoad = new List<string>();
		_embeddedBundlesList = new string[0];
	}

	public class BundleType
	{
		public string bundleName { get; private set; }

		public BundleType(string resourcePath, bool loadNow = false)
		{
			bundleName = getBundleNameForResource(resourcePath);
			assetBundleTypes.Add(this);
		}
	}

}

#if UNITY_EDITOR
// An (editor-only) pop-up window to show asset clashes...
class TextWindow : EditorWindow
{
	private string content;
	private Vector2 scrollPos = new Vector2();

	public static TextWindow Show(string title, string content)
	{
		TextWindow window = GetWindow<TextWindow>(true, title);
		window.content = content;
		return window;
	}

	void OnGUI ()
	{
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
		EditorGUILayout.TextArea(content, GUILayout.ExpandHeight(true) );
		EditorGUILayout.EndScrollView();
	}
}
#endif

