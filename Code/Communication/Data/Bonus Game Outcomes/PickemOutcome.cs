using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/**
Data structure to process and dish out outcomes tailor-made for pickem bonus games.
*/

public class PickemOutcome : GenericBonusGameOutcome<PickemPick>
{
	public JSON[] paytableGroups;		// If a game needs access to the paytable groups referenced by the pickem for visual display, this can be used.
	public int initialMultiplier = 1;
	public int multiplier = 0;          // This is the multiplier from SCAT.  0 means that it's not used.
	public int finalMultiplier = 1;		// If a game needs a look-ahead to the final multiplier for visual display, this can be used.
	public long jackpotBaseValue = 0;   // This comes from SCAT, in the basic properties of the game's pickem pay table
	public long jackpotFinalValue = 0;  // This is calulated from outcome values, only if we have a base jackpot value or else we'll use the highest credit reveal amount.
	public int maxCardsPicked = 0;      // The maximum number of cards you could theoretically pick before the round ends.
	public JSON modifiers = null;

	private int _bonusPicks = 0;

	public const string GAMEOVER_JSON_VALUE = "game_over";
	public const string JACKPOT_INCREASE_KEY = "jackpot_increase";
	public const string JACKPOT_AWARDED_KEY = "jackpot_credits_awarded";

	public PickemOutcome() : base("")
	{
	}

