using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/*
This tools shows the entire asset tree hierarchy (dependencies) for selected bundle(s).
This provides a simple indented file of asset paths one can look at & search to which
assets include which other assets.
*/

public class AssetTreeReporter
{
	[MenuItem ("Zynga/Asset Reports/Generate Asset Tree Report for selected bundles")]
	public static void generateAssetTreeReportForSelectedBundles()
	{
		var bundleNames = AssetBundleTagger.findAllBundleTagsInSelection();
		Debug.Log("Creating reports for  " + bundleNames.Length + " bundles: " + string.Join(", ", bundleNames));

		string allReports = "";
		foreach (var bundleName in bundleNames)
		{
			string report = generateAssetTreeReportForBundle(bundleName);
			allReports += report;
			allReports += "\n==================================================================\n";
		}

		// Put in copy-paste buffer so user can just 'paste' into a text editor
		EditorGUIUtility.systemCopyBuffer = allReports;
		Debug.Log("Report copied to system clipboard; you can <paste> it to an editor...");
	}

	private static string generateAssetTreeReportForBundle(string bundleName)
	{
		string[] topLevelAssets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName).OrderBy(path => path).ToArray();
		string report = "Bundle: '" + bundleName + "'  contains " + topLevelAssets.Length + " top level assets:\n\n";

		foreach(string topLevelAsset in topLevelAssets)
		{
			// Prefer to recurse through root-level prefabs, instead of when found as dependencies
			var excludeAssets = new List<string>(topLevelAssets);
			excludeAssets.Remove(topLevelAsset);

			string[] lines = buildAssetTree(topLevelAsset, excludeAssets);
			report += string.Join("\n", lines) + "\n";
		}
		return report;
	}

	// Starting with a given asset, returns an indented list of all assets & dependencies,
	// any duplicated/seeen item has a " (dup)" suffix, and duplicate .prefab's won't be expanded
	private static string[] buildAssetTree( string startPath, IEnumerable<string> assetsToIgnore )
	{
		var assetResults = new List<string>();
		var assetsSeen = new HashSet<string>(assetsToIgnore);

		buildAssetTreeRecurse(startPath, 0, assetResults, assetsSeen);

		return assetResults.ToArray();
	}

	private static void buildAssetTreeRecurse( string path, int depth, List<string> assetResults, HashSet<string> assetsSeen )
	{
		// Add asset info (indented by hierarchy depth, flag if duplicate)
		bool isDuplicate = assetsSeen.Contains(path);
		assetsSeen.Add(path);

		// print indented path info, + (dup) suffix if it's a duplicate
		var indent = "".PadRight(depth*4);
		assetResults.Add(indent + path + (isDuplicate ? " (dup)" : ""));

		// Don't recurse through duplicate prefabs (avoid endless cycles)
		if (isDuplicate && path.ToLower().FastEndsWith(".prefab"))
		{
			assetResults.Add(indent + "    ... (not expanding duplicate prefab)"); 
		}
		else
		{
			// Recurse through immediate children (avoiding unused material texture references)
			var childrenAssets = CommonEditor.getDependenciesFixed(path).OrderBy(p => p).ToArray();
			foreach (var child in childrenAssets)
			{
				buildAssetTreeRecurse(child, depth + 1, assetResults, assetsSeen);
			}
		}
	}

	//=====================================================================================

	[MenuItem ("Zynga/Asset Reports/Generate cross-game asset report for selected bundles")]
	public static void generateCrossGameAssetReportForSelectedBundles()
	{
		var bundleNames = AssetBundleTagger.findAllBundleTagsInSelection();
		var fullReport = generateCrossGameAssetReportForBundles(bundleNames);

		// Put in copy-paste buffer so user can just 'paste' into a text editor
		EditorGUIUtility.systemCopyBuffer = fullReport;
		Debug.Log("Report copied to system clipboard; you can <paste> it to an editor...");
	}

	public static string generateCrossGameAssetReportForBundlesInPath(string path)
	{
		var bundleNames = AssetBundleTagger.findAllBundleTagsInPath(path);
		var fullReport = generateCrossGameAssetReportForBundles(bundleNames);
		return fullReport;
	}

	static string generateCrossGameAssetReportForBundles(string[] bundleNames)
	{
		Debug.Log("Creating cross-game asset reports for  " + bundleNames.Length + " bundles: " + string.Join(", ", bundleNames) + "\n");
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		string allReports = "";
		foreach (var bundleName in bundleNames)
		{
			string report = generateCrossGameAssetReportForBundle(bundleName);
			allReports += report;
		}

		Debug.Log("completed cross-game asset report in " + stopwatch.Elapsed);
		return allReports;
	}

	static string generateCrossGameAssetReportForBundle(string bundleName)
	{
		// Get the normal indented asset-tree, and process it
		string report = generateAssetTreeReportForBundle(bundleName);
		string[] lines = report.Split('\n').Select(line => line.Trim()).ToArray();

		string basePath = null;
		string firstGroup = null;
		string firstGame = null;
		string prevGroup = null;
		string prevGame = null;

		string errors = "";
		foreach (var line in lines)
		{
			// Find first "Assets/Data/Games/<group>/<game>" path
			if (firstGroup == null)
			{
				getGameGroupAndNameFromPath(line, out firstGroup, out firstGame);
				basePath = "    Assets/Data/Games/" + firstGroup + "/" + firstGame + "/..." + errors;
			}
			else
			{
				// Compare each remaining line against the first game group/path
				string group;
				string game;
				if (getGameGroupAndNameFromPath(line, out group, out game))
				{
					// same as the previous line? no need to say anything
					if (group == prevGroup && game == prevGame)
					{
						continue;
					}

					// Currently we only warn on cross-group mismatches (not cross-game)
					if (group != firstGroup)
					{
						errors += " !  " + line + "\n";
					}

					prevGroup = group;
					prevGame = game;
				}
			}
		}

		// If we have errors, add the expected path to the first line...
		if (errors == "")
		{
			return ("Bundle '" + bundleName + "' (okay)\n");
		}
		else
		{
			return ("Bundle '" + bundleName + "' ...\n" + basePath + "\n" + errors);
		}
	}


	// Game folders are generally of the form:
	//   Assets/Data/Games/<GroupName>/<GameName>/...  
	private static bool getGameGroupAndNameFromPath( string path, out string groupName, out string gameName )
	{
		groupName = null;
		gameName = null;

		if (path.Contains("(dup)"))
		{
			return false;
		}

		if (path.Contains("Assets/Data/Games/"))
		{
			var baseAndRemainder = path.Split( new string[] {"Assets/Data/Games/"}, System.StringSplitOptions.None);
			var groupAndGame = baseAndRemainder[1].Split('/');
			if (groupAndGame.Length >= 2)
			{
				if (groupAndGame[0] == "-Unsorted-")
				{
					return false;
				}

				groupName = groupAndGame[0];
				gameName = groupAndGame[1];
				return true;
			}
		}
		return false;
	}


}
