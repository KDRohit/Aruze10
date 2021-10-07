using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Finds renderers with some mesh and replaces their material(s).
*/
public class MaterialReplacer : ScriptableWizard
{
	public bool find = false;					///< Toggle true to force a search next frame, toggled false automatically afterwards
	public bool searchAssetLibrary = false;		///< Do we search the current selection or the entire library?
	public bool performMatReplacement = false;	///< Toggle true to perform material replacement
	public bool performMeshReplacement = false;	///< Toggle true to perform mesh replacement
	public Mesh[] targetMeshes;					///< Required meshes to find the renderer of
	public Material[] replaceMaterials;			///< Material to put on the renderer
	public Mesh replaceMesh;					///< Mesh to put on the renderer
	
	public GameObject[] results;				///< The array of results from the most recent find
	
	[MenuItem ("Zynga/Wizards/Find Stuff/Find Mesh, Replace Mats")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<MaterialReplacer>("Find Materials", "Close");
	}
	
	public void OnWizardUpdate()
	{
		helpString = "1a. Select all the objects you want to search (in the scene and/or project prefabs)." + 
					"\n1b. OR select the 'search asset library' checkbox to search all prefabs in the project." +
					"\n2. Add the target mesh to look for." +
					"\n3. Check the \"find\" checkbox (it will uncheck when done)." +
					"\n4. See the results in the results array below";
	
		if (find)
		{
			find = false;
			
			List<GameObject> searchSpace = new List<GameObject>();
			
			if (searchAssetLibrary)
			{
				// Search the asset library for all instances
				List<GameObject> allPrefabs = CommonEditor.gatherPrefabs("Assets/");
				foreach (GameObject prefab in allPrefabs)
				{
					List<GameObject> allChildren = CommonGameObject.findAllChildren(prefab, true);
					foreach (GameObject child in allChildren)
					{
						searchSpace.Add(child);
					}
				}
			}
			else
			{
				// Search the current selection for all instances
				Object[] selection = Selection.GetFiltered(typeof(GameObject), SelectionMode.TopLevel | SelectionMode.DeepAssets);
				foreach (Object go in selection)
				{
					List<GameObject> allChildren = CommonGameObject.findAllChildren((GameObject)go, true);
					foreach (GameObject child in allChildren)
					{
						searchSpace.Add(child);
					}
				}
			}
			
			List<GameObject> matches = new List<GameObject>();
			
			// Search selection for matches
			foreach (GameObject searchItem in searchSpace)
			{
				MeshRenderer renderer = searchItem.GetComponent<MeshRenderer>();
				if (renderer != null)
				{
					MeshFilter filter = searchItem.GetComponent<MeshFilter>();
					if (filter != null)
					{
						// Check each target mesh for a match
						foreach (Mesh mesh in targetMeshes)
						{
							if (filter.sharedMesh == mesh)
							{
								matches.Add(searchItem);
								
								// Perform material replacement (only if that was checked)
								if (performMatReplacement)
								{
									renderer.sharedMaterials = replaceMaterials;
									EditorUtility.SetDirty(searchItem);
								}
								
								// Perform mesh replacement (only if that was checked)
								if (performMeshReplacement)
								{
									filter.sharedMesh = replaceMesh;
									EditorUtility.SetDirty(searchItem);
								}
								break;
							}
						}
					}
				}
			}
			
			results = matches.ToArray();
			performMatReplacement = false;
			performMeshReplacement = false;
		}
	}
	
	public void OnWizardCreate()
	{
	
	}
}