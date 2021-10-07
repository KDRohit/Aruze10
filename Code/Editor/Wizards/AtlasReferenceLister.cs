using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Text;
using System.Collections.Generic;

/**
Finds all the GameObjects in the project assets that has a UISprite component with a given sprite name.
*/
public class AtlasReferenceLister : FinderBase
{
	//public UIAtlas optionalAtlas = null;		// Only consider sprites using the given atlas if provided
	//public string spriteName = "";				// Only consider objects using the given sprite name. If blank, then optionalAtlas is required to find all sprites that use that atlas.
	public GameObject[] results;				// The array of results from the most recent find.
												// Put here instead of in FinderBase simply so it comes after the input properties in the inspector.
													
	[MenuItem ("Zynga/Wizards/Find Stuff/List UIAtlas References")]static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<AtlasReferenceLister>("List UIAtlas References", "Close", "Find");
	}

	new protected virtual void OnWizardUpdate() {
		//base.OnWizardUpdate();
		helpString = "Results shown in Debug Log (open Editor.log) and copied to user-clipboard";
	}

	public bool showOnlyHIRReferences = false;

	// the Find button was clicked
	public override void OnWizardOtherButton()
	{
		if (!isValidInput)
		{
			return;
		}

		// Gather all the objects in the assets and/or scene.
		List<GameObject> searchSpace = new List<GameObject>();

		if (searchAssets)
		{
			List<GameObject> allPrefabs = gatherPrefabs(assetsPath);
			filterPrefabResults(allPrefabs);
		}

		if (searchScene)
		{
			// Also find objects in the scene.
			foreach (GameObject rootObject in CommonEditor.sceneRoots())
			{
				foreach (Transform child in rootObject.GetComponentsInChildren<Transform>(true))
				{
					searchSpace.Add(child.gameObject);
				}
			}
			// TODO: need to refactor code below so I can pass it list of objects instead of list of prefabs,
			//       and implement filterResults() on that
			Debug.LogError("TODO: implement search for game scene");
		}

		// Filter the results for display.
		//filterResults(searchSpace);
	}
		
	UIAtlas GetReferencedAtlasFromGameObj(GameObject obj, out string spriteName)
	{
		spriteName = null;
		// Does it have the UISprite component?
		UIImageButton imageButton = null;
		UILabel uiLabel = null;
		UISprite sprite = obj.GetComponent<UISprite>();

		if (sprite == null)
		{
			uiLabel = obj.GetComponent<UILabel>();
			if (uiLabel == null)
			{
				imageButton = obj.GetComponent<UIImageButton>();
				if (imageButton == null)
					return null;
			}
		}

		UIAtlas atlas = null;

		if (sprite != null)
		{
			atlas = getFinalAtlas(sprite.atlas);
			spriteName = string.Format("sprite: '{0}'", sprite.spriteName);
		}
		else if (uiLabel != null)
		{
			if (uiLabel.font != null && uiLabel.font.atlas != null)
			{
				atlas = getFinalAtlas(uiLabel.font.atlas);
				spriteName = string.Format("UIFont: '{0}'", uiLabel.font.name);
			}
			else
			{
				spriteName = string.Format("UIFont: NULL");
			}
		}
		else if(imageButton != null)
		{
			if (imageButton.target != null && imageButton.target.atlas != null)
			{
				atlas = getFinalAtlas(imageButton.target.atlas);
				spriteName = string.Format("ImageButton: '{0}'", imageButton.name);
			}
			else
			{
				spriteName = string.Format("ImageButton: NULL");
			}
		}

		return atlas;
	}

	private enum skuType { Unknown, Common, HIR };

	private skuType GetSkuFromPath(string path)
	{
		if (path.Contains("/HIR/"))
		{
			return skuType.HIR;
		}
		
		if (path.Contains("/Common/"))
		{
			return skuType.Common;
		}

		return skuType.Unknown;
	}

	private bool areSkusMismatched(skuType sku1, skuType sku2)
	{
		if (sku1 == skuType.Common || sku2 == skuType.Common)
			return false;
		if (sku1 == skuType.Unknown || sku2 == skuType.Unknown)
			return false;

		return sku1 != sku2;
	}

	protected void filterPrefabResults(List<GameObject> prefabList)
	{
		var prefab2AtlasReferencingObjListDict = new Dictionary<GameObject, List<GameObject>>();
		var atlas2PrefabsReferencedDict = new Dictionary<UIAtlas, HashSet<GameObject>>();
		var atlasObjReferenceCount = new Dictionary<UIAtlas, int>();   // number of objects within prefabs (Gameobjects) that reference the atlas
		foreach (GameObject prefab in prefabList)
		{
			List<GameObject> atlasRefGameObjs = new List<GameObject>();
			List<GameObject> allChildren = CommonGameObject.findAllChildren(prefab, true);
			foreach (GameObject child in allChildren)
			{
				string spriteName;
				UIAtlas atlas = GetReferencedAtlasFromGameObj(child, out spriteName);
				if (atlas == null)
				{
					continue;
				}
				atlasRefGameObjs.Add(child);

				HashSet<GameObject> prefabsReferencedSet;
				if (atlas2PrefabsReferencedDict.TryGetValue(atlas, out prefabsReferencedSet))
				{
					prefabsReferencedSet.Add(prefab);
				}
				else
				{
					prefabsReferencedSet = new HashSet<GameObject>();
					prefabsReferencedSet.Add(prefab);
					atlas2PrefabsReferencedDict[atlas] = prefabsReferencedSet;
				}

				int numRefs = 0;
				if (atlasObjReferenceCount.TryGetValue(atlas, out numRefs))
				{
					atlasObjReferenceCount[atlas] = numRefs+1;
				}
				else
				{
					atlasObjReferenceCount[atlas] = 1;
				}
			}
			// really want prefab->Atlas->list of all game objects names with sprites names used
			// and dictionary of Atlases, each with list of all referencing prefabs
			prefab2AtlasReferencingObjListDict[prefab] = atlasRefGameObjs;
		}

		/////////////////////////////  Print Report //////////////////////////////
			
		StringBuilder finalReportStrBuilder = new StringBuilder();

		finalReportStrBuilder.AppendFormat("Atlas Prefab Report for SearchPath: {0}  (copied to clipboard)\n",assetsPath.ToString());
		bool bFoundHIRPrefab = false;

		var atlasDictEnum = atlas2PrefabsReferencedDict.GetEnumerator();
		while (atlasDictEnum.MoveNext())
		{
			UIAtlas atlas = atlasDictEnum.Current.Key;
			string atlasPath = AssetDatabase.GetAssetPath(atlas);

			skuType atlasSku = GetSkuFromPath(atlasPath);

			if (showOnlyHIRReferences && !atlasPath.Contains("/HIR/"))
			{
				continue;
			}
			StringBuilder msg = new StringBuilder();

			bFoundHIRPrefab = true;

			HashSet<GameObject> prefabsReferencedSet = atlasDictEnum.Current.Value;
			msg.AppendFormat("Atlas {0} ({1}) : Prefab references: {2}, Object references: {3}   :\n",atlas.name,atlasPath,prefabsReferencedSet.Count,atlasObjReferenceCount[atlas]);
			string atlasPathFormatted = string.Format("(atlas \"{0}\")", atlas.name);

			foreach (GameObject prefab in prefabsReferencedSet)
			{
				string prefabPath = AssetDatabase.GetAssetPath(prefab);
				msg.AppendFormat("    {0}.prefab : {1}\n",prefab.name,prefabPath);

				skuType prefabSku = GetSkuFromPath(prefabPath);

				if (areSkusMismatched(prefabSku, atlasSku))
				{
					msg.AppendFormat("*** Mismatched skus for prefab {0} and atlas {1}!!\n",prefabSku.ToString(),atlasSku.ToString());
				}

				List<GameObject> atlasRefGameObjs = prefab2AtlasReferencingObjListDict[prefab];
				foreach(GameObject referencingObj in atlasRefGameObjs)
				{
					string spriteName = null;
					UIAtlas usedAtlas = GetReferencedAtlasFromGameObj(referencingObj, out spriteName);
					if (usedAtlas != atlas)  // only print out the atlas refs for this specific atlas
						continue;
					string refMsg = string.Format("obj '{0}' uses {1}", referencingObj.name, spriteName).PadRight(80);  // try to right-align the atlas name in 1 column
					msg.AppendFormat("        {0}{1}\n",refMsg, atlasPathFormatted);
				}
			}
				
			finalReportStrBuilder.Append("\n");
			finalReportStrBuilder.Append(msg);
		}

		if(showOnlyHIRReferences && !bFoundHIRPrefab)
			finalReportStrBuilder.Append("Found No Prefabs referencing HIR atlases!\n");

		string finalReportStr = finalReportStrBuilder.ToString();
		EditorGUIUtility.systemCopyBuffer = finalReportStr;   // allow user to paste into text editor
		Debug.Log(finalReportStr);
	}
		
	protected override void filterResults(List<GameObject> searchSpace)
	{
		// TODO: fill this out so we can search scene
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