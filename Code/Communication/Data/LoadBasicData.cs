using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;
using Zynga.Core.Platform;
using Zynga.Core.Util;
using System.Text;
using System;

public static class LoadBasicData
{
	public const string RESPONSE_KEY = "LoadBasicData";
	public const string RESPONSE_CACHE_FILE = "_basic_data";
	public const int SEVEN_DAY_UNIX_TIMESTAMP_DIFFERENCE = 604800;

#if !UNITY_EDITOR && UNITY_IPHONE
	[System.Runtime.InteropServices.DllImport("__Internal")]
	extern static public void SetLocale();
#endif
	
	/// get all the basic data (mostly URLs) needed to start loading the game
	public static IEnumerator getBasicGameData()
	{
		PreferencesBase preferences = SlotsPlayer.getPreferences();
#if !UNITY_EDITOR && UNITY_IPHONE
		// SetLocale() is an external function that sets the PlayerPref "locale" to the device locale.
		SetLocale();
#endif
		StatsManager.Instance.LogLoadTimeStart("BIL_BasicDataPrep");

		string locale = preferences.GetString(DebugPrefs.LOCALE);
		
		if (string.IsNullOrEmpty(locale) || locale == "none")
		{
			// Make this simpler.
			locale = "";
		}

		if (Data.basicDataUrl == "none")
		{
			Data.loadConfig();
		}

		// Get basicData...
		JSON basicData = null;
		JSON liveData = null;
#if !ZYNGA_PRODUCTION
		if (Data.canvasBasedConfig != null && SharedConfig.configJSON == null)
#else
		if (Data.canvasBasedConfig != null)
#endif
		{
			// Use canvas-based basicData if it exists (for WebGL)
			Debug.Log("Using canvas-based BasicData");
			basicData = Data.canvasBasedBasicData;
		}
		else
		{
			// Else request & wait for basic data from server...
			Debug.Log("Requesting server-based BasicData");
			yield return RoutineRunner.instance.StartCoroutine(requestBasicData(locale));
			basicData = Server.getResponseData(RESPONSE_KEY);
		}

		if (basicData != null && basicData.isValid)
		{
			StatsManager.Instance.LogLoadTimeStart("BIL_BasicDataSet");

			string cachedLiveDataVersion = getCachedLiveDataVersion();
			int liveDataVersion = basicData.getInt("live_data_version", 0);
			storeLiveDataVersion(liveDataVersion.ToString());

			//Main control to turn on or off the live data caching
			if (liveDataVersion <= 0)
			{
				liveData = basicData.getJSON("live_data");
			}
			else
			{
				//if the live data version is equal to the cached live data version. Then use the cached live data
				if (liveDataVersion.ToString().Equals(cachedLiveDataVersion) && !liveDataVersion.ToString().Equals("0"))
				{
					liveData = getCachedLiveData();
				}
				else
				{
					JSON newLiveData = basicData.getJSON("live_data");
					liveData = mergeLiveData(newLiveData);

					if (newLiveData != null)
					{
						// If keys have been removed from live data then remove those keys from the cache
						string[] removeLiveData = newLiveData.getStringArray("removed_live_data");
						if (removeLiveData != null && removeLiveData.Length > 0)
						{
							liveData = deleteLiveData(removeLiveData);
						}
					}

					storeLiveData(liveData.ToString());
				}
			}
			// Also, define the LiveData object, which is going to replace zrt, so everything can use it.
			Data.setLiveData(liveData);

			// pull out prelobby message
			string preLobbyMessage = Data.liveData.getString("CLIENT_PRE_LOBBY_MESSAGE", "");
			if (!string.IsNullOrEmpty(preLobbyMessage))
			{
				yield return RoutineRunner.instance.StartCoroutine(MOTDFramework.showPreLobbyDialog(preLobbyMessage));
			}
			else
			{
				// Reset the playerpref to be empty, in case the message we want to turn on next time is the same.
				preferences.SetString(Prefs.LAST_PRE_LOBBY_MESSAGE, "");
				preferences.Save();
			}

			Data.setBasicData(basicData);
			StatsManager.Instance.LogLoadTimeEnd("BIL_BasicDataSet");
			BugsnagHIR.SetupSampling();
		}
		else
		{
			Server.connectionCriticalFailure("BD", Server.recentErrorMessage);
		}
	}

	//Merging the live data and the cache live data changes
	private static JSON mergeLiveData (JSON liveData)
	{
		JSON cachedLiveData = getCachedLiveData();
		if (cachedLiveData == null) 
		{
			return liveData;
		}

		if (liveData == null && cachedLiveData != null) 
		{
			return cachedLiveData;
		}

		foreach (var temp  in liveData.jsonDict)
		{
			if (temp.Key == "removed_live_data")
			{
				continue;
			}
			if (cachedLiveData.hasKey(temp.Key))
			{
				cachedLiveData.jsonDict[temp.Key] = temp.Value;
			}
			else
			{
				cachedLiveData.jsonDict.Add(temp.Key, temp.Value);
			}
		}
		return cachedLiveData;
	}

