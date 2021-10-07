using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Stores data for the SlotResourceMap
*/

public class SlotResourceData
{
	const string FORMATTED_COMMON_ANTICIPATION_PATH        = "Initialization/Games/Common/Anticipation Animatons/{0}";     // Path with all the common anticipations ready to be formated (0: fileName)
	const string FORMATTED_GROUP_PREFAB_PATH               = "Games/{0}/{0}_common/Prefabs/{1}";            // Path with all the group level images ready to be formated (0: groupname 1: fileName)
	const string FORMATTED_GROUP_IMAGE_PATH                = "Games/{0}/{0}_common/Images/{1}";             // Path with all the group level images ready to be formated (0: groupname 1: fileName)
	const string FORMATTED_GROUP_WING_PATH                 = "Games/{0}/{0}_common/Images/{0}_wings";       // Formated path for all the common wings (0: groupname)
	const string FORMATTED_GAME_PREFAB_PATH                = "Games/{0}/{1}/Prefabs/{2}";                   // Path with all of the game objects in it ready to be formated (0: groupname 1: gameName 2: fileName)
	const string FORMATTED_GAME_IMAGE_PATH                 = "Games/{0}/{1}/Images/{2}";                    // Path with all of images for a game ready to be formated (0: groupname 1: gameName 2: fileName)
	const string FORMATTED_GAME_WING_PATH                  = "Games/{0}/{1}/Images/{1}_wings";              // Formated path for all the game wings (0: groupname 1: gameName)
	const string FORMATTED_FREE_SPINS_GAME_INTRO_WING_PATH = "Games/{0}/{1}/Images/{1}_wings_fs_intro";
	const string FORMATTED_FREE_SPINS_GAME_WING_PATH       = "Games/{0}/{1}/Images/{1}_wings_fs";           // Formated path for free-spin specific game wings (0: groupname 1: gameName)
	const string FORMATTED_FREE_SPINS_TWO_GAME_WING_PATH   = "Games/{0}/{1}/Images/{1}_wings_fs_2";
	const string FORMATTED_FREE_SPINS_THREE_GAME_WING_PATH = "Games/{0}/{1}/Images/{1}_wings_fs_3";
	const string FORMATTED_CHALLENGE_GAME_WING_PATH        = "Games/{0}/{1}/Images/{1}_wings_ch";
	const string FORMATTED_CHALLENGE_2_GAME_WING_PATH      = "Games/{0}/{1}/Images/{1}_wings_ch_2";
	const string FORMATTED_CHALLENGE_3_GAME_WING_PATH      = "Games/{0}/{1}/Images/{1}_wings_ch_3";
	const string FORMATTED_CHALLENGE_4_GAME_WING_PATH      = "Games/{0}/{1}/Images/{1}_wings_ch_4";
	const string FORMATTED_PORTAL_WING_PATH                = "Games/{0}/{1}/Images/{1}_wings_portal";
	const string FORMATTED_BIG_WIN_WING_PATH               = "Games/{0}/{1}/Images/{1}_wings_bigwin";
	const string FORMATTED_GAMES_COMMON_BUNDLE_PREFAB_PATH = "Games/common/Prefabs/{0}/{1}";					// Prefab path for bundled common assets like big wins (0: SKU, 1: fileName)


