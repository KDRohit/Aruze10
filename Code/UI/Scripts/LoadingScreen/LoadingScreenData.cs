//#define LOADING_SCREEN_TESTING

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Zynga.Core.Util;

// Info on DYNAMIC_LOADING_JSON, which specifies the custom loading-screen to be downloaded from s3 (could also be bundled, theoretically)
// https://wiki.corp.zynga.com/display/hititrich/HIR+Dynamic+Loading+Page

public class LoadingScreenData
{
	public static LoadingScreenData currentData;

	public bool isEnabled = false;
	public bool isPlatformEnabled = false;
	
	// Variables read from the data file.
	public string backgroundUrl = ""; // The URL of the background image (should be a jpg).
	public string particleEffect = ""; // The name of the particle effect we want to use (currently only "snow" is an option)
	public string overlayUrl = ""; // The URL of the overlay image with non-scaling objects. (png with transparency).
	public string logoUrl = ""; // The URL of the logo image (png with transparency)
	public string theme = "";
	public JSON[] widgetsRaw = null;
	public Dictionary<string, JSON> widgetLookup = null;
	
	public string[] tips; // List of the tips that we will show to users on the loading screen.

	public bool isDownloaded = false;
	public bool isLoading = false;
	
	// Variables used when loading the textures from the cache into the scene.
	private bool isBackgroundLoaded = false;
	private bool isOverlayLoaded = false;
	private bool isLogoLoaded = false;
	private bool hasBeenLoaded = false;
	private System.DateTime startTime = System.DateTime.MinValue;
	private System.DateTime endTime = System.DateTime.MinValue;
	private int minLevel = -1;
	private int maxLevel = -1;
	private Dictionary<string, string> localizationLookup;

#if UNITY_ANDROID
	private const string PLATFORM = "android";
#elif UNITY_IOS
	private const string PLATFORM = "ios";
#elif UNITY_WEBGL
    private const string PLATFORM = "unityweb";
#elif ZYNGA_KINDLE
    private const string PLATFORM = "kindle";
#else
	private const string PLATFORM = "unknown";
#endif

	public bool hasLoadedAllRequiredAssets
	{
		get
		{
			return (isBackgroundLoaded || string.IsNullOrEmpty(backgroundUrl)) && // If there is a background url, it must be loaded.
				(isOverlayLoaded || string.IsNullOrEmpty(overlayUrl)) && // If there is an overlay url, it must be loaded.
				(isLogoLoaded || string.IsNullOrEmpty(logoUrl)); // If there is a logo url, it must be loaded.
		}
	}
	private const int NUMBER_OF_FLOATING_SLOTS = 5;
	
	public static void init()
	{
		if (currentData != null && currentData.hasBeenLoaded)
		{
			currentData.loadAssetsIntoScene();
			return;
		}
		
		currentData = null;

		if (!File.Exists(dataPath))
		{
			Debug.Log("LoadingScreenData: no file found, setting local data version to be 0");
			PlayerPrefsCache.Save();
			return;
		}

		bool needsToCleanUpCachedData = false;
		try
		{
			string jsonString = File.ReadAllText(dataPath, System.Text.Encoding.UTF8);
			JSON json = new JSON(jsonString);

			Debug.Log("LoadingScreenData: found cached file");

			if (json != null && json.isValid)
			{
				Debug.Log("LoadingScreenData: found loading screen data");
				
				JSON[] loadingScreens = json.getJsonArray("loading_screens");
				if (loadingScreens == null)
				{
					loadingScreens = new[] { json };
				}
				
				foreach (JSON loadingScreenJSON in loadingScreens)
				{
					LoadingScreenData loadingScreenData = new LoadingScreenData(loadingScreenJSON);
					string name = loadingScreenJSON.getString("name", "");

					if (loadingScreenData.hasLoadedAllAssets && loadingScreenData.hasValidTime && loadingScreenData.hasValidLevel)
					{
						currentData = loadingScreenData;
					}
					else
					{
						Debug.LogFormat
						(
							"LoadingScreenData: Invalid loading screen {0} | hasLoadedAllAssets: {1}, hasValidTime: {2}, hasValidLevel: {3}",
							name,
							loadingScreenData.hasLoadedAllAssets,
							loadingScreenData.hasValidTime,
							loadingScreenData.hasValidLevel
						);
					}
				}

				if (currentData != null)
				{
					string localizationFileData = loadCachedLocalizationsFromDisk();
					currentData.parseLocalizations(localizationFileData);

					Debug.Log("LoadingScreenData: data is ready, loading textures into scene.");
					currentData.loadAssetsIntoScene();
				}
				else
				{
					logTesting("init",
							"current data is not ready, not loading textures into scene right now, will do so on next checkForUpdates()");
				}
			}
			else
			{
				Debug.Log("LoadingScreenData: failed to validate json");
				needsToCleanUpCachedData = true;
			}
			
			logTesting("init", "read data from file: {0}", jsonString);
		}
		catch (System.Exception e)
		{
			Debug.LogError("LoadingScreenData.cs -- init -- Exception when reading the loading screen json: " + e.ToString());
			needsToCleanUpCachedData = true;
		}
			
		if (needsToCleanUpCachedData)
		{
			// If we couldn't read it, then delete it and download the update.
			attemptDelete(dataPath);
			attemptDelete(localizationCachePath);
		}
		
		PlayerPrefsCache.Save();
	}

