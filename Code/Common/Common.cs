using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
#if UNITY_WSA_10_0 && NETFX_CORE
using System.Reflection;
#endif
using Zynga.Core;
using Zynga.Core.UnityUtil;

/**
This is a purely static class of generic useful functions with no other home.

NOTE: Most functions were split into other classes. The ones remaining here don't really fit anywhere. Please see:
CommonAnimation.cs
CommonDataStructures.cs
CommonDeviceInfo.cs
CommonEffects.cs
CommonGameObject.cs
CommonGeometry.cs
CommonGraphics.cs
CommonMath.cs
CommonText.cs
*/
public static class Common
{
	// We used to use numeric delimiters for numbers 10,000+ (so it was 1000 instead of 1,000).
	// Now we're using numeric delimiters for numbers 1,000+.
	//public const int DO_GROUPING_AT = 1000;

	public const int SECONDS_PER_MINUTE = 60;
	public const int SECONDS_PER_HOUR = 60 * SECONDS_PER_MINUTE;
	public const int SECONDS_PER_DAY = 24 * SECONDS_PER_HOUR;
	public const float MILLISECONDS_PER_SECOND = 1000f;

	public static WaitForSeconds WaitOneSecond = new WaitForSeconds(1.0f);   // for use in yield in Coroutines, avoids tiny heap allocation every second in coroutines
	
	public static bool dontWait
	{
		set
		{
			if (value != _dontWait)
			{
				_dontWait = value;
				
				if (_dontWait)
				{
					PlayerPrefsCache.SetInt(DebugPrefs.DONT_WAIT , 1);
				}
				else
				{
					PlayerPrefsCache.SetInt(DebugPrefs.DONT_WAIT , 0);
				}
				
				_syncDontWait = false;
			}
		}
		
		get
		{
			if (_syncDontWait)
			{
				int value = PlayerPrefsCache.GetInt(DebugPrefs.DONT_WAIT , 0);
				
				if (value == 0)
				{
					_dontWait = false;
				}
				else
				{
					_dontWait = true;
				}
				
				_syncDontWait = false;
			}
			
			return (_dontWait);
		}
	}
	private static bool _syncDontWait = true;
	private static bool _dontWait = false;
	
	public static bool hasWaitedLongEnough(int waitedDuration, int longEnoughDuration)
	{
		if (dontWait)
		{
			return (true);
		}
		
		return (waitedDuration >= longEnoughDuration);
	}

	public static readonly System.DateTime EPOCH_START = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

	// returns epoch time in seconds
	public static int dateTimeToUnixSecondsAsInt(System.DateTime? time)
	{
		var totalSeconds = time?.Subtract(EPOCH_START).TotalSeconds;
		if (totalSeconds != null)
			return (int) totalSeconds; // No fractional times for Unix time.

		return 0;
	}

	// returns epoch time in seconds + fractional sub-second time
	public static double dateTimeToUnixSecondsAsDouble(System.DateTime time)
	{
		return time.Subtract(EPOCH_START).TotalSeconds; // WITH fractional times
	}

	// Similar to waitForCoroutinesToEnd() but if the screen is tapped the waiting will be
	// canceled and the coroutines will be stopped.  allCoroutinesThatCanSkip should contain all
	// coroutines and subcoroutines spawned from those coroutines (assuming you want everything
	// to be skipped when tapped). If subcoroutines aren't included they will not be skipped (that
	// could be beneficial if you have a reason why you want some subcorutines to finish though).
	public static IEnumerator waitForTapToSkipCoroutinesToEnd(CoroutineObjectTracker allCoroutinesThatCanSkip)
	{
		bool allEnded = false;
		while (!allEnded)
		{
			allEnded = allCoroutinesThatCanSkip.isEveryCoroutineFinished();

			if (!allEnded)
			{
				// Wait a frame for them all to finish and then check again
				yield return null;

				if (TouchInput.didTap)
				{
					// Cancel the coroutines
					allCoroutinesThatCanSkip.stopAndClearAllTrackedCoroutines();

					allEnded = true;
				}
			}
		}
	}


