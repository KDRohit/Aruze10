#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class AssetBundleMapping {

	public List<string> listAssetPath = new List<string>();
	public List<Object> listAssets = new List<Object>();
#if UNITY_WSA_10_0
    private Dictionary<string, Object> dictPathToAsset = new Dictionary<string, Object>(System.StringComparer.OrdinalIgnoreCase);
#else
    private Dictionary<string, Object> dictPathToAsset = new Dictionary<string, Object>(System.StringComparer.InvariantCultureIgnoreCase);
#endif

	public bool isAsync { get; private set; }
	public bool isFailed { get; private set; }
	public bool isSkippingLoadAllAssets { get; private set; }

	private const string ASYNC_KEY_LOAD_ALL = "*";

	private AssetBundle bundle;

	private const string BUNDLES_PATH = "assets/data/hir/bundles/{0}{1}";
	
	public IEnumerator createAssetBundleMappingAsync(AssetBundle bundle)
	{
		yield return RoutineRunner.instance.StartCoroutine(loadAsyncBundle(bundle));
	}

	private IEnumerator loadAsyncBundle(AssetBundle bundle)
	{
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		AssetBundleRequest loadReq = bundle.LoadAllAssetsAsync();

		while (!loadReq.isDone)
		{
			//Debug.Log("bundle " + bundle.name + " prog = " + loadReq.progress);
			yield return null;
		}

		var allAssets = loadReq.allAssets;
		Debug.Log("AssetBundleMapping:: " + stopwatch.Elapsed.TotalSeconds + " sec to ASYNC load for " + bundle.name);

		Init(bundle, allAssets);
	}

	public AssetBundleMapping(AssetBundle bundle, bool loadNow = false, bool isSkippingMapping = false)
	{
		isFailed = false;
		isSkippingLoadAllAssets = isSkippingMapping;
		// check livedata for specific bundles to be downloaded asynchronously
		if (!loadNow && Data.liveData != null &&
			Data.liveData.getBool("LAZY_LOAD_BUNDLES", false) &&
			AssetBundleManager.hasLazyBundle(bundle.name))
		{
			isAsync = true;
		}
		else
		{
			Init(bundle);
		}
	}

	// V2 asset bundles mappings no longer are embedded as components, instead, we construct it from an AssetBundle at runtime
	public void Init(AssetBundle bundle, Object[] preloadedAssets = null)
	{
		// Setup listAssetPath and listAssets
		if (bundle.name.FastStartsWith(AssetBundleManager.INITIALIZATION_BUNDLE_NAME))
		{
			return;
		}

		this.bundle = bundle;

		if (!isSkippingLoadAllAssets)
		{
			listAssets = new List<Object>( preloadedAssets ?? bundle.LoadAllAssets() );       // includes sub-assets
		}

		listAssetPath = new List<string>( bundle.GetAllAssetNames() ); // does not include sub-assets :(

		// Discrepency? Unity sub-assets can mess things up (such as FBX files). Fix it so our assetPaths & asset lists both contain sub-assets
		if (!isSkippingLoadAllAssets && listAssets.Count != listAssetPath.Count)
		{
			var newAssets = new List<Object>( listAssets.Count );
			var newAssetPaths = new List<string>( listAssets.Count );

			foreach (var path in listAssetPath) 
			{
				// track the assets (including sub-assets)
				var objs = bundle.LoadAssetWithSubAssets(path);
				newAssets.AddRange(objs);

				// and track the assetPaths (use main assetPath, and create placeholders for the sub-assets)
				string pathNoExt = Path.ChangeExtension(path, null); 
				newAssetPaths.Add(pathNoExt);
				for (var i = 1; i < objs.Length; i++)
				{
					newAssetPaths.Add (pathNoExt + "_sub" + i);
				}
			}

			listAssets = newAssets;
			listAssetPath = newAssetPaths;
		}


		if (!isSkippingLoadAllAssets && listAssets.Count != listAssetPath.Count)
		{
			Debug.LogError("Bundle LoadAllAssets.Count != GetAllAssetNames.Count !");
		}

		// Fixup paths (convert unity project-relative path to our legacy short resource relative paths)
		for(int i=0; i < listAssetPath.Count; i++)
		{
			string path = longAssetPathToShortPath(listAssetPath[i]);
			//Debug.Log("InitFromBundle: Renaming " + listAssetPath[i] + " --> " + path);
			listAssetPath[i] = path;
		}
			
		// Setup the assetPath -> asset dictionary 
		if (!isSkippingLoadAllAssets)
		{
			for (int i = 0; i < listAssetPath.Count; ++i)
			{
				string lowerAssetPath = listAssetPath[i].ToLower();
				if (dictPathToAsset.ContainsKey(lowerAssetPath))
				{
					Debug.LogError("Duplicate asset " + listAssetPath[i]);
				}
				else
				{
					dictPathToAsset.Add(lowerAssetPath, listAssets[i]);
				}
			}
		}

#if UNITY_EDITOR
		// Fixup in-editor shaders, no more pink broken shaders (only in-editor)
		AssetBundleMapping.fixDependentMaterialShaders(listAssets.Where(obj => obj is GameObject).ToArray());
#endif
	}

	public void rebuildBundleMap()
	{
		if (bundle != null)
		{
			isSkippingLoadAllAssets = false;
			Init(bundle);
		}
	}


	public bool hasAsset(string assetPath, string fileExtension = "")
	{
		Object asset = getAsset(assetPath, fileExtension);
		return asset != null;
	}

	public Object getAsset(string assetPath, string fileExtension = "")
	{
		if (bundle == null || string.IsNullOrEmpty(assetPath) || assetPath.FastEndsWith("/"))
		{
			return null;
		}
		// A simple dictionary lookup
		Object result;
		if (!isSkippingLoadAllAssets && dictPathToAsset.TryGetValue(assetPath.ToLower(), out result)) 
		{
			return result;  //Grab from the dictionary if we didn't skip loading everything
		}
		else
		{
			if (string.IsNullOrWhiteSpace(fileExtension))
			{
#if UNITY_EDITOR
				Debug.LogErrorFormat("No file extension for path {0} in non-mapped bundle {1}", assetPath, bundle.name);
#else
				Bugsnag.LeaveBreadcrumb(string.Format("No file extension for path {0} in non-mapped bundle {1}", assetPath, bundle.name));
#endif				
			}
			string fullPath = string.Format(BUNDLES_PATH, assetPath, fileExtension).ToLower();
			result = bundle.LoadAsset(fullPath);
			if (result == null && (fileExtension == ".png" || fileExtension == ".jpg"))
			{
				string fallbackExtension = fileExtension == ".png" ? ".jpg" : ".png";
				fullPath = string.Format(BUNDLES_PATH, assetPath, fallbackExtension).ToLower();
				result = bundle.LoadAsset(fullPath);
			}
#if UNITY_EDITOR
			if (fileExtension == ".prefab")
			{
				List<Object> objectToList = new List<Object>();
				objectToList.Add(result);
				fixDependentMaterialShaders(objectToList.Where(obj => obj is GameObject).ToArray());
			}
#endif
			return result;
		}
	}


	// Convert Unity project-relative-assetpaths to our shorter "resource relative paths"
	//
	// Unity's new bundle build system emits a per-bundle manifest that keeps full project relative pathnames. 
	// But our existing code expects to lookup "resource" relative paths, all in lower-case, no extension.
	//
	// Examples:
	//                 "Assets/Assets Games/satc/Resources/Sounds/SATC_slots_common/SummaryFreespinSATC.wav"
	// becomes:                                           "sounds/satc_slots_common/summaryfreespinsatc" 
	//
	// (another):      "Assets/Assets Games/elvira/Resources/Games/elvira/elvira01/images/elvira01_gifting_paytable.png"
	// becomes:                                             "games/elvira/elvira01/images/elvira01_gifting_paytable"
	//
	// (newer format): "Data/Games/satc/satc01/Sounds/SATC_slots_common/SummaryFreespinSATC.wav"
	// also becomes:                          "sounds/satc_slots_common/summaryfreespinsatc" 
	//
	// (newer format): "Data/Games/elvira/elvira01/images/elvira01_gifting_paytable.png"
	// becomes:             "games/elvira/elvira01/images/elvira01_gifting_paytable"
	//
	//                 "Assets/Data/HIR/Bundles/Images/level_up/Level_Up_Bonus_Dialog_BG.jpg"
	// becomes:                                "images/level_up/level_up_bonus_dialog_bg"
	//
	static public string longAssetPathToShortPath(string longPath)
	{
		// drop extension, make lower case
		string path = Path.ChangeExtension(longPath, null).ToLower(); 

		// drop everything before "Resources" or "ToBundle" or "Bundles"
		string[] separators = { "/resources/", "/tobundle/", "/bundles/" };
		string[] splitPath = path.Split( separators, System.StringSplitOptions.RemoveEmptyEntries );
		path = splitPath[splitPath.Length-1];
		// Fixup Game-paths
		if (path.FastStartsWith("assets/data/games/") || path.FastStartsWith("assets/assets games/"))
		{
			// if contains /sounds/, then use path starting at "sounds/"...
			int soundIndex = path.IndexOf("/sounds/");
			if (soundIndex >= 0)
			{
				path = path.Substring(soundIndex + 1); //+1 to skip first '/'
			}
			else 
			{
				// just keep the path starting at last "games/"...
				int gameIndex = path.LastIndexOf("games/");
				if( gameIndex >= 0)
				{
					path = path.Substring(gameIndex);
				}
				else
				{
					Debug.LogError("couldn't match '/sounds/' or 'games/' in assetpath: " + path);
				}
			}
		}

		return path;
	}


#if UNITY_EDITOR
	// if shader not supported (such as loading bundled iOS shader in Mac editor)
	// replace it with a Unity loaded-and-compiled version
	// TODO: see http://docs.unity3d.com/ScriptReference/ShaderCache.find.html for rules on shader inclusion
	public static void fixDependentMaterialShaders(Object[] allGameObjs)
	{				
		{
			int numShadersSwapped = 0;
			var deps = EditorUtility.CollectDependencies( allGameObjs );
			foreach(var dep in deps)
			{
				var material = dep as Material;
				if(material)
				{
					var shader = material.shader;

					// Some shaders ("Unlit/Special HSV", "Unlit/Transparent Alpha Colored", "Particles/Alpha Blended", etc)
					// say they're supported but aren't working in-editor, so we just reload all shaders...
					//if (!shader.isSupported)
					{
						var newShader = ShaderCache.find( shader.name );
						if (newShader != null)
						{
							material.shader = newShader;
							//myLog("shader swap: " + shader.name);
							numShadersSwapped++;
						}
						else
						{
							Debug.LogError("ERROR - can't find replacement shader for: " + shader.name);
						}
					}
				}
			}
			//Debug.Log("Swapped " + numShadersSwapped + " shaders");
		}
	}
#endif
	
}
