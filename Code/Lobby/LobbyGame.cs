using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using Com.HitItRich.EUE;
using TMPro;
using Zynga.Core.Util;

/*
Data structure to hold information about games in the lobby. Not for actually playing games.
*/

public enum MysteryGiftType
{
	// These must be defined in the same order as the mysteryGiftTypeNames array.
	NONE = -1,
	MYSTERY_GIFT,
	BIG_SLICE
}

public enum ExtraFeatureType
{
	NONE = -1,
	SUPER_FAST_SPINS,
	PROGRESSIVE_FREE_SPINS,
	CASH_CHAIN,
	STICK_AND_WIN_NO_PROGRESSIVE
}

public class LobbyGame : IResetGame
{
	public enum LaunchResult
	{
		NO_LAUNCH,
		ASK_INITIAL_BET,
		LAUNCHED
	}
	
	public const int SKU_GAME_MINIMUM_UNLOCK_LEVEL = 5;
	public const int SKU_GAME_LOBBY_PAGE = 2;	// The page to put the xpromo game, where page 1 is the first page.
	
	// These must be defined in the same order as the MysteryGiftType enum.
	public readonly static string[] mysteryGiftTypeNames = new string[]
	{
		"mystery_gift",
		"big_slice_wheel_gift"
	};
	
	public static string[] sirGames = null;	// Will be obsolete when LoLa is used.
	
	public string keyName;
	public string name;
	public string license;
	public string paytableTitleOverride = "";
	public string paytableDescOverride = "";
	public string paytableImageOverride = "";
	public GameExperience xp = null;
#if RWR
	public static string[] rwrSweepstakesGames = null;
	public bool isRWRSweepstakes = false;
	public long rwrSweepstakesMeterMax = 0;
#endif
	public bool isActive;
	public LoLaGame lolaGame = null;	// If LoLa is used, a reference to the data from LoLa for this game.
	
	// These fields are controlled by LoLa if enabled.
	public bool isHighLimit = false;	// High limit games now go into the main lobby since they're not the same as VIP games.
	public bool isProgressive = false;
	public bool isMultiProgressive = false;
	public bool isGiantProgressive = false;
	public bool isAnyMysteryGift = false;
	public bool isVIPEarlyAccess = false;
	public bool isRoyalRush = false;
	public bool isBuiltInProgressive = false; // This doesn't come from LoLa, and instead is based on global data to determine if this game uses a built in (i.e. always part of the game) progressive feature
	public bool isGoldPassGame { get; private set; }
	public bool isSilverPassGame { get; private set; }

	// gets flipped in the launch stats call and by DoSomethingPersonalizedContent.
	public bool isRecomended = false;

	//public bool isFeatured = false;	// Not being used with coming soon or sneak preview currently... keeping in case we reverse that
	public bool isComingSoon = false;
	public bool isSneakPreview = false;
	public bool isDeluxe = false;		// Generally a fancier version of some other game.
	public bool isDoubleFreeSpins = false;

	// when a game can be in the main lobby, or a EOS controlled lobby (e.g. land of oz, or VIP revamp room)
	// we set this eosControlledLobby reference to parse through instead of checking for
	// booleans like isLOZGame, or alternatively isVIPRevampGame which would bloat the LoppyOption.populateAll()
	public LoLaLobby eosControlledLobby = null;

	public string skinKey;
	public string assetKey;				// The asset that contains this game's prefab.
	public List<Info> info = new List<Info>();
	public LobbyGameGroup groupInfo = null;
	public List<ProgressiveJackpot> progressiveJackpots = null;
	public VIPLevel vipLevel = null;		// If this is a VIP game, this is the level that unlocks it.
	public MysteryGiftType mysteryGiftType = MysteryGiftType.NONE;
	public ExtraFeatureType extraFeatureType = ExtraFeatureType.NONE;
	public int mysteryGift1X2SortOrder = 0;		// The sort order for 1X2 options, which are automatically pinned to each lobby page.
	public long progressiveMysteryBetAmount = 0;	// when using flat wager values this stores the bet info set from the dialog when entering a special type of game
	public bool isUpdateRequired = false;		// Does this game require an app update?
	public Color lobbyColor = Color.white;
	public bool isUnlocked = false;				// Whether the game is unlocked for any reason.
	public bool isLevelLockedWithFeatures = false;	// Not used with LoLa. If true, the game is level locked even when it has a feature like progressives or mystery gift.
	public long defaultBetValue { get; private set; } // default bet amount based on wallet size, and smart bet selector EOS modifier
	
	private string sneakPreviewWagerSet = "";
	
	public static LobbyGame skuGameUnlock = null;       // Play sku to unlock this game.
	public static LobbyGame vipEarlyAccessGame = null;	// Gets set to the early access VIP game.
	public static LobbyGame priorityGame = null;		// Game that gets first spot in lobby
	public static EosExperiment priorityExperiment = null; //experiment that determines if priority game should be shown first
	public static List<LobbyGame> doubleFreeSpinGames = new List<LobbyGame>();
	public static LobbyGame bigSliceGame = null;

	// Used in the MOTD and carousel slide for sneak preview.
	// There can be more than 1 sneak preview game but we only reference one of them for UI.
	public static LobbyGame sneakPreviewGame = null;

	public Texture bitmap;
	
	public static List<LobbyGame> pinnedMysteryGiftGames = new List<LobbyGame>();
	public static List<LobbyGame> deluxeGames = new List<LobbyGame>();

	private Dictionary<int, int> variantBasedUnlockLevels = new Dictionary<int, int>();

	private static Dictionary<string, LobbyGame> all = new Dictionary<string, LobbyGame>();

	// Since accurate results from SlotLicense.isLicenseAllowed relies on player data
	// to be already loaded too, we can't set isAllowedLicense as a variable in the LobbyGame constructor.
	// So, it looks it up on the fly.
	public bool isAllowedLicense
	{
		get
		{
			return
				SlotLicense.isLicenseAllowed(groupInfo.license) &&
				SlotLicense.isLicenseAllowed(license);
		}
	}
	
	// Whether this game can be unlocked via a game unlocking feature.						
	public bool canBeUnlocked
	{
		get
		{
			return
				_canBeUnlocked &&
				xp != null &&
				!isUnlocked &&
				!xp.isPendingPlayerUnlock &&
				vipLevel == null;	// Make sure it's not in the VIP lobby.
		}
	}
	private bool _canBeUnlocked = false;

	public bool canBeUnlockedFtue
	{
		get
		{
			return _canBeUnlocked &&
				xp != null &&
				!xp.isPendingPlayerUnlock &&
				vipLevel == null;
		}
	}
	
	// Returns the unlock level for this game based on the current experiment variant value.
	public int unlockLevel
	{
		get
		{
			if (isGoldPassGame)
			{
				return 0;
			}
			int level;
			if (variantBasedUnlockLevels.TryGetValue(ExperimentWrapper.LockedLobby.variant, out level))
			{
				return level;
			}
			return 0;
		}
	}

	// sorted bet values for buttons in progressive/mystery game bet selector dialog
	public long[] getBetButtonValues(string wagerSet)
	{
		if (ExperimentWrapper.SmartBetSelector.isInExperiment)
		{
			return getSmartBetValues(wagerSet);
		}
		
		long[] buttonValues = new long[4];
		// Dynamically determine the four values to show.
		// 0 = Lowest game based wager
		// 1 = 1 lower than min qualifying bet
		// 2 = Min qualifying bet
		// 3 = Max bet (see SIR-4768)

		// handle flat wager version which will involve less math
		long[] allBetAmounts = SlotsWagerSets.getWagerSetValuesForGame(keyName);

		int qualifyingAmountBetIndex = -1;
		long amount = specialGameMinQualifyingAmount;

		for (int i = 0; i < allBetAmounts.Length; i++)
		{
			if (allBetAmounts[i] >= amount)
			{
				qualifyingAmountBetIndex = i;
				break;
			}
		}

		if (qualifyingAmountBetIndex < 1)
		{
			Data.showIssue("betButtonValues: Could not find a suitable bet index for min qualifying multiplier " + amount);
			qualifyingAmountBetIndex = 1;	// Do something to prevent a crash, though the button values might be weird.			
		}

		int topIndex = Mathf.Max(0, allBetAmounts.Length - 1);

		buttonValues[0] = allBetAmounts[0];
		buttonValues[1] = allBetAmounts[qualifyingAmountBetIndex - 1];
		buttonValues[2] = allBetAmounts[qualifyingAmountBetIndex];
		buttonValues[3] = allBetAmounts[topIndex];

		return buttonValues;
	}

