using UnityEngine;
using System;
using System.Collections.Generic;
using Com.Rewardables;

/**
Original Author: Scott Lepthien
This is a class that tries to combine our 3 types of bonus game outcomes into a single type which can be used by our new modular bonus game system
*/
public class ModularChallengeGameOutcome  
{
	public enum ModularChallengeGameOutcomeTypeEnum
	{
		UNDEFINED = -1,
		PICKEM_OUTCOME_TYPE = 0,
		WHEEL_OUTCOME_TYPE = 1,
		NEW_BASE_BONUS_OUTCOME_TYPE = 2
	}

	public string outcomeType = ""; // Stored info about the outcome type name that was used to build this outcome
	public int outcomeIndex; // Numerical index for current outcome (when sequencing or alternating different bonus types)
	public long progressiveJackpotCredits; // progressive jackpot win used by ProgressiveJackpotsChallengeGameModule
	public bool outcomeContainsPersonalJackpotOutcome; // Easy way to check if there is a personal jackpot win so it doesn't conflict with normal credit collecting modules

	private string payTableName; // Get the pay table name from the slot outcome.
	private string eventID; // The eventID from the outcome used to create this ModularChallengeGameOutcome, may be needed when sending certain bonus actions to the server
	private PickemOutcome pickemOutcome = null;	// Original picking game outcome, only supports a single round
	private WheelOutcome wheelOutcome = null;	// Second type of outcome, supports wheels and multi round games
	private NewBaseBonusGameOutcome newBaseBonusGameOutcome = null;	// New outcome type that should be used for all new games, unifies ideas of PickemOutcome and WheelOutcome
	private List<ModularChallengeGameOutcomeRound> rounds = new List<ModularChallengeGameOutcomeRound>();	// Basic generic representation of the rounds for a game, hopefully most things can use this, and only use the outcomes directly if they really need something only those contain

	public class PickGroupInfo // Representation for pick item group mappings within an outcome paytable.
	{
		public long baseCredits;
		public long credits;
		public int hitsNeeded;
		public bool gameOver;
	}
	private Dictionary<string, PickGroupInfo> pickGroups; // mappings from group name to winning code values

	public class PickPoolInfo // Representation for pick pools from paytable
	{
		public int sortIndex;	// index of multiplier result within a horizontal tier
		public int horizontalSortIndex;	// horizontal tier representing "all value" increases
		public int multiplier;
	}

	private Dictionary<string, Dictionary<int, List<PickPoolInfo>>> pickPools; // mappings for pool name to pool list collections

	public class PickPersonalJackpotInfo
	{
		public string jackpotKey;
		public long credits;
	}

	public Dictionary<string, PickPersonalJackpotInfo> pickPersonalJackpots; // mapping for jackpot key to jackpot payouts

	public ModularChallengeGameOutcome(SlotOutcome outcome, bool forceOutcomeJson = false, long baseCredits = 0)
	{
		initJackpot(outcome);
		payTableName = outcome.getBonusGamePayTableName();
		eventID = outcome.getBonusGameCreditChoiceEventID();
		ModularChallengeGameOutcomeTypeEnum outcomeType = BonusGamePaytable.getPaytableOutcomeType(outcome.getBonusGamePayTableName());
		createOutcome(outcome, outcomeType, forceOutcomeJson, baseCredits);
	}

	private void initJackpot(SlotOutcome outcome)
	{
		initPersonalJackpotWinnings(outcome);
		progressiveJackpotCredits = outcome.getProgressiveJackpotCredits();
	}

	private void createOutcome(SlotOutcome outcome, ModularChallengeGameOutcomeTypeEnum outcomeType, bool forceOutcomeJson = false, long baseCredits = 0)
	{
		switch (outcomeType)
		{
			case ModularChallengeGameOutcomeTypeEnum.PICKEM_OUTCOME_TYPE:
				PickemOutcome pickem = new PickemOutcome(outcome);
				setPickemOutcome(pickem);
				break;
				
			case ModularChallengeGameOutcomeTypeEnum.WHEEL_OUTCOME_TYPE:
				WheelOutcome wheel = new WheelOutcome(outcome);
				setWheelOutcome(wheel);
				break;

			case ModularChallengeGameOutcomeTypeEnum.NEW_BASE_BONUS_OUTCOME_TYPE:
				NewBaseBonusGameOutcome newBaseBonus = new NewBaseBonusGameOutcome(outcome, false, forceOutcomeJson, baseCredits);
				setNewBaseBonusGameOutcome(newBaseBonus);
				break;

			case ModularChallengeGameOutcomeTypeEnum.UNDEFINED:
				Debug.LogError("ModularChallengeGameOutcome() - Trying to setup outcome with UNDEFINED type, make sure you set the type!");
				break;
		}
	}

