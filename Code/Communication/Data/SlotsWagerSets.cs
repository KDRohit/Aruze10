using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Class to store wager values with unlock levels, sets of wagers values, and the mapping of wager sets to game keys

Original Author: Scott Lepthien
*/
public class SlotsWagerSets : IResetGame
{
	private static Dictionary<string, WagerSetData> allWagerSets = new Dictionary<string, WagerSetData>();
	private static Dictionary<string, string> allGamesToWagerSets = new Dictionary<string, string>();

	private const string WAGER_JSON_KEY = "wager";
	private const string LEVEL_REQ_JSON_KEY = "level_required";
	private const string WAGER_SET_WAGERS_JSON_KEY = "wagers";
	private const string WAGER_SET_LEVELS_JSON_KEY = "required_levels";
	private const string WAGER_SET_JACKPOT_QUALIFYING_WAGERS_KEY = "absolute_min_wager";
	private const string MIN_PROGRESSIVE_JACKPOT_BET_JSON_KEY = "progressive_jackpot_minimum";
	private const string MIN_MYSTERY_GIFT_BET_JSON_KEY = "mystery_gift_minimum";
	private const string MIN_BIG_SLICE_JSON_KEY = "big_slice_minimum";
	private const string MIN_MULTI_PROGRESSIVE_JACKPOT_BET_JSON_KEY = "multi_progressive_minimum";
	private const string MIN_VIP_PROGRESSIVE_BET_JSON_KEY = "vip_progressive_minimum";
	private const string GAME_KEY_JSON_KEY = "key_name";
	private const string VIP_REVAMP_WAGER_SET = "vip_revamp_wager_set";
	private const string DEFAULT_MAX_VOLTAGE_WAGER_SET = "max_voltage_wager_set";
	private const string MAX_VOLTAGE_BRONZE_MIN = "bronze_tier_minimum";
	private const string MAX_VOLTAGE_SILVER_MIN = "silver_tier_minimum";
	private const string MAX_VOLTAGE_GOLD_MIN = "gold_tier_minimum";

	[Obsolete("Wager levels are now wager set specific")]
	private static int highestWagerLevel;

	public static bool hasLevelData { get; private set; }

	private static string maxVoltageWagerSet = DEFAULT_MAX_VOLTAGE_WAGER_SET;
	

	[Obsolete("Wager levels are now sent down in the wager sets and are not global")]
	public class WagerValue : IResetGame
	{
		public long wager = 0L;
		public int requiredLevel = 1;
		
		public static List<WagerValue> all = new List<WagerValue>();
		
		public WagerValue(JSON data)
		{
			wager = data.getLong(WAGER_JSON_KEY, 0L);
			requiredLevel = data.getInt(LEVEL_REQ_JSON_KEY, 0);

			if (requiredLevel > highestWagerLevel)
			{
				highestWagerLevel = requiredLevel;
			}

			all.Add(this);
		}
		
		// Poplulate all the wager values with their unlock levels
		public static void populateAllWagerValues(JSON[] wagerUnlockJsonArray)
		{
			foreach (JSON data in wagerUnlockJsonArray)
			{
					
				new WagerValue(data);
			}
			all.Sort(sortByRequiredLevel);
		}
		
		// Get all the wagers unlocked at a specific level
		public static List<long> getWagersAtLevel(int level)
		{
			List<long> returnVal = new List<long>();
		
			foreach (WagerValue wagerVal in all)
			{
				if (wagerVal.requiredLevel > level)
				{
					break;
				}
			
				returnVal.Add(wagerVal.wager);
			}
		
			return returnVal;
		}

		// Get the level a specific wager value should unlock at
		public static int getUnlockLevelForWager(long wager)
		{
			foreach (WagerValue wagerVal in all)
			{
				if (wagerVal.wager == wager)
				{
					return wagerVal.requiredLevel;
				}
			}
		
			return 0;
		}

		public static int sortByWager(WagerValue a, WagerValue b)
		{
			return a.wager.CompareTo(b.wager);
		}

		public static int sortByRequiredLevel(WagerValue a, WagerValue b)
		{
			return a.requiredLevel.CompareTo(b.requiredLevel);
		}

		public static void resetStaticClassData()
		{
			all = new List<WagerValue>();
		}
	}

