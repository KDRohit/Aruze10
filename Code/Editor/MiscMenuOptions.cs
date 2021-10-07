using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/**
Various menu utility options.
*/
public static class MiscMenuOptions
{
	[MenuItem("Zynga/Wizards/Clear PlayerPrefsCache")] 
	public static void menuClearPlayerPrefsCache()
	{
		PlayerPrefsCache.DeleteAll();
		PlayerPrefsCache.Save();
	}

	[MenuItem ("Zynga/Wizards/Clear PlayerPrefs")]
    public static void menuClearPlayerPrefs() 
    {
	   PlayerPrefs.DeleteAll();
	   PlayerPrefs.Save();
    }

	[MenuItem("Zynga/Editor Atlas/Use Hi Res")] 
	public static void menuUseHiResAtlas()
	{
		useAtlases(AtlasSwap.GameResolution.High);
	}

	[MenuItem("Zynga/Editor Atlas/Use Low Res")] 
	public static void menuUseLowResAtlas()
	{
		useAtlases(AtlasSwap.GameResolution.Low);
	}

	[MenuItem("Zynga/Create Optimized Symbols For All Games")] 
	public static void menuCreateOptimizedSymbolsForAllGames()
	{
		ReelSetupEditor.createOptimizedSymbolsForAllGames();
	}

	private static void useAtlases(AtlasSwap.GameResolution resolutionToUse)
	{
		AtlasSwap swapper = Object.FindObjectOfType(typeof(AtlasSwap)) as AtlasSwap;

		if (swapper == null)
		{
			Debug.LogError("You must have the Startup scene loaded before swapping atlases.");
			return;
		}

		swapper.swapAtlases(resolutionToUse);
	}
	
	[MenuItem("Zynga/Assets/Clear Unity Caching")] static void menuClearUnityCache()
	{
		AssetBundleManager.clearUnityAssetBundleCache();
	}
	
	[MenuItem("Zynga/Assets/Remove Empty Folders")] public static void menuRemoveEmptyFolders()
	{
		CommonEditor.removeEmptyFolders("Assets/");
		Debug.Log("Done cleaning up empty folders.");
	}

	[MenuItem("Zynga/Assets/Fix Prefab Flags")] public static void fixPrefabFlags()
	{
		List<GameObject> parents = CommonEditor.gatherPrefabs("Assets/");

		foreach (GameObject parentObject in parents)
		{
			fixFlags(parentObject, parentObject);
		}
		
		Debug.Log("Done checking/fixing bad prefab flags.");
	}

	private static void fixFlags(GameObject go, GameObject prefabRoot)
	{
		switch (go.hideFlags)
		{
			case HideFlags.DontSave:
			case HideFlags.HideAndDontSave:
				Debug.LogError(string.Format("Fixed GameObject {0}hideFlags from {1} to None", go.name + " ", go.hideFlags), prefabRoot);
				go.hideFlags = HideFlags.None;
				EditorUtility.SetDirty(go);
				EditorUtility.SetDirty(prefabRoot);
				break;
		}

		foreach (Transform childTransform in go.transform)
		{
			fixFlags(childTransform.gameObject, prefabRoot);
		}
	}

	[MenuItem ("CONTEXT/Transform/Create Empty GameObject")]
	public static void CreateGameObjectAsChild (MenuCommand command)
	{
		AddChild(new GameObject("GameObject"), (Transform)command.context);
	}

	[MenuItem ("CONTEXT/Transform/Create Cube")]
	public static void CreateCubeAsChild (MenuCommand command)
	{
		AddChild(GameObject.CreatePrimitive(PrimitiveType.Cube), (Transform)command.context);
	}

	[MenuItem ("CONTEXT/Transform/Create Sphere")]
	public static void CreateSphereAsChild (MenuCommand command)
	{
		AddChild(GameObject.CreatePrimitive(PrimitiveType.Sphere), (Transform)command.context);
	}

	[MenuItem ("CONTEXT/Transform/Create Particle System")]
	public static void CreateParticleSystemAsChild (MenuCommand command)
	{
		GameObject g = new GameObject("ParticleSystem");
		g.AddComponent<ParticleSystem>();
		AddChild(g, (Transform)command.context);
	}

	[MenuItem ("CONTEXT/Transform/Flatten selected hierarchy")]
	public static void flattenSelectedHierarchy (MenuCommand command)
	{
		CommonEditor.flattenHierarchy((Transform)command.context, false, false);
	}


	private static void AddChild(GameObject g, Transform t)
	{
		g.transform.parent = t;
		g.transform.localPosition = Vector3.zero;
		g.transform.localScale = Vector3.one;
		g.layer = t.gameObject.layer;
		Selection.activeObject = g;
	}

#if ZYNGA_SKU_HIR
	[MenuItem("Zynga/Change SKU (current: HIR)/HIR", false, -1000)]
#else
	[MenuItem("Zynga/Change SKU (current: )/HIR", false, -1000)]
#endif
	public static void menuChangeSKUHIR()
	{
		changeSku(SkuId.HIR);
	}

#if ZYNGA_SKU_HIR
	[MenuItem("Zynga/Change SKU (current: HIR)/HIR", true)]
#else
	[MenuItem("Zynga/Change SKU (current: )/HIR", true)]
#endif
	public static bool menuChangeSKUHIRAllow()
	{
		return SkuResources.currentSku != SkuId.HIR;
	}

	private static void changeSku(SkuId newSku)
	{
		Debug.Log("Changing SKU to " + newSku.ToString());
		foreach (SkuId val in System.Enum.GetValues(typeof(SkuId)))
		{
			if (val == SkuId.UNKNOWN)
			{
				continue;
			}
			string defineString = string.Format("ZYNGA_SKU_{0}", val);
			if (val == newSku)
			{
				Debug.Log("Adding build define " + defineString);
				CommonEditor.AddScriptingDefineSymbolForGroup(defineString, BuildTargetGroup.iOS);
				CommonEditor.AddScriptingDefineSymbolForGroup(defineString, BuildTargetGroup.Android);
				CommonEditor.AddScriptingDefineSymbolForGroup(defineString, BuildTargetGroup.WebGL);
			}
			else
			{
				if (CommonEditor.IsScriptingDefineSymbolDefinedForGroup(defineString, BuildTargetGroup.iOS))
				{
					Debug.Log("Removing iOS build define " + defineString);
					CommonEditor.RemoveScriptingDefineSymbolForGroup(defineString, BuildTargetGroup.iOS);
				}
				if (CommonEditor.IsScriptingDefineSymbolDefinedForGroup(defineString, BuildTargetGroup.Android))
				{
					Debug.Log("Removing Android build define " + defineString);
					CommonEditor.RemoveScriptingDefineSymbolForGroup(defineString, BuildTargetGroup.Android);
				}
				if (CommonEditor.IsScriptingDefineSymbolDefinedForGroup(defineString, BuildTargetGroup.WebGL))
				{
					Debug.Log("Removing WebGL build define " + defineString);
					CommonEditor.RemoveScriptingDefineSymbolForGroup(defineString, BuildTargetGroup.WebGL);
				}
			}
		}
		// Force recompile, sometimes Unity doesn't otherwise.
		AssetDatabase.Refresh();
	}
}
