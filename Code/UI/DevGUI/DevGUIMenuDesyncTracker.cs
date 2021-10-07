using UnityEngine;
using System.Collections;

public class DevGUIMenuDesyncTracker : DevGUIMenu
{
	public override void drawGuts()
	{
		GUILayout.BeginVertical();

		if (GUILayout.Button("Force Desync Log - Underpay"))
		{
			DesyncTracker.trackDesyncViaStatsManager(1000);
		}

		if (GUILayout.Button("Force Desync Log - Overpay"))
		{
			DesyncTracker.trackDesyncViaStatsManager(-1000);
		}

		GUILayout.Label("Current coin flows:");
		GUILayout.EndVertical();

		for (int i = 0; i < DesyncTracker.recentCoinFlows.Length; ++i)
		{
			PlayerResource.DesyncCoinFlow flow = DesyncTracker.recentCoinFlows[i];

			if (flow != null)
			{
				GUILayout.BeginHorizontal();

				GUILayout.Label("Credit source : " + flow.source);
				GUILayout.Label("Value : " + flow.amount);

				GUILayout.EndHorizontal();
			}
		}
	}
}