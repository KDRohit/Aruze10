using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Finds all the GameObjects in the project assets that has a UISprite component with a given sprite name.
*/
public class ListSpritesAndAtlases : FinderBase
{
	public bool searchForSpriteUsages; // looks for all prefabs that are using that sprite
	public UISprite[] spriteResults;
	public UIAtlas[] atlasResults;
	public GameObject[] spriteUsages;
													
	[MenuItem ("Zynga/Wizards/Find Stuff/List Sprites and Atlases on Object")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<ListSpritesAndAtlases>("List Sprites and Atlases on Object", "Close", "Find");
	}
	
	protected override bool isValidInput
	{
		get
		{
			if (searchGameObject == null)
			{
				EditorUtility.DisplayDialog("Invalid Parameters", "You must specify a game object.", "You're right, I had one job.");
				
				return false;
			}
			return true;
		}
	}

	public override void OnWizardOtherButton()
	{
		if (!isValidInput)
		{
			return;
		}

		List<GameObject> globalSearchSpace = new List<GameObject>();
		List<GameObject> searchSpace = new List<GameObject>();

		// Gather all the objects in the assets and/or scene.
		if (!FinderObjectCache.shouldSpaceCache || (FinderObjectCache.shouldSpaceCache && FinderObjectCache.searchSpaceCache == null))
		{

			if (searchGameObject != null)
			{
				searchScene = searchForSpriteUsages;
				searchAssets = searchForSpriteUsages;
				searchSpace.Add(searchGameObject);

				foreach (Transform child in searchGameObject.GetComponentsInChildren<Transform>(true))
				{
					searchSpace.Add(child.gameObject);
				}
			}

			if (searchAssets)
			{
				// this takes about 3 minutes to complete
				System.DateTime startTime =  System.DateTime.Now;

				int numDumped = 0;
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
						globalSearchSpace.Add(child);
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
						globalSearchSpace.Add(child.gameObject);
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

		if (searchForSpriteUsages)
		{
			searchAllUsages(globalSearchSpace);
		}
	}

	protected override void filterResults(List<GameObject> searchSpace)
	{
		List<UISprite> matches = new List<UISprite>();
		List<UIAtlas> atlases = new List<UIAtlas>();
		
		foreach (GameObject searchItem in searchSpace)
		{			
			// Does it have the UISprite component?
			UISprite sprite = searchItem.GetComponent<UISprite>();
			UIImageButton imageButton = searchItem.GetComponent<UIImageButton>();
			
			if (sprite == null && imageButton == null)
			{
				continue;
			}
			
			// If a sprite is find, add it and the atlas
			if (sprite != null)
			{
				UIAtlas atlas = sprite.atlas;
				if (!atlases.Contains(atlas))
				{
					atlases.Add(atlas);
				}
				matches.Add(sprite);
			}
			
			if (sprite == null && imageButton != null)
			{
				if (imageButton.target == null)
				{
					continue;
				}
				else
				{
					UIAtlas imageAtlas = imageButton.target.atlas;
					if (!atlases.Contains(imageAtlas))
					{
						atlases.Add(imageAtlas);
					}
				}
			}
		}

		prepareNewResults(matches, atlases);
	}

	protected void searchAllUsages(List<GameObject> searchSpace)
	{
		List<GameObject> matches = new List<GameObject>();
		List<string> spriteNames = new List<string>();

		foreach (GameObject searchItem in searchSpace)
		{
			foreach (UISprite asset in spriteResults)
			{
				// we already got it, dont bother
				if (matches.Contains(getParent(searchItem)))
				{
					break;
				}

				// Does it have the UISprite component?
				UISprite sprite = searchItem.GetComponent<UISprite>();
				UIImageButton imageButton = searchItem.GetComponent<UIImageButton>();

				if (sprite == null && imageButton == null)
				{
					continue;
				}
				// Does it have the sprite name we're looking for?
				if (sprite != null && sprite.spriteName != asset.spriteName)
				{
					continue;
				}

				if (sprite == null && imageButton != null)
				{
					if (imageButton.normalSprite != asset.spriteName &&
						imageButton.hoverSprite != asset.spriteName &&
						imageButton.pressedSprite != asset.spriteName &&
						imageButton.disabledSprite != asset.spriteName)
					{
						continue;
					}
					if (imageButton.target == null)
					{
						continue;
					}
				}

				matches.Add(getParent(searchItem));
			}
		}

		spriteUsages = matches.ToArray();
	}

	private GameObject getParent(GameObject searchItem)
	{
		while (searchItem != null && searchItem.transform.parent != null)
		{
			searchItem = searchItem.transform.parent.gameObject;
		}

		return searchItem;
	}

	// Prepares the results of searching for objects for use in a wizard.
	protected void prepareNewResults(List<UISprite> matches, List<UIAtlas> atlases)
	{
		atlasResults = atlases.ToArray();
		spriteResults = matches.ToArray();
	}
}
