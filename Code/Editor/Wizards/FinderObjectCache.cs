using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Creates or destroys a cache that can be used by all FinderBase derived classes.
*/
public class FinderObjectCache : FinderBase
{
	public static bool shouldSpaceCache = false;
	public static List<GameObject> searchSpaceCache = null;

	[MenuItem ("Zynga/Wizards/Find Stuff/Manage Finder Cache")]
	static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<FinderObjectCache>("Finder Cache Manager", "Close", getButtonName());
	}

	static string getButtonName()
	{
		string buttonName = "Create Cache";
		if (searchSpaceCache != null)
			buttonName = "Clear Cache";

		return buttonName;
	}
	
	protected override bool isValidInput
	{
		get
		{
			return true;
		}
	}

	new protected virtual void OnWizardUpdate() 
	{
		helpString = "Manage cache of game objects used by Zynga/Wizards/Find Stuff. Building a cache takes about 3 minutes. Each search will then use the cache greatly reducing search times.\n";

		if (searchSpaceCache != null)
		{
			helpString += "Cache contains " + searchSpaceCache.Count + " objects.";
		}
		else
		{
			helpString += "Cache is empty.";
		}
	}

	public override void OnWizardOtherButton()
	{
		shouldSpaceCache = !shouldSpaceCache;

		searchSpaceCache = null;

		if (shouldSpaceCache)
		{
			base.OnWizardOtherButton();
		}

		otherButtonName = getButtonName();

		OnWizardUpdate();
	}

	protected override void filterResults(List<GameObject> searchSpace)
	{
		searchSpaceCache = searchSpace;
		shouldSpaceCache = true;
	}
}