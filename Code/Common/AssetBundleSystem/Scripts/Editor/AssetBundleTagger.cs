//
//  AssetBundleTagger.cs
//
// Utility scripts to automate tagging of asset bundles, labelling by SKU, and renaming/organizing file hierarchy.
// These exist to help transition us from the old unity bundling system (driven by config files) to our new
// Unity-5 based bundling system, which is driven by tags & labels on our assets.
//
// This file will likely go away after we're fully transitioned.
//
#pragma warning disable 0436 // So we can have that static constructor below.

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class TextWindow : EditorWindow
{
	string content;
	Vector2 scrollPos = new Vector2();

	public static TextWindow Show(string title, string content)
	{
		var window = EditorWindow.GetWindow<TextWindow>(true, title);
		window.content = content;
		return window;
	}

	void OnGUI ()
	{
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
		EditorGUILayout.TextArea(content, GUILayout.ExpandHeight(true) );
		EditorGUILayout.EndScrollView();
	}
}


public class AssetBundleTagger 
{
	[MenuItem ("Zynga/BundlesV2/Show BundleTags")]
	public static void showBundleTags()
	{
		var allBundleNames = AssetDatabase.GetAllAssetBundleNames();
		var unusedBundleNames = AssetDatabase.GetUnusedAssetBundleNames();

		var text = "";
		foreach(var name in allBundleNames)
		{
			text += name + (unusedBundleNames.Contains(name) ? "  (unused)" : "") + "\n";
		}
		TextWindow.Show( "BundleTags", text );
	}


	[MenuItem ("Zynga/BundlesV2/Show Tagged Assets")]
	public static void showTaggedAssets()
	{
		System.Text.StringBuilder text = new System.Text.StringBuilder();
		foreach(var bundleName in AssetDatabase.GetAllAssetBundleNames())
		{
			var assetPaths = AssetDatabase.FindAssets("b:" + bundleName).Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();
			text.AppendFormat("{0} :\n\t{1}\n", bundleName, string.Join("\n\t", assetPaths));
		}

		var report = text.ToString();

		// and stick it in the debugger
		Debug.Log(report);

		// Put in copy-paste buffer so user can just 'paste' into a text editor
		Debug.LogWarning("Show Tagged Assets: results copied to cut/paste buffer and Editor Log");
		EditorGUIUtility.systemCopyBuffer = report;

		//TextWindow.Show( "Tagged Assets", report );   // string too long for TextWindow
	}


	// Only shows the directly-labelled assets (not children assets)
	[MenuItem ("Zynga/BundlesV2/Show Labelled Assets")]
	public static void showLabelledAsset()
	{
		// get a list of all the labelled assets per sku
		var skuToAssetPaths = new Dictionary<string, IEnumerable<string>>();
		foreach(var sku in SkuResources.ALL_SKUS)
		{
			skuToAssetPaths[sku.ToString()] = AssetDatabase.FindAssets("l:"+sku).Select( guid => AssetDatabase.GUIDToAssetPath(guid) );
		}

		// Make a single sorted list of all the distinct assetpaths
		var distinctAssets = skuToAssetPaths.Values.SelectMany(x => x).Distinct().ToList();
		distinctAssets.Sort();

		displaySkuLabelledElements(skuToAssetPaths, distinctAssets, "Sku-Labelled Assets");
	}


	// Only shows the directly-labelled assets (not children assets)
	[MenuItem ("Zynga/BundlesV2/Show Sku specific Asset Bundles")]
	public static void showSkuSpecificAssetBundles()
	{
		// Get a list of all the sku-specific tagged bundles per sku
		var skuToBundleNames = new Dictionary<string, IEnumerable<string>>();
		foreach(var sku in SkuResources.ALL_SKUS)
		{
			skuToBundleNames[sku.ToString()] = CreateAssetBundlesV2.getAssetBundleNamesForSku(sku);
		}

		// Make a single sorted list of all the distinct bundlenames
		var distinctBundles = skuToBundleNames.Values.SelectMany(x => x).Distinct().ToList();
		distinctBundles.Sort();

		displaySkuLabelledElements(skuToBundleNames, distinctBundles, "Sku-Specific Asset Bundles");
	}

