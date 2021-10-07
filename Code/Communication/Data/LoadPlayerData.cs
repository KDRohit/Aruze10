using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using Zynga.Zdk;
using Zynga.Zdk.Services.Common;
using Zynga.Core.Util;
using Facebook.Unity;
using Zynga.Core.Platform;
using Zynga.Core.UnityUtil;
using System;
using System.Collections.ObjectModel;
using Zynga.Authentication;

public class LoadPlayerData : IResetGame
{
	public const string RESPONSE_KEY = "LoadPlayerData";
	public const string RESPONSE_CACHE_FILE = "_player_data";

	public static float lastPlayerDataRequestTime;
	private static string loginUrl
	{
		get
		{
			if (Application.isEditor)
			{
				return Glb.loginUrl;
			}
			else
			{
				if (_loginUrl == null || _loginUrl == "")
				{
					_loginUrl = Glb.loginUrl;
				}

				return _loginUrl;
			}
		}
	}
	private static string _loginUrl = null;
	
	// Reusable function with nullchecking built in.
	private static string getAnonZid()
	{
		ReadOnlyCollection<AccountDetails> accounts = PackageProvider.Instance.Authentication.AccountStore.GetAccounts();
		if (accounts.Count > 0)
		{
			foreach (AccountDetails account in accounts)
			{
				if (account.IsAnonymous)
				{
					return account.GameAccount.PlayerId.ToString();
				}
			}
		}
		return null;
	}

	private static IEnumerator retrieveMergeCount(System.Action loginDoneCallback, Dictionary<string,string> elements)
	{
		int currentMergeCount = 0;
		int currentMergeMax = 0;
		string anonZid = getAnonZid();

		// This protocol will retrieve the users merge count for the facebook account they're trying to use
		yield return RoutineRunner.instance.StartCoroutine(Server.attemptRequest(Glb.zidCheckUrl, elements, "", "OnGetMergeCount"));

		// Grab the response and set currentMergeCount and currentMergeMax. 
		JSON response = Server.getResponseData("OnGetMergeCount");
		currentMergeCount = response.getInt("merge_count", -1);
		currentMergeMax = response.getInt("merge_max", -1);
		// If we have some merges left
		if (currentMergeCount < currentMergeMax)
		{
			// Pop a dialog and give the user the option to merge
			/*GenericDialog.showDialog(
				Dict.create(
					D.TITLE,		Localize.text("alert"),
					D.MESSAGE,		Localize.text("merge_warning_mobile_{0}", "Facebook"),
					D.OPTION1,		Localize.textUpper("yes"),
					D.OPTION2,		Localize.textUpper("no"),
					D.CALLBACK,		new DialogBase.AnswerDelegate(mergeDialogCallback),
					D.DATA,			elements,
					D.CUSTOM_INPUT, loginDoneCallback,
					D.REASON, "player-data-merge-alert"
				),
				true
			);*/
			mergeDialogCallback(Dict.create
			(
				D.ANSWER, "1",
				D.CUSTOM_INPUT, loginDoneCallback,
				D.DATA, elements
			));
		}
		else
		{
			RoutineRunner.instance.StartCoroutine(finishLoginRoutine(elements, anonZid, loginDoneCallback));    
		}
	}

	private static void mergeDialogCallback(Dict answerArgs)
	{
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		string anonZid = getAnonZid();
		System.Action loginDoneCallback = answerArgs.getWithDefault(D.CUSTOM_INPUT, null) as System.Action;
		Dictionary<string,string> elements = answerArgs.getWithDefault(D.DATA, null) as Dictionary<string,string>;
		if ((answerArgs.getWithDefault(D.ANSWER, "") as string) == "1")
		{
			StatsManager.Instance.LogCount("dialog", "fb_connect_persistent");
			Debug.LogWarning("Upgrading Anonymous account to facebook, passing: " + anonZid + " as anonymous zid, " + ZdkManager.Instance.Zsession.Zid + " as new zid");
			RoutineRunner.instance.StartCoroutine(finishLoginRoutine(elements, anonZid, loginDoneCallback));
		}
		else
		{
			StatsManager.Instance.LogCount("dialog", "fb_disconnect_persistent");
			SlotsPlayer.facebookLogout();
			UnityPrefs.SetInt(SocialManager.kLoginPreference, (int)SocialManager.SocialLoginPreference.Anonymous); // Go back to anonymous.
			UnityPrefs.SetInt(SocialManager.kFacebookLoginSaved, 0); // Need to clear this also in order to login anonymous.
			UnityPrefs.Save();
			Glb.resetGame("Player declined account merge dialog.");
		}
		
	}

