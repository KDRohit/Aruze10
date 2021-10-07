using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Finds all the GameObjects in the project assets that has a TMPro TextContainer component.
*/
public class TMProTextContainerFinder : FinderBase
{
	public bool addObjectsToSelection = false;

	public GameObject[] results;				// The array of results from the most recent find.
												// Put here instead of in FinderBase simply so it comes after the input properties in the inspector.

	[MenuItem ("Zynga/Wizards/Find Stuff/Find TextMeshPro TextContainers")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<TMProTextContainerFinder>("Find TextMeshPro TextContainers", "Close", "Find");
	}

	protected override void filterResults(List<GameObject> searchSpace)
	{
		List<GameObject> matches = new List<GameObject>();

		foreach (GameObject searchItem in searchSpace)
		{
			// Does it have the TMPro TextContainer component?
			TextContainer textContainer = searchItem.GetComponent<TextContainer>();
			if (textContainer == null)
			{
				continue;
			}

			matches.Add(searchItem);
		}

		if (!searchGameObject)
		{
			results = prepareSearchResultsDisplay(matches);
		}
		else
		{
			results = matches.ToArray();
		}

		if (results.Length > 0)
		{
			if (addObjectsToSelection)
			{
				Selection.objects = results;
				helpString = "Objects selected! Use \"Zynga/Assets/Remove All TextMeshPro TextContainer components\" to remove TextContainer.";
			}
			else
			{
				helpString = "Select items and use \"Zynga/Assets/Remove All TextMeshPro TextContainer components\" to remove TextContainer.";
			}
		}
		else
		{
			helpString = "Have fun storming the castle!";
		}
	}
}