	[MenuItem ("Zynga/BundlesV2/Show Sku specific Games Asset Bundles")]
	public static void showSkuSpecificGamesAssetBundles()
	{
		// Get a list of all the sku-specific tagged bundles per sku
		var skuToBundleNames = new Dictionary<string, IEnumerable<string>>();
		foreach(var sku in SkuResources.ALL_SKUS)
		{
			var bundleNames = CreateAssetBundlesV2.getAssetBundleNamesForSku(sku);
			skuToBundleNames[sku.ToString()] = CreateAssetBundlesV2.filterAssetBundleNames(bundleNames, "games");
		}

		// Make a single sorted list of all the distinct bundlenames
		var distinctBundles = skuToBundleNames.Values.SelectMany(x => x).Distinct().ToList();
		distinctBundles.Sort();

		displaySkuLabelledElements(skuToBundleNames, distinctBundles, "Sku-Specific Games-only Asset Bundles");
	}

	[MenuItem ("Zynga/BundlesV2/Show Sku specific Features Asset Bundles")]
	public static void showSkuSpecificFeaturesAssetBundles()
	{
		// Get a list of all the sku-specific tagged bundles per sku
		var skuToBundleNames = new Dictionary<string, IEnumerable<string>>();
		foreach(var sku in SkuResources.ALL_SKUS)
		{
			var bundleNames = CreateAssetBundlesV2.getAssetBundleNamesForSku(sku);
			skuToBundleNames[sku.ToString()] = CreateAssetBundlesV2.filterAssetBundleNames(bundleNames, "features");
		}

		// Make a single sorted list of all the distinct bundlenames
		var distinctBundles = skuToBundleNames.Values.SelectMany(x => x).Distinct().ToList();
		distinctBundles.Sort();

		displaySkuLabelledElements(skuToBundleNames, distinctBundles, "Sku-Specific Features-only Asset Bundles");
	}

	private static void displaySkuLabelledElements(Dictionary<string, IEnumerable<string>> skuToElements, IEnumerable<string> distinctElements, string windowTitle)
	{
		// Want to print each line with the SKU indicators
		StringBuilder text = new StringBuilder();

		foreach(var element in distinctElements)
		{
			// Create each line showing skulabels (hir/sir/tv) that are set and the bundle name
			foreach(var sku in SkuResources.ALL_SKUS)
			{
				bool inThisSku = skuToElements[sku.ToString()].Contains(element);
				text.AppendFormat("{0}\t", (inThisSku ? sku.ToString() : ""));
			}
			text.AppendFormat("  {0}", element);
			text.AppendLine();
		}

		TextWindow.Show(windowTitle, text.ToString());
	}

	[MenuItem ("Zynga/BundlesV2/Clear All BundleTags")]
	public static void ClearAllBundleTags()
	{
		// Delete all assetbundle tags
		AssetDatabase.RemoveUnusedAssetBundleNames();
		foreach(var bundleName in AssetDatabase.GetAllAssetBundleNames())
		{
			AssetDatabase.RemoveAssetBundleName(bundleName, true);
		}
	}

	[MenuItem ("Zynga/BundlesV2/Clear All Labels")]
	public static void ClearAllLabels()
	{
		// Remove all sku-labels

		// First get all the sku-labelled assets; ie: search with "l:hir l:sir l:tv"
		var searchFilter = SkuResources.ALL_SKUS.Select(sku => " l:"+sku).Aggregate((a,b) => a+b);
		var allSkuLabelledAssets = AssetDatabase.FindAssets(searchFilter).Select( guid => AssetDatabase.GUIDToAssetPath(guid) );
		foreach(var assetPath in allSkuLabelledAssets)
		{
			var obj = AssetDatabase.LoadMainAssetAtPath( assetPath );
			AssetDatabase.SetLabels(obj, null );
		}
	}

	// Renames all the "/ToBundle/" folders back to "/Resources/" folders
	// Do this to undo the resource->ToBundle folder renaming that happened as part of ApplyAllTags()
	[MenuItem ("Zynga/BundlesV2/Restore Resources folders")]
	public static void RestoreResourcesFolders()
	{
		List<string> allToBundlePaths = new List<string>();
		CommonEditor.recursiveGatherPaths("/ToBundle", "Assets", allToBundlePaths);

		foreach(var path in allToBundlePaths)
		{
			Debug.Log( "Renaming Folder:  " + path );
			var result = AssetDatabase.RenameAsset(path, "Resources");
			if (!string.IsNullOrEmpty(result))
			{
				Debug.LogError("ERROR trying to rename folder: " + path + "\nUnity Msg: " + result);
			}
		}
	}