	// Function that quits the app
	// gnair - Unity 2019 has an issue where it doesn't properly clean up termination of the
	//  application on quit. For now we'll just background the app. However, in future version
	//  of Unity will can test to see if we can use Application.Quit again.
	public static void QuitApp()
	{
#if UNITY_ANDROID
		// BY 05-21-2021 We are selectively choosing not to Application.Quit() here due to a unity bug in 2019
 		// Fixed in Unity 2020.2
		using (AndroidJavaClass javaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) 
		{
			AndroidJavaObject unityActivity = javaClass.GetStatic<AndroidJavaObject>("currentActivity");
			unityActivity.Call<bool>("moveTaskToBack", true);
		}
#else
		Application.Quit();
#endif
	}

		public static IEnumerator waitForCoroutinesToEnd(List<TICoroutine> runningCoroutines)
	{
		// Wait for all the leftover reveal coroutines to end.
		bool allEnded = false;
		while (!allEnded)
		{
			allEnded = true;
			foreach (TICoroutine coroutine in runningCoroutines)
			{
				allEnded = allEnded && coroutine.finished;
			}
			// Wait for them all to finish.
			yield return null;
		}
	}
	
	// More generic version of waitForCoroutinesToEnd that supports some additional classes like TITweenYieldInstruction
	public static IEnumerator waitForITIYieldInstructionsToEnd(List<ITIYieldInstruction> runningInstructions)
	{
		// Wait for all the leftover reveal coroutines to end.
		bool allEnded = false;
		while (!allEnded)
		{
			allEnded = true;
			foreach (ITIYieldInstruction instruction in runningInstructions)
			{
				allEnded = allEnded && instruction.isFinished;
			}
			// Wait for them all to finish.
			yield return null;
		}
	}

	public delegate void SplineToCompleteDelegate(GameObject spliningObj);


	/// Calls Object.Destroy() on the Object if this is not in the editor.
	/// This was made for self-destructing scripts.
	public static void selfDestructObject(Object obj)
	{
#if UNITY_EDITOR
		if (obj != null && !Data.debugMode)
		{
			Object.Destroy(obj);
		}
#else
		if (obj != null)
		{
			Object.Destroy(obj);
		}
#endif
	}

	public static System.DateTime convertTimestampToDatetime(int timeStamp)
	{
		int dateCompleted = timeStamp;

		System.DateTime lastOnlineTime = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
		lastOnlineTime = lastOnlineTime.AddSeconds(dateCompleted);

		return lastOnlineTime;
	}

	// Deletes everything in our file cache directory.
	public static void clearTemporaryCache()
	{
		AssetBundleManager.clearUnityAssetBundleCache(); // Cleaning the AssetBundle cache.
		
		// Delete our local custom cached files (images and static data files)
		if (!string.IsNullOrEmpty(FileCache.path))
		{
			// Find all version files, and delete them along with their associated data files
			foreach (string versionPath in System.IO.Directory.GetFiles(FileCache.path, "*.version", System.IO.SearchOption.TopDirectoryOnly)) 
			{
				try
				{
					System.IO.File.Delete(versionPath);
				} catch { }
				try
				{
					string dataPath = versionPath.Substring(0, versionPath.LastIndexOf(".")) + ".data";
					System.IO.File.Delete(dataPath);
				} catch { }
				try
				{
					string dataTxtPath = versionPath.Substring(0, versionPath.LastIndexOf(".")) + ".txt";
					System.IO.File.Delete(dataTxtPath);
				} catch { }

			}

			// we now have other types of files too
			foreach (string path in System.IO.Directory.GetFiles(FileCache.path, "*.json", System.IO.SearchOption.TopDirectoryOnly))
			{
				try
				{
					System.IO.File.Delete(path);
				}
				catch
				{
				}
			}
			foreach (string path in System.IO.Directory.GetFiles(FileCache.path, "*.txt", System.IO.SearchOption.TopDirectoryOnly))
			{
				try
				{
					System.IO.File.Delete(path);
				}
				catch
				{
				}
			}
		}
	}
	
	// Converts a DateTime to a Unix timestamp with second resolution.
	public static int convertToUnixTimestampSeconds(System.DateTime date)
	{
		System.TimeSpan diff = date.ToUniversalTime() - EPOCH_START;
		return (int)System.Math.Floor(diff.TotalSeconds);
	}
	
	// Get the Unix timestamp of "now".
	public static int utcTimeInSeconds()
	{
		return convertToUnixTimestampSeconds(System.DateTime.UtcNow);
	}
	
	// Converts a Unix timestamp to a DateTime with second resolution.
	public static System.DateTime convertFromUnixTimestampSeconds(int l)
	{
		return EPOCH_START.AddSeconds(l);
	}
	