	private static string loadCachedLocalizationsFromDisk()
	{
		string localizationFileData = "";
		
		// If we have a cached version, then load it now. Otherwise leave it as null.
		if (!File.Exists(localizationCachePath))
		{
			return localizationFileData;
		}

		try
		{
			// Attempt to get cached localizations.
			localizationFileData = File.ReadAllText(localizationCachePath);
			logTesting("loadCachedLocalizationsFromDisk", "Localization file contents: {0}", localizationFileData);
		}
		catch (System.Exception e)
		{
			Debug.LogError("LoadingScreenData.cs -- Exception during localization cache reading: " +
			               e.ToString());
		}

		return localizationFileData;
	}

	// Data path to the cached json file.
	private static string dataPath
	{
		get
		{
			return Application.persistentDataPath + "/" + "loading_screen_data_cache.json";
		}
	}

	private static string localizationCachePath
	{
		get
		{
			return Application.persistentDataPath + "/" + "loading_screen_localizations.txt";
		}
	}
	
	// Static wrapper for the isReady function.
	public static bool isDataReady
	{
		get
		{
			if (currentData == null || !currentData.hasLoadedAllAssets)
			{
				return false;
			}
			return true;
		}
	}

	public bool hasValidTime
	{
		get
		{
			System.DateTime currentTime = System.DateTime.UtcNow;
			//If the start/end times weren't set then assuming they're not meant to be used
			if ((startTime != System.DateTime.MinValue && startTime > currentTime) ||
			    (endTime != System.DateTime.MinValue && endTime < currentTime))
			{
				return false;
			}
				
			return true;

		}
	}

	public bool hasValidLevel
	{
		get
		{
			int cachedPlayerLevel = PlayerPrefsCache.GetInt(Prefs.PLAYER_LEVEL, 0);
			if (cachedPlayerLevel >= minLevel)
			{
				if (maxLevel == -1)
				{
					return true;
				}
				
				if (maxLevel > -1 && cachedPlayerLevel <= maxLevel)
				{
					return true;
				}
				
				return false;
			}
			
			return false;
		}
	}
	
	// Checks the given version number against the currently stored one.
	// If they are different, downloads the new data and loads it.
	//Uses Live Data JSON
	public static void checkForUpdates()
	{
		if (Data.liveData == null)
		{
			// We don't have liveData yet.
			return;
		}

		// Info on DYNAMIC_LOADING_JSON, which specifies the custom loading-screen to be downloaded from s3 (could also be bundled, theoretically)
		// https://wiki.corp.zynga.com/display/hititrich/HIR+Dynamic+Loading+Page
		JSON loadingScreenBlob = Data.liveData.getJSON("DYNAMIC_LOADING_JSON");

		if (loadingScreenBlob == null || !loadingScreenBlob.isValid)
		{
			return;
		}

		int currentVersion = PlayerPrefsCache.GetInt(Prefs.LOADING_SCREEN_DATA_VERSION, 0);
		int newestVersion = loadingScreenBlob.getInt("version_number", 0);
		if (currentVersion == newestVersion)
		{
			// If we already have the most up to date version, don't do anything.
			logTesting("checkForUpdates", "versions match: {0}, using loading screen JSON: {1}", newestVersion.ToString(), loadingScreenBlob.ToString());
			if (currentData != null && !currentData.hasBeenLoaded)
			{
				// If we have the newest version, but we haven't loaded it at this point
				// (This can only happen if we have a loading screen data cache, but not the images)
				// Then load those in now.
				currentData.loadAssetsIntoScene();
			}
			return;
		}
			
		// If the new data version is not the same as the stored one, and there is a new data string, then try and load it.
		Debug.Log($"LoadingScreenData.cs -- checkForUpdates -- Loading new loading screen data -- version:{newestVersion} from {loadingScreenBlob.ToString()}");
		
		try
		{
			attemptDelete(dataPath);
			attemptDelete(localizationCachePath);
			currentData = new LoadingScreenData(loadingScreenBlob);
			if (currentData != null && currentData.isEnabled)
			{
				// Only try to download and load the textures if we have the loading screen enabled
				File.WriteAllText(localizationCachePath, currentData.getLocalizedTipsAsJson(), System.Text.Encoding.UTF8);
				File.WriteAllText(dataPath, loadingScreenBlob.ToString(), System.Text.Encoding.UTF8);
				PlayerPrefsCache.SetInt(Prefs.LOADING_SCREEN_DATA_VERSION, newestVersion);
				PlayerPrefsCache.Save();
				currentData.loadAssetsIntoScene();
			}
		}
		catch (System.Exception e)
		{
			Debug.LogError("LoadingScreenData.cs -- checkForUpdates -- Failed to write data to the cache with Exception: " + e);
		}
	}

