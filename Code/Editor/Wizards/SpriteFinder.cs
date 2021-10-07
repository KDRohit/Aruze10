using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Finds all the GameObjects in the project assets that has a UISprite component with a given sprite name.
*/
public class SpriteFinder : FinderBase
{
	public UIAtlas optionalAtlas = null;		// Only consider sprites using the given atlas if provided
	public string spriteName = "";				// Only consider objects using the given sprite name. If blank, then optionalAtlas is required to find all sprites that use that atlas.
	public GameObject[] results;				// The array of results from the most recent find.
												// Put here instead of in FinderBase simply so it comes after the input properties in the inspector.
													
	[MenuItem ("Zynga/Wizards/Find Stuff/Find Sprites and Atlases")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<SpriteFinder>("Find Sprites and Atlases", "Close", "Find");
	}
	
	protected override bool isValidInput
	{
		get
		{
			if (spriteName.Trim() == "" && optionalAtlas == null)
			{
				EditorUtility.DisplayDialog("Invalid Parameters", "You must specify either an atlas or a sprite name, or both.", "Oops, sorry.");
				
				return false;
			}
			return true;
		}
	}

	protected override void filterResults(List<GameObject> searchSpace)
	{
		// Make sure we're using the final actual atlas instead of references.
		optionalAtlas = getFinalAtlas(optionalAtlas);
		
		spriteName = spriteName.Trim();

		List<GameObject> matches = new List<GameObject>();
		
		foreach (GameObject searchItem in searchSpace)
		{			
			// Does it have the UISprite component?
			UISprite sprite = searchItem.GetComponent<UISprite>();
			UIImageButton imageButton = searchItem.GetComponent<UIImageButton>();
			
			if (sprite == null && imageButton == null)
			{
				continue;
			}
			
			// If an atlas is specified, does this srite use that atlas?
			if (sprite != null && optionalAtlas != null && optionalAtlas != getFinalAtlas(sprite.atlas))
			{
				continue;
			}

			// Does it have the sprite name we're looking for?
			if (sprite != null && spriteName != "" && sprite.spriteName != spriteName)
			{
				continue;
			}
			
			if (sprite == null && imageButton != null)
			{
				if (imageButton.normalSprite != spriteName &&
				imageButton.hoverSprite != spriteName &&
				imageButton.pressedSprite != spriteName &&
				imageButton.disabledSprite != spriteName)
				{
					continue;
				}
				if (imageButton.target == null)
				{
					continue;
				}
				else if (getFinalAtlas(imageButton.target.atlas) != optionalAtlas)
				{
					continue;
				}
			}
			
			matches.Add(searchItem);

		}

		results = prepareSearchResultsDisplay(matches);
	}
	
	public static UIAtlas getFinalAtlas(UIAtlas atlas)
	{
		while (atlas != null && atlas.replacement != null)
		{
			atlas = atlas.replacement;
		}
		return atlas;
	}
}