	public static void getLoginData(string zid, ServiceSession session, System.Action loginDoneCallback, bool ios = false)
	{
		
		// Setup login form
		Dictionary<string, string> elements = new Dictionary<string,string>();

		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();

		if (!string.IsNullOrEmpty(UnityPrefs.GetString(Prefs.SIMULATED_USER_ID, "")))
		{
			zid = UnityPrefs.GetString(Prefs.SIMULATED_USER_ID, "");
		}
		
		// Only used if we are in the google upgrade process.
		string googleUpgradeZid = zid;
		if (SocialManager.isInGoogleUpgrade)
		{
			// If we are upgrading the google zid, then we need to swap the logged in zid
			// Set anon zid to whatever we have stored, defaulting to nothing.
			string anonymousZid = UnityPrefs.GetString(Prefs.ANONYMOUS_ZID, "");
			// If anon zid is nothing, grab one anyway.
			if (string.IsNullOrEmpty(anonymousZid))
			{
				anonymousZid = getAnonZid();
			}

			if (zid != anonymousZid)
			{
				// Override the login zid
				zid = anonymousZid;
				elements.Add("zid_to_upgrade", googleUpgradeZid);
			
				string message = "Starting user migration from google+ to anonymous.";
				Dictionary<string, string> extraFields = new Dictionary<string, string>();
				extraFields.Add("old_google_zid", googleUpgradeZid);
				extraFields.Add("anonymous_login_zid", zid);
				Server.sendLogInfo("Google Upgrade", message, extraFields);
			}
			else
			{
				// If the merge zid is the same as the login zid, then don't do the merge and log an error.
				string message = "Not performing migration from google+ to anonymous because the migration zid is the same as the login zid.";
				Dictionary<string, string> extraFields = new Dictionary<string, string>();
				extraFields.Add("old_google_zid", googleUpgradeZid);
				extraFields.Add("anonymous_login_zid", zid);
				Server.sendLogInfo("Google Upgrade", message, extraFields);
			}
		}
	   
		//Debug.Log("LOGGING IN WITH ZID: " + zid);
		
		// Set early zid for error logging (updates later once we have more data).
		Bugsnag.SetUser(zid, "", "");
		Bugsnag.StartSession();
		
		elements.Add("zid",	zid);
		elements.Add("client_id", (int)ZyngaConstants.ClientId + "");
		elements.Add("client_build", Glb.clientVersion);
		elements.Add("missing_feature_assets", string.Join(",", AssetBundleManager.getLoadedBundles().ToArray()));
		elements.Add("install_credentials", PackageProvider.Instance.Authentication.Flow.Account.InstallCredentials.ToString());
		//Debug.Log("Missing Feature Assets : " + elements["missing_feature_assets"]);

		//Don't send empty ad_id or the default empty id (00000000-0000-0000-0000-000000000000)
		if (!string.IsNullOrEmpty(Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID) && !Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID.Equals("00000000-0000-0000-0000-000000000000")) 
		{
			elements.Add("ad_id", Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID);
		}

		string userKey = session.Password;


		if (!string.IsNullOrEmpty(userKey))
		{
			Debug.Log("userKey: " + userKey);
			string userKeyHash = Common.Base64Encode(userKey);
			if (!string.IsNullOrEmpty (userKeyHash))
			{
				Debug.Log("Adding auth_user_hash: " + userKeyHash);
				elements.Add("auth_user_hash", userKeyHash);
			}
		}

		string authToken = "";
		if (session != null)
		{
			elements.Add("auth_user_id", session.Snuid.ToStringInvariant());
			authToken = session.Token;
		}

		string language = Application.systemLanguage.ToString().ToLower();
		if (!string.IsNullOrEmpty(language))
		{
			elements.Add("lang", language);
		}

		// If the player already has and a fb auth token pass it up to the backend, if the user is logging in non-anonymously.
		// This is most relavent to the mobile build but could come in handy for the editor also.  However, we don't want to send this up to the server 
		// while anonymous, because it makes the server search for an fbid and causes problems with experiments.
		//if (!string.IsNullOrEmpty(authToken) && (UnityPrefs.GetInt(SocialManager.kFacebookLoginSaved,0) == 1 || UnityPrefs.GetInt(SocialManager.kLoginPreference,0) == 2))
		if (SlotsPlayer.isFacebookUser)
		{
			// !!! dont pass along the access token if we are simulating a user's zid, this will update server side data for the user with things that are undesirable
			if (string.IsNullOrEmpty(UnityPrefs.GetString(Prefs.SIMULATED_USER_ID, "")))
			{
				if (UnityPrefs.HasKey(SocialManager.fbToken))
				{
					authToken = UnityPrefs.GetString(SocialManager.fbToken);
					Debug.LogFormat("fb_token {0}", authToken);
					elements.Add("fb_token", authToken);
					logAccessToken("FB: " + authToken);
				}
				else
				{
					Debug.LogFormat("auth_token {0}", authToken);
					elements.Add("fb_token", "");
					logAccessToken("FB-token-is-null");
				}

			}
			else
			{
				Debug.Log("is simulated user id");	
			}
		} 
		else
		{
			Debug.Log("Not FB: " + authToken);
			logAccessToken("Not FB: " + authToken);
		}

		if (PackageProvider.Instance.Track.Service != null)
		{
			long? appLoadId = PackageProvider.Instance.Track.Service.CurrentAppLoadId;
			if (appLoadId.HasValue)
			{
				elements.Add("app_load_id", appLoadId.Value.ToString());
			}
		}

		// Set anon zid to whatever we have stored, defaulting to nothing.
		
		string anonZid = UnityPrefs.GetString(Prefs.ANONYMOUS_ZID, "");

		//Zis sdk is present
		elements.Add("zis_sdk", "true");
		elements.Add("zis_token", PackageProvider.Instance.Authentication.Flow.Account.ZisToken.Token);

		if (SlotsPlayer.isFacebookUser)
		{
			elements.Add("sn_id", (int)Snid.Facebook + "");
		}
		else if (SlotsPlayer.isAnonymous)
        {
			elements.Add("sn_id", (int)Snid.Anonymous + "");
		}
		else
		{
			elements.Add("sn_id", (int)ZdkManager.Instance.Zsession.Snid + "");
		}

		if (UnityPrefs.GetInt(SocialManager.conflictSwitch) == 0)
		{
			if (SocialManager.Instance._previousSession != null && UnityPrefs.GetInt(SocialManager.kUpgradeZid) == 1)
			{
				// If anon zid is nothing, grab one anyway.
				if (string.IsNullOrEmpty(anonZid))
				{
					anonZid = getAnonZid();
				}
				elements.Add("zid_to_upgrade", anonZid);
				UnityPrefs.SetInt(SocialManager.kUpgradeZid, 0);
				UnityPrefs.Save();
				Debug.LogFormat("Elements 1 ");
				printoutElements(elements);
				RoutineRunner.instance.StartCoroutine(retrieveMergeCount(loginDoneCallback, elements));
			}
			else
			{
				// If the player is logging into a social network: map the associate the anon Zid with the authenticated Zid if not already done.
				// This is also included if the user is a new siwa user
				if (FB.IsLoggedIn)
				{
					string associatedZid = getAnonZid();
					if (associatedZid != "")
					{
						//elements.Add("zid_to_associate", associatedZid);
					}
#if !UNITY_WEBGL
					else
					{
						Debug.LogError("No Anon user session associated with SNID: " + ZdkManager.Instance.Zsession.Snid + ", Zid: " + zid);
					}
#endif
				}
				Debug.LogFormat("Elements 2 ");
				printoutElements(elements);
				RoutineRunner.instance.StartCoroutine(finishLoginRoutine(elements, anonZid, loginDoneCallback));
			}
		}
		else
		{
			
			UnityPrefs.SetInt(SocialManager.conflictSwitch, 0);
			UnityPrefs.Save();
			Debug.LogFormat("Elements 3 ");
#if !ZYNGA_PRODUCTION
			printoutElements(elements);
#endif
			RoutineRunner.instance.StartCoroutine(finishLoginRoutine(elements, anonZid, loginDoneCallback));
		}
	}
	private static void printoutElements(Dictionary<string, string> elements = null) 
	{
		foreach (KeyValuePair<string, string> kvp in elements)
		{
			
			string test = string.Format("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
			Debug.LogFormat("Elements {0}", test);

		}
	}

	//Logging access token
	private static void logAccessToken(string authToken)
	{
		Dictionary<string, string> extraFields = new Dictionary<string, string>();
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		extraFields.Add("access_token", authToken);
		extraFields.Add("kFacebookLoginSaved", UnityPrefs.GetInt(SocialManager.kFacebookLoginSaved, 0).ToString());
		extraFields.Add("kLoginPreference", UnityPrefs.GetInt(SocialManager.kLoginPreference, 0).ToString());
		Server.sendLogInfo("log_accesstoken", "logging accesstoken", extraFields);
	}

	// Function that checks and connects FB if the user is already connected to SIWA
	private static void checkAndConnectFB()
	{

		if (SlotsPlayer.getPreferences().HasKey(SocialManager.fbToken))
		{
			Debug.LogFormat("AppleLogin: in loadplayer has FB token {0}", SlotsPlayer.getPreferences().GetString(SocialManager.fbToken));
			SlotsPlayer.IsFacebookConnected = true;
		}
		else
		{
			//Debug.Log("AppleLogin: in loadplayer has FB token not present");
		}
	}

	//function that check to see if authorization code is valid or expired
	private static bool isAppleAuthCodeValid()
	{
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		if (UnityPrefs.HasKey(SocialManager.siwaAuthCodeIssueTime))
		{
			int expirationTime = (int)UnityPrefs.GetDouble(SocialManager.siwaAuthCodeIssueTime, 0) + SocialManager.SIWA_AUTH_CODE_EXIPRATION_TIME;
			int currentTime = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

			Debug.LogFormat("AppleLogin: expiration time {0} current time {1}", expirationTime.ToString(), currentTime.ToString());
			if (currentTime < expirationTime)
			{
				return true;
			}
		}
		return false;
	}


	private static bool isRecycledZid(float experience, bool viewedTOS, bool isFirstLogin)
	{
#if UNITY_WEBGL && !UNITY_EDITOR
		return false;
#else
		//If the user has logged in since gdpr is active it's impossible to have 0 experience, server data is invalid
		return 0 == experience && !isFirstLogin && !viewedTOS;
#endif
	}

	private static IEnumerator finishLoginRoutine(Dictionary<string, string> elements, string anonZid, System.Action loginDoneCallback)
	{	
		Dictionary<string, string> newFields = new Dictionary<string, string>();
		PreferencesBase UnityPrefs = SlotsPlayer.getPreferences();
		// Get PlayerData...
		JSON jsonData = null;
		if (Data.canvasBasedPlayerData != null)
		{
			// Use canvas-based player data if it exists (for WebGL)
			//Debug.Log("Using canvas-based PlayerData");
			jsonData = Data.canvasBasedPlayerData;
		}
		else
		{
			// Else request playerdata from server...
			Debug.LogFormat("Requesting server-based PlayerData elements {0} login url {1}", elements.ToString(), loginUrl);

			Loading.show(Loading.LoadingTransactionTarget.LOBBY_LOGIN);
			lastPlayerDataRequestTime = Time.realtimeSinceStartup;
			StatsManager.Instance.LogLoadTimeStart("LoginDataRequest");
			yield return RoutineRunner.instance.StartCoroutine(Server.attemptRequest(loginUrl, elements, "error_failed_to_login", RESPONSE_KEY, false, RESPONSE_CACHE_FILE, false));
			StatsManager.Instance.LogLoadTimeEnd("LoginDataRequest");

			jsonData = Server.getResponseData(RESPONSE_KEY, false);
		}


		if (jsonData == null)
		{
			Debug.LogError("Failed to get login data, bailing.");
			Loading.hide(Loading.LoadingTransactionResult.FAIL);
			bool inMaintenanceMode = Data.liveData.getBool("USE_APP_ENABLED_PERCENT", false);
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, Localize.text("maintenance"),
					D.MESSAGE, inMaintenanceMode ? Localize.text("under_construction") : Localize.text("actions_error_message"),
					D.REASON, "load-player-data-failed",
					D.CALLBACK, new DialogBase.AnswerDelegate( (args) => { Glb.resetGame("Failed to get login data."); })
				),
				SchedulerPriority.PriorityType.MAINTENANCE
			);
			yield break;
		}