	// Slot Resource Map Processing - JSON keys
	private const string BASEGAME_PREFAB_KEY = "basegame_prefab";
	private const string FREESPIN_PREFAB_KEY = "freespin_prefab";
	private const string FREESPIN_PREFAB_KEY_2 = "freespin_prefab_2";
	private const string FREESPIN_PREFAB_KEY_3 = "freespin_prefab_3";
	private const string FREESPIN_SUPER_PREFAB = "super_freespin_prefab";
	private const string GROUP_FREESPIN_PREFAB_KEY = "group_freespin_prefab";
	private const string CHALLENGE_PREFAB_KEY = "challenge_prefab";
	private const string SCATTER_PREFAB_KEY = "scatter_prefab";
	private const string GROUP_CHALLENGE_PREFAB_KEY = "group_challenge_prefab";
	private const string CREDIT_BONUS_PREFAB_KEY = "credit_bonus_prefab";
	private const string GROUP_CREDIT_BONUS_PREFAB_KEY = "group_credit_bonus_prefab";
	private const string SUPER_BONUS_PREFAB_KEY = "super_bonus_prefab";
	private const string PORTAL_PREFAB_KEY = "portal_prefab";
	private const string BIGWIN_PREFAB_KEY = "bigwin_prefab";
	private const string SUPERWIN_PREFAB_KEY = "superwin_prefab";
	private const string MEGAWIN_PREFAB_KEY = "megawin_prefab";
	private const string GROUP_BIGWIN_PREFAB_KEY = "group_bigwin_prefab";
	private const string GROUP_SUPERWIN_PREFAB_KEY = "group_superwin_prefab";
	private const string GROUP_MEGAWIN_PREFAB_KEY = "group_megawin_prefab";
	private const string COMMON_BIGWIN_PREFAB_KEY = "common_bigwin_prefab";
	private const string COMMON_SUPERWIN_PREFAB_KEY = "common_superwin_prefab";
	private const string COMMON_MEGAWIN_PREFAB_KEY = "common_megawin_prefab";
	private const string BASEGAME_ANTICIPATION_PREFAB_KEY = "basegame_anticipation_prefab";
	private const string BASEGAME_LINKED_ANTICIPATION_PREFAB_KEY = "basegame_linked_anticipation_prefab";
	private const string GROUP_BASEGAME_REEL_ANTICIPATION_PREFAB_KEY = "group_basegame_reel_anticipation_prefab";
	private const string COMMON_BASEGAME_REEL_ANTICIPATION_PREFAB_KEY = "common_basegame_reel_anticipation_prefab";
	private const string OPTIONAL_ANTICIPATION_COLUMN_DEFINITION_PATHS = "optional_anticipation_column_definition_paths";
	private const string FREESPIN_ANTICIPATION_PREFAB_KEY = "freespin_anticipation_prefab";
	private const string FREESPIN_LINKED_ANTICIPATION_PREFAB_KEY = "freespin_anticipation_prefab";
	private const string FEATURE_ANTICIPATION_PATHS_KEY = "feature_anticipation_prefabs";
	private const string FREESPIN_FEATURE_ANTICIPATION_PATHS_KEY = "freespins_feature_anticipation_prefabs";
	private const string GROUP_FREESPINS_REEL_ANTICIPATION_PREFAB_KEY = "group_freespins_reel_anticipation_prefab";
	private const string COMMON_FREESPINS_REEL_ANTICIPATION_PREFAB_KEY = "common_freespins_reel_anticipation_prefab";
	private const string OPTIONAL_FREESPIN_ANTICIPATION_COLUMN_DEFINITION_PATHS = "optional_freespin_anticipation_column_definition_paths";
	private const string FREESPIN_SUMMARY_ICON = "freespin_summary_icon";
	private const string GROUP_FREESPIN_SUMMARY_ICON = "group_freespin_summary_icon";
	private const string CHALLENGE_SUMMARY_ICON = "challenge_summary_icon";
	private const string GROUP_CHALLENGE_SUMMARY_ICON = "group_challenge_summary_icon";
	private const string CREDIT_SUMMARY_ICON = "credit_summary_icon";
	private const string GROUP_CREDIT_SUMMARY_ICON = "group_credit_summary_icon";
	private const string PORTAL_SUMMARY_ICON = "portal_summary_icon";
	private const string GROUP_PORTAL_SUMMARY_ICON = "group_portal_summary_icon";
	private const string SUPER_BONUS_SUMMARY_ICON = "super_bonus_summary_icon";
	private const string GROUP_SUPER_BONUS_SUMMARY_ICON = "group_super_bonus_summary_icon";
	private const string COMMON_BUNDLES = "common_bundles";
	private const string IS_USING_OPTIMIZED_FLATTENED_SYMBOLS = "is_using_optimized_flattened_symbols"; // default if not set is false
	private const string GAME_KEY_PATH_OVERRIDE = "game_key_path_override"; // used in case games share an entire bundle, for instance gen42/gen38 
	private const string BUILT_IN_PROGRESSIVE_LOBBY_FRAME = "built_in_progressive_lobby_frame"; // Used to define a progressive frame that will be shown in the lobby for this game with a built in progressive (see JackpotLobbyOptionDecorator and JackpotLobbyOptionDecorator1x2 for what can be loaded)
	private const string CREDIT_BONUS_OUTCOME_KEY = "credit_bonus_outcome_key"; // In games using CREDIT_BONUS_PREFAB_KEY, this will hint at what outcome is supposed to be treated as a credit vs challenge as far as SlotOutcome is concerned
	
