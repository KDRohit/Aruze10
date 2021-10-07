using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
The top-level dev panel menu. Mostly a portal for submenus.
*/

public class DevGUIMenuMain : DevGUIMenu
{	
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("RTSS: " + Time.realtimeSinceStartup.ToString("0.000"));
		GUILayout.Label("SSSS: " + GameTimer.SSSS.ToString("0.000"));
		GUILayout.EndHorizontal();

		GUILayout.TextArea("Start URL: " + PlayerPrefsCache.GetString("previous_launchURL", ""));

		GUILayout.Space(10);

		//Install Date
		GUILayout.BeginHorizontal();
		Glb.installDateTime = dateInputField("Install Date", Glb.installDateTime, 1);
		GUILayout.EndHorizontal();

		GUILayout.Space(10);

		PlayerResource.displayLog(isHiRes);
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
