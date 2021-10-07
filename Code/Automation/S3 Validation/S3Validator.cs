using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[ExecuteInEditMode]
[InitializeOnLoad]
public class S3Validator
{
	public static S3Validator _instance = null;

	private static string CANONICAL_URL = "{0}Images/{1}?version={2}";
	private static string BASE_BUNDLE_IMAGES_PATH = Path.Combine(Application.dataPath, "Data/HIR/Bundles/Images");
	private const int ONE_BY_TWO_MAX_SIZE = 512;
	private const int LOBBY_OPTION_MAX_SIZE = 256;
	private const int INBOX_ICON_MAX_SIZE = 128;

	public string[] gameKeyList = null;	// used for building pop up menus
	public int pickedGameIndex = 0;

	private string mobileStreamingAssetsUrl = "BAD_URL";
	private int mobileStreamingAssetsVersion = -1;
	private bool calledAsScript = false;

	public S3Validator()
	{
		Data.loadConfig();
		discoverGamesToTest();
		initStreamingUrlAndVersionInfo();
	}

	//requests data from server and stores mobileStreamingAssetsUrl and version number
	private void initStreamingUrlAndVersionInfo()
	{
		string url = Data.basicDataUrl;
		string locale = PlayerPrefsCache.GetString(DebugPrefs.LOCALE);

		if (string.IsNullOrEmpty(locale) || locale == "none")
		{
			locale = "";
		}

		Dictionary<string, string> elements = new Dictionary<string, string>();

		// We need to use StatsManager.ClientID here because ZyngaConstants.ClientId isn't set yet.
		elements["client_id"] = ((int)StatsManager.ClientID).ToString();

		WWW www = Server.getRequestWWW(url, elements);

		while (!www.isDone)
		{
			Thread.Sleep(100);
		}

		if (www.error == null)
		{
			JSON response = new JSON(www.text);
			LiveData liveData = new LiveData(response);

			mobileStreamingAssetsUrl = response.getString("mobile_streaming_assets_base_url", "BAD_URL");
			mobileStreamingAssetsVersion = response.getInt("STREAMING_ASSET_VERSION", 0);
		}
		else
		{
			Debug.LogErrorFormat("Error getting mobile streaming assets info at {0}: {1}", url, www.error);
			if (calledAsScript)
			{
				EditorApplication.Exit(1);
			}
		}
	}

	//only one instance of the S3 validator can exist at a time
	public static S3Validator instance
   	{
      get
      {
         if (_instance == null)
         {
            _instance = new S3Validator();
         }
         return _instance;
      }
   	}

	public string curGameKey
	{
		get
		{
			if (gameKeyList != null)
			{
				return gameKeyList[pickedGameIndex];
			}
			return "";
		}
	}

	// Generate folder name from gameKey, e.g. for "bev02" return "bev".
	private string getFolderName(string gameKey)
	{
		var data = SlotResourceMap.getData(gameKey);
		if (data != null)
		{
			return data.getGroupName();
		}
		else
		{
			// Fallback but this should never happen.
			return gameKey.Substring(0, gameKey.Length - 2);
		}
	}

	// Generate file path for lobby options image file from gameKey.
	private string getLobbyOptionsPath(string gameKey)
	{
		string lobbyOptionsBasePath = Path.Combine(BASE_BUNDLE_IMAGES_PATH, "lobby_options");
		string fullBasePath = Path.Combine(lobbyOptionsBasePath, getFolderName(gameKey));
		string fileBaseName = string.Format("{0}.png", gameKey);
		return Path.Combine(fullBasePath, fileBaseName);
	}

	// Generate file path for lobby icons image file from gameKey.
	private string getLobbyIconsPath(string gameKey)
	{
		string lobbyOptionsBasePath = Path.Combine(BASE_BUNDLE_IMAGES_PATH, "inbox_icons");
		string fileBaseName = string.Format("{0}.png", gameKey);
		return Path.Combine(lobbyOptionsBasePath, fileBaseName);
	}

	// Generate partial path to asset on S3 from bundled lobby image path, e.g. starting after BASE_BUNDLE_IMAGES_PATH.
	private string getPartialS3Path(string filePath)
	{
		int index = BASE_BUNDLE_IMAGES_PATH.Length + 1; // Add 1 for path separator.
		return filePath.Substring(index);
	}

	// Generate path relative to Application.dataPath from full path.
	private string getAssetDataPathRelativePath(string filePath)
	{
		return Path.Combine("Assets", filePath.Substring(Application.dataPath.Length + 1));	// Add 1 for path separator.
	}