	// Special values which can be assigned to the JSON values, denoting special cases/handling
	private const string GAMES_COMMON_DEFAULT = "default";
	private const string GAMES_COMMON_UNIVERSAL_BIG_WIN_FILENAME_FORMAT = "{0} Universal Big Win"; // File name of the universal big win prefab

	// An array of game keys in the sort order they should be in as a tie-breaker to XP level sorting.
	// This will be obsolete when LoLa is used.
	public static string[] gameSort = null;

	public string anticipationFXPath = "";
	public string anticipationLinkedFXPath = ""; 				// Anticipation paths for linked reels.
	public string freespinAnticipationLinkedFXPath = ""; 		// Anticipation paths for linked reels.
	public string bigWinPrefabPath = "";
	public string superWinPrefabPath = "";
	public string megaWinPrefabPath = "";
	public string bonusPrefabPath = "";
	public string superBonusPrefabPath = "";
	public Dictionary<BonusGameType, string> bonusSummaryIcons;
	public string[] commonBundles = new string[0];
	public string creditBonusPrefabPath = "";
	public string scatterBonusPrefabPath = "";
	public string freespinAnticipationFXPath = "";
	public string[] freespinOptionalAnticipationColumnDefinitionPaths = new string[0];
	public string freeSpinPrefabPath = "";
	public string freeSpinPrefabVersion2Path = "";
	public string freeSpinPrefabVersion3Path = "";
	public string freeSpinPrefabSuperPath = "";
	public string[] optionalAnticipationColumnDefinitionPaths = new string[0];
	public Dictionary<string, string> featureAnticipationPaths; // maps anticipations for specfic features to names, names can be whatever, will need to override ReelGame:getFeatureNameForAnticipation()
	public Dictionary<string, string> freespinFeatureAnticipationPaths; // for free spins, maps anticipations for specfic features to resource names
	public string portalPrefabPath = "";
	public string slotPrefabPath = "";
	public string wingTexturePath = "";
	public string wingTexturePathFSIntro = ""; // For free spins games. If empty, uses wingTexturePath.
	public string wingTexturePathFS = "";       // For free spins games. If empty, uses wingTexturePath.	
	public string wingTexturePathFS_2 = "";		// Free spins can have up to 3 wings, on bonus game choices.
	public string wingTexturePathFS_3 = "";		// Free spins can have up to 3 wings, on bonus game choices.
	public string wingTexturePathCH = "";		// For challenge games.
	public string wingTexturePathCH_2 = "";		// Art wants to use 2 wings in one game, go figure -_-
	public string wingTexturePathCH_3 = "";		// Art at some point wanted to use 3 wings...
	public string wingTexturePathCH_4 = "";		// And why not go to 4 wings?
	public string wingTexturePortalPath = "";
	public string bigWinWingTexturePath = "";	// For big wins can override using the regular wings.  If empty, uses wingTexturePath.
	// The follow properties are used for lobby options.
	public string lobbyImageFilename = "";
	public string builtInProgressiveLobbyFrame = "";  // Used to define a progressive frame that will be shown in the lobby for this game with a built in progressive (see JackpotLobbyOptionDecorator and JackpotLobbyOptionDecorator1x2 for what can be loaded)
	public string creditBonusOutcomeKey = ""; // In games using CREDIT_BONUS_PREFAB_KEY, this will hint at what outcome is supposed to be treated as a credit vs challenge as far as SlotOutcome is concerned
	