	public PickemOutcome(SlotOutcome baseOutcome) : base(baseOutcome.getBonusGame())
	{
		if (baseOutcome == null)
		{
			Debug.LogError("PickemOutcome - PickemOutcome - baseOutcome is null.");
			return;
		}

		entries = new List<PickemPick>();
		reveals = new List<PickemPick>();

		SlotOutcome pickemOutcome = baseOutcome.getSubOutcomesReadOnly()[0];
		if (pickemOutcome == null)
		{
			Debug.LogError("PickemOutcome - PickemOutcome - pickemOutcome is null.");
			return;
		}

		JSON bonusGamePayTable = baseOutcome.getBonusGamePayTable();
		JSON[] paytableCards = null;

		if (bonusGamePayTable != null)
		{
			paytableCards = bonusGamePayTable.getJsonArray("cards");
		}
		else
		{
			Debug.LogError("PickemOutcome::ctor - bonusGamePayTable is null.");
			return;
		}

		if (paytableCards == null)
		{
			Debug.LogError("PickemOutcome - PickemOutcome - baseOutcome.getBonusGamePayTable().getJsonArray(\"cards\") is null.");
			return;
		}

		initialMultiplier = bonusGamePayTable.getInt("initial_multiplier", 1);
		multiplier = bonusGamePayTable.getInt("multiplier", 0); // 0 means that it's not used.
		jackpotBaseValue = bonusGamePayTable.getLong("jackpot_credits", 0) * GameState.baseWagerMultiplier *
		                   GameState.bonusGameMultiplierForLockedWagers;
		jackpotFinalValue = jackpotBaseValue; //This should start out as our baseValue and will be Increased later on
		maxCardsPicked = bonusGamePayTable.getInt("max_cards_picked", 0);

		paytableGroups = bonusGamePayTable.getJsonArray("groups");
		if (paytableGroups == null)
		{
			Debug.LogError("PickemOutcome - PickemOutcome - baseOutcome.getBonusGamePayTable().getJsonArray(\"groups\") is null.");
			return;
		}

		JSON[] paytableBonusCards = null;
		JSON paytableGameOverCard = null;

		//Debug.Log("found " + paytableGroups.Length + " paytable groups");
		foreach (JSON paytableGroup in paytableGroups)
		{
			// if reading from a wheel paytable the groups may not be JSON and just a list of string
			// which will result in this array containing only nulls, so we should double check for
			// null here
			if (paytableGroup != null)
			{
				//Debug.Log("paytableGroup code: " + paytableGroup.getString("group_code", ""));
				switch (paytableGroup.getString("group_code", "").ToUpper())
				{
					case "BONUS":
						paytableBonusCards = paytableGroup.getJsonArray("cards");
						if (paytableBonusCards == null)
						{
							Debug.LogError(
								"PickemOutcome - PickemOutcome - paytableGroup.getJsonArray(\"cards\") is null.");
							return;
						}
						break;

					case "BAD":
					case "GAMEOVER":
						JSON[] cards = paytableGroup.getJsonArray("cards");
						if (cards != null && cards.Length > 0)
						{
							// There's only one gameover card, so we always use it no matter how many gameover picks/reveals there are.
							paytableGameOverCard = cards[0];
						}
						else
						{
							paytableGameOverCard = null;
						}

						break;
				}
			}
		}

		// some games are formatted to send the "picks" json down as "simple", they must be parsed differently than the verbose version
		bool useSimple = false;
		string[] pickemOutcomePicks = pickemOutcome.getPicks();

		if (pickemOutcomePicks == null)
		{
			Debug.LogError("PickemOutcome::Ctor - Missing the picks!");
			return;
		}

		foreach (string simplePick in pickemOutcomePicks)
		{
			if (simplePick == "")
			{
				break;
			}
			else
			{
				useSimple = true;
				PickemPick pick = createPick(paytableCards, paytableBonusCards, paytableGameOverCard, simplePick,
					false);
				entries.Add(pick);
				if (jackpotBaseValue > 0 && pick.jackpotIncrease > 0)
				{
					jackpotFinalValue += pick.jackpotIncrease;
				}
			}
		}

		// if picks came down in the verbose version
		if (!useSimple)
		{
			// Create the picks list.
			JSON[] jsonPicks = pickemOutcome.getJsonPicks();

			if (jsonPicks != null)
			{
				foreach (JSON pick in jsonPicks)
				{
					string verbosePick = pick.getString("credits", "");
					if (verbosePick == "")
					{
						verbosePick = pick.getString("group_code", "");
					}

					if (verbosePick == "")
					{
						verbosePick = pick.getString("collect_all", "");
					}

					PickemPick newPick = createPick(paytableCards, paytableBonusCards, paytableGameOverCard, verbosePick, false);
					SlotOutcome nestedBonusOutcome = null;
					if (pick != null)
					{
						nestedBonusOutcome = getNestedBonusOutcome(pick);
					}
					updatePickWithJSONData(pick, newPick, pickemOutcome.getJsonModifiers(), nestedBonusOutcome);
					entries.Add(newPick);

					if (jackpotBaseValue > 0 && newPick.jackpotIncrease > 0)
					{
						jackpotFinalValue += newPick.jackpotIncrease;
					}
				}
			}
			else
			{
				Debug.LogError("PickemOutcome::ctor - Returned json picks array was null");
			}
		}

		// reveals will use the same format as picks, so we don't need to find out again
		if (useSimple)
		{
			string[] revealsArray = pickemOutcome.getReveals();

			if (revealsArray != null)
			{
				// Create the reveals list.
				foreach (string pick in revealsArray)
				{
					reveals.Add(createPick(paytableCards, paytableBonusCards, paytableGameOverCard, pick, true));
				}
			}
			else
			{
				Debug.LogError("PickemOutcome::ctor - Returned reveals array was null");
			}
		}
		else
		{
			JSON[] jsonReveals = pickemOutcome.getJsonReveals();

			if (jsonReveals != null)
			{
				// Create the picks list.
				foreach (JSON pick in jsonReveals)
				{
					string verbosePick = pick.getString("credits", "");
					if (verbosePick == "")
					{
						verbosePick = pick.getString("group_code", "");
					}

					if (verbosePick == "")
					{
						verbosePick = pick.getString("collect_all", "");
					}

					PickemPick newPick = createPick(paytableCards, paytableBonusCards, paytableGameOverCard, verbosePick, false);
					SlotOutcome nestedBonusOutcome = null;
					if (pick != null)
					{
						nestedBonusOutcome = getNestedBonusOutcome(pick);
					}
					updatePickWithJSONData(pick, newPick, pickemOutcome.getJsonModifiers(), nestedBonusOutcome);
					reveals.Add(newPick);

				}
			}
			else
			{
				Debug.LogError("PicemOutcoe::ctor - Missing json reveals");
			}

		}
	}

