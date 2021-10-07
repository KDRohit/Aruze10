using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Com.Rewardables;

/**
Data structure to process and dish out outcomes tailor-made for pickem bonus games.
*/

public class NewBaseBonusGameOutcome: BaseBonusGameOutcome
{
	private Dictionary<int, JSON[]> possiblePaytableWins;
	private Dictionary<string, JSON> paytableGroups;
	private Dictionary<int, JSON> allWins;
	public Dictionary<string, JSON> paytablePools;
	public Dictionary<int, RoundPicks> roundPicks;
	public long awardMultiplier = -1; // A special multiplier applied to whatever was awarded, used by munsters01 to apply a multiplier to a win value when it is displayed, needed because munsters01 tug of war doesn't have a pay table
	public JSON[] paytableGroupsJsonArray;		// If a game needs access to the paytable groups referenced by the pickem for visual display, this can be used.
	public long dynamicBaseCredits = 0; //Used to determine credits values for picks when credits isn't set in SCAT data. Usually used in conjunction with some multiplier

	// Allows creation of a custom NewBaseBonusGameOutcome which may not be based on a SlotOutcome
	// NOTE: This should be used sparingly, ideally the server provides you data which can be parsed instead of
	// being constructed piecemeal by us
	public NewBaseBonusGameOutcome() : base("")
	{
		paytableGroups = new Dictionary<string, JSON>();
		paytablePools = new Dictionary<string, JSON>();
		possiblePaytableWins = new Dictionary<int, JSON[]>();
		allWins = new Dictionary<int, JSON>();
		roundPicks = new Dictionary<int, RoundPicks>();
	}

	// Normal constructor used to create a NewBaseBonusGameOutcome from a SlotOutcome
	public NewBaseBonusGameOutcome(SlotOutcome baseOutcome, bool isUsingBaseGameMultiplier = false, bool forceOutcomeJson = false, long baseCredits = 0) : base(baseOutcome.getBonusGame())
	{
		paytableGroups = new Dictionary<string, JSON>();
		paytablePools = new Dictionary<string, JSON>();
		possiblePaytableWins = new Dictionary<int, JSON[]>();
		allWins = new Dictionary<int, JSON>();
		roundPicks = new Dictionary<int, RoundPicks>();
		dynamicBaseCredits = baseCredits;

		if (baseOutcome.getNewBonusGamePayTable() != null)
		{
			JSON[] paytableRoundJSON = baseOutcome.getNewBonusGamePayTable().getJsonArray("rounds");
			JSON[] paytableGroupJSON = baseOutcome.getNewBonusGamePayTable().getJsonArray("groups");
			JSON[] paytablePoolsJSON = baseOutcome.getNewBonusGamePayTable().getJsonArray("pools");
			paytableGroupsJsonArray = paytableGroupJSON;
			foreach (JSON paytableGroup in paytableGroupJSON)
			{
				paytableGroups.Add(paytableGroup.getString("key_name", ""), paytableGroup);
			}

			foreach (JSON paytablePool in paytablePoolsJSON)
			{
				paytablePools.Add(paytablePool.getString("key_name", ""), paytablePool);
			}

			foreach (JSON paytableRounds in paytableRoundJSON)
			{
				JSON[] innerWins = paytableRounds.getJsonArray("wins");
				possiblePaytableWins.Add(paytableRounds.getInt("round_number", -1), innerWins);
				foreach (JSON possibleWin in innerWins)
				{
					allWins.Add(possibleWin.getInt("id", -1), possibleWin);
				}
			}
		}

		awardMultiplier = baseOutcome.getAwardMultiplier();

		JSON[] nestedBonusOutcomes = baseOutcome.getJsonSubOutcomes();
		JSON[] rewardablesData = baseOutcome.getPickRewardables(); 
		
		int roundIndex = 0;
		foreach (JSON singlePick in baseOutcome.getBonusGameRounds())
		{
			List<BasePick> selected = new List<BasePick>();
			List<BasePick> unselected = new List<BasePick>();
			JSON[] selectedPicks = singlePick.getJsonArray("selected");
			JSON[] unSelectedPicks = singlePick.getJsonArray("unselected");

			foreach (JSON singleSelectedPick in selectedPicks)
			{
				selected.Add(makePick(singleSelectedPick, isUsingBaseGameMultiplier, forceOutcomeJson, nestedBonusOutcomes, rewardablesData));
			}
			foreach (JSON singleSelectedPick in unSelectedPicks)
			{
				unselected.Add(makePick(singleSelectedPick, isUsingBaseGameMultiplier, forceOutcomeJson));
			}
			
			// Check if there is a special bonus attached to this round, like a Super Bonus that gen97 Cash Tower uses
			SlotOutcome specialRoundBonusOutcome = null;
			JSON[] subOutcomes = singlePick.getJsonArray("outcomes");
			if (subOutcomes != null && subOutcomes.Length > 0)
			{
				// For now we only support one nested bonus, so grab the first valid one we find
				specialRoundBonusOutcome = new SlotOutcome(subOutcomes[0]);
			}

			roundPicks.Add(roundIndex, new RoundPicks(bonusGameName, selected, unselected, specialRoundBonusOutcome));
			roundIndex++;
		}
	}
	