	public enum GameStatus { NON_PRODUCTION_READY, PRODUCTION_READY, PRODUCTION_READY_REFACTORED, PRODUCTION_READY_POSSIBLY_REFACTORED, LICENSE_LAPSED, PORT, PORT_NEEDS_ART};
	public GameStatus gameStatus;
	public bool isUsingOptimizedFlattenedSymbols = false;

	private string gameName = "";
	private string gameKeyPathOverride = "";
	private string audioKeyPathOverride = ""; // override for the audio key of the game, used specifically to deal with some issues of TrueVegas games which should have been in ip specific sections, the audioKeyPathOverride will override gameKeyPathOverride for Audio if both are set
	private string groupName = "";
	private JSON gameMap = null;
	
	public bool isProductionReady
	{
		get
		{
			return gameStatus != GameStatus.NON_PRODUCTION_READY &&
			 gameStatus != GameStatus.LICENSE_LAPSED &&
			 gameStatus != GameStatus.PORT &&
			 gameStatus != GameStatus.PORT_NEEDS_ART;
		}
	}

	public bool isPort
	{
		get
		{
			return gameStatus == GameStatus.PORT || gameStatus == GameStatus.PORT_NEEDS_ART;
		}
	}

	// use this for the game key when creating paths to assets, this ensures that games that share an entire bundle will path correctly
	// see "game_key_path_override" to see how this is used, gen42/gen38 is an example
	public string gameKeyPath
	{
		get
		{
			if (!string.IsNullOrEmpty(gameKeyPathOverride))
			{
				return gameKeyPathOverride;
			}
			else
			{
				return gameName;
			}
		}
	}

	// use this for the game key when accessing stuff for audio, basically adds another layer of override for audio key override,
	// this allows us to fix issues for instance from TrueVegas games where they were called tvxx but should have actually been
	// related to an ip, like aruze01
	public string audioKeyPath
	{
		get
		{
			if (!string.IsNullOrEmpty(audioKeyPathOverride))
			{
				return audioKeyPathOverride;
			}
			else if (!string.IsNullOrEmpty(gameKeyPathOverride))
			{
				return gameKeyPathOverride;
			}
			else
			{
				return gameName;
			}
		}
	}

	public string getGroupName()
	{
		return groupName;
	}

	// This constructor should be obsolete when we're 100% driven from the JSON text file.
	public SlotResourceData(string gameName, string groupName)
	{
		this.gameName = gameName;
		this.groupName = groupName;
	}

	public string GenerateWingPath(string expectedKey, System.Func<string> pathFunction)
	{
		string imgName = gameMap.getString(expectedKey, "");

		if (!string.IsNullOrEmpty(imgName))
		{
			if (imgName == "default")
			{
                return pathFunction();
            }
			else
			{
				return getCustomWingPath(imgName);
			}
		}

		return "";
	}

	public string GenerateWingPath(string expectedKey, Dictionary<string, System.Func<string>> dict)
	{
		string imgName = gameMap.getString(expectedKey, "");
		if (!string.IsNullOrEmpty(imgName))
		{
			System.Func<string> pathFunction;
			if (dict.TryGetValue(imgName, out pathFunction))
			{
				return pathFunction();
			}
			else
			{
				return getCustomWingPath(imgName);
			}
		}
		return "";
	}