	public void forceUpdateLobbyOptionImages(string gameKey)
	{
		validateAndImport(gameKey, lobbyOption:true, doImport:true, forceImport:true);
	}

	public void forceUpdateAllLobbyOptionImages()
	{
		foreach (string gameKey in gameKeyList)
		{
			forceUpdateLobbyOptionImages(gameKey);
		}
	}

	public void forceUpdateLobbyIconImages(string gameKey)
	{
		validateAndImport(gameKey, lobbyOption:false, doImport:true, forceImport:true);
	}

	public void forceUpdateAllLobbyIconImages()
	{
		foreach (string gameKey in gameKeyList)
		{
			forceUpdateLobbyIconImages(gameKey);
		}
	}

	public void validateLobbyOptionImages(string gameKey)
	{
		validateAndImport(gameKey, lobbyOption:true, doImport:false, forceImport:false);
	}

	public void validateAllLobbyOptions()
	{
		foreach (string gameKey in gameKeyList)
		{
			validateLobbyOptionImages(gameKey);
		}
	}

	public void validateLobbyIconImages(string gameKey)
	{
		validateAndImport(gameKey, lobbyOption:false, doImport:false, forceImport:false);
	}

	public void validateAllLobbyIcons()
	{
		foreach (string gameKey in gameKeyList)
		{
			validateLobbyIconImages(gameKey);
		}
	}

	public void importMissingLobbyOptionImages(string gameKey)
	{
		validateAndImport(gameKey, lobbyOption:true, doImport:true, forceImport:false);
	}

	public void importAllMissingLobbyOptionImages()
	{
		foreach (string gameKey in gameKeyList)
		{
			importMissingLobbyOptionImages(gameKey);
		}
	}

	public void importMissingLobbyIconImages(string gameKey)
	{
		validateAndImport(gameKey, lobbyOption:false, doImport:true, forceImport:false);
	}

	public void importAllMissingLobbyIconImages()
	{
		foreach (string gameKey in gameKeyList)
		{
			importMissingLobbyIconImages(gameKey);
		}
	}

	public void updateLobbyIconImageSettings(string gameKey)
	{
		if (System.Array.Exists(gameKeyList, x => x == gameKey))
		{
			string filePath = getLobbyIconsPath(gameKey);
			int maxSize = getMaxSizeForImage(filePath, lobbyOption:false);

			if (File.Exists(filePath))
			{
				changeLobbyImageTextureImporterSettings(filePath, maxSize);
			}
			else
			{
				string jpgFilePath = Path.ChangeExtension(filePath, "jpg");
				if (File.Exists(jpgFilePath))
				{
					changeLobbyImageTextureImporterSettings(jpgFilePath, maxSize);
				}
				else
				{
					validateAndImport(gameKey, lobbyOption:false, doImport:true, forceImport:false);
				}
			}
		}
	}

	public void updateAllLobbyIconImageSettings()
	{
		foreach (string gameKey in gameKeyList)
		{
			updateLobbyIconImageSettings(gameKey);
		}
	}

	public void updateLobbyOptionImageSettings(string gameKey)
	{
		if (System.Array.Exists(gameKeyList, x => x == gameKey))
		{
			string filePath = getLobbyOptionsPath(gameKey);
			int maxSize = getMaxSizeForImage(filePath, lobbyOption:true);

			if (File.Exists(filePath))
			{
				changeLobbyImageTextureImporterSettings(filePath, maxSize);
			}
			else
			{
				string jpgFilePath = Path.ChangeExtension(filePath, "jpg");
				if (File.Exists(jpgFilePath))
				{
					changeLobbyImageTextureImporterSettings(jpgFilePath, maxSize);
				}
				else
				{
					validateAndImport(gameKey, lobbyOption:true, doImport:true, forceImport:false);
				}
			}
		}
	}

	public void updateAllLobbyOptionImageSettings()
	{
		foreach (string gameKey in gameKeyList)
		{
			updateLobbyOptionImageSettings(gameKey);
		}
	}

	// Returns true if did deletion.
	private bool maybeDeleteImageFile(string filePath)
	{
		if (File.Exists(filePath))
		{
			Debug.LogFormat("Deleting {0}", filePath);
			File.Delete(filePath);
			return true;
		}
		return false;
	}

