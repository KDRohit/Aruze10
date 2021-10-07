using UnityEngine;
using System.Collections.Generic;

class DevGUIMenuRobustChallenges : DevGUIMenu
{
	private string timeOverride = "";

	public override void drawGuts()
	{
		if (CampaignDirector.robust != null && CampaignDirector.robust.isActive)
		{
			GUILayout.BeginVertical();
			GUILayout.Label("Override time remaining: ");
			timeOverride = GUILayout.TextField(timeOverride).Trim();

			if (!string.IsNullOrEmpty(timeOverride))
			{
				int time = int.Parse(timeOverride);
				if (GUILayout.Button("Update time remaining"))
				{
					if (time > 0)
					{
						CampaignDirector.robust.timerRange = GameTimerRange.createWithTimeRemaining(time);
					}
				}
			}

			GUILayout.EndHorizontal();
		}
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}