	private void updatePickWithJSONData(JSON pick, PickemPick newPick, JSON modifiersJson, SlotOutcome nestedBonusOutcome)
	{
		int multiplier = pick.getInt("multiplier", -1);
		if (multiplier != -1 && newPick.multiplier == 0)
		{
			newPick.multiplier = multiplier;
		}

		bool isCollectAll = pick.getBool("is_collect_all", false) || pick.getBool("collect_all", false);
		if (isCollectAll)
		{
			newPick.isCollectAll = isCollectAll;
		}

		long credits = pick.getLong("credits", 0);
		if (credits != 0 && newPick.credits == 0)
		{
			newPick.credits = credits * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
		}

		string groupId = pick.getString("group_code", "");
		if (groupId != "" && newPick.groupId == "")
		{
			newPick.groupId = groupId;

			newPick.isProgressive = (groupId == "PROGRESSIVE") || newPick.isProgressive;
			newPick.isPrize = (groupId == "PRIZE") || newPick.isPrize;
			newPick.isGameOver = (groupId == "BAD") || newPick.isGameOver;
		}

		newPick.setSubsequentBonusGame(paytableGroups);

		// this might be a little game specific, used for bev01, but that is the only game looking at finalMultiplier
		if (newPick.groupId == "BAD")
		{
			finalMultiplier = newPick.multiplier;
		}

		bool hasJsonGameOver = pick.getBool(GAMEOVER_JSON_VALUE, false);
		if (hasJsonGameOver)
		{
			newPick.isGameOver = hasJsonGameOver;
		}

		// right now only zom01 uses this modifier stuff
		string modifier = pick.getString("modifier", null);
		if(modifier != null && modifiersJson != null)
		{
			modifiers = modifiersJson;

			JSON modifierEntryJson = modifiersJson.getJSON(modifier);
			if(modifierEntryJson != null)
			{
				newPick.multiplier = modifierEntryJson.getInt("instant_multiplier", 0);
			}
		}

		long jackpotIncrease = pick.getLong(JACKPOT_INCREASE_KEY, 0L);
		if (jackpotIncrease != 0 && newPick.jackpotIncrease == 0)
		{
			newPick.jackpotIncrease = jackpotIncrease * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
		}

		long jackpotAwarded = pick.getLong(JACKPOT_AWARDED_KEY, 0L);
		if (jackpotAwarded != 0 && newPick.jackpotAwarded == 0)
		{
			newPick.jackpotAwarded = jackpotAwarded * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
		}

		JSON[] pickSpecificReveals = pick.getJsonArray(SlotOutcome.FIELD_REVEALS);
		if (newPick.revealCount == 0)
		{
			newPick.addReveals(pickSpecificReveals, paytableGroups);
		}

		newPick.nestedBonusOutcome = nestedBonusOutcome;
		newPick.spins = pick.getInt("spins", 0);
			
		// Check for super bonus meter info
		JSON superBonusMeterJson = pick.getJSON("super_bonus_meter");
		if (superBonusMeterJson != null)
		{
			newPick.superBonusDelta = superBonusMeterJson.getInt("delta", 0);
		}
	}

	protected SlotOutcome getNestedBonusOutcome(JSON parentOutcomeJson)
	{
		// For now we only support one nested bonus, so grab the first valid one we find
		return SlotOutcome.getBonusGameInOutcomeDepthFirstFromJson(parentOutcomeJson);
	}
	
	/// Creates a PickemPick with properties of the picked card from the paytable cards.
	private PickemPick createPick(JSON[] paytableCards, JSON[] bonusCards, JSON gameOverCard, string pick, bool isReveal)
	{
		JSON card = null;
		//Debug.Log("pick = " + pick);

		switch (pick.ToUpper())
		{
			case "BONUS":
				if (isReveal)
				{
					// If this is a reveal instead of a pick, always use the first bonus card since it's bogus anyway.
					card = bonusCards[0];
				}
				else
				{
					_bonusPicks++;
					foreach (JSON search in bonusCards)
					{
						if (search.getInt("card_number", 0) == _bonusPicks)
						{
							card = search;
							break;
						}
					}
				}
				break;
			
			case "BAD":
			case "GAMEOVER":
			case "SHARK":
				card = gameOverCard;
				break;

			default:
				foreach (JSON search in paytableCards)
				{
					if (search.getString("id", "") == pick)
					{
						card = search;
						break;
					}
				}
				if (card == null)
				{
					foreach (JSON search in paytableGroups)
					{
						if (search.getString("group_code", "") == pick)
						{
							card = search;
						}
					}
				}
				break;
		}
		
		PickemPick newPick = new PickemPick();
		SlotOutcome nestedBonusOutcome = null;
		if (card != null)
		{
			nestedBonusOutcome = getNestedBonusOutcome(card);
		}
		newPick.parsePick(card, pick, paytableGroups, nestedBonusOutcome);
		return newPick;
	}
}

/**
Simple data structure used by PickemOutcome.
*/
public class PickemPick : CorePickData
{
	public bool isBonus = false;
	public bool isProgressive = false;
	public bool isPrize = false;
	public int quantity = 0;
	public bool isCollectAll = false;
	public string groupId = "";
	public string modifier = null;
	public long jackpotIncrease = 0;						// Increase amount for the final jackpot in some pick games
	public long jackpotAwarded = 0;							// Awarded jackpot amount for some pick games
	public int prizePopPicks = 0;
	
	private List<PickemPick> reveals = new List<PickemPick>();	// Some picks may contain their own list of reveals

	public PickemPick() {}

