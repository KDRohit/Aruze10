using UnityEngine;
using System;
using System.Reflection;

class DevGUIMenuZIDSwitch : DevGUIMenu
{
	private static string userZid = "";
	
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Specify a user ZID to simulate");

		userZid = GUILayout.TextField(userZid).Trim();

		if (GUILayout.Button("Reload Game as user.") && !string.IsNullOrEmpty(userZid))
		{
			Glb.resetGame("Reloading game as user: " + userZid);
		}

		PlayerPrefsCache.SetString(Prefs.SIMULATED_USER_ID, userZid);
		
		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		
	}
}