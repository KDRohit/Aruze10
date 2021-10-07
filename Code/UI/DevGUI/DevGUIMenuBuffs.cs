using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Identities dev panel.
*/
using Zynga.Core.Util;

public class DevGUIMenuBuffs : DevGUIMenu
{
	private static	string trackingLog = "";
	private static	string trackingDisplayLog = "";
	private int logBufferSize;

	private static string[] options = new string[] {"xp_multiplier", "levelup_bonus_multiplier", "daily_bonus_reduced_timer"};
	private static string[] optionsKeyNames = new string[] {"doublexp_for_8_hours", "even_levelupbonus_for_24_hours", "dailybonus_every_90_min_for_12_hours"};
	public static int optionIndex;

	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Trigger Buff"))
		{
			triggerBuffApplied();
		}
		if (GUILayout.Button("Clear Buff Log"))
		{
			handleClearLogClick();
		}
		optionIndex = GUILayout.SelectionGrid(optionIndex, options, 10);								
		GUILayout.EndHorizontal();
		
		drawLog();
	}

	private void drawLog()
	{
		GUILayout.Label("========== Buffs feed logging =============");

		drawEmailButtonGuts("Email Log", handleEmailClick);				

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
		GUILayout.TextArea(trackingDisplayLog);
		GUILayout.EndHorizontal();
	}

	public void handleEmailClick()
	{
		string subject = "Buffs log";

		sendDebugEmail(subject, trackingLog);
	}

	public void handleClearLogClick()
	{
		trackingLog = "";
	}

	public static void triggerBuffApplied()
	{
		string buffKey = optionsKeyNames[optionIndex];
		string buffType = options[optionIndex];
		int nowInSecs = GameTimer.currentTime;
		int endTimestamp = nowInSecs + 60;
		int value = 2;
		string appliesTo = "any";
		if (options[optionIndex] == "levelup_bonus_multiplier")
		{
			appliesTo = "even";
		}
		triggerBuffApplied(buffKey, buffType, appliesTo, value, endTimestamp);
	}

	public static void triggerBuffApplied(string buffKey, string buffType, string appliesTo, int value, int endTimestamp)
	{
		string jsonString = "{ \"buff\": {" +
			"\"base_type\": " + "\"" + buffType + "\"" + "," +
			"\"end_ts\": " + endTimestamp + "," +
			"\"value\": " + value + "," +
			"\"applies_to\": \"" + appliesTo + "\"" + "," +
			"\"key_name\":" + "\"" + buffKey + "\"" +
		"} }";
		Buff.log("Triggering buff_applied:{0}", jsonString);
		Buff.onPlayerBuffApplied(new JSON(jsonString));
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		
	}

}