	// Convert PickemOutcome to have entries/reveals in ModularChallengeGameOutcome rounds
	public ModularChallengeGameOutcome(PickemOutcome outcome)
	{
		setPickemOutcome(outcome);
	}

	// Convert Wheel Outcome to have entries/reveals in ModularChallengeGameOutcome rounds
	public ModularChallengeGameOutcome(WheelOutcome outcome)
	{
		setWheelOutcome(outcome);
	}

	// Convert NewBaseBonusGameOutcome to have entries/reveals in ModularChallengeGameOutcome rounds
	public ModularChallengeGameOutcome(NewBaseBonusGameOutcome outcome)
	{
		setNewBaseBonusGameOutcome(outcome);
	}

	public string getPayTableName()
	{
		return payTableName;
	}

	public string getEventID()
	{
		return eventID;
	}

	// Populate the winning group configuration from the paytable values.
	private void initWinningGroups()
	{
		pickGroups = new Dictionary<string, PickGroupInfo>();

		// get the base bonus table for mapping the win groups
		JSON baseBonusTable = BonusGamePaytable.findPaytable("base_bonus", payTableName);
		
		if (baseBonusTable != null)
		{
			foreach(JSON group in baseBonusTable.getJsonArray("groups"))
			{
				PickGroupInfo winGroup = new PickGroupInfo();
				string keyName = group.getString("key_name", null);
				winGroup.baseCredits = group.getLong("credits", 0);
				winGroup.credits = winGroup.baseCredits * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
				winGroup.hitsNeeded = group.getInt("hits_needed", -1);
				winGroup.gameOver = group.getBool("game_over", false);
				pickGroups.Add(keyName, winGroup);
			}
		}
	}

	private void initPersonalJackpotWinnings(SlotOutcome outcome)
	{
		pickPersonalJackpots = new Dictionary<string, PickPersonalJackpotInfo>();		
		JSON[] personalJackpots = outcome.getPersonalJackpotJSONArray();

		if (personalJackpots != null)
		{
			foreach (JSON jackpotJSON in personalJackpots)
			{
				string jackpotKey = jackpotJSON.getString("jackpot_key", "");
				long credits = jackpotJSON.getLong("credits", 0);
				if (pickPersonalJackpots.ContainsKey(jackpotKey))
				{
					Debug.LogErrorFormat("Duplicate personal jackpot found in ModularChallengeGameOutcome: {0}", jackpotKey);
					continue;
				}

				pickPersonalJackpots[jackpotKey] = new PickPersonalJackpotInfo
				{
					jackpotKey = jackpotKey,
					credits = credits
				};
			}
		}

		outcomeContainsPersonalJackpotOutcome = pickPersonalJackpots.Count > 0;
	}

	// Return the associated winnings code for a target group ID
	public long getCreditValueForGroup(string groupName)
	{
		if (pickGroups.ContainsKey(groupName))
		{
			return pickGroups[groupName].credits;
		}
		else
		{
			Debug.LogWarning("Error, no group found in dictionary with key: " + groupName + " - returning 0 credit value");
			return 0;
		}
	}

	public PickGroupInfo getPickInfoForGroup(string groupName)
	{
		if (pickGroups.ContainsKey(groupName))
		{
			return pickGroups[groupName];
		}
		else
		{
			Debug.LogWarning("Error, no group found in dictionary with key: " + groupName + " - returning null pick info");
			return null;
		}
	}