	// This is for a special Zynga URL encoding method that once lived in MECO source.
	private static string safeChars = "-_.~abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
	public static string zEncodeURL(string value)
	{
		var result = new System.Text.StringBuilder();
		foreach (var s in value)
		{
			if (safeChars.IndexOf(s) != -1)
			{
				result.Append(s);
			}
			else
			{
				result.Append('%' + string.Format("{0:X2}", (int)s));
			}
		}
		return result.ToString();
	}

	//Method to base encode a string
	public static string Base64Encode(string plainText)
	{
		var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
		return System.Convert.ToBase64String(plainTextBytes);
	}

	//Method to base decode a string
	public static string Base64Decode(string base64EncodedData)
	{
		var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
		return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
	}


	// Open Url's need special treatment for WebGL; must be called during mouse PRESS event
	// Calls normal Application.OpenUrl for non-webGl
	public static void openUrlWebGLCompatible(string url)
	{
#if UNITY_WEBGL
		// WebGL needs special treatment to prevent browsers from suppressing pop-up requests.
		// Must be in direct response to a user input, so we add an OnMouseUp event the browser during the mouse-down event.
		// See: http://va.lent.in/opening-links-in-a-unity-webgl-project/
		WebGLFunctions.openUrlOnMouseUp(url);
#else
		// normal for everybody else
		Application.OpenURL(url);
#endif
	}

	public static string RewriteHelpshiftSupportUrl(string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return "";
		}

