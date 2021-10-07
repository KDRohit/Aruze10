using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/**
This class hooks into Unity's asset import process and sets some
default settings for audio, textures, and models.
*/

public class AssetsImport : AssetPostprocessor
{
	private static System.DateTime importStartTime = System.DateTime.MinValue;
	private static bool importStarted = false;

	private void setImportStartTime()
	{
		if (importStartTime == System.DateTime.MinValue)
		{
			importStarted = true;
			importStartTime = System.DateTime.Now;
		}
	}

	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
	{
		if (importStarted && importedAssets != null && importedAssets.Length > 0)
		{
			// Logging is too spammy while using editor; but maybe we enable it while running in batched mode? TBD
			// System.TimeSpan totalImportTime = System.DateTime.Now - importStartTime;
			// Debug.Log("Import time for " + importedAssets.Length + " assets was " + totalImportTime.TotalSeconds + " seconds.");
		}
		
		importStarted = false;
		importStartTime = System.DateTime.MinValue;
	}
	
	/// When loading sounds, set some default settings
	void OnPreprocessAudio()
	{
		setImportStartTime();
		if (!assetImporter.importSettingsMissing)
		{
			// If a meta file exists, then someone else committed this asset and takes responsibility for it.
			// This class should never change meta file settings that may have already been purposely set.
			return;
		}
		
		AudioImporter importer = (AudioImporter)assetImporter;

		importer.forceToMono = true;
		importer.preloadAudioData = true;
		importer.loadInBackground = false;
		
		AudioImporterSampleSettings defaultSettings = new AudioImporterSampleSettings();
		defaultSettings.compressionFormat = AudioCompressionFormat.Vorbis;
		defaultSettings.loadType = AudioClipLoadType.CompressedInMemory; // Decompress on load for specific cases. 
		defaultSettings.quality = 0.04f;
		defaultSettings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
		
		importer.defaultSampleSettings = defaultSettings;
		
		EditorUtility.SetDirty(assetImporter);
		assetImporter.SaveAndReimport();
	}

	/// When loading textures, process them based on asset path
	void OnPreprocessTexture()
	{
		setImportStartTime();
		if (!assetImporter.importSettingsMissing)
		{
			// If a meta file exists, then someone else committed this asset and takes responsibility for it.
			// This class should never change meta file settings that may have already been purposely set.
			return;
		}
		
		if (assetPath.Contains("/Source Hi/"))
		{
			processTextureForAtlasSource(assetPath, (TextureImporter)assetImporter);
		}
		else
		{
			processTextureForDynamicLoading(assetPath, (TextureImporter)assetImporter);
		}

		TextureSanityCheck.checkMobileTextureSettings(assetPath, (TextureImporter)assetImporter, true, false);
	}
	
	/// When loading 3D models, process them based on whether they have a @ in the path or not
	void OnPreprocessModel()
	{
		setImportStartTime();
		if (!assetImporter.importSettingsMissing)
		{
			// If a meta file exists, then someone else committed this asset and takes responsibility for it.
			// This class should never change meta file settings that may have already been purposely set.
			return;
		}
	 	
	 	if (assetPath.ToLower().Contains('@'))
		{
			processModelAnimated(assetPath, (ModelImporter)assetImporter);
		}
		else
		{
			processModelStatic(assetPath, (ModelImporter)assetImporter);
		}
		
		EditorUtility.SetDirty(assetImporter);
		assetImporter.SaveAndReimport();
	}
	
	[MenuItem("Zynga/Save Unsaved Assets %#a")] public static void menuSaveUnsavedAssets()
	{
		AssetDatabase.SaveAssets();
		Debug.Log("Assets saved successfully.");
	}
	
	// Set properties for atlas source sprites.
	public static void processTextureForAtlasSource(string texturePath, TextureImporter textureImporter)
	{
		textureImporter.textureType = TextureImporterType.Default;
		textureImporter.textureShape = TextureImporterShape.Texture2D;
		textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
		textureImporter.textureCompression = TextureImporterCompression.Uncompressed; // dont compress source assets
		textureImporter.npotScale = TextureImporterNPOTScale.None;

		textureImporter.anisoLevel = 0;
		textureImporter.filterMode = FilterMode.Point;
		textureImporter.wrapMode = TextureWrapMode.Clamp;
		textureImporter.isReadable = false;
		textureImporter.mipmapEnabled = false;
		textureImporter.convertToNormalmap = false;
		
		textureImporter.maxTextureSize = 2048;
	}

	/// Set specific properties for UI textures that are dynamically loaded.
	private static void processTextureForDynamicLoading(string texturePath, TextureImporter textureImporter)
	{
		textureImporter.textureType = TextureImporterType.Default;
		textureImporter.textureShape = TextureImporterShape.Texture2D;
		textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
		textureImporter.textureCompression = TextureImporterCompression.Compressed; // compress runtime assets
		textureImporter.npotScale = TextureImporterNPOTScale.ToLarger;

		textureImporter.anisoLevel = 0;
		textureImporter.filterMode = FilterMode.Bilinear;
		textureImporter.wrapMode = TextureWrapMode.Clamp;
		textureImporter.isReadable = false;
		textureImporter.mipmapEnabled = false;
		textureImporter.convertToNormalmap = false;

		textureImporter.maxTextureSize = 2048;
	}
	
	 /// Process an "animated" model
	private static void processModelAnimated(string modelPath, ModelImporter modelImporter)
	{
		modelImporter.optimizeMesh = true;
		modelImporter.globalScale = 1f;
		modelImporter.importMaterials = false;
		modelImporter.useFileUnits = false;
		modelImporter.addCollider = false;
		modelImporter.swapUVChannels = false;
		modelImporter.generateAnimations = ModelImporterGenerateAnimations.InRoot;
		modelImporter.animationWrapMode = WrapMode.Default;
	}
	
	/// Process some generic model
	private static void processModelStatic(string modelPath, ModelImporter modelImporter)
	{
		modelImporter.optimizeMesh = true;
		modelImporter.globalScale = 1f;
		modelImporter.importMaterials = false;
		modelImporter.useFileUnits = false;
		modelImporter.addCollider = false;
		modelImporter.swapUVChannels = false;
		//modelImporter.generateAnimations = ModelImporterGenerateAnimations.None;
	}
}