	// Populate the pool configuration from the paytable
	private void initWinningPools()
	{
		pickPools = new Dictionary<string, Dictionary<int, List<PickPoolInfo>>>();

		JSON baseBonusTable = BonusGamePaytable.findPaytable("base_bonus", payTableName);

		if (baseBonusTable != null)
		{
			foreach (JSON pool in baseBonusTable.getJsonArray("pools"))
			{
				string poolKey = pool.getString("key_name", null);
				Debug.Log("Initializing a pool for key: " + poolKey);

				// pools have a special item arrangement, with a double-sort key
				// first the *horizontal_sort_index*, then the *sort_index*
				JSON[] poolItems = pool.getJsonArray("items");

				// dictionary to sort on the two axes
				Dictionary<int, List<PickPoolInfo>> sortedPools = new Dictionary<int, List<PickPoolInfo>>();

				foreach (JSON poolItem in poolItems)
				{
					PickPoolInfo pickPoolInfo = new PickPoolInfo();

					// sort outcomes are returned as 1-indexed, convert to 0-indexed
					pickPoolInfo.sortIndex = (poolItem.getInt("sort_index", 0) - 1);
					pickPoolInfo.horizontalSortIndex = (poolItem.getInt("horizontal_sort_index", 0) - 1);
					pickPoolInfo.multiplier = poolItem.getInt("multiplier", -1);

					// add to the proper horizontal category
					if(!sortedPools.ContainsKey(pickPoolInfo.horizontalSortIndex))
					{
						sortedPools.Add(pickPoolInfo.horizontalSortIndex, new List<PickPoolInfo>());
					}

					sortedPools[pickPoolInfo.horizontalSortIndex].Add(pickPoolInfo);
				}

				// sort each horizontal category
				foreach (int horizontalSortIndex in sortedPools.Keys)
				{
					sortedPools[horizontalSortIndex].Sort((PickPoolInfo x, PickPoolInfo y) => x.sortIndex.CompareTo(y.sortIndex));
				}

				pickPools.Add(poolKey, sortedPools);
			}
		}
		else
		{
			Debug.Log("No pick pool infos found for paytable: " + payTableName);
		}
	}

	// Return a list of pool infos for a specific multiplier tier
	public List<PickPoolInfo> getPoolInfoForLadderTier(string poolKey, int horizontalIndex)
	{
		if (!pickPools.ContainsKey(poolKey))
		{
			Debug.LogError("ModularChallengeGameOutcome: No pick pool found for key: " + poolKey);
			return null;
		}

		if (!pickPools[poolKey].ContainsKey(horizontalIndex))
		{
			Debug.LogError("ModularChallengeGameOutcome: No pick pool tier found for level: " + horizontalIndex);
			return null;
		}

		return pickPools[poolKey][horizontalIndex];
	}

	// Return the appropriate multiplier for a pool level
	public int getMultiplierForPickPool(string poolKey, int horizontalIndex, int verticalIndex)
	{
		List<PickPoolInfo> tierPools = getPoolInfoForLadderTier(poolKey, horizontalIndex);

		if (tierPools[verticalIndex] == null)
		{
			Debug.LogError("ModularChallengeGameOutcome: No pick pool multiplier found for level: " + verticalIndex);
		}

		return tierPools[verticalIndex].multiplier;
	}

	// Return the current round from the incrementing index
	public ModularChallengeGameOutcomeRound getCurrentRound()
	{
		return getRound(outcomeIndex);
	}

	// Return if the current round is defined from the incrementing index.
	public bool hasCurrentRound()
	{

		if (outcomeIndex < rounds.Count)
		{
			return rounds[outcomeIndex] != null;
		}

		return false;
	}

	// return the number of rounds in this outcome, some games might want to check this so they don't try 
	// and initialize stuff for rounds which aren't going to be reached because they aren't in the outcome
	public int roundCount
	{
		get { return rounds.Count; }
	}

	// Get a round from the outcome by index
	public ModularChallengeGameOutcomeRound getRound(int roundIndex)
	{
		if (roundIndex < rounds.Count)
		{
			return rounds[roundIndex];
		}
		else
		{
			Debug.LogWarning("ModularChallengeGameOutcome.getRound() - Trying to get round which is out of bounds! roundIndex: " + roundIndex + "; rounds.Count: " + rounds.Count + "; returning null!");
			return null;
		}
	}

	// Convert PickemOutcome to have entries/reveals in ModularChallengeGameOutcome rounds
	private void setPickemOutcome(PickemOutcome outcome)
	{
		pickemOutcome = outcome;

		// create a single round out of this pickem outcome
		List<ModularChallengeGameOutcomeEntry> entries = new List<ModularChallengeGameOutcomeEntry>();
		foreach (PickemPick pick in pickemOutcome.entries)
		{
			entries.Add(new ModularChallengeGameOutcomeEntry(pick));
		}

		List<ModularChallengeGameOutcomeEntry> reveals = new List<ModularChallengeGameOutcomeEntry>();
		foreach (PickemPick pick in pickemOutcome.reveals)
		{
			reveals.Add(new ModularChallengeGameOutcomeEntry(pick));
		}

		rounds.Add(new ModularChallengeGameOutcomeRound(entries, reveals, outcome.specialBonusOutcome));
	}
	
