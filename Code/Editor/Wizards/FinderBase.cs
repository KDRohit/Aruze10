using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/*
Base class for making a wizard to find any kind of objects with any custom filtering.
*/

public class FinderBase : ScriptableWizard
{
	public enum SearchPath
	{
		ALL,
		COMMON,
		GAMES,
		HIR,
		CURRENT_SELECTION
	}
	
	public SearchPath assetsPath = SearchPath.ALL;			// If searching assets, only search these assets.
	public string currentSelection; 						// If searching current project, MUST be directory
	public bool searchAssets = true;						// Whether to search assets.
	public bool searchScene = true;							// Whether to search scene objects.
	public bool ignoreGames = false;							// exclude ganes from any searches
	public GameObject searchGameObject = null; 				// Just search this gameobject, nothing else.

	// Must match the order of SearchPath enum.
	protected string[] searchPaths = new string[]
	{
		"Assets/Data/",
		"Assets/Data/Common/",
		"Assets/Data/Games/",
		"Assets/Data/HIR/",
		"" // Only populated if CURRENT_SELECTION is the search path
	};

	// Populates the current editor selection when wizard is launched
	public virtual void Awake() 
	{
		displayCurrentSelection();
	}

	// May be overridden to validate input before doing a search.
	protected virtual bool isValidInput
	{
		get { return true; }
	}

	protected virtual bool isSelectionValid
	{
		get { return AssetDatabase.IsValidFolder(currentSelection); }
	}
	
	// the Find button was clicked
	public virtual void OnWizardOtherButton()
	{
		if (!isValidInput)
		{
			return;
		}

		if (assetsPath == SearchPath.CURRENT_SELECTION && !isSelectionValid)
		{
			EditorUtility.DisplayDialog("Invalid selection!", "Current selection is not a valid folder!", "OK");
			return;
		}

		List<GameObject> searchSpace = new List<GameObject>();

		// Gather all the objects in the assets and/or scene.
		if (!FinderObjectCache.shouldSpaceCache || (FinderObjectCache.shouldSpaceCache && FinderObjectCache.searchSpaceCache == null))
		{

			if (searchGameObject != null)
			{
				searchScene = false;
				searchAssets = false;
				searchSpace.Add(searchGameObject);

				foreach (Transform child in searchGameObject.GetComponentsInChildren<Transform>(true))
				{
					searchSpace.Add(child.gameObject);
				}
			}

			// Allow for custom path via selection in project window 
			else if (assetsPath == SearchPath.CURRENT_SELECTION)
			{
				searchAssets = true;
				searchGameObject = null;
				searchPaths[(int)SearchPath.CURRENT_SELECTION] = currentSelection;
				assetsPath = SearchPath.CURRENT_SELECTION;
			}

			if (searchAssets)
			{
				// this takes about 3 minutes to complete
				System.DateTime startTime =  System.DateTime.Now;

				// Search the asset library for all instances 
				List<GameObject> allPrefabs = gatherPrefabs(assetsPath);
				foreach (GameObject prefab in allPrefabs)
				{
					if (ignoreGames && AssetDatabase.GetAssetPath(prefab).Contains(searchPaths[(int)SearchPath.GAMES]))
					{
						continue;
					}					
					List<GameObject> allChildren = CommonGameObject.findAllChildren(prefab, includeInactives:true);
					foreach (GameObject child in allChildren)
					{
						searchSpace.Add(child);
					}
				}

				double timeElapsed = (System.DateTime.Now - startTime).TotalSeconds;
				Debug.Log("Gathering game objects done, total size: " + searchSpace.Count + " time: " + timeElapsed);
			}
			
			if (searchScene)
			{
				// Also find objects in the scene.
				foreach (GameObject rootObject in CommonEditor.sceneRoots())
				{
					foreach (Transform child in rootObject.GetComponentsInChildren<Transform>(true))
					{
						searchSpace.Add(child.gameObject);
					}
				}
			}
		}
		else
		{
			searchSpace = FinderObjectCache.searchSpaceCache;
		}
		
		// Filter the results for display.
		filterResults(searchSpace);
	}
	
	// Each derived class can implement this function to filter the results based on custom input properties,
	// and return the final list of matches.
	protected virtual void filterResults(List<GameObject> searchSpace)
	{
	}
	
	/// <summary>
	/// Prepares the results of searching for objects for use in a wizard.  If match object is part of a prefab asset,
	/// list the prefab instead.  Duplicate prefabs in listing are removed from results.
	/// </summary>
	protected GameObject[] prepareSearchResultsDisplay(List<GameObject> matches)
	{
		List<GameObject> results = new List<GameObject>();
		HashSet<GameObject> prefabsFound = new HashSet<GameObject>();
		for (int i = 0; i < matches.Count; i++)
		{
			GameObject go = matches[i];
			if (go != null)
			{
				// If go is part of a prefab asset, list the prefab in the results.  If go is a non-nested instance in a
				// scene or not part of a prefab, list the actual game object.
				if (PrefabUtility.IsPartOfAnyPrefab(go))
				{
					bool isSceneInstance = PrefabUtility.IsPartOfNonAssetPrefabInstance(go);
					if (!isSceneInstance)
					{
						// This is a prefab asset (not an object in the scene). Find the root object.
						while (go.transform.parent != null)
						{
							go = go.transform.parent.gameObject;
						}
						// Dedupe prefabs from results list.
						if (prefabsFound.Contains(go))
						{
							continue;
						}
						prefabsFound.Add(go);
					}
				}
				results.Add(go);
			}
		}
		return results.ToArray();
	}

	//Populates the "Current Selection" field with user's currently clicked item in the project view
	protected void displayCurrentSelection()
	{
		Object selection = Selection.activeObject;
		if (selection != null) 
		{
			currentSelection = AssetDatabase.GetAssetPath(selection.GetInstanceID());

			if (string.IsNullOrEmpty(currentSelection))
			{
				currentSelection = "Not in the project view.";
			}
			else if (!isSelectionValid)
			{
				currentSelection = "Not a directory.";
			}
			Repaint();
		}
	}
	
	public void OnWizardUpdate()
	{
		helpString = "Have fun storming the castle!";
	}

	public void OnWizardCreate()
	{
	}

	public List<GameObject> gatherPrefabs(SearchPath searchPathType)
	{
		return CommonEditor.gatherPrefabs(searchPaths[(int)searchPathType]);
	}

	public void OnSelectionChange() 
	{
		displayCurrentSelection();	
	}
}
