using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Finds all the GameObjects in the project assets that has a UILabel component with a given atlas.
*/
public class UILabelFinder : FinderBase
{
	public UIAtlas optionalAtlas = null;		// Only consider sprites using the given atlas if provided
	public GameObject[] results;				// The array of results from the most recent find.
												// Put here instead of in FinderBase simply so it comes after the input properties in the inspector.
	
	[MenuItem ("Zynga/Wizards/Find Stuff/Find UILabels and Atlases")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<UILabelFinder>("Find UILabels and Atlases", "Close", "Find");
	}
	
	protected override bool isValidInput
	{
		get
		{
			// if (optionalAtlas == null)
			// {
			// 	EditorUtility.DisplayDialog("Invalid Parameters", "You must specify an atlas.", "Oops, sorry.");
			//
			// 	return false;
			// }
			return true;
		}
	}

	protected override void filterResults(List<GameObject> searchSpace)
	{
		// Make sure we're using the final actual atlas instead of references.
		optionalAtlas = getFinalAtlas(optionalAtlas);
		
		List<GameObject> matches = new List<GameObject>();
		
		foreach (GameObject searchItem in searchSpace)
		{			
			// Does it have the UILabel component?
			UILabel label = searchItem.GetComponent<UILabel>();
			if (label == null)
			{
				continue;
			}
			
			// If an atlas is specified, does this srite use that atlas?
			if (optionalAtlas != null && label.font != null && label.font.atlas != null && optionalAtlas != getFinalAtlas(label.font.atlas))
			{
				continue;
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