	public static void overwriteWagerData(JSON[] wagerSetOverrides)
	{
		foreach (JSON wagerSetData in wagerSetOverrides)
		{
			long[] wagerList = wagerSetData.getLongArray(WAGER_SET_WAGERS_JSON_KEY);
			string keyName = wagerSetData.getString(GAME_KEY_JSON_KEY, "");

			// if the wager list is or less than 4 we are going to return out. we need at least 4 wagers for
			// smart bet selector, and built in progressive games
			if (wagerList == null || wagerList.Length < 4)
			{
				Debug.LogError("SlotsWagerSets: Invalid wagerset override set from server: " + keyName);
				continue;
			}
			updateWagerSetOverride(wagerSetData);
		}
	}

	public static void parseMaxVoltageLoginData(JSON data)
	{
		maxVoltageWagerSet = data.getString("wager_set", DEFAULT_MAX_VOLTAGE_WAGER_SET);
	}
	
	// Adds a set of dynamic override wager set data to an existing wager set.
	// This dynamic wager set will restrict what the player can bet at, but will only
	// be used for the display.  All multiplier calculations will still be based on
	// the full original wager set from SCAT so that we only deal in integer values,
	// and not floating point (which would be truncated).
	private static void updateWagerSetOverride(JSON data)
	{
		WagerSetData wagerSetOverrideData = createNewWagerSet(data);
		
		string keyName = data.getString(GAME_KEY_JSON_KEY, "");
		// Make sure that we have a valid entry to add this override data to
		if (allWagerSets.ContainsKey(keyName))
		{
			allWagerSets[keyName].dynamicWagerSetOverride = wagerSetOverrideData;
		}
		else
		{
			Debug.LogError($"SlotsWagerSets.createOrUpdateWagerSetOverride() - Unable to find wager set to override for keyName={keyName}");
		}
	} 

	private static WagerSetData createNewWagerSet(JSON data)
	{
		// ensure that the list of wagers is sorted (smallest to biggest), on the off chance it isn't sent sorted
		long[] wagerList = data.getLongArray(WAGER_SET_WAGERS_JSON_KEY);
		//Array of wagers that are used as floor values for the feature's wager that they line up with
		long[] absMinQualifyingWagers = data.getLongArray(WAGER_SET_JACKPOT_QUALIFYING_WAGERS_KEY);
		long progressiveJackPotMinBet = data.getLong(MIN_PROGRESSIVE_JACKPOT_BET_JSON_KEY, 0);
		long multiProgressiveMinBet = data.getLong(MIN_MULTI_PROGRESSIVE_JACKPOT_BET_JSON_KEY, 0);
		long vipProgressiveMinBet = data.getLong(MIN_VIP_PROGRESSIVE_BET_JSON_KEY, 0);

		long mvzBronzeWager = data.getLong(MAX_VOLTAGE_BRONZE_MIN, 0);
		long mvzSilverWager = data.getLong(MAX_VOLTAGE_SILVER_MIN, 0);
		long mvzGoldWager = data.getLong(MAX_VOLTAGE_GOLD_MIN, 0);

		long absMinProgressiveJackpotBet = 0;
		long absMinMultiProgressiveJackpotBet = 0;
		long absMinVipProgressiveJackpotBet = 0;

		if (wagerList != null && wagerList.Length > 0)
		{
			if (absMinQualifyingWagers != null && absMinQualifyingWagers.Length > 0 && wagerList.Length == absMinQualifyingWagers.Length)
			{
				//Match up the minQualifyingBetWagers to the associated absMinQualifyingBet
				//The absolute min wagers are unaffected by the jackpot inflation factor and is used if the calculated
				//min wager is lower than the absolute value
				int singleProgJackpotIndex = -1;
				int multiProgJackpotIndex = -1;
				int vipJackpotIndex = -1;

				for (int i = 0; i < wagerList.Length; i++)
				{
					if (wagerList[i] == progressiveJackPotMinBet)
					{
						singleProgJackpotIndex = i;
						absMinProgressiveJackpotBet = absMinQualifyingWagers[singleProgJackpotIndex];
					}

					if (wagerList[i] == multiProgressiveMinBet)
					{
						multiProgJackpotIndex = i;
						absMinMultiProgressiveJackpotBet = absMinQualifyingWagers[multiProgJackpotIndex];
					}

					if (wagerList[i] == vipProgressiveMinBet)
					{
						vipJackpotIndex = i;
						absMinVipProgressiveJackpotBet = absMinQualifyingWagers[vipJackpotIndex];
					}

					if (singleProgJackpotIndex >= 0 && multiProgJackpotIndex >= 0 && vipJackpotIndex >= 0)
					{
						break;
					}
				}
			}
			Array.Sort(wagerList);
		}


		long[] levelList = data.getLongArray(WAGER_SET_LEVELS_JSON_KEY);
		if (levelList != null && levelList.Length > 0)
		{
			Array.Sort(levelList); //sort to match wagerss
			hasLevelData = true; //set flag
		}

		if (hasLevelData && (wagerList == null || levelList.Length != wagerList.Length))
		{
			Debug.LogError("Invalid server wager data.  Wager list does not match length of unlock level list");
		}

		WagerSetData newWagerSet = new WagerSetData();
		newWagerSet.wagers = wagerList;
		newWagerSet.requiredLevels = levelList;
		newWagerSet.mvzBronzeWager = mvzBronzeWager;
		newWagerSet.mvzSilverWager = mvzSilverWager;
		newWagerSet.mvzGoldWager = mvzGoldWager;
		newWagerSet.absMinQualifyingWagers = absMinQualifyingWagers;
		newWagerSet.progressiveJackpotMinBet = progressiveJackPotMinBet;
		newWagerSet.mysteryGiftMinBet = data.getLong(MIN_MYSTERY_GIFT_BET_JSON_KEY, 0);
		newWagerSet.bigSliceMinBet = data.getLong(MIN_BIG_SLICE_JSON_KEY, 0);
		newWagerSet.multiProgressiveMinBet = multiProgressiveMinBet;
		newWagerSet.vipProgressiveMinBet = absMinVipProgressiveJackpotBet;
		newWagerSet.absoluteProgressiveJackpotMinBet = absMinProgressiveJackpotBet;
		newWagerSet.absoluteMultiProgressiveMinBet = absMinMultiProgressiveJackpotBet;
		newWagerSet.absoluteVipProgressiveMinBet = absMinVipProgressiveJackpotBet;

		return newWagerSet;
	}

