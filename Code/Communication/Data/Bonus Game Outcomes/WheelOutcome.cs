using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/**
Data structure to process and dish out outcomes tailor-made for basic wheel bonus games.
*/

public class WheelOutcome : GenericBonusGameOutcome<WheelPick>
{
	public JSON[] rounds = null;				// Rounds may need to be readable when there is 1 slice win within 2 wheels.
	public int parameter;						// If a parameter was in the json, let's store it here.
	public int[] roundStopIDS;					// List of round stops
	public int extraInfo;						// Any extra info we need to setup when creating this outcome?
	protected SlotOutcome baseOutcome;			// Store the passed base outcome so that entries/reveals can be recreated for ModularChallengeGameOutcome
	private bool multipleSliceWinsSetByData;    // this flag is used to determine if multiple slice processing has been turned on by data instead of in the WheelOutcome constructor
	
	public WheelOutcome(SlotOutcome baseOutcome, bool multipleSliceWins = false, int maxPossibleRoundStops = 0, bool getChildOutcomes = false) : base(baseOutcome.getBonusGame())
	{
		if (getChildOutcomes)
		{
			getSubSubOutcomes(baseOutcome);
			return;
		}

		entries = new List<WheelPick>();
		reveals = new List<WheelPick>();
		
		this.baseOutcome = baseOutcome;

		ReadOnlyCollection<SlotOutcome> wheelOutcomes = baseOutcome.getSubOutcomesReadOnly();
		JSON paytable = baseOutcome.getBonusGamePayTable();
		if (paytable == null)
		{
			Debug.LogErrorFormat("missing bonusgame paytable in {0}!",baseOutcome.getJsonObject().ToString());
			return;
		}
		rounds = paytable.getJsonArray("rounds");

		// Sometimes, a wheel outcome has an optional parameter attached, ala wow04
		parameter = baseOutcome.getParameter();
		
		// Gets the "round_1_stop_ids" types listed up for future use
		if (maxPossibleRoundStops != 0)
		{
			roundStopIDS = new int[maxPossibleRoundStops];
			for (int i = 0; i < maxPossibleRoundStops; i++)
			{
				roundStopIDS[i] = baseOutcome.getRoundStop(i+1);
			}
		}
		
		// Create the picks list.
		// wasn't set in the constructor, lets see if data wants this feature on
		// this is the case when we have muliple wheeloutcomes and only 1 round
		if (multipleSliceWins == false)
		{
			multipleSliceWins = wheelOutcomes.Count > 1 && rounds.Length == 1;
			multipleSliceWinsSetByData = multipleSliceWins;
		}
		
		int round = 0;
		foreach (SlotOutcome pick in wheelOutcomes)
		{
			if (multipleSliceWins)
			{
				for (int i = 0; i < rounds.Length; i++)
				{
					populateEntriesAndReveals(i, pick, entries, reveals);
				}
			}
			else
			{
				populateEntriesAndReveals(round, pick, entries, reveals);
				round++;
			}
		}
	}

	// Get full list of paytable entries for a specific round
	public List<WheelPick> getAllPaytableEntriesForRound(int roundNumber = 0)
	{
		List<WheelPick> fullEntryList = new List<WheelPick>();

		if (rounds != null && roundNumber < rounds.Length)
		{
			JSON[] roundWins = rounds[roundNumber].getJsonArray("wins");

			for (int i = 0; i < roundWins.Length; i++)
			{
				int id = roundWins[i].getInt("id", -1);
				fullEntryList.Add(new WheelPick(roundWins, id, null, 0));
			}
		}

		return fullEntryList;
	}

