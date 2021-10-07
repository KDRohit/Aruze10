using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/**
This static class provides Asset Bundle Variant bundle-building support
*/

public static class BuildVariant
{
	// userdata keyword to indicate a metafile is a temporary variant override
	public static string TEMPOVERRIDE_KEYWORD = "TempOverride-";


	// The texture size at which our SD variant textures will be size reduced.
	// Textures GREATER than this size will be size-reduced, otherwise they will maintain their initial size.
	//
	// The intent is to reduce the size of the largest atlases, while preserving textures that are already conservatively sized.
	// There is less urgency on shrinking small textures, as we get a diminshing return on memory savings by shrinking them.
	//
	// This particular value was obtained by viewing (colorized mipmap test) T102 game content at 800p resolution, 
	// and noticing that most content was using the 2nd miplevel EXCEPT for a handful of symbols that used the 1st miplevel, 
	// which came from 256x256 textures. Assuming these are common sizes elsewhere, lets err on the side of keep 256x256 textures.
	// Not scientific, but we have to choose something. We can tune this value (or specific content) later. 
	// (Pic of the T102 colorized mip test; https://screencast.com/t/YhUpayRiZk4W
	//  Red elements are 2nd mip level, ideal for size reduction. Yellow are 1st miplevel, and happen to be 256x256 textures).

	const int SHRINK_SD_THRESHOLD = 256; // Shrink SD textures that are GREATER than 256


	// Which platforms support which variants...
	// Throws an exception for unhanlded platform/variant cases
	public static bool isVariantAllowedForTargetPlatform(Variant variant, BuildTarget target)
	{
		switch(target)
		{
			case BuildTarget.iOS:
			case BuildTarget.Android:
			case BuildTarget.WebGL:
				switch (variant)
				{
					case Variant.HD:
					case Variant.SD:
					case Variant.SD4444:
					case Variant.TintedMipTest:
						return true;
					default:
						fatalError("unhandled variant: " + variant + " for target" + target);
						break;
				}
				break;

			case BuildTarget.WSAPlayer:
				switch (variant)
				{
					case Variant.HD:
						return true;
					case Variant.SD:
					case Variant.SD4444:
					case Variant.TintedMipTest:
						return false;
					default:
						fatalError("unhandled variant: " + variant + " for target" + target);
						break;
				}
				break;
			
			default:
				fatalError("Unhandled target platform: " + target);
				break;
		}

		// shouldn't really get here...
		return false;
	}


