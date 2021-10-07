using UnityEngine;

/*
A dev panel used to download new manifests from S3 at runtime to test bundles made outside of the current build.
Can download multiple manifests by separating names with a comma.
Will insert new bundles into out AssetBundleManifestWrapper replace any existing bundles with the version from the downloaded manifest
*/

public class DevGUIMenuDevManifests : DevGUIMenu
{
	private static string manifestsToLoad = "";
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Comma separated manifests list:");	
		manifestsToLoad = GUILayout.TextField(manifestsToLoad).Trim();
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Load Extra Manifest") && !string.IsNullOrWhiteSpace(manifestsToLoad))
		{
			AssetBundleManager.Instance.loadManifest(manifestsToLoad);
		}
		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
		manifestsToLoad = "";
	}
}