	// Info on DYNAMIC_LOADING_JSON, which specifies the custom loading-screen to be downloaded from s3 (could also be bundled, theoretically)
	// https://wiki.corp.zynga.com/display/hititrich/HIR+Dynamic+Loading+Page
	// Checks the given version number against the currently stored one.
	// If they are different, downloads the new data and loads it.
	// Also checks that we're in a valid time range for the loading screen
	// Uses Data from MLCS Admin tool scheduler
	public static void checkForUpdates(JSON data)
	{
		JSON[] loadingScreens = data.getJsonArray("loading_screens");

		Debug.Log("LoadingScreenData: checkForUpdates(JSON data) running");

		try
		{
			attemptDelete(dataPath);
			attemptDelete(localizationCachePath);

			Debug.Log("LoadingScreenData: checkForUpdates(JSON data) checking loading screens: " + loadingScreens.Length);

			LoadingScreenData newLoadingScreenData = null;
			foreach (JSON loadingScreenJSON in loadingScreens)
			{
				LoadingScreenData loadingScreenData = new LoadingScreenData(loadingScreenJSON);

				if(loadingScreenData.isEnabled && loadingScreenData.hasValidTime && loadingScreenData.hasValidLevel)
				{
					newLoadingScreenData = loadingScreenData;
					Debug.Log("LoadingScreenData: checkForUpdates(JSON data) loading assets: " + loadingScreenJSON.getString("name", ""));
				}
			}

			if (newLoadingScreenData != null)
			{
				currentData = newLoadingScreenData;
				currentData.loadAssetsIntoScene();
				
				File.WriteAllText(localizationCachePath, currentData.getLocalizedTipsAsJson(), System.Text.Encoding.UTF8);
			}

			string jsonString = JSON.createJsonString("loading_screens", loadingScreens);
			File.WriteAllText(dataPath, "{" + jsonString + "}", System.Text.Encoding.UTF8);
			
			PlayerPrefsCache.Save();
		}
		catch (System.Exception e)
		{
			Debug.LogError(
				"LoadingScreenData.cs -- checkForUpdates -- Failed to write data to the cache with Exception: " +
				e);
		
		}
	}

	// Returns a string to be written to the FileCache of all the localization keys and values to be
	// used when we load the loading screen, but don't have localization data loaded yet.
	private string getLocalizedTipsAsJson()
	{
		Dictionary<string, string> dict = new Dictionary<string, string>();

		if (tips != null)
		{
			foreach (string tip in tips)
			{
				if (!dict.ContainsKey(tip))
				{
					dict.Add(tip, Localize.text(tip, ""));
				}
			}
		}
		
		return "{" + JSON.createJsonString("localizations", dict) + "}";
	}

	// Parses the localization file we read from the cache.
	private void parseLocalizations(string data)
	{
		if (string.IsNullOrEmpty(data))
		{
			return;
		}
		
		JSON json = new JSON(data);
		
		if (!json.isValid)
		{
			return;
		}
		
		localizationLookup = json.getStringStringDict("localizations");
	}
	
	// Loads the textures from the internet/cache and loads them into the loading screen.
	// This does not check for if it is cached, so we should check before we call this that
	// they are already cached if we call this during the loading screen.
	public void loadAssetsIntoScene()
	{
		if (isLoading || hasBeenLoaded)
		{
			logTesting("loadAssetsIntoScene", "either already loading: {0}, or hasBeenLoaded already: {1}",
				isLoading.ToString(), hasBeenLoaded.ToString());
			return;
		}

		logTesting("loadAssetsIntoScene", "starting the coroutines to load the textures from bundles");
		
		// We dont want to do these twice, so only start this if we are not currently loading it.
		isLoading = true;
		
		// Tell the loading screen that we should be using a new asset for the background
		Loading.hirV3.notifyNewBackgroundIsAvailable(backgroundUrl);

		if (!string.IsNullOrEmpty(overlayUrl))
		{
			logTesting("loadAssetsIntoScene", "starting download of overlay {0}", overlayUrl);
			DisplayAsset.loadLoadingScreenTexture(overlayUrl, overlayLoadingCallback);
		}

		if (!string.IsNullOrEmpty(logoUrl))
		{
			logTesting("loadAssetsIntoScene", "starting download of logoURL {0}", logoUrl);
			DisplayAsset.loadLoadingScreenTexture(logoUrl, logoLoadingCallback);
		}

		if (!string.IsNullOrEmpty(theme))
		{
			RoutineRunner.instance.StartCoroutine(loadTheme());
		}

		if (widgetsRaw != null)
		{
			RoutineRunner.instance.StartCoroutine(loadWidgets());
		}
	}