	// Method to store the live data version
	private static void storeLiveDataVersion (string liveDataVersion)
	{
		SlotsPlayer.getPreferences().SetString(Prefs.LIVE_DATA_VERSION, liveDataVersion);
		SlotsPlayer.getPreferences().Save();
	}

	// Returns the live data version that is attached to the url
	private static string getCachedLiveDataVersion ()
	{
		return SlotsPlayer.getPreferences().GetString(Prefs.LIVE_DATA_VERSION, "0");
	}

	// Returns the cached live data
	private static JSON getCachedLiveData ()
	{
		string cachedLiveData = "";
		if (SlotsPlayer.getPreferences().HasKey(Prefs.CACHED_LIVE_DATA))
		{
			cachedLiveData = SlotsPlayer.getPreferences().GetString(Prefs.CACHED_LIVE_DATA, "");
		}

		if (cachedLiveData.IsNullOrWhiteSpace()) {
			return null;
		}
		JSON cachedLiveDataJSON = new JSON(cachedLiveData);
		return cachedLiveDataJSON;
	}

	// Method to store the live data
	private static void storeLiveData(string liveData)
	{
		SlotsPlayer.getPreferences().SetString(Prefs.CACHED_LIVE_DATA, liveData);
		SlotsPlayer.getPreferences().Save();
	}

	// Method to delete the live data keys
	private static JSON deleteLiveData(string[] removeLiveData)
	{
		JSON cachedLiveData = getCachedLiveData();
		if (cachedLiveData == null)
		{
			return null;
		}

		foreach (string temp in removeLiveData)
		{
			if (cachedLiveData.hasKey(temp))
			{
				cachedLiveData.jsonDict.Remove(temp);
			}
		}

		return cachedLiveData; 
	}

	private static IEnumerator requestBasicData(string locale)
	{
		PreferencesBase preferences = SlotsPlayer.getPreferences();
		string url = Data.basicDataUrl;
		Debug.Log("PlayerInfo requestBasicData from url: " + url);
		Dictionary<string, string> elements = new Dictionary<string, string>();
		
#if UNITY_EDITOR
		Localize.language = preferences.GetString(DebugPrefs.CURRENT_LANGUAGE).ToLower();
#else
		Localize.language = Application.systemLanguage.ToString().ToLower();
#endif

		if (!string.IsNullOrEmpty(Localize.language))
		{
			elements["lang"] = Localize.language;
		}
		else if (locale != "")
		{
			elements["loc"] = locale;
		}

		elements["live_data_version"] = getCachedLiveDataVersion();

		//Checking the TTL. If there is a difference of more than 7 days then redownload the whole live data from the server
		Int32 currentUnixTimestamp = (Int32)TimeUtil.CurrentTimestamp();
		int cachedLiveDataTimestamp = SlotsPlayer.getPreferences().GetInt(Prefs.CACHED_LIVE_DATA_TIMESTAMP, 0);
		int diff = currentUnixTimestamp - cachedLiveDataTimestamp;
		if (cachedLiveDataTimestamp == 0 || diff >= SEVEN_DAY_UNIX_TIMESTAMP_DIFFERENCE)
		{
			elements["live_data_version"] = "0";
			//PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
			SlotsPlayer.getPreferences().SetInt(Prefs.CACHED_LIVE_DATA_TIMESTAMP, currentUnixTimestamp);
			SlotsPlayer.getPreferences().Save();
		}

		//Don't send empty ad_id or the default empty id (00000000-0000-0000-0000-000000000000)
		if (!string.IsNullOrEmpty(Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID) && !Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID.Equals("00000000-0000-0000-0000-000000000000")) 
		{
			elements["ad_id"] = Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID;
		}
		Debug.Log("ad_id = " + Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID);

		// We need to use StatsManager.ClientID here because ZyngaConstants.ClientId isn't set yet.
		elements["client_id"] = ((int)StatsManager.ClientID).ToString();
		StatsManager.Instance.LogLoadTimeEnd("BIL_BasicDataPrep");

		StatsManager.Instance.LogLoadTimeStart("BIL_BasicDataRequest");
		yield return RoutineRunner.instance.StartCoroutine(Server.attemptRequest(url, elements, "", RESPONSE_KEY, false, RESPONSE_CACHE_FILE, false));
		StatsManager.Instance.LogLoadTimeEnd("BIL_BasicDataRequest");
	}

}