	public static void populateAllWagerSets(JSON[] wagerSetsJsonArray)
	{
		foreach (JSON data in wagerSetsJsonArray)
		{
			WagerSetData newWagerSet = createNewWagerSet(data);

			string keyName = data.getString(GAME_KEY_JSON_KEY, "");

			if (keyName != "")
			{
				if (!allWagerSets.ContainsKey(keyName))
				{
					allWagerSets.Add(keyName, newWagerSet);
				}
				else
				{
					Debug.LogError($"SlotsWagerSets::populateAll() - Trying to add wager set with duplicate keyName={keyName}");
				}
			}
			else
			{
				Debug.LogError("SlotsWagerSets::populateAll() - Couldn't get key_name for one of the wager sets!");
			}
		}
	}

	// Grab a wager set using a key name.  If the wager set has a Dynamic Wager set override set
	// that will be returned.  If you don't want to check and use the Dynamic Wager set then use
	// getWagerSetIgnoreDynamicWagerSet().
	public static WagerSetData getWagerSet(string keyName)
	{
		if (allWagerSets.ContainsKey(keyName))
		{
			WagerSetData wagerSetToReturn = allWagerSets[keyName];
			if (wagerSetToReturn.dynamicWagerSetOverride != null)
			{
				wagerSetToReturn = wagerSetToReturn.dynamicWagerSetOverride;
			}
			return wagerSetToReturn;
		}
		return null;
	}
	
	// Does almost the same thing as getWagerSet but ignores the Dynamic Wager Set.
	// You want to use this version of the function if wager multipliers are going to be calculated
	// so that we ensure that we don't end up with floating point results that get truncated.
	public static WagerSetData getWagerSetIgnoreDynamicWagerSet(string keyName)
	{
		if (allWagerSets.ContainsKey(keyName))
		{
			WagerSetData wagerSetToReturn = allWagerSets[keyName];
			return wagerSetToReturn;
		}
		return null;
	}

	// Get the list of available wager bets at a given level
	// Will utilize the override if one exists (so this function call should not be used
	// when trying to do calculations).
	private static List<long> getWagersUnlockedAtLevelFromSet(string keyName, int level)
	{
		List<long> wagersUnlockedAtLevelFromSet = new List<long>();
		if (useGlobalWagersUnlockLevels())
		{
			List<long> unlockedWagers = WagerValue.getWagersAtLevel(level);
			long[] wagerSetForGame = getWagerSet(keyName).wagers;

			foreach (long wager in wagerSetForGame)
			{
				if (unlockedWagers.Contains(wager))
				{
					wagersUnlockedAtLevelFromSet.Add(wager);
				}
			}
		}
		else
		{
			WagerSetData data = getWagerSet(keyName);

			if (data != null)
			{
				for (int i = 0; i < data.requiredLevels.Length; ++i)
				{
					if (data.requiredLevels[i] <= level)
					{
						wagersUnlockedAtLevelFromSet.Add(data.wagers[i]);
					}
				}
			}
			else
			{
				Debug.LogError($"SlotsWagerSets::getWagersUnlockedAtLevelFromSet() - Unable to find wager set for keyName={keyName}");
			}
		}

		return wagersUnlockedAtLevelFromSet;
	}

