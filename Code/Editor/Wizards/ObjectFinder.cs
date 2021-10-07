using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Finds all the GameObjects in the current selection that match the specified criteria filters.
*/
public class ObjectFinder : ScriptableWizard
{
	public enum SearchPath
	{
		ALL,
		COMMON,
		GAMES,
		HIR
	}

	public bool searchAssetLibrary = false;		///< Do we search the current selection or the entire library?
	public bool displayRootObjects = false;		///< Do we only want to see the roots (for easier double clicking)?
	public bool removeIfPrefabChild = false;	///< Do we remove the child from a prefab it lives in?
	public bool ignoreGames = false;			///< Ignore games directory?
	public string ignoreSpecificClasses = "";	// ignore any specific classes (comma separated)
	public bool removeComponent = false;		///< Should we remove any such found components?
	public bool onlyActive = false;				///< Searches for only active components
	public string rootPath = "";				///< Only consider objects in this path. Empty means the whole asset library.
	public string onlyWithName = "";			///< Only consider objects with the given name
	public string onlyWithComponent = "";		///< Filter out all components but this named one
	public LayerMask onlyInLayers = -1;			///< Only consider objects on the given layers
	public string onlyWithTag = "";				///< Only consider objects with the given tag
	
	public GameObject[] results;				///< The array of results from the most recent find
	
	[MenuItem ("Zynga/Wizards/Find Stuff/Find Objects %#&f")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<ObjectFinder>("Find Scripts", "Close", "Find");
	}
	
	// find button clicked
	public void OnWizardOtherButton()
	{
		string[] ignoreClasses = ignoreSpecificClasses.Split(',');
		List<GameObject> searchSpace = new List<GameObject>();
		
		if (searchAssetLibrary)
		{
			// Search the asset library for all instances
			if (!rootPath.FastEndsWith("/"))
			{
				rootPath += "/";
			}
			
			List<GameObject> allPrefabs = CommonEditor.gatherPrefabs(System.IO.Path.Combine("Assets" + rootPath));
			foreach (GameObject prefab in allPrefabs)
			{
				if (ignoreGames && AssetDatabase.GetAssetPath(prefab).Contains("Assets/Data/Games/"))
				{
					continue;
				}

				if (ignoreClasses.Length > 0)
				{
					for (int i = 0; i < ignoreClasses.Length; ++i)
					{
						string classType = ignoreClasses[i];
						
						if (prefab.GetComponent(ignoreClasses[i]) != null)
						{
							continue;
						}
					}
				}

				List<GameObject> allChildren = CommonGameObject.findAllChildren(prefab, true);
				foreach (GameObject child in allChildren)
				{
					searchSpace.Add(child);
				}
			}
		}
		else
		{
			// Search the current selection for all instances
			Object[] selection = Selection.GetFiltered(typeof(GameObject), SelectionMode.TopLevel | SelectionMode.DeepAssets);
			foreach (Object go in selection)
			{
				List<GameObject> allChildren = CommonGameObject.findAllChildren((GameObject)go, true);
				foreach (GameObject child in allChildren)
				{
					searchSpace.Add(child);
				}
			}
		}
		
		List<GameObject> matches = new List<GameObject>();
		
		// Search selection for matches
		foreach (GameObject searchItem in searchSpace)
		{
			// Does it have the layer we're looking for?
			int layer = 1 << searchItem.layer;
			if ((onlyInLayers.value & layer) == 0)
			{
				continue;
			}
			
			// Does it have the tag we're looking for?
			if (onlyWithTag != "")
			{
				if (searchItem.tag != onlyWithTag)
				{
					continue;
				}
			}
			
			// Does it have the component we're looking for?
			if (onlyWithComponent != "")
			{
				Component item = searchItem.GetComponent(onlyWithComponent);
				if (item == null)
				{
					continue;
				}
				if (onlyActive)
				{
					// Need to cast to several types in order to check enabled
					Collider col = item as Collider;
					if (col != null && (!col.enabled || !searchItem.activeSelf))
					{
						continue;
					}
					
					Behaviour behavior = item as Behaviour;
					if (behavior != null && (!behavior.enabled || !searchItem.activeSelf))
					{
						continue;
					}
					
					Renderer renderer = item as Renderer;
					if (renderer != null && (!renderer.enabled || !searchItem.activeSelf))
					{
						continue;
					}
				}
				if (removeComponent)
				{
					DestroyImmediate(item, true);
				}
			}
			
			// Does it have the name we're looking for?
			if (onlyWithName != "")
			{
				if (searchItem.name != onlyWithName)
				{
					continue;
				}
				else if (removeIfPrefabChild)
				{
					// TODO:UNITY2018:nestedprefab:confirm//old
					// Obsolete: Use GetOutermostPrefabInstanceRoot if source is a Prefab instance or source.transform.root.gameObject if source is a Prefab Asset object.
					// GameObject prefabParent = PrefabUtility.FindPrefabRoot(searchItem);
					// TODO:UNITY2018:nestedprefab:confirm//new
					GameObject prefabParent = searchItem.transform.root.gameObject;
					if (prefabParent != null)
					{
						DestroyImmediate(searchItem, true);
						
						
						// Add parent to matches in place of the item
						matches.Add(prefabParent);
					}
					continue;
				}
			}
			
			matches.Add(searchItem);
		}
		
		results = matches.ToArray();
		
		if (displayRootObjects)
		{
			for (int i = 0; i < results.Length; i++)
			{
				while (results[i].transform.parent != null)
				{
					results[i] = results[i].transform.parent.gameObject;
				}
			}
		}
		
		if (removeComponent || removeIfPrefabChild)
		{
			AssetDatabase.SaveAssets();
		}
		
		removeComponent = false;
	}
	
	public void OnWizardUpdate()
	{
		helpString = "1a. Select all the objects you want to search (in the scene and/or project prefabs)." + 
					"\n1b. OR select the 'search asset library' checkbox to search all prefabs in the project." +
					"\n2. Enter any filter criteria (fields left blank are ignored)." +
					"\n3. Check the \"find button\"." +
					"\n4. See the results in the results array below";
	
	}
	
	public void OnWizardCreate()
	{
	
	}
}
