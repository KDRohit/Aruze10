#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using TMPro;

public class CreatorData {

	public Transform root;
	public PrefabEntry currentSelection;
	public GameObject baseTemplate;
	public GameObject scrollBar;
	public GameObject baseMesh;
	public Dictionary<string, List<PrefabEntry>> prefabIndex = new Dictionary<string, List<PrefabEntry>>();

	public CreatorData(CreatorData data = null)
	{
		init(data);
	}

	public void cleanUp()
	{
		root = null;
		currentSelection = null;
		prefabIndex = null;
	}

	public void listInit()
	{
		prefabIndex = new Dictionary<string, List<PrefabEntry>>();
	}

	public void populateList()
	{
		// Populate the dialog prefab list.
		DirectoryInfo dir = new DirectoryInfo("Assets/Data/HIR/Prefabs/Templates");
		FileInfo[] info = dir.GetFiles("*.prefab");
		if(info.Length > 0)
		{
			prefabIndex.Add("Default", new List<PrefabEntry>());
			foreach (FileInfo f in info)
			{
				prefabIndex["Default"].Add(new PrefabEntry(f.Name.Split('.')[0], "Assets" + f.FullName.Substring(Application.dataPath.Length)));
			}
		}

		DirectoryInfo[] subDir = dir.GetDirectories();
		if(subDir.Length > 0)
		{
			foreach(DirectoryInfo d in subDir)
			{
				prefabIndex.Add(d.Name, new List<PrefabEntry>());
				FileInfo[] subInfo = d.GetFiles("*.prefab");
				if(subInfo.Length > 0)
				{
					foreach(FileInfo f in subInfo)
					{
						prefabIndex[d.Name].Add(new PrefabEntry(f.Name.Split('.')[0], "Assets" + f.FullName.Substring(Application.dataPath.Length)));
					}
				}
			}
		}
		baseTemplate = AssetDatabase.LoadAssetAtPath("Assets/Data/HIR/Prefabs/Templates/Dialog/Dialog Generic 00.prefab", typeof(GameObject)) as GameObject;
		baseMesh = AssetDatabase.LoadAssetAtPath("Assets/Data/HIR/Prefabs/Templates/Mesh/MeshTemplate.prefab", typeof(GameObject)) as GameObject;
	}


	public GameObject tryLoadPrefab(PrefabEntry prefabEntry)
	{
		GameObject result = AssetDatabase.LoadAssetAtPath(prefabEntry.prefabPath, typeof(GameObject)) as GameObject;
		if(result == null)
		{
			Debug.LogError("Prefab loading failed for: " + prefabEntry.displayName + " at " + prefabEntry.prefabPath);
		}
		return result;
	}

	public void init(CreatorData data = null)
	{
		root = null;
		prefabIndex = new Dictionary<string, List<PrefabEntry>>();
		listInit();
		populateList();
	}


	public class PrefabEntry
	{
		public string displayName;
		public string prefabPath;

		public PrefabEntry(string name, string path)
		{
			displayName = name;
			prefabPath = path;
		}
	}

}
#endif
