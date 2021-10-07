using UnityEngine;
using System.Collections.Generic;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

// Other than UNKNOWN, the integer values here are what are defined on the backend.

public enum SkuId
{
	UNKNOWN = 0,
	HIR = 1,
	SIR = 6
}

public class SkuResources
{
	public static SkuId[] ALL_SKUS = new SkuId[] { SkuId.HIR, SkuId.SIR };
	private static object fakeCallerObject = new object(); // used when no caller object provided to loadFromMegaBundleWithCallbacks() function

	public static string skuString
	{
		get
		{
			switch (currentSku)
			{
				case SkuId.HIR: 
					return "hir";
				case SkuId.SIR:
					return "sir";
				default:
					Debug.LogError("Unknown SKU");
					return "";
			}
		}
	}

	public static SkuId currentSku
	{
		get
		{
#if ZYNGA_SKU_HIR
			return SkuId.HIR;
#elif ZYNGA_SKU_SIR
			return SkuId.SIR;
#else
			return SkuId.UNKNOWN;
#endif
		}
	}

	public static GameObject loadFromMegaBundleOrResource(string path)
	{
		if (Application.isPlaying && AssetBundleManager.useAssetBundles)
		{
			GameObject obj = getObjectFromMegaBundle<GameObject>(path);

			if (obj != null)
			{
				return obj;
			}
		}

		path = path.Replace("Assets/Data/HIR/Bundles/Initialization/", "");
		return loadSkuSpecificResource<GameObject>(path, ".prefab") as GameObject;
	}
	public static GameObject loadSkuSpecificResourcePrefab(string path)
	{
		return loadSkuSpecificResource<GameObject>(path, ".prefab") as GameObject;
	}

	public static TextAsset loadSkuSpecificResourceText(string path)
	{
		return loadSkuSpecificResource<TextAsset>(path, ".txt") as TextAsset;
	}
	
	// Tries both jpg and png.
	public static Texture2D loadSkuSpecificResourceImage(string path)
	{
		Texture2D img = loadSkuSpecificResourcePNG(path);
		if (img == null)
		{
			img = loadSkuSpecificResourceJPG(path);
		}
		return img;
	}

	public static Texture2D loadSkuSpecificResourcePNG(string path)
	{
		return loadSkuSpecificResource<Texture2D>(path, ".png") as Texture2D;
	}

	public static Texture2D loadSkuSpecificResourceJPG(string path)
	{
		return loadSkuSpecificResource<Texture2D>(path, ".jpg") as Texture2D;
	}
	
	public static AudioClip loadSkuSpecificResourceWAV(string path)
	{
		return loadSkuSpecificResource<AudioClip>(path, ".wav") as AudioClip;
	}

	public static Material loadSkuSpecificResourceMaterial(string path)
	{
		return loadSkuSpecificResource<Material>(path, ".mat") as Material;
	}
	
	public static bool isCorrectSku(int skuId)
	{
		if (currentSku == SkuId.UNKNOWN)
		{
			Debug.LogError("Unknown SKU, cannot determine if this is the same SKU");
			return false;
		}
		else
		{
			return (SkuId)skuId == currentSku;
		}
	}

	public static T loadSkuSpecificResource<T>(string path, string extension) where T: Object
	{
#if UNITY_EDITOR
		// If we are in the editor, then we have both resource directories, so we need to for it to load the one we want.
		string[] testPaths;
		string upcasedSku = skuString.ToUpper();
		// TODO: Should this become RESOURCE-based only? 
		// Is the /Bundles/ support just legacy code from when bundle assets moved in & out of resources?
		testPaths = new string[] 
		{
			"Assets/Data/" + upcasedSku + "/Resources/",
			"Assets/Data/" + upcasedSku + "/Bundles/Initialization/",
			"Assets/Data/" + upcasedSku + "/Bundles/"
		};

		// if user checks 'UseAssetBundles', dont load files directly from /Bundles/ dir, so we can emulate real asset bundle loading
		int numSearchPaths = AssetBundleManager.useAssetBundles ? 2 : 3;
		
		// Check each test path for the asset in the editor, if found then use that asset.
		for(int i=0;i<numSearchPaths;i++)
		{
			string newPath = testPaths[i] + path + extension;
			T asset = AssetDatabase.LoadAssetAtPath(newPath, typeof(T)) as T;
			if (asset != null)
			{
				return asset;
			}
		}
#endif
		if (Application.isPlaying)
		{
			// Fallback to AssetBundleManager.loadImmediately()
			T asset = AssetBundleManager.loadImmediately(path, extension) as T;
			if (asset != null)
			{
				return asset;
			}

			if (AssetBundleManager.useAssetBundles && NGUILoader.instance.initialBundle != null)
			{
				return asset = getObjectFromMegaBundle<T>("Assets/Data/" + skuString.ToUpper() + "/Bundles/Initialization/" + path + extension) as T;
			}
		}
		
		return null;
	}