	// Convert Wheel Outcome to have entries/reveals in ModularChallengeGameOutcome rounds
	private void setWheelOutcome(WheelOutcome outcome)
	{
		wheelOutcome = outcome;
		rounds.AddRange(outcome.getWheelEntriesAndRevealsByOutcome());
	}

	// Convert NewBaseBonusGameOutcome to have entries/reveals in ModularChallengeGameOutcome rounds
	private void setNewBaseBonusGameOutcome(NewBaseBonusGameOutcome outcome)
	{
		newBaseBonusGameOutcome = outcome;

		List<RoundPicks> roundPicksList = newBaseBonusGameOutcome.getRoundPicksAsList();

		for (int i = 0; i < roundPicksList.Count; ++i)
		{
			List<ModularChallengeGameOutcomeEntry> entries = new List<ModularChallengeGameOutcomeEntry>();
			foreach (BasePick pick in roundPicksList[i].entries)
			{
				entries.Add(new ModularChallengeGameOutcomeEntry(pick));
			}

			List<ModularChallengeGameOutcomeEntry> reveals = new List<ModularChallengeGameOutcomeEntry>();
			foreach (BasePick pick in roundPicksList[i].reveals)
			{
				reveals.Add(new ModularChallengeGameOutcomeEntry(pick));
			}

			rounds.Add(new ModularChallengeGameOutcomeRound(entries, reveals, roundPicksList[i].specialBonusOutcome));
		}

		initWinningGroups(); // we may have additional group info from the base_bonus table
		initWinningPools();
	}
 
#region pickemOutcome
	// Access for pickemOutcome paytableGroups value
	public JSON[] paytableGroups
	{
		get
		{
			if (pickemOutcome != null)
			{
				return pickemOutcome.paytableGroups;
			}
			else if (newBaseBonusGameOutcome != null)
			{
				return newBaseBonusGameOutcome.paytableGroupsJsonArray;
			}
			else
			{
				Debug.LogWarning("pickemOutcome is null!");
				return null;
			}
		}
	}

	public int initialMultiplier
	{
		get
		{
			if (pickemOutcome != null)
			{
				return pickemOutcome.initialMultiplier;
			}
			else
			{
				return 1;
			}
		}
	}
	
	public int multiplier
	{
		get
		{
			if (pickemOutcome != null)
			{
				return pickemOutcome.multiplier;
			}
			else
			{
				Debug.LogWarning("pickemOutcome is null!");
				return 0;
			}
		}
	}
	
	
	//Credits field used mostly for non-slot related bonus games. This is expected if the credits in the picks themselves
	//aren't set in SCAT because they need to be dynamic based on some player metrics rather than being adjusted by the slot's
	//wager multiplier.
	public long dynamicBaseCredits
	{
		get
		{
			if (newBaseBonusGameOutcome != null)
			{
				return newBaseBonusGameOutcome.dynamicBaseCredits;
			}
			else
			{
				return 0;
			}
		}
	}
	
	// Access for pickemOutcome jackpotBaseValue value
	public long jackpotBaseValue
	{
		get
		{
			if (pickemOutcome != null)
			{
				if (BonusGameManager.instance.betMultiplierOverride != -1)
				{
						long jackpotBaseValue = pickemOutcome.jackpotBaseValue;
						
						jackpotBaseValue /= (GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers);
						jackpotBaseValue *= BonusGameManager.instance.betMultiplierOverride;
						
						return jackpotBaseValue;
				}
				
				return pickemOutcome.jackpotBaseValue;
			}
			else
			{
				return 0;
			}
		}
	}

