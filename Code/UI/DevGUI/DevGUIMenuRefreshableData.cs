using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuRefreshableData : DevGUIMenu
{
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("Watch to Earn Inventory: {0}", WatchToEarn.inventory));
		if (GUILayout.Button("Refresh"))
		{
			PlayerAction.refreshData(new List<string>(){"mobile_w2e_inventory", "mobile_w2e_inventory_ttl"});
		}
		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