	// Init the defaultBetValue and return it if it hasn't been set yet
	public long getDefaultBetValue()
	{
		defaultBetValue = SlotsPlayer.creditAmount * getSmartBetModifier() / 100;
		return defaultBetValue;
	}

	// Rule_1: the lowest value in the bet selector should be the bet closest to the default bet.
	// Rule_2: The subsequent values in the bet selector would increment to remain in escalating order.
	// Number of increments between bet selector values is tunable in EOS per topper
	// Example: mysterygift: 1,1,1 would increment subsequent bets on top of the default bet by 1,1,1
	// Rule_3: If the greatest value in the bet selector as determined by rules 1 and 2 is less than the minQ bet, the minQ bet should be displayed as the greatest value instead. 
	public long[] getSmartBetValues(string wagerSet)
	{
		defaultBetValue = SlotsPlayer.creditAmount * getSmartBetModifier() / 100;
		int[] betIncrements = getSmartBetIncrements();

		int totalIncrements = 0;
		for (int j = betIncrements.Length; --j >= 0; ) { totalIncrements += betIncrements[j]; }

		long[] allBetAmounts = SlotsWagerSets.getWagerSetValuesForGame(keyName);
		
		int lastIndex = allBetAmounts.Length-1;
		
		// user is near the max bet range, just return the last 4 wagers the user can bet on
		if (defaultBetValue >= allBetAmounts[lastIndex-3] &&
			SlotsPlayer.instance.socialMember.experienceLevel >= SlotsWagerSets.getHighestWagerLevel(wagerSet))
		{
			// they can place any wager, grab the last 4
			return new long[]
			{
				  allBetAmounts[lastIndex-3]
				, allBetAmounts[lastIndex-2]
				, allBetAmounts[lastIndex-1]
				, allBetAmounts[lastIndex]
			};
		}
		else
		{
			// check to see if increment order will cause indexing error, if so, return users bet, and next 3
			int startingIndex = getSmartBetStartingIndex(wagerSet, allBetAmounts);

			// indexing error resolution
			int finalIndex = startingIndex + totalIncrements;
			if (finalIndex > lastIndex || betIncrements.Length != 3)
			{
				betIncrements = new int[]{1, 1, 1};

				if (finalIndex > lastIndex)
				{
					startingIndex = lastIndex - 3;
					finalIndex = lastIndex;
				}
			}

			// if the highest bet amount is < the min qualifying bet amount, we set the highest amount to the min qualifier
			long finalWager  =  allBetAmounts[finalIndex] < specialGameMinQualifyingAmount ?
				specialGameMinQualifyingAmount : allBetAmounts[finalIndex];
			
			// set final bet amounts
			return new long[]
			{
				allBetAmounts[startingIndex],
				allBetAmounts[startingIndex + betIncrements[0]],
				allBetAmounts[startingIndex + betIncrements[0] + betIncrements[1]],
				finalWager
			};
		}
	}
	
	private int getSmartBetModifier(string type = "jackpot")
	{
		if (isProgressive)
		{
			return ExperimentWrapper.SmartBetSelector.jackpotModifier;
		}
		else if (mysteryGiftType != MysteryGiftType.NONE)
		{
			if (mysteryGiftType == MysteryGiftType.BIG_SLICE)
			{
				return ExperimentWrapper.SmartBetSelector.bigSliceModifier;
			}
			else
			{
				return ExperimentWrapper.SmartBetSelector.mysteryModifier;
			}
		}
		
		return Mathf.Max(1,ExperimentWrapper.SmartBetSelector.nonTopperModifier);
	}

	public int getSmartBetStartingIndex(string wagerSet, long[] allBetAmounts)
	{
		defaultBetValue = getDefaultBetValue();
		int startingIndex = 0;
		for (int i = 0; i < allBetAmounts.Length; ++i)
		{
			// check if the player can even bet 
			if (!SlotsWagerSets.isAbleToWager(wagerSet, allBetAmounts[i]))
			{
				startingIndex = Mathf.Max(0, i-1);
				break;
			}
			// if the next wager amount is higher than the default bet, check to see if we should
			// use the next wager index, or the current one, based on the smallest numerical distance
			else if (defaultBetValue < allBetAmounts[i])
			{
				// no comparison needed, break the loop
				if (i == 0)
				{
					break;
				}

				long deltaFromLargerWager = allBetAmounts[i] - defaultBetValue;
				long deltaFromSmallerWager = defaultBetValue - allBetAmounts[i-1];

				// take the bet closer to the default amount
				startingIndex = deltaFromSmallerWager < deltaFromLargerWager ? i-1 : i;
				break;
			}
		}
		return startingIndex;
	}

	private int[] getSmartBetIncrements(string type = "jackpot")
	{
		if (isProgressive)
		{
			return ExperimentWrapper.SmartBetSelector.jackpotIncrements;
		}
		else if (mysteryGiftType != MysteryGiftType.NONE)
		{
			if (mysteryGiftType == MysteryGiftType.BIG_SLICE)
			{
				return ExperimentWrapper.SmartBetSelector.bigSliceIncrements;
			}
			else
			{
				return ExperimentWrapper.SmartBetSelector.mysteryIncrements;
			}
		}
		
		return new int[]{1,1,1};
	}

	public void setAsRichPassGame()
	{
		if (lolaGame != null)
		{
			if (lolaGame.originalUnlockMode != LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS)
			{
				lolaGame.originalUnlockMode = lolaGame.unlockMode;
				lolaGame.unlockMode = LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS;
			}

			foreach (LoLaLobbyDisplay display in lolaGame.gameDisplays)
			{
				if (display.originalUnlockMode != LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS)
				{
					display.originalUnlockMode = display.unlockMode;
					display.unlockMode = LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS;
				}
			}
			setIsUnlocked();
		}
	}

	public void removeRichPassUnlock()
	{
		if (lolaGame != null && lolaGame.unlockMode == LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS)
		{
			lolaGame.unlockMode = lolaGame.originalUnlockMode;
			foreach (LoLaLobbyDisplay display in lolaGame.gameDisplays)
			{
				display.unlockMode = display.originalUnlockMode;
			}
			setIsUnlocked();
		}
	}
	

	// returns the number of valid bet levels for a progressive/mysterty game
	public int getNumBetLevels(string wagerSet)
	{
		long[] buttonValues = getBetButtonValues(wagerSet);

		int numAvail = 0;

		for (int i = 0; i < buttonValues.Length; i++)
		{
			int wagerUnlockLevel = SlotsWagerSets.getWagerUnlockLevel(wagerSet, buttonValues[i]);
			if (wagerUnlockLevel <= SlotsPlayer.instance.socialMember.experienceLevel)
			{
				// this wager is unlocked for the player, or is the lowest bet amount which should always be unlocked
				numAvail++;
			}
		}

		return numAvail;		
	}	
	
	// Set all the booleans that say what kind of lobby feature this game has, if any.
	// Must be done after populating all global data, experiments, and LoLa data (if used).
	public static void setAllLobbyFeatures()
	{
		resetStaticFeatureData();
		foreach (LobbyGame game in all.Values)
		{
			game.setLobbyFeatures();
		}
	}