		if (url.Contains("https://zyngasupport.helpshift.com/a/hit-it-rich/"))
		{
			string csproxyUrl = Data.serverUrl + Data.liveData.getString("CS_PROXY_URL", "/client/helpshift.php");
			// rewrite URL to hit csproxy
			url = url.Replace("https://zyngasupport.helpshift.com/a/hit-it-rich/", csproxyUrl);

			// Url is to helpshift, so add parameters.
			url = CommonText.appendQueryStringsDictionaryToUrl(url, Common.collectHelpshiftMetadata());
		}
		return url;
	}

	// Open support URL with Helpshift app/user parameters.
	public static void openSupportUrl(string url)
	{
		string rewrittenUrl = Common.RewriteHelpshiftSupportUrl(url);
		if (!string.IsNullOrEmpty(rewrittenUrl))
		{
			Common.openUrlWebGLCompatible(rewrittenUrl);
		}
	}

	private static Dictionary<string, object> collectHelpshiftMetadata()
	{
		Dictionary<string,object> metadata = new Dictionary<string,object>();

		if (SlotsPlayer.instance != null)
		{
			metadata.Add("vipSpendTier", SlotsPlayer.instance.vipSpendTier);
			metadata.Add("spent200OrMore", SlotsPlayer.instance.spent200OrMore);
			metadata.Add("vipStatus", SlotsPlayer.instance.vipStatus);
			metadata.Add("playedInTheLast30Days", SlotsPlayer.instance.playedInTheLast30Days);
			metadata.Add("secondsSinceLastPlayed", SlotsPlayer.instance.secondsSinceLastPlayed);

			metadata.Add("credits", SlotsPlayer.creditAmount);
			metadata.Add("country", SlotsPlayer.instance.country);
			metadata.Add("payer", SlotsPlayer.instance.isPayerMobile);
			if (XPMultiplierEvent.instance != null)
			{
				metadata.Add("xpMultiplier", XPMultiplierEvent.instance.xpMultiplier);
			}
			if (SlotsPlayer.instance.socialMember != null)
			{
				metadata.Add("isMaxLevel", SlotsPlayer.instance.isMaxLevel); // calls socialMember internally
				metadata.Add("uid", SlotsPlayer.instance.socialMember.zId);
				metadata.Add("socialMember.id", SlotsPlayer.instance.socialMember.id);
				metadata.Add("vipLevel", SlotsPlayer.instance.socialMember.vipLevel);
				metadata.Add("isFemale", SlotsPlayer.instance.socialMember.isFemale);
				metadata.Add("experienceLevel", SlotsPlayer.instance.socialMember.experienceLevel);
				metadata.Add("gameusername", SlotsPlayer.instance.socialMember.fullName);
			}
		}

		metadata.Add("deviceid", Zynga.Core.Platform.DeviceInfo.DeviceUniqueIdentifier);
		metadata.Add("devicemake", Zynga.Core.Platform.DeviceInfo.DeviceName);
		metadata.Add("devicemodel", Zynga.Core.Platform.DeviceInfo.DeviceModel);
		metadata.Add("deviceType", SystemInfo.deviceType.ToString());
		metadata.Add("gameid", Data.zAppId);
		if (ZdkManager.Instance != null && ZdkManager.Instance.Zsession != null)
		{
			metadata.Add("sn", (int)ZdkManager.Instance.Zsession.Snid);
		}
		metadata.Add("ts", Common.utcTimeInSeconds());
		metadata.Add("gamebuildver", Glb.clientVersion);

		List<string> tags = new List<string>();

		#if UNITY_EDITOR
		tags.Add("UNITY_EDITOR");
		#endif

		#if UNITY_IOS
		tags.Add("ios");
		#elif UNITY_ANDROID
		tags.Add("android");
		#endif

		switch (SkuResources.currentSku)
		{
			case SkuId.HIR:
				tags.Add("hit_it_rich");
				break;
			default:
			case SkuId.UNKNOWN:
				tags.Add("sku_unknown");
				break;
		}

		if (!string.IsNullOrEmpty(SystemInfo.operatingSystem))
		{
			tags.Add(SystemInfo.operatingSystem);
		}

		if (!string.IsNullOrEmpty(Localize.language))
		{
			tags.Add(Localize.language);
		}

		if (SlotsPlayer.instance != null)
		{
			if (SlotsPlayer.instance.xp != null)
			{
				tags.Add("xp " + SlotsPlayer.instance.xp.amount.ToString());
			}

			if (SlotsPlayer.instance.socialMember != null)
			{
				tags.Add("socialMember.id " + SlotsPlayer.instance.socialMember.id);
			}
		}

		if (tags.Count > 0)
		{
			metadata.Add("tags", string.Join(";", tags.ToArray()));
		}

		string queryString = string.Empty;

		foreach  (KeyValuePair<string,object> entry in metadata)
		{
			string key = entry.Key;
			if (string.IsNullOrEmpty(key))
			{
				continue;
			}
			string value = entry.Value?.ToString() ?? string.Empty;
			queryString += key + "/" + value + "/";
		}

		if (!string.IsNullOrEmpty(queryString))
		{
			queryString = Common.EncryptURLForCSProxy(queryString);
		}

		Dictionary<string,object> metadataParams = new Dictionary<string,object>();
		metadataParams.Add("id", queryString);
		return metadataParams;
	}

	// Taken from FVCE: encrypt URL params for CS proxy to helpshift support.
	private static string EncryptURLForCSProxy (string rawQueryString)
	{
		string KEY = "aZutDiPlebMqWrygdpEwBNAzYULmR";
		int keyLength = KEY.Length;
		int asciiNumByteToEncode, rmKey, pmKey;
		char byteToEncode, encodedByte;
		string encoded = string.Empty;

		for (int i=0; i<rawQueryString.Length; i++)
		{
			byteToEncode = rawQueryString[i];
			asciiNumByteToEncode = System.Convert.ToInt32(byteToEncode); //convert each url char to ascii value
			rmKey = (asciiNumByteToEncode + keyLength) % keyLength;
			pmKey = (asciiNumByteToEncode / keyLength);
			encodedByte = KEY[rmKey];
			if (pmKey == 0)
			{
				encoded += encodedByte.ToString();
			}
			else
			{
				encoded += (pmKey).ToString() + encodedByte.ToString();
			}
		}
		return encoded;
	}

	// Please note, this is an expensive operation and should be limited to usage outside of a loop/core loop
	public static List<System.Type> getAllClassTypes(string fullyQualifiedClassName)
	{
		// Search all assemblies for types that implement the IResetGame interface
		var types = new List<System.Type>();
#if UNITY_WSA_10_0 && NETFX_CORE
        var assembly = typeof(Glb).GetTypeInfo().Assembly;
        {
#else
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
		{
			if (assembly.FullName.FastStartsWith("Mono.Cecil") ||
				assembly.FullName.FastStartsWith("UnityScript") ||
				assembly.FullName.FastStartsWith("Boo.Lan") ||
				assembly.FullName.FastStartsWith("System") ||
				assembly.FullName.FastStartsWith("I18N") ||
				assembly.FullName.FastStartsWith("UnityEngine") ||
				assembly.FullName.FastStartsWith("UnityEditor") ||
				assembly.FullName.FastStartsWith("mscorlib"))
			{
				continue;
			}
#endif

			System.Type classType = System.Type.GetType(fullyQualifiedClassName);
            foreach (System.Type type in assembly.GetTypes())
			{
				if (!type.IsClass() || type.IsGenericType())
				{
					continue;
				}
				System.Type[] interfaceTypes = type.GetInterfaces();

				bool typeFound = false;
				foreach (System.Type interTypes in interfaceTypes)
				{
					if (interTypes == classType)
					{
						typeFound = true;
					}
				}

				if (typeFound)
				{
					types.Add(type);
				}
			}
		}

		return types;
	}
	
	public static float getLevelProgress(ExperienceLevelData currentLevel = null)
	{
		if (currentLevel == null)
		{
			currentLevel = ExperienceLevelData.find(SlotsPlayer.instance.socialMember.experienceLevel);
		}

		float lvlProgress = 1f;

		if (currentLevel.level < ExperienceLevelData.maxLevel)
		{
			ExperienceLevelData newlevel = ExperienceLevelData.find(currentLevel.level + 1);
			
			if (newlevel == null)
			{
				Debug.LogError("XPUI.getTargetXpBarWidth(): newlevel is null for " + (currentLevel.level + 1));
				return 0.0f;
			}

			lvlProgress = ((float)(SlotsPlayer.instance.xp.amount - currentLevel.requiredXp) / (newlevel.requiredXp - currentLevel.requiredXp));
			lvlProgress = Mathf.Min(lvlProgress, 1f);	// Just in case the player has enough xp for the next level, but hasn't leveled up yet.
		}
		return lvlProgress;
	}

	public static void logVerbose(string msg, params object[] args)
	{
		if (Data.debugMode)
		{
			int shouldLogVerbose = PlayerPrefsCache.GetInt(Prefs.VERBOSE_LOGGING, 0);

			if (shouldLogVerbose >= 1)
			{
				Debug.LogFormat(msg, args);
			}
		}
	}
	
	public static List<int> getIntListFromCommaSeperatedString(string intListString)
	{ 
		string[] splitIntStringList = intListString.Split(',');
		List<int> validInts = new List<int>();
		for (int i = 0; i < splitIntStringList.Length; i++)
		{
			int result;
			if (int.TryParse(splitIntStringList[i], out result))
			{
				validInts.Add(result);
			}
			else
			{
				Debug.LogWarning("Not a valid integer" + splitIntStringList[i]);
			}
		}

		return validInts;
	}
}



