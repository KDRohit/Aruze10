using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;

/*
Game Network dev panel.
*/

public class DevGUIMenuGameNetwork : DevGUIMenu
{
	
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Network ID: " + SlotsPlayer.instance.socialMember.networkID.ToString());
		GUILayout.Label(string.Format("Network isPending: {0}", LinkedVipProgram.instance.isPending));
		GUILayout.Label(string.Format("Network isConnected: {0}", LinkedVipProgram.instance.isConnected));
		GUILayout.Label(string.Format("Network isElligible: {0}", LinkedVipProgram.instance.isEligible));
		GUILayout.Label(string.Format("Network incentiveCredits: {0}", LinkedVipProgram.instance.incentiveCredits));
		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
