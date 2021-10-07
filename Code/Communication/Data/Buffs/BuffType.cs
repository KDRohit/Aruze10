using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
The most basic data that defines buffs.
*/

public class BuffType : IResetGame
{
	// Set constants that match the 'Buff Type' values in SCAT Buffs table, which are used throughout
	// the code to find the current value for a particular feature.
	public const string DAILY_BONUS_REDUCED_TIMER = "daily_bonus_reduced_timer";
	public const string XP_MULTIPLIER = "xp_multiplier";
	public const string LEVELUP_BONUS_MULTIPLIER = "levelup_bonus_multiplier";
	public const string UNLOCK_ALL_GAMES = "unlock_all_games";
	
	public string keyName { get; private set; }
		
	private static Dictionary<string, BuffType> all = new Dictionary<string, BuffType>();
	
	private BuffType(string keyName)
	{
		this.keyName = keyName;
		all.Add(keyName, this);
	}
	
	public static BuffType find(string keyName)
	{
		BuffType type = null;
		if (!all.TryGetValue(keyName, out type))
		{
			// Doesn't yet exist. Create it.
			type = new BuffType(keyName);
		}
		return type;
	}
	
	public static void resetStaticClassData()
	{
		all = new Dictionary<string, BuffType>();
	}
}
