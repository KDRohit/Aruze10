using UnityEngine;
using System.Collections.Generic;
using System.Text;

/**
This class maps game id keys to their various prefab resources.
*/
public class SlotResourceMap : IResetGame
{
	public static Dictionary<string, SlotResourceData> map { get; private set; }

	public static FreeSpinTypeEnum freeSpinType = FreeSpinTypeEnum.DEFAULT;													// 3 free spin prefabs are now possible.
	public enum FreeSpinTypeEnum
	{
		DEFAULT = 0,
		VERSION_2 = 1,
		VERSION_3 = 2,
		SUPER = 3
	}

	private const string FORMATTED_LOBBY_OPTION_1X1_PATH = "lobby_options/{0}/{1}.jpg";
	
	private const string FORMATTED_LOBBY_OPTION_MXN_PATH = "lobby_options/{0}/{1}_{2}.jpg";
	private const string FORMATTED_LOBBY_OPTION_MXN_DFS_PATH = "lobby_options/{0}/{1}_dfs_{2}.jpg";

	const string FORMATTED_COMMON_PAYTABLE_LEGAL_IMAGE_PREFAB_PATH = "Legal Images/Prefabs/{0}";     // Path with all the common legal image prefabs ready to be formated (0: fileName)
	const string COMMON_PAYTABLE_LEGAL_IMAGE_PREFAB_POSTFIX = "_gameobject";

	// Populate all the data. Called once per session.
	public static void populateAll()
	{
		map = new Dictionary<string, SlotResourceData>();

		string resourceMapFilePath = "Data/slot_resource_map";
		Debug.Log("Reading slot resource map file " + resourceMapFilePath);
		TextAsset resourceMapFile = SkuResources.loadSkuSpecificEmbeddedResourceText(resourceMapFilePath) as TextAsset;
		if (resourceMapFile != null && resourceMapFile.text != null)
		{
			JSON resourceMapJSON = new JSON(resourceMapFile.text);

			if (resourceMapJSON == null)
			{
				Debug.LogError("JSON file not found or unreadable.");
			}
			else
			{
				foreach (JSON gameMap in resourceMapJSON.getJsonArray("games"))
				{
					string gameKey = gameMap.getString("game_key", "");

					SlotResourceData slotResourceData = new SlotResourceData(gameKey, gameMap);

					if (slotResourceData.isProductionReady || Data.showNonProductionReadyGames)
					{
#if UNITY_EDITOR
						if (map.ContainsKey(gameKey))
						{
							Debug.LogError("Duplicate game key: " + gameKey);
							Debug.Break();
						}
#endif
						map[gameKey] = slotResourceData;
					}
				}
			}
		}
	}

	public static bool isPopulated
	{
		get
		{
			return map != null;
		}
	}

	// Get the data for a specific get.
	public static SlotResourceData getData(string key)
	{
		// Check for and return the desired entry
		if (isPopulated && map.ContainsKey(key))
		{
			return map[key];
		}
		return null;
	}

	/// Check simply to see if data exists for a key.
	public static bool hasEntry(string key)
	{
		return getData(key) != null;
	}

	/// Check simply to see if data exists for a key.
	public static bool gameHasFreespinsPrefab(string gameKey)
	{
		string freeSpinPath = null;
		SlotResourceData entry = getData(gameKey);
		if (entry != null)
		{
			switch (freeSpinType)
			{
				case FreeSpinTypeEnum.DEFAULT:
					freeSpinPath = entry.freeSpinPrefabPath;
					break;
				case FreeSpinTypeEnum.VERSION_2:
					freeSpinPath = entry.freeSpinPrefabVersion2Path;
					break;
				case FreeSpinTypeEnum.VERSION_3:
					freeSpinPath = entry.freeSpinPrefabVersion3Path;
					break;
				case FreeSpinTypeEnum.SUPER:
					freeSpinPath = entry.freeSpinPrefabSuperPath;
					break;
				default:
					Debug.LogWarning("SlotResourceMap.gameHasFreespinsPrefab() - Unhandled freeSpinType = " + freeSpinType + "; for gameKey = " + gameKey);
					break;
			}
		}
		return !string.IsNullOrEmpty(freeSpinPath);
	}
	