	// Get the wheel entries and reveals for the list of wheel outcome rounds, used by ModularChallengeGameOutcome
	public List<ModularChallengeGameOutcomeRound> getWheelEntriesAndRevealsByOutcome()
	{
		List<List<WheelPick>> wheelEntries = new List<List<WheelPick>>();
		List<List<WheelPick>> wheelReveals = new List<List<WheelPick>>();

		ReadOnlyCollection<SlotOutcome> wheelOutcomes = baseOutcome.getSubOutcomesReadOnly();
		int round = 0;
		if (rounds == null || round > rounds.Length)
		{
			return null;
		}

		//process the wheeloutcome picks, by either using multiple slice win process (treats multiple outcomes as 1 round)
		//or by processing the pick for regular outcomes
		foreach (SlotOutcome pick in wheelOutcomes)
		{
			wheelEntries.Add(new List<WheelPick>());
			wheelReveals.Add(new List<WheelPick>());
			
			if (multipleSliceWinsSetByData)
			{
				for (int i = 0; i < rounds.Length; i++)
				{
					List<WheelPick> wheelEntry = wheelEntries[i];
					List<WheelPick> wheelReveal = wheelReveals[i];
					populateEntriesAndReveals(i, pick, wheelEntry, wheelReveal);
				}
			}
			else
			{
				List<WheelPick> wheelEntry = wheelEntries[round];
				List<WheelPick> wheelReveal = wheelReveals[round];
				populateEntriesAndReveals(round, pick, wheelEntry, wheelReveal);
				round++;
			}
		}

		List<ModularChallengeGameOutcomeRound> returnRounds = new List<ModularChallengeGameOutcomeRound>();
	
		List<ModularChallengeGameOutcomeEntry> theEntries = new List<ModularChallengeGameOutcomeEntry>();
		List<ModularChallengeGameOutcomeEntry> theReveals = new List<ModularChallengeGameOutcomeEntry>();

		for (int i = 0; i < wheelEntries.Count; ++i)
		{
			//we need a new set of these each time, because there are multiple rounds instead of multi-slice wins 
			//which come from multiple outcomes treated as one round
			if (!multipleSliceWinsSetByData)
			{
				theEntries = new List<ModularChallengeGameOutcomeEntry>();
				theReveals = new List<ModularChallengeGameOutcomeEntry>();
			}
			
			foreach (WheelPick pick in wheelEntries[i])
			{
				theEntries.Add(new ModularChallengeGameOutcomeEntry(pick));
			}
			
			foreach (WheelPick pick in wheelReveals[i])
			{
				theReveals.Add(new ModularChallengeGameOutcomeEntry(pick));
			}

			returnRounds.Add(new ModularChallengeGameOutcomeRound(theEntries, theReveals, specialBonusOutcome));
		}
		
		return returnRounds;
	}

	private void populateEntriesAndReveals(int roundIndex, SlotOutcome pick, List<WheelPick> newEntries, List<WheelPick> newReveals)
	{
		JSON[] roundWins = rounds[roundIndex].getJsonArray("wins");
		SlotOutcome nestedBonusOutcome = getNestedBonusOutcome(pick);

		foreach (JSON roundWin in roundWins)
		{
			int id = roundWin.getInt("id", -1);
			int winId = pick.getWinId();

			//used to fill in credit values where they are defined in pick data
			long pickCredits = 0;
			pickCredits = pick.getOverrideCredits();
			pickCredits *= GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;

			if (id == winId)
			{
				// Check if this pick awards a Super Bonus amount
				int superBonusValueAdded = getSuperBonusValueAdded(pick);
				WheelPick winPick = new WheelPick(roundWins, winId, nestedBonusOutcome, superBonusValueAdded);
				
				//replace the winPick credits with values where they are currently 0 and we have credit
				//values from the SlotOutcome pick
				if (pickCredits > 0 && winPick.credits <= 0)
				{
					winPick.credits = pickCredits;
				}
				newEntries.Add(winPick);
			}
			else
			{
				//replace the revealPick credits with values where they are currently 0 and we have credit
				//values from the SlotOutcome pick
				WheelPick revealPick = new WheelPick(roundWins, id, null, 0);
				if (pickCredits > 0 && revealPick.credits <= 0)
				{
					revealPick.credits = pickCredits;
				}
				newReveals.Add(revealPick);	
			}
		}
	}

	private void getSubSubOutcomes(SlotOutcome baseOutcome)
	{
		entries = new List<WheelPick>();
		
		ReadOnlyCollection<SlotOutcome> parentWheelOutcomes = baseOutcome.getSubOutcomesReadOnly();
		foreach (SlotOutcome parentWheelOutcome in parentWheelOutcomes)
		{
			int round = 0;
			ReadOnlyCollection<SlotOutcome> childWheelOutcomes = parentWheelOutcome.getSubOutcomesReadOnly();
			foreach(SlotOutcome childWheelOutcome in childWheelOutcomes)
			{
				JSON paytable = childWheelOutcome.getBonusGamePayTable();
				rounds = paytable.getJsonArray("rounds");
				
				SlotOutcome nestedBonusOutcome = getNestedBonusOutcome(childWheelOutcome);
				// Check if this pick awards a Super Bonus amount
				int superBonusValueAdded = getSuperBonusValueAdded(childWheelOutcome);
				
				entries.Add(new WheelPick(rounds[round].getJsonArray("wins"), childWheelOutcome.getFirstRoundStopID(), nestedBonusOutcome, superBonusValueAdded, paytable.getString("key_name", "")));
				round++;
			}
		}
	}
	
