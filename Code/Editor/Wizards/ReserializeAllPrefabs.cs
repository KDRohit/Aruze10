using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
loads all prefabs and saves them out, to get rid of 'induced' changes from added/deleted fields in component definitions 
showing up when Editor runs the game and Saves the Project.
also fixes up changes in material definitions.
*/
public class ReserializeAllPrefabs : ScriptableWizard
{
	[MenuItem ("Zynga/Editor Tools/Reserialize All Prefabs-linked Assets")] static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<ReserializeAllPrefabs>("Reserialize All Prefabs", "Close", "Reserialize & Save");
	}

	public void OnWizardOtherButton()
	{	
		ReserializeAllPrefabs.Run();
	}
	
	public void OnWizardUpdate()
	{
		helpString = "Load all prefabs under Assets/Data/ and prefab-linked assets, then save them out again.";
	}
	
	public void OnWizardCreate()
	{
	
	}

	static public void Run()
	{
		List<GameObject> allPrefabs = CommonEditor.gatherPrefabs("Assets/Data/");
		AssetDatabase.SaveAssets();
		Debug.Log("All Reserialized Assets Saved.");
	}
}
