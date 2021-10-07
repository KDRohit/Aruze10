using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine.Networking;
using Reporting = UnityEditor.Build.Reporting;
using UnityEngine.Profiling;
using Zynga.Metrics.UserAcquisition;

/**
This static class contains methods used to create client application builds.
*/
public static class Build
{
	#region Build Paths
	public static string BUILD_ROOT_PATH
	{
		get
		{
			string path = System.Environment.GetEnvironmentVariable("BUILD_FOLDER");
			if (string.IsNullOrEmpty(path))
			{
				// If BUILD_FOLDER is not set, fall back to this heuristic (which should work since we are almost
				// certainly working locally):
				// '.../../../Build' should make the folder next to the Unity folder.
				path = Path.Combine(Application.dataPath, "../../build");
			}
			return path;
		}
	}

	public static string IOS_CLIENT_BUILD_PATH
	{
		get
		{
			return Path.Combine(BUILD_ROOT_PATH, "client/ios");
		}
	}

	public static string ANDROID_CLIENT_BUILD_PATH
	{
		get
		{
			return Path.Combine(BUILD_ROOT_PATH, "client/android");
		}
	}

	public static string WSA_CLIENT_BUILD_PATH
	{
		get
		{
			return Path.Combine("./", "client/wsa");
		}
	}
	
	public static string WEBGL_CLIENT_BUILD_PATH
	{
		get
		{
			return Path.Combine("./", "client/webgl");
		}
	}

	public static string IOS_BUNDLE_BUILD_PATH
	{
		get
		{
			return Path.Combine(BUILD_ROOT_PATH, "bundles/ios");
		}
	}

	public static string ANDROID_BUNDLE_BUILD_PATH
	{
		get
		{
			return Path.Combine(BUILD_ROOT_PATH, "bundles/android");
		}
	}

	public static string WSA_BUNDLE_BUILD_PATH
	{
		get
		{
			return Path.Combine("./", "bundles/wsa");
		}
	}
	
	public static string WEBGL_BUNDLE_BUILD_PATH
	{
		get
		{
			return Path.Combine("./", "bundles/webgl");
		}
	}

	public static string ANDROID_CLIENT_APK_PATH
	{
		get
		{
			string path = System.Environment.GetEnvironmentVariable("APP_PATH");
			if (string.IsNullOrEmpty(path))
			{
				path = Path.Combine(ANDROID_CLIENT_BUILD_PATH, "hititrich.apk");
			}
			return path;
		}
	}

	#endregion
	
	#region Client Scenes
	private static string[] buildMobileScenes = new string[]
	{
		"Assets/Data/HIR/Scenes/Startup.unity",
		"Assets/Data/Common/Scenes/Startup Logic.unity",
		"Assets/Data/Common/Scenes/Loading.unity",
		"Assets/Data/Common/Scenes/Lobby.unity",
		"Assets/Data/Common/Scenes/Game.unity",
		"Assets/Data/Common/Scenes/ResetGame.unity"
	};
	#endregion
	
	#region Unity Editor Menu Options

/* temp
	[MenuItem ("Zynga/BundlesV2/Generate Bundle Reports - TestHD", false, 1)]
	static void GenerateBundlesReports_TestHD()
	{
		Debug.Log("GenerateBundlesReports_Default (HD Variant) for SKU: " + SkuResources.skuString.ToUpper() + ",  Target: " + EditorUserBuildSettings.activeBuildTarget.ToString() );
		CreateAssetBundlesV2.BuildBundles(SkuResources.currentSku, EditorUserBuildSettings.activeBuildTarget, Variant.HD, true); //REPORTS, just HD for now
	}
*/

	[MenuItem ("Zynga/BundlesV2/BuildBundles - HD Variant only", false, 1)]
	static void BuildBundlesV2_Default()
	{
		Debug.Log("BuildBundlesV2 (HD Variant) for SKU: " + SkuResources.skuString.ToUpper() + ",  Target: " + EditorUserBuildSettings.activeBuildTarget.ToString() );
		CreateAssetBundlesV2.BuildBundles(SkuResources.currentSku, EditorUserBuildSettings.activeBuildTarget, Variant.HD);
	}

	[MenuItem ("Zynga/BundlesV2/BuildBundles - SD", false, 1)]
	static void BuildBundlesV2_SD()
	{
		Debug.Log("BuildBundlesV2 (SD) for SKU: " + SkuResources.skuString.ToUpper() + ",  Target: " + EditorUserBuildSettings.activeBuildTarget.ToString() );
		CreateAssetBundlesV2.BuildBundles(SkuResources.currentSku, EditorUserBuildSettings.activeBuildTarget, Variant.SD);
	}

	// [MenuItem ("Zynga/BundlesV2/BuildBundles - SD4444", false, 1)]
	// static void BuildBundlesV2_SD4444()
	// {
	// 	Debug.Log("BuildBundlesV2 (SD4444) for SKU: " + SkuResources.skuString.ToUpper() + ",  Target: " + EditorUserBuildSettings.activeBuildTarget.ToString() );
	// 	CreateAssetBundlesV2.BuildBundles(SkuResources.currentSku, EditorUserBuildSettings.activeBuildTarget, Variant.SD4444);
	// }