	/// Virtual funciton to setup the pick information, used to parse out data from JSON data
	public virtual void parsePick(JSON card, string pick, JSON[] paytableGroups, SlotOutcome nestedBonusOutcome)
	{
		this.pick = pick;
		this.nestedBonusOutcome = nestedBonusOutcome;
		isBonus = (pick == "bonus" || pick == "SPIN"); //SPIN added for Blondie bonus game
		isGameOver = (pick == "gameover" || pick.ToUpper() == "BAD");
		if (card == null)
		{
			// if card is null, the data should be filled in when updatePickWithJSONData() is called

			if (pick.ToUpper() == "TRUE")
			{
				// Added in because there are times where they'll just send down the damn collect_all:true json down only -_-
				isCollectAll = true;
				isGameOver = true;
			}
		}
		else
		{
			baseCredits = card.getLong("credits", 0L);
			credits = baseCredits * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;

			multiplier = card.getInt("multiplier", 0);
			quantity = card.getInt("quantity", 0);

			// server has another key called collect_all used JUST for t201 Chopper
			isCollectAll = card.getBool("is_collect_all", false) || card.getBool("collect_all", false);

			groupId	= card.getString("group_code", "");
			jackpotIncrease = card.getLong(PickemOutcome.JACKPOT_INCREASE_KEY, 0L) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			jackpotAwarded = card.getLong(PickemOutcome.JACKPOT_AWARDED_KEY, 0L) * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
			
			// set a bonus game that this will go into if it is an advance pick
			setSubsequentBonusGame(paytableGroups);

			setIsJackpot(paytableGroups);

			bool groupGameOver = card.getBool (PickemOutcome.GAMEOVER_JSON_VALUE, false);
			isGameOver = (isGameOver || groupGameOver);

			isProgressive = (groupId == "PROGRESSIVE");
			isPrize = (groupId == "PRIZE");

			foreach (JSON reveal in card.getJsonArray(SlotOutcome.FIELD_REVEALS))
			{
				reveals.Add(new PickemPick(reveal, "reveal", paytableGroups, null));
			}

			spins = card.getInt("spins", 0);
			
			// Check for super bonus meter info
			JSON superBonusMeterJson = card.getJSON("super_bonus_meter");
			if (superBonusMeterJson != null)
			{
				superBonusDelta = superBonusMeterJson.getInt("delta", 0);
			}
		}
	}

	public void addReveals(JSON[] pickSpecificReveals, JSON[] paytableGroups)
	{
		foreach (JSON reveal in pickSpecificReveals)
		{
			reveals.Add(new PickemPick(reveal, "reveal", paytableGroups, null));
		}
	}

	public PickemPick(JSON card, string pick, JSON[] paytableGroups, SlotOutcome nestedBonusOutcome)
	{
		parsePick(card, pick, paytableGroups, nestedBonusOutcome);
	}
	
	/// Output for debugging purposes.
	public override string ToString()
	{
		if (isBonus)
		{
			return "BONUS";
		}
		
		if (isGameOver)
		{
			return "GAME OVER";
		}

		if (isProgressive)
		{
			return "PROGRESSIVE";
		}
		
		return string.Format(
			"credits: {0}, multiplier: {1}, quantity: {2}, isCollectAll: {3}, groupId: {4}",
			credits,
			multiplier,
			quantity,
			isCollectAll,
			groupId
		);
	}

	/// How many reveals are left?
	public int revealCount
	{
		get { return reveals.Count; }
	}

	/// Returns the next reveal and removes it from the list.
	public PickemPick getNextReveal()
	{
		if (reveals.Count == 0)
		{
			return null;
		}
		
		PickemPick entry = reveals[0];
		reveals.RemoveAt(0);
		
		return entry;
	}

	public void setSubsequentBonusGame(JSON[] paytableGroups)
	{
		if (paytableGroups == null)
		{
			return;
		}

		string groupID = groupId == "" ? pick : groupId;

		foreach (JSON group in paytableGroups)
		{
			if (group.getString("group_code", "") == groupID)
			{
				bonusGame = group.getString("bonus_game", "");
			}
		}
	}

	public void setIsJackpot(JSON[] paytableGroups)
	{
		if (paytableGroups == null)
		{
			return;
		}

		string groupIDToCheck = groupId;
		if (groupId == "")
		{
			groupIDToCheck = pick;
		}

		foreach (JSON group in paytableGroups)
		{
			if (group.getString("group_code", "") == groupIDToCheck)
			{
				isJackpot = group.getBool("win_jackpot", false);
			}
		}
	}

	public void parseRewardableInfo(JSON data)
	{
		string type = data.getString("reward_type", "");
		switch (type)
		{
			case "collectible_pack":
				cardPackKey = data.getString("pack_key", "");
				break;
			
			case "coin":
				credits = data.getLong("value", 0);
				break;
			
			case "prize_pop_extra_picks":
				prizePopPicks = data.getInt("value", 0);
				break;
			
			default:
				Debug.LogWarning("UNHANDLED REWARD TYPE: " + type);
				break;
		}
	}
}
