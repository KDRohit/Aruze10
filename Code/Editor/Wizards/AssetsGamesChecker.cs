using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
Sanity checks all the asset dependencies in the 'Assets Games' folder.
*/
public class AssetsGamesChecker : ScriptableWizard
{
	public bool doFileMoving = false;
	public bool doFileDeleting = false;
	
	[MenuItem ("Zynga/Assets/Check 'Assets Games' dependencies")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<AssetsGamesChecker>("Check 'Assets Games' dependencies", "Close", "Run check");
	}
	
	public void OnWizardOtherButton()
	{	
		checkAssetsGames(doFileMoving, doFileDeleting);
	}
	
	public void OnWizardUpdate()
	{
		helpString = "";
	}
	
	public void OnWizardCreate()
	{
	
	}
	
	// Checks all cross-dependencies in 'Assets/Assets Games/' folders, outputs detailed report in console
	public static void checkAssetsGames(bool doFileMoving, bool doFileDeleting)
	{
		Dictionary<string, List<string>> usageMap = new Dictionary<string, List<string>>();
		
		// Gather up all asset dependencies of all assets in "Assets/Assets Games/"
		string[] folders = Directory.GetDirectories("Assets/Data/Games", "*", SearchOption.TopDirectoryOnly);

		int index = 0;
		foreach (string folder in folders) 
		{
			bool cancelRequested = EditorUtility.DisplayCancelableProgressBar("Checking dependencies for " + folder, "Gathering assets and dependencies...", (float)index / (float)folders.Length);
			if (cancelRequested)
			{
				EditorUtility.ClearProgressBar();
				Debug.Log("Aborted dependency check.");
				return;
			}
			index++;
		
			gatherDependencies(folder, usageMap);
		}
		EditorUtility.ClearProgressBar();
		
		int fineCount = 0;
		int deleteCount = 0;
		int deleteFailCount = 0;
		int unmovableCount = 0;
		int moveCount = 0;
		int moveFailCount = 0;
		int unknownCount = 0;
		
		Debug.Log("DEPENDENCY REPORT, FILES CHECKED: " + usageMap.Count);
		index = 0;
		foreach (KeyValuePair<string, List<string>> p in usageMap)
		{
			string asset = p.Key;
			List<string> references = p.Value;
			
			if (index % 100 == 0)
			{
				bool cancelRequested = EditorUtility.DisplayCancelableProgressBar("Logging for " + usageMap.Count + " dependencies", "Processing and logging results...", (float)index / (float)usageMap.Count);
				if (cancelRequested)
				{
					EditorUtility.ClearProgressBar();
					Debug.Log("REPORT ABORTED, RESULTS INCOMPLETE");
					return;
				}
			}
			index++;
			
			int groupCount;
			bool isExternallyReferenced = false;
			if (references.Count == 1)
			{
				groupCount = references.Count;
			}
			else
			{
				Dictionary<string, bool> groups = new Dictionary<string, bool>();
				foreach (string reference in references)
				{
					string[] parts = reference.Split('/');
					if (parts != null &&
						parts.Length > 3 &&
						parts[0] == "Assets" &&
						parts[1] == "Data" &&
						parts[2] == "Games" &&
						!groups.ContainsKey(parts[3]))
					{
						groups.Add(parts[3], true);
					}
					else if(!reference.FastStartsWith("Assets/Data/Games/"))
					{
						isExternallyReferenced = true;
					}
				}
				groupCount = groups.Count;
			}

			string groupPath = "Assets/Data/Games/-Unsorted-/";
			if (groupCount == 1 && !isExternallyReferenced)
			{
				string[] parts = references[0].Split('/');
				if (parts != null && parts.Length > 2)
				{
					groupPath = string.Format("{0}/{1}/{2}/", parts[0], parts[1], parts[2]);
				}
			}
			
			bool isUnusedAsset = (references.Count == 1 && !asset.Contains("/Resources/") && !asset.Contains("/Source Hi/") && !asset.Contains("/Keep/"));
			bool isFineStayingPut = (asset.StartsWith(groupPath));
			bool isMovableAsset = (!asset.Contains("/NGUI/") && !asset.Contains("/Resources/"));
			bool isSingleGroupAsset = (groupCount == 1 && !asset.StartsWith(groupPath));
			bool isMultiGroupAsset = (groupCount > 1 && !asset.StartsWith(groupPath));
			
			if (isExternallyReferenced)
			{
				Debug.LogError(getLogStringForReferences("Externally referenced asset " + asset,
					isUnusedAsset, isFineStayingPut, isMovableAsset, isSingleGroupAsset, isMultiGroupAsset, asset, references));
			}
			
			if (isUnusedAsset)
			{
				// This asset is seemingly unused, delete it
				if (isExternallyReferenced)
				{
					Debug.LogWarning(getLogStringForReferences("Can't delete asset " + asset,
						isUnusedAsset, isFineStayingPut, isMovableAsset, isSingleGroupAsset, isMultiGroupAsset, asset, references));
				}
				else if (doFileDeleting)
				{
					bool success = AssetDatabase.DeleteAsset(asset);
					if (success)
					{
						Debug.Log(getLogStringForReferences("Deleted " + asset,
							isUnusedAsset, isFineStayingPut, isMovableAsset, isSingleGroupAsset, isMultiGroupAsset, asset, references));
						
						deleteCount++;
					}
					else
					{
						Debug.LogError(getLogStringForReferences("Unable to delete " + asset,
							isUnusedAsset, isFineStayingPut, isMovableAsset, isSingleGroupAsset, isMultiGroupAsset, asset, references));
					
						deleteFailCount++;
					}
				}
				else
				{
					Debug.Log(getLogStringForReferences("Would delete unused asset " + asset,
						isUnusedAsset, isFineStayingPut, isMovableAsset, isSingleGroupAsset, isMultiGroupAsset, asset, references));
						
					deleteCount++;
				}
			}
			else if (isFineStayingPut)
			{
				// This asset is all good, don't do anything with it
				//Debug.Log(getLogStringForReferences("Asset okay",
				//	isUnusedAsset, isFineStayingPut, isMovableAsset, isSingleGroupAsset, isMultiGroupAsset, asset, references));
			
				fineCount++;
			}
			else if (!isMovableAsset)
			{
				// Asset can't be moved, but should be moved
				Debug.LogWarning(getLogStringForReferences("Unmovable asset",
					isUnusedAsset, isFineStayingPut, isMovableAsset, isSingleGroupAsset, isMultiGroupAsset, asset, references));
			
				unmovableCount++;
			}
			else if (isSingleGroupAsset || isMultiGroupAsset)
			{
				// Move the file into the appropriate "Assets/Assets Games/<something>" folder
				string sourceFile = asset;
				string destinationFile = Path.Combine(groupPath + "Stuff/", Path.GetFileName(asset));
				
				string testMoveResult = AssetDatabase.ValidateMoveAsset(sourceFile, destinationFile);
				if (string.IsNullOrEmpty(testMoveResult))
				{
					if (doFileMoving)
					{
						AssetDatabase.MoveAsset(sourceFile, destinationFile);
						Debug.Log(getLogStringForReferences(string.Format("Moved {0} -> {1}", sourceFile, destinationFile),
							isUnusedAsset, isFineStayingPut, isMovableAsset, isSingleGroupAsset, isMultiGroupAsset, asset, references));
					}
					else
					{
						Debug.Log(getLogStringForReferences(string.Format("Would move {0} -> {1}", sourceFile, destinationFile),
							isUnusedAsset, isFineStayingPut, isMovableAsset, isSingleGroupAsset, isMultiGroupAsset, asset, references));
					}
					
					moveCount++;
				}
				else
				{
					Debug.LogError(getLogStringForReferences(string.Format("Unable to move {0} -> {1} because '{2}'", sourceFile, destinationFile, testMoveResult),
						isUnusedAsset, isFineStayingPut, isMovableAsset, isSingleGroupAsset, isMultiGroupAsset, asset, references));
					
					moveFailCount++;
				}
			}
			else 
			{
				// Unanticipated edge case, what happened?
				Debug.LogError(getLogStringForReferences("Unknown error",
					isUnusedAsset, isFineStayingPut, isMovableAsset, isSingleGroupAsset, isMultiGroupAsset, asset, references));
					
				unknownCount++;
			}
		}
		
		// Sync for the moved asset files
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		
		EditorUtility.ClearProgressBar();
		Debug.Log(string.Format("END REPORT: {0} fine, {1} moved, {2} deleted, {3} unmovable, {4} move failed, {5} delete failed, {6} unknown errors",
			fineCount, moveCount, deleteCount, unmovableCount, moveFailCount, deleteFailCount, unknownCount));
	}
	