	// Attempts to load embedded resource ONLY from a sku-specific RESOURCE folder; NOT bundles.
	// This is needed to fetch some assets (NGUI content, BundleManifests) before AssetBundleManager is started.
	public static T loadSkuSpecificEmbeddedResource<T>(string path, string extension) where T: Object
	{
#if UNITY_EDITOR
		// If we are in the editor, then we have multiple sku resource directories, so only check appropriate one
		string[] testPaths = new string[] 
		{ 
			"Assets/Data/" + skuString.ToUpper() + "/Bundles/Initialization/",
			"Assets/Data/" + skuString.ToUpper() + "/Resources/"
		};

		// Check each test path for the asset in the editor, if found then use that asset.
		foreach(string skuResourceFolder in testPaths)
		{
			string newPath = skuResourceFolder + path + extension;
			T asset = AssetDatabase.LoadAssetAtPath(newPath, typeof(T)) as T;
			if (asset != null)
			{
				return asset;
			}
		}
#endif
		if (Application.isPlaying)
		{
			// Fallback to general resource load
			T asset = Resources.Load(path) as T;
			if (asset != null)
			{
				return asset;
			}

			if (AssetBundleManager.useAssetBundles && NGUILoader.instance.initialBundle != null)
			{
				return getObjectFromMegaBundle<T>("Assets/Data/" + skuString.ToUpper() + "/Bundles/Initialization/" + path + extension);
			}
		}

		return null;
	}

	// Attempts to load embedded resource ONLY from a sku-specific RESOURCE folder (not bundles)
	public static GameObject loadSkuSpecificEmbeddedResourcePrefab(string path)
	{
		return loadSkuSpecificEmbeddedResource<GameObject>(path, ".prefab") as GameObject;
	}

	public static TextAsset loadSkuSpecificEmbeddedResourceText(string path)
	{
		return loadSkuSpecificEmbeddedResource<TextAsset>(path, ".txt") as TextAsset;
	}

	public static AudioClip loadSkuSpecificEmbeddedResourceWAV(string path)
	{
		return loadSkuSpecificEmbeddedResource<AudioClip>(path, ".wav") as AudioClip;
	}


	public static T getObjectFromMegaBundle<T>(string path) where T: Object
	{
		T loadedObject = null;
		if (Application.isPlaying && AssetBundleManager.useAssetBundles)
		{
			loadedObject = NGUILoader.instance.initialBundle.LoadAsset(path) as T;
#if UNITY_EDITOR
			List<Object> objectToList = new List<Object>();
			objectToList.Add(loadedObject);
			AssetBundleMapping.fixDependentMaterialShaders(objectToList.Where(obj => obj is GameObject).ToArray());
		}
		else
		{
			loadedObject = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
#endif
		}

		return loadedObject;
	}

	public static void loadFromMegaBundleWithCallbacks(object caller, string resourcePath, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data = null)
	{
		Object loadedObject = getObjectFromMegaBundle<Object>(resourcePath);
		if (loadedObject != null)
		{
			if (caller != null && successCallback != null)
			{
				successCallback(resourcePath, loadedObject, data);
			}
			else
			{
				Debug.LogWarningFormat("Successfully loaded {0}, but the caller or success callback is null so we're not doing anything with it", resourcePath);	
			}
		}
		else
		{
			failCallback(resourcePath, data);
		}
	}

	public static void loadFromMegaBundleWithCallbacks(string resourcePath, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data = null)
	{
		loadFromMegaBundleWithCallbacks(fakeCallerObject, resourcePath, successCallback, failCallback, data);
	}
	
	public static IEnumerator loadFromMegaBundleWithCallbacksAsync(object caller, string resourcePath, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data = null)
	{
		Object loadedObject = null;
		if (Application.isPlaying && NGUILoader.instance.useAssetBundles)
		{
			AssetBundleRequest req = NGUILoader.instance.initialBundle.LoadAssetAsync(resourcePath);
			// It was yield return req, but I got an error "yield return AssetBundleRequest" is not supported in editor
			// when useAssetBundles is turned on in Zynga -> Game Login Settings
			while (!req.isDone)
			{
				yield return null;
			}

			loadedObject = req.asset as Object;
#if UNITY_EDITOR
			List<Object> objectToList = new List<Object>();
			objectToList.Add(loadedObject);
			AssetBundleMapping.fixDependentMaterialShaders(objectToList.Where(obj => obj is GameObject).ToArray());
		}
		else
		{
			loadedObject = AssetDatabase.LoadAssetAtPath(resourcePath, typeof(Object)) as Object;
#endif
		}

		if (loadedObject != null)
		{
			if (caller != null && successCallback != null)
			{
				successCallback(resourcePath, loadedObject, data);
			}
			else
			{
				Debug.LogWarningFormat("Successfully loaded {0}, but the caller or success callback is null so we're not doing anything with it", resourcePath);	
			}
		}
		else if(caller != null && failCallback != null)
		{
			failCallback(resourcePath, data);
		}
	}
	
	public static IEnumerator loadFromMegaBundleWithCallbacksAsync(string resourcePath, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data = null)		
	{		
		yield return RoutineRunner.instance.StartCoroutine(loadFromMegaBundleWithCallbacksAsync(fakeCallerObject, resourcePath, successCallback, failCallback, data));		
	}
}