	// [MenuItem ("Zynga/BundlesV2/BuildBundles - TintedMipMapTest", false, 1)]
	// static void BuildBundlesV2_TintedMipTest()
	// {
	// 	Debug.Log("BuildBundlesV2 (TintedMipTest) for SKU: " + SkuResources.skuString.ToUpper() + ",  Target: " + EditorUserBuildSettings.activeBuildTarget.ToString() );
	// 	CreateAssetBundlesV2.BuildBundles(SkuResources.currentSku, EditorUserBuildSettings.activeBuildTarget, Variant.TintedMipTest);
	// }

	[MenuItem ("Zynga/BundlesV2/BuildBundles - ALL Variants", false, 1)]
	static void BuildBundlesV2_AllVariants()
	{
		Debug.Log("BuildBundlesV2_AllVariants...");
		AssetDatabase.Refresh(); // start in a known state...
		var variantsToBuild = new Variant[] { Variant.HD, Variant.SD };
		CreateAssetBundlesV2.BuildBundles(SkuResources.currentSku, EditorUserBuildSettings.activeBuildTarget, variantsToBuild);
	}


	//..... Menu items for dev's to (locally and destructively) apply variant overides .....

	[MenuItem ("Zynga/BundlesV2/Restore Variant - HD", false, 20)]
	static void setupVariant_RestoreDefaultHD()
	{
		BuildVariant.restoreAllTextureImporters();
	}

	[MenuItem ("Zynga/BundlesV2/Setup Variant - SD", false, 20)]
	static void setupVariant_SD()
	{
		BuildVariant.setupTextureVariant(SkuResources.currentSku, EditorUserBuildSettings.activeBuildTarget, Variant.SD);
	}
	
	// Menu option that can test out the same process our builds do when swapping textures to SD
	[MenuItem ("Zynga/BundlesV2/Convert Selected Textures to SD", false, 20)]
	static void convertSelectedTextureToSD()
	{
		int countModified = 0;
	
		Texture[] allSelectedTextures = Selection.GetFiltered<Texture>(SelectionMode.Editable);
		foreach (Texture texture in allSelectedTextures)
		{
			string assetPath = AssetDatabase.GetAssetPath(texture);
			if (!string.IsNullOrEmpty(assetPath))
			{
				bool isModified = BuildVariant.convertTextureToVariant(assetPath, EditorUserBuildSettings.activeBuildTarget, Variant.SD, true);
				if (isModified)
				{
					countModified++;
				}
			}
		}
		
		if (countModified > 0)
		{
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
		}
	}

	// Menu option that can test out the same process our builds do when restoring HD versions of textures
	// NOTE: You should use the "Zynga/BundlesV2/Convert Selected Textures to SD" menu option first to create
	// a file to restore from
	[MenuItem("Zynga/BundlesV2/Restore Selected Textures to HD From Backup", false, 20)]
	static void restoreSelectedTextureToHdFromBackup()
	{
		int countRestored = 0;
	
		Texture[] allSelectedTextures = Selection.GetFiltered<Texture>(SelectionMode.Editable);
		foreach (Texture texture in allSelectedTextures)
		{
			string assetPath = AssetDatabase.GetAssetPath(texture);
			if (!string.IsNullOrEmpty(assetPath))
			{
				bool didRestoreFile = BuildVariant.restoreOriginalMetaFile(assetPath);
				if (didRestoreFile)
				{
					BuildVariant.restoreAtlasForTexture(assetPath);
				}
				countRestored += didRestoreFile ? 1 : 0;
			}
		}
		
		if (countRestored > 0)
		{
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
		}
	}

	// Menu option that attempts to save selected texture meta files into update formats.  This can
	// be used to try and change the hash used for the asset into the cache server to prevent grabbing
	// a broken version from the cache server until the affected files can actually be removed from
	// the cache server itself. May not change the file if the meta file is already in the latest format.
	[MenuItem("Zynga/BundlesV2/Ressave Selected Textures Meta Files", false, 20)]
	static void resaveSelectedTexturesMetaFiles()
	{
		int countModified = 0;
	
		Texture[] allSelectedTextures = Selection.GetFiltered<Texture>(SelectionMode.Editable);
		foreach (Texture texture in allSelectedTextures)
		{
			string assetPath = AssetDatabase.GetAssetPath(texture);
			if (!string.IsNullOrEmpty(assetPath))
			{
				bool isModified = BuildVariant.resaveTextureMetaFile(assetPath);
				if (isModified)
				{
					countModified++;
				}
			}
		}
		
		if (countModified > 0)
		{
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
		}
	}
	