	// Access for pickemOutcome jackpotFinalValue value
	public long jackpotFinalValue
	{
		get
		{
			if (pickemOutcome != null)
			{
				if (pickemOutcome.jackpotFinalValue > 0)
				{
					if (BonusGameManager.instance.betMultiplierOverride != -1)
					{
						long jackpotFinalValue = pickemOutcome.jackpotFinalValue;
						
						jackpotFinalValue /= (GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers);
						jackpotFinalValue *= BonusGameManager.instance.betMultiplierOverride;
						
						return jackpotFinalValue;
					}
						
					return pickemOutcome.jackpotFinalValue;
				}
				else
				{
					return getCurrentRound().getHighestPossibleCreditValue(); //If the pickemOutcome.jackpotFinalValue is 0 in the data then we'll use the highest credit reveal as the jackpot
				}
			}
			else
			{
				Debug.LogWarning("pickemOutcome is null!");
				return 0;
			}
		}
	}

	// The maximum number of cards you could theoretically pick before the round ends.
	public int maxCardsPicked
	{
		get
		{
			if (pickemOutcome != null)
			{
				return pickemOutcome.maxCardsPicked;
			}
			else
			{
				Debug.LogWarning("pickemOutcome is null!");
				return 0;
			}
		}
	}

#endregion

#region wheelOutcome
	// Get full list from pay table of all entries on the wheel for a round
	public List<ModularChallengeGameOutcomeEntry> getAllWheelPaytableEntriesForRound(int roundNumber = 0)
	{
		if (wheelOutcome != null)
		{
			List<WheelPick> allEntriesAsWheelPick = wheelOutcome.getAllPaytableEntriesForRound(roundNumber);
			List<ModularChallengeGameOutcomeEntry> allEntriesAsModularEntries = new List<ModularChallengeGameOutcomeEntry>();

			if (allEntriesAsWheelPick != null)
			{
				foreach (WheelPick wheelPick in allEntriesAsWheelPick)
				{
					allEntriesAsModularEntries.Add(new ModularChallengeGameOutcomeEntry(wheelPick));
				}
			}

			return allEntriesAsModularEntries;
		}
		else
		{
			Debug.LogWarning("ModularChallengeGameOutcome.getAllWheelPaytableEntriesForRound() - wheelOutcome is null!");
			return null;
		}
	}
#endregion

#region newBaseBonusGameOutcome
	// A multiplier that applies to a feature which doesn't exist as a picking game with paytables, see munsters01 tug_of_war feature for an example
	public long newBaseBonusAwardMultiplier
	{
		get
		{
			if (newBaseBonusGameOutcome != null)
			{
				return newBaseBonusGameOutcome.awardMultiplier;
			}
			else
			{
				return -1;
			}
		}
	}

	public List<RoundPicks> getNewBaseBonusRoundPicks()
	{
		if (newBaseBonusGameOutcome != null)
		{
			return newBaseBonusGameOutcome.getRoundPicksAsList();
		}
		else
		{
			return null;
		}
	}
#endregion
}

/**
Represents a round of a game in ModularChallengeGameOutcomeEntry
*/
public class ModularChallengeGameOutcomeRound : GenericBonusGameOutcome<ModularChallengeGameOutcomeEntry>
{
	public ModularChallengeGameOutcomeRound(List<ModularChallengeGameOutcomeEntry> entries, List<ModularChallengeGameOutcomeEntry> reveals, SlotOutcome specialRoundBonusOutcome) : base("")
	{
		this.entries = entries;
		this.reveals = reveals;
		this.specialBonusOutcome = specialRoundBonusOutcome;
	}

	// Gets the highest possible credit value in the available picks & reveals
	public long getHighestPossibleCreditValue()
	{
		long returnValue = 0;

		foreach (ModularChallengeGameOutcomeEntry entry in entries)
		{
			if (entry.credits > returnValue)
			{
				returnValue = entry.credits;
			}
		}

		foreach (ModularChallengeGameOutcomeEntry entry in reveals)
		{
			if (entry.credits > returnValue)
			{
				returnValue = entry.credits;
			}
		}

		return returnValue;
	}

	// Given a value, return the ranking for credit values (0 being the lowest)
	public int getRankIndexForCreditValue(long creditValue)
	{
		int creditRank = 0;

		// create a list of both entries & reveals & sort
		List<ModularChallengeGameOutcomeEntry> sortedOutcomes = new List<ModularChallengeGameOutcomeEntry>();
		sortedOutcomes.AddRange(entries);
		sortedOutcomes.AddRange(reveals);
		sortedOutcomes.Sort(ModularChallengeGameOutcomeEntry.compareOutcomeCreditValues);

		// eliminate duplicates
		List<long> sortedCredits = new List<long>();
		foreach (ModularChallengeGameOutcomeEntry outcome in sortedOutcomes)
		{
			if (!sortedCredits.Contains(outcome.credits))
			{
				sortedCredits.Add(outcome.credits);
			}
		}

		// find the appropriate place in the list for the target credit value
		creditRank = sortedCredits.IndexOf(creditValue);

		return creditRank;
	}
}

