using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Rewardables;

/**
Base class for all variations of bonus game outcomes, just so all outcome types can live in the same Dictionary in BonusGameManager.
*/
public abstract class BaseBonusGameOutcome
{
	public string bonusGameName = "";

	public BaseBonusGameOutcome(string bonusGameName)
	{
		this.bonusGameName = bonusGameName;
	}
}

public abstract class CorePickData
{	
	public long credits = 0;								// Amount of credits the user will win, includes baseWagerMultiplier
	public long baseCredits = 0;							// Base amount of credits without baseWagerMultiplier applied
	public int multiplier = 0;								// A multiplier value that this pick is worth
	public int spins = 0;									// Number of spins or additional that this pick awards
	public int meterValue = 0;								// Value which gets added to a meter of some kind
	public string meterAction = "";							// String name of an action that the meter will take, for instance performing some kind of "upgrade"
	public string bonusGame = "";
	public string pick = "";
	public bool isGameOver = false;
	public int additionalPicks = 0;
	public bool isJackpot = false;
	public int qfcKeys = 0;									//Amount of Quest for the Chest keys the user will win
	public SlotOutcome nestedBonusOutcome;					// If this pick triggers another bonus, that bonus will be stored here
	public int superBonusDelta;								// Tracks how much super bonus value this pick was worth for games like gen97 Cash Tower which have a Super Bonus meter that triggers and extra bonus
	public int landedRung;
	public string cardPackKey = "";							//Card pack keyname pick will award
	public List<Rewardable> rewardables = new List<Rewardable>();
	public int randomAffectedLadderRung = -1; //Used by board game mystery card bonus when random rungs are lit/unlit
}

/**
Generic bonus game outcome that implements a list of entries the represent the outcomes of the bonus game
*/
public abstract class GenericBonusGameOutcome<T> : BaseBonusGameOutcome where T : class
{
	public List<T> entries = null;
	public List<T> reveals = null;

	// Used for stuff like a Super Bonus in gen97 Cash Tower where the bonus is tied to the game as a whole
	// and not necessarily a specific pick.  If the bonus is for a specific pick it should be handled using
	// CorePickData.nestedBonusOutcome and not this.
	public SlotOutcome specialBonusOutcome; 

	public GenericBonusGameOutcome(string bonusGameName) : base(bonusGameName)
	{}

	/// How many picks are left?
	public int entryCount
	{
		get { return entries.Count; }
	}
	
	/// Returns the next bonus game value (pick, wheel pick, SlotOutcome) and removes it from the list.
	public virtual T getNextEntry()
	{
		return getNextBonusGameEntry(entries);
	}

	/// Helper function for getNextPick and getNextReveal.
	static protected T getNextBonusGameEntry(List<T> list)
	{
		if (list == null)
		{
			// Avoiding a crash here, but this is an error that should not be happening
			Debug.LogErrorFormat("BonusGameOutcome.cs -- getNextBonusGameEntry -- list was null");
			return null;
		}
		
		if (list.Count == 0)
		{
			return null;
		}
		
		T entry = list[0];
		list.RemoveAt(0);
		
		return entry;
	}

	/// Look at what the next entry value is
	public T lookAtNextEntry()
    {
        if (entries.Count == 0)
        {
            return null;
        }

        return entries[0]; 
    }
    
	/// How many reveals are left?
	public int revealCount
	{
		get { return reveals.Count; }
	}
	
	/// Returns the next pick and removes it from the list.
	public T getNextReveal()
	{
		return getNextBonusGameEntry(reveals);
	}
}