	// Menu option that attempts to save a list of texture asset path meta files into updated formats.  This can
	// be used to try and change the hash used for the asset into the cache server to prevent grabbing
	// a broken version from the cache server until the affected files can actually be removed from
	// the cache server itself. May not change the file if the meta file is already in the latest format.
	// NOTE: Fill in assetPaths with the paths you want updated
	[MenuItem("Zynga/BundlesV2/Ressave Broken List of Textures Meta Files", false, 20)]
	static void resaveBrokenListTexturesMetaFiles()
	{
		int countModified = 0;
		
		// Add list of Asset paths here to change them
		string[] assetPaths = new string[0];

		foreach (string assetPath in assetPaths)
		{
			if (!string.IsNullOrEmpty(assetPath))
			{
				bool isModified = BuildVariant.resaveTextureMetaFile(assetPath);
				if (isModified)
				{
					countModified++;
				}
			}
		}
		
		Debug.Log("Build.resaveBrokenListTexturesMetaFiles() - assetPaths.Length = " + assetPaths.Length + "; countModified = " + countModified);
		if (countModified > 0)
		{
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
		}
	}

/* temp...
	[MenuItem ("Zynga/BundlesV2/TEMP-MergeManifests", false, 1)]
	static void BuildBundlesV2_MergedManifest()
	{
		Debug.Log("MergedManifests for SKU: " + SkuResources.skuString.ToUpper() + ",  Target: " + EditorUserBuildSettings.activeBuildTarget );

		//var combined = CreateAssetBundlesV2.mergeVariantManifests(SkuResources.skuString.ToUpper(), EditorUserBuildSettings.activeBuildTarget, new Variant[] { Variant.HD, Variant.TintedMipTest });
		CreateAssetBundlesV2.mergeManifestsAndCopyBundles(SkuResources.currentSku, EditorUserBuildSettings.activeBuildTarget, new Variant[] { Variant.HD, Variant.SD, Variant.SD4444, Variant.TintedMipTest });

	}
*/
	[MenuItem ("Zynga/BundlesV2/Download and Embed Initialization Bundle")]
	static void DownloadAndEmbedInitializationBundle()
	{
		//Pull the initialization bundle name from the current manifest
		AssetBundleManifest manifestV2 = new AssetBundleManifest();
		manifestV2.ReadAssetBundleManifestFileV2();
		
		foreach (string embeddedBundle in AssetBundleManager.embeddedBundlesList)
		{
			embedBundle(manifestV2, embeddedBundle);
		}
	}

	static void embedBundle(AssetBundleManifest manifestV2, string bundleName)
	{
		string fullBundleName =
			manifestV2.getFullBundleNameFromBaseBundleName(bundleName);
		string filePath = NGUILoader.HARDCODED_CDN + fullBundleName;

		//Using UnityWebRequest instead of WWW because without being able to yield, the WWW.isDone always returns false even with a progress of 1
		UnityWebRequest www = new UnityWebRequest(filePath);
		www.downloadHandler = new DownloadHandlerBuffer();
		www.SendWebRequest();
		DateTime start = System.DateTime.Now;;
			
		//Imposing a 300 second timeout limit here. The download usually only takes a few seconds so reaching a minute means we're probably in trouble
		while (!www.downloadHandler.isDone && (System.DateTime.Now - start).TotalSeconds < 300)
		{

		}

		if (www.downloadHandler.isDone)
		{
			if (www.isNetworkError || www.isHttpError)
			{
				throw new System.Exception("Error in downloading the " + bundleName +" bundle: " + www.error);
			}
			else
			{
				System.TimeSpan timeSpan = System.DateTime.Now - start;
				System.Console.WriteLine("Successfully downloaded the " + bundleName + " bundle. Writing to Streaming Assets now: " + timeSpan);
				string filePathToWriteTo = Path.Combine(Application.streamingAssetsPath, fullBundleName);
				File.WriteAllBytes(filePathToWriteTo, www.downloadHandler.data);
				www.Dispose();
			}
		}
		else
		{
			System.TimeSpan timeSpan = System.DateTime.Now - start;
			System.Console.WriteLine("Could not download " + bundleName + " bundle. Exceeded download time. Took " + timeSpan);
		}
	}
	
	/*
	 * Run this to download all the bundles from the current bundle manifest
	 * Useful if you want to test local bundles without building them yourself
	 */ 
	[MenuItem ("Zynga/BundlesV2/Download and Embed All Bundles")]
	static void DownloadAndEmbedAllBundles()
	{
		//Pull the initialization bundle name from the current manifest
		AssetBundleManifest manifestV2 = new AssetBundleManifest();
		manifestV2.ReadAssetBundleManifestFileV2();
		string localBundlePath = Application.dataPath + "/../../build/bundlesv2/" + AssetBundleManager.PLATFORM + "/";
		foreach (string bundleName in manifestV2.baseBundleNameToFullBundleNameDict.Values)
		{
			string filePathToWriteTo = Path.Combine(localBundlePath, bundleName);

			if (File.Exists(filePathToWriteTo))
			{
				continue;
			}

			if (bundleName.FastStartsWith("initialization"))
			{
				DownloadAndEmbedInitializationBundle();
				continue;
			}

			string url = NGUILoader.HARDCODED_CDN + bundleName;
			UnityWebRequest www = new UnityWebRequest(url);
			www.downloadHandler = new DownloadHandlerBuffer();
			www.SendWebRequest();
			DateTime start = System.DateTime.Now;
			//Imposing a 60 second timeout limit here. The download usually only takes a few seconds so reaching a minute means we're probably in trouble
			while (!www.downloadHandler.isDone && (System.DateTime.Now - start).TotalSeconds < 60)
			{

			}

			if (www.downloadHandler.isDone)
			{
				if (www.isNetworkError)
				{
					Debug.LogError("Error in downloading " + bundleName);
				}
				else
				{
					File.WriteAllBytes(filePathToWriteTo, www.downloadHandler.data);
				}
			}
		}
	}
	