	// Show us all the files / assets / dependent files that are included in a bundle (help us track down circular dependencies)
	[MenuItem ("Zynga/BundlesV2/Show Bundle Files And Dependencies")]
	public static void showBundleFilesAndDependencies()
	{
		string report = "(Full Report included in system copy-paste buffer...)";
		string bundleName = "";
		if (Selection.objects != null && Selection.objects.Length > 0)
		{
			// If there is a selected asset then grab its bundle tag and use it.
			string selectedAssetPath = AssetDatabase.GetAssetPath(Selection.objects[0]);

			AssetImporter importer = AssetImporter.GetAtPath(selectedAssetPath);
		    bundleName = importer.assetBundleName;
			if (string.IsNullOrEmpty(bundleName))
			{
				Debug.LogErrorFormat("AssetBundleTagger.cs -- showBundleFiledAndDependencies -- Asset at path {0} does not have a bundle tag.", selectedAssetPath);
				return;
			}
		}
		else
		{
			Debug.LogErrorFormat("AssetBundleTagger.cs -- showBundleFiledAndDependencies -- no asset currently selected.");
			return;
		}
		{
			// gets the items that are tagged for this bundle (usually just a few folders)
			var bundlePaths = AssetDatabase.FindAssets("b:" + bundleName).Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();

			// Find all the folders that were returned
			var folderPaths = bundlePaths.Where(path => AssetDatabase.IsValidFolder(path)).ToArray();

			var assetPathsInFolders = AssetDatabase.FindAssets("", folderPaths)
				.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
				.Concat( bundlePaths ) // include original list (files/folders)
				.Where(path => !AssetDatabase.IsValidFolder(path)) //exclude folders
				.Distinct()
				.ToArray();
			//Debug.Log("InFolders (" + inFolders.Length + ") : \n  " + string.Join("\n  ", inFolders));

			report += "Dependencies for bundle '" + bundleName + "' : \n";
			foreach(var assetPath in assetPathsInFolders)
			{
				var dependencies = AssetDatabase.GetDependencies(assetPath, true);
				report += "Asset " + assetPath + " depends on: \n  " + string.Join("\n  ", dependencies); 
				report += "\n\n";
			}

		}
		report += "\n<End Report>\n";

		// and stick it in the debugger
		Debug.Log(report);

		// and show in a pop-up  (doh, maxes out at 64KB)...
		//TextWindow.Show( "Bundle Dependencies", report );

		// Put in copy-paste buffer so user can just 'paste' into a text editor
		EditorGUIUtility.systemCopyBuffer = report;
	}

	// Checks all bundles for any top-level FBX files... we don't want to include FBX files!
	[MenuItem ("Zynga/BundlesV2/Check For Bundled FBX Files")]
	public static void checkForBundledFBXFiles()
	{
		int fbxCount = 0;
		var bundlesWithFBXFiles = new HashSet<string>();

		var bundleNames = AssetDatabase.GetAllAssetBundleNames();
		foreach (var bundleName in bundleNames)
		{
			string[] assets = AssetDatabase.GetAssetPathsFromAssetBundle( bundleName );
			foreach(var asset in assets)
			{
				if (asset.ToLowerInvariant().FastEndsWith(".fbx"))
				{
					Debug.Log("WARNING! Bundle " + bundleName + " contains FBX " + asset);
					bundlesWithFBXFiles.Add(bundleName);
					fbxCount++;
				}
			}
		}
		Debug.Log("COMPLETE; Checked " + bundleNames.Length + " bundles, found " + fbxCount + " FBX files in " + bundlesWithFBXFiles.Count + " bundles" );
	}
		

	//==================== Helper Functions

	// Searches the selected files & folders for any/all bundle tags
	//
	// Returns an array of unique bundleNames found
	//
	public static string[] findAllBundleTagsInSelection()
	{
		// Get all objects hilighted in the project pane...
		var selectedObjects = Selection.GetFiltered( typeof(UnityEngine.Object), SelectionMode.Assets);

		// Make a list of all the assetpaths (including folders) to check, checking inside of folders as we go
		var assetsToCheck = new List<string>();
		foreach (var obj in selectedObjects)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			assetsToCheck.Add(path);

			// Get any tagged assets inside any requested folders
			if (Directory.Exists(path))
			{
				var taggedAssets = AssetDatabase
					.FindAssets("b:", new string[] { path })
					.Select(guid => AssetDatabase.GUIDToAssetPath(guid));
				assetsToCheck.AddRange(taggedAssets);
			}
		}

