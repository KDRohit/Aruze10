#pragma warning disable 0618, 0168, 0414
/// URLStartupManager.cs
/// This class handles the parsing and processing of URLs that are passed into the game
/// when a user clicks on a link of the right format (which also opens the app)
/// We check for URLs at startup and on unpause
/// 
/// author: Nick Reynolds <nreynolds@zynga.com>
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;
using CustomLog;
using System.Runtime.InteropServices;
using Com.Scheduler;

public class URLStartupManager : IDependencyInitializer
{
	public Dictionary<string,string> urlParams { get; private set; }   // key-value pairs parsed from URL/query string
	private bool urlActionsReadyToProcess; // true if new urlParams have been set, ready to be processed
	private static string oldURL = "";

	public static URLStartupManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new URLStartupManager();
			}
			return _instance;
		}
	}
	private static URLStartupManager _instance;
	
	/// When we unpause the game, check if any URLs were passed
	public void pauseHandler(bool paused)
	{
		if (!paused)
		{
		    if (SlotsPlayer.isLoggedIn)
			{
				processUrlActions();
			}
		}
	}
	
	private void parseLatestUrl()
	{
		// Checking for a change in the URL
		copyLaunchURLToPlayerPrefs(); // Android Check, iOS check happens in native code.
		string newURL = PlayerPrefsCache.GetString("launchURL");

		if (newURL != oldURL && !string.IsNullOrEmpty(newURL))
		{
			Debug.Log("URLStartupManager: launchUrl = " + newURL);
			urlParams = parseURL(newURL);
			urlActionsReadyToProcess = true;

			// blank playerPrefs URL after parsing it
			PlayerPrefsCache.SetString("launchURL", "");

			oldURL = newURL;
		}
	}
	
	public void processUrlActions()
	{
		parseLatestUrl();
					
		// Right now all actions involve being in the main lobby or in a game
		if (urlActionsReadyToProcess && Overlay.instance != null)
		{
			processActions(urlParams);
			urlActionsReadyToProcess = false;
		}
	}
	
	// Parse the URL (if any) into a dictionary of parameters
	//
	// Returns a Dictionary of Key Value Pairs, null if empty url string (with no side affects)
	public static Dictionary<string, string> parseURL(string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return null;
		}

		Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
		url = WWW.UnEscapeURL(url);
		Bugsnag.LeaveBreadcrumb("Launch URL: " + url);

		string urlString = "";
		string queryString = "";

		int queryLocation = url.IndexOf('?');
		if (queryLocation >= 0)
		{
			// Seperate out the url from the query string. (if one exists).
		    urlString = url.Substring(0, queryLocation);
			queryString = url.Substring(queryLocation);
		}
		else
		{
			// Otherwise there is no querystring, so the urlString is the whole thing.
			urlString = url;
		}
			
		// With the new facebook deep-linking, we have moved some of the information into the url path,
		// rather than the querystring. We need to parse these out now.
		if (!string.IsNullOrEmpty(urlString))
		{
			// Remove the hititrich:// prefix if it exists.
			urlString = urlString.Replace("hititrich://", "");
			// Remove the spinitrich:// prefix if it exists.
			urlString = urlString.Replace("spinitrich://", "");

			string[] segments = urlString.Split(new char[] {'/'});
			int currentIndex = 0;
			while ((segments.Length - currentIndex) >= 2)
			{
				string key = segments[currentIndex];
				string val = segments[currentIndex + 1];

				if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val))
				{
					keyValuePairs[key] = val;
				}
				currentIndex += 2;
			}
		}
		
		
		if (!string.IsNullOrEmpty(queryString))
		{
			string[] segments = queryString.Split(new char[] {'?','&','#'});

			foreach (string s in segments)
			{
				if (s.Contains('='))
				{
					string[] param = s.Split(new char[] {'='});
					if (param.Length == 2)
					{
						if (!keyValuePairs.ContainsKey(param[0]))
						{
							keyValuePairs.Add(param[0], param[1]);
						}
						else
						{
							keyValuePairs[param[0]] = param[1];
						}
					}
				}
			}
		}

		//Debug.Log("url: params: " + JSON.createJsonString("", keyValuePairs));

		return keyValuePairs;
	}
	
	/// Loop through the dictionary of parameters and do actions for them if applicable. (no class side affects)
	private void processActions(Dictionary<string, string> actionParams)
	{
		if (actionParams != null && actionParams.Count > 0)
		{
			foreach(KeyValuePair<string,string> action in actionParams)
			{
				Log.log("Process action name: " + action.Key + " -- value: " + action.Value);
				switch(action.Key)
				{
					case "rewardKey":
					case "rewardkey":
					case "reward_key":
						// If we are using this as a reward action, then we want to keep using
						// this url at startup until it has been granted, or it errored out.
						// So we want to keep it stored.
						RewardAction.validateReward(action.Value);
					break;

					case "app_request_type":
						if (action.Value == "user_to_user")
						{
							Scheduler.addTask(new InboxTask());
						}	
						break;
					case "destination":
					    DoSomething.now(action.Value);
						break;
				}
			}

			logVisitURL(actionParams);
		}
	}
	
	private void logVisitURL(Dictionary<string,string> URLParams)
	{
		string affiliate = null;
		string actionType = null;
		string creative = "other";

		// Tracking for DAU from link
		if (URLParams.ContainsKey("fb_action_types"))
		{
			actionType = URLParams["fb_action_types"];
		}

		if (URLParams.ContainsKey("src"))
		{
			actionType = URLParams["src"];
		}

		if (URLParams.ContainsKey("fb_source"))
		{
			affiliate = URLParams["fb_source"];
		}

		if (URLParams.ContainsKey("aff"))
		{
			affiliate = URLParams["aff"];
		}

		if (URLParams.ContainsKey("crt"))
		{
			creative = URLParams["crt"];
		}

		if (!string.IsNullOrEmpty(affiliate) || !string.IsNullOrEmpty(actionType))
		{
			//we want a default direct actionType if it is left empty, meaning affiliate has a value and actionType does not
			if (!string.IsNullOrEmpty(affiliate) && string.IsNullOrEmpty(actionType))
			{
				//direct refers to directly from the app icon
				actionType = "direct";
			}

			StatsManager.Instance.LogVisit(actionType.Replace("%3A",":"), 0, 0, true, affiliate, creative, "", "", creative);
		}
	}

	// Be careful; this data is pulled from ZyngaUnityActivity.jar, which we have overwritten to be able to support this feature!
	private void copyLaunchURLToPlayerPrefs()
	{
#if UNITY_EDITOR
		string launchString = PlayerPrefsCache.GetString(DebugPrefs.LOCAL_URL, "");
		if (!string.IsNullOrEmpty(launchString))
		{
			PlayerPrefsCache.SetString("launchURL", launchString);
		}
#elif UNITY_IOS
		// iOS AppController+OpenURL.mm sets this directly in playerprefs, copy it to our playerprefscache
		string launchUrl = PlayerPrefs.GetString("launchURL", ""); // Yes, use PlayerPrefs
		if (!string.IsNullOrEmpty(launchUrl))
		{
			PlayerPrefsCache.SetString("launchURL", launchUrl);
		}
#elif UNITY_ANDROID
		AndroidJNI.AttachCurrentThread();
		AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
		//ZyngaUnityActivity puts parameters into a playerprefs for reading, check on initial load
		AndroidJavaClass zyngaUnityActivity = new AndroidJavaClass("com.zynga.ZyngaUnityActivity.ZyngaUnityActivity");
		if (zyngaUnityActivity != null)
		{
			// Grab the launch URL from the java class.
			string launchString = zyngaUnityActivity.CallStatic<string>("getLaunchURL");
			if (!string.IsNullOrEmpty(launchString))
			{
				PlayerPrefsCache.SetString("launchURL", launchString);
			}
			// Now that we have it, set it to null.
			zyngaUnityActivity.CallStatic("resetLaunchURL");
		}
#elif UNITY_WEBGL
		if (!string.IsNullOrEmpty(Application.absoluteURL))
		{
			PlayerPrefsCache.SetString("launchURL", Application.absoluteURL);
		}
#endif
	}


	// Updates "refValue" with value if a key-value-pair key exists, else leaves it alone
	public void updateRefValueFromKVP(string key, ref string refValue)
	{
		string value;
		if (urlParams != null && urlParams.TryGetValue(key, out value))
		{
			refValue = value;
			//czablocki - 2/3/2021 Commenting this out because it spams logs that crash v4.8.4 of Bugsnag when its
			//Notify() hook handles them to leave a Breadcrumb
			//Debug.Log("Using querystring kvp: " + key + " = " + value);
		}
		else
		{
			//Debug.Log("No querystring kvp found for: " + key);
		}
	}

	// overloaded variation for int refvals
	public void updateRefValueFromKVP(string key, ref int refValue)
	{
		string value;
		if (urlParams != null && urlParams.TryGetValue(key, out value))
		{
			int intValue = 0;
			int.TryParse(value, out intValue);
			refValue = intValue;
			//Debug.Log("Using querystring kvp: " + key + " = " + intValue);
		}
		else
		{
			//Debug.Log("No querystring kvp found for: " + key);
		}
	}

	// overloaded variation for bool refvals
	public void updateRefValueFromKVP(string key, ref bool refValue)
	{
		string value;
		if (urlParams != null && urlParams.TryGetValue(key, out value))
		{
			bool boolValue = false;
			bool.TryParse(value, out boolValue);
			refValue = boolValue;
			//Debug.Log("Using querystring kvp: " + key + " = " + boolValue);
		}
		else
		{
			//Debug.Log("No querystring kvp found for: " + key);
		}
	}

	
	#region ISVDependencyInitializer implementation	
	// This method should be implemented to return the set of class type definitions that the implementor
	// is dependent upon.
	public System.Type[] GetDependencies() 
	{
		return new System.Type[] {};
	}

	// This method should contain the logic required to initialize an object/system.  Once initialization is
	// complete, the implementing class should call the "mgr.InitializationComplete(this)" method to signal
	// that downstream dependencies can be initialized.
	public void Initialize(InitializationManager mgr) 
	{
		parseLatestUrl();
		mgr.InitializationComplete(this);
	}
	
	// short description of this dependency for debugging purposes
	public string description()
	{
		return "URLStartupManager";
	}
	#endregion
}
#pragma warning restore 0618, 0168, 0414