	// Returns the custom lobby frame to use for a built in progressive for this game
	public static string getBuiltInProgressiveLobbyFrameName(string gameKey)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry != null)
		{
			return entry.builtInProgressiveLobbyFrame;
		}

		return JackpotLobbyOptionDecorator.JackpotTypeEnum.Default.ToString();
	}

	// Returns the path to texture in lobby options
	public static string getLobbyImagePath(string folderName, string baseFilename, string size = "", bool isDFSGame = false)
	{
		// If someone puts in 1x2 instead of 1X2 it fails and makes them want to flip tables trying to figure out why the image isn't loading.
		size = size.ToUpper();

#if UNITY_EDITOR
		string testGroup = PlayerPrefsCache.GetString(DebugPrefs.LOBBY_INBOX_ICON_GROUP, "");
		string testGame = PlayerPrefsCache.GetString(DebugPrefs.LOBBY_INBOX_ICON_GAME, "");
		
		if (testGroup != "" && testGame != "")
		{
			folderName = testGroup;
			baseFilename = testGame;
		}
#endif

		string path = "";
		
		if (size =="")
		{
			path = string.Format(FORMATTED_LOBBY_OPTION_1X1_PATH, folderName, baseFilename);
		}
		else
		{
			if (isDFSGame)
			{
				path = string.Format(FORMATTED_LOBBY_OPTION_MXN_DFS_PATH, folderName, baseFilename, size);
			}
			else
			{
				path = string.Format(FORMATTED_LOBBY_OPTION_MXN_PATH, folderName, baseFilename, size);
			}
		}
		
		return path;
	}

	/// Returns the bonus game summary icon for the given game.
	public static void getSummaryIcon(string gameKey, BonusGameType gameType, AssetLoadDelegate successCallback, AssetFailDelegate failCallback)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry == null || entry.bonusSummaryIcons == null)
		{
			if (failCallback != null)
			{
				failCallback(null);
			}
			return;
		}
		string summaryIconPath = "";
		if (entry.bonusSummaryIcons.ContainsKey(gameType))
		{
			summaryIconPath = entry.bonusSummaryIcons[gameType];
		}
		else
		{
			Debug.LogWarning(string.Format("No Summary Icon found for {0} - {1} bonus", gameKey, gameType));
		}
		AssetBundleManager.load(summaryIconPath, successCallback, failCallback, blockingLoadingScreen:true);
	}

	/// Returns the big win prefab for the given game.
	public static void getBigWin(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null);
			}
			return;
		}

		AssetBundleManager.load(entry.bigWinPrefabPath, successCallback, failCallback, blockingLoadingScreen:true);
	}

	/// Returns the super win prefab for the given game.
	public static void getSuperWin(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null);
			}
			return;
		}
		string superWinPrefabPath = entry.superWinPrefabPath;
		if (!string.IsNullOrEmpty(superWinPrefabPath))
		{
			AssetBundleManager.load(superWinPrefabPath, successCallback, failCallback, blockingLoadingScreen:true);
		}
	}

	/// Returns the mega win prefab for the given game.
	public static void getMegaWin(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null);
			}
			return;
		}
		string megaWinPrefabPath = entry.megaWinPrefabPath;
		if (!string.IsNullOrEmpty(megaWinPrefabPath))
		{
			AssetBundleManager.load(megaWinPrefabPath, successCallback, failCallback,blockingLoadingScreen:true);
		}
	}

	/// Returns the wings prefab for the given game.
	public static void getWings(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null, data);
			}
			return;
		}

		string path = entry.wingTexturePath;

		if (FreeSpinGame.instance != null && entry.wingTexturePathFS != "")
		{
			path = entry.wingTexturePathFS;
		}
		else if (ChallengeGame.instance != null)
		{
			if (entry.wingTexturePortalPath != "" && (ChallengeGame.instance is PickPortal))
			{
				path = entry.wingTexturePortalPath;
			}
			else if (entry.wingTexturePathCH != "")
			{
				path = entry.wingTexturePathCH;
			}
		}

		if (FreeSpinGame.instance != null && entry.wingTexturePathFS != "")
		{
			path = entry.wingTexturePathFS;
		}
		else if (ChallengeGame.instance != null && entry.wingTexturePortalPath != "" && (ChallengeGame.instance is PickPortal))
		{
			path = entry.wingTexturePortalPath;
		}
		else if (ChallengeGame.instance != null && entry.wingTexturePathCH != "")
		{
			path = entry.wingTexturePathCH;
		}

		AssetBundleManager.load(path, successCallback, failCallback, data, blockingLoadingScreen:true);
	}

	// Returns the wings that are setup to load for a Big Win, these can be set in case they don't match the wings that the game normally uses.
	public static void getBigWinWings(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data)
	{
		SlotResourceData entry = getData(gameKey);

		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null, data);
			}
		}
		else
		{
			AssetBundleManager.load(entry.bigWinWingTexturePath, successCallback, failCallback, data, blockingLoadingScreen:true);
		}
	}

	// Returns the wings that are setup to load for a bonus game portal.
	public static void getPortalWings(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data)
	{
		SlotResourceData entry = getData(gameKey);

		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null, data);
			}
		}
		else
		{
			AssetBundleManager.load(entry.wingTexturePortalPath, successCallback, failCallback, data, blockingLoadingScreen:true);
		}
	}

	// Returns the wings that are setup to load for a challenge game first screen.
	public static void getChallengeWings(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data)
	{
		SlotResourceData entry = getData(gameKey);

		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null, data);
			}
		}
		else
		{
			AssetBundleManager.load(entry.wingTexturePathCH, successCallback, failCallback, data, blockingLoadingScreen:true);
		}
	}

	// Returns the wings that are setup to load for a challenge game second screen.
	public static void getChallengeWingsVariant(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data)
	{
		SlotResourceData entry = getData(gameKey);

		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null, data);
			}
		}
		else
		{
			AssetBundleManager.load(entry.wingTexturePathCH_2, successCallback, failCallback, data, blockingLoadingScreen:true);
		}
	}

	// Returns the wings that are setup to load for a challenge game third screen.
	public static void getChallengeWingsVariantTwo(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data)
	{
		SlotResourceData entry = getData(gameKey);

		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null, data);
			}
		}
		else
		{
			AssetBundleManager.load(entry.wingTexturePathCH_3, successCallback, failCallback, data, blockingLoadingScreen:true);
		}
	}

	// Returns the wings that are setup to load for a challenge game third screen.
	public static void getChallengeWingsVariantThree(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data)
	{
		SlotResourceData entry = getData(gameKey);

		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null, data);
			}
		}
		else
		{
			AssetBundleManager.load(entry.wingTexturePathCH_4, successCallback, failCallback, data, blockingLoadingScreen:true);
		}
	}

	// Returns the wings that are setup to load for the initial free spin screen.
	public static void getFreeSpinWings(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data)
	{
		SlotResourceData entry = getData(gameKey);

		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null, data);
			}
		}
		else
		{
			AssetBundleManager.load(entry.wingTexturePathFS, successCallback, failCallback, data, blockingLoadingScreen:true);
		}
	}

	// Returns the wings that are setup to load for the initial free spin screen.
	public static void getFreeSpinIntroWings(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data)
	{
		SlotResourceData entry = getData(gameKey);

		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null, data);
			}
		}
		else
		{
			AssetBundleManager.load(entry.wingTexturePathFSIntro, successCallback, failCallback, data, blockingLoadingScreen:true);
		}
	}

	// Returns the wings that are setup to load for a free spin second screen.
	public static void getFreeSpinWingsVariantTwo(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data)
	{
		SlotResourceData entry = getData(gameKey);

		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null, data);
			}
		}
		else
		{
			AssetBundleManager.load(entry.wingTexturePathFS_2, successCallback, failCallback, data, blockingLoadingScreen:true);
		}
	}

	// Returns the wings that are setup to load for a free spin third screen.
	public static void getFreeSpinWingsVariantThree(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data)
	{
		SlotResourceData entry = getData(gameKey);

		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null, data);
			}
		}
		else
		{
			AssetBundleManager.load(entry.wingTexturePathFS_3, successCallback, failCallback, data, blockingLoadingScreen:true);
		}
	}

	// Tells if the game is using an override for the wings displayed during a big win.
	public static bool isGameUsingBigWinWings(string gameKey)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry == null)
		{
			return false;
		}
		else
		{
			return entry.bigWinWingTexturePath != null && entry.bigWinWingTexturePath != "";
		}
	}

	// Returns the normal wings without checking for any of the freeSpinGame stuff.
	public static void getNormalWings(string gameKey, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry == null)
		{
			if (failCallback != null)
			{
				failCallback(null, data);
			}
			return;
		}

		string path = entry.wingTexturePath;

		AssetBundleManager.load(path, successCallback, failCallback, data, blockingLoadingScreen:true);
	}

	/// Return anticipation fx prefab path for given game.
	public static string getAnticipationFXPath(string gameKey, bool isFreeSpins)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry != null)
		{
			if (isFreeSpins)
			{
				return entry.freespinAnticipationFXPath;
			}
			return entry.anticipationFXPath;
		}
		return "";
	}

	/// Return anticipation fx prefab path for given game.
	public static string getAnticipationLinkedFXPath(string gameKey, bool isFreeSpins)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry != null)
		{
			if (isFreeSpins)
			{
				return entry.freespinAnticipationLinkedFXPath;
			}
			return entry.anticipationLinkedFXPath;
		}
		return "";
	}

	/// Return optional data defined for user named features that have their own anticipations
	public static Dictionary<string, string> getFeatureAnticipationPaths(string gameKey, bool isFreeSpins)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry != null)
		{
			if (isFreeSpins && entry.freespinFeatureAnticipationPaths != null)
			{
				return entry.freespinFeatureAnticipationPaths;
			}
			else
			{
				return entry.featureAnticipationPaths;
			}
		}
		else
		{
			return null;
		}
	}

	/// Return optional column defined anticipation fx prefab paths for given game.
	public static string[] getOptionalAnticipationFXColumnPaths(string gameKey, bool isFreeSpins)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry != null)
		{
			if (isFreeSpins)
			{
				return entry.freespinOptionalAnticipationColumnDefinitionPaths;
			}
			return entry.optionalAnticipationColumnDefinitionPaths;
		}
		return null;
	}

	/// Determine if this game is using optimized symbols
	public static bool isGameUsingOptimizedFlattenedSymbols(string gameKey)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry != null)
		{
			return entry.isUsingOptimizedFlattenedSymbols;
		}
		else
		{
			// if we can't find an entry, default to not use optimized sybmols
			return false;
		}
	}

	/// Returns true if given game requires asset bundles that need to be downloaded and are not currently cached.
	public static bool needsDownload(string key)
	{
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			foreach (string bundle in entry.commonBundles)
			{
				if (!AssetBundleManager.isBundleCached(bundle))
				{
					return true;
				}
			}

			return ((!string.IsNullOrEmpty(entry.slotPrefabPath) && !AssetBundleManager.isAvailable(entry.slotPrefabPath)) ||
					(!string.IsNullOrEmpty(entry.bonusPrefabPath) && !AssetBundleManager.isAvailable(entry.bonusPrefabPath)) ||
					(!string.IsNullOrEmpty(entry.creditBonusPrefabPath) && !AssetBundleManager.isAvailable(entry.creditBonusPrefabPath)) ||
					(!string.IsNullOrEmpty(entry.freeSpinPrefabPath) && !AssetBundleManager.isAvailable(entry.freeSpinPrefabPath)) ||
					(!string.IsNullOrEmpty(entry.portalPrefabPath) && !AssetBundleManager.isAvailable(entry.portalPrefabPath)) ||
					(!string.IsNullOrEmpty(entry.scatterBonusPrefabPath) && !AssetBundleManager.isAvailable(entry.scatterBonusPrefabPath))
					);
		}
		return false;
	}

	/// Function used to create the slot base game instance, used for TestGameData.cs to check things about the ReelGame
	public static GameObject getSlotPrefabForTesting(string key)
	{
		GameObject slotPrefab = null;

#if UNITY_EDITOR
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			AssetBundleManager.load(
				entry.slotPrefabPath,

				// Success callback.
				(string asset, Object obj, Dict data) => {
					GameObject resource = obj as GameObject;
					if (resource != null)
					{
						slotPrefab = CommonGameObject.instantiate(resource) as GameObject;
					}
				},

				// Failure callback.
				(string asset, Dict data) => {
					Debug.LogWarning("Failed to load Basegame for " + key);
				},
				blockingLoadingScreen:true
				);
		}
		else
		{
			Debug.LogWarning("No SlotResourceMap entry for game: " + key);
		}