	// Determine if this game is using REEVALUATOR ProgressiveJackpots which means
	// that the game should have a built in progressive we will need to display
	private bool hasBuiltInProgressive()
	{
		// see if we can find a jackpot entry
		foreach (ProgressiveJackpot jp in ProgressiveJackpot.all.Values)
		{
			if (jp.type == ProgressiveJackpot.Type.REEVALUATOR)
			{
				foreach (string gameKey in jp.possibleGameKeys)
				{
					if (gameKey == keyName)
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	public static void setSpecificLobbyFeatures()
	{
		resetStaticFeatureData();
		bool shouldUseDisplayFeatures = Data.liveData.getBool("ENABLE_EARLY_USER_LOBBIES", false);
		foreach (LobbyGame game in all.Values)
		{
			if (shouldUseDisplayFeatures)
			{
				game.setDisplayFeatures();
			}
			else
			{
				game.setLobbyFeatures();
			}
		}
	}

	private static void resetStaticFeatureData()
	{
		skuGameUnlock = null;
		vipEarlyAccessGame = null;
		bigSliceGame = null;
		priorityGame = null;
		priorityExperiment = null;
		sneakPreviewGame = null;
		doubleFreeSpinGames = new List<LobbyGame>();
		pinnedMysteryGiftGames = new List<LobbyGame>();
		deluxeGames = new List<LobbyGame>();
	}

	private void resetFeatures()
	{
		mysteryGiftType = MysteryGiftType.NONE;
		isDoubleFreeSpins = false;
		isRoyalRush = false;
		isComingSoon = false;
		progressiveJackpots = null;
		isProgressive = false;
		isBuiltInProgressive = false;
		isMultiProgressive = false;
		isGiantProgressive = false;
		isAnyMysteryGift = false;
		isVIPEarlyAccess = false;
		isGoldPassGame = false;
	}

	public void setDisplayFeatures(LobbyInfo.Type targetLobby = LobbyInfo.Type.UNDEFINED)
	{
		if (targetLobby == LobbyInfo.Type.UNDEFINED)
		{
			targetLobby = LobbyLoader.lastLobby;
		}
		string lobbyName = LoLaLobby.findKeyByLobbyInfo(targetLobby);
		
		resetFeatures();

		if (!string.IsNullOrEmpty(lobbyName) && lolaGame != null)
		{
			foreach (LoLaLobbyDisplay display in lolaGame.gameDisplays)
			{
				if (display.lobbyKey == lobbyName)
				{
					setRichPassUnlock();
					setFeature(display.feature);
				}
			}
		}
	}

	// Instance method for setAllLobbyFeatures().
	public void setLobbyFeatures()
	{
		resetFeatures();
		if (lolaGame != null)
		{
			setRichPassUnlock();
			setFeature(lolaGame.feature);	
		}
	}

	private void setRichPassUnlock()
	{
		if (CampaignDirector.richPass == null || !CampaignDirector.richPass.isActive)
		{
			return;
		}
		if (lolaGame != null && lolaGame.game != null)
		{
			//Don't unlock the seasonal challenge game if its also the gold game
			if (RichPassCampaign.silverGameKeys.Contains(lolaGame.game.keyName) && lolaGame.unlockMode != LoLaGame.UnlockMode.UNLOCK_BY_GOLD_PASS)
			{			
				lolaGame.unlockMode = LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS;	
			}
		}
	}

	private void setFeature(LoLaGame.Feature feature)
	{
		bool isGameWithBuiltInProgressive = hasBuiltInProgressive();

		if (lolaGame != null)
		{
			switch (feature)
			{
				case LoLaGame.Feature.STANDARD_PROGRESSIVE:
				case LoLaGame.Feature.MULTI_PROGRESSIVE:
				case LoLaGame.Feature.GIANT_PROGRESSIVE:
				case LoLaGame.Feature.REEVALUATOR_PROGRESSIVE:
				case LoLaGame.Feature.STICK_AND_WIN:
					// We need to find the ProgressiveJackpot data for this game and link them.
					// We do that by brute-force searching each ProgressiveJackpot object's games array for this game key.
					bool foundProgressiveJackpot = false;
					foreach (ProgressiveJackpot jp in ProgressiveJackpot.all.Values)
					{
						if (jp.type == lolaGame.displayProgressiveType(feature))
						{
							foreach (string gameKey in jp.possibleGameKeys)
							{
								if (gameKey == keyName)
								{
									foundProgressiveJackpot = true;
								
									if (feature == LoLaGame.Feature.MULTI_PROGRESSIVE &&
										progressiveJackpots != null &&
										progressiveJackpots.Count == 3)
									{
										Debug.LogError("Found more than 3 jackpots for multiprogressive game " + gameKey);
									}
									else
									{
										jp.setGame(this, false);
									}
								}
							}
						}
					}
					
					// If this is tagged for STICK_AND_WIN but it doesn't have any progressives, we'll use the ExtraFeatureType
					// system to display a version of the STICK_AND_WIN feature frame that doesn't have a progressive value display.
					if (feature == LoLaGame.Feature.STICK_AND_WIN && !foundProgressiveJackpot)
					{
						extraFeatureType = ExtraFeatureType.STICK_AND_WIN_NO_PROGRESSIVE;
					}
					
					break;
		
				case LoLaGame.Feature.MYSTERY_GIFT:
					mysteryGiftType = MysteryGiftType.MYSTERY_GIFT;
					break;

				case LoLaGame.Feature.BIG_SLICE:
					mysteryGiftType = MysteryGiftType.BIG_SLICE;
					if (bigSliceGame == null && Data.liveData.getBool("FEATURE_BIG_SLICE_ENABLED", false))
					{
						bigSliceGame = this;
					}
					break;

				case LoLaGame.Feature.DOUBLE_FREE_SPINS:
					isDoubleFreeSpins = true;
					doubleFreeSpinGames.Add(this);
					break;

				case LoLaGame.Feature.ROYAL_RUSH:
					// Lets make sure. 
					isRoyalRush = RoyalRushEvent.instance.getInfoByKey(keyName) != null;
					break;
				
				case LoLaGame.Feature.SUPER_FAST_SPINS:
					extraFeatureType = ExtraFeatureType.SUPER_FAST_SPINS;
					break;
				
				case LoLaGame.Feature.PROGRESSIVE_FREE_SPINS:
					extraFeatureType = ExtraFeatureType.PROGRESSIVE_FREE_SPINS;
					break;
				
				case LoLaGame.Feature.CASH_CHAIN:
					extraFeatureType = ExtraFeatureType.CASH_CHAIN;
					break;
			}

			isComingSoon = (lolaGame.unlockMode == LoLaGame.UnlockMode.COMING_SOON);
			isGoldPassGame = lolaGame.unlockMode == LoLaGame.UnlockMode.UNLOCK_BY_GOLD_PASS;
			isSilverPassGame = lolaGame.unlockMode == LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS;
		}
		
		string[] doubleFreeSpinGameList = Data.liveData.getString("DOUBLE_FREE_SPIN_GAME", "notyet").Split(',');
		for (int i = 0; i < doubleFreeSpinGameList.Length; i++)
		{
			LobbyGame doubleFreeSpinGame = find(doubleFreeSpinGameList[i]);
			if (doubleFreeSpinGame != null)
			{
				doubleFreeSpinGame.isDoubleFreeSpins = true;
				doubleFreeSpinGames.Add(doubleFreeSpinGame);
			}
		}

		isProgressive = (progressiveJackpots != null && progressiveJackpots.Count > 0);
		isBuiltInProgressive = (isProgressive && isGameWithBuiltInProgressive);
		isMultiProgressive = (isProgressive && progressiveJackpots.Count == 3);
		isGiantProgressive = (isProgressive && progressiveJackpots.Count == 1 && progressiveJackpots[0] == ProgressiveJackpot.giantJackpot);
		isAnyMysteryGift = (mysteryGiftType != MysteryGiftType.NONE);
		isVIPEarlyAccess = (vipEarlyAccessGame != null && vipEarlyAccessGame.keyName == keyName);
		if (isVIPEarlyAccess)
		{
			// Early Access games have a different wager set. Lets make sure we have the right one.
			if (Glb.EARLY_ACCESS_WAGER_SET != "")
			{
				SlotsWagerSets.addGameWagerSetEntry(keyName, Glb.EARLY_ACCESS_WAGER_SET);
			}
			else
			{
				Debug.LogError("Glb.EARLY_ACCESS_WAGER_SET is not set, but we have a game in Early Access.");
			}			
		}

		if (isAnyMysteryGift)
		{
			// Add pinned games to a list to be sorted to determine page number for each.
			LobbyGame.pinnedMysteryGiftGames.Add(this);
		}

		setSneakPreview();	// Must be called after isComingSoon is set above, since setSneakPreview() may change it.
	}
	
	// Sets the isUnlocked status of the game, based on various factors.
	public void setIsUnlocked()
	{
		bool isGoldGame = lolaGame != null && lolaGame.unlockMode == LoLaGame.UnlockMode.UNLOCK_BY_GOLD_PASS;
		bool isSilverGame = lolaGame != null && lolaGame.unlockMode == LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS;
		if (isGoldGame || isSilverGame)
		{
			setIsUnlockedByLoLaGame();
		}
		else if (isChallengeLobbyGame)
		{
			isUnlocked = CampaignDirector.isChallengeLobbyGameUnlocked(keyName);
		}
		else if (isMaxVoltageGame && SlotsPlayer.instance.socialMember.experienceLevel >= Glb.MAX_VOLTAGE_MIN_LEVEL)
		{
			isUnlocked = true;
		}
		else if (vipLevel != null)
		{
			// This has to be checked before xp.isPermanentUnlock because we've been getting
			// false positives on that for VIP games, possibly from bad player reset code.
			// VIP games are only locked by VIP level, and ignore the LoLa unlock mode.

			// Part of the VIPLevel event, try to do the comparison with the updated level.
			VIPLevel trueLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel, "vip_room_games");
			isUnlocked = (trueLevel.levelNumber >= vipLevel.levelNumber);
		}
		else if (UnlockAllGamesFeature.instance != null && UnlockAllGamesFeature.instance.isEnabled)
		{
			// Set it unlocked when the unlockAllGame charm is active.
			isUnlocked = true;
		}
		else if (xp.isSkuGameUnlock || xp.isPermanentUnlock)
		{
			// If a game is sku game unlocked, then it's always unlocked.
			// This just means no lock will appear on the lobby option,
			// but touching it will launch the xpromo MOTD, not the game.
			// Also, if the game is explicitly unlocked for the player, then it's unlocked.
			isUnlocked = true;
		}
		else if (lolaGame != null)
		{
			setIsUnlockedByLoLaGame();
		}

		// Make sure the lobby option is refreshed if it currently exists.
		LobbyOption option = LobbyOption.activeGameOption(this);
		if (option != null)
		{
			option.refreshButton();
		}
	}

	private void setIsUnlockedByLoLaGame()
	{
		if (!setUnlockForLobby(LobbyLoader.lastLobby))
		{
			switch (lolaGame.unlockMode)
			{
				case LoLaGame.UnlockMode.SNEAK_PREVIEW:
				case LoLaGame.UnlockMode.UNLOCK_FOR_ALL:
					isUnlocked = true;
					break;
						
				case LoLaGame.UnlockMode.UNLOCK_BY_LEVEL:
					isUnlocked = SlotsPlayer.instance.socialMember.experienceLevel >= unlockLevel;
					break;

				case LoLaGame.UnlockMode.COMING_SOON:
					isUnlocked = false;
					break;

				case LoLaGame.UnlockMode.LEVEL_LOCK_EXPERIMENT:
					if (!ExperimentWrapper.LockedGamesOnInstall.isInExperiment)
					{
						isUnlocked = true;
					}
					else
					{
						isUnlocked = SlotsPlayer.instance.socialMember.experienceLevel >= unlockLevel;
					}
					break;
						
				case LoLaGame.UnlockMode.UNLOCK_BY_GOLD_PASS:
					if (!ExperimentWrapper.RichPass.isInExperiment)
					{
						isUnlocked = false;
					}
					else
					{
						isUnlocked = CampaignDirector.richPass != null && 
						             CampaignDirector.richPass.isActive &&
						             CampaignDirector.richPass.isPurchased();
					}
					break;
				
				case LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS:
					if (!ExperimentWrapper.RichPass.isInExperiment)
					{
						isUnlocked = false;
					}
					else
					{
						isUnlocked = CampaignDirector.richPass != null && 
						             CampaignDirector.richPass.isActive;
					}
					break;
			}
		}
	}

	public bool setUnlockForLobby(LobbyInfo.Type type)
	{
		bool didSetUnlock = false;
		string lobbyName = LoLaLobby.findKeyByLobbyInfo(type);

		if (!string.IsNullOrEmpty(lobbyName))
		{
			foreach (LoLaLobbyDisplay display in lolaGame.gameDisplays)
			{
				if (display.lobbyKey == lobbyName)
				{
					switch (display.unlockMode)
					{
						case LoLaGame.UnlockMode.SNEAK_PREVIEW:
						case LoLaGame.UnlockMode.UNLOCK_FOR_ALL:
							didSetUnlock = true;
							isUnlocked = true;
							break;

						case LoLaGame.UnlockMode.UNLOCK_BY_LEVEL:
							didSetUnlock = true;
							isUnlocked = SlotsPlayer.instance.socialMember.experienceLevel >= unlockLevel;
							break;

						case LoLaGame.UnlockMode.COMING_SOON:
							didSetUnlock = true;
							isUnlocked = false;
							break;

						case LoLaGame.UnlockMode.LEVEL_LOCK_EXPERIMENT:
							didSetUnlock = true;
							if (!ExperimentWrapper.LockedGamesOnInstall.isInExperiment)
							{
								isUnlocked = true;
							}
							else
							{
								isUnlocked = SlotsPlayer.instance.socialMember.experienceLevel >= unlockLevel;
							}
							break;
						
						case LoLaGame.UnlockMode.UNLOCK_BY_GOLD_PASS:
							didSetUnlock = true;
							if (!ExperimentWrapper.RichPass.isInExperiment)
							{
								isUnlocked = false;
							}
							else
							{
								isUnlocked = CampaignDirector.richPass != null && 
								             CampaignDirector.richPass.isActive &&
								             CampaignDirector.richPass.isPurchased();
							}
							break;
						
						case LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS:
							didSetUnlock = true;
							if (!ExperimentWrapper.RichPass.isInExperiment)
							{
								isUnlocked = false;
							}
							else
							{
								isUnlocked = CampaignDirector.richPass != null && 
								             CampaignDirector.richPass.isActive;
							}
							break;
					}
				}
			}
		}

		return didSetUnlock;
	}

	public bool isUnlockForAll()
	{
		if (lolaGame != null)
		{
			return lolaGame.unlockMode == LoLaGame.UnlockMode.UNLOCK_FOR_ALL;
		}

		return false;
	}
	
	// Call setSneakPreview() for all sneak preview games when the sneak preview time runs out.
	public static void expireSneakPreview()
	{
		foreach (LobbyGame game in all.Values)
		{
			if (game.isSneakPreview)
			{
				// Change the mode to the fallback.
				game.setSneakPreview();
			}
		}
	}
	
	// Handles the logic to set sneak preview mode or the fallback mode.
	// The initial LoLa-based unlockMode and sneakPreviewFallback values must be set before calling this.
	// Only gets called if using LoLa.
	public void setSneakPreview()
	{
		if (lolaGame != null)
		{
			// If a game isn't in a LoLa lobby, then this will be null. It's ok, it just won't show up in the game.
		
			if (lolaGame.unlockMode == LoLaGame.UnlockMode.SNEAK_PREVIEW)
			{
				// Make sure the game uses the special wager set for sneak preview mode.
				// This happens even if the game doesn't SHOW as sneak preview mode
				// due to fallback being unlock by level and being high enough level.
				// It's still in sneak preview mode even if the UI doesn't show it.
				if (sneakPreviewWagerSet != "")
				{
					SlotsWagerSets.addGameWagerSetEntry(keyName, sneakPreviewWagerSet);
					sneakPreviewWagerSet = "";	// Only need to set it once, so clear this to prevent setting it again later.
				}

				if (LoLa.sneakPreviewTimeRange == null || !LoLa.sneakPreviewTimeRange.isActive)
				{
					isSneakPreview = false;

					switch (lolaGame.fallbackMode)
					{
						case LoLaGame.UnlockMode.COMING_SOON:
							isComingSoon = true;
							lolaGame.unlockMode = LoLaGame.UnlockMode.COMING_SOON;
							break;
						
						case LoLaGame.UnlockMode.UNLOCK_BY_GOLD_PASS:
							lolaGame.unlockMode = LoLaGame.UnlockMode.UNLOCK_BY_GOLD_PASS;
							break;
						
						case LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS:
							lolaGame.unlockMode = LoLaGame.UnlockMode.UNLOCK_BY_SILVER_PASS;
							break;
					
						case LoLaGame.UnlockMode.UNLOCK_BY_LEVEL:
						case LoLaGame.UnlockMode.UNLOCK_FOR_ALL:	// This shouldn't happen, but making sure it's accounted for.
						case LoLaGame.UnlockMode.SNEAK_PREVIEW:		// This shouldn't happen, but making sure it's accounted for.
							lolaGame.unlockMode = LoLaGame.UnlockMode.UNLOCK_BY_LEVEL;
							break;
					}
				
				}
				else if (lolaGame.fallbackMode == LoLaGame.UnlockMode.COMING_SOON)
				{
					// When a game's fallback mode is "coming soon", then treat this game like
					// it's never been released before. Show is as "sneak preview" even if the
					// player's level is already high enough to unlock it.
					isSneakPreview = true;
				}
				else
				{
					// Only mark it sneak preview if the player doesn't have it unlocked anyway via levels.
					isSneakPreview = (unlockLevel > SlotsPlayer.instance.socialMember.experienceLevel);
				}
			}
			else
			{
				isSneakPreview = false;
			}
		}
		
		// Make sure the isUnlocked value is updated based on possible changes to isSneakPreview.
		setIsUnlocked();
	}
	
	// Is the game enabled at all?
	public bool isEnabledForLobby
	{
		get
		{
			if (isVIPEarlyAccess)
			{
				// The VIP early access game is always enabled if specified, even if it's not in a lobby.
				return true;
			}

			if (LoLaLobby.main == null)
			{
				// This shouldn't happen.
				return false;
			}
			if (LoLaLobby.vip == null)
			{
				// This shouldn't happen.
				return false;
			}
			
			LoLaLobbyDisplay vipDisplay = LoLaLobby.vip.findGame(keyName);			
			LoLaLobbyDisplay display = LoLaLobby.main.findGame(keyName);
			LoLaLobbyDisplay eosDisplay = LoLaLobby.findGameInEOS(keyName);

			if (display == null && vipDisplay == null && eosDisplay == null)
			{
				//Debug.LogWarningFormat("LobbyGame.cs -- isEnabledForLobby -- lola lobby display not found in a valid lobby for game: {0}", keyName);
				return false;
			}
			
			return true;
		}
	}

	public LobbyGame(LobbyGameGroup group, bool isGroupLicensed, JSON gameJson)
	{
		// Don't assign GameExperience object here because it has to be processed from player data first.                
		keyName = gameJson.getString("key_name", "");
		name = gameJson.getString("name", "");
		
		if (!Data.debugMode)
		{
			SlotResourceData mapData = SlotResourceMap.getData(keyName);
			if (mapData == null || mapData.gameStatus == SlotResourceData.GameStatus.NON_PRODUCTION_READY)
			{
#if !UNITY_EDITOR
				// Only do this for actual builds, since we need this data to be able
				// to test slots that are work in progress, and can't upgrade from the editor.
				if (Glb.isUpdateAvailable)
				{
					isUpdateRequired = true;
				}
				else
#endif
				{
					// If no upgrade is available, then we shouldn't be showing this game in the lobby at all.
					return;
				}
			}
		}
	#if RWR
		int anIndex = System.Array.IndexOf(rwrSweepstakesGames, keyName);
		isRWRSweepstakes = (anIndex != -1) && (keyName == rwrSweepstakesGames[anIndex]);
		rwrSweepstakesMeterMax = gameJson.getLong("rwr_meter_max", 0L);
	#endif
		isActive = (group.isActive && gameJson.getBool("is_active", false));
		assetKey = gameJson.getString("asset_key", "");
		lobbyColor = CommonColor.colorFromHex(gameJson.getString("lobby_color", "92e0ff"));
		string customType = gameJson.getString("custom_machine_type", "");	// fucking "machine" instead of "game".
		isDeluxe = (customType == "deluxe");
		isHighLimit = (customType == "high_limit");
		
		if (isDeluxe)
		{
			deluxeGames.Add(this);
		}
		
		groupInfo = group;  // Allows us to see what group this game is in.

		// Whether this game can be unlocked via a game unlocking feature.
#if UNITY_IPHONE
		_canBeUnlocked = gameJson.getBool("can_redeem_game_unlock_ios", false);
#elif ZYNGA_KINDLE
		_canBeUnlocked = gameJson.getBool("can_redeem_game_unlock_kindle", false);
#elif UNITY_ANDROID
		_canBeUnlocked = gameJson.getBool("can_redeem_game_unlock_android", false);
#elif UNITY_WEBGL
		_canBeUnlocked = gameJson.getBool("can_redeem_game_unlock_unityweb", false);
#elif UNITY_WSA_10_0 && NETFX_CORE //SMP this should probably be WSA specific
		_canBeUnlocked = gameJson.getBool("can_redeem_game_unlock_windows", false);
#endif

		license = gameJson.getString("license", "");

		// check if we have a license_new block, in which case we will override all the license data with that
		JSON licenseNewJSON = gameJson.getJSON("license_new");
		if (licenseNewJSON != null)
		{
			// make sure that a license key is still setup for license_new
			// since we need that for the default settings for this given license
			string newLicenseKey = licenseNewJSON.getString("key_name", "");
			if (newLicenseKey != "")
			{
				license = newLicenseKey;
			}
			else
			{
				Debug.LogWarning("LobbyGame.LobbyGame() - name = " + name + "; contained a license_new section with no key_name for the default license, falling back to just using license.");
			}

			// check for overrides for license data that is part of license_new
			paytableTitleOverride = licenseNewJSON.getString("paytable_title", "");
			paytableDescOverride = licenseNewJSON.getString("paytable_description", "");
			paytableImageOverride = licenseNewJSON.getString("paytable_image_path", "");
			// remove everything from the image path but the image name
			int lastSlashIndexInImagePath = paytableImageOverride.LastIndexOf("/");
			if (lastSlashIndexInImagePath != -1)
			{
				paytableImageOverride = paytableImageOverride.Substring(lastSlashIndexInImagePath + 1);
			}
		}

		if (license == "")
		{
			// If the game has no license specified but the group does,
			// set the game's license to the group's license.
			license = group.license;
		}

		// populate wager set info
		string wagerSet = gameJson.getString("wager_set", ""); 
		if (wagerSet != "")
		{
			SlotsWagerSets.addGameWagerSetEntry(keyName, wagerSet);
		}

		sneakPreviewWagerSet = gameJson.getString("sneak_preview_wager_set", ""); 

		foreach (JSON infoJson in gameJson.getJsonArray("info"))
		{
			info.Add(new Info(infoJson.getInt("sort_index", 0), infoJson.getString("info", "")));
		}
		info.Sort(Info.sortByOrder);
		
		if (all.ContainsKey(keyName))
		{
			Debug.LogWarning("Overwriting LobbyGame data");
			all[keyName] = this;
		}
		else
		{
			all.Add(keyName, this);
		}
	}
	
	// Used by the Sort() method to sort by the mystery gift's order,
	// which is the order in which the 1X2 mystery gift games appear in the lobby.
	// This is only called by games that have been added to the pinnedMysteryGiftGames list.
	public static int sortMysteryGift(LobbyGame a, LobbyGame b)
	{
		if (LobbyGame.skuGameUnlock == a)
		{
			Debug.LogWarning("Sorting sku game unlock " + a.keyName + " before " + b.keyName);
			return -1;
		}
		else if (LobbyGame.skuGameUnlock == b)
		{
			Debug.LogWarning("Sorting " + a.keyName + " after sku game unlock " + b.keyName);
			return 1;
		}
		
		// If a mystery gift game is the VIP early access game, then sort it to the end of the list
		// so it doesn't mess up the sort order of all the other games.
		if (LobbyGame.vipEarlyAccessGame == a)
		{
			return 1;
		}
		if (LobbyGame.vipEarlyAccessGame == b)
		{
			return -1;
		}

		if (a.mysteryGift1X2SortOrder == b.mysteryGift1X2SortOrder)
		{
			// If sort order values are the same, then sort by key name next.
			return a.keyName.CompareTo(b.keyName);
		}
		
		return a.mysteryGift1X2SortOrder.CompareTo(b.mysteryGift1X2SortOrder);
	}

// Called after populating the SlotResourceMap, remove any games that aren't defined in SlotResourceMap.
	// Also remove games that aren't enabled by experiments.
	public static void removeUnknownMysteryGiftGames()
	{
		for (int i = 0; i < pinnedMysteryGiftGames.Count; i++)
		{
			LobbyGame game = pinnedMysteryGiftGames[i];
			
			bool isNotBigSlice = (game.mysteryGiftType == MysteryGiftType.BIG_SLICE && !Data.liveData.getBool("FEATURE_BIG_SLICE_ENABLED", false));

			if (isNotBigSlice)
			{
				// If not in the Mystery Gifts experiment, make sure the games aren't flagged as mystery gifts.
				// This must be done here instead of when populating LobbyGame data because
				// we need to make sure experiment data has been populated first.
				game.mysteryGiftType = MysteryGiftType.NONE;
			}
			
			if (game == skuGameUnlock ||
				isNotBigSlice ||
				SlotResourceMap.getData(game.keyName) == null ||
				!game.isEnabledForLobby
				)
			{
				// Log the reason for removing the game from the list.
				// if (game.hasFeatureForLobby(LoLaGame.Feature.FEATURE))
				// {
				// 	Debug.LogWarning("Removing " + game.keyName + " from pinnedMysteryGiftGames since it is the free preview game, and will appear in the first slot due to that.");
				// }
				// else if (game == skuGameUnlock)
				// {
				// 	Debug.LogWarning("Removing " + game.keyName + " from pinnedMysteryGiftGames since it is the sku game unlock, and will appear in the first slot due to that.");
				// }
				// else if (SlotResourceMap.getData(game.keyName) == null)
				// {
				// 	Debug.LogWarning("Removing " + game.keyName + " from pinnedMysteryGiftGames since it doesn't exist in SlotResourceMap.");
				// }
				// else if (!game.isEnabledForLobby)
				// {
				// 	Debug.LogWarning("Removing " + game.keyName + " from pinnedMysteryGiftGames since it isn't experiment enabled.");
				// }
				// else if (isNotMysteryGift)
				// {
				// 	Debug.LogWarning("Removing " + game.keyName + " from pinnedMysteryGiftGames since it is a mystery gift or big slice, yet the player isn't in the associated experiment.");
				// }
				
				pinnedMysteryGiftGames.RemoveAt(i);
				i--;
			}
		}
			
		// Now is a good time to sort them.
		// Sorting puts the free preview game first, and VIP early access game last,
		// so the page numbers of the rest of the progressive games are correct.
		pinnedMysteryGiftGames.Sort(sortMysteryGift);
	}	

	// Returns the list of all games for iterating.
	public static Dictionary<string, LobbyGame>.ValueCollection getAll()
	{
		return all.Values;
	}

	// Add an unlock level to the dictionary.
	public void addUnlockLevel(int variant, int level)
	{
		if (variantBasedUnlockLevels.ContainsKey(variant))
		{
			// This is reliably erroring every app load 4 times, without breaking the app.
			// So let's not make this a LogError since that would spam splunk on production.
			Debug.LogWarning("Trying to add unlock level " + level + " to variant " + variant + " for " + keyName + " that already exists.");
		}
		else
		{
			variantBasedUnlockLevels.Add(variant, level);
		}
	}
	
	public static LobbyGame find(string keyName)
	{
		LobbyGame game;
		if (all.TryGetValue(keyName, out game))
		{
			return game;
		}
//		Debug.LogWarning("Could not find LobbyGame " + keyName);
		return null;
	}
	
	/// Late-bind the GameExperience object, because this has to be done
	/// after the player data has been processed.
	public static void bindGameExperience()
	{
		foreach (KeyValuePair<string, LobbyGame> kvp in all)
		{
			LobbyGame game = kvp.Value;
			
			game.xp = GameExperience.findOrCreate(game);

			if (game.xp.isSkuGameUnlock)
			{
				if (skuGameUnlock != null)
				{
					Debug.LogError("SKU Game Unlock is already " + skuGameUnlock.keyName);
					Debug.LogError("Hiding other SKU Game Unlock " + game.keyName);
					
					game.xp.isVisible = false;
				}
				else
				{
					skuGameUnlock = game;
				}
			}
		}
	}
	
	// Unlocks all games for the given level.
	public static void unlockGamesForLevel(int level, bool isDevLevelUp = false)
	{
		List<string> unlocked = GameUnlockData.findUnlockedGamesForLevel(level);
		
		if (unlocked == null)
		{
			// No games unlocked at this level.
			return;
		}

		// If level up user experience is running we'll just handle all the UI in the toaster
		// and unlock things here and now
		if (LevelUpUserExperienceFeature.instance.isEnabled &&  unlocked.Count > 0)
		{
			LobbyGame game;
			foreach (string gameKey in unlocked)
			{
				game = find(gameKey);
				if (game != null)
				{
					game.setIsUnlocked();
				}
			}
		}
		else
		{
			foreach (string gameKey in unlocked)
			{
				// It's possible to have games that aren't in the lobby,
				// so only unlock if the game is actually in the current lobby.

				LobbyGame game = LobbyGame.find(gameKey);
				if (game != null && LobbyOption.activeGameOption(game) != null)
				{
					// Don't show sneak preview games as being unlocked, since they are
					// already currently unlocked, possibly temporarily.
					if (!isDevLevelUp && !game.isSneakPreview && !game.isComingSoon && !game.isUnlocked && !game.isChallengeLobbyGame)
					{
						GameUnlockedDialog.showDialog(game, getNextUnlocked(level));
					}
				
					// Refresh the sneak preview status, which also refreshes the unlocked status.
					// Must be done after the logic above that checks game.isSneakPreview for its old status.
					game.setSneakPreview();
				}
			}
		}
	}

	// Look for the next unlocked game after the given level.
	public static LobbyGame getNextUnlocked(int level)
	{
		LobbyOption nextOption = null;

		for (int nextLevel = level + 1; nextLevel <= GameUnlockData.maxUnlockLevel; nextLevel++)
		{
			List<string> nextUnlock = GameUnlockData.findUnlockedGamesForLevel(nextLevel);

			if (nextUnlock != null)
			{
				foreach (string nextKey in nextUnlock)
				{
					LobbyGame game = LobbyGame.find(nextKey);
					if (game != null && 
						(!game.isUnlocked ||
						!game.isSneakPreview ||
						!game.isComingSoon))
					{
						nextOption = LobbyOption.activeGameOption(game);
					}
					if (nextOption != null)
					{
						// Even though there might be more than one game unlocked at a level,
						// we only need the first one since there's only room for one on the dialog.
						break;
					}
				}
			
				if (nextOption != null)
				{
					return nextOption.game;
				}
			}
		}
	
		return null;
	}
		
	// Registers the list of labels to be updated with ticker values for a multiprogressive game.
	// Sometime the labels are defined in reverse order, depending on the UI.
	public void registerMultiProgressiveLabels(UILabel[] labels, bool isReverseOrder)
	{
		if (labels.Length != 3)
		{
			Debug.LogError("registerMultiProgressiveLabels: Did not pass in exactly 3 labels to register.");
			return;
		}

		if (!isMultiProgressive)
		{
			Debug.LogError("registerMultiProgressiveLabels: Tried to register labels for a game that isn't multi-progressive.");
			return;
		}
		
		for (int i = 0; i < 3; i++)
		{
			int labelIndex = i;
			
			if (isReverseOrder)
			{
				labelIndex = 2 - i;
			}
			
			progressiveJackpots[i].registerLabel(labels[labelIndex]);
		}
	}

	// Registers the list of labels to be updated with ticker values for a multiprogressive game.
	// Sometime the labels are defined in reverse order, depending on the UI.
	public void registerMultiProgressiveLabels(TextMeshPro[] labels, bool isReverseOrder)
	{
		if (labels == null)
		{
			Debug.LogErrorFormat("LobbyGame.cs -- registerMultiProgressiveLabels() -- passed in a null labels array, so nothing to register for game: {0}", keyName);
			return;
		}

		if (!isMultiProgressive)
		{
			// This check also checks if the progressiveJackpots array is null.
			Debug.LogErrorFormat("registerMultiProgressiveLabels: Tried to register TextMeshPro labels for a game that isn't multi-progressive for game: {0}", keyName);
			return;
		}

		if (progressiveJackpots == null)
		{
			Debug.LogErrorFormat("LobbyGame.cs -- registerMultiProgressiveLabels() -- progressive jackpots is null even though isMultiProgressive is true, this is bad and should never happen, for game: {0}", keyName);
			return;
		}
		if (labels.Length != 3)
		{
			Debug.LogErrorFormat("registerMultiProgressiveLabels: Did not pass in exactly 3 TextMeshPro labels to register for game: {0}.", keyName);
			return;
		}

		if (labels.Length != progressiveJackpots.Count)
		{
			Debug.LogErrorFormat("LobbyGame.cs -- registerMultiProgressiveLabels() -- the number of TMPro labels {0} did not match the number of progressive Jackpots: {1} for game: {2}",
				labels.Length,
				progressiveJackpots.Count,
				keyName);
			return;
		}
		
		for (int i = 0; i < 3; i++)
		{
			int labelIndex = i;
			
			if (isReverseOrder)
			{
				labelIndex = 2 - i;
			}

			if (progressiveJackpots[i] != null)
			{
				progressiveJackpots[i].registerLabel(labels[labelIndex]);
			}
			else
			{
				Debug.LogErrorFormat("LobbyGame.cs -- registerMultiProgressiveLabels() -- trying to register a label on a null progressive jackpot for game: {0}", keyName);
			}

		}
	}

#if RWR
	// RWR Sweepstakes
	
	public bool hasRWRSweepstakesTicket()
	{
		return isRWRSweepstakes && (xp.rwrSweepstakesMeterCount == rwrSweepstakesMeterMax);
	}

	public void addToRWRSweepstakesCount(long amount)
	{
		xp.rwrSweepstakesMeterCount += amount;
		
		if (xp.rwrSweepstakesMeterCount > rwrSweepstakesMeterMax)
		{
			xp.rwrSweepstakesMeterCount = rwrSweepstakesMeterMax;
		}
	}
#endif

	public static bool checkSkuGameUnlock(bool shouldIgnoreLevel = false)
	{
		if (SlotsPlayer.isLoggedIn && skuGameUnlock != null)
		{
			bool isLevelRequirementMet = (SlotsPlayer.instance.socialMember.experienceLevel >= SKU_GAME_MINIMUM_UNLOCK_LEVEL || shouldIgnoreLevel);
			
			if (isLevelRequirementMet && AppsManager.isBundleIdInstalled(AppsManager.WOZ_SLOTS_ID))		    
			{
				AppAction.appInstalled(skuGameUnlock.xp.xpromoTarget);
				return true;
			}   
		}
		return false;
	}

	// Returns the minimum wager to qualify for progressive or mystery gift winnings.
	public long specialGameMinQualifyingAmount
	{
		get
		{
			bool shouldShowBigSlice = (mysteryGiftType == MysteryGiftType.BIG_SLICE);

			long vipProgressiveMinBet = SlotsWagerSets.getVipProgressiveMinBetForGameWagerSet(keyName);

			if (eosControlledLobby != null)
			{
				if (eosControlledLobby == LoLaLobby.maxVoltage)
				{
					return SlotsWagerSets.getMaxVoltageMinBet();
				}

				if (eosControlledLobby == LoLaLobby.vipRevamp)
				{
					return SlotsWagerSets.getVIPRevampMinBet();
				}
			}

			if (vipProgressiveMinBet != 0 && (isVIPGame || (isEarlyAccessGame && Glb.IS_USING_VIP_EARLY_ACCESS_WAGER_SETS_VIP_MIN_WAGER)))
			{
				// this is a vip or the early access game (where we are using VIP progressive value for early access)
				// AND vipProgressiveMinBet is defined for this game
				return vipProgressiveMinBet;
			}
			else if (isProgressive)
			{
				if (isMultiProgressive)
				{
					return SlotsWagerSets.getMultiProgressiveMinBetForGameWagerSet(keyName);
				}
				else
				{
					return SlotsWagerSets.getProgressiveJackpotMinBetForGameWagerSet(keyName);
				}
			}
			else if (mysteryGiftType != MysteryGiftType.NONE)
			{
				if (shouldShowBigSlice)
				{
					return SlotsWagerSets.getBigSliceMinBetForGameWagerSet(keyName);
				}
				else
				{
					return SlotsWagerSets.getMysteryGiftMinBetForGameWagerSet(keyName);
				}
			}
			else
			{
				Debug.LogWarning("Didn't find a suitable condition for determining specialGameMinQualifyingAmount. Using 0.");
				return 0L;
			}
		}
	}

	// First check if an initial bet dialog is needed, and show it if so.
	// Otherwise just try launching the game.
	public LaunchResult askInitialBetOrTryLaunch(bool logStats = true, bool forceRoyalRushLaunch = false)
	{
		//If the SmartBetSelector is enabled just use that
		//else directly take the player into the game
		if (isUnlocked &&
			((isProgressive && !isBuiltInProgressive) || mysteryGiftType != MysteryGiftType.NONE) &&
			(!ExperimentWrapper.VIPLobbyRevamp.isInExperiment || LoLaLobby.vipRevamp == null || eosControlledLobby != LoLaLobby.vipRevamp) && //Don't ask for the initial bet if the we're in the revamp experiment and the game is in the revamp lobby
			ExperimentWrapper.SmartBetSelector.isInExperiment)
		{
			progressiveMysteryBetAmount = 0;

			Dict args = Dict.create(D.CALLBACK, new DialogBase.AnswerDelegate(progressiveMysteryBetCallback),
									D.GAME_KEY, keyName,
									D.SHOW_CLOSE_BUTTON, true,
									D.OPTION, logStats,
									D.FEATURE_TYPE, mysteryGiftType.ToString()
									);

			SmartBetSelector.showDialog(args);
			return LaunchResult.ASK_INITIAL_BET;
		}
		
		if (isRoyalRush)
		{
			RoyalRushInfo info = RoyalRushEvent.instance.getInfoByKey(keyName);

			// In case we launch from a 1x1
			if (SlotsPlayer.instance.socialMember.experienceLevel < RoyalRushEvent.minLevel || info == null)
			{
				return LaunchResult.NO_LAUNCH;
			}

			if (info.currentState == RoyalRushInfo.STATE.AVAILABLE && info.inWithinRegistrationTime())
			{
				info.onRushRegister -= royalRushRegisterCallback;
				info.onRushRegister += royalRushRegisterCallback;
				info.registerForRush();

				return LaunchResult.LAUNCHED;
			}
			else if (info.currentState != RoyalRushInfo.STATE.AVAILABLE && info.userInfos != null && info.userInfos.Count > 0)
			{
				Dict rushDict = Dict.create(D.DATA, info);

				StatsManager.Instance.LogCount("dialog", "royal_rush_standings", klass: "main_lobby", genus: "view");

				if (forceRoyalRushLaunch)
				{
					tryLaunch(logStats);
				}
				else
				{
					RoyalRushStandingsDialog.showDialog(rushDict);
				}
				return LaunchResult.ASK_INITIAL_BET;
			}
			return LaunchResult.NO_LAUNCH;
		}
		
		if(keyName == PersonalizedContentLobbyOptionDecorator1x2.gameKey)
		{
			// If this is your personalized game, just let them in.
			launch(true);
			return LaunchResult.LAUNCHED;
		}
		
		// normal game so just launch it
		return tryLaunch(logStats);
	}

	private void royalRushRegisterCallback(Dict args = null)
	{
		tryLaunch(false);
		NGUIExt.enableAllMouseInput();
	}

	// Callback for ProgressiveSelectBetDialog dialog.
	private void progressiveMysteryBetCallback(Dict args)
	{
		// check if the user selected something
		if (args.containsKey(D.ANSWER))
		{
			progressiveMysteryBetAmount = (long)args.getWithDefault(D.ANSWER, 0L);

			// now that the bet amount is set, try and launch the game
			tryLaunch((bool)args.getWithDefault(D.OPTION, false));
		}
	}

	private void addJackpotOverlay()
	{
		if (Overlay.instance != null)
		{
			Overlay.instance.addJackpotOverlay();
		}
	}

	// Try to launch the game, enforcing locked status. Returns whether it actually did launch.
	private LaunchResult tryLaunch(bool logStats)
	{
		LaunchResult result = LaunchResult.NO_LAUNCH;
		
		// If in the VIP experiment and the game is a VIP game,
		// then override the level locked status since VIP games aren't XP level locked.
		if (isUpdateRequired)
		{
			// TODO: Localize the messages in this dialog.
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, "Update Required",
					D.MESSAGE, "You need to update the app in order to play this slot.",
					D.OPTION1, Localize.textUpper("update"),
					D.OPTION2, Localize.textUpper("not_now"),
					D.REASON, "lobby-game-update-required",
					D.CALLBACK, new DialogBase.AnswerDelegate(updateCallback)
				)
			);
		}
		else if (xp.isSkuGameUnlock)
		{
			if (!checkSkuGameUnlock(true))
			{
				MOTDFramework.showMOTD(xp.xpromoTarget + "_game_unlock_" + keyName);
			}
		}
		else if (isUnlocked)
		{
			if (EUEManager.isEnabled && !EUEManager.shouldDisplayChallengeIntro) //eue campaign is active and user has seen the challenge intro
			{
				StatsManager.Instance.LogCount("game_actions", "select_game", "", keyName, "", "click");
			}
			result = launch(logStats);	// Even though this is the only place this function is called, still a separate function to simplify this function.
		}
		else if (lolaGame != null && lolaGame.unlockMode == LoLaGame.UnlockMode.UNLOCK_BY_GOLD_PASS &&
		         (CampaignDirector.richPass == null || !CampaignDirector.richPass.isActive ||
		          !CampaignDirector.richPass.isPurchased()))
		{
			RichPassUpgradeToGoldDialog.showDialog("lobby_game", SchedulerPriority.PriorityType.IMMEDIATE);
		}
		else
		{
			LockedGameDialog.showDialog(this);
		}
		
		return result;
	}
	
	// Callback for update suggestion dialog.
	private void updateCallback(Dict args)
	{
		if ((string)args.getWithDefault(D.ANSWER, "") == "1")
		{
			Application.OpenURL(Glb.clientAppstoreURL);
		}
	}

	// Tries to launch this game without restrictions. Returns whether it actually did launch.
	// The only time it should fail is if the game isn't found in the SlotResourceMap.
	private LaunchResult launch(bool logStats)
	{
		LaunchResult result = LaunchResult.NO_LAUNCH;

		if (!isActive)
		{
			return result;
		}	
		
		// If the group for the game has a click sound, play it now.
		Audio.play(groupInfo.clickSound);	
		Audio.play("initialbet0");
		
		// Temp code for spring-boarding a slot game
		
		Bugsnag.LeaveBreadcrumb("Attempting to load up a game with a keyname of " + keyName);
		
		if (SlotResourceMap.hasEntry(keyName))
		{
			// Disable all game buttons to make sure a second click isn't registered.
			NGUIExt.disableAllMouseInput();
			GameState.clearGameStack();
			GameState.pushGame(this);
			Loading.show(Loading.LoadingTransactionTarget.GAME);
			Overlay.instance.top.showLobbyButton();
			
			Overlay.instance.topHIR.hideMaxVoltageWinner();
			Overlay.instance.topV2.showWeeklyRaceButton();
			
			if (logStats)
			{
				StatsManager.Instance.LogCount("timing", "LobbyTime", "", "", "", "", StatsManager.getTime(false));

				// This will wait till we get the slot data so we don't send up the hacky ztrack key.
				RoutineRunner.instance.StartCoroutine(waitForSlotData());
			}
			
			PlayerPrefsCache.SetString(Prefs.LAST_SLOT_GAME, keyName);
			PlayerPrefsCache.Save();
			result = LaunchResult.LAUNCHED;

			// NON_PRODUCTION_READY are usually not in LOLA so EnterGameAction.gameLaunched would get an NRE
			if (SlotResourceMap.getData(keyName).gameStatus != SlotResourceData.GameStatus.NON_PRODUCTION_READY)
			{
				EnterGameAction.gameLaunched(keyName);
			}

			Glb.loadGame();
		}
		else
		{
			// This should never happen.
			Debug.LogError("Tried to launch a game that has no entry in SlotResourceMap: " + keyName);
			GenericDialog.showDialog(
				Dict.create(
					D.TITLE, "UNDER CONSTRUCTION",
					D.MESSAGE, name + " is not ready to be played yet.",
					D.REASON, "lobby-game-missing-slot-resource-map-entry"
				)
			);
		}
		
		return result;
	}

	private IEnumerator waitForSlotData()
	{
		string zTrackString = "";
		SlotGameData data = SlotGameData.find(keyName);

		int secondsWasted = 0;
		while (data == null && secondsWasted < 30)
		{
			secondsWasted++;
			yield return new WaitForSeconds(1.0f);
			data = SlotGameData.find(keyName);
		}

		zTrackString = data != null ? data.zTrackString : StatsManager.getGameName(keyName);

		if (isRecomended)
		{
			zTrackString = "personalized_content";
		}

		string milestone = LobbyInfo.currentTypeToString;
		if (MainLobby.instance != null)
		{
			milestone += "_" + MainLobby.instance.getTrackedScrollPosition().ToString();
		}

		StatsManager.Instance.LogCount
		(
			counterName: "lobby",
			kingdom: "select_game",
			phylum: keyName,
			klass: zTrackString,
			family: lolaGame != null ? lolaGame.feature.ToString() : "",
			genus: "click",
			val: 0,
			milestone: milestone
		);

		isRecomended = false;

		yield return null;
	}

	/// Tells if a game is a VIP game
	public bool isVIPGame
	{
		get
		{
			return vipLevel != null;
		}
	}

	/// Tells if this game is the early access game
	public bool isEarlyAccessGame
	{
		get
		{
			return (this == vipEarlyAccessGame);
		}
	}

	// Tells if this game was the most recent early access game.
	public bool isRecentEarlyAccessGame
	{
		get
		{
			return (keyName == PlayerPrefsCache.GetString(Prefs.EARLY_ACCESS_RECENT, ""));
		}
	}

	public bool isLOZGame
	{
		get
		{
			return LoLaLobby.loz != null && eosControlledLobby == LoLaLobby.loz;
		}
	}

	public bool isSlotventure
	{
		get
		{
			ChallengeLobbyCampaign slotventureCampaign =
				CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as ChallengeLobbyCampaign;
			if (slotventureCampaign != null && slotventureCampaign.currentMission.containsGame(GameState.game.keyName))
			{
				return true;
			}

			return false;
		}
	}

	public bool isRobustCampaign
	{
		get
		{
			RobustCampaign campaign = CampaignDirector.find(CampaignDirector.ROBUST_CHALLENGES) as RobustCampaign;
			if (campaign != null && 
			    // Either we found a matched game in current mission
			    campaign.currentMission != null &&
			    (campaign.currentMission.containsGame(keyName) || 
			     // or the current mission works with any games
			     (campaign.currentMission.gameObjectives != null && campaign.currentMission.gameObjectives.IsEmpty()) || 
			      campaign.currentMission.hasIncompleteObjectiveWithoutAGame()))
			{
				return true;
			}

			return false;
		}
	}

	public bool isMaxVoltageGame
	{
		get
		{
			return LoLaLobby.maxVoltage != null && eosControlledLobby == LoLaLobby.maxVoltage;
		}
	}

	public bool isChallengeLobbyGame
	{
		get
		{
			//LoLaLobby lobby = LoLaLobby.findEOSWithGame(keyName);
			return CampaignDirector.findWithGame(keyName) is ChallengeLobbyCampaign;
		}
	}
	
	/// <summary>
	/// Gets a value indicating whether this <see cref="LobbyGame"/> is EOS controlled.
	/// </summary>
	/// <value><c>true</c> if game is in land of oz, max voltage, vip room, or a challenge lobby (e.g. sin city); otherwise, <c>false</c>.</value>
	public bool isEOSControlled
	{
		get
		{
			return isLOZGame || isMaxVoltageGame || isChallengeLobbyGame || isVIPGame;
		}
	}

	// Queue the action to start a game.
	public static void queueGame(string gameKey)
	{
		Dict args = Dict.create(D.DATA, gameKey);
		Scheduler.addFunction(startGameFromScheduler, args);
	}

	public static void startGameFromScheduler(Dict args)
	{
		Scheduler.removeFunction(startGameFromScheduler);
		string gameKey = (string)args[D.DATA];
		LobbyGame game = LobbyGame.find(gameKey);
		if (game != null)
		{
			game.askInitialBetOrTryLaunch();
		}
		else
		{
			Debug.LogError("Game " + gameKey + " is invalid.");
		}
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		all = new Dictionary<string, LobbyGame>();
		pinnedMysteryGiftGames = new List<LobbyGame>();
		deluxeGames = new List<LobbyGame>();
		vipEarlyAccessGame = null;
		skuGameUnlock = null;
		sirGames = null;
		doubleFreeSpinGames = new List<LobbyGame>();
		bigSliceGame = null;
	}
	
	/// A simple structure to hold game info so it can be sorted.
	public class Info
	{
		public int sortOrder;
		public string info;
		
		public Info(int sortOrder, string info)
		{
			this.sortOrder = sortOrder;
			this.info = info;
		}
		
		/// Function used for sorting.
		public static int sortByOrder(Info a, Info b)
		{
			return a.sortOrder - b.sortOrder;
		}
	}
}
