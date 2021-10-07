using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class AssetBundleManifest
{
	// to allow deletion of other-platform manifests, so they dont get included in build
	public const string BUNDLE_MANIFEST_FILE_V2_BASENAME = "bundleContentsV2_";

#if UNITY_IPHONE
	public const string BUNDLE_MANIFEST_FILE_V2 = "bundleContentsV2_ios";
#elif UNITY_ANDROID && ZYNGA_KINDLE
	public const string BUNDLE_MANIFEST_FILE_V2 = "bundleContentsV2_kindle";
#elif UNITY_ANDROID
	public const string BUNDLE_MANIFEST_FILE_V2 = "bundleContentsV2_android";
#elif UNITY_WSA_10_0
	public const string BUNDLE_MANIFEST_FILE_V2 = "bundleContentsV2_windows";
#elif UNITY_WEBGL
	public const string BUNDLE_MANIFEST_FILE_V2 = "bundleContentsV2_webgl";
#else
	public const string BUNDLE_MANIFEST_FILE_V2 = "Expected iOS, Android, Windows, or WebGL platform!";
#endif

	public Dictionary<string, string> dictAssetPathToBundle { get; private set; } // asset paths (all lower-case)
	public Dictionary<string, string[]> bundleDependencies { get; private set; }  // bundles dependent on other bundles
	public Dictionary<string, string> baseBundleNameToFullBundleNameDict { get; private set; }     // base-bundle names to full mangled-hashed-variant bundle names

	public string getBundleNameForResource(string resourcePath)
	{
		if (string.IsNullOrEmpty(resourcePath) || resourcePath.FastEndsWith("/"))
		{
			return "";
		}

		string result = null;
		if (dictAssetPathToBundle != null && dictAssetPathToBundle.TryGetValue (resourcePath.ToLower(), out result)) 
		{
			return result;
		}

		return "";
	}

	// Lookup v2 full bundlename from base bundlename (ie: "whatever_bundlename")
	// This is needed for preload requests
	//
	// Returns v2 bundle name, else null if no match
	//
	// ie: base bundlename = "main_img_lobby_options"
	//     v2 bundlename = "main_img_lobby_options_hir_9be86a828ffecf412bbe02e4548dcfde.bundlev2"
	public string getFullBundleNameFromBaseBundleName(string baseBundleName)
	{
		string v2BundleName = null;
		if (baseBundleNameToFullBundleNameDict != null)
		{
			baseBundleNameToFullBundleNameDict.TryGetValue(baseBundleName.ToLower(), out v2BundleName); 
		}
		return v2BundleName;
	}

	// returns array of bundles a particular bundle depends on (NULL if none)
	public string[] getBundleDependencies(string bundleName)
	{
		string[] dependencies = null;
		if (bundleDependencies != null)
		{
			bundleDependencies.TryGetValue(bundleName, out dependencies);
		}
		return dependencies;
	}

	public bool ReadAssetBundleManifestFileV2()
	{
		// load embedded manifest file...  (NOT from bundle))
		TextAsset textFile = SkuResources.loadSkuSpecificEmbeddedResource<TextAsset>(BUNDLE_MANIFEST_FILE_V2, ".txt");
		string manifestText = (textFile != null) ? textFile.text : null;

		if (manifestText != null)
		{
			// parse the manifest json
			var wrapper = new AssetBundleManifestWrapper(manifestText);

#if !UNITY_WEBGL
			// swap in the HD initialization bundle since that's embedded in all non webgl builds.
			// webgl now builds the SD version of the initialization bundle
			{
				Debug.Log("Attempting to patch HD initialization bundle into manifest...");

				Dictionary<string,string> hdVariantMapping;
				if (wrapper.bundleVariants.TryGetValue("HD", out hdVariantMapping))
				{
					foreach (string bundleName in AssetBundleManager.embeddedBundlesList)
					{
						string baseBundleName = bundleName + AssetBundleManager.bundleV2Extension;
						string hdBundleName;

						if (hdVariantMapping.TryGetValue(baseBundleName, out hdBundleName))
						{
							// Found the hdBundleName, now patch it into all the variant lists...
							foreach (string variantName in wrapper.bundleVariants.Keys)
							{
								Debug.Log("...Patched: " + hdBundleName + "  into '" + variantName + "' variant");
								wrapper.bundleVariants[variantName][baseBundleName] = hdBundleName;
							}
						}
					}
				}
			}
#endif

			// get the ideal variantMapping for this platform/device/capabilities, null if none (legacy pre-variant manifest)
			Dictionary<string,string> variantMapping = AssetBundleVariants.getIdealVariantMapping(wrapper);

			// Add the current variant name to our informative loading screen info
			if (Loading.instance != null)
			{
				Loading.instance.setStageVariantLabel("variant=" + AssetBundleVariants.getActiveVariantName());
			}

			// Build a dictionary of "assets -> bundle" mappings from the JSON "bundle -> assets" mappings (JSON is 1/2 the size this way)
			var assetPathToBundle = new Dictionary<string, string>(wrapper.assetsCount);
			foreach (var kvp in wrapper.bundleContents)
			{
				string baseBundleName = kvp.Key;
				string[] assetPaths = kvp.Value;

				// if variantMapping exists, remap simple bundlefile to full variant-specific bundlefile
				string remappedBundleName = (variantMapping != null) ? variantMapping[baseBundleName] : baseBundleName;

				foreach (string assetPath in assetPaths)
				{
					if (assetPathToBundle.ContainsKey(assetPath))
					{
						Debug.LogErrorFormat("ERROR: Asset '{0}' already exists in bundle '{1}', cannot add to bundle '{2}'.", assetPath, assetPathToBundle[assetPath], remappedBundleName);
					}
					else
					{
						assetPathToBundle.Add(assetPath, remappedBundleName);
					}
				}
			}

			// Build a table of base-bundlename to full-bundlename mappings so we can do lookups (mostly for caching)
			var baseToFullBundleName = new Dictionary<string, string>();
			if (variantMapping == null)
			{
				// backwards compatible support for non-variant manifests that require bundlename de-mangling
				foreach (string fullBundleName in wrapper.bundleContents.Keys)
				{
					// Demangle the full bundlename to just it's basename
					string baseBundleName = convertFullBundleNameToBaseBundleName( fullBundleName );
					baseToFullBundleName.Add( baseBundleName, fullBundleName );
				}
			}
			else
			{
				// New manifest format already has a variant base bundle -> full bundle name mapping
				foreach (var kvp in variantMapping)
				{
					string baseBundleName = kvp.Key;
					string variantBundleName = kvp.Value;

					// new manifest already has (almost) base names in manifest, just strip off any _<sku> and/or ".bundlev2" extension
					baseBundleName = baseBundleName.Replace("_" + SkuResources.skuString + ".", ".");
					baseBundleName = baseBundleName.Replace(AssetBundleManager.bundleV2Extension, "");
					baseToFullBundleName.Add(baseBundleName, variantBundleName);
				}
			}

			// Make variant-specific Dependency mapping (if no variant, uses original dependency table)
			var newDependencies = wrapper.bundleDependencies;
			if (variantMapping != null)
			{
				newDependencies = new Dictionary<string, string[]>();
				foreach (var kvp in wrapper.bundleDependencies)
				{
					string baseBundleName = kvp.Key;
					string[] dependentBundles = kvp.Value;
					newDependencies.Add(variantMapping[baseBundleName], dependentBundles.Select(name => variantMapping[name]).ToArray());
				}
			}

			// remember these...
			this.dictAssetPathToBundle = assetPathToBundle;
			this.baseBundleNameToFullBundleNameDict = baseToFullBundleName;
			this.bundleDependencies = newDependencies;
		}
		else
		{
			Debug.LogError("Asset Bundle Manifest not found: " + BUNDLE_MANIFEST_FILE_V2);
			return false;
		}
		
		return true;
	}



	// Converts a v2 full bundle name to a base bundle name
	//
	// Differences include: possible _<sku>, possible _<hash>, .bundlev2 extension
	//
	// ie: base bundlename = "main_img_lobby_options"
	//     v2 full bundlename = "main_img_lobby_options_hir_9be86a828ffecf412bbe02e4548dcfde.bundlev2"
	// or  v2 full bundlename = "main_img_lobby_options_hir_9be86a828ffecf412bbe02e4548dcfde_1ffc53b8.bundlev2"  (has crc)
	public string convertFullBundleNameToBaseBundleName(string v2FullBundleName)
	{
		// matches: basename, optional _<sku>, optional _<hash>, optional _<crc>, and ".bundlev2" extension
		string bundleRegexPattern = @"(.+?)(_hir|_sir|_tv)?(_[0-9a-f]{32})?(_[0-9a-f]{8})?\" + AssetBundleManager.bundleV2Extension;

		var match = Regex.Match(v2FullBundleName, bundleRegexPattern);
		if (match.Success && match.Groups.Count < 2)
		{
			Debug.LogError("Unable to extract bundle basename from: " + v2FullBundleName);
			return v2FullBundleName;
		}

		// return basename + v1Extension
		return match.Groups[1].Value;
	}

	public void readExtraManifest(string manifestText)
	{
		AssetBundleManifestWrapper wrapper = new AssetBundleManifestWrapper(manifestText);
		Dictionary<string,string> variantMapping = AssetBundleVariants.getIdealVariantMapping(wrapper);

		foreach (var kvp in wrapper.bundleContents)
		{
			string baseBundleName = kvp.Key;
			string[] assetPaths = kvp.Value;

			// if variantMapping exists, remap simple bundlefile to full variant-specific bundlefile
			string remappedBundleName = (variantMapping != null) ? variantMapping[baseBundleName] : baseBundleName;

			foreach (string assetPath in assetPaths)
			{
				if (dictAssetPathToBundle.ContainsKey(assetPath))
				{
					//Override path but log error since paths are usually already part of the code somewhere
					dictAssetPathToBundle[assetPath] = remappedBundleName;
				}
				else
				{
					dictAssetPathToBundle.Add(assetPath, remappedBundleName);
				}
			}
		}

		// Build a table of base-bundlename to full-bundlename mappings so we can do lookups (mostly for caching)
		if (variantMapping == null)
		{
			// backwards compatible support for non-variant manifests that require bundlename de-mangling
			foreach (string fullBundleName in wrapper.bundleContents.Keys)
			{
				// Demangle the full bundlename to just it's basename
				string baseBundleName = convertFullBundleNameToBaseBundleName( fullBundleName );
				//baseToFullBundleName.Add( baseBundleName, fullBundleName );
				if (baseBundleNameToFullBundleNameDict.ContainsKey(baseBundleName))
				{
					baseBundleNameToFullBundleNameDict[baseBundleName] = fullBundleName;
				}
				else
				{
					baseBundleNameToFullBundleNameDict.Add(baseBundleName, fullBundleName);
				}
			}
		}
		else
		{
			// New manifest format already has a variant base bundle -> full bundle name mapping
			foreach (var kvp in variantMapping)
			{
				string baseBundleName = kvp.Key;
				string variantBundleName = kvp.Value;

				// new manifest already has (almost) base names in manifest, just strip off any _<sku> and/or ".bundlev2" extension
				baseBundleName = baseBundleName.Replace("_" + SkuResources.skuString + ".", ".");
				baseBundleName = baseBundleName.Replace(AssetBundleManager.bundleV2Extension, "");
				
				if (baseBundleNameToFullBundleNameDict.ContainsKey(baseBundleName))
				{
					baseBundleNameToFullBundleNameDict[baseBundleName] = variantBundleName;
				}
				else
				{
					baseBundleNameToFullBundleNameDict.Add(baseBundleName, variantBundleName);
				}
			}
		}

			// Make variant-specific Dependency mapping (if no variant, uses original dependency table)
		if (variantMapping != null)
		{
			Dictionary<string, string[]> newDependencies = new Dictionary<string, string[]>();
			foreach (var kvp in wrapper.bundleDependencies)
			{
				string baseBundleName = kvp.Key;
				string[] dependentBundles = kvp.Value;
				string depends = "";

				if (bundleDependencies.ContainsKey(variantMapping[baseBundleName]))
				{
					bundleDependencies[variantMapping[baseBundleName]] = dependentBundles.Select(name => baseBundleNameToFullBundleNameDict[name.Replace(AssetBundleManager.bundleV2Extension, "")]).ToArray();
				}
				else
				{
					bundleDependencies.Add(variantMapping[baseBundleName], dependentBundles.Select(name => baseBundleNameToFullBundleNameDict[name.Replace(AssetBundleManager.bundleV2Extension, "")]).ToArray());
				}
			}
		}
	}
}
