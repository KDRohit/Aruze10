// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class AppsManager
{
#if UNITY_IPHONE && !UNITY_EDITOR
	[DllImport ("__Internal")] 
	private static extern bool CheckCanOpenApp(string callbackId);
#elif UNITY_ANDROID && !UNITY_EDITOR
	static AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
	static AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
	static AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
#endif

#if UNITY_EDITOR
	public const string WOZ_SLOTS_ID = "WoZSlots";
	public const string WONKA_SLOTS_ID = "WonkaSlots";
	public const string HIR_SLOTS_ID = "hititrich";
	public const string BDC_SLOTS_ID = "blackdiamondcasino";
	public const string GOT_SLOTS_ID = "gotslots";
#elif UNITY_IPHONE
	public const string WOZ_SLOTS_ID = "WoZSlots";
	public const string WONKA_SLOTS_ID = "WonkaSlots";
	public const string HIR_SLOTS_ID = "hititrich";
	public const string BDC_SLOTS_ID = "blackdiamondcasino";
	public const string GOT_SLOTS_ID = "gotslots";
#elif ZYNGA_GOOGLE
	public const string WOZ_SLOTS_ID = "com.zynga.wizardofoz";
	public const string WONKA_SLOTS_ID = "com.zynga.wonka";
	public const string HIR_SLOTS_ID = "com.zynga.hititrich";
	public const string BDC_SLOTS_ID = "com.risingtidegames.blackdiamondslots.beta";
	public const string GOT_SLOTS_ID = "com.zynga.gotslots";
#elif ZYNGA_KINDLE
	public const string WOZ_SLOTS_ID = "com.zynga.wizardofoz";
	public const string WONKA_SLOTS_ID = "com.zynga.wonka";
	public const string HIR_SLOTS_ID = "com.zynga.hititrich";
	public const string BDC_SLOTS_ID = "com.risingtidegames.blackdiamondslots.beta";
	public const string GOT_SLOTS_ID = "com.zynga.gotslots";
#elif UNITY_WSA_10_0 && NETFX_CORE
	// TODO
	public const string WOZ_SLOTS_ID = "com.zynga.wizardofoz";
	public const string WONKA_SLOTS_ID = "com.zynga.wonka";
	public const string HIR_SLOTS_ID = "com.zynga.hititrich";
	public const string BDC_SLOTS_ID = "com.risingtidegames.blackdiamondslots.beta";
#else
	public const string WOZ_SLOTS_ID = "";
	public const string WONKA_SLOTS_ID = "";
	public const string HIR_SLOTS_ID = "";
	public const string BDC_SLOTS_ID = "";
	public const string GOT_SLOTS_ID = "";
#endif

	public AppsManager ()
	{
	}
	
	public static bool isBundleIdInstalled(string appBundleID)
	{
		bool isInstalled = false;
		
#if UNITY_EDITOR

		// In the editor we use player prefs from the login settings to simulate whether an app is installed.
		// Split the comma-separated list of simulated installed app id's.
		string[] appIds = PlayerPrefsCache.GetString(DebugPrefs.SIMULATED_INSTALLED_APP_IDS, "").Replace(" ", "").Split(',');
		isInstalled = (System.Array.IndexOf(appIds, appBundleID) > -1);
		
#elif UNITY_IPHONE

		isInstalled = CheckCanOpenApp(appBundleID);
		
#elif UNITY_ANDROID

		AndroidJavaObject launchIntent = null;
		try
		{
			launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", appBundleID);

			isInstalled = null != launchIntent;
			
		} catch(Exception ex) {
			isInstalled = false;
		}
		finally
		{
			if (null != launchIntent)
			{
				launchIntent.Dispose();
				launchIntent = null;
			}
		}
#endif
		
		return isInstalled;
	}

	public static void launchBundle(string appBundleID)
	{
		if (!isBundleIdInstalled(appBundleID))
		{
			return;
		}

#if UNITY_EDITOR
		Debug.LogFormat("AppsManager.cs -- launchBundle() -- attemping to load into app: {0}", appBundleID);
		//nothing

#elif UNITY_IPHONE

		Application.OpenURL(appBundleID);
		
#elif UNITY_ANDROID

		AndroidJavaObject launchIntent = null;
		AndroidJavaObject currentActivity = null;
		AndroidJavaObject unityPlayer = null;
		try
		{
			launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", appBundleID);
			unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			currentActivity =  unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			currentActivity.Call("startActivity", launchIntent);
			
		} catch(Exception ex) 
		{
			Debug.LogError("Unable to launch application:" + System.Environment.NewLine + ex.ToString());
		}
		finally
		{
			if (null != launchIntent)
			{
				launchIntent.Dispose();
				launchIntent = null;
			}

			if (null != currentActivity)
			{
				currentActivity.Dispose();
				currentActivity = null;
			}

			if (null != unityPlayer)
			{
				unityPlayer.Dispose();
				unityPlayer = null;
			}
		}		
#endif
	}
}	