	[MenuItem ("Zynga/Asset Checks/Check for Circular Bundle Dependencies")]
	static void circularBundleDependencyCheck()
	{
		CreateAssetBundlesV2.checkForCircularDependencies();
	}

	[MenuItem ("Zynga/Asset Checks/Check for Duplicate Assets In Bundles")]
	static void findDuplicateAssetsinBundles()
	{
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		
		string[] bundles = AssetDatabase.GetAllAssetBundleNames();
		Dictionary<string, List<string>> allDependencies = new Dictionary<string, List<string>>();
		for (int i = 0; i < bundles.Length; i++)
		{
			string bundle = bundles[i];
			string[] bundleAssets = AssetDatabase.GetAssetPathsFromAssetBundle(bundle); //Get all assets in bundle

			foreach (string assetPath in bundleAssets)
			{
				string[] assetDependencies = AssetDatabase.GetDependencies(assetPath); //Get dependencies for bundle asset
				for (int j = 0; j < assetDependencies.Length; j++)
				{
					string dependency = assetDependencies[j];
					if (dependency.FastEndsWith(".cs")) //Don't worry about code files being duplicated
					{
						continue;
					}

					if (!allDependencies.ContainsKey(dependency)) //Add this dependency to the dict if isn't there yet
					{
						allDependencies.Add(dependency, new List<string>()); 
					}

					if (!allDependencies[dependency].Contains(bundle)) //Add this bundle to the list of bundles pulling in this asset
					{
						allDependencies[dependency].Add(bundle);
					}
				}
			}
		}

		foreach (KeyValuePair<string, List<string>> kvp in allDependencies)
		{
			if (kvp.Value.Count > 1) //Don't worry about assets that are only used once in al the bundles
			{
				UnityEngine.Object obj = (UnityEngine.Object)AssetDatabase.LoadAssetAtPath(kvp.Key, typeof(UnityEngine.Object));
				long memorySize = Profiler.GetRuntimeMemorySizeLong(obj);
				if (memorySize < 500000) //Only worry about assets larger than 0.5 MB for right now
				{
					continue;
				}
				int mag = (int)Math.Max(0, Math.Log(memorySize, 1024));
				long convertedSize =(long)(memorySize / Math.Pow(1024, mag));
				string memorySuffix = "";
				switch (mag)
				{
					case 0:
						memorySuffix = "bytes";
						break;
					case 1:
						memorySuffix = "KBs";
						break;
					case 2:
						memorySuffix = "MBs";
						break;
					default:
						memorySuffix = "Not Supported: " + mag;
						Debug.LogErrorFormat("WARNING: asset {0} is too big {1} bytes", kvp.Key, memorySize); //non-fatal
						break;
				}

				string dependentBundlesString = "";
				foreach (string dependentBundle in kvp.Value)
				{
					if (!dependentBundlesString.IsNullOrWhiteSpace())
					{
						dependentBundlesString += ", ";
					}
					dependentBundlesString += dependentBundle;
				}

				string dupeFoundMessage = string.Format("DUPLICATE ASSET ~{0}~ WITH SIZE ~{2} {3}~ FOUND IN BUNDLES: {1}", kvp.Key, dependentBundlesString, convertedSize, memorySuffix);
				Debug.LogWarning(dupeFoundMessage);
			}
		}

		string finalString = string.Format("Finished finding duplicate assets in {0} seconds", stopwatch.Elapsed.TotalSeconds);
		Debug.Log(finalString);
	}

	static private string getBundleBuilderArgument(Dictionary<string,string> argsDict)
	{
		if (argsDict.ContainsKey("BuildBundles"))
		{
			return argsDict["BuildBundles"];
		}
		else
		{
			return "all";
		}
	}

	static void consoleBuildBundlesV2HIR_IOS()
	{
		Dictionary<string,string> args = CommandLineReader.GetCustomArguments(isRemoveEmptyArgs:true);
		string bundleBuildArg = getBundleBuilderArgument(args);

		CreateAssetBundlesV2.BuildBundles(SkuId.HIR, BuildTarget.iOS, new Variant[] { Variant.SD, Variant.HD }, bundleBuildArg );
	}
	
	static void consoleBuildBundlesV2HIR_Android()
	{
		Dictionary<string,string> args = CommandLineReader.GetCustomArguments(isRemoveEmptyArgs:true);
		string bundleBuildArg = getBundleBuilderArgument(args);

		// HD should be the last variant made, so a final app build that has any embedded resources will also be HD
		CreateAssetBundlesV2.BuildBundles(SkuId.HIR, BuildTarget.Android, new Variant[] { Variant.SD, Variant.HD }, bundleBuildArg );
	}

	static void consoleBuildBundleReportsV2HIR_Android()
	{
		CreateAssetBundlesV2.BuildBundles(SkuId.HIR, BuildTarget.Android, new Variant[] { Variant.SD, Variant.HD }, "all", true ); //Reports only!
	}

	static void consoleBuildBundlesV2HIR_Windows()
	{
		Dictionary<string,string> args = CommandLineReader.GetCustomArguments(isRemoveEmptyArgs:true);
		string bundleBuildArg = getBundleBuilderArgument(args);

		CreateAssetBundlesV2.BuildBundles(SkuId.HIR, BuildTarget.WSAPlayer, Variant.HD, bundleBuildArg);
	}
	
