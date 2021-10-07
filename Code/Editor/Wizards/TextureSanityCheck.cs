using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

/**
This scans over all textures in the project for suspitious import settings.
It will auto-correct compressed textures that are not high-quality for whatever reason.
Other warning flagged textures will appear in the various array buckets.

It is encouraged that we expand this gradually to do full texture validation.
*/
public class TextureSanityCheck : ScriptableWizard
{
	[System.Serializable] 
	public class TextureReference
	{
		public string path;
		public int maxSize;
		public TextureFormat format;
		public Texture2D texture;
	}
	
	public Texture textureLocation = null;
	public string folderToCheck = "Assets/-Temporary Storage-";
	public bool performScan = false;
	public TextureReference[] nonsquare;
	public TextureReference[] large;
	public TextureReference[] mipped;
	public TextureReference[] pixelReadWrite;
	public TextureReference[] uncompressed;
	public TextureReference[] compressed;
	public TextureReference[] badFormat;
	public TextureReference[] all;
	
	[MenuItem ("Zynga/Wizards/Texture Sanity Check")]
	public static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<TextureSanityCheck>("Texture Sanity Check", "Close");
	}
	
	public void OnWizardUpdate()
	{
		if (performScan)
		{
			performScan = false;
			scanTextures();
		}

		if (textureLocation != null)
		{
			folderToCheck = Path.GetDirectoryName(AssetDatabase.GetAssetPath(textureLocation));
			textureLocation = null;
		}
		
		helpString =
			"After entering a 'Folder To Check', check the 'Perform Scan' button to run the checks." +
			"\n Example folders to check:" + 
			"\n  Assets/-Temporary Storage-" +
			"\n  Assets/Data/Common/NGUI/" +
			"\n  Assets/Data/HIR/Resources/Images" +
			"\n\n NOTES: Processing too many files in one sweep will crash Unity!";
	}
	
	public void OnWizardCreate()
	{
	
	}
	
	private void scanTextures()
	{
		Debug.Log("Begin scanning textures.");

		// This improves performance
		AssetDatabase.StartAssetEditing();
		
		List<TextureReference> nonsquareList = new List<TextureReference>();
		List<TextureReference> largeList = new List<TextureReference>();
		List<TextureReference> mippedList = new List<TextureReference>();
		List<TextureReference> pixelReadWriteList = new List<TextureReference>();
		List<TextureReference> uncompressedList = new List<TextureReference>();
		List<TextureReference> compressedList = new List<TextureReference>();
		List<TextureReference> badFormatList = new List<TextureReference>();
		List<TextureReference> allList = new List<TextureReference>();
		
		int errorCount = 0;
		int correctionCount = 0;
		int totalCount = 0;		

		foreach (Texture2D texture in CommonEditor.gatherAssets<Texture2D>(folderToCheck, "/Source Hi"))
		{
			string texturePath = AssetDatabase.GetAssetPath(texture);
			TextureImporter importer = TextureImporter.GetAtPath(texturePath) as TextureImporter;
		
			// Validate the importer
			if (importer == null)
			{
				string lowerPath = texturePath.ToLower();
				if (!(lowerPath.FastEndsWith(".ttf") || lowerPath.FastEndsWith(".otf")))
				{
					// Toss an error if this isn't a font
					Debug.LogError("Texture had no importer: " + texturePath);
					errorCount++;
				}
				continue;
			}
			
			if (checkMobileTextureSettings(texturePath, importer))
			{
				correctionCount++;
			}
			
			// Generate texture reference for scan results
			TextureReference reference = new TextureReference();
			reference.path = texturePath;
			reference.texture = texture;
			reference.maxSize = importer.maxTextureSize;
			reference.format = texture.format;
			allList.Add(reference);
		
			if (texture.width != texture.height || !Mathf.IsPowerOfTwo(texture.width))
			{
				nonsquareList.Add(reference);
			}
		
			if (Mathf.Max(texture.width, texture.height) > 1024)
			{
				largeList.Add(reference);
			}
		
			if (importer.mipmapEnabled)
			{
				mippedList.Add(reference);
			}
			
			if (importer.isReadable)
			{
				pixelReadWriteList.Add(reference);
			}
		
			switch (texture.format)
			{
				case TextureFormat.Alpha8:
				case TextureFormat.RGB24:
				case TextureFormat.RGBA32:
				case TextureFormat.ARGB32:
					uncompressedList.Add(reference);
					break;
					
				case TextureFormat.PVRTC_RGB4:
				case TextureFormat.PVRTC_RGBA4:
				case TextureFormat.ETC_RGB4:
				case TextureFormat.ETC2_RGBA8:
					compressedList.Add(reference);
					break;

				default:
					badFormatList.Add(reference);
					errorCount++;
					break;
			}
			
			totalCount++;
		}
		
		nonsquare = nonsquareList.ToArray();
		large = largeList.ToArray();
		mipped = mippedList.ToArray();
		pixelReadWrite = pixelReadWriteList.ToArray();
		uncompressed = uncompressedList.ToArray();
		compressed = compressedList.ToArray();
		badFormat = badFormatList.ToArray();
		all = allList.ToArray();
		
		AssetDatabase.StopAssetEditing();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
		
		Debug.Log(string.Format("Done scanning textures, {0} corrections, {1} errors, {2} total.", correctionCount, errorCount, totalCount));		
	}
	
	
	/// Updates mobile-specific texture settings for a given texture
	/// The wizard should call with isForceSave = true as a fix for the import package bug,
	/// which keeps the settings from the package even if they are wrong.
	public static bool checkMobileTextureSettings(string texturePath, TextureImporter importer, bool isForceSave = false, bool isLoggingSuccesses = true)
	{
		// default quality levels
		int desiredAppleQuality = 100;
		int desiredAndroidQuality = 50;
		int desiredWindowsQuality = 100;

		// Platform specific importer settings
		TextureImporterPlatformSettings appleSettings = importer.GetPlatformTextureSettings("iPhone");
		TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
		TextureImporterPlatformSettings windowsSettings = importer.GetPlatformTextureSettings("Windows Store Apps");
		TextureImporterPlatformSettings webSettings = importer.GetPlatformTextureSettings("WebGL");
		
		// Want equivelant texture formats for all platforms; evaluate the first override that we find, else use the default setting
		// (This means if you change an iOS setting and run the tool, it might change the other platforms;
		//  but if you change an android setting and run the tool, the android setting will get overridden by the iOS choice.)
 		TextureImporterFormat checkFormat;
		if (appleSettings.overridden)
		{
			checkFormat = appleSettings.format;
		}
		else if (androidSettings.overridden)
		{
			checkFormat = androidSettings.format;
		}
		else if (windowsSettings.overridden)
		{
			checkFormat = windowsSettings.format;
		}
		else
		{
			// No overrides set yet, And With Unity 5.5, textureImporter no longer contains a base textureFormat, so use "automatic"
			checkFormat = TextureImporterFormat.Automatic;
		}

		// Set equivelant formats for iOS, Android, and Windows 
		TextureImporterFormat desiredAppleFormat;
		TextureImporterFormat desiredAndroidFormat;
		TextureImporterFormat desiredWindowsFormat;

		switch (checkFormat)
		{
			case TextureImporterFormat.Automatic:  // Handle the 'no overrides set' case
				switch (importer.textureCompression)
				{
					case TextureImporterCompression.Uncompressed:
						if (importer.DoesSourceTextureHaveAlpha())
						{
							desiredAppleFormat = TextureImporterFormat.RGBA32;
							desiredAndroidFormat = TextureImporterFormat.RGBA32;
							desiredWindowsFormat = TextureImporterFormat.RGBA32;
						}
						else
						{
							desiredAppleFormat = TextureImporterFormat.RGB24;
							desiredAndroidFormat = TextureImporterFormat.RGB24;
							desiredWindowsFormat = TextureImporterFormat.RGB24;
						}
						break;

					case TextureImporterCompression.Compressed:
					case TextureImporterCompression.CompressedLQ:
					case TextureImporterCompression.CompressedHQ:
						if (importer.DoesSourceTextureHaveAlpha())
						{
							desiredAppleFormat = TextureImporterFormat.PVRTC_RGBA4;
							desiredAndroidFormat = TextureImporterFormat.ETC2_RGBA8;
							desiredWindowsFormat = TextureImporterFormat.DXT5;
						}
						else
						{
							desiredAppleFormat = TextureImporterFormat.PVRTC_RGB4;
							desiredAndroidFormat = TextureImporterFormat.ETC_RGB4;
							desiredWindowsFormat = TextureImporterFormat.DXT1;
						}
						break;

					default:
						Debug.LogError("Unknown importer.textureCompression: " + importer.textureCompression + " for asset: " + texturePath);
						return false;
				}
				break;
			
			case TextureImporterFormat.Alpha8:
				desiredAppleFormat = TextureImporterFormat.Alpha8;
				desiredAndroidFormat = TextureImporterFormat.Alpha8;
				desiredWindowsFormat = TextureImporterFormat.Alpha8;
				break;
			
			case TextureImporterFormat.ARGB16:
			case TextureImporterFormat.RGBA16:
				desiredAppleFormat = TextureImporterFormat.RGBA16;
				desiredAndroidFormat = TextureImporterFormat.RGBA16;
				desiredWindowsFormat = TextureImporterFormat.RGBA16;
				break;
				
			case TextureImporterFormat.RGB24:
				desiredAppleFormat = TextureImporterFormat.RGB24;
				desiredAndroidFormat = TextureImporterFormat.RGB24;
				desiredWindowsFormat = TextureImporterFormat.RGB24;
				break;
				
			case TextureImporterFormat.ARGB32:
			case TextureImporterFormat.RGBA32:
				desiredAppleFormat = TextureImporterFormat.RGBA32;
				desiredAndroidFormat = TextureImporterFormat.RGBA32;
				desiredWindowsFormat = TextureImporterFormat.RGBA32;
				break;
			
			case TextureImporterFormat.PVRTC_RGB2:
			case TextureImporterFormat.PVRTC_RGB4:
			case TextureImporterFormat.ETC_RGB4:
			case TextureImporterFormat.ETC2_RGB4:
			case TextureImporterFormat.DXT1:
				desiredAppleFormat = TextureImporterFormat.PVRTC_RGB4;
				desiredAndroidFormat = TextureImporterFormat.ETC_RGB4;
				desiredWindowsFormat = TextureImporterFormat.DXT1;
				break;
			
			case TextureImporterFormat.PVRTC_RGBA2:
			case TextureImporterFormat.PVRTC_RGBA4:
			case TextureImporterFormat.ETC2_RGBA8:
			case TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA:
			case TextureImporterFormat.DXT5:
				desiredAppleFormat = TextureImporterFormat.PVRTC_RGBA4;
				desiredAndroidFormat = TextureImporterFormat.ETC2_RGBA8;
				desiredWindowsFormat = TextureImporterFormat.DXT5;
				break;

			default:
				Debug.LogError("Unhandled importer.textureFormat: " + checkFormat + " for asset: " + texturePath);
				return false;
		}

		// If an alpha-split main or derived texture, gets the desired texture settings; else they're unchanged
		AlphaSplitTextureInspector.getDesiredTextureSettings(texturePath,
			ref desiredAndroidFormat, ref desiredAndroidQuality, 
			ref desiredAppleFormat, ref desiredAppleQuality,
			ref desiredWindowsFormat, ref desiredWindowsQuality);

		// A note about using this "isChanged" flag. Ideally - we would just update and write new importer settings if dirty.
		// HOWEVER... That results in rewriting the importer meta file with a newer version fileformat, which will force a re-import
		// So, we will decide if there are any effective changes worthy of updating the importer, else we let it remain as-is

		bool isChanged = false;
		
		if (importer.compressionQuality != 100)
		{
			importer.compressionQuality = 100;
			isChanged = true;
		}
		
		if (importer.npotScale != TextureImporterNPOTScale.None && importer.npotScale != TextureImporterNPOTScale.ToLarger)
		{
			importer.npotScale = TextureImporterNPOTScale.ToLarger;
			isChanged = true;
		}

		// Set all the platform maxSize's to the smallest maxSize we see (if you reduce one, they should all get reduced)
		int desiredMaxSize = Mathf.Min(importer.maxTextureSize, appleSettings.maxTextureSize, androidSettings.maxTextureSize, windowsSettings.maxTextureSize);
		if (importer.maxTextureSize != desiredMaxSize)
		{
			importer.maxTextureSize = desiredMaxSize;
			isChanged = true;
		}

		if (!appleSettings.overridden || appleSettings.compressionQuality != desiredAppleQuality || appleSettings.format != desiredAppleFormat || appleSettings.maxTextureSize != desiredMaxSize)
		{
			appleSettings.overridden = true;
			appleSettings.format = desiredAppleFormat;
			appleSettings.compressionQuality = desiredAppleQuality;
			appleSettings.maxTextureSize = desiredMaxSize;
			appleSettings.allowsAlphaSplitting = false;

			importer.SetPlatformTextureSettings(appleSettings);
			isChanged = true;
		}
		
		if (!androidSettings.overridden || androidSettings.compressionQuality != desiredAndroidQuality || androidSettings.format != desiredAndroidFormat || androidSettings.maxTextureSize != desiredMaxSize)
		{
			androidSettings.overridden = true;
			androidSettings.format = desiredAndroidFormat;
			androidSettings.compressionQuality = desiredAndroidQuality;
			androidSettings.maxTextureSize = desiredMaxSize;
			androidSettings.allowsAlphaSplitting = false;

			importer.SetPlatformTextureSettings(androidSettings);
			isChanged = true;
		}

		if (!windowsSettings.overridden || windowsSettings.compressionQuality != desiredWindowsQuality || windowsSettings.format != desiredWindowsFormat || windowsSettings.maxTextureSize != desiredMaxSize)
		{
			windowsSettings.overridden = true;
			windowsSettings.format = desiredWindowsFormat;
			windowsSettings.compressionQuality = desiredWindowsQuality;
			windowsSettings.maxTextureSize = desiredMaxSize;
			windowsSettings.allowsAlphaSplitting = false;

			importer.SetPlatformTextureSettings(windowsSettings);
			isChanged = true;
		}
		
		if (!webSettings.overridden || webSettings.compressionQuality != desiredWindowsQuality || webSettings.format != desiredWindowsFormat || webSettings.maxTextureSize != desiredMaxSize)
		{
			webSettings.overridden = true;
			webSettings.format = desiredWindowsFormat;
			webSettings.compressionQuality = desiredWindowsQuality;
			webSettings.maxTextureSize = desiredMaxSize;
			webSettings.allowsAlphaSplitting = false;

			importer.SetPlatformTextureSettings(webSettings);
			isChanged = true;
		}

		if (isChanged || isForceSave)
		{
			// Save import settings in a cache-server friendly manner (imports asset from cache if possible during refresh)
			AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath);

			if (isLoggingSuccesses)
			{
				Debug.Log("Set texture import settings for: " + texturePath);
			}
		}

		return isChanged;
	}
}