	private void validateAndImport(string gameKey, bool lobbyOption, bool doImport, bool forceImport)
	{
		if (System.Array.Exists(gameKeyList, x => x == gameKey))
		{
			string filePath;
			if (lobbyOption)
			{
				filePath = getLobbyOptionsPath(gameKey);
			}
			else
			{
				filePath = getLobbyIconsPath(gameKey);
			}

			string partialFilePath = getPartialS3Path(filePath);
			bool pngAlreadyExists = File.Exists(filePath);
			string partialJpgFilePath = Path.ChangeExtension(partialFilePath, "jpg");
			string jpgFilePath = Path.ChangeExtension(filePath, "jpg");
			bool jpgAlreadyExists = File.Exists(jpgFilePath);

			if (!calledAsScript)
			{
				// A little extra logging to console to let interactive user know that something is happening.
				Debug.Log(string.Format("Validating {0} for {1}: import {2} force {3}", partialFilePath, gameKey, doImport, forceImport));
			}

			if (!pngAlreadyExists || forceImport)
			{
				if (doImport)
				{
					bool didImportFile = false;
					if (getAndImportFileWithCorrectSettings(filePath, lobbyOption))
					{
						Debug.LogFormat("{0}: Imported bundled lobby image {1}", gameKey, partialFilePath);
						didImportFile = true;
						if (maybeDeleteImageFile(jpgFilePath))
						{
							AssetDatabase.Refresh();
						}
					}
					// Don't bother downloading JPG version from S3, just log error so that it can be fixed in S3.

					if (!didImportFile)
					{
						Debug.LogErrorFormat("{0}: bundled lobby image {1} in bundle is missing and couldn't be imported from S3", gameKey, partialFilePath);
					}
				}
				else
				{
					if (jpgAlreadyExists)
					{
						Debug.LogErrorFormat("{0}: bundled lobby image {1} is a JPG! Should be PNG.", gameKey, partialJpgFilePath);
					}
					else if (!pngAlreadyExists)
					{
						Debug.LogErrorFormat("Bundled lobby image for {0} missing at {1}!", gameKey, partialFilePath);
					}
					else
					{
						if (!calledAsScript)
						{
							Debug.Log(string.Format("{0}: OK", partialFilePath));
						}
					}
				}
			}
			else
			{
				if (!calledAsScript)
				{
					Debug.Log(string.Format("{0}: OK", partialFilePath));
				}
			}
		}
	}

	private int getMaxSizeForImage(string filePath, bool lobbyOption)
	{
		if (lobbyOption)
		{
			bool isOneByTwo = filePath.Contains("1X2");
			if (isOneByTwo)
			{
				return ONE_BY_TWO_MAX_SIZE;
			}
			else
			{
				return LOBBY_OPTION_MAX_SIZE;
			}
		}
		else
		{
			return INBOX_ICON_MAX_SIZE;
		}
	}

	// Return true if it succeeded in downloading and importing file.
	private bool getAndImportFileWithCorrectSettings(string filePath, bool lobbyOption)
	{
		return getAndImportFile(filePath, getMaxSizeForImage(filePath, lobbyOption));
	}

	// Returns all available full URLs to the given resource's relative path.
	private string[] findAllImageUrls(string path)
	{
		if (mobileStreamingAssetsUrl == "BAD_URL")
		{
			// Somehow got here without properly setting up the URL to hit.
			Debug.LogError("Missing/invalid mobileStreamingAssetsUrl");
			if (calledAsScript)
			{
				EditorApplication.Exit(1);
			}
		}

		string canonicalUrl = string.Format(
			CANONICAL_URL,
			mobileStreamingAssetsUrl,
			path,
			mobileStreamingAssetsVersion);

		return Server.findAllStaticUrls(canonicalUrl);
	}

	// Return true if it succeeded in downloading and importing file.
	private bool getAndImportFile(string filePath, int maxSize)
	{
		Texture2D tex = null;
		string partialFileName = getPartialS3Path(filePath);
		string [] urls = findAllImageUrls(partialFileName);
		bool success = false;
		bool fileAlreadyExists = File.Exists(filePath);

		foreach (string url in urls)
		{
			WWW loader = new WWW(url);

			while (!loader.isDone)
			{
				Thread.Sleep(100);
			}

			if (loader.error == null)
			{
				tex = loader.texture;
				if (tex != null)
				{
					if (!string.IsNullOrEmpty(FileCache.path))
					{
						try
						{
							// Ensure directory exists before writing bytes to file.
							Directory.CreateDirectory(Path.GetDirectoryName(filePath));
							File.WriteAllBytes(filePath, tex.EncodeToPNG());
							Debug.LogFormat("Saving file: {0}", filePath);

							// Don't make expensive call to refresh asset database if the file was already there.
							if (!fileAlreadyExists)
							{
								// Refresh so Unity can find the file to import.
								AssetDatabase.Refresh();
							}

							changeLobbyImageTextureImporterSettings(filePath, maxSize);
							success = true;
						}
						catch (System.Exception ex)
						{
							Debug.LogException(ex, null);
						}
					}
					// Done, can stop trying urls.
					break;
				}
			}
			else
			{
				Debug.LogWarningFormat("Error downloading texture at {0}: {1}", url, loader.error);
			}
		}
		return success;
	}