	static void consoleBuildBundlesV2HIR_WebGL()
	{
		Dictionary<string,string> args = CommandLineReader.GetCustomArguments(isRemoveEmptyArgs:true);
		string bundleBuildArg = getBundleBuilderArgument(args);

		CreateAssetBundlesV2.BuildBundles(SkuId.HIR, BuildTarget.WebGL, new Variant[] { Variant.SD }, bundleBuildArg);
	}

	[MenuItem ("Zynga/BundlesV2/PostBuildRestoreResourcesV2")]
	static void PostBuildRestoreResourcesV2()
	{
		// This use to do things when we had multisku support.
	}

#if UNITY_ANDROID
#if ZYNGA_KINDLE
	[MenuItem("Zynga/Build/Android/Toggle Kindle (currently ON)")]
#else
	[MenuItem("Zynga/Build/Android/Toggle Kindle (currently OFF)")]
#endif
#endif
	public static void menuBuildKindleToggle()
	{
		BuildTargetGroup gp = BuildTargetGroup.Android;
		string kindleSymbol = "ZYNGA_KINDLE";
		string gplaySymbol = "ZYNGA_GOOGLE";
		Debug.Log("Old defines: " + PlayerSettings.GetScriptingDefineSymbolsForGroup(gp));

		CommonEditor.ToggleScriptingDefineSymbolForGroup(kindleSymbol, gp);
		if (CommonEditor.IsScriptingDefineSymbolDefinedForGroup(kindleSymbol, gp))
		{
			CommonEditor.RemoveScriptingDefineSymbolForGroup(gplaySymbol, gp);
		}
		else
		{
			CommonEditor.AddScriptingDefineSymbolForGroup(gplaySymbol, gp);
		}

		Debug.Log("New defines: " + PlayerSettings.GetScriptingDefineSymbolsForGroup(gp));
	}

#if UNITY_ANDROID
#if ZYNGA_GOOGLE
	[MenuItem("Zynga/Build/Android/ZYNGA GOOGLE (currently ON)")]
#else
	[MenuItem("Zynga/Build/Android/ZYNGA GOOGLE (currently OFF)")]
#endif
#endif
	private static void dummyShowZyngaGoogleDef()
	{
		menuBuildKindleToggle();
	}
	#endregion
	
	#region Other public editor methods
	public static void cleanBuildDirectory()
	{
		cleanBuildPath(true, false, EditorUserBuildSettings.activeBuildTarget);
	}
	
	public static void cleanBuildBundles()
	{
		cleanBuildPath(false, true, EditorUserBuildSettings.activeBuildTarget);
	}
	
	public static void cleanBuildAll()
	{
		cleanBuildPath(true, true, EditorUserBuildSettings.activeBuildTarget);
	}
	#endregion

	#region Utility Methods

	/// Setup proper build folder structure if needed	
	private static void cleanBuildPath(bool cleanClient, bool cleanBundles, BuildTarget buildTarget)
	{
		string clientBuildPath = "";
		string bundleBuildPath = "";

		switch (buildTarget)
		{
			case BuildTarget.Android:
				clientBuildPath = ANDROID_CLIENT_BUILD_PATH;
				bundleBuildPath = ANDROID_BUNDLE_BUILD_PATH;
				break;
		
			case BuildTarget.iOS:
				clientBuildPath = IOS_CLIENT_BUILD_PATH;
				bundleBuildPath = IOS_BUNDLE_BUILD_PATH;
				break;

			case BuildTarget.WSAPlayer:
				clientBuildPath = WSA_CLIENT_BUILD_PATH;
				bundleBuildPath = WSA_BUNDLE_BUILD_PATH;
				break;
			
			case BuildTarget.WebGL:
				clientBuildPath = WEBGL_CLIENT_BUILD_PATH;
				bundleBuildPath = WEBGL_BUNDLE_BUILD_PATH;
				break;

			default:
				Debug.LogError(string.Format("Unsupported build target {0}!", buildTarget));
				return;
		}

		if (cleanClient)
		{
			if (Directory.Exists(clientBuildPath))
			{
				Debug.Log(string.Format("Deleting directory {0} recursively.", clientBuildPath));
				Directory.Delete(clientBuildPath, true);
			}
			else if (File.Exists(clientBuildPath))
			{
				Debug.Log(string.Format("Deleting file {0}.", clientBuildPath));
				File.Delete(clientBuildPath);
			}
		}

		if (cleanBundles)
		{
			if (Directory.Exists(bundleBuildPath))
			{
				Debug.Log(string.Format("Deleting directory {0} recursively.", bundleBuildPath));
				Directory.Delete(bundleBuildPath, true);
			}
			else if (File.Exists(bundleBuildPath))
			{
				Debug.Log(string.Format("Deleting file {0}.", bundleBuildPath));
				File.Delete(bundleBuildPath);
			}
		}
	}
	
