using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
  Class: LevelUpBonus
  Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
  Description: Handles the logic/events behind the Level Up Bonus 
  I am building this class to support more than just the Odd/Even level patterns in the future, as
  the spec has indicated that we are likely to extend this to fit patterns such as every 3rd level, etc. in the future.
  Thus there are a lot of switch statements where it would be cleaner to use a tertiary statement, but for the ease
  of future extendability I am going to make them switches so it is easy to add new entries.
 */
class LevelUpBonus : IResetGame
{
	public static int multiplier = 1;
	public static int minLevel = 0;
	public static LevelPattern pattern;

	public enum LevelPattern
	{
		ODD,	// Matching on odd levels (including the min level if its odd)
		EVEN,	// Matching on even levels (including the min level if its even)
		NONE	// WIll not match anything and should turn the feature off.
	}
	
	public static void init()
	{
		setLevelPattern(Data.liveData.getString("LEVEL_UP_BONUS_COINS_LEVEL_REQUIRED", "").ToUpper());
		minLevel   = Data.liveData.getInt("LEVEL_UP_BONUS_COINS_MIN_LEVEL", 11);
		multiplier = Data.liveData.getInt("LEVEL_UP_BONUS_COINS_MULTIPLIER", 1);

		timeRange = new GameTimerRange(
			Data.liveData.getInt("LEVEL_UP_BONUS_COINS_START_DATE", 0),
			Data.liveData.getInt("LEVEL_UP_BONUS_COINS_END_DATE", 0),
			Data.liveData.getBool("LEVEL_UP_BONUS_COINS_EVENT", false)
		);
	}

	public static bool isBonusActive
	{
		get
		{
			return (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_EVEN_LEVELS_KEY) ||
			        PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_ODD_LEVELS_KEY)) ||
					(meetsLevelReq && timeRange.isActive && pattern != LevelPattern.NONE);
		}
	}

	public static bool isBonusActiveFromPowerup
	{
		get
		{
			return (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_EVEN_LEVELS_KEY) ||
			        PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_ODD_LEVELS_KEY)) &&
			       !(meetsLevelReq && _timeRange.isActive && pattern != LevelPattern.NONE);
		}
	}

	private static GameTimerRange _timeRange = null;
	public static GameTimerRange timeRange
	{
		get
		{
			PowerupBase evenLevelPowerup = PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_EVEN_LEVELS_KEY);
			PowerupBase oddLevelPowerup = PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_ODD_LEVELS_KEY);

			// both power ups are active, pick the one with most time left
			if (evenLevelPowerup != null && oddLevelPowerup != null)
			{
				return oddLevelPowerup.runningTimer.timeRemaining < evenLevelPowerup.runningTimer.timeRemaining ? oddLevelPowerup.runningTimer : evenLevelPowerup.runningTimer;
			}

			if (evenLevelPowerup != null)
			{
				return evenLevelPowerup.runningTimer;
			}

			if (oddLevelPowerup != null)
			{
				return oddLevelPowerup.runningTimer;
			}

			return _timeRange;
		}
		set { _timeRange = value; }
	}

	private static bool meetsLevelReq
	{
		get
		{
			return SlotsPlayer.instance.socialMember != null &&
			SlotsPlayer.instance.socialMember.experienceLevel >= minLevel;
		}
	}

	public static bool doesLevelMatch(int level)
	{
		if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_EVEN_LEVELS_KEY) ||
		    PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_ODD_LEVELS_KEY))
		{
			if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_EVEN_LEVELS_KEY) && level % 2 == 0)
			{
				return true;
			}

			if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_ODD_LEVELS_KEY) && level % 2 == 1)
			{
				return true;
			}
		}

		switch(pattern)
		{
			case LevelPattern.ODD:
				return level % 2 == 1;
			case LevelPattern.EVEN:
				return level % 2 == 0;
			default:
				return false;
		}
	}

	public static string patternKey
	{
		get
		{
			if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_EVEN_LEVELS_KEY) ||
			    PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_ODD_LEVELS_KEY))
			{
				if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_EVEN_LEVELS_KEY))
				{
					return "even";
				}

				if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_ODD_LEVELS_KEY))
				{
					return "odd";
				}
			}

			switch(pattern)
			{
  				case LevelPattern.ODD:
  					return "odd";
  				case LevelPattern.EVEN:
  					return "even";
  				default:
  					return "none";
			}
		}
	}

	// Get a list of numbers as strings, that represent the pattern for the bonus
	// (eg. 1, 3, 5, 7... for odd)
	public static List<string> getPatternList(int count)
	{
		List<string> result = new List<string>();
		int start = 0;
		switch (pattern)
		{
			case LevelPattern.ODD:
				start = 1;
				break;
			case LevelPattern.EVEN:
				start = 2;
				break;
			default:
				break;
		}
		for (int i = 0; i < count; i++)
		{
			result.Add(start.ToString());
			start += 2;
		}
		return result;
	}

	public static string getMultiplierString()
	{
		return Localize.text("level_up_bonus", "");
	}

	// Function to sett the enum based off of the string we recieve from data.
	public static void setLevelPattern(string patternString)
	{
		if (System.Enum.IsDefined(typeof(LevelPattern), patternString))
		{
		    pattern = (LevelPattern)System.Enum.Parse(typeof(LevelPattern), patternString);
		}
		else
		{
			Debug.LogWarning(string.Format("LevelUpBonus -- pattern is unrecognized on client: {0}", patternString));
		    pattern = LevelPattern.NONE;
		}
	}

	public static void resetStaticClassData()
	{
		timeRange = null;
		multiplier = 1;
		minLevel = 0;
		pattern = LevelPattern.NONE;
	}
}