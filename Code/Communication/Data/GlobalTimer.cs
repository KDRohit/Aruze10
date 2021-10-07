using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
THIS CLASS HOLDS DATA FROM GLOBAL DATA, NOT PLAYER!
*/

public class GlobalTimer : IResetGame
{
	public string keyName;
	public JSON[] vipLevels = null;
	public JSON[] multipliers = null;
	public JSON[] rewards = null;
	public JSON[] extraCoins = null;
	
	private static Dictionary<string, GlobalTimer> _all = new Dictionary<string, GlobalTimer>();		
			
	public GlobalTimer(JSON timer)
	{
		keyName = timer.getString("key_name", "");
		vipLevels = timer.getJsonArray("vip_levels");
		multipliers = timer.getJsonArray("multipliers");
		rewards = timer.getJsonArray("rewards");
		extraCoins = timer.getJsonArray("extra_coins");
		// We are going to sort extra Coins so that find payout will be faster.
		System.Array.Sort(extraCoins, delegate(JSON a, JSON b) {
                    return (extraCoinComparator(a,b));
                  });
		
		if (_all.ContainsKey(keyName))
		{
			Debug.LogWarning("Duplicate GlobalTimer key: " + keyName);
		}
		else
		{
			_all.Add(keyName, this);
		}
	}

	// Used to sort the array from lowest to highest, By level and then by day
	private int extraCoinComparator(JSON a, JSON b)
	{
		// Sort by the level first.
		if (a.getInt("min_level", -1) < b.getInt("min_level", -1))
		{
			return -1;
		}
		if (a.getInt("min_level", -1) == b.getInt("min_level", -1))
		{
			// Now we want to sort by the day, but only if the levels are the same.
			if (a.getInt("min_level", -1) == b.getInt("min_level", -1))
			{
				// a is the day before b.
				return a.getInt("payout_number",-1) - b.getInt("payout_number",-1);
			}
			return -1;
		}
		else
		{
			return 1;
		}
	}
	
	public static GlobalTimer find(string keyName)
	{
		if (_all.ContainsKey(keyName))
		{
			return _all[keyName];
		}
		return null;
	}
		
	// TODO: This should probably be using a binary search to find the correct value. For now it's just sorted and short circuits
	public static long findPayout(string timerKey, int level, int day)
	{
		GlobalTimer timer = find(timerKey);
		JSON[] payouts = timer.extraCoins;
		long coinAmt = -1;
		foreach (JSON coinObj in payouts)
		{
			if (day == coinObj.getInt("payout_number",-1))
			{
				if (coinObj.getInt("min_level", -1) <= level)
				{
					coinAmt = coinObj.getInt("coins", -1);
				}
				else
				{
					// We have passed up the spot in the list and we can exit out.`
					break;
				}
			}
		}
		
		return coinAmt;
	}

	public static void populateAll(JSON[] timers)
	{
		foreach (JSON timer in timers)
		{
			new GlobalTimer(timer);
		}
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		_all = new Dictionary<string, GlobalTimer>();
	}
}
