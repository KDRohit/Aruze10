using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Data structure to process and dish out outcomes tailor-made for threshold ladder bonus games.

A threshold ladder has one more more rounds. Each round has a certain number of picks and reveals.
*/

public class ThresholdLadderOutcome : GenericBonusGameOutcome<ThresholdLadderRound>
{
	public JSON paytable = null;
	public int winRound = 0;
	public string progressivePool = "";

	public ThresholdLadderOutcome(SlotOutcome baseOutcome) : base(baseOutcome.getBonusGame())
	{
		paytable = baseOutcome.getBonusGamePayTable();
		
		JSON ladderOutcome = baseOutcome.getJsonSubOutcomes()[0];
		
		progressivePool = ladderOutcome.getString("progressive_pool", "");
		winRound = ladderOutcome.getInt("win_round", 0) - 1;	// Subtract 1 so it's 0-based to match with arrays in game.
		
		List<List<int>> picks = ladderOutcome.getIntListList("cards_picked");
		List<List<int>> reveals = ladderOutcome.getIntListList("cards_revealed");

		entries = new List<ThresholdLadderRound>();
		
		for (int i = 0; i < picks.Count; i++)
		{
			entries.Add(new ThresholdLadderRound(picks[i], reveals[i]));
		}
	}
}

/**
Simple data structure used by ThresholdLadderOutcome.
*/

public class ThresholdLadderRound
{
	public List<int> _picks = null;
	public List<int> _reveals = null;
	
	public ThresholdLadderRound(List<int> picks, List<int> reveals)
	{
		_picks = picks;
		_reveals = reveals;
	}
	
	/// Returns the next pick and removes it from the picks list.
	public int getNextPick()
	{
		if (_picks.Count == 0)
		{
			return -1;
		}
		int pick = _picks[0];
		_picks.RemoveAt(0);
		return pick;
	}
	
	public int pickCount
	{
		get { return _picks.Count; }
	}

	/// Returns the next reveal and removes it from the reveals list.
	public int getNextReveal()
	{
		if (_reveals.Count == 0)
		{
			return -1;
		}
		int reveal = _reveals[0];
		_reveals.RemoveAt(0);
		return reveal;
	}

	public int revealCount
	{
		get { return _reveals.Count; }
	}
}