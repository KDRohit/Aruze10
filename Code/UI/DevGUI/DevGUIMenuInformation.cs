using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuInformation : DevGUIMenu
{
	public override void drawGuts()
	{
		GUILayout.BeginVertical();
		
		GUILayout.Label("Data version : " + SlotGameData.dataVersion);
		
		GUILayout.Label(Loading.instance.loadingUserflowString);
		
		GUILayout.Label(AssetBundleDownloader.bundleDownloadUserflowLog);
		
		GUILayout.EndVertical();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