		//before we finish login check for invalid client data
		JSON playerJSON = null;
		if (Data.liveData.getBool("GDPR_CLIENT_ENABLED", false))
		{
			/*
				Have to do this the hard way because we don't want to load all this data yet GDPR regulations prevents us form doing so
			 */
			playerJSON = jsonData.getJSON("player");
			JSON customData = null == playerJSON ? null : playerJSON.getJSON("custom_data");
			JSON tosBlob = null == customData ? null : customData.getJSON(CustomPlayerData.SEEN_TERMS_OF_SERVICE);
			CustomPlayerData data = null == tosBlob ? null : new CustomPlayerData(CustomPlayerData.SEEN_TERMS_OF_SERVICE, 
					tosBlob.getString("last_updated", System.DateTime.Now.ToString()),
					tosBlob.getString("value", ""));

			//has the user accepted the tos
			bool viewedTOS = null == data ? false : CustomPlayerData.getBool(CustomPlayerData.SEEN_TERMS_OF_SERVICE, false, data);
			viewedTOS = viewedTOS || TOSDialog.setCustomData;

			string installDateTimeString = UnityPrefs.GetString(Prefs.FIRST_APP_START_TIME, null);
			bool isFirstLogin = string.IsNullOrEmpty(installDateTimeString) || NotificationManager.DayZero;

			float experience = playerJSON.getFloat("experience", 0);

			//for existing users that haven't run with gdpr code yet
			if (!viewedTOS && !isFirstLogin)
			{
				TOSDialog.GDPRUpgrade();
				viewedTOS = true;
			}

			if (Data.liveData.getBool("GDPR_CHECK_RECYCLED_ZID", true))
			{
				if (isRecycledZid(experience, viewedTOS, isFirstLogin))
				{
					//delete all data
					UnityPrefs.DeleteAll();
					UnityPrefs.Save();

					//reset the game
					Glb.resetGame("Failed to get login data.");

					//abort current instance
					yield break;
				}
			}
		}