	public SlotResourceData(string gameName, JSON gameMap)
	{
		this.gameMap = gameMap;
		this.gameName = gameName;
		this.groupName = gameMap.getString("group", "");

		audioKeyPathOverride = gameMap.getString("audio_key_path_override", "");
		gameKeyPathOverride = gameMap.getString("game_key_path_override", "");

		builtInProgressiveLobbyFrame = gameMap.getString(BUILT_IN_PROGRESSIVE_LOBBY_FRAME, JackpotLobbyOptionDecorator.JackpotTypeEnum.Default.ToString());

		creditBonusOutcomeKey = gameMap.getString(CREDIT_BONUS_OUTCOME_KEY, "");

		slotPrefabPath = getGameSpecificAssetPath(gameMap.getString(BASEGAME_PREFAB_KEY, ""));
		portalPrefabPath = getGameSpecificAssetPath(gameMap.getString(PORTAL_PREFAB_KEY, ""));
		bigWinPrefabPath = getGameSpecificAssetPath(gameMap.getString(BIGWIN_PREFAB_KEY, ""));
		superWinPrefabPath = getGameSpecificAssetPath(gameMap.getString(SUPERWIN_PREFAB_KEY, ""));
		megaWinPrefabPath = getGameSpecificAssetPath(gameMap.getString(MEGAWIN_PREFAB_KEY, ""));
		bonusSummaryIcons = new Dictionary<BonusGameType, string>();
		scatterBonusPrefabPath = getGameSpecificAssetPath(gameMap.getString(SCATTER_PREFAB_KEY, ""));
		superBonusPrefabPath = getGameSpecificAssetPath(gameMap.getString(SUPER_BONUS_PREFAB_KEY, ""));

		// basegame anticipations
		defineAnticipationAssetPath (
			ref anticipationFXPath,
			ref optionalAnticipationColumnDefinitionPaths,
			BASEGAME_ANTICIPATION_PREFAB_KEY,
			GROUP_BASEGAME_REEL_ANTICIPATION_PREFAB_KEY,
			COMMON_BASEGAME_REEL_ANTICIPATION_PREFAB_KEY,
			OPTIONAL_ANTICIPATION_COLUMN_DEFINITION_PATHS
		);
		
		// basegame linked anticipations
		defineAssetPath(ref anticipationLinkedFXPath, BASEGAME_LINKED_ANTICIPATION_PREFAB_KEY);

		// freespin anticipations
		defineAnticipationAssetPath (
			ref freespinAnticipationFXPath,
			ref freespinOptionalAnticipationColumnDefinitionPaths,
			FREESPIN_ANTICIPATION_PREFAB_KEY,
			GROUP_FREESPINS_REEL_ANTICIPATION_PREFAB_KEY,
			COMMON_FREESPINS_REEL_ANTICIPATION_PREFAB_KEY,
			OPTIONAL_FREESPIN_ANTICIPATION_COLUMN_DEFINITION_PATHS
		);

		// Freespin linked anticipations
		defineAssetPath(ref freespinAnticipationLinkedFXPath, FREESPIN_LINKED_ANTICIPATION_PREFAB_KEY);

		defineFeatureAnticipationPaths(ref featureAnticipationPaths, FEATURE_ANTICIPATION_PATHS_KEY);
		defineFeatureAnticipationPaths(ref freespinFeatureAnticipationPaths, FREESPIN_FEATURE_ANTICIPATION_PATHS_KEY);

		addBonusSummaryIcon(BonusGameType.GIFTING, FREESPIN_SUMMARY_ICON, GROUP_FREESPIN_SUMMARY_ICON);
		addBonusSummaryIcon(BonusGameType.CHALLENGE, CHALLENGE_SUMMARY_ICON, GROUP_CHALLENGE_SUMMARY_ICON);
		addBonusSummaryIcon(BonusGameType.CREDIT, CREDIT_SUMMARY_ICON, GROUP_CREDIT_SUMMARY_ICON);
		addBonusSummaryIcon(BonusGameType.PORTAL, PORTAL_SUMMARY_ICON, GROUP_PORTAL_SUMMARY_ICON);
		addBonusSummaryIcon(BonusGameType.SUPER_BONUS, SUPER_BONUS_SUMMARY_ICON, GROUP_SUPER_BONUS_SUMMARY_ICON);

		defineAssetPath(ref creditBonusPrefabPath, CREDIT_BONUS_PREFAB_KEY, GROUP_CREDIT_BONUS_PREFAB_KEY);
		defineAssetPath(ref freeSpinPrefabPath, FREESPIN_PREFAB_KEY, GROUP_FREESPIN_PREFAB_KEY);
		defineAssetPath(ref bonusPrefabPath, CHALLENGE_PREFAB_KEY, GROUP_CHALLENGE_PREFAB_KEY);
		defineAssetPath(ref freeSpinPrefabVersion2Path, FREESPIN_PREFAB_KEY_2);
		defineAssetPath(ref freeSpinPrefabVersion3Path, FREESPIN_PREFAB_KEY_3);
		defineAssetPath(ref freeSpinPrefabSuperPath, FREESPIN_SUPER_PREFAB);

		defineBigWinAssetPath(
			ref bigWinPrefabPath,
			BIGWIN_PREFAB_KEY,
			GROUP_BIGWIN_PREFAB_KEY,
			COMMON_BIGWIN_PREFAB_KEY
		);

		defineBigWinAssetPath(
			ref superWinPrefabPath,
			SUPERWIN_PREFAB_KEY,
			GROUP_SUPERWIN_PREFAB_KEY,
			COMMON_SUPERWIN_PREFAB_KEY
		);

		defineBigWinAssetPath(
			ref megaWinPrefabPath,
			MEGAWIN_PREFAB_KEY,
			GROUP_MEGAWIN_PREFAB_KEY,
			COMMON_MEGAWIN_PREFAB_KEY
		);

		wingTexturePath = GenerateWingPath("basegame_wings", new Dictionary<string, System.Func<string>>(){{"default", getGameWingPath}, {"group default", getGroupWingPath}});
		wingTexturePathFSIntro = GenerateWingPath("freespin_wings_intro", getFreeSpinsIntroGameWingPath);
		wingTexturePathFS = GenerateWingPath("freespin_wings", getFreeSpinsGameWingPath);
		wingTexturePathFS_2 = GenerateWingPath("freespin_wings_2", getFreeSpinVariantOneGameWingPath);
		wingTexturePathFS_3 = GenerateWingPath("freespin_wings_3", getFreeSpinVariantTwoGameWingPath);
		wingTexturePathCH = GenerateWingPath("challenge_wings", getChallengeGameWingPath);
		wingTexturePathCH_2 = GenerateWingPath("challenge_wings_2", getChallengeVariantOneGameWingPath);
		wingTexturePathCH_3 = GenerateWingPath("challenge_wings_3", getChallengeVariantTwoGameWingPath);
		wingTexturePathCH_4 = GenerateWingPath("challenge_wings_4", getChallengeVariantThreeGameWingPath);
		wingTexturePortalPath = GenerateWingPath("portal_wings", getPortalWingPath);
		bigWinWingTexturePath = GenerateWingPath("bigwin_wings", getBigWinWingPath);

		commonBundles = gameMap.getStringArray(COMMON_BUNDLES);
		lobbyImageFilename = SlotResourceMap.getLobbyImagePath(groupName, gameName);
		string status = gameMap.getString("status", "").ToUpper();

		if (System.Enum.IsDefined(typeof(GameStatus), status))
		{
			gameStatus = (GameStatus)System.Enum.Parse(typeof(GameStatus), status);
		}
		else
		{
			Debug.LogWarning(string.Format("GameStatus {0} is invalid for game {1}", status, gameName));

			gameStatus = GameStatus.NON_PRODUCTION_READY;
		}

		isUsingOptimizedFlattenedSymbols = gameMap.getBool(IS_USING_OPTIMIZED_FLATTENED_SYMBOLS, false);
	}
	