	private SlotOutcome getNestedBonusOutcome(SlotOutcome parentOutcome)
	{
		// For now we only support one nested bonus, so grab the first valid one we find
		return parentOutcome.getBonusGameInOutcomeDepthFirst();
	}
    
	// Intended to get a Super Bonus meter change value from the outcome JSON.
	// If a super bonus entry can't be found it will just return 0.
	// NOTE: This code is untested!
	private int getSuperBonusValueAdded(SlotOutcome parentOutcome)
	{
		JSON superBonusMeterJson = parentOutcome.getJsonObject().getJSON("super_bonus_meter");
		if (superBonusMeterJson != null)
		{
			return superBonusMeterJson.getInt("delta", 0);
		}

		return 0;
	}
}
/**
Simple data structure used by WheelOutcome.
*/
public class WheelPick : CorePickData
{
	public List<WheelPick> wins = new List<WheelPick>(); 	// We need to know the other possible wins for display purposes.
	public int winID = 0;									// Paytable win ID, in case it is needed.
	public int winIndex = -1;								// The array index in wins that this pick represents.
	public string progressivePool = "";
	public string extraData = "";							// extra data, used for special features of games
	public bool canContinue = false;						// tells if getting this pick will end the game
	public int extraRound = 0;
	public string paytableName = "";
	public string group = "";
	
	// Add more properties as necessary, since probably not all wheel games use all properties.
	
	public WheelPick(JSON[] winsJson, int winId, SlotOutcome nestedBonusOutcome, int superBonusDelta, string paytableName = "")
	{
		winID = winId;
		for (int i = 0; i < winsJson.Length; i++)
		{
			wins.Add(new WheelPick(winsJson[i]));

			if (winsJson[i].getInt("id", -1) == winId)
			{
				winIndex = i;
			}
		}

		baseCredits = wins[winIndex].baseCredits;
		credits = wins[winIndex].credits;
		multiplier = wins[winIndex].multiplier;
		progressivePool = wins[winIndex].progressivePool;
		extraData = wins[winIndex].extraData;
		canContinue = wins[winIndex].canContinue;
		bonusGame = wins[winIndex].bonusGame;
		group = wins[winIndex].group;
		extraRound = wins[winIndex].extraRound;
		additionalPicks = wins[winIndex].additionalPicks;
		qfcKeys = wins[winIndex].qfcKeys;
		spins = wins[winIndex].spins;

		this.paytableName = paytableName;
		this.nestedBonusOutcome = nestedBonusOutcome;
		this.superBonusDelta = superBonusDelta;
	}

	/// Initialize the list value of one of the possible wins
	private WheelPick(JSON possibleWin)
	{
		winIndex = possibleWin.getInt("id", -1);
		baseCredits = possibleWin.getLong("credits", 0L);
		credits = baseCredits * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers;
		multiplier = possibleWin.getInt("multiplier", 0);
		extraRound = possibleWin.getInt("extra_rounds", 0);
		progressivePool = possibleWin.getString("progressive_pool", "");
		extraData = possibleWin.getString("extra_data", "");
		canContinue = possibleWin.getBool("continue", false);
		bonusGame = possibleWin.getString("bonus_game", "");
		group = possibleWin.getString("group", "");
		qfcKeys = possibleWin.getInt("qfc_tokens", 0);
		spins = possibleWin.getInt("spins", 0);

		if (!string.IsNullOrEmpty(bonusGame))
		{
			// get the additonal picks that can be awarded from max_cards_picked
			// so we don't have to parse the bonus game name
			// gen26 is the first game to use this
			if (BonusGamePaytable.hasPaytablesOfType(BonusGamePaytable.PICKEM_PAYTABLE))
			{
				JSON _payTable = BonusGamePaytable.findPaytable(BonusGamePaytable.PICKEM_PAYTABLE, bonusGame);
				if (_payTable != null)
				{
					additionalPicks = _payTable.getInt("max_cards_picked", 0);
				}
			}
		}

		// Check for nested bonuses
		// For now we only support one nested bonus, so grab the first valid one we find
		nestedBonusOutcome = SlotOutcome.getBonusGameInOutcomeDepthFirstFromJson(possibleWin);

		// Check for super bonus meter info
		JSON superBonusMeterJson = possibleWin.getJSON("super_bonus_meter");
		if (superBonusMeterJson != null)
		{
			superBonusDelta = superBonusMeterJson.getInt("delta", 0);
		}
	}
	
	/// Output for debugging purposes.
	public override string ToString()
	{
		return string.Format(
			"credits: {0}, multiplier: {1}, progressive_pool {2}",
			credits,
			multiplier,
			progressivePool
		);
	}
}
