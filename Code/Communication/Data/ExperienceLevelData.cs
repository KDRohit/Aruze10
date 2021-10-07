using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Data structure for experience levels.
*/

public class ExperienceLevelData : IResetGame
{
	public int level;
	public long requiredXp;
	public long bonusAmt;
	public int bonusVIPPoints;
	public long maxBetIncrease;
	/* 
	   Only used on client to keep track of what the multiplier was when reaching this level,
	   so the tally of VIP points earned over multiple levels is correct when the player finally
	   opens the level up dialog after several levels.
	 */
	public int vipMultiplier = 1;
	public long levelUpBonusAmount = 0;
	
	public static int maxLevel = int.MaxValue;		// If at MaxValue, then we haven't received the max level data yet.
	
	private static Dictionary<int, ExperienceLevelData> all = new Dictionary<int, ExperienceLevelData>();
	
	// Populates the initial data upon startup,
	// which comes from login data.
	public static void populateStartup()
	{
		JSON[] loginLevels = Data.login.getJsonArray("player.xp_levels");
		
		if (loginLevels != null && loginLevels.Length > 0)
		{
			populateAll(loginLevels);
		}
		else
		{
			Debug.LogError("Missing player.xp_levels in login data!");
		}
	}
	
	public static void populateAll(JSON[] sourceArray)
	{
		if (sourceArray == null)
		{
			return;
		}
		
		foreach (JSON level in sourceArray)
		{
			new ExperienceLevelData(level);
		}
	}
	
	public ExperienceLevelData(JSON data) 
	{
		if (data != null)
		{
			level = data.getInt("level", 0);
			
			if (all.ContainsKey(level))
			{
				Debug.LogWarning("Received ExperienceLevelData for level we already have: " + level);
			}
			else
			{
				requiredXp = data.getLong("required_xp", 0);
				bonusAmt = data.getLong("bonus_amount", 0);
				bonusVIPPoints = data.getInt("bonus_vip_points", 0);
				if (data.getBool("is_max_level", false))
				{
					maxLevel = level;
				}

				if (data.hasKey("max_bet"))
				{
					maxBetIncrease = data.getLong("max_bet", 0);
				}

				all.Add(level, this);
			#if UNITY_EDITOR
				Debug.LogFormat("Reading in data for level: {0} -- {1}",level, data.ToString());
			#endif
			}
		}
	}
	
	public static ExperienceLevelData find(int level)
	{
		ExperienceLevelData levelData;
		if (all.TryGetValue(level, out levelData))
		{
			return levelData;
		}
		return null;
	}
	
	// Implements IResetGame
	public static void resetStaticClassData()
	{
		maxLevel = int.MaxValue;
		all = new Dictionary<int, ExperienceLevelData>();
	}
}
