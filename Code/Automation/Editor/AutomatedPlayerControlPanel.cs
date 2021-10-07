using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


public class AutomatedPlayerControlPanel : EditorWindow 
{
#if ZYNGA_TRAMP

	private static Vector2 branchScroll;

	// We don't want to run a process every frame to get the branch name, so store it and only refresh when needed.
	private static string branchName;

	[MenuItem ("TRAMP/Control Panel")]
	static void Init()
	{
		// Get existing open window or if none, make a new one:
		AutomatedPlayerControlPanel window = (AutomatedPlayerControlPanel)EditorWindow.GetWindow(typeof(AutomatedPlayerControlPanel));
		window.Show();
	}

	void OnGUI()
	{
		DrawControlPanel(AutomatedPlayer.instance);
	}

	public static void DrawControlPanel(AutomatedPlayer ap)
	{
		// Try to say what is currently testing
		string status = "UNKNOWN";
		if (ap != null && ap.companion != null && ap.companion.activeGame != null)
		{
			if (UnityEditor.EditorApplication.isPaused)
			{
				status = string.Format("{0} [PAUSED]", ap.companion.activeGame.commonGame.gameKey);	
			}
			else
			{
				status = ap.companion.activeGame.commonGame.gameKey;
			}
		}
		else
		{
			if (AutomatedPlayer.isAutomating)
			{
				status = "STALLED";
			}
			else
			{
				status = "OFF";
			}
		}

		if (string.IsNullOrEmpty(branchName))
		{
			branchName = AutomatedPlayerProcesses.getBranchName();
		}

		// Draw the status and branch text to show some helpful info.
		GUILayout.Label(string.Format("Status: {0}", status));
		if (SlotsPlayer.instance != null &&
			SlotsPlayer.instance.socialMember != null)
		{
			GUILayout.Label(string.Format("ZID: {0}", SlotsPlayer.instance.socialMember.zId));
		}
	
		if (ap != null && ap.companion != null)
		{
			GUILayout.Label(string.Format("Branch:\n{0}", branchName));
 		}

		// ** Draw Action Buttons **
		if (AutomatedPlayer.isAutomating)
		{
			if (GUILayout.Button("Turn Automation OFF"))
			{
				TRAMPLogFiles.logToOther("TRAMP> OPERATOR clicked \"\"Turn Automation OFF\""); 

				AutomatedPlayer.turnAutomationOff(ap);

				Debug.LogFormat("<color={0}>TRAMP> Automation set to {1}</color>", 
					AutomatedPlayer.TRAMP_DEBUG_COLOR, AutomatedPlayer.isAutomating);
			}
		}
		else
		{
			if (GUILayout.Button("Turn Automation ON"))
			{
				TRAMPLogFiles.logToOther("TRAMP> OPERATOR clicked \"Turn Automation ON\""); 

				if (AutomatedPlayerCompanion.instance != null)
				{
					AutomatedPlayerCompanion.instance.loadCurrentTest();
				}
				
				AutomatedPlayer.turnAutomationOn(ap);

				// AutomatedPlayer.isAutomating doesn't become true for a little longer.
				Debug.LogFormat("<color={0}>TRAMP> Automation set to {1}</color>", 
					AutomatedPlayer.TRAMP_DEBUG_COLOR, true);
			}
		}

		if (ap != null)
		{
			// Allowing pauses can cause TRAMP to stop in the event of Debug.Break().
			if (ap.allowPause)
			{
				if (GUILayout.Button("Block Pause"))
				{
					TRAMPLogFiles.logToOther("TRAMP> OPERATOR clicked \"Block Pause\""); 

					ap.allowPause = false;

					Debug.LogFormat("<color={0}>TRAMP> allowPause set to {1}</color>", 
						AutomatedPlayer.TRAMP_DEBUG_COLOR, ap.allowPause);
				}
			}
			else
			{
				if (GUILayout.Button("Allow Pause"))
				{
					TRAMPLogFiles.logToOther("TRAMP> OPERATOR clicked \"Allow Pause\""); 

					ap.allowPause = true;

					Debug.LogFormat("<color={0}>TRAMP> allowPause set to {1}</color>", 
						AutomatedPlayer.TRAMP_DEBUG_COLOR, ap.allowPause);
				}
			}

			if (GUILayout.Button("Force Game Test Done"))
			{
				TRAMPLogFiles.logToOther("TRAMP> OPERATOR clicked \"Force Game Test Done\""); 

				if (ap.companion.activeGame != null)
				{
					ap.companion.activeGame.spinFinished(ap.getGameMode());
					ap.restartAutomation(true);

					Debug.LogFormat("<color={0}>TRAMP> gameTestDone called.</color>", 
						AutomatedPlayer.TRAMP_DEBUG_COLOR);
				}
				else
				{
					TRAMPLogFiles.logToOther("TRAMP> no active game to force done."); 
				}
			}
		}

		if (GUILayout.Button("Flush All Logs"))
		{
			TRAMPLogFiles.logToOther("TRAMP> OPERATOR clicked \"Flush All Logs\""); 

			AutomatedPlayer.flushAllLogs();

			Debug.LogFormat("<color={0}>TRAMP> Glb.resetGame() called.</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR);
		}

		if (GUILayout.Button("END TEST (Saves and archives results)"))
		{
			AutomatedPlayer.endTest();
		}

		if (GUILayout.Button("ABORT (Delete all files and exit)"))
		{
			TRAMPLogFiles.logToOther("TRAMP> OPERATOR clicked \"ABORT (Delete all files and exit)\""); 

			AutomatedPlayer.abort();

			Debug.LogFormat("<color={0}>TRAMP> Abort called.</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR);
		}

		if (GUILayout.Button("EXIT (Does *not* delete files)"))
		{
			TRAMPLogFiles.logToOther("TRAMP> OPERATOR clicked \"EXIT (Does *not* delete files)\""); 

			AutomatedPlayer.exit();

			Debug.LogFormat("<color={0}>TRAMP> Exit called.</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR);
		}

		if (GUILayout.Button("RESET MOTDs"))
		{
			TRAMPLogFiles.logToOther("TRAMP> OPERATOR clicked \"RESET MOTDs\""); 

			// See DevGUIMenuTools.drawGuts()
			PlayerPrefsCache.SetInt(Prefs.SHOWN_CHARMS_TOOLTIP, 0);
			CustomPlayerData.setValue(CustomPlayerData.CHARM_WITH_BUY_VIEWED_MOTD_VERSION, 0);
			PlayerPrefsCache.SetInt(Prefs.LAST_SEEN_MOTD_QUEST_COUNTER, 0);
			PlayerPrefsCache.SetInt(Prefs.HAS_WOZ_SLOTS_INSTALLED_BEFORE_CHECK, -1);

			// See DevGUIMenuMOTD.drawGuts()
			MOTDDialog.clearAllSeenDialogs();
			MOTDFramework.clearAllSeenDialogs();
			CarouselData.activateAll();

			PlayerAction.getNewMotdList();

			Debug.LogFormat("<color={0}>TRAMP> Reset MOTDs called.</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR);
		}

		if (GUILayout.Button("Reset Game"))
		{
			TRAMPLogFiles.logToOther("TRAMP> OPERATOR clicked \"Reset Game\""); 

			Glb.resetGame("TRAMP> OPERATOR clicked \"Reset Game\"");

			Debug.LogFormat("<color={0}>TRAMP> Glb.resetGame() called.</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR);
		}

	}
#else
	void OnGUI()
	{
		GUILayout.Label("TRAMP disabled.");
		if (GUILayout.Button("Close"))
		{
			this.Close();
		}
	}
#endif
}