	private static string getLogStringForReferences(
		string message,
		bool isUnusedAsset,
		bool isFineStayingPut,
		bool isMovableAsset,
		bool isSingleGroupAsset,
		bool isMultiGroupAsset,
		string asset,
		List<string> references)
	{
		string msg = string.Format("[{0}{1}{2}{3}{4}] {5}:\n{6}",
			isUnusedAsset ? "U" : "-",
			isFineStayingPut ? "P" : "-",
			isMovableAsset ? "V" : "-",
			isSingleGroupAsset ? "S" : "-",
			isMultiGroupAsset ? "M" : "-",
			message,
			asset);
			
		foreach (string reference in references)
		{
			msg += "\n  @ " + reference;
		}
		
		return msg;
	}
	
	// Displays a list of all dependencies in the console for the selected asset
	[MenuItem ("Zynga/Assets/List selection dependents in 'Assets Games'")]
	public static void checkAsset()
	{
		// Build list of paths to current selection
		List<string> checkList = new List<string>();
		foreach(Object selectedObject in Selection.objects)
		{
			string selectedPath = AssetDatabase.GetAssetPath(selectedObject);
			
			if (!string.IsNullOrEmpty(selectedPath))
			{
				checkList.Add(selectedPath);
			}
		}
		
		Dictionary<string, List<string>> usageMap = new Dictionary<string, List<string>>();
		
		// Gather up all asset dependencies of all assets in "Assets/Assets Games/"
		string[] folders = Directory.GetDirectories("Assets/Data/Games", "*", SearchOption.TopDirectoryOnly);

		int index = 0;
		foreach (string folder in folders) 
		{
			bool cancelRequested = EditorUtility.DisplayCancelableProgressBar("Checking dependencies for " + folder, "Processing", (float)index / (float)folders.Length);
			if (cancelRequested)
			{
				EditorUtility.ClearProgressBar();
				return;
			}
			index++;
		
			gatherDependencies(folder, usageMap);
		}
		EditorUtility.ClearProgressBar();
		
		foreach (KeyValuePair<string, List<string>> p in usageMap)
		{
			string asset = p.Key;
			List<string> references = p.Value;
			
			if (index % 100 == 0)
			{
				bool cancelRequested = EditorUtility.DisplayCancelableProgressBar("Logging for " + usageMap.Count + " dependencies", "Logging", (float)index / (float)usageMap.Count);
				if (cancelRequested)
				{
					EditorUtility.ClearProgressBar();
					return;
				}
			}
			index++;
			
			foreach (string checkItem in checkList)
			{
				if (asset == checkItem)
				{
					string msg = "Checking: " + asset;
					foreach (string reference in references)
					{
						msg += "\n  @ " + reference;
					}
					Debug.Log(msg);
				}
			}
		}
		
		EditorUtility.ClearProgressBar();
	}
	
	private static void gatherDependencies(string path, Dictionary<string, List<string>> usageMap)
	{
		foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)) 
		{
			if (!file.FastEndsWith(".meta"))
			{
				foreach (string dependency in AssetDatabase.GetDependencies(new string[] { file } ))
				{
					if (!string.IsNullOrEmpty(dependency) && dependency.FastStartsWith("Assets/Data/Games/"))
					{
						if (usageMap.ContainsKey(dependency))
						{
							usageMap[dependency].Add(file);
						}
						else
						{
							List<string> references = new List<string>();
							references.Add(file);
							usageMap.Add(dependency, references);
						}
					}
				}
			}
		}
	}
}