#else
		Debug.LogError("This function should ONLY be used for writing Tests.");
#endif
		return slotPrefab;
	}

	public static string getSlotPrefabPathForTesting(string key)
	{
		string path = "";

#if UNITY_EDITOR
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			path = AssetBundleManager.getProjectRelativePathFromResourcePath(entry.slotPrefabPath);
		}
#else
		Debug.LogError("This function should ONLY be used for writing Tests.");
#endif
		return path;
	}

	/// Function used to create the slot base game instance, used for TestGameData.cs to check things about the ReelGame
	public static List<GameObject> getFreespinPrefabsForTesting(string key)
	{
		List<GameObject> freespinPrefabs = new List<GameObject>();
		GameObject freespinPrefab = null;

#if UNITY_EDITOR
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			if (!string.IsNullOrEmpty(entry.freeSpinPrefabVersion2Path))
			{
				AssetBundleManager.load(
				entry.freeSpinPrefabVersion2Path,

				// Success callback.
				(string asset, Object obj, Dict data) => {
					GameObject resource = obj as GameObject;
					if (resource != null)
					{
						freespinPrefabs.Add(CommonGameObject.instantiate(resource) as GameObject);
					}
				},

				// Failure callback.
				(string asset, Dict data) => {
					Debug.LogWarning("Failed to load Basegame for " + key);
				},
				blockingLoadingScreen:true);
			}
			if (!string.IsNullOrEmpty(entry.freeSpinPrefabVersion3Path))
			{
				AssetBundleManager.load(
				entry.freeSpinPrefabVersion3Path,

				// Success callback.
				(string asset, Object obj, Dict data) => {
					GameObject resource = obj as GameObject;
					if (resource != null)
					{
						freespinPrefabs.Add(CommonGameObject.instantiate(resource) as GameObject);
					}
				},

				// Failure callback.
				(string asset, Dict data) => {
					Debug.LogWarning("Failed to load Basegame for " + key);
				},
				blockingLoadingScreen:true);
			}
			if (!string.IsNullOrEmpty(entry.freeSpinPrefabPath))
			{
				AssetBundleManager.load(
				entry.freeSpinPrefabPath,

				// Success callback.
				(string asset, Object obj, Dict data) => {
					GameObject resource = obj as GameObject;
					if (resource != null)
					{
						freespinPrefabs.Add(CommonGameObject.instantiate(resource) as GameObject);
					}
				},

				// Failure callback.
				(string asset, Dict data) => {
					Debug.LogWarning("Failed to load Basegame for " + key);
				},
				blockingLoadingScreen:true);
			}
		}
		else
		{
			Debug.LogWarning("No SlotResourceMap entry for game: " + key);
		}