		var bundleNamesFound = getBundleTagsOnAssetPaths( assetsToCheck.ToArray() );
		return bundleNamesFound;
	}

	// only evaluates specific asset paths, does not search children folders...
	public static string[] getBundleTagsOnAssetPaths(string[] paths)
	{
		// Then check each asset for a possible bundle tag
		var bundleNamesFound = new HashSet<string>();
		foreach (var assetPath in paths)
		{
			var importer = AssetImporter.GetAtPath(assetPath);
			if (importer != null)
			{
				var bundleName = importer.assetBundleName;
				if (!string.IsNullOrEmpty(bundleName))
				{
					bundleNamesFound.Add(bundleName);
				}
			}
		}
		return bundleNamesFound.OrderBy(name => name).ToArray();
	}

	// Returns a list of bundlenames found within a folder
	public static string[] findAllBundleTagsInPath(string path)
	{
		var assetPaths = AssetDatabase
			.FindAssets("b:", new string[] { path })
			.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
			.ToArray();

		var bundleNamesFound = getBundleTagsOnAssetPaths( assetPaths );
		return bundleNamesFound;
	}

	// Tries to move an asset and clear's any labels/tags, log's error if there's an error
	// Returns TRUE if successful
	static bool moveAssetAndClearTags(string srcAssetPath, string destAssetPath)
	{
		string errorMsg = AssetDatabase.MoveAsset( srcAssetPath, destAssetPath );
		if (!string.IsNullOrEmpty(errorMsg))
		{
			Debug.LogError("ERROR: " + errorMsg);
			return false;
		}

		setLabelForAsset( null, destAssetPath );
		setBundleForAsset( "", destAssetPath );
		return true;
	}

	static bool hasSameElements<T>(IEnumerable<T> a, IEnumerable<T> b)
	{
		bool same = a.Except(b).Count()==0 && b.Except(a).Count()==0;
		return same;
	}

	static bool assetFolderExists(string assetPath)
	{
		var absolutePath = Path.GetFullPath( assetPath );
		return Directory.Exists( absolutePath );
	}

	static void setBundleAndSku(string bundleName, string[] labels, string assetPath)
	{
		// set (optional) bundlename
		if (bundleName != null) { setBundleForAsset(bundleName, assetPath); }

		// set (optional) labels
		if (labels != null) { setLabelsForAsset(labels, assetPath); }
	}

	static void setBundleForAsset(string bundleName, string assetPath)
	{
		var importer = AssetImporter.GetAtPath( assetPath );
		importer.assetBundleName = bundleName;
	}

	// Replaces any existing labels with new set of labels
	static void setLabelsForAsset(string[] labels, string assetPath)
	{
		// convert null to empty array
		if (labels==null) { labels = new string[0]; }

		// make all sku labels UPPERCASE
		labels = labels.Select( label => label.ToUpper() ).ToArray();

		var obj = AssetDatabase.LoadMainAssetAtPath( assetPath );
		AssetDatabase.SetLabels(obj, labels );
	}

	// Replaces any existing labels with new single label (or no label)
	static void setLabelForAsset(string label, string assetPath)
	{
		string[] labelArray = (label != null) ? new string[1] {label} : null;
		setLabelsForAsset( labelArray, assetPath );
	}

	static void addLabelToAsset(string label, string assetPath)
	{
		// Labels should always start in Uppercase, Unity editor loses its shit if you enter lowercase labels
		// It also has issues managing labels that only differ by case, so try not to introduce inconsistently cased labels

		// do sku labels in UPPERCASE
		label = label.ToUpper();

		var obj = AssetDatabase.LoadMainAssetAtPath( assetPath );
		var existingLabels = AssetDatabase.GetLabels( obj );
		if (!existingLabels.Contains(label, System.StringComparer.CurrentCultureIgnoreCase))
		{
			var newLabels = existingLabels.ToList();
			newLabels.Add(label);
			AssetDatabase.SetLabels(obj, newLabels.ToArray() );
		}
	}
}


