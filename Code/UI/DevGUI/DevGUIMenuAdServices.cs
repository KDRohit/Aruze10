using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CustomLog;

/*
A dev panel.
*/

public class DevGUIMenuAdServices : DevGUIMenu
{
	public static bool useOneSecondCoinsComingDelay;	// override wait time when checking if should show coins are coming dialog
	private static	string w2eTrackingLog = "";
	private static	string w2eTrackingDisplayLog = "";
	private int logBufferSize;
	
	private static UnityAdsManager.PlacementId placementId = UnityAdsManager.PlacementId.VIDEO;
	public override void drawGuts()
	{
		//titles
		GUIStyle headingStyle = new GUIStyle();
		headingStyle.fontSize = 20;
		headingStyle.border.top = 2;
		headingStyle.border.bottom = 2;

		//data text
		GUIStyle defaultStyle = new GUIStyle();
		defaultStyle.fontSize = 14;
		
		//error text
		GUIStyle redStyle = new GUIStyle();
		redStyle.fontSize = 14;
		redStyle.normal.textColor = Color.red;

		GUILayout.BeginVertical();
		
		//Watch 2 Earn details
		GUILayout.Label("Watch 2 Earn:", headingStyle);
		GUILayout.Label("Is Initialized: " + WatchToEarn.isInitialized, defaultStyle);
		GUILayout.Label("Is Enabled: " + WatchToEarn.isEnabled, defaultStyle);
		GUILayout.Label("Is Server Enabled: " + WatchToEarn.isServerEnabled, defaultStyle);
		GUILayout.Label("Is Ad Available: " + WatchToEarn.isAdAvailable, defaultStyle);
		GUILayout.Label("Inventory: " + WatchToEarn.inventory, defaultStyle);

		if (!WatchToEarn.isEnabled)
		{
			GUILayout.Label(WatchToEarn.getNotAvailableReason(), redStyle);
		}
		
		//20 pixel break
		GUILayout.Space(20);
		
		//Unity ads details
		GUILayout.Label("Unity Ads:", headingStyle);
		GUILayout.Label("Current placement id: " + System.Enum.GetName(typeof(UnityAdsManager.PlacementId), placementId), defaultStyle);
		GUILayout.Label("Is initialized: " + (UnityAdsManager.instance != null && UnityAdsManager.instance.isInitialized), defaultStyle);
		GUILayout.Label("Is ad ready: " + UnityAdsManager.isAdAvailable(placementId), defaultStyle);
		
		//20 pixel break
		GUILayout.Space(20);
		
		//Buttons to change placement id
		GUILayout.Label("Change Unity Ads Placement ID:", headingStyle);
		GUILayout.BeginHorizontal();
		foreach (UnityAdsManager.PlacementId id in (UnityAdsManager.PlacementId[]) System.Enum.GetValues(typeof(UnityAdsManager.PlacementId)))
		{
			if (GUILayout.Button(System.Enum.GetName(typeof(UnityAdsManager.PlacementId), id)))
			{
				placementId = id;
			}
		}
		GUILayout.EndHorizontal();
		
		// so we can test on device if ad network is responding quicly. (default wait time is 10seconds)
		useOneSecondCoinsComingDelay = GUILayout.Toggle(useOneSecondCoinsComingDelay, "Coins are coming dialog will only wait 1 second before showing");
		
		if (GUILayout.Button("W2E Collect Dialog"))
		{
			WatchToEarnCollectDialog.showDialog(WatchToEarn.rewardAmount, "");
			DevGUI.isActive = false;
		}

		renderLog();
		
		GUILayout.EndVertical();
	}

	public static void resetStaticClassData()
	{
		placementId = UnityAdsManager.PlacementId.VIDEO;
	}

	public static void w2eLog(string msg)
	{
		w2eTrackingLog += msg + "\n";
	}		
	
	private void renderLog()
	{
		if (logBufferSize != w2eTrackingLog.Length)
		{
			if (w2eTrackingLog.Length > 32000)   // unity gets unhappy if it has to render more than this
			{
				w2eTrackingDisplayLog = w2eTrackingLog.Substring(w2eTrackingLog.Length - 32000);
			}
			else
			{
				w2eTrackingDisplayLog = w2eTrackingLog;
			}

			logBufferSize = w2eTrackingLog.Length;
		}

		GUILayout.BeginHorizontal();
		GUI.enabled = false;		
		GUILayout.TextArea(w2eTrackingDisplayLog);
		GUI.enabled = true;		
		GUILayout.EndHorizontal();
	}

}
