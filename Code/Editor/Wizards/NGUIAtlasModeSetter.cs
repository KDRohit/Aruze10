using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Finds all the NGUI UIAtlases in the project 
(This file duplicated from ObjectFinder.cs and modified)
*/
public class NGUIAtlasModeSetter : ScriptableWizard
{
	public bool searchAssetLibrary = false;		///< Do we search the current selection or the entire library?
	public string rootPath = "";				///< Only consider objects in this path. Empty means the whole asset library.
	public string onlyWithName = "";			///< Only consider objects with the given name
	public string onlyWithComponent = "UIAtlas";		///< Filter out all components but this named one
	public bool setModeToPixels = false;		///< Do we only want to see the roots (for easier double clicking)?
	public bool setModeToTexCoords = false;		///< Do we only want to see the roots (for easier double clicking)?

	public GameObject[] results;				///< The array of results from the most recent find
	public int numFoundAsPixels = 0;
	public int numFoundAsTexCoords = 0;
	public int numUpdated = 0;

	// 283

	[MenuItem ("Zynga/Wizards/Find Stuff/Find NGUI UIAtlases")]
	static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<NGUIAtlasModeSetter>("Find NGUI UIAtlases", "Close", "Find");
	}
	
	// find button clicked
	public void OnWizardOtherButton()
	{	
		if (setModeToPixels && setModeToTexCoords)
		{
			Debug.LogError("CANNOT SET TO BOTH PIXELS AND TEXCOORDS; CHOOSE JUST ONE");
			return;
		}

		List<GameObject> searchSpace = new List<GameObject>();
		
		if (searchAssetLibrary)
		{
			// Search the asset library for all instances
			if (!rootPath.FastEndsWith("/"))
			{
				rootPath += "/";
			}
			
			List<GameObject> allPrefabs = CommonEditor.gatherPrefabs("Assets/" + rootPath);
			foreach (GameObject prefab in allPrefabs)
			{
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
			// Does it have the component we're looking for?
			if (onlyWithComponent != "")
			{
				Component item = searchItem.GetComponent(onlyWithComponent);
				if (item == null)
				{
					continue;
				}
			}
			
			// Does it have the name we're looking for?
			if (onlyWithName != "")
			{
				if (searchItem.name != onlyWithName)
				{
					continue;
				}
			}
			
			matches.Add(searchItem);
		}
		
		results = matches.ToArray();
		

		// Gather some stats about coordinate modes
		numFoundAsPixels = 0;
		numFoundAsTexCoords = 0;
		foreach (GameObject obj in results)
		{
			UIAtlas atlas = obj.GetComponent<UIAtlas>();
			if (atlas != null)
			{
				if (atlas.coordinates == UIAtlas.Coordinates.Pixels)
				{
					numFoundAsPixels++;
				}

				if (atlas.coordinates == UIAtlas.Coordinates.TexCoords)
				{
					numFoundAsTexCoords++;
				}
			}
		}

		// And optionally set their coordinates modes...
		numUpdated = 0;
		if (setModeToPixels || setModeToTexCoords)
		{
			var newMode = setModeToPixels ? UIAtlas.Coordinates.Pixels : UIAtlas.Coordinates.TexCoords;
			foreach (GameObject obj in results)
			{
				UIAtlas atlas = obj.GetComponent<UIAtlas>();
				if (atlas != null)
				{
					if (atlas.coordinates != newMode)
					{
						atlas.allowPixelCoordinates = true;
						atlas.coordinates = newMode;
						atlas.allowPixelCoordinates = false;
						numUpdated++;
					}
					EditorUtility.SetDirty(atlas);
				}
			}

			// and save
			AssetDatabase.SaveAssets();
		}

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