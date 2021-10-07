using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Zynga.Zdk;

/*
Game Network XPromo dev panel.
*/

public class DevGUIMenuNetworkXPromo : DevGUIMenu
{
	public static Dictionary<string, string> xpromoAppIdMap = new Dictionary<string, string>()
	{
		{ "woz", AppsManager.WOZ_SLOTS_ID },
		{ "wonka", AppsManager.WONKA_SLOTS_ID },
		{ "hir", AppsManager.HIR_SLOTS_ID },
		{ "bdc", AppsManager.BDC_SLOTS_ID },
		{ "got", AppsManager.GOT_SLOTS_ID }
	};

	private string appId = "";
	private bool installed = false;
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Network Games Detected as Installed:");
		GUILayout.EndHorizontal();
		
		foreach (KeyValuePair<string, string> kvp in xpromoAppIdMap)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(kvp.Key, GUILayout.Width(150));
			GUILayout.Label(AppsManager.isBundleIdInstalled(kvp.Value).ToString());
			GUILayout.EndHorizontal();
		}


		GUILayout.BeginHorizontal();
		GUILayout.Label("Network ID: " + SlotsPlayer.instance.socialMember.networkID.ToString());
		appId = GUILayout.TextField(appId);
		if (GUILayout.Button("Check AppID"))
		{
			installed = AppsManager.isBundleIdInstalled(appId);
		}
		GUILayout.Label(installed.ToString());
		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