/// A common delegate type for ease of use
public delegate TResult Func<TArg0, TResult>(TArg0 arg0);
public delegate TResult Func<TArg0, TArg1, TResult>(TArg0 arg0, TArg1 arg1);
public delegate void GenericDelegate();
public delegate IEnumerator GenericIEnumeratorDelegate();

public static class Extensions
{
    public static System.Reflection.MethodInfo Method(this System.Delegate the)
    {
#if UNITY_WSA_10_0 && NETFX_CORE
        return the.GetMethodInfo();
#else
        return the.Method;
#endif
    }

    public static System.Reflection.MethodInfo GetMethod(this System.Type type, string name)
    {
#if UNITY_WSA_10_0 && NETFX_CORE
        return type.GetTypeInfo().GetDeclaredMethod(name);
#else
        return type.GetMethod(name);
#endif
    }

    public static System.Type ReflectedType(this System.Reflection.MemberInfo mi)
    {
		System.Type t = null;
#if UNITY_WSA_10_0 && NETFX_CORE
		t = mi.DeclaringType.GetTypeInfo().BaseType; //Gets the class that declares this member, differs from ReflectedType if 
#else
        t = mi.ReflectedType; //Gets the class object that was used to obtain this instance of MemberInfo.
#endif
		return t;
	}

	public static bool IsClass(this System.Type t)
    {
#if UNITY_WSA_10_0 && NETFX_CORE
        return t.GetTypeInfo().IsClass;
#else
        return t.IsClass;
#endif
    }

    public static bool IsGenericType(this System.Type t)
    {
#if UNITY_WSA_10_0 && NETFX_CORE
        return t.GetTypeInfo().IsGenericType;
#else
        return t.IsGenericType;
#endif
    }
}