	/// Setup proper build folder structure if needed
	private static void initBuildPath(bool cleanClient = true, bool cleanBundles = true, BuildTarget buildTarget = BuildTarget.iOS)
	{
		string clientBuildPath = "";
		string bundleBuildPath = "";
		
		switch (buildTarget)
		{
			case BuildTarget.Android:
				clientBuildPath = ANDROID_CLIENT_BUILD_PATH;
				bundleBuildPath = ANDROID_BUNDLE_BUILD_PATH;
				break;
			
			case BuildTarget.iOS:
				clientBuildPath = IOS_CLIENT_BUILD_PATH;
				bundleBuildPath = IOS_BUNDLE_BUILD_PATH;
				break;

			case BuildTarget.WSAPlayer:
				clientBuildPath = WSA_CLIENT_BUILD_PATH;
				bundleBuildPath = WSA_BUNDLE_BUILD_PATH;
				break;
			
			case BuildTarget.WebGL:
				clientBuildPath = WEBGL_CLIENT_BUILD_PATH;
				bundleBuildPath = WEBGL_BUNDLE_BUILD_PATH;
				break;

			default:
				Debug.LogError(string.Format("Unsupported build target {0}!", buildTarget));
				return;
		}

		cleanBuildPath(cleanClient, cleanBundles, buildTarget);
		
		if (!Directory.Exists(BUILD_ROOT_PATH))
		{
			Directory.CreateDirectory(BUILD_ROOT_PATH);
		}
		
		if (!Directory.Exists(clientBuildPath))
		{
			Directory.CreateDirectory(clientBuildPath);
		}

		if (!Directory.Exists(bundleBuildPath))
		{
			Directory.CreateDirectory(bundleBuildPath);
		}
	}

	/// <summary>
	/// Builds the Unity player binary using the asset bundle config in order to build a smaller size app package
	/// compatible with downloaded asset bundles.
	/// </summary>
	/// <remarks>
	/// If the config is top-level enabled, any asset folders listed as belonging to an asset bundle will be moved out
	/// and not built into the player binary resources data.  Additionally, if "unused_sound_folders_out" is enabled in
	/// the "move_assets_out_of_resources" section of the config, asset folders in Resources/Sounds will be moved out and
	/// not built in, except for those folders listed in "common_sound_folders", and if "unused_asset_folders_out" is
	/// enabled, any folders listed under "unused_asset_folders" will also be moved out and not built in.
	/// </remarks>
	private static void buildPlayerWithAssetBundleConfigs(BuildOptions buildOptions = BuildOptions.None, BuildTarget buildTarget = BuildTarget.iOS)
	{
		string clientBuildPath = "";
		switch (buildTarget)
		{
			case BuildTarget.Android:
				clientBuildPath = ANDROID_CLIENT_APK_PATH;
				break;

			case BuildTarget.iOS:
				clientBuildPath = IOS_CLIENT_BUILD_PATH;
				// Needs to be set for AdColonyPostProcessBuild processing.
				EditorUserBuildSettings.SetBuildLocation(BuildTarget.iOS, IOS_CLIENT_BUILD_PATH);
				break;

			case BuildTarget.WSAPlayer:
				clientBuildPath = WSA_CLIENT_BUILD_PATH;
				// Needs to be set for AdColonyPostProcessBuild processing.
				EditorUserBuildSettings.SetBuildLocation(BuildTarget.WSAPlayer, WSA_CLIENT_BUILD_PATH);
				break;
			
			case BuildTarget.WebGL:
				clientBuildPath = WEBGL_CLIENT_BUILD_PATH;
				EditorUserBuildSettings.SetBuildLocation(BuildTarget.WebGL, WEBGL_CLIENT_BUILD_PATH);
				break;

			default:
				Debug.LogError(string.Format("Unsupported build target {0}!", buildTarget));
				return;
		}

		initBuildPath(true, false, buildTarget);

		_checkAndSwitchBuildTarget(buildTarget);
		
		AssetDatabase.Refresh();
			
		Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildMobileScenes, clientBuildPath, buildTarget, buildOptions);

		logBuildReport("buildPlayerWithAssetBundleConfigs", report);
	}

	private static void logBuildReport(string buildName, Reporting.BuildReport report)
	{
		System.Text.StringBuilder output = new System.Text.StringBuilder();
		Reporting.BuildSummary summary = report.summary;
		output.AppendLine("");
		output.AppendFormat("BUILD COMPLETED: {0} for {1} at {2} after {3}: {4} errors, {5} warnings\n",
							buildName, summary.platform, CommonText.formatDateTime(summary.buildEndedAt),
							CommonText.formatTimeSpan(summary.totalTime), summary.totalErrors, summary.totalWarnings);
		output.AppendFormat("BUILD RESULT: {0}\n", summary.result);

		foreach(Reporting.BuildStep step in report.steps)
		{
			for (int i=0; i <= step.depth; i++)
			{
				output.Append("  ");
			}
			output.AppendFormat("Step {0} took {1}\n", step.name, CommonText.formatTimeSpan(step.duration));
		}

		Debug.Log(output.ToString());
		if (summary.result == Reporting.BuildResult.Failed)
		{
			throw new System.Exception("BUILD FAILED");
		}
	}