#else
		Debug.LogError("This function should ONLY be used for writing Tests.");
#endif
		return freespinPrefabs;
	}

	public static List<string> getFreespinPrefabsPathForTesting(string key)
	{
		List<string> paths = new List<string>();

#if UNITY_EDITOR
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			if (!string.IsNullOrEmpty(entry.freeSpinPrefabVersion2Path))
			{
				paths.Add(AssetBundleManager.getProjectRelativePathFromResourcePath(entry.freeSpinPrefabVersion2Path));
			}
			if (!string.IsNullOrEmpty(entry.freeSpinPrefabVersion3Path))
			{
				paths.Add(AssetBundleManager.getProjectRelativePathFromResourcePath(entry.freeSpinPrefabVersion3Path));
			}
			if (!string.IsNullOrEmpty(entry.freeSpinPrefabPath))
			{
				paths.Add(AssetBundleManager.getProjectRelativePathFromResourcePath(entry.freeSpinPrefabPath));
			}
		}
#else
		Debug.LogError("This function should ONLY be used for writing Tests.");
#endif
		return paths;
	}

	// Tells if this game will use a legal image in the paytable
	// determined based on LobbyGame and SlotLicense data
	private static bool doesGameHavePaytableLegalImage(string gameKey)
	{
		LobbyGame game = LobbyGame.find(gameKey);
		if (game.license != "" || game.groupInfo.license != "")
		{
			string licenseId = (!string.IsNullOrEmpty(game.license)) ? game.license : game.groupInfo.license;
			SlotLicense licenseInfo = SlotLicense.find(licenseId);
			string copyrightImage = (!string.IsNullOrEmpty(game.paytableImageOverride)) ? game.paytableImageOverride : licenseInfo.legal_image;

			if (!string.IsNullOrEmpty(copyrightImage))
			{
				return true;
			}
		}

		return false;
	}

	// Creates an instance of the associated slot game prefab
	public static void createSlotInstance(string key, AssetLoadDelegate callerSuccess, AssetFailDelegate callerFail)
	{
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			// Ensure the common bundles are downloaded, if any.
			if (!AssetBundleManager.useLocalBundles)
			{
				foreach (string bundle in entry.commonBundles)
				{
					AssetBundleManager.downloadAndCacheBundle(bundle, blockingLoadingScreen:true);
				}

				// Ensure that we also load the legal image paytable bundle if required
				if (doesGameHavePaytableLegalImage(key))
				{
					AssetBundleManager.downloadAndCacheBundle("legal_images", blockingLoadingScreen:true);
				}
			}

			// Preload the bonus games too.
			if (!string.IsNullOrEmpty(entry.bonusPrefabPath) && !AssetBundleManager.isAvailable(entry.bonusPrefabPath))
			{
				AssetBundleManager.downloadAndCacheBundle(AssetBundleManager.getBundleNameForResource(entry.bonusPrefabPath), blockingLoadingScreen:true);
			}
			if (!string.IsNullOrEmpty(entry.creditBonusPrefabPath) && !AssetBundleManager.isAvailable(entry.creditBonusPrefabPath))
			{
				AssetBundleManager.downloadAndCacheBundle(AssetBundleManager.getBundleNameForResource(entry.creditBonusPrefabPath), blockingLoadingScreen:true);
			}
			if (!string.IsNullOrEmpty(entry.freeSpinPrefabPath) && !AssetBundleManager.isAvailable(entry.freeSpinPrefabPath))
			{
				AssetBundleManager.downloadAndCacheBundle(AssetBundleManager.getBundleNameForResource(entry.freeSpinPrefabPath), blockingLoadingScreen:true);
			}
			if (!string.IsNullOrEmpty(entry.portalPrefabPath) && !AssetBundleManager.isAvailable(entry.portalPrefabPath))
			{
				AssetBundleManager.downloadAndCacheBundle(AssetBundleManager.getBundleNameForResource(entry.portalPrefabPath), blockingLoadingScreen:true);
			}
			// Preload the big win too.
			if (!string.IsNullOrEmpty(entry.bigWinPrefabPath) && !AssetBundleManager.isAvailable(entry.bigWinPrefabPath))
			{
				AssetBundleManager.downloadAndCacheBundle(AssetBundleManager.getBundleNameForResource(entry.bigWinPrefabPath), blockingLoadingScreen:true);
			}
			if (!string.IsNullOrEmpty(entry.superWinPrefabPath) && !AssetBundleManager.isAvailable(entry.superWinPrefabPath))
			{
				AssetBundleManager.downloadAndCacheBundle(AssetBundleManager.getBundleNameForResource(entry.superWinPrefabPath), blockingLoadingScreen:true);
			}
			if (!string.IsNullOrEmpty(entry.megaWinPrefabPath) && !AssetBundleManager.isAvailable(entry.megaWinPrefabPath))
			{
				AssetBundleManager.downloadAndCacheBundle(AssetBundleManager.getBundleNameForResource(entry.megaWinPrefabPath), blockingLoadingScreen:true);
			}

			// need this here because it seems like we seem to get crashes in 	AssetBundleManager.load(this, GiftedSpinsVipMultiplier.BADGE_PREFAB_PATH, badgeLoadCallbackSuccess, badgeLoadCallbackFailure);
			// badgeLoadCallbackSuccess, where it fails to instantiate badge prefab
			AssetBundleManager.downloadAndCacheBundle("gifted_spins_vip_multiplier", keepLoaded:true, blockingLoadingScreen:true);
			
			// Load the main game prefab last, so ^ everything else (especially audio) has a chance to load first
			createInstance(
				entry.slotPrefabPath,
				callerSuccess,
				callerFail
#if UNITY_WSA_10_0 && NETFX_CORE
//Blank platform special case
#else
				,System.Reflection.MethodBase.GetCurrentMethod().Name
#endif
			);
		}
		else
		{
			Debug.LogWarning("Failed to instantiate resource: SlotResourceMap.createSlotInstance(" + key + ")");
			if (callerFail != null)
			{
				callerFail(key);
			}
		}
	}

	// Creates an instance of the associated bonus prefab.
	public static void createBonusInstance(string key, AssetLoadDelegate callerSuccess, AssetFailDelegate callerFail)
	{
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			createInstance(
				entry.bonusPrefabPath,
				callerSuccess,
				callerFail
#if UNITY_WSA_10_0 && NETFX_CORE
				//Blank platform special case
#else
				, System.Reflection.MethodBase.GetCurrentMethod().Name
#endif
			);
		}
		else
		{
			Debug.LogWarning("Failed to instantiate resource: SlotResourceMap.createBonusInstance(" + key + ")");
			if (callerFail != null)
			{
				callerFail(key);
			}
		}
	}

	// Creates an instance of the associated credit bonus prefab.
	public static void createCreditBonusInstance(string key, AssetLoadDelegate callerSuccess, AssetFailDelegate callerFail)
	{
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			createInstance(
				entry.creditBonusPrefabPath,
				callerSuccess,
				callerFail
#if UNITY_WSA_10_0 && NETFX_CORE
				//Blank platform special case
#else
				, System.Reflection.MethodBase.GetCurrentMethod().Name
#endif
			);
		}
		else
		{
			Debug.LogWarning("Failed to instantiate resource: SlotResourceMap.creditBonusPrefabPath(" + key + ")");
			if (callerFail != null)
			{
				callerFail(key);
			}
		}
	}

	// Creates an instance of the associated scatter game prefab.
	public static void createScatterBonusInstance(string key, AssetLoadDelegate callerSuccess, AssetFailDelegate callerFail)
	{
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			createInstance(
				entry.scatterBonusPrefabPath,
				callerSuccess,
				callerFail
#if UNITY_WSA_10_0 && NETFX_CORE
				//Blank platform special case
#else
				, System.Reflection.MethodBase.GetCurrentMethod().Name
#endif
			);
		}
		else
		{
			Debug.LogWarning("Failed to instantiate resource: SlotResourceMap.creditBonusPrefabPath(" + key + ")");
			if (callerFail != null)
			{
				callerFail(key);
			}
		}
	}
	
	// Creates an instance of the associated super bonus prefab.
	public static void createSuperBonusInstance(string key, AssetLoadDelegate callerSuccess, AssetFailDelegate callerFail)
	{
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			createInstance(
				entry.superBonusPrefabPath,
				callerSuccess,
				callerFail
#if UNITY_WSA_10_0 && NETFX_CORE
				//Blank platform special case
#else
				, System.Reflection.MethodBase.GetCurrentMethod().Name
#endif
			);
		}
		else
		{
			Debug.LogWarning("Failed to instantiate resource: SlotResourceMap.createBonusInstance(" + key + ")");
			if (callerFail != null)
			{
				callerFail(key);
			}
		}
	}

	public static void createPaytableImage(string gameKey, string imageBaseName, AssetLoadDelegate callerSuccess, AssetFailDelegate callerFail)
	{
		SlotResourceData entry = getData(gameKey);
		if (entry != null)
		{
			string basicImagePath = entry.getGameSpecificImagePath(imageBaseName);
			string commonImagePath = entry.getGroupSpecificImagePath(imageBaseName);
			Dict dict = Dict.create(
				D.IMAGE_PATH, commonImagePath,
				D.CALLBACK, callerSuccess,
				D.OPTION, callerFail,
				D.OPTION1, basicImagePath);
			AssetBundleManager.load(basicImagePath, callerSuccess, createPaytableImageRetryCommon, dict, blockingLoadingScreen:true);
		}
		else
		{
			if (callerFail != null)
			{
				callerFail(gameKey);
			}
		}
	}

	private static void createPaytableImageRetryCommon(string filename, Dict data)
	{
		string commonImagePath = data[D.IMAGE_PATH] as string;
		AssetLoadDelegate callerSuccess = data[D.CALLBACK] as AssetLoadDelegate;
		AssetFailDelegate callerFail = data[D.OPTION] as AssetFailDelegate;
		AssetBundleManager.load(commonImagePath, callerSuccess, callerFail, data, blockingLoadingScreen:true);
	}

	// Load a paytable legal image
	public static void createPaytableLegalImagePrefab(string imageBaseName, AssetLoadDelegate callerSuccess, AssetFailDelegate callerFail)
	{
		string pathToImagePrefab = string.Format(FORMATTED_COMMON_PAYTABLE_LEGAL_IMAGE_PREFAB_PATH + COMMON_PAYTABLE_LEGAL_IMAGE_PREFAB_POSTFIX, imageBaseName);
		createInstance(pathToImagePrefab, 
			callerSuccess, 
			callerFail
#if UNITY_WSA_10_0 && NETFX_CORE
			//Blank platform special case
#else
			,System.Reflection.MethodBase.GetCurrentMethod().Name
#endif
			);
	}

	// Creates an instance of a slot or bonus game prefab.
