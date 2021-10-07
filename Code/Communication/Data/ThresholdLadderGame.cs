using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Data structure for threshold_ladder_game global data.
*/

public class ThresholdLadderGame : IResetGame
{
	public string keyName;
	public List<ThresholdLadderGameRound> rounds;
	
	public static Dictionary<string, ThresholdLadderGame> all = new Dictionary<string, ThresholdLadderGame>();
	
	public ThresholdLadderGame(JSON data)
	{
		keyName = data.getString("key_name", "");
		rounds = new List<ThresholdLadderGameRound>();
		
		foreach (JSON round in data.getJsonArray("rounds"))
		{
			rounds.Add(new ThresholdLadderGameRound(round));
		}
		
		if (all.ContainsKey(keyName))
		{
			Debug.LogWarning("Duplicate ThresholdLadderGame key: " + keyName);
		}
		else
		{
			all.Add(keyName, this);
		}
	}
	
	public static void populateAll(JSON[] array)
	{
		foreach (JSON data in array)
		{
			if (!all.ContainsKey(data.getString("key_name", "")))
			{
				// Only add a new one if it doesn't already exist from loading a game previously.
				new ThresholdLadderGame(data);
			}
		}
	}
	
	public static ThresholdLadderGame find(string keyName)
	{
		if (all.ContainsKey(keyName))
		{
			return all[keyName];
		}
		return null;
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{	
		all = new Dictionary<string, ThresholdLadderGame>();
	}	
}

/// Data structure for a single round of a threshold ladder.
public class ThresholdLadderGameRound
{
	public int id;
	public int roundNumber;
	public int cardsDisplayed;
	public int cardsSelected;
	public int targetScore;
	public int basePayout;
	public string progressivePool;
	
	public ThresholdLadderGameRound(JSON data)
	{
		id = data.getInt("id", 0);
		roundNumber = data.getInt("round_number", 0);
		cardsDisplayed = data.getInt("cards_displayed", 0);
		cardsSelected = data.getInt("cards_selected", 0);
		targetScore = data.getInt("target_score", 0);
		basePayout = data.getInt("base_payout", 0);
		progressivePool = data.getString("progressive_pool", "");
	}
}