		StatsManager.Instance.LogLoadTimeStart("LoginDataSet");
		Data.setLoginData(jsonData);
		StatsManager.Instance.LogLoadTimeEnd("LoginDataSet");

		if (TOSDialog.setCustomData)
		{
			CustomPlayerData.setValue(CustomPlayerData.SEEN_TERMS_OF_SERVICE, true);
		}
		
#if !UNITY_WEBGL || UNITY_EDITOR
		// Do not associate when on FB canvas using WebGL!
		
		if (SocialManager.isInGoogleUpgrade)
		{
			string message = "Google Plus user finished migration to anonymous.";
			Dictionary<string, string> extraFields = new Dictionary<string, string>();
			extraFields.Add("login_zid", anonZid);
			Server.sendLogInfo("Google Upgrade", message, extraFields);
		}

		if (!string.IsNullOrEmpty(anonZid) && anonZid != "none")
		{
			ServiceSession session = ZdkManager.Instance.Zsession;
			
			if (session != null && session.Snid != Snid.Anonymous)
			{
				// Log when a user switches from anonymous play to FB Connected
				long? previousSessionSnid = null;
				long? previousSessionZid = null;
				if (SocialManager.Instance != null && SocialManager.Instance._previousSession != null)
				{
					previousSessionSnid = (long)SocialManager.Instance._previousSession.Snid;
					previousSessionZid = long.Parse(SocialManager.Instance._previousSession.Zid.ToString());
					newFields.Add("prevSessionZid", SocialManager.Instance._previousSession.Zid.ToString());
					newFields.Add("prevSessionSnid", SocialManager.Instance._previousSession.Snid.ToString());
				}
				string androidDeviceId = "";
				#if UNITY_ANDROID
				androidDeviceId = Zynga.Slots.ZyngaConstantsGame.androidDeviceID;
				#endif
				StatsManager.Instance.LogAssociate("snuid_device_mapping", Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID, ZyngaConstants.GameSkuVersion, SystemInfo.operatingSystem, StatsManager.DeviceModel, "", "", previousSessionSnid, previousSessionZid, androidDeviceId);


				newFields.Add("zdkZid", session.Zid.ToString());
				newFields.Add("anonChannelZid", getAnonZid());
			} 	

			if (session != null)
			{
				newFields.Add("zdkSnid", session.Snid.ToString());
			}
		}