	/// Return the multiplier for a given wagerAmount of a wager_set, basically divides the wagerAmount by the base level wager which should be Glb.GLOBAL_BASE_WAGER or the current games base wager
	private static long getMultiplierForWagerSetValue(string keyName, long wagerAmount)
	{
		long baseWager = Glb.GLOBAL_BASE_WAGER;

		if (ReelGame.activeGame != null && ReelGame.activeGame.slotGameData != null)
		{
			baseWager = ReelGame.activeGame.slotGameData.baseWager;
		}
		
		// double check we have a valid wager_set key and also that the amount we are looking up exists in it
		if (allWagerSets.ContainsKey(keyName))
		{
			long[] wagerList = getWagerSetIgnoreDynamicWagerSet(keyName).wagers;
			bool containsWager = System.Array.Exists(wagerList, element => element == wagerAmount);

			if (containsWager)
			{
				return wagerAmount / baseWager;
			}
			else
			{
				Debug.LogError("Wager value: " + wagerAmount + " doesn't exist for wager set with key: " + keyName + ", returning baseWager = " + baseWager + "!");
				return baseWager;
			}
		}
		else
		{
			Debug.LogError("Unable to find wager set for key: " + keyName + ", returning baseWager = " + baseWager + "!");
			return baseWager;
		}
	}

	/// Return the min multiplier for a game, this is used in bonus games to display the correct value
	private static long getMinMultiplierForWagerSet(string keyName)
	{
		return getMultiplierForWagerSetValue(keyName, getMinWagerSetValue(keyName));
	}

	/// Return the minimum (i.e. wagerList[0]) value for the wager set
	// This should never use the dynamic override since it is used in multiplier calculations
	private static long getMinWagerSetValue(string keyName)
	{
		// double check we have a valid wager_set key, also DO NOT get the dynamic wager set
		// since this needs to return the actual minimum defined in SCAT
		// (not just the lowest value shown to the player)
		WagerSetData wagerSetData = getWagerSetIgnoreDynamicWagerSet(keyName);
		if (wagerSetData != null)
		{
			if (wagerSetData.wagers.Length > 0)
			{
				return wagerSetData.wagers[0];
			}
			else
			{
				Debug.LogError($"SlotsWagerSets.getMinWagerSetValue() - Array of wagers was empty for keyName={keyName}, returning 0!");
				return 0;
			}
		}
		else
		{
			Debug.LogError($"SlotsWagerSets.getMinWagerSetValue() - Unable to find wager set for keyName={keyName}, returning 0!");
			return 0;
		}
	}
	
	// Returns the maximum (i.e. last wager in the list) for the wager set
	// This doesn't take into account dynamic override, since currently all uses of this just
	// want to know what the total possible max bet is in a game (which may not be reflected in a dynamic
	// wager set).  If we ever need a dynamic wager set version we can add it, but it isn't really needed
	// for any calculations we've done in the past.
	private static long getMaxWagerSetValue(string keyName)
	{
		// double check we have a valid wager_set key, also DO NOT get the dynamic wager set
		// since this needs to return the actual minimum defined in SCAT
		// (not just the lowest value shown to the player)
		WagerSetData wagerSetData = getWagerSetIgnoreDynamicWagerSet(keyName);
		if (wagerSetData != null)
		{
			if (wagerSetData.wagers.Length > 0)
			{
				return wagerSetData.wagers[wagerSetData.wagers.Length - 1];
			}
			else
			{
				Debug.LogError($"SlotsWagerSets.getMaxWagerSetValue() - Array of wagers was empty for keyName={keyName}, returning 0!");
				return 0;
			}
		}
		else
		{
			Debug.LogError($"SlotsWagerSets.getMaxWagerSetValue() - Unable to find wager set for keyName={keyName}, returning 0!");
			return 0;
		}
	}
	

	/// Get the min bet amount to qualify for progressive jackpot when using this wager set
	private static long getProgressiveJackpotMinBetForWagerSet(string keyName)
	{
		WagerSetData wagerSetData = getWagerSet(keyName);

		// double check we have a valid wager_set key
		if (wagerSetData != null && wagerSetData.wagers != null)
		{
			return calculateMinBetForFeature(wagerSetData, wagerSetData.progressiveJackpotMinBet, wagerSetData.absoluteProgressiveJackpotMinBet);
		}
		else
		{
			Debug.LogError("Unable to find wager set for key: " + keyName + ", returning 0!");
			return 0;
		}
	}