	// Convert the RoundPicks dictionary to a list, useful for getting the rounds in order
	public List<RoundPicks> getRoundPicksAsList()
	{
		List<RoundPicks> roundPicksList = new List<RoundPicks>();
		int roundIndex = 0;
		while (roundPicks.ContainsKey(roundIndex))
		{
			roundPicksList.Add(roundPicks[roundIndex]);
			++roundIndex;
		}

		return roundPicksList;
	}

	private BasePick makePick(JSON singleSelectedPick, bool isUsingBaseGameMultiplier = false, bool forceOutcomeJson = false, JSON[] subOutcomes = null, JSON[] rewardables = null)
	{
		int currentID = singleSelectedPick.getInt("win_id", -1);
		
		if (currentID != -1)
		{
			// normal base_bonus built using a paytable
			string groupCode = allWins[currentID].getString("group", "");
			string[] groups = allWins[currentID].getStringArray("groups");
			
			if (!string.IsNullOrEmpty(groupCode))
			{
				groups = new string[] {groupCode};
			}

			List<JSON> groupJSON = getGroupJSON(groups);

			//if base pick has a pjp - personal jackpot value - store that JSON data for use later in picking module
			BasePick basePick = new BasePick(forceOutcomeJson ? null : allWins[currentID], singleSelectedPick, groupJSON, isUsingBaseGameMultiplier, subOutcomes, rewardables, dynamicBaseCredits);

			return basePick;
		}
		else
		{
			// this is probably a custom game who's outcome was built on the fly, see munsters01 tug_of_war feature for an example
			return new BasePick(null, singleSelectedPick, null, isUsingBaseGameMultiplier);
		}
	}

	private List<JSON> getGroupJSON(string[] groups)
	{
		if (groups == null || groups.Length == 0)
		{
			return null;
		}

		List<JSON> groupJSON = new List<JSON>();

		for (int i = 0; i < groups.Length; i++)
		{
			groupJSON.Add(paytableGroups[groups[i]]);
		}

		return groupJSON;
	}
	
	public bool hasPicksLeft()
	{
		foreach(KeyValuePair<int, RoundPicks> entry in roundPicks)
		{
			if (entry.Value.entryCount > 0)
				return true;
		}
		
		return false;	
	}

	public int totalPicksLeft()
	{
		int picks = 0;

		foreach(KeyValuePair<int, RoundPicks> entry in roundPicks)
		{
			picks += entry.Value.entryCount;
		}
		return picks;
	}
}

public class RoundPicks : GenericBonusGameOutcome<BasePick>
{
	public RoundPicks(string bonusGameName, List<BasePick> entries, List<BasePick> reveals, SlotOutcome specialRoundBonusOutcome) : base(bonusGameName)
	{
		this.entries = entries;
		this.reveals = reveals;
		this.specialBonusOutcome = specialRoundBonusOutcome;
	}

	public long getHighestPossibleCreditValue()
	{
		long returnValue = 0;

		foreach (BasePick entry in entries)
		{
			if (entry.credits > returnValue)
			{
				returnValue = entry.credits;
			}
		}

		foreach (BasePick entry in reveals)
		{
			if (entry.credits > returnValue)
			{
				returnValue = entry.credits;
			}
		}

		return returnValue;
	}

	public int getHighestPossibleMultiplierValue()
	{
		int multiplier = 1;

		foreach (BasePick entry in entries)
		{
			if (entry.multiplier > multiplier)
			{
				multiplier = entry.multiplier;
			}
		}

		foreach (BasePick entry in reveals)
		{
			if (entry.multiplier > multiplier)
			{
				multiplier = entry.multiplier;
			}
		}

		return multiplier;
	}
}

