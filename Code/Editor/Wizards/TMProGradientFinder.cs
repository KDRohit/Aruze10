using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Finds all the GameObjects in the project assets that has a TextMeshPro component with vertex gradient enabled.
*/
public class TMProGradientFinder : FinderBase
{
	public GameObject[] results;				// The array of results from the most recent find.
												// Put here instead of in FinderBase simply so it comes after the input properties in the inspector.
	
	[MenuItem ("Zynga/Wizards/Find Stuff/Find TextMeshPro Gradients")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<TMProGradientFinder>("Find TextMeshPro Gradients", "Close", "Find");
	}
	
	protected override void filterResults(List<GameObject> searchSpace)
	{	
		List<GameObject> matches = new List<GameObject>();

		foreach (GameObject searchItem in searchSpace)
		{			
			// Does it have the TextMeshPro component?
			TextMeshPro label = searchItem.GetComponent<TextMeshPro>();
			if (label == null)
			{
				continue;
			}
			
			// Does it have vertex gradient enabled?
			if (!label.enableVertexGradient ||
				label.colorGradient.bottomLeft.a > 0.0f ||
				label.colorGradient.bottomRight.a > 0.0f
				)
			{
				continue;
			}
			
			matches.Add(searchItem);
		}

		results = prepareSearchResultsDisplay(matches);
	}
}