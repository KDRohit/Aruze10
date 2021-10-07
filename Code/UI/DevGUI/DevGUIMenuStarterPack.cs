using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuStarterPack : DevGUIMenu
{
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();

		if (SlotsPlayer.instance.isPayerMobile || SlotsPlayer.instance.isPayerWeb)
		{
			if (SlotsPlayer.instance.isPayerMobile)
			{
				GUILayout.Label("Is Payer on Mobile.");
			}
			if (SlotsPlayer.instance.isPayerWeb)
			{
				GUILayout.Label("Is Payer on Web.");
			}
		}

		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Starter Pack Dialog"))
		{
			StarterDialog.showDialog();
			DevGUI.isActive = false;
		}

		if (Glb.STARTERPACK_SEC_AFTER_INSTALL == -1)
		{
			GUILayout.Label("Starter pack timer is invalid, starter packs are disabled.");
		}
		else if (SlotsPlayer.instance.isPayerMobile || SlotsPlayer.instance.isPayerWeb)
		{
			GUILayout.Label("User is a payer, starter packs are disabled.");
		}
		else
		{
			// If we're not enabled yet, show the countdown:
			int elapsedTimeSeconds = GameTimer.sessionStartTime - SlotsPlayer.instance.firstPlayTime;
			int starterCountDown = elapsedTimeSeconds - Glb.STARTERPACK_SEC_AFTER_INSTALL;
			if (starterCountDown <= 0)
			{
				GUILayout.Label("Countdown until enabled: " + starterCountDown.ToString());
			}
			else
			{
				// Starter pack should be enabled at this point. See if we have a repeat time:
				int starterTime = PlayerPrefsCache.GetInt(Prefs.STARTERPACK_SLIDING_TIME, 0);
				if (starterTime != 0)
				{
					int deltaTime = 0;
					deltaTime = GameTimer.currentTime - starterTime;
					if (GUILayout.Button("Clear Time:" + starterTime.ToString() + " Nxt:" + deltaTime.ToString()))
					{
						PlayerPrefsCache.DeleteKey(Prefs.STARTERPACK_SLIDING_TIME);
						PlayerPrefsCache.Save();
					}
				}
				else
				{
					GUILayout.Label("Should be displayed on next game start.");
				}
			}
		}

		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