	/// Get the min bet amount to qualify for mystery gift when using this wager set
	private static long getMysteryGiftMinBetForWagerSet(string keyName)
	{
		WagerSetData wagerSetData = getWagerSet(keyName);

		// double check we have a valid wager_set key
		if (wagerSetData != null)
		{
			return wagerSetData.mysteryGiftMinBet;
		}
		else
		{
			Debug.LogError("Unable to find wager set for key: " + keyName + ", returning 0!");
			return 0;
		}
	}

	/// Get the min bet amount to qualify for big slice when using this wager set
	private static long getBigSliceMinBetForWagerSet(string keyName)
	{
		WagerSetData wagerSetData = getWagerSet(keyName);

		// double check we have a valid wager_set key
		if (wagerSetData != null)
		{
			return wagerSetData.bigSliceMinBet;
		}
		else
		{
			Debug.LogError("Unable to find wager set for key: " + keyName + ", returning 0!");
			return 0;
		}
	}

	/// Get the min bet amount to qualify for multi progressive when using this wager set
	private static long getMultiProgressiveMinBetForWagerSet(string keyName)
	{
		WagerSetData wagerSetData = getWagerSet(keyName);

		// double check we have a valid wager_set key
		if (wagerSetData != null)
		{
			return calculateMinBetForFeature(wagerSetData, wagerSetData.multiProgressiveMinBet, wagerSetData.absoluteMultiProgressiveMinBet);
		}

		Debug.LogError("Unable to find wager set for key: " + keyName + ", returning 0!");
		return 0;
	}

	/// Get the min bet amount to qualify for vip jackpot when using this wager set
	private static long getVipProgressiveMinBetForWagerSet(string keyName)
	{
		WagerSetData wagerSetData = getWagerSet(keyName);

		// double check we have a valid wager_set key
		if (wagerSetData != null)
		{
			return calculateMinBetForFeature(wagerSetData, wagerSetData.vipProgressiveMinBet, wagerSetData.absoluteVipProgressiveMinBet);
		}

		Debug.LogError("Unable to find wager set for key: " + keyName + ", returning 0!");
		return 0;
	}
	
	// Calculate the min bet for the passed in feature values based on the passed in WagerSetData
	private static long calculateMinBetForFeature(WagerSetData wagerSetData, long minBetForFeature, long absoluteMinBetForFeature)
	{
		float calculatedMinBet = minBetForFeature * SlotsPlayer.instance.currentPjpWagerInflationFactor;
		float minBet = Mathf.Max(calculatedMinBet, absoluteMinBetForFeature);
		long actualMinBet = 0;

		for (int i = 0; i < wagerSetData.wagers.Length; ++i)
		{
			if (wagerSetData.wagers[i] >= minBet)
			{
				actualMinBet = wagerSetData.wagers[i];
				break;
			}
		}

		return actualMinBet;
	}

	/// Allows you to double check if a given game is missing a wager set, needed to handle cases where data isn't setup yet
	/// Idealy we should never have a game missing a wager set once we switch to the new wager set system
	public static bool doesGameHaveWagerSet(string gameKey)
	{
		if (allGamesToWagerSets.ContainsKey(gameKey))
		{
			return allGamesToWagerSets[gameKey] != null && allGamesToWagerSets[gameKey] != "";
		}

		return false;
	}

	/// Allow the dynamic loading of these entries, basically this is a stop gap until the data can be loaded in while in the lobby
	/// Until then I'll load it in as the games load
	public static void addGameWagerSetEntry(string gameKey, string wagerSetKey)
	{
		if (wagerSetKey != "")
		{
			if (!allGamesToWagerSets.ContainsKey(gameKey))
			{
				allGamesToWagerSets.Add(gameKey, wagerSetKey);
			}
			else
			{
				// update the entry, just on the off chance there is a difference
				allGamesToWagerSets[gameKey] = wagerSetKey;
			}
		}
	}