	public void defineFeatureAnticipationPaths(ref Dictionary<string, string> pathsDict, string key)
	{
		if (!gameMap.hasKey(key))
		{
			return;
		}
		
		pathsDict = new Dictionary<string, string>();
		
		foreach (KeyValuePair<string, string> kvp in gameMap.getStringStringDict(key))
		{
			pathsDict.Add(kvp.Key, getGameSpecificAssetPath(kvp.Value));
		}
	}

	// Sets up the bigwin paths
	public void defineBigWinAssetPath(ref string path_out, string gameKey, string groupKey, string commonKey)
	{
		if (!defineAssetPath(ref path_out, gameKey, groupKey))
		{
			if (!string.IsNullOrEmpty(gameMap.getString(commonKey, "")))
			{
				path_out = getCommonBigWinPath(gameMap.getString(commonKey, ""));
			}
		}
	}

	// Sets up the freespin anticipation paths
	public void defineAnticipationAssetPath(ref string path_out, ref string[] paths_out, string gameKey, string groupKey, string commonKey, string optionalKey)
	{
		if (!defineAssetPath(ref path_out, gameKey, groupKey))
		{
			if (!string.IsNullOrEmpty(gameMap.getString(commonKey, "")))
			{
				path_out = getCommonAnticipationPath(gameMap.getString(commonKey, ""));
			}
			else if (gameMap.hasKey(optionalKey))
			{
				string[] paths = gameMap.getStringArray(optionalKey);
				for (int i = 0; i < paths.Length; i++)
				{
					paths[i] = getGameSpecificAssetPath(paths[i]);
				}

				paths_out = paths;
			}
		}
	}

