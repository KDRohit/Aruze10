using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;

/*
Identities dev panel.
*/
using Zynga.Core.Util;

public class DevGUIMenuFacebook : DevGUIMenu
{
	private static	string trackingLog = "";
	private static	string trackingDisplayLog = "";
	private int logBufferSize;

	public override void drawGuts()
	{

		drawOGFeedLogGuts();
	}

	private void drawOGFeedLogGuts()
	{
		GUILayout.Label("========== Facebook feed logging =============");		

		GUILayout.Label("zid : " + SlotsPlayer.instance.socialMember.zId);		

		drawEmailButtonGuts("Email Facebook Log", handleEmailClick);				

		renderLog();
	}

	public void handleEmailClick()
	{
		string subject = "FacebookFeed log";

		sendDebugEmail(subject, trackingLog);
	}	

	public static void facebookLog(string msg)
	{
		trackingLog += msg + "\n";
	}	

	private void renderLog()
	{
		if (logBufferSize != trackingLog.Length)
		{
			if (trackingLog.Length > 32000)   // unity gets unhappy if it has to render more than this
			{
				trackingDisplayLog =trackingLog.Substring(trackingLog.Length - 32000);
			}
			else
			{
				trackingDisplayLog = trackingLog;
			}

			logBufferSize = trackingLog.Length;
		}

		GUILayout.BeginHorizontal();
		GUI.enabled = false;		
		GUILayout.TextArea(trackingDisplayLog);
		GUI.enabled = true;		
		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}	
}