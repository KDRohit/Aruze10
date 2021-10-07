using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuRateMe : DevGUIMenu
{
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Rate Me Dialog"))
		{
			RateMe.checkAndPrompt(RateMe.RateMeTrigger.MISC);
		}
		if (GUILayout.Button("Force Rate Me Dialog"))
		{
			RateMe.checkAndPrompt(RateMe.RateMeTrigger.MISC, true);
		}
		GUILayout.EndHorizontal();

		//Rate Me Views
		GUILayout.BeginHorizontal();
		RateMe.versionViewCount = intInputField("Rate Me! Views", RateMe.versionViewCount.ToString(), 1, 0, int.MaxValue);
		GUILayout.EndHorizontal();

		//Last Prompted Date
		GUILayout.BeginHorizontal();
		RateMe.lastPromptDateTime = dateInputField("Last Prompt", RateMe.lastPromptDateTime, 1);
		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
