using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;

/*
Game Network dev panel.
*/

public class DevGUIMenuChallenges : DevGUIMenu
{
	private string shownKey = "";
	
	public override void drawGuts()
	{
		GUILayout.BeginVertical();
		if (CampaignDirector.campaigns != null)
		{
			foreach (KeyValuePair<string, ChallengeCampaign> pair in CampaignDirector.campaigns)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(pair.Key);
				if (pair.Key == shownKey)
				{
					if (GUILayout.Button("Hide"))
					{
						shownKey = "";
					}
					GUILayout.EndHorizontal();
					GUILayout.BeginVertical();
					if (CampaignDirector.campaigns.ContainsKey(shownKey))
					{
						CampaignDirector.campaigns[shownKey].drawInDevGUI();
					}
					GUILayout.EndVertical();
				}
				else
				{
					if (GUILayout.Button("Show"))
					{
						shownKey = pair.Key;
					}
					GUILayout.EndHorizontal();
				}
			}
		}
		else
		{
			GUILayout.Label("Campaigns was null.");
		}

		GUILayout.EndVertical();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