	/// Get the wager set for a given game based on the games key
	public static string getWagerSetForGame(string gameKey)
	{
		if (!Data.liveData.getBool("USE_DATA_FEATURE_WAGERS", false))
		{
			if (ExperimentWrapper.VIPLobbyRevamp.isInExperiment && 	LoLaLobby.vipRevamp != null && LoLaLobby.vipRevamp.findGame(gameKey) != null)
			{
				//We've confirmed the game is in the revamp lobby, now make sure we're acutally a high enough level to be playing the VIP version of it
				LobbyGame currentGame = LobbyGame.find(gameKey);
				int vipLevel = VIPStatusBoostEvent.isEnabled() ? VIPStatusBoostEvent.getAdjustedLevel() : SlotsPlayer.instance.vipNewLevel; //If the status boost event is on then grab the boosted level
				if (currentGame != null && currentGame.vipLevel != null && currentGame.vipLevel.levelNumber <= vipLevel)
				{
					return VIP_REVAMP_WAGER_SET;
				}
			}

			if (LoLaLobby.maxVoltage != null && LoLaLobby.maxVoltage.findGame(gameKey) != null)
			{
				return maxVoltageWagerSet;
			}
		}

		if (allGamesToWagerSets.ContainsKey(gameKey))
		{
			return allGamesToWagerSets[gameKey];
		}
		else
		{
			Debug.LogError("Unable to find wager set for game: " + gameKey + ", returning empty string!");
			return "";
		}
	}

	/// Get the wager set for a given game based on the games key
	public static long[] getWagerSetValuesForGame(string gameKey)
	{
		if (allGamesToWagerSets.ContainsKey(gameKey))
		{
			WagerSetData wagerSetData = getWagerSet(allGamesToWagerSets[gameKey]);
			if (wagerSetData != null)
			{
				return wagerSetData.wagers;
			}
			else
			{
				return null;
			}
		}
		else
		{
			Debug.LogError("Unable to find wager set for game: " + gameKey + ", returning empty string!");
			return null;
		}
	}

	public static int getHighestWagerLevel(string wagerSet)
	{
		if (!useGlobalWagersUnlockLevels())
		{
			WagerSetData wagerSetData = getWagerSet(wagerSet);
			if (wagerSetData != null)
			{
				int wagerIndex = wagerSetData.wagers.Length - 1;
				if (wagerSetData.requiredLevels == null || wagerIndex < 0 || wagerSetData.requiredLevels.Length <= wagerIndex)
				{
					Debug.LogError("Invalid unlock level; returning impossible level");
					return int.MaxValue;
				}
				return (int)wagerSetData.requiredLevels[wagerIndex];
			}
			else
			{
				//data is invalid
				Debug.LogError("Wager sets does not contain set: " + wagerSet);
				return int.MaxValue;
			}
		}
		else
		{
			return highestWagerLevel;
		}
	}

	/// Gets the unlock level for a particular wager
	public static int getWagerUnlockLevel(string wagerSet, long wager)
	{
		if (!useGlobalWagersUnlockLevels())
		{
			WagerSetData wagerSetData = getWagerSet(wagerSet);
			if (wagerSetData != null)
			{
				int wagerIndex = System.Array.IndexOf(wagerSetData.wagers, wager);
				if (wagerSetData.requiredLevels == null || wagerIndex < 0 || wagerSetData.requiredLevels.Length <= wagerIndex)
				{
					Debug.LogError("Invalid unlock level; returning impossible level");
					return int.MaxValue;
				}
				return (int)wagerSetData.requiredLevels[wagerIndex];
			}
			else
			{
				//data is invalid
				Debug.LogError("Wager sets does not contain set: " + wagerSet);
				return int.MaxValue;
			}
		}
		else
		{
			return WagerValue.getUnlockLevelForWager(wager);
		}

	}

	/// Get the list of available wager bets at a given level
	public static List<long> getWagersUnlockedAtLevelFromGameWagerSet(string gameKey, int level)
	{
		return getWagersUnlockedAtLevelFromSet(getWagerSetForGame(gameKey), level);
	}

	/// Return the multiplier for a given wagerAmount of a wager_set, basically divides the wagerAmount by the base level wager which should be wagerList[0]
	public static long getMultiplierForGameWagerSetValue(string gameKey, long wagerAmount)
	{
		return getMultiplierForWagerSetValue(getWagerSetForGame(gameKey), wagerAmount);
	}

	// Return the minimum (i.e. wagerList[0]) value for the wager set
	// Never uses the dynamic wager set if one is in use (will still use the original
	// wager set to ensure that multiplier math calculations are always whole numbers)
	public static long getMinGameWagerSetValue(string gameKey)
	{
		return getMinWagerSetValue(getWagerSetForGame(gameKey));
	}

