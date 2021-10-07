using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FeatureOrchestrator;
/*
A dev panel.
*/

public class DevGUIMenuFeatures : DevGUIMenu
{
	public override void drawGuts()
	{
		GUILayout.Label("Maybe put active features here?");

		GUILayout.BeginHorizontal();

		if (MainLobbyV3.instance != null && MainLobbyV3.hirV3 != null)
		{
			if (MainLobbyV3.hirV3.isEliteTranstionLoaded())
			{
				if (GUILayout.Button("Test Elite Lobby"))
				{
					EliteManager.debugToggleActive(true);
					EliteManager.forceLobbyTransition(true);
					MainLobbyV3.hirV3.playEliteTransition();
			
				}

				if (GUILayout.Button("Test Main Lobby"))
				{
					EliteManager.debugToggleActive(false);
					EliteManager.forceLobbyTransition(false);
					MainLobbyV3.hirV3.playLobbyTransition();
				}
			}
			else if (MainLobbyV3.hirV3.isEliteTranstionLoading())
			{
				GUILayout.Label("Loading...");
			}
			else if (GUILayout.Button("Load Elite Transition"))
			{
				MainLobbyV3.hirV3.initElite(true);
			}	
		}
		
		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
