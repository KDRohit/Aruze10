using UnityEngine;
using System.Collections.Generic;

class DevGUIMenuPowerups : DevGUIMenu
{
	public static string allTimeOverride = "";
	private string[] timeOverrides;
	private bool init = true;

	public override void drawGuts()
	{
		List<PowerupBase> activePowerups = PowerupsManager.activePowerups;
		if (init)
		{
			timeOverrides = new string[activePowerups.Count];
			for (int i = 0; i < activePowerups.Count; i++)
			{
				timeOverrides[i] = "";
			}

			init = false;
		}

		GUILayout.BeginVertical();
		for (int i = 0; i < activePowerups.Count; i++)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Override time remaining: ");
			GUILayout.Label(activePowerups[i].name);
			timeOverrides[i] = GUILayout.TextField(timeOverrides[i]).Trim();
			if (!string.IsNullOrEmpty(timeOverrides[i]))
			{
				int time = int.Parse(timeOverrides[i]);
				if (GUILayout.Button("Update time remaining"))
				{
					if (time > 0)
					{
						PowerupsManager.updatePowerupTimer(activePowerups[i], time);
					}
				}
			}

			GUILayout.EndHorizontal();
		}

		GUILayout.Label("Override All time remaining: ");
		allTimeOverride = GUILayout.TextField(allTimeOverride).Trim();
		
		if (!string.IsNullOrEmpty(allTimeOverride))
		{
			int time = int.Parse(allTimeOverride);

			if (time > 0)
			{
				if (GUILayout.Button("Update All time remaining"))
				{
					PowerupsManager.overridePowerupTimers(time);
				}
			}
		}
		GUILayout.EndVertical();

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Play FTUE"))
		{
			DoSomething.now("powerups_ftue");
		}

		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}