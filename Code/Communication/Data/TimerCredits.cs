using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Simple data structure.
*/

public class TimerCredits : IResetGame
{
	// A dictionary of ids to credits from timers.
	// the payout id is returned as an event from the server: "timer_outcome"
	private static Dictionary<string, TimerCredits> _all = new Dictionary<string, TimerCredits>();	
	
	public string id;		// unique id for each payout.
	public int credits;		// credits awarded for this payout.
	public string bonusGame;
	public string vipLevel;

	public TimerCredits(string vipLevel, JSON data)
	{
		id = data.getString("id", "");
		credits = data.getInt("credits", 0);
		bonusGame = data.getString("bonus_game", "");
		this.vipLevel = vipLevel;
		
		if (_all.ContainsKey(id))
		{
			Debug.LogWarning("Duplicate TimerCredits id: " + id);
		}
		else
		{
			_all.Add(id, this);
		}
	}
	
	public static void populateAll(JSON[] timers)
	{
		foreach (JSON timer in timers)
		{
			foreach (JSON level in timer.getJsonArray("levels"))
			{
				foreach (JSON payout in level.getJsonArray("payouts"))
				{
					new TimerCredits(level.getString("vip_level", ""), payout);
				}
			}
		}
	}

	public static TimerCredits find(string id)
	{
		if (_all.ContainsKey(id))
		{
			return _all[id];
		}
		return null;
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		_all = new Dictionary<string, TimerCredits>();
	}
}
