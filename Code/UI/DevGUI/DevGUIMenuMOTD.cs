using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuMOTD : DevGUIMenu
{
	private static string motdKey = "";

	private static bool showPassedOverThisSession = false;
	private static bool showSeenThisSession = false;
	private static bool showSortedThisSession = false;
	private static bool showToShowQueueSession = false;
	private static bool showNoShow = false;


	public override void drawGuts()
	{
		drawInternals();
	}

	public static void drawInternals()
	{
		GUILayout.BeginHorizontal();
		
		GUILayout.Label("Specific MOTD Key:");		
		motdKey = GUILayout.TextField(motdKey).Trim();

		if (motdKey != "")
		{
			if (GUILayout.Button("Show " + motdKey))
			{
				MOTDFramework.showMOTD(motdKey);
				DevGUI.isActive = false;
			}
		}
		
		if (GUILayout.Button("Clear MOTDs"))
		{
			MOTDDialog.clearAllSeenDialogs();
			MOTDFramework.clearAllSeenDialogs();
			PlayerPrefsCache.SetString(Prefs.SEEN_DYNAMIC_MOTD_TEMPLATES, "");
			PlayerAction.getNewMotdList();
		}
		
		GUILayout.EndHorizontal();
		////////////////////////////////////////////////////////////////////////////////////////


		GUILayout.BeginHorizontal();

		GUILayout.Label("Recently seen new game MOTD's: " + PlayerPrefsCache.GetString(CustomPlayerData.RECENTLY_VIEWED_NEW_GAME_MOTD_MOBILE, ""));		
		
		if (GUILayout.Button("Clear"))
		{
			PlayerPrefsCache.SetString(CustomPlayerData.RECENTLY_VIEWED_NEW_GAME_MOTD_MOBILE, "");
			PlayerPrefsCache.Save();
			DevGUI.isActive = false;
			Glb.resetGame("Cleared recently seen new game MOTD's on dev panel.");
		}

		GUILayout.EndHorizontal();

		////////////////////////////////////////////////////////////////////////////////////////
		// Show reasons for the filtered MOTD not showing.
		GUILayout.BeginHorizontal();
		{
			GUILayout.Label("Use the MOTD key filter above to see the reason why the MOTD isn't being shown.");
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		{
			GUILayout.Label("If no reasons are shown, you need to implement the MOTDDialogData.noShowReason override.");
		}
		GUILayout.EndHorizontal();
		
		if (motdKey != "")
		{
		    MOTDDialogData data = MOTDDialogData.find(motdKey);
			if (data == null)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Could not find MOTDDialogData key " + motdKey + " on client.");
				}
				GUILayout.EndHorizontal();
			}
			else
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label(motdKey, GUILayout.Width(200f));
					if (MOTDFramework.sortingOrder.ContainsKey(motdKey))
					{
						GUILayout.Label("Sort: " + MOTDFramework.sortingOrder[motdKey].ToString(), GUILayout.Width(50f));
					}
					else
					{
						GUILayout.Label("No Sort?", GUILayout.Width(50f));
					}
		
					GUILayout.TextArea(data.noShowReason);
				}
				GUILayout.EndHorizontal();
			}
		}

#if !ZYNGA_PRODUCTION
		GUILayout.Label(MOTDFramework.getMOTDStatusReport());		

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("sortedMOTDQueue List"))
		{
			showSortedThisSession = !showSortedThisSession;
		}
		GUILayout.EndHorizontal();
		if (showSortedThisSession && MOTDFramework.sortedMOTDQueue != null)
		{
			GUILayout.Label("sortedMOTDQueue:");
			if (MOTDFramework.sortedMOTDQueue.Count == 0)
			{
				GUILayout.Label("No MOTDs sorted this session");
			}
			else
			{
				foreach (string motd in MOTDFramework.sortedMOTDQueue)
				{
					GUILayout.Label(motd);
				}
			}
		}

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("motdToShowQueue List"))
		{
			showToShowQueueSession = !showToShowQueueSession;
		}
		GUILayout.EndHorizontal();
		if (showToShowQueueSession && MOTDFramework.motdToShowQueue != null)
		{
			GUILayout.Label("toShowQueue:");
			if (MOTDFramework.motdToShowQueue.Count == 0)
			{
				GUILayout.Label("No MOTDs in motdToShowQueue");
			}
			else
			{
				foreach (string motd in MOTDFramework.motdToShowQueue)
				{
					GUILayout.Label(motd);
				}
			}
		}

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("noShowList MOTD's that claim they can show but don't when asked to"))
		{
			showNoShow = !showNoShow;
		}
		GUILayout.EndHorizontal();
		if (showNoShow && MOTDFramework.noShowList != null)
		{
			GUILayout.Label("noShowList:");
			if (MOTDFramework.noShowList.Count == 0)
			{
				GUILayout.Label("No MOTDs in noShowList");
			}
			else
			{
				foreach (string motd in MOTDFramework.noShowList)
				{
					GUILayout.Label(motd);
				}
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////
		// Show MOTD that showed this session.
		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Seen this session List"))
		{
			showSeenThisSession = !showSeenThisSession;
		}

		GUILayout.EndHorizontal();

		if (showSeenThisSession && MOTDFramework.seenThisSession != null)
		{
			GUILayout.Label("Seen this session:");
			if (MOTDFramework.seenThisSession.Count == 0)
			{
				GUILayout.Label("No MOTDs shown this session");
			}
			else
			{
				foreach (string motd in MOTDFramework.seenThisSession)
				{
					GUILayout.Label(motd);
				}
			}
		}

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Passed Over Reasons List"))
		{
			showPassedOverThisSession = !showPassedOverThisSession;
		}
		GUILayout.EndHorizontal();
		
		if (showPassedOverThisSession && MOTDFramework.passedOverThisSession != null)
		{
			GUILayout.Label("Passed over for reasons:");
			if (MOTDFramework.passedOverThisSession.Count == 0)
			{
				GUILayout.Label("No MOTDs passed over this session");
			}
			else
			{
				foreach (KeyValuePair<string, string> pair in MOTDFramework.passedOverThisSession)
				{
					Color oldColor = GUI.color;
					GUILayout.Label(pair.Key);
					GUI.color = Color.red;
					GUILayout.Label(pair.Value);
					GUI.color = oldColor;
				}
			}
		}
		GUILayout.EndVertical();
#endif		
	}
	
	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