	private IEnumerator loadTheme()
	{
		while (!AssetBundleManagerInit.Instance.hasInitialized)
		{
			yield return null;
		}

		if (!string.IsNullOrEmpty(theme))
		{
			LoadingFactory.loadTheme(theme);
		}
	}

	private IEnumerator loadWidgets()
	{
		while (!AssetBundleManagerInit.Instance.hasInitialized)
		{
			yield return null;
		}
		if (widgetLookup != null)
		{
			foreach (KeyValuePair<string, JSON> widget in widgetLookup)
			{
				LoadingFactory.loadWidget(widget.Key);
			}
		}
	}

	// TextureDelegate callback for loading the overlay texture.
	private void overlayLoadingCallback(Texture2D tex, Dict args)
	{
	}

	private void logoLoadingCallback(Texture2D tex, Dict args)
	{
	}
	
	// Final callback for textures loading from the cache.
	private void handleAssetLoaded()
	{
		// If the background has loaded, and we have loaded all of the bubbles.
		if (hasLoadedAllRequiredAssets)
		{
			isLoading = false;
			hasBeenLoaded = true;
		}
	}
	
	// Simple method to make file deletes shorter elsewhere.
	private static void attemptDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch
		{
			// Nothing to catch here
		}
	}

	// Constructor.
	public LoadingScreenData(JSON data)
	{
		if (data == null)
		{
			Debug.LogError("LoadingScreenData -- bad data, JSON is null");
			isEnabled = false;
			backgroundUrl = "";
			overlayUrl = "";
			logoUrl = "";
			particleEffect = "none";
			tips = null;
			isDownloaded = false;
		}
		else
		{
			isEnabled = data.getBool("isEnabled", false);
			List<string> platforms = new List<string>(data.getStringArray("platforms"));
			isPlatformEnabled = platforms.Count == 0 || platforms.Contains(PLATFORM); //If the list is empty then assume its on for all platforms
			backgroundUrl = data.getString("background", "");
			overlayUrl = data.getString("overlay", "");
			logoUrl = data.getString("logo", "");
			particleEffect = data.getString("particle_effect", "none");
			tips = data.getStringArray("tips");
			widgetsRaw = data.getJsonArray("widgets");
			theme = data.getString("theme", "");
			isDownloaded = false;

			int startTimeInt = data.getInt("start_time", -1);
			int endTimeInt = data.getInt("end_time", -1);
			if (startTimeInt >= 0)
			{
				startTime = Common.convertFromUnixTimestampSeconds(startTimeInt);
			}
			if (endTimeInt >= 0)
			{
				endTime = Common.convertFromUnixTimestampSeconds(endTimeInt);
			}

			minLevel = data.getInt("min_level", -1);
			maxLevel = data.getInt("max_level", -1);

			if (widgetsRaw != null && widgetsRaw.Length > 0)
			{
				widgetLookup = new Dictionary<string, JSON>();
				for (int i = 0; i < widgetsRaw.Length; ++i)
				{
					string widgetName = widgetsRaw[i].getString("widget", "");
					if (!string.IsNullOrEmpty(widgetName))
					{
						widgetLookup.Add(widgetName, widgetsRaw[i]);
					}
				}
			}
		} 
	}

	// Boolean function to determine whether all of the data is downloaded.
	private bool hasLoadedAllAssets
	{
		get
		{
			if (string.IsNullOrEmpty(backgroundUrl))
			{
				logTesting("isReady", "backgroundUrl is null or empty, returning false");
				return false;
			}
			
			if (!DisplayAsset.isTextureDataCachedOnDisk(backgroundUrl))
			{
				logTesting("isReady", "backgroundUrl is not cached, returning false");
				return false; // We are not ready.	
			}	
			return true;
		}
	}

	private static void logTesting(string functionName, string baseString, params string[] keys)
	{
#if LOADING_SCREEN_TESTING
		Debug.LogFormat("LoadingScreenData.cs -- {0} -- {1}", functionName, string.Format(baseString, keys));
#endif
	}
}

