using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class SlotGameData : IResetGame
{
	public string keyName;
	public string name;
	public string description;
	public string groupKey;
	
	public string localShellPath;
	public string spinPanelPath;
	public string topOverlaySkin;
	public string bottomOverlaySkin;
	public string uiSkin;
	public string zTrackString;
	
	public long baseWager;
	public long baseWagerMultiplier;
	public long giftBetMultiplier;
	public string basePayTable;
	public List<JSON> mysteryGiftForcedOutcomeData = null;
	
	public int baseReelLandingStart;
	public int baseReelLandingInterval;
	public int baseAnticipationDelay;
	public int freeSpinReelLandingStart;
	public int freeSpinReelLandingInterval;
	public int freeSpinAnticipationDelay;
	
	public JSON[] progressivePools = null;	// Holds the raw JSON array from global data, so it can be passed directly into updateProgressivePools.
	
	public string[] bonusGames;
	public List<string> bonusGameDataKeys = new List<string>();
	
	public int paylineThickness;
	public int numVisibleSymbols;
	
	public int symbolHeight;
	
	public float spinMovementNormal;
	public float spinMovementAutospin;
	public float spinMovementAnticipation;
	
	public float reelDelay;
	
	public float reelStopAmount;
	public float rollbackAmount;
	public float beginRollbackSpeed;
	public float endRollbackSpeed;

	public long[] wagerMultipliers;

	// list of game wager sets that correlate to a wager set for particular lobbies.
	// a lobby can have a wager set stored in SCAT, and a each game can assign the wager set they will use for that
	// lobby. see SCAT > common > Lobbies > mobile_vip_revamp, and any game that goes in the VIP lobby view
	// their game data > Casino Games > Games > <some game key> > wager sets
	public JSON wagerSetsData = null;
	
	public List<SymbolDisplayInfo> symbolDisplayInfoList = new List<SymbolDisplayInfo>();
	public Dictionary<string, string> soundMap = new Dictionary<string, string>();
	public static Dictionary<string, string> defaultSoundMap = new Dictionary<string, string>();
	
	private Dictionary<string, ReelSetData> reelSets = new Dictionary<string, ReelSetData>();
	
	private static Dictionary<string, SlotGameData> all = new Dictionary<string, SlotGameData>();

	public static string baseUrl = "";		// This is just the base URL from basic data. The game-specific URL has the game's key appended to this when requested.
	public static string dataVersion = "0";	// The version of the data from baseUrl. We include this in the querystring to avoid caching issues for old versions.


	private bool canAddKey(Dictionary<string, string> platformDict, string audioKey)
	{
		bool canAdd = true;
		string platformTag = "";

		// If the platformDict contains the audio key, let's check its platform tag
		if (platformDict.TryGetValue(audioKey, out platformTag))
		{
			// Platform tag can equal: web, mobile, or both
			//	We only care that it isn't marked 'web'. If it is mark flag canAddKey to false.
			if (platformTag.Equals("web"))
			{
				canAdd = false;
			}
		}

		return canAdd;
	}
	
	public SlotGameData (string key, JSON gameJson)
	{
		keyName = key;
		zTrackString = gameJson.getString("ztrack_key_name", "");
		name = gameJson.getString("name", "");
		description = gameJson.getString ("description", "");
		groupKey = gameJson.getString("group", "");
		
		basePayTable = gameJson.getString ("base_pay_table", "");
		baseWager = gameJson.getLong("base_wager", 1);

		// force baseWagerMultiplier to 1 since we aren't using multipliers
		baseWagerMultiplier = 1;

		giftBetMultiplier = gameJson.getLong("gift_multiplier", 1);
		wagerMultipliers = gameJson.getLongArray("wager_multipliers");
		wagerSetsData = gameJson.getJSON("wager_sets");

		LobbyGame lobbyGame = LobbyGame.find(keyName);

		// typically wager sets are already loaded before reaching this point so the basic call doesn't matter, 
		// however for early access we know all data should be loaded at this point so override the wager set if this game is the early access game
		if (lobbyGame != null && lobbyGame.isEarlyAccessGame && Glb.IS_USING_VIP_EARLY_ACCESS_WAGER_SETS && Glb.EARLY_ACCESS_WAGER_SET != "")
		{
			// override this game's wager_set with the early access one
			SlotsWagerSets.addGameWagerSetEntry(keyName, Glb.EARLY_ACCESS_WAGER_SET);
		}
		else if (lobbyGame != null && !lobbyGame.isSneakPreview) // Don't overwrite sneak preview wager sets. When sneak preview expires, it'll flip this bool so it can be overwritten again.
		{
			// this will probably do nothing if the wager_set was already loaded via the lobby (which it will have been)
			SlotsWagerSets.addGameWagerSetEntry(keyName, gameJson.getString("wager_set", ""));
		}
		
		if (lobbyGame != null && lobbyGame.mysteryGiftType != MysteryGiftType.NONE)
		{
			// Mystery gifts use special data for the min multiplier.
			// This data is in an array. Typically only the first element is real data,
			// and any potential elements after that are in SCAT for forced outcome testing.
			JSON[] jsonArray = gameJson.getJsonArray("mystery_gifts");
			if (jsonArray != null && jsonArray.Length > 0)
			{
				mysteryGiftForcedOutcomeData = new List<JSON>();
				// Nowadays it appears that even the actual mystery gift outcome uses 1 multiplier,
				// so we have no way to differentiate between test outcomes and forced outcomes.
				// So, the dev panel/keyboard shortcuts for forcing a mystery gift may not actually force it.
				foreach (JSON forced in jsonArray)
				{
					mysteryGiftForcedOutcomeData.Add(forced);
				}
			}
		}
		
		localShellPath = gameJson.getString("local_shell_path", "");
		topOverlaySkin = gameJson.getString("top_overlay_skin", "");
		bottomOverlaySkin = gameJson.getString("bottom_overlay_skin", "");
		
		baseReelLandingStart = gameJson.getInt("reel_landing_start_ms", 0);
		baseReelLandingInterval = gameJson.getInt("reel_landing_interval_ms", 50);
		baseAnticipationDelay = gameJson.getInt("anticipation_delay_ms", 2250);
		freeSpinReelLandingStart = gameJson.getInt("free_spin_start_ms", 0);
		freeSpinReelLandingInterval = gameJson.getInt("free_spin_interval_ms", 50);
		freeSpinAnticipationDelay = gameJson.getInt("free_spin_anticipation_ms", 2250);

		// Process the paytable symbol list.
		JSON[] symbolJsonArray = gameJson.getJsonArray("symbols");
		foreach (JSON symbolJson in symbolJsonArray)
		{
			symbolDisplayInfoList.Add(new SymbolDisplayInfo(symbolJson));
		}
		
		progressivePools = gameJson.getJsonArray("progressive_pools");
		bonusGames = gameJson.getStringArray("bonus_games");

		// Sometimes bonus_games does not contain all the keys we want to look into, so we also cache the ones out of bonus game data.
		JSON[] bonusGameDataJsonArray = gameJson.getJsonArray("bonus_games_data");
		foreach (JSON elem in bonusGameDataJsonArray)
		{
			bonusGameDataKeys.Add(elem.getString("key_name", ""));
		}
		
		// Refer to SlotBasicProperty slot module for properties that should be set on client instead of scat
		// Leaving here to support older games that use scat data to populate these values. 
		symbolHeight = gameJson.getInt("symbol_height", 1);
		
		float spinSpeed = gameJson.getFloat("spin_speed", 120f);
		float autoSpinSpeed = gameJson.getInt("auto_spin_speed", 85);
		float anticipationSpeed = gameJson.getFloat("anticipation_speed", 120f);
		
		// Animation is assumed 30fps in SCAT because of web.
		spinMovementNormal = (spinSpeed / symbolHeight) * 30f;
		float webAutoSpinSpeedCalculation = (100 * spinSpeed / autoSpinSpeed);
		spinMovementAutospin = (webAutoSpinSpeedCalculation / symbolHeight) * 30f;
		spinMovementAnticipation = (anticipationSpeed / symbolHeight) * 30f;
		
		float reelStopHeight = gameJson.getInt("reel_stop_height", 0);
		reelStopAmount = reelStopHeight / symbolHeight;
		
		float rollbackHeight = gameJson.getInt("rollback_height", 0);
		rollbackAmount = rollbackHeight / symbolHeight;
			
		beginRollbackSpeed = gameJson.getFloat("begin_rollback_speed", 0f);
		endRollbackSpeed = gameJson.getFloat("end_rollback_speed", 0f);
		if (endRollbackSpeed < 0)
		{
			Debug.LogError("Rollback speed for " + key + " is less than 0, defualting to 0. Please fix SCAT data!");
			endRollbackSpeed = 0;
		}
		
		paylineThickness = gameJson.getInt("payline_thickness", 0);
		reelDelay = gameJson.getFloat("reel_delay", 0f);
		
		// Process the reel sets.
		JSON[] reelSetArray = gameJson.getJsonArray("reel_sets");
		foreach (JSON reelSetJson in reelSetArray)
		{
			string reelSetKey = reelSetJson.getString("key_name", "");
			if (reelSetKey == "")
			{
				Debug.LogError("Cannot process empty game key");
				continue;
			}
			else if (reelSets.ContainsKey(reelSetKey))
			{
				Debug.LogWarning("ReelSetData already exists for key " + reelSetKey);
				continue;
			}
			
			reelSets.Add(reelSetKey, new ReelSetData(reelSetJson));
			ReelSetData data = reelSets[reelSetKey];
			
			if (data.reelDataList.Count == 0)
			{
				Debug.LogError(String.Format("Reel set '{0}' contains no data! Skipping it.", reelSetKey));
			}
			else
			{
				if (reelSets[reelSetKey].reelDataList != null && reelSets[reelSetKey].reelDataList.Count > 0)
				{
					numVisibleSymbols = reelSets[reelSetKey].reelDataList[0].visibleSymbols;
				}
			}
		}

		// Grab platform data
		Dictionary<string, string> platformDict = gameJson.getStringStringDict("audio_platforms");
		
		AudioInfo.populateAll(gameJson.getJsonArray("audio_assets"));
		
		// If the data is there for the improved sound map population this
		// Makes a copy of the defaultSoundMap, and then replaces the values
		// Sent down on a pergame basis with new sounds. Sounds maps can be found
		// In SCAT Under Slots -> Games -> Game Name / Audio Clips.
		JSON audioKeysDelta = gameJson.getJSON("audio_wd");
		if (defaultSoundMap.Count > 0 && audioKeysDelta != null)
		{
			soundMap = new Dictionary<string, string>(defaultSoundMap);
			foreach (string audiokey in audioKeysDelta.getKeyList())
			{
				if (canAddKey(platformDict, audiokey))
				{
					if (soundMap.ContainsKey(audiokey))
					{
						soundMap[audiokey] = audioKeysDelta.getString(audiokey, "");
					}
					else
					{
						Debug.LogWarning(string.Format("{0} for game {1} is not in the default Sound Map!", audiokey, name));
						soundMap.Add(audiokey, audioKeysDelta.getString(audiokey, ""));
					}
				}
			}
		}
		else
		{
			//Debug.LogWarning(string.Format("Falling back to old way of getting soundMap for game {0}!", name));
			JSON audiokeys = gameJson.getJSON("audio");
			if (audiokeys != null)
			{
				foreach (string audiokey in audiokeys.getKeyList())
				{
					if (canAddKey(platformDict, audiokey))
					{
						soundMap.Add(audiokey, audiokeys.getString(audiokey, ""));
					}
				}
			}
			else
			{
				Debug.LogWarning(string.Format("Audio data missing for game {0}!", name));
			}
		}
	}

	public string getWagerForLobby(string lobbyWagerSet)
	{
		if (Data.liveData.getBool("USE_DATA_FEATURE_WAGERS", false) && wagerSetsData != null)
		{
			return wagerSetsData.getString(lobbyWagerSet, SlotsWagerSets.getWagerSetForGame(keyName));
		}
		return SlotsWagerSets.getWagerSetForGame(keyName);
	}
	
	public static void populateDefaultSoundMap(JSON defaultSoundMapJSON)
	{
		if (defaultSoundMapJSON != null)
		{
			foreach (string audiokey in defaultSoundMapJSON.getKeyList())
			{
				defaultSoundMap.Add(audiokey, defaultSoundMapJSON.getString(audiokey, ""));
			}
		}
		else
		{
			Debug.LogWarning("There was no default sound map passed.");
		}
	}

	// Returns the URL to use for downloading a game's data.
	public static string getDataUrl(string gameKey)
	{
		return string.Format("{0}{1}_data.dat?version={2}", baseUrl, gameKey, dataVersion);
	}
	
	// Returns the list of all games for iterating.
	public static Dictionary<string, SlotGameData>.ValueCollection getAll()
	{
		return all.Values;
	}

	// Finds and returns the game's ReelSetData with the given key name.
	public ReelSetData findReelSet(string reelSetKey)
	{
		ReelSetData reelSetData;
		if (reelSets.TryGetValue(reelSetKey, out reelSetData))
		{
			return reelSetData;
		}
		return null;
	}

	// Validate data used for each reelset from global data, which will in turn validate each ReelStrip
	public void validateReelSets()
	{
		foreach (KeyValuePair<string, ReelSetData> entry in reelSets)
		{
			entry.Value.validateData();
		}
	} 

	// Grab all symbols that can show up on the reels for this game
	public HashSet<string> getUniqueSymbolList()
	{
		HashSet<string> uniqueSymbolList = new HashSet<string>();

		foreach (KeyValuePair<string, ReelSetData> entry in reelSets)
		{
			HashSet<string> reelSetUniqueSymbolList = entry.Value.getUniqueSymbolList();

			// now append that to the overall list of all reelsets
			foreach (string symbol in reelSetUniqueSymbolList)
			{
				if (!uniqueSymbolList.Contains(symbol))
				{
					uniqueSymbolList.Add(symbol);
				}
			}
		}

		return uniqueSymbolList;
	}

	// Grab all symbols that will be used in the pay table, this may include 1x1 
	// versions of symbols that only appeared expanded on the reels.  This is used
	// as part of symbol validation for a GameDataTest.
	public HashSet<string> getPayTableSymbolList()
	{
		HashSet<string> paytableSymbols = new HashSet<string>();

		foreach (SymbolDisplayInfo symbolInfo in symbolDisplayInfoList)
		{
			if (!paytableSymbols.Contains(symbolInfo.keyName))
			{
				paytableSymbols.Add(symbolInfo.keyName);
			}
		}

		return paytableSymbols;
	}
	
	// Finds and returns a game's PaylineSet based on ReelSet.
	public PaylineSet findPaylineSetForReelset(string reelSetKeyName)
	{
		ReelSetData reelSet = findReelSet(reelSetKeyName);
		if (reelSet == null)
		{
			return null;
		}
		
		return PaylineSet.find(reelSet.payLineSet);
	}
	
	// Returns the number of ways to win. Returns 0 if not a "ways" game.
	public int getWaysToWin(string initialReelsetKey)
	{
		ReelSetData initialReelSet = findReelSet(initialReelsetKey);
		PaylineSet paylineSet = PaylineSet.find(initialReelSet.payLineSet);
		if (paylineSet.usesClustering) 
		{
			int ways = 1;
			foreach (ReelData reelData in initialReelSet.reelDataList)
			{
				ways *= reelData.visibleSymbols;
			}
			return ways;
		}
		return 0;
	}
	
	// Returns the number of lines that can win. I guess it might return 0 if not a "lines" game?
	public int getWinLines(string initialReelSetKey)
	{
		ReelSetData initialReelSet = findReelSet(initialReelSetKey);
		PaylineSet paylineSet = PaylineSet.find(initialReelSet.payLineSet);
		if (paylineSet != null)
		{
			return paylineSet.payLines.Count;
		}
		return 0;
	}
	
	// This function will be obsolete when we finish implementing just-in-time game data retrieving.
	public static void populateAll(JSON[] slotGames)
	{
		foreach (JSON gameJson in slotGames)
		{
			populateGame(gameJson);
		}
	}
	
	public static SlotGameData populateGame(JSON gameJson)
	{
		string gameKey = gameJson.getString("key_name", "");
		
		if (gameKey == "")
		{
			Debug.LogError("Cannot process empty game key");
			return null;
		}
		else if (all.ContainsKey(gameKey))
		{
			Debug.LogError("SlotGameData already exists for key " + gameKey);
			return null;
		}
		
		SlotGameData newGame = new SlotGameData(gameKey, gameJson);

		LoLaLobby lobby = (GameState.game != null && GameState.game.isEOSControlled) ? LoLaLobby.findEOSWithGame(gameKey) : LoLaLobby.findWithGame(gameKey);

		if (lobby != null && lobby.lobbyWagerSet != LoLaLobby.DEFAULT_WAGER_SET)
		{
			SlotsWagerSets.addGameWagerSetEntry(gameKey, newGame.getWagerForLobby(lobby.lobbyWagerSet));
		}

		all[gameKey] = newGame;
		return newGame;
	}
	
	public static SlotGameData find(string gameKey)
	{
		SlotGameData gameData;

		if (all.TryGetValue(gameKey, out gameData))
		{
			return gameData;
		}
		// This can happen if the game's data hasn't been requested yet.
		// Game data is requested on a just-in-time basis.
		return null;
	}

	// Gets the amount to move a symbol in this game during any given frame.
	// This is only good for the frame where it is calculated - do not cache this value!
	public float getSpinMovement(bool isAutoSpin, int reelNumber)
	{
		// Speed is in pixels per 1/30th of a second on the flash/web version of the game.
		// Our "amount" values represent normalization from pixels to symbol units (where 1 = the smallest height of a symbol).
		// Note that movement is always a positive value, spinning backwards is handled via SlotReel._spinDirection
		
		// Set targetMovement to the normalized desired spin speed
		float targetMovement = spinMovementNormal;
		
		if (isAutoSpin)
		{
			// This formula is a little unintuitive, but matches Web client. 
			// Resolves to: 
			//				autoSpinSpeed > 100 = slower auto-spin 
			//				autoSpinSpeed < 100 = faster auto-spin
			targetMovement = spinMovementAutospin;
		}

		// This is fucking disgusting, but whatever, the game needs it and it can't happen with the scat data we have atm.
		// I wonder if we still need this now that we can override symbol height
		// but the original comment does not explain why we do it
		if (SlotBaseGame.instance != null && GameState.isDeprecatedMultiSlotBaseGame())
		{
			// Slows down spin speed for isDeprecatedMultiSlotBaseGame reels
			targetMovement *= 0.4f;
		}
		
		// So far targetMovement is in 1x1 symbols per second speed.
		// Adjust this statically based on the target application framerate,
		// so that the changes are resistent to device hiccups.
		float estimatedMovement = targetMovement / MobileUIUtil.deviceTargetFrameRate;
		float estimatedFractionalMovement = Mathf.Repeat(estimatedMovement, 1f);
		if (estimatedFractionalMovement > 0.9f)
		{
			// Nudge faster to avoid slow motion backwards movement illusion.
			targetMovement += 0.2f * MobileUIUtil.deviceTargetFrameRate;
		}
		else if (estimatedFractionalMovement > 0.7f)
		{
			// Nudge slower to avoid backwards movement illusion.
			targetMovement -= 0.2f * MobileUIUtil.deviceTargetFrameRate;
		}
		else if (estimatedFractionalMovement < 0.1f)
		{
			// Nudge faster to avoid slow motion movement illusion.
			targetMovement += 0.1f * MobileUIUtil.deviceTargetFrameRate;
		}
		
		// Calculate frameTime as a reasonable amount of time passed, to avoid weird glitches.
		float frameTime = Mathf.Max(0.01f, Mathf.Min(0.5f, Time.deltaTime));
		
		// Adjust for current frame time and the scat assumed fixed rate of 30 fps.
		float frameMovement = targetMovement * frameTime;
		
		// Insanity cap: moving more than numVisibleSymbols-1 symbols in one frame is insane, don't do it.
		float maxMovement = Mathf.Max(0.7f, 0.1f + (float)(numVisibleSymbols - 1));
		if (frameMovement > maxMovement)
		{
			frameMovement = maxMovement;
		}
		
		return frameMovement;
	}
	
	// getSpinTiming - various timings of reel speeds - in milliseconds.
	public void getSpinTiming(bool isFreeSpin, out int landingStart, out int landingInterval, out int anticipationDelay)
	{
		if (isFreeSpin)
		{
			landingStart = freeSpinReelLandingStart;
			landingInterval = freeSpinReelLandingInterval;
			anticipationDelay = freeSpinAnticipationDelay;
		}
		else
		{
			landingStart = baseReelLandingStart;
			landingInterval = baseReelLandingInterval;
			anticipationDelay = baseAnticipationDelay;
		}
	}
	
	// Returns the total bet amount based on the given multiplier.
	public long getBetAmount(long multiplier)
	{
		return multiplier * baseWager * baseWagerMultiplier;
	}

	// Checks if a symbol is part of a large symbol
	public static bool isLargeSymbolPart(string symbolName)
	{
		Regex rgx = new Regex("^M[1-9]([-][1-9][A-Z])+$");
		return rgx.IsMatch(symbolName);
	}

	// Use regular expression to check if a symbol is the top left corener part 
	// of a multi-part symbol (i.e. contains -#A only).
	public static bool isUpperLeftLargeSymbolPart(string symbolName)
	{
		// should have already checked this before now, but I'll check it here as a double check
		if (SlotGameData.isLargeSymbolPart(symbolName))
		{
			Regex rgx = new Regex("^M[1-9]([-][1-9][A])+$");
			// return if this isn't an A only part of a large symbol
			return rgx.IsMatch(symbolName);
		}
		else
		{
			Debug.LogWarning("Should check that a symbol isLargeSymbolPart() before running this function.  Returning false for symbol = " + symbolName);
			return false;
		}
	}
	
	// Implements IResetGame
	public static void resetStaticClassData()
	{
		all = new Dictionary<string, SlotGameData>();
		defaultSoundMap = new Dictionary<string, string>();
	}
}