	// TODO: "inNoBundle" assets (particularly Games/-Unsorted-/...) should be moved into other folders in git (several unclassified)
	// TODO: Fix asset file permissions... (either copy them as-is, or just fix in git) so restored backups dont appears as diffs
	// TODO: WARN ABOUT SLOW TEXTURE IMPORTS, OR PUT MESSAGE IN MENU...
	// TODO: Can optimize for same-variant-case (no need to restore then setup; only would help local dev builds)
	public static void setupTextureVariant(SkuId sku, BuildTarget target, Variant variant)
	{
		bool verbose = true;
		var startTime = System.DateTime.Now;

		// always restore original importers before attemping to modify them (don't want cumulative modifications)
		restoreAllTextureImporters();

		// nothing to override for default HD variant
		if (variant == Variant.HD)
		{
			return;
		}

		if (verbose) { Debug.Log("Starting setupTextureVariant() for " + variant); }


		// Start with the entire potential set of textures we can modify (for this sku)
		string[] texturePaths = getTextureAssetPathsToOperateOn(sku, target);
		//=== FIRST FILTER ASSET LIST BY ASSET PATHS...
		switch (variant)
		{
			case Variant.TintedMipTest: // 55
				texturePaths = texturePaths.Where(path => path.ToLower().Contains("t101") || path.ToLower().Contains("ainworth")).ToArray(); // include just T101 (55) for now...
 				break;

			case Variant.SD4444:
				// Try all of them!  HIR: 4821 textures,   SIR: 2603 textures
				break;

			case Variant.SD:
				// Try all of them!  HIR: 4821 textures,   SIR: 2603 textures
				break;

			default:
				fatalError("unhandled variant: " + variant);
				break;
		}
		if (verbose) { logAndCopy("TextureSet after assetpath filtering...", texturePaths); }

		int modifiedCount = 0;
		foreach (var texturePath in texturePaths)
		{
			bool isTextureModified = convertTextureToVariant(texturePath, target, variant, verbose);

			if (isTextureModified)
			{
				modifiedCount++;
			}
		}

		Debug.Log("setupTextureVariant - DONE. Considered " + texturePaths.Length + ", modified " + modifiedCount);
		Debug.Log("setupTextureVariant - ElapsedTime = " + (System.DateTime.Now-startTime));

		// Important we refresh, else we might destroy unity's perception of reality
		startTime = System.DateTime.Now;
		AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport); 
		if (verbose) { Debug.Log("setupTextureVariant - Refresh time = " + (System.DateTime.Now-startTime)); }
	}

	// Attempts to force a .meta file for a texture to resave in a new format.  This
	// can be used to try and force the cache server to treat the file as a new version
	// of the file, which can be used as a stop gap fix if a texture gets pushed to the
	// cache server incorrectly until the cache server can have the bad files cleaned out.
	// May not change the file if the meta file is already in the latest format.
	public static bool resaveTextureMetaFile(string texturePath)
	{
		var texture = AssetDatabase.LoadMainAssetAtPath(texturePath) as Texture2D;
		Debug.Assert(texture != null, "null texture asset! " + texturePath);

		var textureImporter = TextureImporter.GetAtPath(texturePath) as TextureImporter;
		Debug.Assert(textureImporter != null, "null texture importer! " + texturePath);

		if (texture == null || textureImporter == null)
		{
			return false;
		}

		// Force in temp userData to make this file dirty
		string prevUserData = textureImporter.userData;
		textureImporter.userData = prevUserData + "temp";
		AssetDatabase.WriteImportSettingsIfDirty(textureImporter.assetPath);

		// Now save the file back the way it was before
		textureImporter.userData = prevUserData;
		AssetDatabase.WriteImportSettingsIfDirty(textureImporter.assetPath);

		return true;
	}
	
	// Convert a texture to the specified variant for the specified target.  Returns if the texture was modified or not.
	public static bool convertTextureToVariant(string texturePath, BuildTarget target, Variant variant, bool isVerbose)
	{
		Texture2D texture = AssetDatabase.LoadMainAssetAtPath(texturePath) as Texture2D;
		Debug.Assert(texture != null, "null texture asset! " + texturePath);

		TextureImporter textureImporter = TextureImporter.GetAtPath(texturePath) as TextureImporter;
		Debug.Assert(textureImporter != null, "null texture importer! " + texturePath);

		if (texture == null || textureImporter == null)
		{
			return false;
		}

		// original settings...
		TextureImporterSettings origImporterSettings = new TextureImporterSettings();
		textureImporter.ReadTextureSettings(origImporterSettings);
		string origUserData = textureImporter.userData;

		int origPlatformMaxSize;
		int origPlatformQuality;
		TextureImporterFormat origPlatformFormat;
		
		TextureImporterPlatformSettings defaultSettings = textureImporter.GetDefaultPlatformTextureSettings();
		int defaultMaxSize = defaultSettings.maxTextureSize;
		int defaultQuality = defaultSettings.compressionQuality;
		TextureImporterFormat defaultFormat = defaultSettings.format;
		
		string platformName = CommonEditor.getPlatformNameFromBuildTarget( target );
		bool hasPlatformSettings = textureImporter.GetPlatformTextureSettings( platformName, out origPlatformMaxSize, out origPlatformFormat, out origPlatformQuality);
		if (!hasPlatformSettings)
		{
			Debug.LogWarning("failed getting platform '" + platformName + "' settings for: " + texturePath);
			
			//Setting the default settings for these so that we have the chance to 1/2 size large textures that were being missed if they didn't have platform ovverrides
			origPlatformMaxSize = defaultMaxSize;
			origPlatformQuality = defaultQuality;
			origPlatformFormat = defaultFormat;
		}

		// Custom function to get original texture asset size...
		// var originalSize = CommonEditor.getOriginalTextureSize(textureImporter);

		// And 'new' settings we can modify
		TextureImporterSettings newImporterSettings = new TextureImporterSettings();
		origImporterSettings.CopyTo(newImporterSettings);
		string newUserData = origUserData;
		int newPlatformMaxSize = origPlatformMaxSize;
		int newPlatformQuality = origPlatformQuality;
		TextureImporterFormat newPlatformFormat = origPlatformFormat;

		// CHANGE the 'new' settings to our hearts desire; if they're different at the end, we'll update the importer
		// also, you can 'continue' to skip this asset

		//For now ... we expect Platform-specific overrides to already exist...
		//Not skipping WebGL so we can have the chance to reduce all textures, because WebGl needs to be as low on memory as possible
		if (isVerbose && !hasPlatformSettings && target != BuildTarget.WebGL) 
		{
			Debug.LogWarning("Skipping " + texturePath + "; does not have platform-specific settings");
			return false;
		}


		// Be picky about changing importer settings; ANY change to the importer settings from previous versions will change the asset hash,
		// causing the asset to have to re-import and re-upload to the cache server for ALL platforms  :-(
		newImporterSettings.aniso = 0;
		newImporterSettings.filterMode = FilterMode.Bilinear;
		newImporterSettings.mipmapEnabled = (target == BuildTarget.Android) ? true : false;  // Mipmap everything on Android
		newImporterSettings.mipmapBias = 0.0f;
		newImporterSettings.mipmapFilter = TextureImporterMipFilter.BoxFilter;
		newUserData = TEMPOVERRIDE_KEYWORD + variant + ";";

		if (variant == Variant.TintedMipTest)
		{
			// QUICK AND DIRTY tests to show some mipmap colors...
			newPlatformMaxSize = origPlatformMaxSize;
			newPlatformFormat = TextureImporterFormat.RGBA32;
			newPlatformQuality = AlphaSplitTextureInspector.ANDROID_MAIN_TEX_QUALITY;
			newUserData += TextureProcessor.TINTMIPMAPS + TextureProcessor.DITHER + TextureProcessor.MAKE4444;
		}
		else if (variant == Variant.SD && target == BuildTarget.Android)
		{
			newUserData += "v0.01;"; // bump the version to force re-import of this variant
			if (origPlatformFormat == TextureImporterFormat.ETC_RGB4)
			{
				// For ETC1 compressed textures, just add mipmapping and cleanup fields (above)
				// For larger ETC1 textures, also reduce size
				if (origPlatformMaxSize > SHRINK_SD_THRESHOLD) { newPlatformMaxSize = origPlatformMaxSize / 2; } 
			}
			else if (origPlatformFormat == TextureImporterFormat.ETC2_RGBA8)
			{
				// For ETC1 compressed textures, just add mipmapping and cleanup fields
				// For larger ETC2 textures, also reduce size
				if (origPlatformMaxSize > SHRINK_SD_THRESHOLD) { newPlatformMaxSize = origPlatformMaxSize / 2; } 
			}
			else
			{
				// TODO: Should check for RGBA32 textures, etc.
				Debug.Log("SD Skipping " + texturePath + "; not etc1/etc2; is: " + origPlatformFormat);
				return false;
			}
		}
		else if (variant == Variant.SD && target == BuildTarget.iOS)
		{
			newUserData += "v0.01;"; // bump the version to force re-import of this variant

			//Reducing compressed texture with sizes above our SD Threshold
			if (origPlatformFormat == TextureImporterFormat.PVRTC_RGB2)
			{
				if (origPlatformMaxSize > SHRINK_SD_THRESHOLD) { newPlatformMaxSize = origPlatformMaxSize / 2; } 
			}
			else if (origPlatformFormat == TextureImporterFormat.PVRTC_RGB4)
			{
				if (origPlatformMaxSize > SHRINK_SD_THRESHOLD) { newPlatformMaxSize = origPlatformMaxSize / 2; } 
			}
			else if (origPlatformFormat == TextureImporterFormat.PVRTC_RGBA2)
			{
				if (origPlatformMaxSize > SHRINK_SD_THRESHOLD) { newPlatformMaxSize = origPlatformMaxSize / 2; } 
			}
			else if (origPlatformFormat == TextureImporterFormat.PVRTC_RGBA4)
			{ 
				if (origPlatformMaxSize > SHRINK_SD_THRESHOLD) { newPlatformMaxSize = origPlatformMaxSize / 2; } 
			}

		}
		else if (variant == Variant.SD && target == BuildTarget.WebGL)
		{
			newUserData += "v0.01;"; // bump the version to force re-import of this variant

			// For now - try cutting all WebGL textures > 256 in half
			if (origPlatformMaxSize > SHRINK_SD_THRESHOLD) { newPlatformMaxSize = origPlatformMaxSize / 2; }

			//Make sure any compressed textures are using the Crunched format for smaller download sizes/faster loading
			if (origPlatformFormat == TextureImporterFormat.DXT1)
			{
				newPlatformFormat = TextureImporterFormat.DXT1Crunched;
			}
			else if (origPlatformFormat == TextureImporterFormat.DXT5 || origPlatformFormat == TextureImporterFormat.AutomaticCompressed || origPlatformFormat == TextureImporterFormat.Automatic)
			{
				newPlatformFormat = TextureImporterFormat.DXT5Crunched;
			}
		}
		else if (variant == Variant.SD4444)
		{
			newUserData += "v0.01;"; // bump the version to force re-import of this variant
			if (origPlatformFormat == TextureImporterFormat.ETC_RGB4)
			{
				// For ETC1 compressed textures, just add mipmapping and cleanup fields (above)
				// For larger ETC1 textures, also reduce size
				if (origPlatformMaxSize > SHRINK_SD_THRESHOLD) { newPlatformMaxSize = origPlatformMaxSize / 2; } 
			}
			else if (origPlatformFormat == TextureImporterFormat.ETC2_RGBA8)
			{
				// For ETC2 textures : start RGBA32, RESIZE, DITHER, RGBA4444, etc
				newPlatformFormat = TextureImporterFormat.RGBA32;
				newUserData += TextureProcessor.DITHER + TextureProcessor.MAKE4444;

				// For larger ETC2 textures, also reduce size
				if (origPlatformMaxSize > SHRINK_SD_THRESHOLD) { newPlatformMaxSize = origPlatformMaxSize / 2; } 
			}
			else
			{
				Debug.Log("SD4444 Skipping " + texturePath + "; not etc1/etc2; is: " + origPlatformFormat);
				return false;
			}
		}
		else
		{
			fatalError("no importer rules defined for variant: " + variant);
		}

		// Skip if no importer settings have changed
		if (TextureImporterSettings.Equal(origImporterSettings, newImporterSettings) &&
		    origPlatformFormat == newPlatformFormat &&
		    origPlatformMaxSize == newPlatformMaxSize &&
		    origPlatformQuality == newPlatformQuality &&
		    origUserData == newUserData)
		{
			Debug.LogWarning("Skipping " + texturePath + "; no effective importer changes");
			return false;
		}
		
		//Need to verify the texture size is actually being reduced.
		//Its possible for the texture's size to already be smaller than the new max import size, so reducing the importer size has no effect on the actual texture
		//TODO: Might want to use the texture's actualy size to determine the new importer size if we want to make sure we're actually reducing everything above the SD size threshold
		bool sizeActuallyReduced = newPlatformMaxSize < texture.width || newPlatformMaxSize < texture.height;
		if (newPlatformMaxSize == origPlatformMaxSize / 2 && sizeActuallyReduced)
		{
			//Could possibly assume the texture and atlas names should match up but not going to do that just to make sure we adjust an atlas if it needs to be
			//Currently only searching the folder of the texture for the atlas prefab, for efficiency and that should be the typical structure of our atlases
			bool foundAtlas = false;
			int texturenameLength = texture.name.Length + 5; //.Name doesn't include .png
			string folderPath = texturePath.Remove(texturePath.Length - texturenameLength, texturenameLength);
			string[] prefabsInFolder = AssetDatabase.FindAssets("t:prefab", new string[]{folderPath});
			
			foreach (string guid in prefabsInFolder)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				GameObject atlasObj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
				if (atlasObj != null)
				{
					UIAtlas atlas = atlasObj.GetComponent<UIAtlas>();
					if (atlas != null)
					{
						if (atlas.spriteMaterial != null)
						{
							if (atlas.spriteMaterial.mainTexture == texture)
							{
								string prefabPath = AssetDatabase.GetAssetPath(atlas);
								GameObject prefabObj = PrefabUtility.LoadPrefabContents(prefabPath);
								if (prefabObj != null)
								{
									UIAtlas prefabAtlas = prefabObj.GetComponent<UIAtlas>();
									if (prefabAtlas != null)
									{
										foundAtlas = true;

										//If the pixelSize isn't adjusted to compensate for the reduced size, sliced sprites appear incorrect
										prefabAtlas.pixelSize *= 2;
										PrefabUtility.SaveAsPrefabAsset(prefabObj, prefabPath);
										PrefabUtility.UnloadPrefabContents(prefabObj);
									}

									GameObject.DestroyImmediate(prefabObj);
								}
								break;
							}
						}
						else
						{
							Debug.LogErrorFormat("Atlas {0} has a null material.", atlas.name);
						}
					}
				}
			}
			
			Debug.LogFormat("Updated an atlas for {0} ? {1}", texture.name, foundAtlas);
		}


		// make a backup of (original) meta file for later restoration
		backupOriginalMetaFile(textureImporter);

		// Apply new settings to the existing texture importer
		//Debug.LogWarning("Updating Settings for " + texturePath + ";  count = " + modifiedCount);
		textureImporter.SetTextureSettings(newImporterSettings);
		textureImporter.userData = newUserData;
		CommonEditor.setTextureImporterOverrides( textureImporter, platformName, newPlatformMaxSize, newPlatformFormat, newPlatformQuality, false);


		// Save import settings in a cache-server friendly manner (imports asset from cache if possible during refresh)
		AssetDatabase.WriteImportSettingsIfDirty(textureImporter.assetPath);

		// Or... force?  (common when doing dev)
		// AssetDatabase.ImportAsset(textureImporter.assetPath, ImportAssetOptions.DontDownloadFromCacheServer);


		//Debug.Log("MODIFIED " + textureImporter.assetPath);
		//Debug.Log("Asset " + texturePath + "  origSize = " + origWidth + " x " + origHeight + "  assetsize = " + texture.width + " x " + texture.height + "   androidMaxSize = " + androidMaxSize + "   userData=" + textureImporter.userData);

		return true;
	}

	// Copying a metafile back will cause unity (upon refresh) to see the updated file and re-hash it, and it should quickly determine "nothing has changed"
	// TODO: FIRST-TIME restores sometimes seem to force a re-import; WHY? It's an identical metafile, but the combined assethash changes (???)
	public static void restoreAllTextureImporters()
	{
		bool verbose = true;
		var startTime = System.DateTime.Now;

		string[] texturePaths = getAllTextureAssetPaths();
		if (verbose) { Debug.Log("restoring (up to) " + texturePaths.Length + " texture importers"); }

		int count = 0;
		foreach (var texturePath in texturePaths)
		{
			bool didRestoreFile = restoreOriginalMetaFile(texturePath);
			if (didRestoreFile)
			{
				restoreAtlasForTexture(texturePath);
			}
			count += didRestoreFile ? 1 : 0;
		}

		if (verbose) { Debug.Log("actually restored " + count + " texture importers"); }
		if (verbose) { Debug.Log("restoreTextureImporters - Restoration time = " + (System.DateTime.Now - startTime)); }


		// Important we refresh, else we might destroy unity's perception of reality
		startTime = System.DateTime.Now;
		AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
		if (verbose)
		{
			Debug.Log("restoreTextureImporters - Refresh time = " + (System.DateTime.Now - startTime));
		}
	}

	public static void restoreAtlasForTexture(string texturePath)
	{
		Texture2D texture = (Texture2D)AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D));
		string folderPath = Path.GetDirectoryName(texturePath);
		string[] prefabsInFolder = AssetDatabase.FindAssets("t:prefab", new string[]{folderPath});
				
		foreach (string guid in prefabsInFolder)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(guid);
			GameObject atlasObj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			if (atlasObj != null) 
			{
				UIAtlas atlas = atlasObj.GetComponent<UIAtlas>();
				if (atlas != null)
				{
					Debug.LogFormat("Checking atlas {0} for modified texture {1}", atlas.name, texturePath);
					
					if (atlas.spriteMaterial == null)
					{
						Debug.LogErrorFormat("Found Atlas {0} with no material", atlas.name); //Non-fatal but worth investigating
					}
					else if (atlas.spriteMaterial.mainTexture == texture)
					{
						//Atlas texture matches the texture that had previously been adjusted for a non-HD Variant. 
						//Restoring the atlas to its default size also
						string prefabPath = AssetDatabase.GetAssetPath(atlas);
						GameObject prefabObj = PrefabUtility.LoadPrefabContents(prefabPath);
						if (prefabObj != null)
						{
							UIAtlas prefabAtlas = prefabObj.GetComponent<UIAtlas>();
							if (prefabAtlas != null)
							{
								prefabAtlas.pixelSize = 1;
								PrefabUtility.SaveAsPrefabAsset(prefabObj, prefabPath);
								PrefabUtility.UnloadPrefabContents(prefabObj);
							}

							GameObject.DestroyImmediate(prefabObj);
						}
						break;
					}
				}
			}
		}
	}



	// Get's the entire set of texture paths to consider for 
	static string[] getAllTextureAssetPaths()
	{
		bool verbose = false;

		// Get list of AlphaSplit textures via label (only fast way to do it)  575
		var alphaSplitTexturePaths = 
			AssetDatabase.FindAssets("t:texture2d 	l:" + AlphaSplitTextureInspector.ALPHA_SPLIT_LABEL)
			.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
			.Distinct().ToArray();
		if (verbose) { Debug.Log("alphaSplitTexturePaths (" + alphaSplitTexturePaths.Length + ") in project... :\n  " + string.Join("\n  ", alphaSplitTexturePaths)); }

		var texturePaths = 
			AssetDatabase.FindAssets("t:texture2d")                 // all textures
			.Select(guid => AssetDatabase.GUIDToAssetPath(guid))    // as assetPaths
			.Where(path => !path.Contains("Source Hi"))             // exclude atlas source bitmaps
			.Where(path => !path.Contains(".cubemap"))              // exclude cubemaps
			.Where(path => !path.Contains("/Editor/"))              // exclude /Editor/
			.Where(path => !path.Contains("/System/"))              // exclude /System/     (device icons, etc)  -134
			.Where(path => !path.Contains("/Plugins/"))             // exclude /Plugins/    (helpshift, etc)  -46
			.Where(path => !path.Contains("/Libraries/"))           // exclude /Libraries/  (TextMeshPro, etc)  -40
			.Where(path => !path.Contains("Size Enforcer"))         // exclude
			.Where(path => !alphaSplitTexturePaths.Contains(path))  // exclude parent AlphaSplit textures
		//	.Where(path => !AlphaSplitTextureInspector.isDerivedTexturePath(path))    // exclude derived RGB/Alpha alphasplit textures  -964
			.Distinct().ToArray();
		if (verbose) { Debug.Log("AllTextures (" + texturePaths.Length + ") in project... :\n  " + string.Join("\n  ", texturePaths)); }

		return texturePaths;  // ~ 7608
	}

	// Try to selectively reduce the potential set of modifiable textures by SKU,
	// including trying to eliminate texture dependencies via recognizable /Data/Games/.../Stuff/GamePrefix... patterns
	static string[] getTextureAssetPathsToOperateOn(SkuId sku, BuildTarget target)
	{
		//return new string[] { "Assets/Data/Games/t1/t101/Images/t101_wings.png", "Assets/Data/HIR/NGUI/Atlases/Dialog/Dialog Hi (RGB).png", "Assets/Data/HIR/NGUI/Atlases/Personalized Content/Personalized Content (RGB).png" };  //TEMP!
		bool verbose = false;


		var allSkuLabelPaths = AssetDatabase.FindAssets("l:HIR l:SIR l:TV").Select(guid => AssetDatabase.GUIDToAssetPath(guid)).Distinct().ToArray(); // 252
		var allBundleNames = AssetDatabase.GetAllAssetBundleNames(); // 545
		var allBundlePaths = AssetDatabase.FindAssets("b:").Select(guid => AssetDatabase.GUIDToAssetPath(guid)).Distinct().ToArray(); // 545
		var allBundleFolders = allBundlePaths.Select(path => path.Split('/').Last()).Where(folder => !folder.Contains('.')).Distinct().ToArray();  //excludes filenames, 330 (filter this down?)

		var skuLabelPaths = AssetDatabase.FindAssets("l:"+sku).Select(guid => AssetDatabase.GUIDToAssetPath(guid)).Distinct().ToArray(); // 161
		var skuBundleNames = CreateAssetBundlesV2.getAssetBundleNamesForSku(sku); // SLOW!!! 30+ seconds...   219 items
		var skuBundlePaths = allBundlePaths.Where(bundlePath => skuLabelPaths.Any(labelPath => bundlePath.FastStartsWith(labelPath))).ToArray(); // 256
		var skuBundleFolders = skuBundlePaths.Select(path => path.Split('/').Last()).Where(folder => !folder.Contains('.')).Distinct().ToArray();  //

		// start with entire set for consideration
		var allTexturePaths = getAllTextureAssetPaths();
		if (verbose) { Debug.Log("Textures (" + allTexturePaths.Length + ") in project... :\n  " + string.Join("\n  ", allTexturePaths)); } // 7225

		// Keep assets that are labelled for our sku, OR not in any sku (possible dependencies); (eliminates assets from other-sku's)
		// We're trusting the sku labels to be correct; we will miss mis-marked textures that are pulled as dependencies (not fatal)
		
		string[] filteredTexturePaths;
		if (target != BuildTarget.WebGL && target != BuildTarget.iOS)
		{
			// no longer resizing assets that belong to the mega bundle. app size remains the same since the mega bundle
			// we add to the app is always the HD version.
			string[] initBundleAssets = AssetDatabase.GetAssetPathsFromAssetBundle("initialization");
			string[] assetDependencies = AssetDatabase.GetDependencies(initBundleAssets, true);
			filteredTexturePaths =
				allTexturePaths.Where( // Keep if asset
					path => !assetDependencies.Contains(path) && // is it in the initialization bundle?
					        (skuLabelPaths.Any(labelPath =>
						         path.FastStartsWith(labelPath)) || // belongs to this sku? or...
					         !allSkuLabelPaths.Any(bundlePath => path.FastStartsWith(bundlePath))) // not in any sku?
				).ToArray(); //7574
		}
		else
		{
			//Still resizing textures in the initialization bundle on WebGL since it only uses SD bundles
			filteredTexturePaths =
				allTexturePaths.Where( // Keep if asset
					path => (skuLabelPaths.Any(labelPath => path.FastStartsWith(labelPath)) || // belongs to this sku? or...
					         !allSkuLabelPaths.Any(bundlePath => path.FastStartsWith(bundlePath))) // not in any sku?
				).ToArray(); //7574
		}

		// ugh, determine whether to keep various /Game/.../Stuff/... assets, which are often pulled as dependencies (not tagged/labelled)
		// (This is an important step; it identifies over 1700+ textures we should leave alone)
		//
		// ie:    Data/Games/tv/tv01  (with bundle tag & label)
		// uses:  Data/Games/tv/Stuff/tv01 Diamond Cut/...
		//
		// Various /Stuff/... subfolder names we try to identify
		//   OSA06_FSReelBackground_Mask.png  
		//   Oz00_FS_Banner_m.png  
		//   ainsworth01/Textures/ains01_BaseBackground.png  
		//   ani02 Rhino/Textures/ANI02_F6_Drum.png  
		//   batman begins/bb01 Portal/textures/iconParticle.png     <== problematic
		//   Textures/cesar01_FSreelBackground_alpha.png             <== (must skip Textures/)
		//   t101/t101 crusher/Texture/T1_CommonBonus_Pickme_sheen_01.png    <== different bundle name (term101) but it's applied to "t101" folder name

		// returns true if path matches:  Assets/Data/Games/.../Stuff/...
		Func<string, bool> isGameStuff = (path) => path.FastStartsWith("Assets/Data/Games/") && path.Contains("/Stuff/");
		Func<string, bool> isResourceStuff = (path) => path.Contains("/Resources/");

		// tries to return the first pertinant keyword from the stuff subfolder name
		Func<string, string> getBaseNameFromStuffAssetPath = (path) => 
		{
			return path
				.Split( new string[] {"/Stuff/"}, 2, StringSplitOptions.None)[1]     // folder name after /stuff/
				.Replace("Textures/", "")                                            // replace Textures/ prefix (if any)
				.Split( new char[] {' ', '_', ',', '/'}, 2)[0].ToLower();            // isolate folder basename
		};

		// for lookups
		var allBundleNamesAndFolders = allBundleNames.Union(allBundleFolders).ToArray();
		var skuBundleNamesAndFolders = skuBundleNames.Union(skuBundleFolders).ToArray();

		// Keep if keyword matches sku bundlename or sku Bundle foldername or NOT any bundlename/foldername
		Func<string, bool> isKeeper = (keyword) => skuBundleNamesAndFolders.Contains(keyword) || !allBundleNamesAndFolders.Contains(keyword);

		// for assetPath's in gamestuff, only keep if the folder basename matches an sku bundlename/foldername
		var texturePaths = filteredTexturePaths
			.Where(
				path => !isResourceStuff(path) &&
				(!isGameStuff(path) ||
				 isKeeper(getBaseNameFromStuffAssetPath(path)) )
			).ToArray();
		logAndCopy("final texturePaths", texturePaths);  // 4827

		/* testing code...
		var gameStuffPaths = texturePaths.Where(path => isGameStuff(path)).ToArray();
		var keepers    = gameStuffPaths.Where( path => isKeeper( getBaseNameFromStuffAsset(path) ) ).ToArray(); // 3208, in HIR bundle OR no bundle
		var inSku      = gameStuffPaths.Where( path => hirBundleNamesAndFolders.Contains( getBaseNameFromStuffAsset(path) ) ).ToArray(); // 2979  known to be in HIR bundle/folder
		var inNoBundle = gameStuffPaths.Where( path => !allBundleNamesAndFolders.Contains( getBaseNameFromStuffAsset(path) ) ).ToArray(); // 229, we keep this
		var discards   = gameStuffPaths.Where( path => !isKeeper( getBaseNameFromStuffAsset(path) ) ).ToArray(); // 1716 ! well worth it!
		*/

		return texturePaths;

	}

	// temp debug logging to console & clipboard (for copy-pasta)
	static void logAndCopy(string name, string[] strings)
	{
		var msg = name + "... count = " + strings.Length + "...   (COPIED TO CLIPBOARD)...\n" + string.Join("  \n", strings);
		Debug.Log(msg);
		EditorGUIUtility.systemCopyBuffer = msg;
	}


	// The folder we backup and restore .meta files to/from
	static string metafileBackupFolder = "../build/bundlesv2/temp/temp-texture-meta-backups";

	// Makes a backup of a textureImporter meta file, so it can be restored later, without altering the metafile's contents
	// This is the only way to guarantee perfect restoration of a meta file, otherwise unity might alter it, causing textures to re-import
	//
	//	IE: Copy    Assets/Data/Games/t1/t101/Images/t101_summary_icon.png.meta  
	//  to         ../build/bundlesv2/texture_meta_backups/Assets.Data.Games.t1.t101.Images.t101.summary.icon.png.meta
	//
	static void backupOriginalMetaFile( TextureImporter textureImporter )
	{
		// Create folder if it doesn't exist
		Directory.CreateDirectory(metafileBackupFolder);

		// Don't backup any temporary importer variant overrides (marked with a "TempOverride" userdata string)
		if (textureImporter.userData.FastStartsWith(TEMPOVERRIDE_KEYWORD))
		{
			Hash128 hash = AssetDatabase.GetAssetDependencyHash(textureImporter.assetPath);
			// Log non-fatal build error
			string errorLog = "Couldn't backup texture: " + textureImporter.assetPath + ", it is a temporary override: " + textureImporter.userData
							+ "; This might mean a broken version is stored in the Unity Cache Server.  Asset hash for current build target is: " + hash;
			CommonEditor.logNonFatalErrorToBuildErrorsLog(errorLog);
			return;
		}

		// Change filename... replace path's slashes with underscores so we can see them all in one place
		var originalMetaFilePath = getImporterMetaFilePath(textureImporter.assetPath);
		var backupMetaFilePath = getBackupMetaFilePath(textureImporter.assetPath);

		// copy filename...
		//Debug.Log("SHOULD COPY " + originalMetaFilePath + "   to   " + backupMetaFilePath);
		File.Copy(originalMetaFilePath, backupMetaFilePath, true); //overwrite
	}

	// Restores a previously backed-up meta file (if the backup exists); then deletes the backup
	// returns true if it actually found & restored a meta file
	public static bool restoreOriginalMetaFile( string assetPath )
	{
		// Change filename... replace path's slashes with underscores so we can see them all in one place
		var originalMetaFilePath = getImporterMetaFilePath(assetPath);
		var backupMetaFilePath = getBackupMetaFilePath(assetPath);
		var restoredFile = false;

		if (File.Exists(backupMetaFilePath))
		{
			File.Copy(backupMetaFilePath, originalMetaFilePath, true); //overwrite
			File.Delete(backupMetaFilePath);
			
			// Update this so that we hopefully avoid having modified meta files
			// permanently saved to the cache server in place of the originals.
			AssetDatabase.WriteImportSettingsIfDirty(assetPath);
			
			restoredFile = true;
		}

		return restoredFile;
	}

	static string getImporterMetaFilePath(string assetPath)
	{
		return assetPath + ".meta"; 
	}

	static string getBackupMetaFilePath(string assetPath)
	{
		// Replaces assetpath slashes with periods, so all backed-up meta files can live in a single folder
		return metafileBackupFolder + "/" + assetPath.Replace('/', '.') + ".meta";
	}

	static void fatalError(string msg)
	{
		throw new System.Exception(msg);
	}

}
