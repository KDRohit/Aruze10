using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class FileHelper
{
	public static string TrimPathToAssets(string path)
	{
		return path.Substring(path.IndexOf("Assets"));
	}
	
	public static string ConvertToUnitySlashes(string str)
	{
		const string forwardSlash = "/";
		const string backSlash = "\\";

		return str.Replace(backSlash, forwardSlash);
	}

	public static string CreateAssetsRelativePath(string path)
	{
		string fullPath = Path.GetFullPath(path);
		return ConvertToUnitySlashes(fullPath).Replace(Application.dataPath, "Assets");
	}

	/// Wrapper for Path.Combine: don't ignore first path if second path starts with a slash.
	public static string PathCombine(string first, string second)
	{
		string relativeSecond = second;
		if (Path.IsPathRooted(second))
		{
			relativeSecond = second.TrimStart('/');
		}
		return Path.Combine(first, relativeSecond);
	}

	public static List<FileInfo> FindFiles(DirectoryInfo root, string[] fileNamePattern)
	{
		if (!root.Exists)
		{
			return new List<FileInfo>();
		}

		List<FileInfo> resultList = new List<FileInfo>();
		for(int i = 0; i < fileNamePattern.Length; ++i)
		{
			resultList.AddRange(root.GetFiles(fileNamePattern[i], SearchOption.AllDirectories));
		}

		return resultList;
	}

	static List<FileInfo> listOfAllPrefabs = null;
	static Dictionary<string, AssetInfo> assetInfoDict = new Dictionary<string, AssetInfo>();
	[MenuItem ("Zynga/Wizards/Find Stuff/Find Selected Asset(s) Users")]
	static void FindAssetUsers()
	{
		if(!(EditorUtility.DisplayDialog("Find Asset User", "Start Find", "OK")))
		{
			return;
		}

		int index = 0;
		foreach(Object selectedObject in Selection.objects)
		{
			Debug.Log("====== Processing: " + selectedObject.name);
			string selectedPath = AssetDatabase.GetAssetPath(selectedObject);

			if(listOfAllPrefabs == null)
			{
				listOfAllPrefabs = FileHelper.FindFiles(new DirectoryInfo(Application.dataPath), new string[]{"*.mat", "*.prefab"});
			}

			List<string> foundUsers = new List<string>();
			foreach (FileInfo fi in listOfAllPrefabs)
			{
				var count = listOfAllPrefabs.Count() * Selection.objects.Count();
				bool cancelRequested = EditorUtility.DisplayCancelableProgressBar("Finding users of assets", "Processing", index / (1.0f * count));
				if (cancelRequested)
				{
					EditorUtility.ClearProgressBar();
					return;
				}
				index++;

				AssetInfo assetInfo = null;

				//build the lookup dictionary entry if needed
				if(!assetInfoDict.ContainsKey(fi.FullName))
				{
					var asset = AssetDatabase.LoadMainAssetAtPath(FileHelper.CreateAssetsRelativePath(fi.FullName));
					if(!(asset is Material || asset is GameObject))
					{
						continue;
					}

					string assetPath = AssetDatabase.GetAssetPath(asset);
					var deps = AssetDatabase.GetDependencies(new string[]{ assetPath });
					AssetInfo newAssetInfo = new AssetInfo(asset, assetPath, deps);
					assetInfoDict.Add(fi.FullName, newAssetInfo); 
					assetInfo = newAssetInfo;
				}
				else
				{
					assetInfo = assetInfoDict[fi.FullName];
				}

				if(assetInfo.dependencies.Contains(selectedPath))
				{
					if(!foundUsers.Contains(assetInfo.assetPath))
					{
						foundUsers.Add(assetInfo.assetPath);
						Debug.Log(assetInfo.assetPath + " uses " + selectedObject.name, assetInfo.obj);
					}
				}
			}
			foundUsers.Clear();
			Debug.Log("====== Completed Find of: " + selectedObject.name);
		}
		EditorUtility.ClearProgressBar();
	}


	[MenuItem ("Zynga/Editor Cleanup")]
	static void DoCleanUp()
	{
		EditorUtility.ClearProgressBar();

		if(listOfAllPrefabs != null)
		{
			listOfAllPrefabs.Clear();
		}
		listOfAllPrefabs = null;

		if(assetInfoDict != null)
		{
			assetInfoDict.Clear();
		}
		assetInfoDict = null;
		EditorUtility.UnloadUnusedAssetsImmediate();
		System.GC.Collect(3, System.GCCollectionMode.Forced);
	}

	//make usable in right-click menu
	[MenuItem ("Assets/Find/Find Users of This Asset")]
	static void FindThisAssetUsers()
	{
		FindAssetUsers();
	}

	/// <summary>
	/// Asset info.
	/// Temp storage type for speeding up asset user finds
	/// </summary>
	class AssetInfo
	{
		public AssetInfo(Object ob, string ap, string[] deps)
		{
			assetName = ob.name;
			assetPath = ap;
			dependencies = deps;
			obj = ob;
		}

		public Object obj;
		public string assetPath;
		public string assetName;
		public string[] dependencies;
	}
}
