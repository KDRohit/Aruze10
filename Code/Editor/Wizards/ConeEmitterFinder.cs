using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Finds all the GameObjects in the project assets that has a UISprite component with a given sprite name.
*/
public class ConeEmitterFinder : FinderBase
{
	public GameObject[] results;				// The array of results from the most recent find.
												// Put here instead of in FinderBase simply so it comes after the input properties in the inspector.
	
	[MenuItem ("Zynga/Wizards/Find Stuff/Find Cone Emitters")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<ConeEmitterFinder>("Find Cone Emitters", "Close", "Find");
	}
	
	protected override void filterResults(List<GameObject> searchSpace)
	{
		List<GameObject> matches = new List<GameObject>();
		
		foreach (GameObject searchItem in searchSpace)
		{			
			ParticleSystem ps = searchItem.GetComponent<ParticleSystem>();
			if (ps == null)
			{
				continue;
			}

			if (ps.shape.shapeType != ParticleSystemShapeType.Cone && ps.shape.shapeType != ParticleSystemShapeType.ConeVolume)
			{
				continue;
			}

			if (ps.shape.angle != 90.0f)
			{
				continue;
			}
						
			matches.Add(searchItem);
		}

		results = prepareSearchResultsDisplay(matches);
	}
}