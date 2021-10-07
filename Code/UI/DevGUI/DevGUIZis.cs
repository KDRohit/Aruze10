using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CustomLog;

/*
A dev panel.
*/

public class DevGUIMenuZis : DevGUIMenu
{

	public override void drawGuts()
	{

		drawWatchToEarnGuts();
	}

	private void drawWatchToEarnGuts()
	{
		GUILayout.Label("========== Zis =============");
		string anonymousZid = SlotsPlayer.getPreferences().GetString(Prefs.ANONYMOUS_ZID, "");
		GUILayout.Label("Anon zid : " + anonymousZid);
		GUILayout.Label("Player ID : " + ZisData.playerId);
		GUILayout.Label("Zis Token : " + ZisData.zisToken);
		GUILayout.Label("Zis Token issued at: " + ZisData.zisIssuedAt);
		GUILayout.Label("Zis Token expires at: " + ZisData.zisExpiresAt);
		GUILayout.Label("Zis install credentials: " + ZisData.installCredentials?.ToString() ?? "N/A");
		GUILayout.Label("Zis SSO token: " + ZisData.ssoToken);
		GUILayout.Label("Zis SSO token issued at: " + ZisData.ssoIssuedAt);

	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
