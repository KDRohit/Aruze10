using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuExperiments : DevGUIMenu
{
	private bool showActiveExperiments = true;
	private bool showInactiveExperiments = false;
	private string searchFilter = "";
	
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		showActiveExperiments = GUILayout.Toggle(showActiveExperiments, "Active Exp.", new GUIStyle(GUI.skin.button), GUILayout.Width(isHiRes ? 250 : 150));
		showInactiveExperiments = GUILayout.Toggle(showInactiveExperiments, "Inactive Exp.", new GUIStyle(GUI.skin.button), GUILayout.Width(isHiRes ? 250 : 150));
		
		GUILayout.Space(isHiRes ? 1000 : 500);
		Color oldColor = GUI.color;
		
		GUI.color = Color.red;
		
		if (GUILayout.Button("Reload Game", GUILayout.Width(isHiRes ? 250 : 150)))
		{
			Glb.resetGame("Dev Panel Reload Game");
		}
		
		GUI.color = oldColor;
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Filter Experiments: ");
		searchFilter = GUILayout.TextField(searchFilter, GUILayout.Width(isHiRes ? 500 : 300));
		if (GUILayout.Button("X", GUILayout.Width(isHiRes ? 100 : 50)))
		{
			searchFilter = "";
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("ZID: " + SlotsPlayer.instance.socialMember.zId);
		GUILayout.Label("SNID: " + SlotsPlayer.instance.socialMember.id);
		GUILayout.EndHorizontal();

		ExperimentWrapper.displayVariants(showActiveExperiments, showInactiveExperiments, isHiRes, searchFilter);
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