	// Defines the given variable with the value of the first JSON key that has a value.
	public bool defineAssetPath(ref string path, string gameKey, string groupKey = "")
	{
		// Try the game key first.
		string val = gameMap.getString(gameKey, "");
		if (val != "")
		{
			path = getGameSpecificAssetPath(val);
			return true; // found
		}

		// No game, then try group.
		val = gameMap.getString(groupKey, "");
		if (val != "")
		{
			path = getGroupSpecificAssetPath(val);
			return true; // found
		}

		return false; // not found
	}

	// Adds a bonus summary icon using the value of the first JSON key that has a value.
	public void addBonusSummaryIcon(BonusGameType type, string gameKey, string groupKey)
	{
		// Try the game key first.
		string val = gameMap.getString(gameKey, "");
		if (val != "")
		{
			bonusSummaryIcons.Add(type, getGameSpecificImagePath(val));
			return;
		}

		// No game, then try group.
		val = gameMap.getString(groupKey, "");
		if (val != "")
		{
			bonusSummaryIcons.Add(type, getGroupSpecificImagePath(val));
		}
	}

	// Returns the path that all game specific assets should be loaded into with the asset name appended.
	public string getGameSpecificAssetPath(string assetName)
	{
		if (string.IsNullOrEmpty(assetName))
		{
			return "";
		}
		else
		{
			return string.Format(FORMATTED_GAME_PREFAB_PATH, groupName, gameKeyPath, assetName);
		}
	}

	// Returns the path that all the images bundled with with a game should be located with the asset name appended.
	public string getGameSpecificImagePath(string assetName)
	{
		if (string.IsNullOrEmpty(assetName))
		{
			return "";
		}
		else
		{
			return string.Format(FORMATTED_GAME_IMAGE_PATH, groupName, gameKeyPath, assetName);
		}
	}

	// Returns the path that assets in a certian game group are in and appends asset name to the end.
	public string getGroupSpecificAssetPath(string assetName)
	{
		if (string.IsNullOrEmpty(assetName))
		{
			return "";
		}
		else
		{
			return string.Format(FORMATTED_GROUP_PREFAB_PATH, groupName, assetName);
		}
	}

	// Returns the path that images common to a specific game group are located in with the asset name appended to the end.
	public string getGroupSpecificImagePath(string assetName)
	{
		if (string.IsNullOrEmpty(assetName))
		{
			return "";
		}
		else
		{
			return string.Format(FORMATTED_GROUP_IMAGE_PATH, groupName, assetName);
		}
	}