#if !ZYNGA_PRODUCTION
	// Open up network security config options for debug configs to allow charles proxy.
	private static void patchAndroidManifestForCharlesProxy()
	{
		// Add android:networkSecurityConfig to <application> attributes pointing to "Unity/Assets/Plugins/Android/res/xml/network_security_config.xml".
		string pluginPath = Path.Combine(Application.dataPath, "Plugins/Android");
		string configFileBasename = "network_security_config";
		string securityConfigResPath = $"res/xml/{configFileBasename}.xml";
		string securityConfigPath = Path.Combine(pluginPath, securityConfigResPath);
		string manifestPath = Path.Combine(pluginPath, "AndroidManifest.xml");
		if (!File.Exists(manifestPath) || !File.Exists(securityConfigPath))
		{
			Debug.Log($"Cannot enable Charles due to missing file. {manifestPath}_exists={File.Exists(manifestPath)} {securityConfigPath}_exists={File.Exists(securityConfigPath)}");
			return;
		}

		Debug.Log("Adding attribute android:networkSecurityConfig to AndroidManifest.xml for Charles proxy debugging.");
		XmlDocument manifestDoc = new XmlDocument();
		manifestDoc.Load(manifestPath);
		XmlElement manifestRoot = manifestDoc.DocumentElement;
		XmlNode applicationNode = null;

		// Let's find the application node.
		foreach(XmlNode node in manifestRoot.ChildNodes)
		{
			if (node.Name == "application")
			{
				applicationNode = node;
				break;
			}
		}
		
		if (applicationNode == null)
		{
			Debug.LogError($"Could not find application node in AndroidManifest.xml at {manifestPath}");
			return;
		}
		
		bool foundNetSecAttrib = false;
		foreach (XmlAttribute attribute in applicationNode.Attributes)
		{
			if (attribute.Name == "android:networkSecurityConfig")
			{
				foundNetSecAttrib = true;
				break;
			}
		}
		
		if (foundNetSecAttrib)
		{
			Debug.Log("Attribute android:networkSecurityConfig already exists, skipping.");
			return;
		}
		
		XmlAttribute attr = manifestDoc.CreateAttribute("android", "networkSecurityConfig", "http://schemas.android.com/apk/res/android");
		attr.Value = $"@xml/{configFileBasename}";
		applicationNode.Attributes.Append(attr);
		applicationNode.Prefix = "android";
		manifestDoc.Save(manifestPath);
		
		Debug.Log("Added android:networkSecurityConfig attribute to AndroidManifest.xml application node to enable Charles proxy debugging.");
	}