	private void changeLobbyImageTextureImporterSettings(string filePath, int maxSize)
	{
		string dataPath = getAssetDataPathRelativePath(filePath);
		TextureImporter textureImporter = TextureImporter.GetAtPath(dataPath) as TextureImporter;

		if (textureImporter != null )
		{
			changeTextureImporterSettings(textureImporter, maxSize);
		}
		else
		{
			Debug.LogErrorFormat("Failed to get TextureImporter at {0} to update settings.", dataPath);
		}
	}

	private void changeTextureImporterSettings(TextureImporter textureImporter, int maxSize)
	{
		Debug.LogFormat("Texture import settings for {0}: maxSize {1}", textureImporter.assetPath, maxSize);

		bool dirty = false;
		dirty = setTextureImporterSettingsNoWrite(textureImporter, "iPhone", maxSize, TextureImporterFormat.PVRTC_RGB4, 100, false) || dirty;
		dirty = setTextureImporterSettingsNoWrite(textureImporter, "Android", maxSize, TextureImporterFormat.ETC_RGB4, 50, false) || dirty;
		dirty = setTextureImporterSettingsNoWrite(textureImporter, "Windows Store Apps", maxSize, TextureImporterFormat.DXT1, 100, false) || dirty;
		dirty = setTextureImporterSettingsNoWrite(textureImporter, "WebGL", maxSize, TextureImporterFormat.DXT1, 100, false) || dirty;
		dirty = setTextureImporterSettingsNoWrite(textureImporter, "default", maxSize, TextureImporterFormat.Automatic, 50, false) || dirty;

		if (dirty)
		{
			Debug.LogFormat("Writing texture import settings for {0}", textureImporter.assetPath);
			textureImporter.SaveAndReimport();
		}
	}

	// Set texture importer settings ONLY if different, makes a big speed difference not having to write out meta file
	// and reimport for every single game key.
	private bool setTextureImporterSettingsNoWrite(TextureImporter textureImporter, string platform, int maxSize, TextureImporterFormat texFormat, int compression, bool allowAlphaSplit)
	{
		TextureImporterPlatformSettings settings = textureImporter.GetPlatformTextureSettings(platform);
		bool dirty = false;

		if (textureImporter.maxTextureSize != maxSize)
		{
			textureImporter.maxTextureSize = maxSize;
			dirty = true;
		}
		if (settings.overridden == false)
		{
			settings.overridden = true;
			dirty = true;
		}
		if (settings.maxTextureSize != maxSize)
		{
			settings.maxTextureSize = maxSize;
			dirty = true;
		}
		if (settings.format != texFormat)
		{
			settings.format = texFormat;
			dirty = true;
		}
		if (settings.compressionQuality != compression)
		{
			settings.compressionQuality = compression;
			dirty = true;
		}
		if (settings.allowsAlphaSplitting != allowAlphaSplit)
		{
			settings.allowsAlphaSplitting = allowAlphaSplit;
			dirty = true;
		}

		if (dirty)
		{
			textureImporter.SetPlatformTextureSettings(settings);
		}

		return dirty;
	}

	// This adds games then sorts the list alphabetically
	private void discoverGamesToTest()
	{
		SlotResourceMap.populateAll();
		gameKeyList = new string[SlotResourceMap.map.Keys.Count];
		SlotResourceMap.map.Keys.CopyTo(gameKeyList, 0);
	}

	#region scripting commands
	public static void commandRunUpdate()
	{
		runUpdateByScript(forceImport:false);
	}

	public static void commandRunForceUpdate()
	{
		runUpdateByScript(forceImport:true);
	}

	private static void runUpdateByScript(bool forceImport)
	{
		instance.calledAsScript = true;
		if (forceImport)
		{
			instance.forceUpdateAllLobbyOptionImages();
			instance.forceUpdateAllLobbyIconImages();
		}
		else
		{
			instance.updateAllLobbyOptionImageSettings();
			instance.updateAllLobbyIconImageSettings();
		}
	}
	#endregion
}
#endif