	// Return the maximum (i.e. final wager in the list) value for the wager set
	// This call does not use the dynamic wager set, since current usage is only concerned
	// with the max you could ever bet in a game.
	public static long getMaxGameWagerSetValue(string gameKey)
	{
		return getMaxWagerSetValue(getWagerSetForGame(gameKey));
	}

	/// Return the minimum multiplier for a game's wager set, basically finding the multiplier for wagerList[0]
	public static long getMinMultiplierForGameWagerSet(string gameKey)
	{
		return getMultiplierForGameWagerSetValue(gameKey, getMinGameWagerSetValue(gameKey));
	}

	/// Get the min bet amount to qualify for progressive jackpot when using this game's wager set
	public static long getProgressiveJackpotMinBetForGameWagerSet(string gameKey)
	{
		return getProgressiveJackpotMinBetForWagerSet(getWagerSetForGame(gameKey));
	}

	/// Get the min bet amount to qualify for mystery gift when using this game's wager set
	public static long getMysteryGiftMinBetForGameWagerSet(string gameKey)
	{
		return getMysteryGiftMinBetForWagerSet(getWagerSetForGame(gameKey));
	}

	/// Get the min bet amount to qualify for big slice when using this game's wager set
	public static long getBigSliceMinBetForGameWagerSet(string gameKey)
	{
		return getBigSliceMinBetForWagerSet(getWagerSetForGame(gameKey));
	}

	/// Get the min bet amount to qualify for multi progressive when using this game's wager set
	public static long getMultiProgressiveMinBetForGameWagerSet(string gameKey)
	{
		return getMultiProgressiveMinBetForWagerSet(getWagerSetForGame(gameKey));
	}

	/// Get the min bet amount to qualify for vip jackpot when using this game's wager set
	public static long getVipProgressiveMinBetForGameWagerSet(string gameKey)
	{
		return getVipProgressiveMinBetForWagerSet(getWagerSetForGame(gameKey));
	}

	/// Return the min multiplier for a game, this is used in bonus games to display the correct value
	public static long getMinMultiplierForGame(string gameKey)
	{
		return getMinMultiplierForWagerSet(getWagerSetForGame(gameKey));
	}

	/// <summary>
	///   Returns true if the user meets the level requirement for the given wager
	/// </summary>
	public static bool isAbleToWager(string wagerSet, long wagerValue)
	{
		if (SlotsPlayer.instance.socialMember != null)
		{
			return SlotsPlayer.instance.socialMember.experienceLevel >= getWagerUnlockLevel(wagerSet, wagerValue);
		}
		return false;
	}

	/// Return the relative multiplier based on the min wager value, i.e. wager / minWager
	public static long getRelativeMultiplierForGame(string gameKey, long wagerAmount)
	{
		long minGameWagerSetValue = SlotsWagerSets.getMinGameWagerSetValue(gameKey);

		if (minGameWagerSetValue != 0)
		{
			return wagerAmount / minGameWagerSetValue;
		}
		else
		{
			Debug.LogError("SlotsWagerSets::getRelativeMultiplierForGame(gameKey = " + gameKey + ", wagerAmount = " + wagerAmount + ") - minGameWagerSetValue was zero, avoiding divide by zero, and just returning zero!");
			return 0;
		}
	}

	public static long getMaxVoltageMinBet()
	{
		WagerSetData wagerSetData = getWagerSet(maxVoltageWagerSet);
		return wagerSetData.wagers[0];
	}

	public static int getMaxVoltageBetIndex(long wager)
	{
		WagerSetData wagerSet = getWagerSet(maxVoltageWagerSet);
		int index = System.Array.IndexOf(wagerSet.wagers, wager);
		if (index >= 0)
		{
			return index;
		}

		for (int i = 0; i < wagerSet.wagers.Length; i++)
		{
			if (wagerSet.wagers[i] >= wager)
			{
				return i;
			}
		}
		Debug.LogError("Couldn't find a valid wager index for Max Voltage Minimum wager: " + wager);
		return 0;	
	}

	//Returns a wager thats valid in the Max Voltage Wager set. If the given wager doesn't exist then it returns the next higher wager
	public static long getNearestMaxVoltageWager(long wager, bool returnExact = false)
	{
		WagerSetData wagerSet = getWagerSet(maxVoltageWagerSet);

		int index = System.Array.IndexOf(wagerSet.wagers, wager);
		
		if (index >= 0)
		{
			if (!returnExact)
			{
				index++; //Want to return the next wager up
			}

			if (index < wagerSet.wagers.Length)
			{
				return wagerSet.wagers[index];
			}
			else
			{
				return wagerSet.wagers[wagerSet.wagers.Length-1];
			}
		}

		for (int i = 0; i < wagerSet.wagers.Length; i++)
		{
			if (wagerSet.wagers[i] > wager)
			{
				return wagerSet.wagers[i];
			}
		}

		Debug.LogError("Couldn't find a valid wager for Max Voltage Minimum wager: " + wager);
		return wagerSet.wagers[wagerSet.wagers.Length-1];
	}

