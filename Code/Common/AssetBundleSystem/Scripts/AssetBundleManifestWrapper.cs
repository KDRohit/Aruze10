using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Simple strongly typed manifest container to read/write our json-based assetbundle manifest files
// Needed so bundle building pipeline code can read/write/manipulate our manifests
//
// Author: kkralian

public class AssetBundleManifestWrapper
{
	public Dictionary<string, string[]> bundleContents;
	public Dictionary<string, string[]> bundleDependencies;
	public Dictionary<string, Dictionary<string, string>> bundleVariants;
	public int assetsCount = 0;

	// construct from json text
	public AssetBundleManifestWrapper(string jsonText)
	{
		parseFromJson(jsonText);
	}

	// construct from objects
	public AssetBundleManifestWrapper(
		Dictionary<string, string[]> bundleContents,
		Dictionary<string, string[]> bundleDependencies,
		Dictionary<string, Dictionary<string, string>> bundleVariants )
	{
		this.bundleContents = bundleContents;
		this.bundleDependencies = bundleDependencies;
		this.bundleVariants = bundleVariants;
	}

	public string convertToJson()
	{
		// Setup object hierarchy
		Dictionary<string, object> root = new Dictionary<string, object>();
		root.Add("bundle_name_to_assets", bundleContents);
		root.Add("bundle_dependencies", bundleDependencies);
		root.Add("bundle_variants", bundleVariants);
		root.Add("build_tag", Glb.buildTag);

		// Use json's SerializeHumanReadable to get human readable output
		return Zynga.Core.JsonUtil.Json.SerializeHumanReadable(root);
	}

	void parseFromJson(string jsonText)
	{
		JSON manifest = new JSON(jsonText);

		JSON contents = manifest.getJSON("bundle_name_to_assets");
		List<string> contentsKeys = contents.getKeyList();
		bundleContents = new Dictionary<string, string[]>(contentsKeys.Count);
		foreach (var bundleName in contentsKeys)
		{
			string[] bundleContentsArray = contents.getStringArray(bundleName);
			bundleContents[bundleName] = bundleContentsArray;
			assetsCount += bundleContentsArray.Length;
		}

		JSON dependencies = manifest.getJSON("bundle_dependencies");
		List<string> dependenciesKeys = dependencies.getKeyList();
		bundleDependencies = new Dictionary<string, string[]>(dependenciesKeys.Count);
		foreach (var bundleName in dependenciesKeys)
		{
			bundleDependencies[bundleName] = dependencies.getStringArray(bundleName);
		}

		// and optional variant remappings (old manifests did not have this) - TODO: require later
		JSON variants = manifest.getJSON("bundle_variants");
		if (variants != null)
		{
			List<string> variantsKeys = variants.getKeyList();
			bundleVariants = new Dictionary<string, Dictionary<string, string>>(variantsKeys.Count);
			foreach (var variantName in variantsKeys)
			{
				bundleVariants[variantName] = variants.getStringStringDict(variantName);
			}
		}
	}
}