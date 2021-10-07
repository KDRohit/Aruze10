#pragma warning disable 0618, 0168, 0414
using System;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;
using UnityEngine;
using System.Runtime.InteropServices;
#if UNITY_IOS
using UnityEngine.iOS;
#endif
using Zynga.Core.Util;
using Zynga.Zdk.Services.Common;
using Zynga.Zdk.Services.Track;
using Zynga.Core.Platform;
using ClientId = Zynga.Core.Platform.ClientId;

public class StatsManager :  IDependencyInitializer
{
	public const string KEY_FIRST_USE_OF_SN = "firstUseOfSN:";

	private static float _startTime = 0f;
	private static long anonymousZid = 0;
	private static bool sentCrashReport = false;
	private static bool sendCrashReport = false;
	
	private static List<string> startUpSteps = new List<string>();
	
	public delegate void ResponseHandler(string data,string statusCode, string statusDetail="");
	
	// Singleton access to the stats manager 
	public static StatsManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new StatsManager();
#if UNITY_EDITOR
				RoutineRunner.instance.StartCoroutine(_instance.statTallyCoroutine());
#endif
				RoutineRunner.instance.StartCoroutine(_instance.processPendingCalls());
			}
			return _instance;
		}
	}

	private static StatsManager _instance;


	private List<Action> pendingCalls = new List<Action>();
	
	// Access to the ZTrack instance
	private TrackServiceBase ZTrack
	{
		get { return ZdkManager.Instance.ZTrack; }
	}
	
	// Are we ready to send stats?
	private bool IsReady
	{
		get { return ZdkManager.Instance.IsReady; }
	}
	
	// Should we bypass stats all stats call?
	// This is kill-switch functionality for if there are future stats explosions.
	private bool shouldBypassStats
	{
		get
		{
#if UNITY_EDITOR
			// This is for a basic editor-only check for stats flooding.
			// We use this getter only because it is called just once for every stats call.
			statsCallTallyCount += 1;
#endif	
			return Glb.STATS_BYPASS;
		}
	}
	
	private const float PENDING_CALLS_CHECK_INTERVAL = 2.0f;
	private const int PENDING_CALLS_ATTEMPTS_THRESHOLD = 5;
	private float pendingCallsProcessTime = 0f;
	private int processPendingAttempts = 0;

	private IEnumerator processPendingCalls()
	{
		while (true)
		{
			float elapsed = Time.realtimeSinceStartup - pendingCallsProcessTime;
		
			if (elapsed >= PENDING_CALLS_CHECK_INTERVAL)
			{
				pendingCallsProcessTime = Time.realtimeSinceStartup;
				if (IsReady)
				{
					if (pendingCalls.Count > 0)
					{
						for (int i = 0; i < pendingCalls.Count; i++)
						{
							pendingCalls[i]();
						}

						pendingCalls.Clear();
					}
					yield break;
				}
				else
				{
					processPendingAttempts++;
				}

				if (processPendingAttempts > PENDING_CALLS_ATTEMPTS_THRESHOLD)
				{
					Dictionary<string, string> extraFields = new Dictionary<string, string>();
					extraFields.Add("pendingCalls", pendingCalls.Count.ToString());
					SplunkEventManager.createSplunkEvent("StatsManager", "IsReady", extraFields);
					yield break;
				}
			}

			yield return null;
		}
	}
	
#if UNITY_EDITOR
	// Keep a tally of how many stats calls have been called in a given timeframe.
	// This is an editor-only check in order to protect us from accidentally pushing
	// excessive stats calls. Please keep this as an editor-only check.
	
	private const float STATS_FLOODING_INTERVAL = 1f;
	private const int STATS_FLOODING_THRESHOLD = 20;
	private int statsCallTallyCount = 0;
	private float statsCallTallyTime = 0f;
	
	private IEnumerator statTallyCoroutine()
	{
		while(true)
		{
			float elapsed = Time.realtimeSinceStartup - statsCallTallyTime;
		
			if (elapsed > STATS_FLOODING_INTERVAL)
			{
				statsCallTallyCount = 0;
				statsCallTallyTime = Time.realtimeSinceStartup;
			}
			else if (statsCallTallyCount > (STATS_FLOODING_THRESHOLD * Time.timeScale))
			{
				// IF YOU RUN INTO THIS ERROR, PLEASE TALK TO YOUR POD LEAD OR JON
				// WE CAN CONSIDER UPPING THE THRESHOLD IF THIS OCCURS VIA NORMAL/CORRECT OPERATION
				Debug.LogError(string.Format("Excessive stats call making detected!  {0} calls in the past {1} seconds.", statsCallTallyCount, elapsed));
#if !ZYNGA_TRAMP
				Debug.Break();
#endif
				statsCallTallyCount = 0;
				statsCallTallyTime = Time.realtimeSinceStartup;
			}
			yield return null;
		}
	}