#endif	// !ZYNGA_PRODUCTION

	#endregion
	
	#region Console Methods
	/// Provided so that it can be called from the build server.
	public static void consoleDoNothing()
	{
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
	}

	public static void consoleSetTargetIOS()
	{
		_consoleSetTarget(BuildTarget.iOS);
	}

	public static void consoleSetTargetAndroid()
	{
		_consoleSetTarget(BuildTarget.Android);
	}

	public static void consoleSetTargetWindows()
	{
		_consoleSetTarget(BuildTarget.WSAPlayer);
	}
	
	public static void consoleSetTargetWebGL()
	{
		_consoleSetTarget(BuildTarget.WebGL);
	}

	private static BuildTargetGroup getTargetGroupForBuildTarget(BuildTarget target)
	{
		switch (target)
		{
			case BuildTarget.iOS:
				return BuildTargetGroup.iOS;
			case BuildTarget.Android:
				return BuildTargetGroup.Android;
			case BuildTarget.WSAPlayer:
				return BuildTargetGroup.WSA;
			case BuildTarget.WebGL:
				return BuildTargetGroup.WebGL;
			default:
				Debug.LogErrorFormat("Unknown/unsupported BuildTarget {0}", target);
				break;
		}
		return BuildTargetGroup.Unknown;
	}

	private static void _checkAndSwitchBuildTarget(BuildTarget target)
	{
		if (EditorUserBuildSettings.activeBuildTarget != target)
		{
			EditorUserBuildSettings.SwitchActiveBuildTarget(getTargetGroupForBuildTarget(target), target);
		}
	}

	private static void _consoleSetTarget(BuildTarget target)
	{
		System.DateTime startTime = System.DateTime.Now;

		_checkAndSwitchBuildTarget(target);

		// Force script recompilation.  Need this to force Unity to pick up changes made to preprocessor defines.
		// Simply changing modification times doesn't work.
		string assetPath = "Assets/Code/Editor/Build.cs"; // Reimport myself, since I know I exist.
		MonoScript script = AssetDatabase.LoadAssetAtPath(assetPath, typeof(MonoScript)) as MonoScript;
		if (script != null)
		{
			AssetDatabase.ImportAsset(assetPath);
		}

		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

		System.TimeSpan timeSpan = System.DateTime.Now - startTime;
		Debug.LogFormat("BUILDTIME: ImportAssets: consoleSetTarget {3} asset import complete: duration: {0} hours, {1} min, {2} sec",timeSpan.Hours,timeSpan.Minutes,timeSpan.Seconds, target.ToString());
	}

	// version that reads cmdline params
	private static void consoleBuildPlayerCombined()
	{
		BuildTarget buildTgt = BuildTarget.iOS;
		BuildOptions buildOptions = BuildOptions.None;

		// Note: GetCustomArguments expects args packaged as a string with prefix -CustomArgs:
		// example: -executeMethod CommandLineHelpers.trampHIR "-CustomArgs:branchName=$(git symbolic-ref HEAD);testFile=/Users/pludington/hir-client-mobile/Unity/Assets/Code/Automation/allGames.json;timeScale=1;testMemory=true;"
		Dictionary<string,string> cmdLineArgsDict = CommandLineReader.GetCustomArguments(isRemoveEmptyArgs:true);
		
		if (cmdLineArgsDict.ContainsKey("Target"))
		{
			string targetPlatform = cmdLineArgsDict["Target"];
			// target string values used by Makefile
			switch(targetPlatform)
			{
				case "Android":
					buildTgt = BuildTarget.Android;
					break;
				case "IOS":
					buildTgt = BuildTarget.iOS;
					break;
				case "Windows":
					buildTgt = BuildTarget.WSAPlayer;
					break;
				case "WebGL":
					buildTgt = BuildTarget.WebGL;
					break;
			}
		}

		// Always generate debug symbols. These are generated in an output file and can be used to demangle
		// production call stacks.
		PlayerSettings.WebGL.debugSymbols = true;
		if (cmdLineArgsDict.ContainsKey("UnityDevBuild") && cmdLineArgsDict["UnityDevBuild"] == "true")
		{
			buildOptions |= BuildOptions.Development;	// enum bitflags
			if (buildTgt == BuildTarget.WebGL)
			{
				PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; //Development builds are always uncompressed
				PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
			}
		}
		if (cmdLineArgsDict.ContainsKey("UnityAllowDebugging") && cmdLineArgsDict["UnityAllowDebugging"] == "true")
		{
			buildOptions |= BuildOptions.AllowDebugging;	// enum bitflags
		}
		if (cmdLineArgsDict.ContainsKey("UnityAllowProfiling") && cmdLineArgsDict["UnityAllowProfiling"] == "true")
		{
			buildOptions |= BuildOptions.ConnectWithProfiler;    // enum bitflags
		}
#if !ZYNGA_PRODUCTION
		bool shouldEnableCharles =
			cmdLineArgsDict.ContainsKey("EnableCharles") && cmdLineArgsDict["EnableCharles"] == "true";
		Debug.Log($"Checking if Charles should be enabled. buildTgt={buildTgt} shouldEnableCharles={shouldEnableCharles} EnableCharlesParam_exists={cmdLineArgsDict.ContainsKey("EnableCharles")}");
		if (shouldEnableCharles && buildTgt == BuildTarget.Android)
		{
			patchAndroidManifestForCharlesProxy();
		}
#endif
		if (cmdLineArgsDict.ContainsKey("UnityAndroidBuildGradle") && cmdLineArgsDict["UnityAndroidBuildGradle"] == "true")
		{
			EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
			// Signing setup is mandatory for gradle builds.
			string keyStoreFile, keyName;
			cmdLineArgsDict.TryGetValue("SigningKeystore", out keyStoreFile);
			cmdLineArgsDict.TryGetValue("SigningKeyname", out keyName);
			PlayerSettings.Android.keystoreName = keyStoreFile; // Refers to the file name.
			PlayerSettings.Android.keyaliasName = keyName; // Refers to the key name.
			if (keyName == "androiddebugkey")
			{
				PlayerSettings.Android.keystorePass = "android";
				PlayerSettings.Android.keyaliasPass = "android";
			}
			else
			{
				string releaseKeyFilePassword = "###NEWTOY_APK_SIGNING_STOREPASS###";
				string releaseKeyNamePassword = "###NEWTOY_APK_SIGNING_STOREPASS###";
				PlayerSettings.Android.keystorePass = releaseKeyFilePassword;
				PlayerSettings.Android.keyaliasPass = releaseKeyNamePassword;
			}
		}
		else
		{
			EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
		}

		EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

		bool shouldMakeUnityDebugBuild = cmdLineArgsDict.ContainsKey("UnityMakeDebugBuild") &&
		                                 cmdLineArgsDict["UnityMakeDebugBuild"] == "true";
		Debug.Log($"Checking if UnityMakeDebugBuild is present. shouldMakeUnityDebugBuild={shouldMakeUnityDebugBuild} UnityMakeDebugBuild_exists={cmdLineArgsDict.ContainsKey("UnityMakeDebugBuild")}");
		
		if (shouldMakeUnityDebugBuild)
		{
			EditorUserBuildSettings.androidBuildType = AndroidBuildType.Debug;
		}

		// On Android Adjust SDK has a buggy postprocess task to fixup the AndroidManifest.xml; disable it since we already
		// have ours properly setup.  On ios we need to run this to add frameworks to the build.  
		if (buildTgt == BuildTarget.Android)
		{
			if (AdjustEditor.getPostProcessingStatus())
			{
				AdjustEditor.TogglePostProcessingStatus();	
			}
		}
		else
		{
			if (!AdjustEditor.getPostProcessingStatus())
			{
				AdjustEditor.TogglePostProcessingStatus();
			}
		}


		if (buildTgt == BuildTarget.iOS)
		{
			if (!AdjustEditor.isIos14SupportEnabled())
			{
				AdjustEditor.ToggleiOS14SupportStatus();
			}
		}
			


		

		buildPlayerWithAssetBundleConfigs(buildOptions, buildTarget:buildTgt);
	}

	#endregion
}


