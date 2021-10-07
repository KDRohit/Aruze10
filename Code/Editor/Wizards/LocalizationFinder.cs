using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Finds all the GameObjects in the project assets that has a UILabelStaticText component with a given localization key.
*/
public class LocalizationFinder : FinderBase
{
	public string localizationKey = "";				// Only consider objects using the given sprite name
	public bool doExactCaseSearch = false;			// Whether to do an exact case search.
	
	public GameObject[] gameObjectResults;			// The array of results from the most recent find
	public Object[] codeResults;					// The array of results from the most recent find
	
	[MenuItem ("Zynga/Wizards/Find Stuff/Find Localizations")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<LocalizationFinder>("Find Localizations", "Close", "Find");
	}
	
	protected override void filterResults(List<GameObject> searchSpace)
	{	
		List<GameObject> matches = new List<GameObject>();		

		if (!doExactCaseSearch)
		{
			localizationKey = localizationKey.ToLower();
		}
		
		foreach (GameObject searchItem in searchSpace)
		{
			// Does it have the UILabelStaticText component?
			UILabelStaticText staticText = searchItem.GetComponent<UILabelStaticText>();
			if (staticText == null)
			{
				continue;
			}

			string staticLocKey = staticText.localizationKey;
			if (!doExactCaseSearch)
			{
				staticLocKey = staticLocKey.ToLower();
			}
			
			// Does it have the localization key we're looking for?
			if (staticLocKey != localizationKey)
			{
				continue;
			}

			matches.Add(searchItem);
		}
		
		gameObjectResults = prepareSearchResultsDisplay(matches);

		List<Object> codeMatches = new List<Object>();

		// Also search source code for references.
		string quotedLocalizationKey = "\"" + localizationKey + "\"";
		string[] codePaths = new string[]
		{
			"Assets/Code/",
			"Assets/Plugins/"
		};
		
		foreach (string path in codePaths)
		{
			foreach (TextAsset code in CommonEditor.gatherAssets<TextAsset>(path))
			{
				string codeText = code.text;
				if (!doExactCaseSearch)
				{
					codeText = codeText.ToLower();
				}

				if (codeText.Contains(quotedLocalizationKey))
				{
					codeMatches.Add(code);
				}
			}
		}

		codeResults = codeMatches.ToArray();
	}
}