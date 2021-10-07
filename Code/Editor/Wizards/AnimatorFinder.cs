using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Finds all the GameObjects in the project assets that has an Animator component with a given animation controller.
*/
public class AnimatorFinder : FinderBase
{
	public RuntimeAnimatorController animationController = null;	// Only consider objects using the given animation controller
	public GameObject[] results;				// The array of results from the most recent find.
												// Put here instead of in FinderBase simply so it comes after the input properties in the inspector.
	
	[MenuItem ("Zynga/Wizards/Find Stuff/Find Animation Controller")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<AnimatorFinder>("Find Animation Controller", "Close", "Find");
	}
	
	protected override void filterResults(List<GameObject> searchSpace)
	{	
		List<GameObject> matches = new List<GameObject>();		
		foreach (GameObject searchItem in searchSpace)
		{			
			// Does it have the UISprite component?
			Animator anim = searchItem.GetComponent<Animator>();
			if (anim == null)
			{
				continue;
			}
			
			// Does it have the animation controller we're looking for?
			if (anim.runtimeAnimatorController != animationController)
			{
				continue;
			}
			
			matches.Add(searchItem);
		}
		
		results = prepareSearchResultsDisplay(matches);
	}
}