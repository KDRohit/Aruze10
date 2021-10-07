using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Finds and counts the sprite usage of an UIAtlas
**/
public class AtlasAudit : FinderBase
{
	public List<UIAtlas> atlasList = new List<UIAtlas>();		// Only consider sprites using the given atlases, leave empty to do all atlases
	private string spriteName = "";				// Only consider objects using the given sprite name. If blank, then optionalAtlas is required to find all sprites that use that atlas.
	public GameObject[] results;				// The array of results from the most recent find.
												// Put here instead of in FinderBase simply so it comes after the input properties in the inspector.
	private  Dictionary<string, int> countData;
													
	[MenuItem ("Zynga/Wizards/Find Stuff/Atlas Audit Report")]
	static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<AtlasAudit>("Atlas Audit", "Close", "Start");
	}
	
	protected override bool isValidInput
	{
		get
		{
			return true;
		}
	}

	protected override void filterResults(List<GameObject> searchSpace)
	{
		// if the list is empty add all ui atlases found
		if (atlasList.Count == 0)
		{
			foreach (GameObject searchItem in searchSpace)
			{	
				UIAtlas atlas = searchItem.GetComponent<UIAtlas>();
				if (atlas != null && atlas == getFinalAtlas(atlas))
				{
					atlasList.Add(atlas);
				}
			}		
		}

		string report = "";
		foreach(UIAtlas curAtlas in atlasList)
		{
			countData = new Dictionary<string, int>();

			Debug.LogError("adding sprites");
			// maake a dict with all the sprite names
			foreach (UIAtlas.Sprite dSprite in curAtlas.spriteList)
			{
				countData.Add(dSprite.name, 0);
			}

			// Make sure we're using the final actual atlas instead of references.
			
			List<GameObject> matches = new List<GameObject>();
			
			foreach (GameObject searchItem in searchSpace)
			{			
				// Does it have the UISprite component?
				UISprite sprite = searchItem.GetComponent<UISprite>();
				UIImageButton imageButton = searchItem.GetComponent<UIImageButton>();
				UIImageButton[] imageButtonList = searchItem.GetComponents<UIImageButton>();

				if (imageButtonList.Length > 1)
					Debug.LogError("doh " + searchItem.name + "has more than one UIImageButton");
				
				if (sprite == null && imageButton == null)
				{
					continue;
				}
				
				// If an atlas is specified, does this srite use that atlas?
				if (sprite != null && curAtlas != getFinalAtlas(sprite.atlas))
				{
					continue;
				}
				
				if (sprite == null &&
					imageButton != null)
				{
					if 
					(
						!countData.ContainsKey(imageButton.normalSprite) &&
						!countData.ContainsKey(imageButton.hoverSprite) &&
						!countData.ContainsKey(imageButton.pressedSprite) &&
						!countData.ContainsKey(imageButton.disabledSprite)
					)
					{
						continue;
					}
					if (imageButton.target == null)
					{
						continue;
					}
					else if (getFinalAtlas(imageButton.target.atlas) != curAtlas)
					{
						continue;
					}					
				}

				
				matches.Add(searchItem);

				if (sprite != null)
				{
					if (countData.ContainsKey(sprite.spriteName))
					{
						countData[sprite.spriteName]++;
					}
					else
					{
						Debug.LogError("Prefab " + searchItem.name + " contains sprite reference " + sprite.spriteName + " that does not exist in Atlas! ");
					}
				}
				else
				{
					if (countData.ContainsKey(imageButton.normalSprite))
					{
						countData[imageButton.normalSprite]++;
					}
					if (countData.ContainsKey(imageButton.hoverSprite))
					{
						countData[imageButton.hoverSprite]++;
					}			
					if (countData.ContainsKey(imageButton.pressedSprite))
					{
						countData[imageButton.pressedSprite]++;
					}
					if (countData.ContainsKey(imageButton.disabledSprite))
					{
						countData[imageButton.disabledSprite]++;
					}
				}
			}

			results = prepareSearchResultsDisplay(matches);

			report += "*************** Results for " + curAtlas.name + "\n";

			foreach (UIAtlas.Sprite dSprite in curAtlas.spriteList)
			{
				if (countData[dSprite.name] == 0)
				{
					report += "Unused Sprite Texture found : " + dSprite.name + "\n";
				}
			}
			foreach (UIAtlas.Sprite dSprite in curAtlas.spriteList)
			{
				if (countData[dSprite.name] > 0)
				{
					report += dSprite.name + "Is used  : " + countData[dSprite.name] + " times." + "\n";
				}
			}

			report += "\n";
		}

		EditorGUIUtility.systemCopyBuffer = report;   // allow user to paste into text editor
		Debug.Log(report);
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