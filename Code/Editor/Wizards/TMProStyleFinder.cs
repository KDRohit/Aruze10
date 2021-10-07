using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Finds all the GameObjects in the project assets that has a TextMeshPro component with a given style material.
*/
public class TMProStyleFinder : FinderBase
{
	public Material styleMaterial = null;
	public GameObject[] results;				// The array of results from the most recent find.
												// Put here instead of in FinderBase simply so it comes after the input properties in the inspector.
	
	[MenuItem ("Zynga/Wizards/Find Stuff/Find TextMeshPro Style Materials")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<TMProStyleFinder>("Find TextMeshPro Style Materials", "Close", "Find");
	}
	
	protected override void filterResults(List<GameObject> searchSpace)
	{	
		List<GameObject> matches = new List<GameObject>();

		foreach (GameObject searchItem in searchSpace)
		{			
			// Does it have the MeshFilter component?
			TextMeshPro tmPro = searchItem.GetComponent<TextMeshPro>();
			if (tmPro == null)
			{
				continue;
			}
			
			// Does it have the style material we're looking for?
			if (tmPro.fontSharedMaterial != styleMaterial)
			{
				continue;
			}
			
			matches.Add(searchItem);
		}

		results = prepareSearchResultsDisplay(matches);
	}
}