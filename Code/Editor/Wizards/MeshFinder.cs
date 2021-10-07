using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Finds all the GameObjects in the project assets that has a MeshFilter component with a given mesh.
*/
public class MeshFinder : FinderBase
{
	public Mesh mesh = null;
	public GameObject[] results;				// The array of results from the most recent find.
												// Put here instead of in FinderBase simply so it comes after the input properties in the inspector.
	
	[MenuItem ("Zynga/Wizards/Find Stuff/Find Meshes")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<MeshFinder>("Find Meshes", "Close", "Find");
	}
	
	protected override void filterResults(List<GameObject> searchSpace)
	{	
		List<GameObject> matches = new List<GameObject>();

		foreach (GameObject searchItem in searchSpace)
		{			
			// Does it have the MeshFilter component?
			MeshFilter filter = searchItem.GetComponent<MeshFilter>();
			if (filter == null)
			{
				continue;
			}
			
			// Does it have the mesh we're looking for?
			if (filter.sharedMesh != mesh)
			{
				continue;
			}
			
			matches.Add(searchItem);
		}

		results = prepareSearchResultsDisplay(matches);
	}
}