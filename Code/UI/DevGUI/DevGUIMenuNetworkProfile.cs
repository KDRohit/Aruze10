using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;

/*
Game Network dev panel.
*/

public class DevGUIMenuNetworkProfile : DevGUIMenu
{
	
	public override void drawGuts()
	{	
		GUILayout.BeginVertical();
		#if UNITY_EDITOR
		GUILayout.Label(string.Format("get_profile calls: {0}", NetworkProfileAction.getProfileRequests));
		GUILayout.Label(string.Format("multi_get_profile calls: {0}", NetworkProfileAction.multiGetProfileRequests));
		#endif
		GUILayout.Label("Network Profile: ");
		GUILayout.Label(string.Format("Name: {0}", SlotsPlayer.instance.socialMember.networkProfile.name));
		GUILayout.Label(string.Format("Status: {0}", SlotsPlayer.instance.socialMember.networkProfile.status));
		GUILayout.Label(string.Format("Location: {0}", SlotsPlayer.instance.socialMember.networkProfile.location));
		GUILayout.Label(string.Format("PhotoURL: {0}", SlotsPlayer.instance.socialMember.photoSource.getUrl(PhotoSource.Source.PROFILE)));
		GUILayout.Label(string.Format("Gender: {0}", SlotsPlayer.instance.socialMember.networkProfile.gender));
		GUILayout.Label(string.Format("Join Time: {0}", SlotsPlayer.instance.socialMember.networkProfile.joinTime.ToString()));
		GUILayout.Label(string.Format("VIP Level: {0}", SlotsPlayer.instance.socialMember.networkProfile.vipLevel.ToString()));
	
		if (NetworkAchievements.isEnabled)
		{
			GUILayout.Label(string.Format("Achievement Score: {0}", SlotsPlayer.instance.socialMember.networkProfile.achievementScore));
			if (SlotsPlayer.instance.socialMember.networkProfile.achievementLevel != null)
			{
				GUILayout.Label(string.Format("Achievement Rank: {0}", SlotsPlayer.instance.socialMember.networkProfile.achievementLevel.name));
			}
		}
		else
		{
			GUILayout.Label("Achievements DISABLED");
		}

		if (SlotsPlayer.instance.socialMember.networkProfile.gameStats != null)
		{
			if (SlotsPlayer.instance.socialMember.networkProfile.gameStats.ContainsKey("hir"))
			{
				GUILayout.Label(string.Format("HIR Stats: ", SlotsPlayer.instance.socialMember.networkProfile.gameStats["hir"].ToString()));
			}
			if (SlotsPlayer.instance.socialMember.networkProfile.gameStats.ContainsKey("wonka"))
			{
				GUILayout.Label(string.Format("WONKA Stats: ", SlotsPlayer.instance.socialMember.networkProfile.gameStats["wonka"].ToString()));
			}
			if (SlotsPlayer.instance.socialMember.networkProfile.gameStats.ContainsKey("woz"))
			{
				GUILayout.Label(string.Format("WOZ Stats: ", SlotsPlayer.instance.socialMember.networkProfile.gameStats["woz"].ToString()));
			}
			if (SlotsPlayer.instance.socialMember.networkProfile.gameStats.ContainsKey("black_diamond"))
			{
				GUILayout.Label(string.Format("BLACK_DIAMOND Stats: ", SlotsPlayer.instance.socialMember.networkProfile.gameStats["black_diamond"].ToString()));
			}
		}
		if (GUILayout.Button("Open Player Profile"))
		{
			NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember);
			DevGUI.isActive = false;
		}
		
		GUILayout.EndVertical();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
