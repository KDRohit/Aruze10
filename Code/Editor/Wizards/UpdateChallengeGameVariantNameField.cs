using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class UpdateChallengeGameVariantNameField 
{
	[MenuItem("Zynga/Assets/Convert Challenge Game Variant Name Field to Array")]
	public static void updateChallengeGameVariantNameFieldToList()
	{
		if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
		{
			Debug.Log("UpdateChallengeGameVariantNameField.updateChallengeGameVariantNameFieldToList() - Applying variant name field change to selected prefabs! START");
			// apply only to the currently selected objects
			convertVariantNameOfModularChallengeGameVariantToArray(new List<GameObject>(Selection.gameObjects), false);
		}
		else
		{
			Debug.Log("UpdateChallengeGameVariantNameField.updateChallengeGameVariantNameFieldToList() - Applying variant name field change to all prefabs! START");
			// do it across the whole project
			List<GameObject> allPrefabs = CommonEditor.gatherPrefabs("Assets/");
			convertVariantNameOfModularChallengeGameVariantToArray(allPrefabs, true);
		}		
		
		Debug.Log("UpdateChallengeGameVariantNameField.updateChallengeGameVariantNameFieldToList() - DONE");
	}
	
	private static void convertVariantNameOfModularChallengeGameVariantToArray(List<GameObject> objectList, bool isListOfPrefabs)
	{
		// Search all prefabs for ones with the component
		foreach (GameObject searchItem in objectList)
		{
			bool isPrefabChanged = false;
			string challengeVariantsTouchedNameList = "";

			GameObject prefabRoot;
			GameObject prefabParent;
			if (isListOfPrefabs)
			{
				prefabRoot = searchItem;
				prefabParent = searchItem;
			}
			else
			{
				// Need to figure out what the prefab is, because we might be selecting an object that isn't the root of the prefab
				// TODO:UNITY2018:nestedprefab:fix
				prefabRoot = PrefabUtility.FindPrefabRoot(searchItem);
				prefabParent = PrefabUtility.GetPrefabParent(prefabRoot) as GameObject;
				if (prefabParent == null)
				{
					// This is still a prefab selected in the Project window
					prefabParent = prefabRoot;
				}
			}
		
			// Does it have the component we're looking for?
			ModularChallengeGameVariant[] challengeVariantArray = prefabParent.GetComponentsInChildren<ModularChallengeGameVariant>(true);
			foreach (ModularChallengeGameVariant challengeVariant in challengeVariantArray)
			{
				if (challengeVariant != null)
				{
					// Leaving this commented out since it wouldn't compile anymore, 
					// But I want to leave this file in existance so if someone else needs to do something similair there is a reference.
					/*if (!string.IsNullOrEmpty(challengeVariant.variantGameName))
					{
						challengeVariant.variantGameNames = new string[1];
						challengeVariant.variantGameNames[0] = challengeVariant.variantGameName;
						challengeVariantsTouchedNameList += challengeVariant.gameObject.name + ", ";
						isPrefabChanged = true;
					}*/
				}
			}

			if (isPrefabChanged)
			{
				string assetPath = UnityEditor.AssetDatabase.GetAssetPath(prefabParent);

				// Clone the original so we can save out a new modified version
				GameObject parentClone = Object.Instantiate(prefabParent) as GameObject;

				Debug.Log("UpdateChallengeGameVariantNameField.updateChallengeGameVariantNameFieldToList() - Attempting to save changes to: challengeVariantsTouchedNameList = " + challengeVariantsTouchedNameList
					+ "; to prefabParent.name = " + prefabParent.name
					+ "; assetPath = " + assetPath);

				// TODO:UNITY2018:nestedprefab:confirm
				GameObject newPrefabObject = UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(parentClone, assetPath, UnityEditor.InteractionMode.AutomatedAction);
				// Parent clone is now saved as the prefab so we can destroy it.
				Object.DestroyImmediate(parentClone);

				if (!isListOfPrefabs && prefabParent != prefabRoot)
				{
					// We need to hook our currently selected prefab back up to the selected one
					// TODO:UNITY2018:nestedprefab:fix
					PrefabUtility.ConnectGameObjectToPrefab(prefabRoot, newPrefabObject);
				}
			}
		}
	}
}