/**
Data entries that ModularChallengeGameOutcome will spit out, that will be a standard way of getting data regardless of the outcome type used
*/
public class ModularChallengeGameOutcomeEntry
{
	public PickemPick pickemPick = null;
	public WheelPick wheelPick = null;
	public BasePick newBaseBonusGamePick = null;

	// Group combination for getting concatenated list of groups used to map a set of
	// groups to an arbitrary groupName in zynga05 pickgame
	private string _groupCombination;
	public string groupCombination
	{
		get
		{
			if (string.IsNullOrEmpty(_groupCombination))
			{
				if (groupIds != null && groupIds.Length > 0)
				{
					_groupCombination = System.String.Join("", groupIds);
				}
				else
				{
					_groupCombination = groupId;
				}
			}

			return _groupCombination;
		}
	}

	public ModularChallengeGameOutcomeEntry(PickemPick pick)
	{
		pickemPick = pick;
	}

	public ModularChallengeGameOutcomeEntry(WheelPick pick)
	{
		wheelPick = pick;
	}

	public ModularChallengeGameOutcomeEntry(BasePick pick)
	{
		newBaseBonusGamePick = pick;
	}

	public CorePickData corePickData
	{
		get
		{
			if (pickemPick != null)
			{
				return pickemPick as CorePickData;
			}
			else if (wheelPick != null)
			{
				return wheelPick as CorePickData;
			}
			else if (newBaseBonusGamePick != null)
			{
				return newBaseBonusGamePick as CorePickData;
			}
			else
			{
				return null;
			}
		}
	}

	public long credits
	{
		get
		{
			if (corePickData != null)
			{
				// Increase credits with bonus multiplier if necessary
				long revealedCredits = corePickData.credits;
				
				if (BonusGameManager.instance.betMultiplierOverride != -1)
				{
					BonusGamePresenter.instance.useMultiplier = false;
					revealedCredits = revealedCredits / (GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers);
					revealedCredits *= BonusGameManager.instance.betMultiplierOverride;
				}

				ModularChallengeGame currentGame = (ModularChallengeGame.instance as ModularChallengeGame);
				if (currentGame != null)
				{
					ModularChallengeGameRound currentRound = currentGame.getCurrentRound();
					if (currentRound != null)
					{
						ModularChallengeGameVariant currentVariant = currentRound.getCurrentVariant();
						if (currentVariant != null)
						{
							if (currentVariant.useMultipliedCreditValues)
							{
								revealedCredits *= BonusGameManager.instance.currentMultiplier;
							}
						}
						else
						{
							Debug.LogWarning("ModularChallengeGameVariant returned null while attempting to return credits!");
						}
					}
					else
					{
						Debug.LogWarning("ModularChallengeGameRound returned null while attempting to return credits!");
					}
				}
				else
				{
					Debug.LogWarning("ModularChallengeGame returned null while attempting to return credits!");
				}

				return revealedCredits;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return 0;
			}
		}
	}

	public long winID
	{
		get
		{
			if (wheelPick != null)
			{
				return wheelPick.winID;
			}
			else if (newBaseBonusGamePick != null)
			{
				return newBaseBonusGamePick.winId;
			}
			else
			{
				return 0;
			}
		}
	}
	// Compare two modular challenge outcomes & sort by the credit values
	public static int compareOutcomeCreditValues(ModularChallengeGameOutcomeEntry a, ModularChallengeGameOutcomeEntry b)
	{
		if (a.credits >= b.credits)
		{
			return 1;
		}
		else
		{
			return -1;
		}
	}

