using UnityEngine;
using System.Collections.Generic;
using System.IO;

#pragma warning disable 0618 // UnityEngine.Caching.spaceOccupied' is obsolete but we can't use without some caching rework
/*
A dev panel.
*/

public class DevGUIMenuAssetDownloads : DevGUIMenu
{
	private static List<string> allBundles;
	private static string bundleName = "";
	private static string status = "Update to check for cached bundles";
	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Asset bundles: " + AssetBundleManager.BundleManifestName);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Clear Cache"))
		{
			Common.clearTemporaryCache(); // Cleaning the temporary cache.
		}

		long cachingUsed = Caching.spaceOccupied + getTemporaryCacheSize();
		string [] suffix = { "B", "KiB", "MiB", "GiB", "TiB" };
		string readableCachingUsed;

		if (cachingUsed == 0)
		{
			readableCachingUsed = cachingUsed.ToString() + suffix[0];
		}
		else
		{
			int place = System.Convert.ToInt32(System.Math.Floor(System.Math.Log(cachingUsed, 1024)));
			double num = System.Math.Round(cachingUsed / System.Math.Pow(1024, place), 1);
			readableCachingUsed = num.ToString() + suffix[place];
		}

		GUILayout.Label(string.Format("Cache used: {0}", readableCachingUsed));

		GUILayout.EndHorizontal();
		GUILayout.Space(10);
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("List Directory Contents"))
		{
			Debug.Log("#### Listing app storage contents. ####");
			System.Text.StringBuilder builder = new System.Text.StringBuilder();

			builder.Append("FILE LISTING - persistentDataPath:\n");
			DirectoryInfo dir = new DirectoryInfo(Application.persistentDataPath);
			FileInfo[] info = dir.GetFiles("*.*");
			foreach (FileInfo f in info) 
			{
				builder.Append(" " + f.FullName + "\n");
			}
			Debug.Log(builder.ToString());

			builder.Length = 0;
			
			if (!string.IsNullOrEmpty(FileCache.path))
			{
				builder.Append("FILE LISTING - temporaryCachePath:\n");
				dir = new DirectoryInfo(FileCache.path);
				info = dir.GetFiles("*.*");
				foreach (FileInfo f in info) 
				{
					builder.Append(" " + f.FullName + "\n");
				}
				Debug.Log(builder.ToString());
			}
			else
			{
				Debug.Log("FILE LISTING - temporaryCachePath: ERROR");
			}

			Debug.Log("#### Directory listing - all done. ####");
		}

		GUILayout.EndHorizontal();
		GUILayout.Space(10);
		GUILayout.BeginHorizontal();

		GUILayout.EndHorizontal();
		GUILayout.Space(10);
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Delete All Saved Data"))
		{
			Debug.LogError("DevGui -- Attempting to delete all PlayerPrefsCache.");
			PlayerPrefsCache.DeleteAll();
			PlayerPrefsCache.Save();

			// We must quit now - otherwise in-memory data will be written back to prefs when the app exits normally:
			Application.Quit();
		}
		GUILayout.EndHorizontal();
		
		GUILayout.Space(10);
		GUILayout.BeginHorizontal();

		if (allBundles == null)
		{
			allBundles = AssetBundleManager.getAllBundleNames();
		}
		
		if (GUILayout.Button("Download All Bundles From Beginning"))
		{
			downloadAllBundles(0);
		}
		
		if (GUILayout.Button("Download Bundles From First Missing Bundle"))
		{
			int missingBundleIndex = getFirstMissingBundleIndex();
			downloadAllBundles(missingBundleIndex);
		}
		
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		
		GUILayout.Label("Target Bundle");
		bundleName = GUILayout.TextField(bundleName).Trim();
		
		if (GUILayout.Button("Download Only Target Bundle"))
		{
			AssetBundleManager.downloadAndCacheBundle(bundleName, false, true, true);
		}
		
		if (GUILayout.Button("Download All Bundles Starting From Target Bundle: "))
		{
			int startingIndex = allBundles.IndexOf(bundleName);
			if (startingIndex >= 0)
			{
				downloadAllBundles(startingIndex);
			}
			else
			{
				Debug.Log("No missing bundles found");
			}
		}

		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Update Status"))
		{
			status = AssetBundleManager.Instance.getStatusReport();
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.TextArea(status);	
		GUILayout.EndHorizontal();


	}

	private void downloadAllBundles(int startingIndex)
	{
		for (int i = startingIndex; i < allBundles.Count; i++)
		{
			AssetBundleManager.downloadAndCacheBundle(allBundles[i], false, true, true); //Marking as lazy loaded so it can be unloaded & skippingMapping to save memory
		}
	}
	
	// Returns the size of everything in the our file cache directory.
	private long getTemporaryCacheSize()
	{
		long totalSize = 0;
		if (!string.IsNullOrEmpty(FileCache.path))
		{
			DirectoryInfo info = new DirectoryInfo(FileCache.path);
			FileInfo[] files = info.GetFiles();
			foreach (FileInfo file in files)
			{
				totalSize += file.Length;
			}
		}
		return totalSize;
	}

	private int getFirstMissingBundleIndex()
	{
		for (int i = 0; i < allBundles.Count; i++)
		{
			if (!AssetBundleManager.isBundleCached(allBundles[i]))
			{
				return i;
			}
		}

		return -1;
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