#endif
	
	public static ClientId ClientID
	{
		get 
		{
			return Zynga.Core.Platform.DeviceInfo.ClientId;
		}
	}	
	
	// Gets the game state, abstracted for stats calls in case we need to add special logic here
	public static string getGameTheme()
	{
		return GameState.currentStateName;
	}
	
	// Gets a santized Ztrack version of the game name based on game state 
	// or passed in when used before gamestate is updated
	public static string getGameName(string name = "") 
	{
		if (name == "")
		{
			name = GameState.currentStateName;
		}

		if (!string.IsNullOrEmpty(name))
		{
			string[] game = name.ToLower().Split(' ');
			name = string.Join("_", game);
		}
		else
		{
			name = "No current game";
		}
		
		return name;
	}

	public static string getGameKey()
	{
		return GameState.game != null ? GameState.game.keyName : "";
	}
	
	// Gets total time in seconds since app was started
	// or if (false) returns time since last call of getTime
	public static int getTime(bool total = true)
	{
		if (!total)
		{
			float previousTime = _startTime;
			_startTime = Time.realtimeSinceStartup;
			return (int)(_startTime - previousTime);
		}
		return (int)Time.realtimeSinceStartup;
	}
	
	static public string DeviceModel
	{
		get
		{
#if UNITY_IPHONE
			return UnityEngine.iOS.Device.generation.ToString();
#else
			if (string.IsNullOrEmpty(SystemInfo.deviceModel))
			{
				return "unknown";
			}
			else
			{
				// Sanitize potentially weird android device names
				string model = CommonText.makeIdentifier(SystemInfo.deviceModel.Replace(" ", "_"));
				if (model != "")
				{
					return model;
				}
				else
				{
					return "unknown";
				}
			}
#endif
		}
	}

	// Gets a sanitized model name, explicitly wrapped in quotes for ease-of-splunk parsing
	public static string DeviceModelNameInQuotes
	{
		get
		{
			return quoteString(StatsManager.DeviceModel);
		}
	}

	private float lastStartupStepTime = 0.0f;

	public void LogStartUpStep(string step)
	{
		if (!startUpSteps.Contains(step))
		{
			float time = Glb.timeSinceStartOrRestart;

			LogCount("timing", step, "", "", "", "", (long)time);
			startUpSteps.Add(step);

			// logging for splunk
			LogLoadTimeExplicit("step_" + step + "_total", time-lastStartupStepTime );

			lastStartupStepTime = time;
		}
	}

	private Dictionary<string,float> loadTimeStarts = new Dictionary<string, float>();
	private Dictionary<string,string> loadTimeEvents = new Dictionary<string,string>();
	private bool hasFlushedLoadTimeLog = false;

	public void LogLoadTimeExplicit(string key, float time)
	{
		loadTimeEvents[key] = time.ToString("0.00");
	}

	public void LogLoadTimeStart(string key)
	{
		float startTime = Time.realtimeSinceStartup;
		loadTimeStarts[key] = startTime;
	}

	public void LogLoadTimeEnd(string key)
	{
		float startTime;
		if (loadTimeStarts.TryGetValue(key, out startTime)) 
		{
			float endTime = Time.realtimeSinceStartup;
			LogLoadTimeExplicit(key + "_total", endTime-startTime);
			loadTimeStarts.Remove(key);
		}
	}

	public void FlushLoadTimeLog(bool wasPausedDuringLoading)
	{
		// only submit loadtime log once...
		loadTimeEvents["connection_speed"] = Server.connectionLevel.ToString();
		if (!hasFlushedLoadTimeLog && Glb.serverLogLoadTime)
		{
			loadTimeEvents["was_paused"] = wasPausedDuringLoading.ToString();
			
			Server.sendLogInfo("Stats_LoadTime", "LoadTimeLog", loadTimeEvents);
			Debug.Log("StatsManager::FlushLoadTimeLog - Sent load log to server.");
		}
		hasFlushedLoadTimeLog = true;
		loadTimeEvents.Clear();
	}
	

	// Used to report device specific stats at startup via splunk, to better understand our users devices & capabilities
	public void reportDeviceInfo()
	{
		// only report device info once...
		if (!hasReportedDeviceInfoLog && Glb.serverLogDeviceInfo)
		{
			var fields = new Dictionary<string,string>();

			fields["deviceID"]           = SystemInfo.deviceUniqueIdentifier;
			fields["smClientID"]         = ((int)StatsManager.ClientID).ToString();
			fields["smClientIDString"]   = StatsManager.ClientID.ToString();
			fields["device_model_name"]  = StatsManager.DeviceModel;  //sanitized
			fields["sysMem"]             = SystemInfo.systemMemorySize.ToString();
			fields["gpuMem"]             = SystemInfo.graphicsMemorySize.ToString();
			fields["gpuName"]            = SystemInfo.graphicsDeviceName;
			fields["gpuType"]            = SystemInfo.graphicsDeviceType.ToString();
			fields["gpuVersion"]         = SystemInfo.graphicsDeviceVersion;
			fields["gpuVendor"]          = SystemInfo.graphicsDeviceVendor;
			fields["gpuMultiThreaded"]   = SystemInfo.graphicsMultiThreaded.ToString();
			fields["gpuShaderLevel"]     = SystemInfo.graphicsShaderLevel.ToString();
			fields["gpuMaxTextureSize"]  = SystemInfo.maxTextureSize.ToString();
			fields["os"]                 = SystemInfo.operatingSystem;
			fields["cpuCount"]           = SystemInfo.processorCount.ToString();
			fields["cpuType"]            = SystemInfo.processorType;
			fields["width"]              = Screen.width.ToString();
			fields["height"]             = Screen.height.ToString();
			fields["texFormats"]         = getSupportedTexFormatsAsString();
			fields["initialVariant"]     = AssetBundleVariants.getActiveVariantName();

			// quote each field for easy inclusion as fields & easy parsing in splunk; pre-existing quotes become single quotes
			// (fields work fine with slashes, unicode, brackets, newlines, whitespace, null, etc)
			fields = quoteAllValues(fields);

			// queue up the splunk event - this adds a "user_id" & "session_key" to join multiple events together
			SplunkEventManager.createSplunkEvent("MobileClient", "MobileDeviceInfo", fields);
			Debug.Log("StatsManager::reportDeviceInfo - Queued splunk event");
		}
		hasReportedDeviceInfoLog = true;
	}

	private bool hasReportedDeviceInfoLog = false;


	// returns a space-delimited string of supported texture formats we are interested in knowing about
	private string getSupportedTexFormatsAsString()
	{
		// relevant texture formats we care about...
		TextureFormat[] formats = 
		{
			TextureFormat.RGB24,
			TextureFormat.ARGB4444,
			TextureFormat.RGBA4444,
			TextureFormat.RGB565,
			TextureFormat.Alpha8,
			TextureFormat.ETC_RGB4,
			TextureFormat.ETC2_RGBA8,
			TextureFormat.DXT1,
			TextureFormat.DXT5,
			TextureFormat.ASTC_RGBA_4x4,
			TextureFormat.ASTC_RGBA_5x5,
		};

		var sb = new System.Text.StringBuilder();
		foreach(var format in formats)
		{
			if (SystemInfo.SupportsTextureFormat(format))
			{
				sb.Append( format.ToString() );
				sb.Append( ' ' );
			}
		}
		return sb.ToString();
	}

	// Simple quote-fixup of dictionary values; replace quotes with single quotes, then add quotes around entire value
	private static Dictionary<string,string> quoteAllValues(Dictionary<string,string> srcDictionary)
	{
		var newDictionary = new Dictionary<string,string>(srcDictionary.Count);
		foreach (var kvp in srcDictionary)
		{
			var value = quoteString(kvp.Value); // handle nulls, quotes, and add quotes around result
			newDictionary[kvp.Key] = value;     // add to the new dictionary
		}
		return newDictionary;
	}

	private static string quoteString(string value)
	{
		value = value ?? "null";            // handle nulls
		value = value.Replace('"', '\'');   // replace pre-existing quotes with single quotes
		value = "\"" + value + "\"";        // add quotes around enire string
		return value;
	}

	// Called when the app resumes from being suspended
	public void pauseHandler(bool pause)
	{
		// TODO - should we still do this here, or does BatchedTrackService already handle flushing its calls when
		// resuming or at other handy points.
		if (IsReady)
		{

			BatchedTrackServiceBase batcher = PackageProvider.Instance.Track.BatchedService;
			if (batcher != null)
			{
				batcher.SendImmediately();
			}
		}
	}
	
	// Track an install
	public void LogInstall()
	{
		if (shouldBypassStats)
		{
			return;
		}
		
		string channel;
#if UNITY_IPHONE
			channel = "itunes";
#elif ZYNGA_GOOGLE
			channel = "google_play";
#elif ZYNGA_KINDLE
			channel = "amazon";
#elif UNITY_WEBGL
			channel = "facebook_canvas";
#elif UNITY_WSA_10_0 && NETFX_CORE //SMP this should be WSA specific
			channel = "windows_store";
#else
			channel = "";
#endif

		Dictionary<string, string> urlParams = URLStartupManager.Instance.urlParams;
			
		string kingdom = Glb.clientVersion;										// lists in vertica as affiliate
		string phylum = DeviceModel;											// lists in vertica as creative
		string clazz = null;													// lists in vertica as adname
		string family = urlParams != null && urlParams.ContainsKey("cmpn") ? urlParams["cmpn"] : null;		// lists in vertica as campaign

		Taxonomy taxonomy = new Taxonomy(kingdom, phylum, clazz, family);
			
		if (IsReady)
		{
			ZTrack.LogInstall(channel, null, taxonomy); // TODO: sendkey?
		}
		else
		{
			ZdkManager.Instance.HandleNotReady("LogInstall");
			pendingCalls.Add(() => ZTrack.LogInstall(channel, null, taxonomy));
		}
	}

	// Returns true if the counter should be logged
	private bool shouldLogCounter(string counterName, string kingdom = null)
	{
		if (Data.liveData != null && !string.IsNullOrEmpty(counterName))
		{
			string keyCounter = "CLIENT_ZTSR_" + counterName.ToUpper();
			string keyKingdom = "";
			
			// Check for and build the ZRT key that would hold a rate value for 
			if (!string.IsNullOrEmpty(kingdom))
			{
				keyKingdom = keyCounter + "_" + kingdom.ToUpper();
			}
			
			// Get the rate, if there is one
			int rate = 1;
			if (!string.IsNullOrEmpty(keyKingdom) && Data.liveData.hasKey(keyKingdom))
			{
				rate = Data.liveData.getInt(keyKingdom, 1);
			}
			else if (!string.IsNullOrEmpty(keyCounter) && Data.liveData.hasKey(keyCounter))
			{
				rate = Data.liveData.getInt(keyCounter, 1);
			}
			
			// Only if there is a rate above one do we consider not sending the stat
			if (rate > 1)
			{
				// Only return true if a random number between 1 and rate is 1
				return UnityEngine.Random.Range(1, rate + 1) == 1;
			}
		}
		return true;
	}

	// Convenience function to make taxonomy.
	public static Taxonomy CreateTaxonomy(
		string kingdom, 
		string phylum = "",
		string klass = "",
		string family = "",
		string genus = "")
	{
		Taxonomy taxonomy = new Taxonomy(kingdom, phylum, klass, family, genus);
		return taxonomy;
	}

	// General purpose call that can be used to count anything. 
	// Five levels of taxonomy (using keyX fields) can optionally be used to qualify the counter. 
	// Do not send unique values into any of these 5 levels.
	public void LogCount(
		string counterName,
		string kingdom,
		string phylum = "",
		string klass = "",
		string family = "",
		string genus = "",
		long val = 1,
		string milestone = "")
	{
		if (shouldBypassStats)
		{
			return;
		}

		if (shouldLogCounter(counterName, kingdom))
		{
			Taxonomy taxonomy = CreateTaxonomy(kingdom, phylum, klass, family, genus);
			if (IsReady)
			{
				ZTrack.LogCount(counterName, (int)val, taxonomy, milestone);
			}
			else
			{
				ZdkManager.Instance.HandleNotReady("LogCount " + kingdom);
				pendingCalls.Add(() => ZTrack.LogCount(counterName, (int)val, taxonomy, milestone));
			}
		}
	}

	// Log economy stat.
	public void LogEconomy(
		string currencyName,
		int amount,
		string kingdom,
		string phylum = "",
		string klass = "",
		string family = "",
		string genus = "")
	{
		if (shouldBypassStats)
		{
			return;
		}

		Taxonomy taxonomy = CreateTaxonomy(kingdom, phylum, klass, family, genus);
		if (IsReady)
		{
			ZTrack.LogEconomy(amount, currencyName, taxonomy);
		}
		else
		{
			ZdkManager.Instance.HandleNotReady("LogEconomy");
			pendingCalls.Add(() => ZTrack.LogEconomy(amount, currencyName, taxonomy));
		}
	}	

	// Logs the message.
	public void LogMessage(
		string channel,
		string status,
		long toZid,
		string kingdom = "",
		string phylum = "", 
		string klass = "",
		string family = "",
		string genus = "")
	{
		List<long> toZids = new List<long>();
		toZids.Add(toZid);
		LogMessage(channel, status, toZids, kingdom, phylum, klass, family, genus);
	}

	// Logs the message.
	public void LogMessage(
		string channel,
		string status,
		Zid toZid,
		string kingdom = "",
		string phylum = "", 
		string klass = "",
		string family = "",
		string genus = "")
	{
		List<Zid> toZids = new List<Zid>();
		toZids.Add(toZid);
		LogMessage(channel, status, toZids, kingdom, phylum, klass, family, genus);
	}
	
	// Logs the message.
	public void LogMessage(
		string channel,
		string status,
		List<long> toZids,
		string kingdom = "", 
		string phylum = "", 
		string klass = "",
		string family = "",
		string genus = "")
	{
		if (shouldBypassStats)
		{
			return;
		}
		// Need to use override taxonomy since general taxonomy only picked up through sendkey.
		Taxonomy overrideTaxonomy = CreateTaxonomy(kingdom, phylum, klass, family, genus);
		
		List<Zid> zidsList = new List<Zid>();
		foreach (var iZid in toZids)
		{
			if (iZid != 0 && iZid != 1)
			{
				zidsList.Add(new Zid(Convert.ToString(iZid)));
			}
		}
		
		if (zidsList.Count <= 0)
		{
			if (Data.debugMode)
			{
				Debug.LogErrorFormat("StatsManager.cs -- LogMessage -- no zids in the list, this will not log anything in vertica");
			}

			return;
		}
		
		if (IsReady)
		{

			ZTrack.LogMessage(zidsList.ToArray(), channel, null, null, null, null, null, null, null, null, overrideTaxonomy, null, null, null, status);
		}
		else
		{
			ZdkManager.Instance.HandleNotReady("LogMessage");
			pendingCalls.Add(() => ZTrack.LogMessage(zidsList.ToArray(), channel, null, null, null, null, null, null, null, null, overrideTaxonomy, null, null, null, status));
		}
	}

	// Logs the message.
	public void LogMessage(
		string channel,
		string status,
		List<Zid> toZids,
		string kingdom = "", 
		string phylum = "", 
		string klass = "",
		string family = "",
		string genus = "")
	{
		if (shouldBypassStats)
		{
			return;
		}
		
		// Need to use override taxonomy since general taxonomy only picked up through sendkey.
		Taxonomy overrideTaxonomy = CreateTaxonomy(kingdom, phylum, klass, family, genus);
		if (toZids.Count <= 0)
		{
			if (Data.debugMode)
			{
				Debug.LogErrorFormat("StatsManager.cs -- LogMessage -- no zids in the list, this will not log anything in vertica");
			}

			return;
		}

		if (IsReady)
		{

			ZTrack.LogMessage(toZids.ToArray(), channel, null, null, null, null, null, null, null, null, overrideTaxonomy, null, null, null, status);
		}
		else
		{
			ZdkManager.Instance.HandleNotReady("LogMessage");
			pendingCalls.Add(() => ZTrack.LogMessage(toZids.ToArray(), channel, null, null, null, null, null, null, null, null, overrideTaxonomy, null, null, null, status));
		}
	}	

	// Logs the message click.
	/*
	  This logs things in a weird way compared to what the PMs see in stats, mapping is here:
	   HERE      |    PM
	   --------------------
	  channel    |  channel
	  kingdom    |  category
	  phylum     |  subcategory
	  class      |  creative
	  family     |  family
	  genus      |  genus
	  clickType1 |  click_type
	  clickType2 |  click_subtype
	  clickType3 |  click_subtype2
	 */
	public void LogMessageClick(string sendkey = null,
								string overrideChannel = null,
								string overrideKingdom = null,
								string overridePhylum = null,
								string overrideClass = null,
								string overrideFamily = null,
								string overrideGenus = null,
								string overrideKey1 = null,
								string overrideKey2 = null,
								string overrideKey3 = null,
								string overrideKey4 = null,
								string overrideKey5 = null,
								string clickType1 = null,
								string clickType2 = null,
								string clickType3 = null)
	{
		if (shouldBypassStats)
		{
			return;
		}
		
		Taxonomy overrideTaxonomy = CreateTaxonomy(overrideKingdom, overridePhylum, overrideClass, overrideFamily, overrideGenus);
		
		if (IsReady)
		{
			ZTrack.LogMessageClick(sendkey, overrideChannel, overrideKey1, overrideKey2, overrideKey3, overrideKey4, overrideKey5,
								   clickType1, clickType2, clickType3, overrideTaxonomy, null, null, null, null, null);
		}
		else
		{
			ZdkManager.Instance.HandleNotReady("LogMessageClick");
			pendingCalls.Add(() => ZTrack.LogMessageClick(sendkey, overrideChannel, overrideKey1, overrideKey2, overrideKey3, overrideKey4, overrideKey5,
				clickType1, clickType2, clickType3, overrideTaxonomy, null, null, null, null, null));
		}
	}

	// Simplified Milestone Call passing only relevant parameters
	public void LogMileStone(string milestone, int val)
	{
		if (shouldBypassStats)
		{
			return;
		}
	
		if (IsReady) 
		{
			ZTrack.LogMilestone(milestone, val.ToString());
		}
		else
		{
			ZdkManager.Instance.HandleNotReady("LogMileStone");
			pendingCalls.Add(() => ZTrack.LogMilestone(milestone, val.ToString()));
		}
	}

	public void LogMileStone(string milestone, string val)
	{
		if (shouldBypassStats)
		{
			return;
		}

		if (IsReady) 
		{
			ZTrack.LogMilestone(milestone, val);
		}
		else
		{
			ZdkManager.Instance.HandleNotReady("LogMileStone");
			pendingCalls.Add(() => ZTrack.LogMilestone(milestone, val));
		}
	}

	// Logs the visit.
	public void LogVisit(
		string sourceChannel = null,
		int? fromSnId = null,
		long? fromZid = null,
		bool? isActive = null,
		string kingdom = "",
		string phylum = "",
		string klass = "",
		string family = "",
		string genus = "")
	{
		if (shouldBypassStats)
		{
			return;
		}
		
		if (IsReady)
		{
			LogVisitInternal(sourceChannel, fromSnId, fromZid, isActive, kingdom, phylum, klass, family, genus);
		}
		else
		{
			ZdkManager.Instance.HandleNotReady("LogVisit");
			pendingCalls.Add(() => LogVisitInternal(sourceChannel, fromSnId, fromZid, isActive, kingdom, phylum, klass, family, genus));
		}
	}

	private void LogVisitInternal(
		string sourceChannel = null,
		int? fromSnId = null,
		long? fromZid = null,
		bool? isActive = null,
		string kingdom = "",
		string phylum = "",
		string klass = "",
		string family = "",
		string genus = "")
	{
			AnalyticsManager.Instance.CheckLogInstall();

			StatsManager.Instance.LogCount
			(
				  counterName : "sku"
				, kingdom	  : Glb.clientVersion
				, phylum	  : SystemInfo.operatingSystem
				, klass		  : SystemInfo.deviceModel
				, family	  : SystemInfo.graphicsDeviceName
				, genus		  : SystemInfo.processorType
			);
	
			if (string.IsNullOrEmpty(kingdom))
			{
				kingdom = Glb.clientVersion;
			}
			Taxonomy taxonomy = CreateTaxonomy( kingdom, phylum, klass, family, genus );
			Zid? zid = null;
			if (fromZid != null)
			{
				zid = new Zid(Convert.ToString(fromZid));
			}
			Snid? snid = null;
			if (fromSnId != null)
			{
				snid = (Snid)fromSnId;
			}
			ZTrack.LogVisit(isActive, taxonomy, sourceChannel, zid, snid);

			string androidDeviceId = "";
#if UNITY_ANDROID
			androidDeviceId = Zynga.Slots.ZyngaConstantsGame.androidDeviceID;
#endif

			//Stats would like a call outside of their taxonomy for when the user logs a visit, so the below snuid_device_mapping calls need to change
			if (ZdkManager.Instance.Zsession != null)
			{
				if(ZdkManager.Instance.Zsession.Snid == Snid.Anonymous || ZdkManager.Instance.Zsession.Snid == Snid.GooglePlay)
				{
					anonymousZid = Convert.ToInt64(ZdkManager.Instance.Zsession.Zid.ToString());
					Bugsnag.AddToTab("HIR", "anon_zid", ZdkManager.Instance.Zsession.Zid.ToString());
					LogAssociate("snuid_device_mapping", Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID,"","","","","", null, null, androidDeviceId);
				}

				if ( ZdkManager.Instance.Zsession.Snid != Snid.Anonymous 
				    && ZdkManager.Instance.Zsession.Snid != Snid.GooglePlay
				    && anonymousZid != 0)
				{
					Bugsnag.AddToTab("HIR", "fb_zid", ZdkManager.Instance.Zsession.Zid.ToString());
					LogAssociate("snuid_device_mapping", Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID,"","","","","", null, null, androidDeviceId);
				}
			}

			Bugsnag.AddToTab("HIR", "clientId", (int)Zynga.Zdk.ZyngaConstants.ClientId);

			LogAssociate("device_start", Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID, "", DeviceModel, SystemInfo.operatingSystem, "", "", null, null, androidDeviceId);
			SendCrashReport();
	}

	// Logs the visit complete.
	public void LogVisitComplete()
	{
		LogVisit(null, null, null, true);
	}
	
	// Logs the visit loading.
	public void LogVisitLoading()
	{
		LogVisit(null, null, null, false);
	}

	// Logs the associate.
	public void LogAssociate(
		string key,
		string val1,
		string kingdom = "",
		string phylum = "",
		string klass = "",
		string family = "",
		string genus = "",
		object val2 = null,
		object val3 = null,
		object val4 = null)
	{
		if (shouldBypassStats)
		{
			return;
		}
		
		Taxonomy taxonomy = CreateTaxonomy(kingdom, phylum, klass, family, genus);
		if (IsReady)
		{
			ZTrack.LogAssociate(key, val1, taxonomy, (long?)val2, (long?)val3, (string)val4);
		}
		else
		{
			ZdkManager.Instance.HandleNotReady("LogAssociate");
			pendingCalls.Add(() => ZTrack.LogAssociate(key, val1, taxonomy, (long?)val2, (long?)val3, (string)val4));
		}
	}

	static public void CheckCrashReporter()
	{
		if (!string.IsNullOrEmpty(FileCache.path))
		{
			string path = System.IO.Path.Combine(FileCache.path, "com.plausiblelabs.crashreporter.data/com.zynga.hititrich/live_report.plcrash");
		
			if(System.IO.File.Exists(path)) 
			{
				sendCrashReport = true;
			}
		}
	}
	
	static public void SendCrashReport()
	{
		if (sendCrashReport && !sentCrashReport)
		{
			Instance.LogCount(
				"errors",
				"crash",
				Glb.clientVersion,
				SystemInfo.deviceModel, 
				SystemInfo.deviceName, 
				SystemInfo.operatingSystem);
			sentCrashReport = true;
		}
	}
	
#region ISVDependencyInitializer implementation

	// The StatsManager is dependent on AuthManager, and the AssetBundleManagerInit in order to get bundle-variant stat
	public System.Type[] GetDependencies() 
	{
		return new System.Type[] { typeof (AuthManager), typeof(AssetBundleManagerInit) } ;	
	}

	// Initializes the AuthManager
	public void Initialize(InitializationManager mgr)
	{
		mgr.InitializationComplete(this);
	}
	
	// Short description of this dependency for debugging purposes
	public string description()
	{
		return "StatsManager";
	}

#endregion

}
#pragma warning restore 0618, 0168, 0414