/**
Simple data structure used by PickemOutcome.
*/
public class BasePick : CorePickData
{
	// For the "pools" section of the wins
	public string poolKeyName = "";
	public int verticalShift = 0;
	public int horizontalShift = 0;

	// Normal groups and info in base picks
	public bool canAdvance = false;
	public string groupId = "";
	public int groupHits = 1;
	public int winId = -1;

	public JSON pjp;

	// zynga05 can have multiple groups with in a single pick
	public string[] groupIds;

	public BasePick()
	{
	}

	public BasePick(JSON paytableJson, JSON outcomeJson, List<JSON> groupJSONList, bool isUsingBaseGameMultiplier = false, JSON[] subOutcomes = null, JSON[] rewardables = null, long creditsOverride = 0)
	{
		// Determine what the primary source of data should be.  outcomeJson should always
		// be provided, but might be the only thing if this result doesn't directly reference a paytable.
		// We'll rely on the paytableJson if it exists, except for stuff that has to be read from outcomeJson,
		// and we'll just use outcomeJson only if that is all we have.
		JSON primaryJSON = paytableJson;

		if (primaryJSON == null)
		{
			primaryJSON = outcomeJson;
		}
		
		winId = primaryJSON.getInt("win_id", -1);
		baseCredits = primaryJSON.getLong("credits", 0L);
		if (baseCredits == 0 && outcomeJson != primaryJSON)
		{
			baseCredits = outcomeJson.getInt("credits", 0);
		}
		
		calculateMultipliedCreditValue(isUsingBaseGameMultiplier);

		spins = primaryJSON.getInt("spins", 0);
		if (spins == 0 && outcomeJson != primaryJSON)
		{
			spins = outcomeJson.getInt("spins", 0);
		}
		
		// These meter values might only be in the outcome since they require being determined by a second probability table
		// so if we don't find the value we'll also check the outcomeJson if it doesn't match the paytableJson
		meterValue = primaryJSON.getInt("meter_value", 0);
		if (meterValue == 0 && outcomeJson != primaryJSON)
		{
			meterValue = outcomeJson.getInt("meter_value", 0);
		}
		meterAction = primaryJSON.getString("meter_action", "");
		if (string.IsNullOrEmpty(meterAction) && outcomeJson != primaryJSON)
		{
			meterAction = outcomeJson.getString("meter_action", "");
		}

		multiplier = primaryJSON.getInt("multiplier", 0);
		
		//If theres a multiplier in the outcome, take that one over the paytable one
		if (primaryJSON != outcomeJson && outcomeJson.hasKey("multiplier"))
		{
			multiplier = outcomeJson.getInt("multiplier", 0);
		}
		
		groupId	= primaryJSON.getString("group", "");
		if (string.IsNullOrEmpty(groupId) && outcomeJson != primaryJSON)
		{
			groupId = outcomeJson.getString("group", "");
		}

		groupIds = primaryJSON.getStringArray("groups");
		groupHits = primaryJSON.getInt("group_hits", 0);
		
		// We really should just have lists of groupIds even if it's just one.
		// So let's enforce this here since it would take too much work and
		// break too many games to change it on the server.
		if (groupIds.Length == 0 && !String.IsNullOrEmpty(groupId))
		{
			groupIds = new string[] { groupId };
		}

		if (primaryJSON.hasKey("add_wins"))
		{
			additionalPicks = primaryJSON.getInt("add_wins", 0);
		}
		else
		{
			additionalPicks = primaryJSON.getInt("additional_picks", 0);
		}

		JSON[] pools = primaryJSON.getJsonArray("pools");

		// For now, we assume there's only one pool in a win. This needs to be updated for multiple pools in 1 win.
		if (pools != null && pools.Length != 0)
		{
			foreach (JSON poolJSON in pools)
			{
				poolKeyName = poolJSON.getString("key_name", "");
				verticalShift = poolJSON.getInt("vertical_shift", 0);
				horizontalShift = poolJSON.getInt("horizontal_shift", 0);
			}
		}

		// Set the gameOver and canAdvance of this pick based on the groups or card
		if (groupJSONList != null && groupJSONList.Count > 0)
		{
			for (int i = 0; i < groupJSONList.Count; i++)
			{
				JSON groupJSON = groupJSONList[i];
				if(groupJSON.getBool("game_over", false))
				{
					isGameOver = true;
				}

				if(groupJSON.getBool("end_round", false))
				{
					canAdvance = true;
				}
			}
		}
		else
		{
			isGameOver = primaryJSON.getBool("game_over", false);
			canAdvance = primaryJSON.getBool("end_round", false);
		}
		
		// The data below this point needs to be read directly from outcomeJson
		// since it isn't going to exist in the paytable.
		pjp = outcomeJson.getJSON("pjp");
		
		// Check for nested bonuses
		//If a game has multiple nested bonuses, get the index of the nested bonus then look for it in the provided subOutcomes
		int nestedBonusOutcomeIndex = outcomeJson.getInt("outcome_index", -1);
		if (nestedBonusOutcomeIndex >= 0)
		{
			if (subOutcomes != null && nestedBonusOutcomeIndex < subOutcomes.Length)
			{
				nestedBonusOutcome = new SlotOutcome(subOutcomes[nestedBonusOutcomeIndex]);
			}
			else
			{
				Debug.LogWarningFormat("Outcome index is {0} but suboutcomes only has {1} items", nestedBonusOutcomeIndex, subOutcomes.Length);
			}
		}
		else
		{
			nestedBonusOutcome = SlotOutcome.getBonusGameInOutcomeDepthFirstFromJson(outcomeJson);
		}

		// Check for nested outcomes which are used in orig001 to trigger off a freespins game
		JSON endEvaluation  = outcomeJson.getJSON("end_evaluation");
		if (endEvaluation != null)
		{
			endEvaluation = endEvaluation.getJSON("outcomes");
			nestedBonusOutcome = new SlotOutcome(endEvaluation);
		}

		landedRung = outcomeJson.getInt("landed_rung", -1);
		// Check for super bonus meter info
		JSON superBonusMeterJson = outcomeJson.getJSON("super_bonus_meter");
		if (superBonusMeterJson != null)
		{
			superBonusDelta = superBonusMeterJson.getInt("delta", 0);
		}

		bonusGame = primaryJSON.getString("bonus_game", "");
		
		//TODO: change key back to "pick_rewardable_indices" when server change is in
		int[] rewardableIndices = outcomeJson.getIntArray("pick_rewards");
		for (int i = 0; i < rewardableIndices.Length; i++)
		{
			int index = rewardableIndices[i];
			if (index < rewardables.Length)
			{
				JSON rewardableData = rewardables[index];
				parseRewardableInfo(rewardableData);
			}
		}

		if (outcomeJson.hasKey("is_jackpot"))
		{
			isJackpot = outcomeJson.getBool("is_jackpot", false);
		}

		//Look for other multiplier fields
		if (multiplier == 0)
		{
			multiplier = outcomeJson.getInt("mystery_card_multiplier", 0);
		}

		if (multiplier == 0)
		{
			multiplier = outcomeJson.getInt("mini_slots_multiplier", 0);
		}

		setMeterActionBasedData(outcomeJson, creditsOverride);
	}