#if UNITY_WSA_10_0 && NETFX_CORE
	//https://msdn.microsoft.com/en-us/library/hh534540.aspx
	public static void createInstance(string prefabPath, AssetLoadDelegate callerSuccess, AssetFailDelegate callerFail, [System.Runtime.CompilerServices.CallerMemberName] string callingFunctionName = "")
#else
	private static void createInstance(string prefabPath, AssetLoadDelegate callerSuccess, AssetFailDelegate callerFail, string callingFunctionName)
#endif
	{
		AssetBundleManager.load(
			prefabPath,

			// Success callback.
			(string asset, Object obj, Dict data) => {
				if (SlotsPlayer.isLoggedIn)	// Only do something if the game didn't reset in the middle of downloading.
				{
					GameObject resource = obj as GameObject;
					if (resource != null && callerSuccess != null)
					{
						GameObject newInstance = CommonGameObject.instantiate(resource) as GameObject;
						callerSuccess(asset, newInstance);
					}
				}
			},

			// Failure callback.
			(string asset, Dict data) => {
				if (SlotsPlayer.isLoggedIn)	// Only do something if the game didn't reset in the middle of downloading.
				{
					// If a game reset happened before the download finished, don't do anything.
					Debug.LogWarning(string.Format("Failed to instantiate resource: SlotResourceMap.{0}({1})", callingFunctionName, asset));
					if (callerFail != null)
					{
						callerFail(asset);
					}
				}
			},
			blockingLoadingScreen:true
			);
	}

	/// Creates and returns an instance of the associated slot game prefab.
	public static void createFreeSpinInstance(string key, AssetLoadDelegate callerSuccess, AssetFailDelegate callerFail)
	{
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			string freeSpinPath = "";
			switch (freeSpinType)
			{
				case FreeSpinTypeEnum.DEFAULT:
					freeSpinPath = entry.freeSpinPrefabPath;
					break;
				case FreeSpinTypeEnum.VERSION_2:
					freeSpinPath = entry.freeSpinPrefabVersion2Path;
					break;
				case FreeSpinTypeEnum.VERSION_3:
					freeSpinPath = entry.freeSpinPrefabVersion3Path;
					break;
				case FreeSpinTypeEnum.SUPER:
					freeSpinPath = entry.freeSpinPrefabSuperPath;
					break;
				
				default:
					Debug.LogWarning("SlotResourceMap.createFreeSpinInstance() - Unhandled freeSpinType = " + freeSpinType + "; for key = " + key);
					break;
			}
			freeSpinType = FreeSpinTypeEnum.DEFAULT;

			AssetBundleManager.load(freeSpinPath,
									(string asset, Object obj, Dict data) => {
										GameObject resource = obj as GameObject;
										if (resource != null && callerSuccess != null)
										{
											GameObject newInstance = CommonGameObject.instantiate(resource) as GameObject;
											callerSuccess(asset, newInstance);
										}
									},
									(string asset, Dict data) => {
										Debug.LogWarning("Failed to instantiate resource: SlotResourceMap.createFreeSpinInstance(" + asset + ")");
										if (callerFail != null)
										{
											callerFail(asset);
										}
									},
				blockingLoadingScreen:true);
		}
		else
		{
			Debug.LogWarning("Failed to instantiate resource: SlotResourceMap.createFreeSpinInstance(" + key + ")");
			if (callerFail != null)
			{
				callerFail(key);
			}
		}
	}

	// Tells if a game has a portalPrefabPath defined, used to detect what games have portals that they will trigger
	public static bool hasPortalPrefabPath(string key)
	{
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			return !string.IsNullOrEmpty(entry.portalPrefabPath);
		}

		return false;
	}

	// Tells if a game has a scatterBonusPrefabPath defined, used to detect if a game can even trigger a scatter base on what is configured on the client
	public static bool hasScatterBonusPrefabPath(string key)
	{
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			return !string.IsNullOrEmpty(entry.scatterBonusPrefabPath);
		}

		return false;
	}

	// Check if this game has a prefab defined to be used as the standard challenge bonus game
	public static bool hasChallengeBonusPrefabPath(string key)
	{
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			return !string.IsNullOrEmpty(entry.bonusPrefabPath);
		}

		return false;
	}

	// Check if this game has a prefab defined to be used as a credit bonus game
	public static bool hasCreditBonusPrefabPath(string key)
	{
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			return !string.IsNullOrEmpty(entry.bonusPrefabPath);
		}

		return false;
	}

	// In Rome, we can't get away from using a portal. So, we have an entry for it.
	public static void createPortalInstance(string key, AssetLoadDelegate callerSuccess, AssetFailDelegate callerFail)
	{
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			AssetBundleManager.load(entry.portalPrefabPath,
									(string asset, Object obj, Dict data) => {
										GameObject resource = obj as GameObject;
										if (resource != null && callerSuccess != null)
										{
											GameObject newInstance = CommonGameObject.instantiate(resource) as GameObject;
											callerSuccess(asset, newInstance);
										}
									},
									(string asset, Dict data) => {
										Debug.LogWarning("Failed to instantiate resource: SlotResourceMap.createPortalInstance(" + asset + ")");
										if (callerFail != null)
										{
											callerFail(asset);
										}
									},
				blockingLoadingScreen:true);
		}
		else
		{
			Debug.LogWarning("Failed to instantiate resource: SlotResourceMap.createPortalInstance(" + key + ")");
			if (callerFail != null)
			{
				callerFail(key);
			}
		}
	}
	
	// In games using CREDIT_BONUS_PREFAB_KEY, this will hint at what outcome is supposed to be treated as a credit vs challenge as far as SlotOutcome is concerned
	public static string getCreditBonusOutcomeKey(string key)
	{
		SlotResourceData entry = getData(key);
		if (entry != null)
		{
			return entry.creditBonusOutcomeKey;
		}

		return "";
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		map = null;
		freeSpinType = FreeSpinTypeEnum.DEFAULT;
	}
}
