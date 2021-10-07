using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuXpromo : DevGUIMenu
{
	private const string IMAGE_PATH_FORMAT = "xpromo/{0}/DialogBG.png";	// This should be an asset bundle in Unity, rather than on P4.

	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		for (int i = 0; i < MobileXpromo.liveXpromos.Count; i++)
		{
			string xpromoKey = MobileXpromo.liveXpromos[i];
			if (GUILayout.Button(xpromoKey))
			{
				MobileXPromoDialog.showDialog(xpromoKey, string.Format(IMAGE_PATH_FORMAT, xpromoKey), MobileXpromo.SurfacingPoint.NONE, null);
			}
		}
		GUILayout.EndHorizontal();
		
		GUILayout.BeginVertical();
		
		GUILayout.BeginHorizontal();
		int rtlCount = CustomPlayerData.getInt(CustomPlayerData.AUTO_POP_XPROMO_RTL_COUNT, 0);
		GUILayout.Label(string.Format("RTL Count: {0}", rtlCount));
		if (GUILayout.Button("Reset RTL Count"))
		{
			CustomPlayerData.setValue(CustomPlayerData.AUTO_POP_XPROMO_RTL_COUNT, 0);
		}
		GUILayout.EndHorizontal();

		GUILayout.EndVertical();

		GUILayout.BeginVertical();
		
		GUILayout.BeginHorizontal();
		int oocCount = CustomPlayerData.getInt(CustomPlayerData.AUTO_POP_XPROMO_OOC_COUNT, 0);
		GUILayout.Label(string.Format("OOC Count: {0}", oocCount));
		if (GUILayout.Button("Reset OOC Count"))
		{
			CustomPlayerData.setValue(CustomPlayerData.AUTO_POP_XPROMO_OOC_COUNT, 0);
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();

		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();
		int lastSeen = CustomPlayerData.getInt(CustomPlayerData.AUTO_POP_XPROMO_LAST_SHOW_TIME, 0);
		GUILayout.Label(string.Format("Last seen: {0}", lastSeen));
		if (GUILayout.Button("Reset Last seen"))
		{
			CustomPlayerData.setValue(CustomPlayerData.AUTO_POP_XPROMO_LAST_SHOW_TIME, 0);
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();

		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();
		int totalArtChanges = CustomPlayerData.getInt(CustomPlayerData.XPROMO_ART_CHANGE_COUNT, 0);
		GUILayout.Label(string.Format("Total art changes: {0}", totalArtChanges));
		if (GUILayout.Button("Reset art change count"))
		{
			CustomPlayerData.setValue(CustomPlayerData.XPROMO_ART_CHANGE_COUNT, 0);
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();

		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();
		int totalViews = CustomPlayerData.getInt(CustomPlayerData.XPROMO_ART_VIEW_COUNT, 0);
		GUILayout.Label(string.Format("Total views: {0}", totalViews));
		if (GUILayout.Button("Reset Last seen"))
		{
			CustomPlayerData.setValue(CustomPlayerData.XPROMO_ART_VIEW_COUNT, 0);
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();

		if (GUILayout.Button("Trigger auto-pop OOC"))
		{
			if (ExperimentWrapper.MobileToMobileXPromo.experimentData != null)
			{
				MobileXpromo.showXpromo(MobileXpromo.SurfacingPoint.OOC);
			}
			else
			{
				Debug.LogError("xpromo auto-pop OOC: invalid data");
			}
			
		}
		if (GUILayout.Button("Trigger auto-pop RTL"))
		{
			if (ExperimentWrapper.MobileToMobileXPromo.experimentData != null)
			{
				MobileXpromo.showXpromo(MobileXpromo.SurfacingPoint.RTL);
			}
			else
			{
				Debug.LogError("xpromo auto-pop RTL: Invalid data");
			}
			
		}
		GUILayout.BeginHorizontal();		
		GUILayout.EndHorizontal();		

	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
