using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Data structure for different types of dialogs.
We don't implement IResetGame on this data since it's read from a local resource,
so populating it once is sufficient.
*/

public class DialogType
{
	public const string DIALOG_PREFAB_ROOT_PATH = "Prefabs/Dialogs/";
	public const string BUNDLED_PREFAB_ROOT_PATH = "Features/";
	public string keyName { get; private set; }
	public bool isPurchaseDialog { get; private set; }
	public bool isBundled { get; private set; }
	public bool isSkippingBundleMap { get; private set; }
	public bool shouldUnloadBundleOnClose { get; private set; }
	
	public bool hasCustomAnim = false;
	public float animInTime = 0.25f;
	public float animOutTime = 0.25f;
	public Dialog.AnimPos animInPos = Dialog.AnimPos.TOP;
	public Dialog.AnimPos animOutPos = Dialog.AnimPos.BOTTOM;	
	public Dialog.AnimScale animInScale = Dialog.AnimScale.FULL;
	public Dialog.AnimScale animOutScale = Dialog.AnimScale.FULL;
	public Dialog.AnimEase animInEase = Dialog.AnimEase.BACK;
	public Dialog.AnimEase animOutEase = Dialog.AnimEase.BACK;

	private string prefabPath = "";

	public string dialogPrefabPath
	{
		get
		{
			string pathToUse = isBundled ? BUNDLED_PREFAB_ROOT_PATH : DIALOG_PREFAB_ROOT_PATH;
			return pathToUse + prefabPath;
		}
	}

	// This needs to be checked on-demand in order to support dialogs
	// that are bundled, and therefore not in the game at app load.
	public GameObject prefab
	{
		get
		{
			// First check for embedded resources in case we are trying to load a dialog before the player data has loaded.
			GameObject obj = SkuResources.loadSkuSpecificEmbeddedResourcePrefab(dialogPrefabPath);
			if (obj == null)
			{
				// Only load dialogs that are immediately available, if they need to
				// download/cache from a bundle then make sure to do that beforehand.
				obj = SkuResources.loadSkuSpecificResourcePrefab(dialogPrefabPath);
				if (obj == null)
				{
					Debug.LogErrorFormat("DialogType.cs -- prefab getter -- attempted to load the prefab from path {0} and failed, prefab was bundled: {1}", dialogPrefabPath, isBundled);
				}
			}
			return obj;
		}
	}
	private GameObject _prefab = null;
	
	private static Dictionary<string, DialogType> all = new Dictionary<string, DialogType>();
	
	public DialogType(JSON data)
	{
		keyName = data.getString("key_name", "");
		
		isPurchaseDialog = data.getBool("is_purchase_dialog", false);
		isBundled = data.getBool("is_bundled", false);
		isSkippingBundleMap = data.getBool("skip_bundle_map", false);
		shouldUnloadBundleOnClose = data.getBool("unload_bundle_on_close", false);

		if (MobileUIUtil.isSmallMobile)
		{
			// On small devices, first try using the small device prefab property.
			prefabPath = data.getString("small_device_prefab", "");
		}
		
		if (string.IsNullOrEmpty(prefabPath))
		{
			// If blank, fall back to the normal prefab.
			prefabPath = data.getString("prefab", "");
		}

		JSON anim = data.getJSON("animation");
		if (anim != null)
		{
			hasCustomAnim = true;
			animInTime = anim.getFloat("in_time", 0.25f);
			animOutTime = anim.getFloat("out_time", 0.25f);
			animInPos = (Dialog.AnimPos)anim.getInt("in_position", 1);
			animInScale = (Dialog.AnimScale)anim.getInt("in_scale", 1);
			animInEase = (Dialog.AnimEase)anim.getInt("in_ease", 0);
			animOutPos = (Dialog.AnimPos)anim.getInt("out_position", 2);
			animOutScale = (Dialog.AnimScale)anim.getInt("out_scale", 2);
			animOutEase = (Dialog.AnimEase)anim.getInt("out_ease", 0);
		}

		if (all.ContainsKey(keyName))
		{
			Debug.LogErrorFormat("Duplicate dialog type detected: {0}, Dialog Types.txt needs fixing", keyName);
			return;
		}
			
		all.Add(keyName, this);
	}
	
	public float getAnimInTime()
	{
		if (hasCustomAnim)
		{
			return animInTime;
		}
		return Dialog.animInTime;
	}

	public float getAnimOutTime()
	{
		if (hasCustomAnim)
		{
			return animOutTime;
		}
		return Dialog.animOutTime;
	}

	public Dialog.AnimPos getAnimInPos()
	{
		if (hasCustomAnim)
		{
			return animInPos;
		}
		return Dialog.animInPos;
	}
	
	public Dialog.AnimScale getAnimInScale()
	{
		if (hasCustomAnim)
		{
			return animInScale;
		}
		return Dialog.animInScale;
	}
	
	public Dialog.AnimEase getAnimInEase()
	{
		if (hasCustomAnim)
		{
			return animInEase;
		}
		return Dialog.animInEase;
	}

	public Dialog.AnimPos getAnimOutPos()
	{
		if (hasCustomAnim)
		{
			return animOutPos;
		}
		return Dialog.animOutPos;
	}
	
	public Dialog.AnimScale getAnimOutScale()
	{
		if (hasCustomAnim)
		{
			return animOutScale;
		}
		return Dialog.animOutScale;
	}
	
	public Dialog.AnimEase getAnimOutEase()
	{
		if (hasCustomAnim)
		{
			return animOutEase;
		}
		return Dialog.animOutEase;
	}

	public void setThemePath(string skinName)
	{
		prefabPath = string.Format(prefabPath, skinName);
	}

	public static void populateAll()
	{
		if (all.Count > 0)
		{
			// Already populated. See notes at the top.
			return;
		}
		
		TextAsset textAsset = SkuResources.loadSkuSpecificEmbeddedResourceText("Data/Dialog Types") as TextAsset;
		
		if (textAsset == null)
		{
			Debug.LogError("Could not find Dialog Types in Resources.");
			return;
		}
		
		JSON json = new JSON(textAsset.text);
		
		if (!json.isValid)
		{
			Debug.LogError("Dialog Types JSON is invalid!");
			return;
		}
		JSON[] dialogTypes = json.getJsonArray("dialog_types");
		foreach (JSON data in dialogTypes)
		{
			new DialogType(data);
		}
	}

	public static DialogType find(string keyName, string skinName = "")
	{
		DialogType dlgType;
		if (all.TryGetValue(keyName, out dlgType))
		{
			return dlgType;
		}
		Debug.LogError("DialogType not found for key: " + keyName);
		return null;
	}
}
