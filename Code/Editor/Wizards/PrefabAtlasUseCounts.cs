using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Finds all the sprites used in a prefab and gives a report with full paths so you can find them
*/
public class PrefabAtlasUseCounts : ScriptableWizard
{
	[SerializeField] private GameObject searchItem;

	[MenuItem("Zynga/Wizards/Find Stuff/Find Prefab Sprite and Atlas usage")]
	private static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<PrefabAtlasUseCounts>("Find Prefab Sprite and Atlas usage", "Close", "Search");
	}

	private void OnWizardUpdate()
	{
		if (searchItem == null)
		{
			this.helpString = "Please select an object for an atlas use report";
		}
	}

	public void OnWizardCreate()
	{
		// Called when Close is clicked.
	}

	private void OnWizardOtherButton()
	{
		int findCount = 0;

		if (searchItem == null)
		{
			EditorUtility.DisplayDialog("We have a problem.", this.helpString, "OK");
			return;
		}

		string report = "Prefab sprite use report for " + searchItem.name + "\n";

		List<GameObject> children = CommonGameObject.findAllChildren(searchItem, true);
		
		Dictionary<string, int> countData = new Dictionary<string, int>();

		foreach (GameObject child in children)
		{
			// get a list of all the UI sprites
			UISprite[] sprites = child.GetComponents<UISprite>();
			UIImageButton[] imageButtonList = child.GetComponents<UIImageButton>();			

			for (int i = 0; i < sprites.Length; i++)
			{
				findCount++;
				UISprite sprite = sprites[i];

				if (sprite.atlas == null)
				{
					report += "GameObject(UISprite) : " + CommonGameObject.getObjectPath(child) + " uses sprite " + sprite.spriteName + " in atlas NULL" + "\n";
					continue;
				}

				string atlasName = SpriteFinder.getFinalAtlas(sprite.atlas).name;
					
				if (!countData.ContainsKey(atlasName))
				{
					countData.Add(atlasName, 0);
				}			
				countData[atlasName]++;

				report += "GameObject(UISprite) : " + CommonGameObject.getObjectPath(child) + " uses sprite " + sprite.spriteName + " in atlas " + atlasName + "\n";
			}

			// check UIImages
			for (int i = 0; i < imageButtonList.Length; i++)
			{
				UIImageButton button = imageButtonList[i];

				UISprite sprite = button.target;

				if (sprite != null)
				{
					findCount += 4;
					string atlasName = SpriteFinder.getFinalAtlas(sprite.atlas).name;
					if (!countData.ContainsKey(atlasName))
					{
						countData.Add(atlasName, 0);
					}			
					countData[atlasName]++;

					report += "GameObject(UIImageButton) at " + CommonGameObject.getObjectPath(child) + " uses UIImageButton normalSprite " + button.normalSprite + " in atlas " + atlasName + "\n";
					report += "GameObject(UIImageButton) at " + CommonGameObject.getObjectPath(child) + " uses UIImageButton hoverSprite " + button.hoverSprite + " in atlas " + atlasName + "\n";
					report += "GameObject(UIImageButton) at " + CommonGameObject.getObjectPath(child) + " uses UIImageButton pressedSprite " + button.pressedSprite + " in atlas " + atlasName + "\n";
					report += "GameObject(UIImageButton) at " + CommonGameObject.getObjectPath(child) + " uses UIImageButton disabledSprite " + button.disabledSprite + " in atlas " + atlasName + "\n";
				}			
			}			
		}

		string atlasUseReport = "";
		foreach(KeyValuePair<string, int> entry in countData)
		{
			atlasUseReport += "Atlas " + entry.Key + " is referenced " + entry.Value + " times.\n";
		}

		report = atlasUseReport + report;

		EditorGUIUtility.systemCopyBuffer = report;   // allow user to paste into text editor
				
		EditorUtility.DisplayDialog("Done Searching report is in clipboard", string.Format("Found {0} sprites.", findCount), "OK");
	}
} 