	public long baseCredits
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.baseCredits;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return 0;
			}
		}
	}

	public int multiplier
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.multiplier;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return 0;
			}
		}
	}

	public int spins
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.spins;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return 0;
			}
		}
	}

	public int meterValue
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.meterValue;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return 0;
			}
		}
	}

	public string meterAction
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.meterAction;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return "";
			}
		}
	}

	// Amount of Super Bonus meter that is filled when this pick is revealed
	public int superBonusDelta
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.superBonusDelta;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return 0;
			}
		}
	}

	public string bonusGame
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.bonusGame;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return "";
			}
		}
	}
	
	public SlotOutcome nestedBonusOutcome
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.nestedBonusOutcome;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return null;
			}
		}
	}

	public string pick
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.pick;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return "";
			}
		}
	}

	public bool isBad
	{
		get
		{
			if (pick != null)
			{
				return pick.ToUpper() == "BAD";
			}
			else
			{
				return false;
			}
		}
	}

	public bool isJackpot
	{
		get
		{
			if (pickemPick != null)
			{
				if (pickemPick.isJackpot)
				{
					return pickemPick.isJackpot;
				}
				else
				{
					//If the isJackpot flag isn't true from SCAT then we're in a game where the highest credit reveal will be the jackpot amount so lets check for this
					ModularChallengeGame currentGame = (ModularChallengeGame.instance as ModularChallengeGame);
					if (currentGame != null)
					{
						ModularChallengeGameRound currentRound = currentGame.getCurrentRound();
						if (currentRound != null)
						{
							ModularChallengeGameVariant currentVariant = currentRound.getCurrentVariant();
							return credits == currentVariant.outcome.jackpotFinalValue;
						}
					}
					
					return false;
				}
			}
			else if (corePickData != null)
			{
				return corePickData.isJackpot;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return false;
			}
		}
	}

	public long jackpotIncrease
	{
		get
		{
			long jackpotIncreaseValue = 0;
			
			if (pickemPick != null)
			{
				jackpotIncreaseValue = pickemPick.jackpotIncrease;	
			}
		
			if (BonusGameManager.instance.betMultiplierOverride != -1)
			{
				jackpotIncreaseValue /= (GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers);
				jackpotIncreaseValue *= BonusGameManager.instance.betMultiplierOverride;	
			}
		
			return jackpotIncreaseValue;
		}
	}
	
	public bool isGameOver
	{
		get
		{
            if (wheelPick != null)
            {
                return !wheelCanContinue;
            }
            else if (corePickData != null)
			{
				return corePickData.isGameOver && !canAdvance;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return false;
			}
		}
	}

	public int additonalPicks
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.additionalPicks;
			}
			else
			{
				return 0;
			}			
		}
	}

	public int extraRound
	{
		get
		{
			if (wheelPick != null)
			{
				return wheelPick.extraRound;
			}
			else
			{
				return 0;
			}
		}
	}

	public bool canAdvance
	{
		get
		{
			return newBaseBonusGamecanAdvance || pickemCanAdvance || wheelCanContinue;
		}
	}

	public bool isCollectAll
	{
		get
		{
			return pickemPickIsCollectAll || wheelPickIsWinAll;
		}
	}

	public string groupId
	{
		get
		{
			if (newBaseBonusGamePick != null)
			{
				return newBaseBonusGamePickGroupId;
			}
			else if (pickemPick != null)
			{
				return pickemGroupId;
			}
			else if (wheelPick != null)
			{
				return wheelPickGroupId;
			}
			else
			{
				return null;
			}
		}
	}

	public string[] groupIds
	{
		get
		{
			if (newBaseBonusGamePick != null)
			{
				return newBaseBonusGamePickGroupIds;
			}
			else
			{
				return null;
			}
		}
	}
	
	//Number of Quest For the Chest keys awarded by the entry
	public int qfcKeys
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.qfcKeys;
			}

			return 0;
		}
	}

	public string cardPackKey
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.cardPackKey;
			}

			return "";
		}
	}
	
	public int prizePopPicks
	{
		get
		{
			if (pickemPick != null)
			{
				return pickemPick.prizePopPicks;
			}

			return 0;
		}
	}
	
	public int landedRung
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.landedRung;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return -1;
			}
		}
	}

	public List<Rewardable> rewardables
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.rewardables;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return null;
			}
		}
	}

	public bool containsRewardable(string type)
	{
		for (int i = 0; i < rewardables.Count; i++)
		{
			if (rewardables[i] != null && rewardables[i].type == type)
			{
				return true;
			}
		}

		return false;
	}

	public int randomAffectedLadderRung
	{
		get
		{
			if (corePickData != null)
			{
				return corePickData.randomAffectedLadderRung;
			}
			else
			{
				Debug.LogWarning("No CorePickData to grab from, this means no Pick variable is set!");
				return - 1;
			}
		}
	}
	
