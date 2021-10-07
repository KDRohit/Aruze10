//#define USE_OPTIMIZED_MOBILEONLY_SCAT_DATA

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class LoadGlobalData
{
	public const string RESPONSE_KEY = "LoadGlobalData";
	public const string RESPONSE_CACHE_FILE = "_global_data";

	public static string globalUrl = "";
	private static string globalDataVersion;

	public static void setGlobalDataURL(JSON basicDataJSON)
	{
		// looking up 'mini_global_data_url' because 'global_data_url' used to contain all the slot-game JSON data as well, which we dont want, but
		// nowdays the 2 entries are identical.  so its a historical vestige.
		LoadGlobalData.globalUrl = basicDataJSON.getString("mini_global_data_url", "BADURL");
		LoadGlobalData.globalUrl = Glb.fixupStaticAssetHostUrl(LoadGlobalData.globalUrl);

		// this gives us:  "mini_global_data_url":"https://zdnhir1-a.akamaihd.net/data/0014629/global/global_data_en_US.dat?version=0014629"
		int dataStart = LoadGlobalData.globalUrl.IndexOf("/data/");
		globalDataVersion = LoadGlobalData.globalUrl.Substring(dataStart + 6, 7);   // version # is 7 digits long
		if (!(char.IsDigit(globalDataVersion[0]) && char.IsDigit(globalDataVersion[globalDataVersion.Length - 1])))
		{
			Debug.LogErrorFormat("bad data version '{0}' retrieved from global data URL {1}, has URL format changed?", globalDataVersion, LoadGlobalData.globalUrl);
		}

	#if USE_OPTIMIZED_MOBILEONLY_SCAT_DATA
		// this gives us:  "mini_global_data_url":"https://zdnhir1-a.akamaihd.net/data/0014629/global/global_data_en_US.dat?version=0014629"
		// munge that to "https://zdnhir1-a.akamaihd.net/data_mobile/0014629/global_data_en_US.jsonb.gz"

		// TODO: I think it might be cleaner to simply add a 'mobile_global_data_url' to basic_game_data.php on server side instead of doing this munging here

		string optimizedDataURL = LoadGlobalData.globalUrl.Replace("/data/","/data_mobile/");
		optimizedDataURL = optimizedDataURL.Replace("/global/","/");
		int endToTrimIndex = optimizedDataURL.IndexOf(".dat?");
		optimizedDataURL = optimizedDataURL.Substring(0,endToTrimIndex);  // truncate before .dat
		optimizedDataURL += ".jsonb.gz";
		LoadGlobalData.globalUrl = optimizedDataURL;
	#endif
	}

	public static string getGlobalDataVersion()
	{
		return globalDataVersion;
	}

	/// This function gets all the static global data for playing the game.
	/// This is only called once when the Unity player is loaded.
	public static IEnumerator getGlobalData()
	{
//		System.GC.Collect();
//		Debug.LogErrorFormat("Profiler.GetMonoUsedSize: {0:0.0} MB", (float)Profiler.GetMonoUsedSize() / (1024f * 1024f));
//		Debug.LogErrorFormat("Profiler.GetMonoHeapSize: {0:0.0} MB", (float)Profiler.GetMonoHeapSize() / (1024f * 1024f));
//		Debug.LogErrorFormat("Profiler.GetTotalAllocatedMemory: {0:0.0} MB", (float)Profiler.GetTotalAllocatedMemory() / (1024f * 1024f));

		StatsManager.Instance.LogLoadTimeStart("BIL_GlobalDataRequest");
		yield return RoutineRunner.instance.StartCoroutine(Server.attemptRequest(globalUrl, null, "error_failed_to_load_data", RESPONSE_KEY, false, RESPONSE_CACHE_FILE));
		StatsManager.Instance.LogLoadTimeEnd("BIL_GlobalDataRequest");

		JSON jsonData = Server.getResponseData(RESPONSE_KEY);

		if (jsonData != null && jsonData.isValid)
		{
			StatsManager.Instance.LogLoadTimeStart("BIL_GlobalDataSet");
			Data.setGlobalData(jsonData);
			StatsManager.Instance.LogLoadTimeEnd("BIL_GlobalDataSet");
		}
		else
		{
			Server.connectionCriticalFailure("GD", Server.recentErrorMessage);
		}

		//		System.GC.Collect();
		//		Debug.LogErrorFormat("Profiler.GetMonoUsedSize: {0:0.0} MB", (float)Profiler.GetMonoUsedSize() / (1024f * 1024f));
		//		Debug.LogErrorFormat("Profiler.GetMonoHeapSize: {0:0.0} MB", (float)Profiler.GetMonoHeapSize() / (1024f * 1024f));
		//		Debug.LogErrorFormat("Profiler.GetTotalAllocatedMemory: {0:0.0} MB", (float)Profiler.GetTotalAllocatedMemory() / (1024f * 1024f));
	}
}