		UnityPrefs.SetString(Prefs.ANONYMOUS_ZID, anonZid.ToString());
#endif

#if UNITY_WEBGL
		JSON oneClickBuyJSON = jsonData.getJSON("oneclickbuy");

		int hasOneClickBuyPackage = 0;
		string oneClickBuyPackageName = "";
		string oneClickBuyPackageBonus = "";
		string oneClickBuyVariant = "";
		string bonusDisplay = "";
		string offerDisplay = "";
		if(oneClickBuyJSON != null)
		{
			hasOneClickBuyPackage = oneClickBuyJSON.getInt("hasOneClickBuyPackage", 0);
			oneClickBuyPackageName = oneClickBuyJSON.getString("oneClickBuyPackageName", "");
			oneClickBuyPackageBonus = oneClickBuyJSON.getString("oneClickBuyPackageBonus", "");
			oneClickBuyVariant = oneClickBuyJSON.getString("oneClickBuyVariant", "");
			bonusDisplay = oneClickBuyJSON.getString("bonusDisplay", "");
			offerDisplay = oneClickBuyJSON.getString("offerDisplay", "");
		}
		else
		{
			Debug.LogWarning("Missing oneclickbuy data.");
		}

		string personalDataRequestURL = jsonData.getString("data_request_url", "");
		string personalSupportURL = jsonData.getString("support_url", "");

		JSON initJSON = jsonData.getJSON("init");
		string fbId = initJSON.getString("fb_id", "");

		playerJSON = playerJSON == null ? jsonData.getJSON("player") : playerJSON;
		if(playerJSON != null)
		{
			string zid = playerJSON.getString("id", "");
			int vipLevel = playerJSON.getInt("vip_level_buffed", 0);

			// Don't love passing in this many variables, but the JS layer only allows primitive types.
			WebGLFunctions.UpdateWebGLData(zid, fbId, vipLevel, personalDataRequestURL, personalSupportURL, hasOneClickBuyPackage, oneClickBuyPackageName, oneClickBuyPackageBonus, oneClickBuyVariant, bonusDisplay, offerDisplay);
		}
		else
		{
			Debug.LogError("Missing player json data.");
		}


#endif

		loginDoneCallback();
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		lastPlayerDataRequestTime = 0;
	}
}
