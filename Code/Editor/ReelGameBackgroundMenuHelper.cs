using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/**
 * Helper function for ReelGameBackground script, mostly intended to do verifications and help
 * identify issues which might exist in our games in relation to their ReelGameBackground scripts.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 8/16/2019
 */
public static class ReelGameBackgroundChecker
{
	[MenuItem("Zynga/ReelGameBackground/Report Games With Parent X Offsets")]
	public static void getParentXOffset()
	{
		List<GameObject> prefabsWithReelGameBackgroundScript = getAllGameObjectsContainingReelGameBackgroundScript();

		string outputStr = "ReelGameBackgroundMenuHelper.detectParentXOffset() - List of objects that have parent x-offset:\n";

		foreach (GameObject prefab in prefabsWithReelGameBackgroundScript)
		{
			ReelGameBackground reelGameBkg = prefab.GetComponentInChildren<ReelGameBackground>(true);
			float parentXOffset = reelGameBkg.getParentLocalXOffset();
			if (parentXOffset != 0.0f)
			{
				outputStr += prefab.name + " has parentXOffset = " + parentXOffset + "\n";
			}
		}
		
		Debug.Log(outputStr);
	}

	private static List<GameObject> getAllGameObjectsContainingReelGameBackgroundScript()
	{
		List<GameObject> prefabsWithReelGameBackgroundScript = new List<GameObject>();

		List<GameObject> allPrefabs = CommonEditor.gatherPrefabs("Assets/");
		foreach (GameObject prefab in allPrefabs)
		{
			ReelGameBackground reelGameBkg = prefab.GetComponentInChildren<ReelGameBackground>(true);
			if (reelGameBkg != null)
			{
				prefabsWithReelGameBackgroundScript.Add(prefab);
			}
		}

		return prefabsWithReelGameBackgroundScript;
	}
}