#region newBaseBonusGamePick
	public bool newBaseBonusGamecanAdvance
	{
		get
		{
			if (newBaseBonusGamePick != null)
			{
				return newBaseBonusGamePick.canAdvance;
			}
			else
			{
				return false;
			}
		}
	}

	public int newBaseBonusGameGroupHits
	{
		get
		{
			if (newBaseBonusGamePick != null)
			{
				return newBaseBonusGamePick.groupHits;
			}
			else
			{
				return 0;
			}
		}
	}

	public string newBaseBonusGamePickGroupId
	{
		get
		{
			if (newBaseBonusGamePick != null)
			{
				return newBaseBonusGamePick.groupId;
			}
			else
			{
				return null;
			}
		}
	}

	public string[] newBaseBonusGamePickGroupIds
	{
		get
		{
			if (newBaseBonusGamePick != null)
			{
				return newBaseBonusGamePick.groupIds;
			}
			else
			{
				return null;
			}
		}
	}

	public string poolKey
	{
		get
		{
			return newBaseBonusGamePick.poolKeyName;
		}
	}

	public int horizontalShift
	{
		get 
		{
			return newBaseBonusGamePick.horizontalShift;
		}
	}

	public int verticalShift
	{
		get 
		{
			return newBaseBonusGamePick.verticalShift;
		}
	}

	#endregion // newBaseBonusGamePick

#region pickemPick
	public string pickemGroupId
	{
		get
		{
			if (pickemPick != null)
			{
				return pickemPick.groupId;
			}
			else
			{
				return "";
			}
		}
	}

	public bool pickemPickIsCollectAll
	{
		get
		{
			if (pickemPick != null)
			{
				return pickemPick.isCollectAll;
			}
			else
			{
				return false;
			}
		}
	}

	// This no longer checks groupId and instead relies on having a bonus_game set to advance to
	public bool pickemCanAdvance
	{
		get
		{
			if (pickemPick != null)
			{
				return pickemBonusGame != "";
			}
			else
			{
				return false;
			}
		}
	}

	// This represents the challenge game that will be advanced to if enough of these picks are revealed
	public string pickemBonusGame
	{
		get
		{
			if (pickemPick != null)
			{
				return pickemPick.bonusGame;
			}
			else
			{
				return "";
			}
		}
	}

#endregion // pickemPick

#region wheelPick
	public string wheelExtraData
	{
		get
		{
			if (wheelPick != null)
			{
				return wheelPick.extraData;
			}
			else
			{
				//Debug.LogWarning("ModularChallengeGameOutcomeEntry.wheelPickExtraData - wheelPick was null!");
				return "";
			}
		}
	}

	public bool wheelCanContinue
	{
		get
		{
			if (wheelPick != null)
			{
				return wheelPick.canContinue;
			}
			else
			{
				return false;
			}
		}
	}

	public bool wheelPickIsWinAll
	{
		get
		{
			if (wheelPick != null)
			{
				long winnings = wheelPick.credits;
				long totalwinnings = 0;

				foreach (WheelPick wp in wheelPick.wins)
				{
					//It seems that in the WheelPick class winIndex gets assigned the id field from the outcome json (see line 244 WheelOutcome.cs)
					//Thats why we are checking winIndex on the wheel pick instead of win ID
					if (!(wp.winIndex == wheelPick.winID))
					{
						totalwinnings += wp.credits;
					}
				}

				if (winnings == totalwinnings)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{				
				return false;
			}
		}
	}

	public int wheelWinIndex
	{
		get
		{
			if (wheelPick != null)
			{
				return wheelPick.winIndex;
			}
			else
			{
				//Debug.LogWarning("ModularChallengeGameOutcomeEntry.wheelWinIndex - wheelPick was null!");
				return -1;
			}
		}
	}
	
	public string wheelPickGroupId
	{
		get
		{
			if (wheelPick != null)
			{
				return wheelPick.group;
			}
			else
			{
				return "";
			}
		}
	}

	// Compare two wheel outcomes & sort by the extra data field
	public static int compareWheelExtraData(ModularChallengeGameOutcomeEntry a, ModularChallengeGameOutcomeEntry b)
	{
		long valueA = long.Parse(a.wheelExtraData);
		long valueB = long.Parse(b.wheelExtraData);

		if (valueA <= valueB)
		{
			return 1;
		}
		else
		{
			return -1;
		}
	}
#endregion // wheelPick
}
