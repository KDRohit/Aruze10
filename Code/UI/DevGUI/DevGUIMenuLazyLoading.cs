using UnityEngine;
using System;

class DevGUIMenuLazyLoading : DevGUIMenu
{
	private static string bundleNames = "";

	public static string missingBundlesAtStartup;

	private static string lastStatus = "Got to update it to see it!";
	
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Specify feature to load (See AssetBundleManager.lazyLoadBundles Values)");

		bundleNames = GUILayout.TextField(bundleNames).Trim();

		if (GUILayout.Button("Load Bundle"))
		{
			string[] bundles = bundleNames.Split(',');
			string bundleListString = "";
			for (int i = 0; i < bundles.Length; ++i)
			{
				bundleListString += string.Format("\"{0}\"", bundles[i]);
			}

			string data = "{ \"features\":[" + bundleListString + "]}";
			AssetBundleManager.loadMissingFeatures();
			HelpDialog.showDialog();
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("AssetBundleManager Status");
		if (GUILayout.Button("Update Status"))
		{
			lastStatus = AssetBundleManager.Instance.getStatusReport();
		}
		GUILayout.EndHorizontal();

		GUILayout.TextArea(lastStatus);	


	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		
	}
}