	public static long getAbsoluteMinMaxVoltageWager(long wager)
	{
		WagerSetData wagerSet = getWagerSet(maxVoltageWagerSet);

		int index = System.Array.IndexOf(wagerSet.wagers, wager);

		if (index >= 0 && wagerSet.absMinQualifyingWagers != null && index < wagerSet.absMinQualifyingWagers.Length) //If this wager exists, find the corresponding abs min wager, which isn't affected by the inflation factor
		{
			return wagerSet.absMinQualifyingWagers[index];
		}

		return 0;
	}
	
	public static long getAbsMinBuiltInProgressiveWager(string wagerSetKey, long wager, bool removeInflationFactor)
	{
		WagerSetData wagerSet = getWagerSet(wagerSetKey);

		if (wagerSet.absMinQualifyingWagers == null || wagerSet.absMinQualifyingWagers.Length == 0)
		{
			return 0;
		}

		if (removeInflationFactor)
		{
			wager = (long)(wager / SlotsPlayer.instance.currentPjpWagerInflationFactor);
		}
		int index = System.Array.IndexOf(wagerSet.wagers, wager);
		
		if (index >= 0 && index < wagerSet.absMinQualifyingWagers.Length) //If this wager exists, find the corresponding abs min wager, which isn't affected by the inflation factor
		{
			return wagerSet.absMinQualifyingWagers[index];
		}
		else
		{
			//Get the next highest wager and use that corresponding abs min wager amount
			for (int i = 0; i < wagerSet.absMinQualifyingWagers.Length; i++)
			{
				if (wagerSet.wagers[i] >= wager)
				{
					return wagerSet.absMinQualifyingWagers[i];
				}
			}
		}

		return 0;
	}

	public static long getVIPRevampMinBet()
	{
		WagerSetData vipRevampWagerSet = getWagerSet(VIP_REVAMP_WAGER_SET);
		return vipRevampWagerSet.wagers[0];
	}

	public static int[] getMVZMinBetIndices(long bronzeWager, long silverWager, long goldWager)
	{
		int[] minBetIndices = new int[3];
		
		WagerSetData data = getWagerSet(maxVoltageWagerSet);

		if (data != null)
		{
			long newBronzeWager = getNearestMaxVoltageWager(bronzeWager, true);
			long newSilverWager = getNearestMaxVoltageWager(silverWager, true);
			long newGoldWager = getNearestMaxVoltageWager(goldWager, true);
			minBetIndices[0] = Array.IndexOf(data.wagers, newBronzeWager);
			minBetIndices[1] = Array.IndexOf(data.wagers, newSilverWager);
			minBetIndices[2] = Array.IndexOf(data.wagers, newGoldWager);
		}

		return minBetIndices;
	}

	private static bool useGlobalWagersUnlockLevels()
	{
		//Wager Unlock Level Lookup Priority
		//1. If you're in the experiment, grab the level from the SCAT table keyed off of the EOS variant
		//2. If you're not in the experiment, use the levelData from SCAT if its available
		//3. If you're not in the experiment and theres no level data, look up the levels from the default unlock levels SCAT table
		return ExperimentWrapper.GlobalMaxWager.isInExperiment || !hasLevelData;
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		allWagerSets = new Dictionary<string, WagerSetData>();
		allGamesToWagerSets = new Dictionary<string, string>();
		hasLevelData = false;
	}

	public class WagerSetData
	{
		public long[] wagers;
		public long[] requiredLevels;
		public long progressiveJackpotMinBet;
		public long mysteryGiftMinBet;
		public long bigSliceMinBet;
		public long multiProgressiveMinBet;
		public long vipProgressiveMinBet;
		public long absoluteProgressiveJackpotMinBet;
		public long absoluteMultiProgressiveMinBet;
		public long absoluteVipProgressiveMinBet;
		public long[] absMinQualifyingWagers;
		public long mvzBronzeWager;
		public long mvzSilverWager;
		public long mvzGoldWager;
		// A secondary set of dynamically made wager set info
		// All math calculations should still be based on the regular wager set data and
		// this should only be used for display purposes
		public WagerSetData dynamicWagerSetOverride;
	}
}