	private void setMeterActionBasedData(JSON outcomeJson, long creditsOverride)
	{
		switch (meterAction)
		{
			case "unlandRandomLadderRung":
				randomAffectedLadderRung = outcomeJson.getInt("mystery_card_unlit_square", -1);
				break;
			case "landRandomLadderRung":
				randomAffectedLadderRung = outcomeJson.getInt("mystery_card_lit_square", -1);
				break;
			case "bg_coin":
				credits = creditsOverride;
				break;
		}
	}
	
	
	public void parseRewardableInfo(JSON data)
	{
		string type = data.getString("reward_type", "");
		rewardables.Add(RewardablesManager.createRewardFromType(type, data));
	}

	// Determines what the actual credit value is by taking the base credit value and
	// multiplying it by the correct multiplier for where it is being used.
	private void calculateMultipliedCreditValue(bool isUsingBaseGameMultiplier)
	{
		if (isUsingBaseGameMultiplier)
		{
			credits = baseCredits * SlotBaseGame.instance.multiplier;
		}
		else
		{
			float uniqueMultiplier = 0f;
			if (GameState.tryGetUniqueMultiplier(out uniqueMultiplier))
			{
				credits = (long)(baseCredits * uniqueMultiplier);
			}
			else
			{
				credits = baseCredits * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			}
		}
	}
}
