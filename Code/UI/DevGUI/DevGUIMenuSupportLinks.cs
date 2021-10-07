using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CustomLog;

/*
A dev panel.
*/

public class DevGUIMenuSupportLinks : DevGUIMenu
{
	private string testUrl = "https://zyngasupport.helpshift.com/a/hit-it-rich/";
	private string testOutputUrl = "";

	public override void drawGuts()
	{
		List<System.Tuple<string, string>> supportUrls = new List<System.Tuple<string, string>>{
			System.Tuple.Create("Standard support link: Glb.HELP_LINK_SUPPORT", Glb.HELP_LINK_SUPPORT),
			System.Tuple.Create("Billing support link: BILLING_HELP_URL", Data.liveData.getString("BILLING_HELP_URL", "")),
			System.Tuple.Create("Linked VIP support link: LINKED_VIP_HELP_URL", Data.liveData.getString("LINKED_VIP_HELP_URL", ""))
		};

		foreach (System.Tuple<string,string> urlPair in supportUrls)
		{
			string label = urlPair.Item1;
			string url = urlPair.Item2;
			GUILayout.Label(label);
			GUILayout.Label(url);
			GUILayout.Label("Rewritten, added metadata and encrypted link:");
			GUILayout.Label(Common.RewriteHelpshiftSupportUrl(url));
			if (GUILayout.Button("Go", GUILayout.Width(100)))
			{
				Common.openSupportUrl(url);
			}
			GUILayout.Label("");
		}

		GUILayout.Label("Test link");
		testUrl = GUILayout.TextField(testUrl);
		if (GUILayout.Button("Rewrite", GUILayout.Width(100)))
		{
			testOutputUrl = Common.RewriteHelpshiftSupportUrl(testUrl);
		}
		GUILayout.Label("Rewritten:");
		GUILayout.Label(testOutputUrl);
		if (GUILayout.Button("Go", GUILayout.Width(100)))
		{
			Common.openSupportUrl(testUrl);
		}
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