	// Returns the path that all common anticipations are located in with asset name appened to the end.
	public string getCommonAnticipationPath(string assetName)
	{
		if (string.IsNullOrEmpty(assetName))
		{
			return "";
		}
		else
		{
			return string.Format(FORMATTED_COMMON_ANTICIPATION_PATH, assetName);
		}
	}

	// Returns the path that all common big wins are located in with the asset name appended to the end.
	public string getCommonBigWinPath(string assetName)
	{
		SkuId currentSku = SkuResources.currentSku;
		string currentSkuStr = SkuResources.skuString.ToUpper();

		// if using default, hten auto determine the path
		if (assetName == GAMES_COMMON_DEFAULT)
		{
			return string.Format(FORMATTED_GAMES_COMMON_BUNDLE_PREFAB_PATH, currentSkuStr, getUniversalBigWinDefaultFilename(currentSkuStr));
		}

		// not using default so build the string using the explicit name
		return string.Format(FORMATTED_GAMES_COMMON_BUNDLE_PREFAB_PATH, currentSkuStr, assetName);
	}

	// Build the universal big win filename using the sku name
	private string getUniversalBigWinDefaultFilename(string sku)
	{
		return string.Format(GAMES_COMMON_UNIVERSAL_BIG_WIN_FILENAME_FORMAT, sku);
	}

	// Returns the path to the wing image that is common to the game group.
	public string getGroupWingPath()
	{
		return string.Format(FORMATTED_GROUP_WING_PATH, groupName);
	}

	// Returns the path to the wing image that is specific to a game.
	public string getGameWingPath()
	{
		return string.Format(FORMATTED_GAME_WING_PATH, groupName, gameKeyPath);
	}

	// Returns the path to the wing image that is specific to a free spins game.
	public string getFreeSpinsGameWingPath()
	{
		return string.Format(FORMATTED_FREE_SPINS_GAME_WING_PATH, groupName, gameKeyPath);
	}

	public string getFreeSpinsIntroGameWingPath()
	{
		return string.Format(FORMATTED_FREE_SPINS_GAME_INTRO_WING_PATH, groupName, gameKeyPath);
	}

	public string getChallengeGameWingPath()
	{
		return string.Format(FORMATTED_CHALLENGE_GAME_WING_PATH, groupName, gameKeyPath);
	}

	public string getChallengeVariantOneGameWingPath()
	{
		return string.Format(FORMATTED_CHALLENGE_2_GAME_WING_PATH, groupName, gameKeyPath);
	}

	public string getChallengeVariantTwoGameWingPath()
	{
		return string.Format(FORMATTED_CHALLENGE_3_GAME_WING_PATH, groupName, gameKeyPath);
	}

	public string getChallengeVariantThreeGameWingPath()
	{
		return string.Format(FORMATTED_CHALLENGE_4_GAME_WING_PATH, groupName, gameKeyPath);
	}

	public string getFreeSpinVariantOneGameWingPath()
	{
		return string.Format(FORMATTED_FREE_SPINS_TWO_GAME_WING_PATH, groupName, gameKeyPath);
	}

	public string getFreeSpinVariantTwoGameWingPath()
	{
		return string.Format(FORMATTED_FREE_SPINS_THREE_GAME_WING_PATH, groupName, gameKeyPath);
	}

	public string getPortalWingPath()
	{
		return string.Format(FORMATTED_PORTAL_WING_PATH, groupName, gameKeyPath);
	}

	public string getBigWinWingPath()
	{
		return string.Format(FORMATTED_BIG_WIN_WING_PATH, groupName, gameKeyPath);
	}

	public string getCustomWingPath(string imageName)
	{
		if (string.IsNullOrEmpty(imageName))
		{
			return "";
		}
		else
		{
			return string.Format(FORMATTED_GAME_IMAGE_PATH, groupName, gameKeyPath, imageName);
		}
	}
}
