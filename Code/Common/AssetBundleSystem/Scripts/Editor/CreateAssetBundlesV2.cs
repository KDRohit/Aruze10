//
//	CreateAssetBundlesV2.cs
//
//	Version 2 of "CreateAssetBundles.cs" - Overhauled to work with Unity 5.x's new Bundle Building Pipeline
//	The primary differences are:
//		Bundles are tagged in the editor with a "BundleName" tag
//		We've added labels to indicate what SKU a bundle belongs
//		Because we can now load assets outside of resource folders, we no longer move assets around (faster and non-destructive)
//		The build system supports fast minimal/incremental builds (< 1 minute if your existing asset bundles are up to date)
//		...
// -Kevin Kralian

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public class CreateAssetBundlesV2
{
    // =============================
    // CONST
    // =============================
    public static bool TEST_BUILD = false;

    //==== The "Variant" versions of paths/folders/filenames are used temporarily while bundle building

	static string getVariantBuildFolder(SkuId sku, BuildTarget target, Variant variant)
	{
		// for example:  ../build/bundlesv2/temp/temp-hir-android-hd
		return "../build/bundlesv2/temp/temp-" + sku.ToString().ToLower() + "-" + target.ToString().ToLower() + "-" + variant.ToString().ToLower();
	}

	static string getVariantManifestFileName(SkuId sku, BuildTarget target, Variant variant, string fileOverride = "")
	{
		// for example:  bundleContentsV2_android_hd.txt
		if (string.IsNullOrEmpty(fileOverride))
		{
			return AssetBundleManifest.BUNDLE_MANIFEST_FILE_V2 + "_" + variant.ToString().ToLower() + ".txt";
		}
		
		return fileOverride + "_" + AssetBundleManager.PLATFORM + "_" + variant.ToString().ToLower() + ".txt";
	}

	static string getVariantManifestFilePath(SkuId sku, BuildTarget target, Variant variant)
	{
		// for example:  ../build/bundlesv2/temp/temp-hir-android-hd/bundleContentsV2_android_hd.txt
		return getVariantBuildFolder(sku,target,variant) + "/" + getVariantManifestFileName(sku,target,variant);
	}

	//==== The "Final" versions of paths/folders/filenames are the final output folders for bundle building

	static string getFinalOutputFolder(SkuId sku, BuildTarget target)
	{
		// for example:  ../build/bundlesv2/android
		return "../build/bundlesv2/" + target.ToString().ToLower();
	}

	static string getFinalManifestFilePath(SkuId sku, BuildTarget target, string fileOverride = "")
	{
		if (string.IsNullOrEmpty(fileOverride))
		{
			return getFinalOutputFolder(sku, target) + "/" + AssetBundleManifest.BUNDLE_MANIFEST_FILE_V2 + ".txt";
		}

		return getFinalOutputFolder(sku, target) + "/" + fileOverride + "_" + AssetBundleManager.PLATFORM + ".txt";
	}

	//==== The "Embedded" paths are used to copy files into Unity's /Resources/ Folder to get embedded with the app

	static string getEmbeddedResourcesFolder(SkuId sku, BuildTarget target)
	{
		// for example:  /Data/HIR/Resources
		return Application.dataPath + "/Data/" + sku.ToString().ToUpper() + "/Resources";
	}

	static string getEmbeddedManifestFilePath(SkuId sku, BuildTarget target)
	{
		// for example:  /Data/HIR/Resources/bundleContentsV2_android_hd.txt
		return getEmbeddedResourcesFolder(sku, target) + "/" + AssetBundleManifest.BUNDLE_MANIFEST_FILE_V2 + ".txt";
	}

	// The jenkins logging/artifacts folder. Anything put here will show up as a jenkins artifacts.
	static string getLogFolder()
	{
		return Build.BUILD_ROOT_PATH + "/logs";
	}

	// BuildBundles is now split into multiple (internal) steps
	//   First - For each variant, build a bundleset for the specific sku-target-variant into an intermediate build folder
	//           (bundles and single-variant manifest only; no copying/renaming/embedding of files at this point)
	//
	//   Second  We merge the resulting variant-specific manifests into a single combined manifest
	//
	//   Third   Using combined manifest, we copy/rename/deploy/embed various bundle sets into output locations
	//           (ie: we have one output folder for all android bundles ready to copy to s3,
	//            we copy a manifest file and shader bundle into the game /resources/ folder, etc)

	public static bool BuildBundles(SkuId sku, BuildTarget target, Variant[] variants, string whatBundles = "all", bool makeReportsInsteadOfBundles = false, string manifestNameOverride = "")
	{
		var startTime = System.DateTime.Now;
			
		// We can make bundles, reports, or both. Set these separately for clarity...
		bool makeReports = makeReportsInsteadOfBundles;
		bool makeBundles = !makeReportsInsteadOfBundles;	
		
		// First, build each desired bundle variant...
		foreach (var variant in variants)
		{
			bool success = BuildBundleVariant(sku, target, variant, makeBundles, makeReports, whatBundles);
			if (!success)
			{
				fatalError("BuildBundleVariant failed for: " + sku + " - " + target + " - " + variant);
			}
		}

		// Restore default importers after building all variants so embedded assets are always the default "HD" content
		// Bennett: commented out as no longer needed, HD switch will reset the imports.
		//BuildVariant.restoreAllTextureImporters();

		// Combine the variant-specific manifests into one, and copy everything to output folders...
		if (makeBundles)
		{
			bool mergeWithOldManifest = whatBundles != "all" && string.IsNullOrEmpty(manifestNameOverride);
			mergeManifestsAndCopyBundles(sku, target, variants, mergeWithOldManifest, manifestNameOverride);
		}

		// Generate an optional per-bundle texture report, for now, to the jenkins /logs/ folder
		// Only emit one per builds, since this doesn't change per-variant (like textureReports do)
		if (makeReports)
		{
			emitCrossGameAssetReportFor(sku, target, "Games", "Assets/Data/Games");
		}

		System.TimeSpan timeSpan = System.DateTime.Now - startTime ;
		Debug.LogFormat("COMPLETED CreateAssetBundlesV2.BuildBundles(" + variants.Length + " variants), duration: " + timeSpan.ToString() );

		return true;
	}

	// alternate entry point with a single variant
	public static bool BuildBundles(SkuId sku, BuildTarget target, Variant variant, string whatBundles = "all", bool makeReportsInsteadOfBundles = false)
	{
		var variants = new Variant[] { variant };
		return BuildBundles(sku, target, variants, whatBundles, makeReportsInsteadOfBundles);
	}


	// Sets up for and builds a single variant
	static bool BuildBundleVariant(SkuId sku, BuildTarget target, Variant variant, bool makeBundles, bool makeReports, string whatBundles)
	{
		System.DateTime startTime = System.DateTime.Now;

		if (!BuildVariant.isVariantAllowedForTargetPlatform(variant, target))
		{
			fatalError("Unsupported Variant " + variant + " for target " + target);
			return false;
		}

		if (sku != SkuId.HIR)
		{
			fatalError("Unsupported SKU: " + sku);
			return false;
		}

		if (!(target == BuildTarget.iOS || target == BuildTarget.Android || target == BuildTarget.WSAPlayer || target == BuildTarget.WebGL))
		{
			fatalError("Unsupported build target: " + target);
			return false;
		}

		// This will restore any backed up default meta files, then apply variant overrides to the meta files
		BuildVariant.setupTextureVariant(sku, target, variant);

		// Get some paths & folders to build to
		var variantBuildFolder = getVariantBuildFolder(sku, target, variant);

		// Create output folders (if needed)
		Directory.CreateDirectory(variantBuildFolder);

		// Clean output folder? (do we need to?)

		// Get all the bundle names that are also labeled for our SKU
		var bundleNames = getAssetBundleNamesForSku(sku);

		bundleNames = filterAssetBundleNames(bundleNames, whatBundles);

		System.Array.Sort(bundleNames);
		Debug.Log("Found " + bundleNames.Length + " filtered bundle names: " + string.Join(", ", bundleNames)); // ie: satc01, satc02, wonka01, etc.
		if (bundleNames.Length == 0)
		{
			fatalError("ERROR: found 0 tagged asset bundles with skuLabel '" + sku + "' applied. ABORTING");
			return false;
		}

        // only build 5 bundles for testing
        if (TEST_BUILD)
        {
            bundleNames = new string[]{ bundleNames[0], bundleNames[1], bundleNames[2], bundleNames[3], bundleNames[4], "initialization" };
        }

		// Setup lists of bundleNames and their assets for the BuildPipeline
		var bundleNamesList = new List<string>();
		var assetBundles = new List<AssetBundleBuild>();
		foreach(var bundleName in bundleNames)
		{
			var assetBundle = new AssetBundleBuild();
			assetBundle.assetBundleName = bundleName + AssetBundleManager.bundleV2Extension;
			assetBundle.assetNames = AssetDatabase.GetAssetPathsFromAssetBundle( bundleName );

			// Only keep bundles that contain assets
			if (assetBundle.assetNames.Length > 0)
			{
				assetBundles.Add( assetBundle );
				bundleNamesList.Add( bundleName );
			}
			else
			{
				Debug.LogError("WARNING: bundle '" + bundleName + "' contains no assets; skipping"); //non-fatal
			}
		}

		// update bundleNames; empty-bundles now removed
		bundleNames = bundleNamesList.ToArray();

		// Verify there are no top-level assets with name collisions after we shorten their paths & drop their file extensions
		{
			var allAssets = assetBundles.SelectMany(x => x.assetNames).Select(path => AssetBundleMapping.longAssetPathToShortPath(path)).ToArray();
			bool hasDuplicates = (allAssets.Distinct().Count() != allAssets.Count());
			if (hasDuplicates) 
			{
				var dups = allAssets.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
				fatalError("ERROR: Duplicate asset-paths detected in bundleContents (remember file extensions are stripped): \n  " + string.Join("\n  ", dups));
			}
		}


		// CREATE THE BUNDLES
		Debug.Log("calling BuildPipeline.BuildAssetBundles...");
		UnityEngine.AssetBundleManifest buildResults = BuildPipeline.BuildAssetBundles
		(
			variantBuildFolder, 
			assetBundles.ToArray(),
			makeBundles ? BuildAssetBundleOptions.None : BuildAssetBundleOptions.DryRunBuild, 
			target
		);

		if (buildResults == null)
		{
			fatalError(string.Format("Error: BuildPipeline.BuildAssetBundles failed. Destination:'{0}', BundleCount:{1}, MakeBundles:{2}", variantBuildFolder, assetBundles.Count, makeBundles));
			return false;
		}

		// Need a 1-to-1 correspondence of our bundlenames to the list of bundle filenames returned by the BuildPipeline
		// Ugh! AssetDatabase.GetAllAssetBundleNames and BuildPipeline.BuildAssetBundles return differently sorted lists (numbers vs '_' underscore)
		// So we sort them both (ideally the buildResults would provide a list of assets for each bundle)
		string [] builtBundles = buildResults.GetAllAssetBundles();

		// Additional sorting problems due to returned names having _hash appended, use custom comparer to sort on base-name only
		System.Array.Sort(builtBundles, new CustomComparer());

		if (bundleNames.Length != builtBundles.Length) 
		{
			logMismatchedBundles(builtBundles, bundleNames);	
			fatalError("ERROR: bundleNames.Length != manifest.GetAllAssetBundles().Length; ABORTING");
			return false;
		}

		bool namesMatch = Enumerable.Range(0, bundleNames.Length).All( i => builtBundles[i].StartsWith(bundleNames[i]) );
		if (!namesMatch)
		{
			Debug.LogError("ERROR: provided bundle names don't appear to match order of resulting bundle names; ABORTING");
			var mismatchedNames = Enumerable.Range(0, bundleNames.Length)
				.Where( i => !builtBundles[i].StartsWith(bundleNames[i]) )
				.Select( i => bundleNames[i] + " != " + builtBundles[i] ).ToArray();
			fatalError("mismatchedNames = \n" + string.Join("\n", mismatchedNames) );
			return false;
		}

		// Check bundle-dependencies for circular dependencies; we don't allow those
		if (checkForCircularDependencies( buildResults ))
		{
			fatalError("Bundles have circular dependencies! ABORTING");
			return false;
		}

		//......... Setup Bundle Manifest Structure .........

		// Create our bundle -> asset mappings (fix Unity project-relative path to use shorter resource-relative path)
		var bundleContents = new Dictionary<string, string[]>();
		for(int i=0; i < builtBundles.Length; i++)
		{
			bundleContents[ builtBundles[i] ] = assetBundles[i].assetNames.Select(path => AssetBundleMapping.longAssetPathToShortPath(path)).ToArray();
		}

		// Create our (sparse) mapping of bundle-dependencies
		var bundleDependencies = new Dictionary<string, string[]>();
		for(int i=0; i < builtBundles.Length; i++)
		{
			if (buildResults.GetAllDependencies(builtBundles[i]).Length > 0)
			{
				bundleDependencies[ builtBundles[i] ] = buildResults.GetAllDependencies(builtBundles[i]);
			}
		}

		// Build a mapping table for:  bundlename -> bundlename-<variant>-<crc>-sz<filesize>.bundlev2
		Debug.Log("Creating BundleName Mappings...");
		Dictionary<string, string> bundleNameMappings = new Dictionary<string, string>();
		StringBuilder remappingLog = new StringBuilder(builtBundles.Length * 100);
		string v2Extension = AssetBundleManager.bundleV2Extension;
		foreach(var bundleName in builtBundles) //doesnt include file extensions
		{
			var bundlePath = variantBuildFolder + "/" + bundleName;
			uint crc = makeBundles ? getBundleCrc(bundlePath) : 0; 
			long filesize = makeBundles ? new FileInfo(bundlePath).Length : 0;

			// ie: wonka01.bundlev2   ==>  wonka01-hd-0e1d73ba-sz13434.bundlev2
			var newBundleName = bundleName.Replace(v2Extension, string.Format("-{0}-{1:x8}{2}{3}{4}", variant.ToString().ToLower(), crc, AssetBundleManager.bundleSizePrefix, filesize, v2Extension));
			bundleNameMappings[bundleName] = newBundleName;

			remappingLog.AppendFormat("Mapped {0} --> {1}\n", bundleName, newBundleName);

			//Copy the bundles in the embedded_bundles.txt file into the StreamingAssets folder so they are included with the built player
			//We don't want to need to download these bundles in game
			//Mobile platforms only use the HD version of the bundle, even when SD is the better variant, to keep their download sizes down & WebGl only ever uses SD to we can safely assume we need to embed that version.
			
			if ( makeBundles && (variant == Variant.HD && target != BuildTarget.WebGL) || (variant == Variant.SD && target == BuildTarget.WebGL))
			{
				foreach (string embeddedBundleName in AssetBundleManager.embeddedBundlesList)
				{
					string sourceFile = embeddedBundleName + AssetBundleManager.bundleV2Extension;
					if (sourceFile == bundleName)
					{
						string sourceBundlePath = variantBuildFolder + "/" + sourceFile;
						if (File.Exists(sourceBundlePath))
						{
							Debug.Log("Copying embedded bundle " + sourceBundlePath);
							File.Copy(sourceBundlePath, Path.Combine(Application.streamingAssetsPath, newBundleName), true);
							
						}
						else
						{
							fatalError("Couldn't find the bundle to embed in Streaming Assets " + bundleName);
							return false;
						}
					}
				}
			}
		}
		Debug.Log(remappingLog.ToString());   // print the whole thing as one string in Log for better readabilty
		var variantBundleMappings = new Dictionary<string, Dictionary<string, string>>();
		variantBundleMappings[variant.ToString()] = bundleNameMappings;


		// create a manifest wrapper object, convert to json, write to manifest file
		var manifest = new AssetBundleManifestWrapper(bundleContents, bundleDependencies, variantBundleMappings);
		string manifestFilePath = getVariantManifestFilePath(sku, target, variant);
		File.WriteAllText(manifestFilePath, manifest.convertToJson());

		// Print bundle size report (only works on actual runs that create actual bundles)
		if (makeBundles)
		{
			printBundleSizeReport(target + "-" + sku + "-" + variant, builtBundles, variantBuildFolder);
		}

		if (makeReports)
		{
			// Generate an optional per-bundle texture report, for now, to the jenkins /logs/ folder
			emitTextureReportFor(sku, target, variant, "Games", "Assets/Data/Games");
			emitTextureReportFor(sku, target, variant, "Other", "Assets/Data/HIR");

			// Write the dependencies as an informative diagnostics file, to the jenkins /logs/ folder
			string dependencyInfo = Zynga.Core.JsonUtil.Json.SerializeHumanReadable(bundleDependencies);
			File.WriteAllText( getLogFolder() + "/BundleDependencies.txt", dependencyInfo);
		}

		System.TimeSpan timeSpan = System.DateTime.Now - startTime ;
		Debug.LogFormat("COMPLETED CreateAssetBundlesV2.BuildBundleVariant(...), duration: " + timeSpan.ToString() );
		return true;
	}

	static void logMismatchedBundles(string[] builtBundles, string[] bundleNames)
	{
		string[] modifiedBuiltStrings = new string[builtBundles.Length];
		
		//make a copy of the built bundles so we don't modify original array
		builtBundles.CopyTo(modifiedBuiltStrings, 0);
		
		//remove the .bundlev2 extension
		for (int i = 0; i < modifiedBuiltStrings.Length; i++)
		{
			if (string.IsNullOrEmpty(modifiedBuiltStrings[i]))
			{
				continue;
			}
			string extension = System.IO.Path.GetExtension(modifiedBuiltStrings[i]);
			if (!string.IsNullOrEmpty(extension))
			{
				modifiedBuiltStrings[i] = modifiedBuiltStrings[i].Replace(extension, "");	
			}
		}
		
		int expectedBundlesIndex = 0;
		int builtBundleIndex = 0;
		bool finished = false;
		while (!finished)
		{
			int compareValue = modifiedBuiltStrings[builtBundleIndex].CompareTo(bundleNames[expectedBundlesIndex]);
			if (compareValue == 0)
			{
				//names are equal
				++builtBundleIndex;
				++expectedBundlesIndex;
			}
			else if (compareValue < 0)
			{
				//built bundle is alphabetically before expected bundle, we have an additional bundle
				Debug.LogError("UNEXPECTED BUILT BUNDLE: " + modifiedBuiltStrings[builtBundleIndex]);
				++builtBundleIndex;
			}
			else
			{
				//built bundle is alphabetically after expected bundle, we skipped a bundle
				Debug.LogError("SKIPPED BUILDING BUNDLE: " + bundleNames[expectedBundlesIndex]);
				++expectedBundlesIndex;

			}
				
			finished = builtBundleIndex >= modifiedBuiltStrings.Length ||
			           expectedBundlesIndex >= bundleNames.Length;
		}

		if (builtBundleIndex >= modifiedBuiltStrings.Length && expectedBundlesIndex < bundleNames.Length)
		{
			for (int i = expectedBundlesIndex; i < bundleNames.Length; i++)
			{
				Debug.LogError("SKIPPED BUILDING BUNDLE: " + bundleNames[i]);
			}
		}

		if (expectedBundlesIndex >= bundleNames.Length && builtBundleIndex < modifiedBuiltStrings.Length)
		{
			for (int i = builtBundleIndex; i < modifiedBuiltStrings.Length; i++)
			{
				Debug.LogError("UNEXPECTED BUILT BUNDLE: " + modifiedBuiltStrings[i]);
			}
		}
	}

	static void emitTextureReportFor(SkuId sku, BuildTarget target, Variant variant, string name, string path)
	{
		string csvReport;
		string textureReport = "Texture report for " + sku + "-" + target + "-" + variant + " (" + name + ") " + "starting at: " + path + "\n\n" + 
			TextureReporter.generateTextureReportForBundlesInPath(path, out csvReport);

		// Copy report to /logs/, so it shows up in the jenkins build artifacts. A dev can copy-pasta it if he wants.
		Directory.CreateDirectory(getLogFolder());
		File.WriteAllText( getLogFolder() + "/TextureReport-" + name + "-" + target + "-" + variant + ".txt", textureReport );

		// And emit a CSV of the summaries to better track this data over time...
		string today = System.DateTime.Today.ToString("yyyy-MM-dd");
		File.WriteAllText( getLogFolder() + "/TextureSummary-" + name + "-" + target + "-" + variant + "-" + today + ".csv", csvReport );
	}

	static void emitCrossGameAssetReportFor(SkuId sku, BuildTarget target, string name, string path)
	{
		var crossGameAssetReport = "Cross-Game Asset report for " + sku + "-" + target + " (" + name + ") " + "starting at: " + path + "\n\n" + 
			AssetTreeReporter.generateCrossGameAssetReportForBundlesInPath(path);

		// Copy report to /logs/, so it shows up in the jenkins build artifacts. A dev can copy-pasta it if he wants.
		Directory.CreateDirectory(getLogFolder());
		File.WriteAllText( getLogFolder() + "/CrossGameReport-" + name + "-" + target + ".txt", crossGameAssetReport);
	}


	// Copy & rename all bundles, manifest to where they need to go
	public static void mergeManifestsAndCopyBundles(SkuId sku, BuildTarget target, Variant[] variants, bool mergeWithOldManifest, string manifestNameOverride)
	{
		// Create output folder
		var finalOutputFolder = getFinalOutputFolder(sku, target);
		Directory.CreateDirectory(finalOutputFolder);

		// Merge the previously built variant-specific manifests into one, and save it
		var mergedManifest = mergeVariantManifests(sku, target, variants);
		var finalManifestFilePath = getFinalManifestFilePath(sku, target, manifestNameOverride);
		File.WriteAllText(finalManifestFilePath, mergedManifest.convertToJson());

		// Deploy all variant bundle files, renaming as per the variant bundlefile mappings...
		foreach(Variant variant in variants)
		{
			// get source variant bundle folder
			var bundleVariantFolder = getVariantBuildFolder(sku, target, variant);

			var variantBundleFileMappings = mergedManifest.bundleVariants[variant.ToString()];
			foreach (var mapping in variantBundleFileMappings)
			{
				var srcBundleName = mapping.Key;
				var dstMangledName = mapping.Value;
				if (srcBundleName == "initialization.bundlev2" && !string.IsNullOrEmpty(manifestNameOverride))
				{
					continue;
				}
				
				if (dstMangledName.Contains('/'))
				{
					dstMangledName = dstMangledName.Replace('/', '_');
				}

				//Could collapse multiple variants with matching CRC into one; for now, for readability, we keep them separate
				//Debug.Log("Copying variant " + variant + " bundle " + srcBundleName + " --> " + dstMangledName);

				File.Copy(
					bundleVariantFolder + "/" + srcBundleName,
					finalOutputFolder + "/" + dstMangledName,
					true);

				// Embed all "shader" bundles we come across...
				// TODO: Combine, should be 1 per variant, verify CRC's match, fix loading code
				if (srcBundleName.FastStartsWith("shaders."))
				{
					// Unity needs a .bytes extension to so we can load this resource as binary data via a TextAsset
					dstMangledName += ".bytes";

					Debug.Log("Embedding " + variant + " variant " + srcBundleName + " --> " + dstMangledName);
					File.Copy( 
						bundleVariantFolder + "/" + srcBundleName,
						getEmbeddedResourcesFolder(sku, target) + "/" + dstMangledName, 
						true 
					);
				}
			}
		}

		// Find & Delete all old manifests such as "Data/HIR/Resources/bundleContentsV2_android" so they don't get
		// embedded in this app build. We find them by just searching for all "bundleContentsV2_*" files and excluding
		// the current platform manifest file.
		 AssetDatabase.FindAssets(AssetBundleManifest.BUNDLE_MANIFEST_FILE_V2_BASENAME, new string[] { "Assets/Data" })  //guids
			.Select(guid => AssetDatabase.GUIDToAssetPath(guid))  //paths
			.Where(path => !path.EndsWith(AssetBundleManifest.BUNDLE_MANIFEST_FILE_V2 + ".txt")) // exclude current platform
			.ToList() //in a list
			.ForEach(path => AssetDatabase.DeleteAsset(path)); //gone!

		 if (string.IsNullOrEmpty(manifestNameOverride))
		 {
			 //Only embed the full bundle manifests 
			 embedManifest(sku, target, mergeWithOldManifest);
		 }

		 // We've modified the project
		AssetDatabase.Refresh();
	}

	// Copy our new manifest file to our app's Resource folder so it gets embedded with our app
	private static void embedManifest(SkuId sku, BuildTarget target, bool mergeWithOldManifest)
	{
		Debug.LogFormat("Embedding bundle manifest {0}:{1} mergeWithOldManifest:{2}", sku, target, mergeWithOldManifest);
		var finalManifestFilePath = getFinalManifestFilePath(sku, target); // As built
		var embeddedManifestFilePath = getEmbeddedManifestFilePath(sku, target); // Old embedded manifest

		if (mergeWithOldManifest && File.Exists(embeddedManifestFilePath))
		{
			Debug.Log("Merging with old manifest...");
			// Copy everything built in new manifest into old one, overwriting any existing values.
			var mergedManifest = new AssetBundleManifestWrapper(File.ReadAllText(embeddedManifestFilePath));
			var newManifest = new AssetBundleManifestWrapper(File.ReadAllText(finalManifestFilePath));

			foreach (var kvp in newManifest.bundleContents)
			{
				mergedManifest.bundleContents[kvp.Key] = kvp.Value;
			}
			foreach (var kvp in newManifest.bundleDependencies)
			{
				mergedManifest.bundleDependencies[kvp.Key] = kvp.Value;
			}
			foreach (var variantDef in newManifest.bundleVariants)
			{
				foreach (var bundleDef in variantDef.Value)
				{
					mergedManifest.bundleVariants[variantDef.Key][bundleDef.Key] = bundleDef.Value;
				}
			}

			File.WriteAllText(embeddedManifestFilePath, mergedManifest.convertToJson());
		}
		else
		{
			Debug.Log("Copying manifest to resources...");
			// Simple copy is sufficient.
			File.Copy(finalManifestFilePath, embeddedManifestFilePath, true);
		}

		// Copy bundle manifest to /logs/, so it shows up in the jenkins build artifacts. A dev can copy-pasta it if he wants.
		// (technically not a 'log' but only way to get a file to show up in Jenkins build artifacts)
		Directory.CreateDirectory(getLogFolder());
		File.Copy(embeddedManifestFilePath, getLogFolder() + "/AssetBundleManifest.txt", true);
	}

	public static AssetBundleManifestWrapper mergeVariantManifests(SkuId sku, BuildTarget target, Variant[] variants)
	{
		Debug.Log("mergeVariantManifests...");

		if (variants.Length < 1)
		{
			fatalError("mergeVariantManifests: received empty variant list");
		}

		AssetBundleManifestWrapper[] manifests = variants
			.Select(variant => getVariantManifestFilePath(sku, target, variant))  // get filepath
			.Select(filepath => File.ReadAllText(filepath))                    // read file
			.Select(jsonText => new AssetBundleManifestWrapper(jsonText))      // create manifest from json
			.ToArray();

		// Only 1 manifest? nothing to merge, return this one
		if (variants.Length == 1)
		{
			return manifests[0];
		}

		// verify manifests are consistent with each other (should have been built from the same projects)
		for (int i = 1; i < variants.Length; i++)
		{
			// will throw fatal exception if problem
			compareManifestsForConsistency(manifests[0], manifests[i]);
		}

		// combine the variant mappings
		var combinedVariantBundleMappings = new Dictionary<string, Dictionary<string, string>>();
		foreach (var manifest in manifests)
		{
			var variantDef = manifest.bundleVariants.Single();
			combinedVariantBundleMappings.Add(variantDef.Key, variantDef.Value);
		}

		// Modify manifest[0] - swap out the bundle_variants
		manifests[0].bundleVariants = combinedVariantBundleMappings;
		return manifests[0];
	}

	static void compareManifestsForConsistency(
		AssetBundleManifestWrapper manifestA,
		AssetBundleManifestWrapper manifestB
	)
	{
		// Should be a single uniquely named variant mapping per manifest
		{
			// compare variant names
			string[] variantNamesA = manifestA.bundleVariants.Keys.ToArray();
			string[] variantNamesB = manifestB.bundleVariants.Keys.ToArray();

			if (variantNamesA.Length != 1)
			{
				fatalError("Expected a single variant definition for manifestA"); 
			}

			if (variantNamesB.Length != 1)
			{
				fatalError("Expected a single variant definition for manifestB"); 
			}

			if (variantNamesA[0] == variantNamesB[0])
			{
				fatalError("Expected manifestA & manifestB to contain different variant definitions"); 
			}

			// compare only SOURCE bundle names of each mapping (don't care about dest bundle names)
			var variantBundleMappingsA = manifestA.bundleVariants.Single().Value;
			var variantBundleMappingsB = manifestB.bundleVariants.Single().Value;
			if (!variantBundleMappingsA.Keys.SequenceEqual(variantBundleMappingsB.Keys)) 
			{
				fatalError("bundleDependency keys don't match between manifest " + variantNamesA[0] + " and " + variantNamesB[0]); 
			}
		}

		// get variant names for better error messages
		string variantA = manifestA.bundleVariants.Keys.Single();
		string variantB = manifestB.bundleVariants.Keys.Single();

		// compare the bundle name to asset list sections...
		{
			// compare bundle names
			if (!manifestA.bundleContents.Keys.SequenceEqual(manifestB.bundleContents.Keys)) 
			{
				fatalError("bundleContents keys don't match between manifest " + variantA + " and " + variantB); 
			}

			// compare asset list for each matching set of bundles
			foreach (var bundleName in manifestA.bundleContents.Keys)
			{
				var assetsListA = manifestA.bundleContents[bundleName];
				var assetsListB = manifestB.bundleContents[bundleName];
				if (!assetsListA.SequenceEqual(assetsListB)) 
				{
					fatalError("bundleContent: " + bundleName + " assets don't match between manifest " + variantA + " and " + variantB); 
				}
			}
		}

		// compare the bundle dependencies sections...
		{
			// compare bundle names
			if (!manifestA.bundleDependencies.Keys.SequenceEqual(manifestB.bundleDependencies.Keys)) 
			{
				fatalError("bundleDependency keys don't match between manifest " + variantA + " and " + variantB); 
			}

			// compare asset list for each matching set of bundles
			foreach (var bundleName in manifestA.bundleDependencies.Keys)
			{
				var assetsListA = manifestA.bundleDependencies[bundleName];
				var assetsListB = manifestB.bundleDependencies[bundleName];
				if (!assetsListA.SequenceEqual(assetsListB)) 
				{
					fatalError("bundleDependencies for: " + bundleName + " assets don't match between manifest " + variantA + " and " + variantB); 
				}
			}
		}
	}


	// get Unity's file crc for a bundle
	static uint getBundleCrc(string bundleFilePath)
	{
		uint crc = 0;
		bool bSuccess = BuildPipeline.GetCRCForAssetBundle( bundleFilePath, out crc);  //appends '.manifest' to filename?
		if (!bSuccess)
		{
			Debug.LogErrorFormat("Failed GetCRCForAssetBundle for: " + bundleFilePath); 
		}
		else if (crc == 0) 
		{ 
			Debug.LogError("bundlemanifest: " + bundleFilePath + " has a CRC of 0!"); 
		}
		return crc;
	}


	static void printBundleSizeReport(string description, string [] builtBundles, string bundleFolderProjRelativePath)
	{
		List<FileInfo> fileInfos = new List<FileInfo>();
		long totalBundleSize = 0;
		for (int i = 0; i < builtBundles.Length; i++)
		{
			string fileName =  string.Format("{0}/{1}", bundleFolderProjRelativePath, builtBundles[i]);
			FileInfo fileInfo = new FileInfo(fileName);
			fileInfos.Add(fileInfo);
			totalBundleSize += fileInfo.Length;
		}

		// sort descending order of filesize
		fileInfos = fileInfos.OrderByDescending(fileinfo => fileinfo.Length).ToList();

		System.Text.StringBuilder log = new System.Text.StringBuilder(80 * builtBundles.Length);
		log.AppendFormat("Bundle Size Report for '{0}': {1,54:N0} bytes total\n", description, totalBundleSize);
		foreach (FileInfo info in fileInfos)
		{
			log.AppendFormat("{0,-60} {1,12:N0} bytes\n", info.Name, info.Length);
		}

		Debug.Log(log.ToString());
	}


	// Checks a Unity AssetBundleManifest for any circular dependencies (A depends on B, while B depends on A)
	// Returns 'true' if a circular dependency chain is detected (and will debug.log with info about the cycles)
	static bool checkForCircularDependencies( UnityEngine.AssetBundleManifest bundleManifest )
	{
		// build dictionary of all bundles and their dependencies (even empty dependency sets); don't care about CRC names for this
		var bundleDependencies = new Dictionary<string, List<string>>();
		foreach (var bundleName in bundleManifest.GetAllAssetBundles())
		{
			bundleDependencies[ bundleName ] = bundleManifest.GetAllDependencies(bundleName).ToList();
		}

		// We will iteratively prune bundles that have no dependencies; each time we prune a bundle, remove it from all the dependency lists.
		// Repeat until we can't find anymore. Should end with an empty dictionary, else what remains have circular dependencies.
		var bundlesWithoutDependencies = new List<string>();
		do
		{
			bundlesWithoutDependencies = bundleDependencies.Where( kvp => kvp.Value.Count == 0 ).Select( kvp => kvp.Key ).ToList();
			foreach(string bundleWithoutDependencies in bundlesWithoutDependencies)
			{
				// remove this bundle from all dependency lists
				foreach(var kvp in bundleDependencies)
				{
					kvp.Value.Remove( bundleWithoutDependencies );
				}

				// remove the bundle entry
				bundleDependencies.Remove( bundleWithoutDependencies );
			}
		} while (bundlesWithoutDependencies.Count > 0);

		// If anything is left in the dictionary, they're circular dependencies. Log info...
		if (bundleDependencies.Count > 0)
		{
			var bundleDependencyInfo = bundleDependencies.Select( kvp => "" + kvp.Key + " DependsOn " + string.Join(", ", kvp.Value.ToArray() )).ToArray();
			Debug.LogError("Circular Bundle Dependencies Detected:\n" + string.Join("\n", bundleDependencyInfo));
		}

		// return true if circular dependencies found
		return (bundleDependencies.Count > 0);
	}


	// Gets a list of AssetBundleNames (tags) that also have the appropriate skuLabel applied (to it or any parent)
	// If no skuLabel is provided, it will return ALL AssetBundleNames
	//
	// Sku-Labels can be applied sparsely, and we treat them as applying to all children, 
	// so you can label a high-level parent folder with "hir" and all children assets are consider to belong to HIR.
	//
	// Guid: db3bed748027f4af9865521c3b336cbe     Assets/Assets Games/satc/Resources/Games/satc
	// Guid: c55b27574e5d24cf28dbb21481fd57b3     Assets/Assets Games/satc/ToBundle/Games/satc/satc01

	static public string[] getAssetBundleNamesForSku(SkuId sku)
	{
		var bundleNames = AssetDatabase.GetAllAssetBundleNames();

//		if(!string.IsNullOrEmpty(skuLabel))
		{
			// Gather all the assetPath's that have a sku Label
			var assetGuidsWithSkuLabel = AssetDatabase.FindAssets("l:" + sku);
			var assetPathsWithSkuLabel = assetGuidsWithSkuLabel.Select( guid => AssetDatabase.GUIDToAssetPath(guid) ).ToArray();

			// Function to determine if an asset path has (actual or inherited) skuLabel
			Func<string, bool> pathHasSkuLabel = (path) => assetPathsWithSkuLabel.Any( labelPath => path.StartsWith(labelPath) );
	
			// Filter the bundle list to only include bundles that have (or inherit) sku label 
			bundleNames = bundleNames.Where( 
				bundleName => AssetDatabase.FindAssets("b:" + bundleName)  //assetGuids that have bundleTags
				.Select( guid => AssetDatabase.GUIDToAssetPath(guid) )     //convert guids to assetPaths
				.Any( bundlePath => pathHasSkuLabel(bundlePath) )          //does any bundlePath have skuLabel?
			).ToArray();
		}

		return bundleNames;
	}

	// Given array of possible 'bundleNames', use descriptive string 'whatBundles' to select and filter desired bundle
	// names to build.  Returns new array of bundle names.
	static public string[] filterAssetBundleNames(string[] bundleNames, string whatBundles)
	{
		string[] bundleDescriptors = whatBundles.Split(',');
		var filteredBundleNames = new List<string>();
		// Always have "initialization" if we return anything at all.
		filteredBundleNames.Add("initialization");

		// Function to use for filtering bundle paths as in games or not.
		Func<string, bool> pathIsInGames = (path) => path.StartsWith("Assets/Data/Games/");

		foreach (var bundleDesc in bundleDescriptors)
		{
			if (string.IsNullOrEmpty(bundleDesc))
			{
				continue;
			}

			if (bundleDesc == "all")
			{
				// Stop here and just return the whole kit and kaboodle.
				return bundleNames;
			}
			else if (bundleDesc == "none")
			{
				// Stop here and just return nothing, except initialization bundle.
				return new string[] {"initialization"};
			}
			else if (bundleDesc == "games")
			{
				// Filter the bundle list to only include bundles in Assets/Data/Games
				var gamesBundles = bundleNames.Where(
									bundleName => AssetDatabase.FindAssets("b:" + bundleName)  //assetGuids that have bundleTags
									.Select( guid => AssetDatabase.GUIDToAssetPath(guid) )     //convert guids to assetPaths
									.Any( bundlePath => pathIsInGames(bundlePath) )          //does any bundlePath have game path?
									);
				filteredBundleNames.AddRange(gamesBundles);
			}
			else if (bundleDesc == "features")
			{
				// Filter the bundle list to only include bundles NOT in Assets/Data/Games
				var featuresBundles = bundleNames.Where(
										bundleName => AssetDatabase.FindAssets("b:" + bundleName)  //assetGuids that have bundleTags
										.Select( guid => AssetDatabase.GUIDToAssetPath(guid) )     //convert guids to assetPaths
										.Any( bundlePath => !pathIsInGames(bundlePath) )          //does any bundlePath have game path?
										);
				filteredBundleNames.AddRange(featuresBundles);
			}
			else
			{
				// Assume bundleDesc is bundle name to build.
				filteredBundleNames.Add(bundleDesc);
			}
		}
		return filteredBundleNames.Distinct().ToArray();
	}

	public static bool checkForCircularDependencies()
	{
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		string[] bundles = AssetDatabase.GetAllAssetBundleNames();
		Dictionary<string, List<string>> bundlesToDependenciesDict = new Dictionary<string, List<string>>();
		bool hasCircularDependency = false;
		for (int i = 0; i < bundles.Length; i++)
		{
			string[] dependencies = AssetDatabase.GetAssetBundleDependencies(bundles[i], false);
			if (dependencies.Length > 0)
			{
				string dependResultString = "";
				for (int j = 0; j < dependencies.Length; j++)
				{
					if (bundlesToDependenciesDict.ContainsKey(dependencies[j]) && bundlesToDependenciesDict[dependencies[j]].Contains(bundles[i]))
					{
						hasCircularDependency = true;
						string circularResult = string.Format("Circular dependency found between -{0}- & -{1}-", dependencies[j], bundles[i]);
						Debug.LogError(circularResult);
					}
					dependResultString += (dependencies[j] + ", ");
				}

				bundlesToDependenciesDict.Add(bundles[i], dependencies.ToList());
			}
		}
		
		string finalString = string.Format("Checked {0} bundles in {1} seconds", bundles.Length, stopwatch.Elapsed.TotalSeconds);
		Debug.Log(finalString);

		if (!hasCircularDependency)
		{
			Debug.Log("No circular bundle dependencies found");
		}
		else
		{
			fatalError("Failing because Circular Dependencies found");
		}
		
		return hasCircularDependency;
	}
	
	static void fatalError(string msg)
	{
		throw new System.Exception(msg);
	}
}


// Custom sorting that ignores filenames with extra _hash and/or .bundlev2 extension
// We effectively sort only on the base filename
//   ie: main_img_soft_prompt_f898335497c15dd3aeebfa75f10cb4c4.bundlev2
//   as: main_img_soft_prompt
public class CustomComparer : IComparer<string>  
{
	public int Compare(string x, string y)  
	{
		x = getBasename(x);
		y = getBasename(y);
		return Comparer<string>.Default.Compare(x, y);
	}

	private string getBasename(string fullname)
	{
		// matches: basename, optional _<hash>, optional .bundlev2 extension
		string bundleRegexPattern = @"(.+?)(_[0-9a-f]{32})?(\" + AssetBundleManager.bundleV2Extension + @")?$";

		var match = Regex.Match(fullname, bundleRegexPattern);
		if (match.Success && match.Groups.Count >= 2)
		{
			return match.Groups[1].Value;
		}

		// couldn't extract a base? return fullname
		return fullname;
	}
}
