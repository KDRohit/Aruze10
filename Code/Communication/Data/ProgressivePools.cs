using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Reads in progressive pool data from player data and static data during load.

- init() reads in player data records with any pool_credits, creates Progressive object to hold them and stores in Dictionary all.
- bindStaticData() function adds in data from static data to all progressives including "reset_value" and whether a progressive is personal.

- updateProgressivePools() gets called from slots games to take the wager and add in percentages for the progressive contributions.
- getPoolCredits() returns the entire progressive, base value and accumulated value for a given wagermultiplier. consume=true will reset if its a personal progressive.
*/

public class ProgressivePools 
{
	// Dictionary of all personal progressive levels.
	private Dictionary<string, Progressive> pools = new Dictionary<string, Progressive>();		

	public ProgressivePools(JSON[] items = null)
	{
		if (items == null)
		{
			return;
		}
		
		foreach (JSON item in items)
		{
			Progressive progressive = new Progressive();
			
			string keyName = item.getString("key_name", "");
			
			progressive.poolCredits = item.getLong("pool_credits", 0);
			progressive.keyName = keyName;
			
//			Debug.Log("Define pool: " + keyName + ": " + progressive.poolCredits);
			if (keyName != null && keyName != "")
			{
				pools.Add(keyName, progressive);
			}
			else
			{
				Debug.LogError("A null key has been attempted to be added to the progressive pools! Please request that the server data be fixed.");
			}
			
		}
	}
	
	public void bindStaticData(JSON[] items)
	{			
		foreach (JSON item in items)
		{
			string keyName = item.getString("key_name", "");
			
			
			Progressive pool;
			
			if (pools.ContainsKey(keyName))
			{
				pool = pools[keyName];
			}
			else
			{
				pool = new Progressive();
				pools.Add(keyName, pool);
			}
			
			pool.keyName = keyName;
			
			if (item.hasKey("reset_value"))
			{
				pool.resetValue = item.getLong("reset_value", 0L);
			}
			
			if (item.hasKey("is_personal"))
			{
				pool.isPersonal = item.getBool("is_personal", false);
			}
			
//			Debug.Log("Pool credits for " + keyName + ": " + getPoolCredits(keyName, 70));
		}
	}

	public bool isValidProgressivePool(string keyName)
	{
		return pools.ContainsKey(keyName);
	}

	/// Returns the number of credits for this progressive.
	public long getPoolCredits(string keyName, long wagerMultiplier, bool consume = false)
	{
		if (!pools.ContainsKey(keyName))
		{
			Debug.LogError("Error cannot find progressive pool:" + keyName);
			return 0;
		}
		
		Progressive progressive = pools[keyName];
		long credits = progressive.poolCredits + progressive.resetValue * wagerMultiplier;
		if (SlotBaseGame.instance != null && SlotBaseGame.instance.slotGameData != null)
		{
			credits *= GameState.baseWagerMultiplier;
		}
		
		if (consume && progressive.isPersonal)
		{
			progressive.poolCredits = 0;
		}
		
		return (long)Mathf.Floor(credits);
	}

	public void updatePool(string keyName, long amount)
	{
		if (!pools.ContainsKey(keyName))
		{
			Debug.LogError("Error cannot find progressive pool:" + keyName);
			return;
		}
		Progressive pool = pools[keyName];
		
		pool.poolCredits += amount;
		
//		Debug.Log("Updating pool: " + keyName + ", +" + amount + " = " + pool.poolCredits + ", new total: " + getPoolCredits(keyName, SlotBaseGame.instance.multiplier));
	}
	
	/// Games that have a personal progressive need to call this when wagering.
	public void updateProgressivePools(JSON[] gameProgressivePools, long wager)
	{
//		Debug.Log("updateProgressivePools: " + gameProgressivePools.Length + ", " + wager);
		
		// Check if there are any progressives to update.
		if (gameProgressivePools == null || gameProgressivePools.Length == 0)
		{
			return;
		}	
		
		// Go through this games pools to update using their contribution and key.
		foreach (JSON pool in gameProgressivePools)
		{
			string keyName = pool.getString("key_name", "");
			long amount = (wager * pool.getLong("contribute_percent", 0L)) / 100L;
			updatePool(keyName, amount);
		}
	}
	
	/// Returns the biggest progressive pool payout in the pools.
	public long getLargestProgressivePayout(string[] progressivePools, long wagerMultiplier)
	{
		long maxPoolCredits = 0;
		foreach (string pool in progressivePools)
		{
			long poolCredits = getPoolCredits(pool, wagerMultiplier);
			if (poolCredits > maxPoolCredits)
			{
				maxPoolCredits = poolCredits;
			}
		}
		return maxPoolCredits;
	}
	
	/// Simple data structure.
	private class Progressive
	{
		public string keyName;
		public long resetValue;
		public bool isPersonal;
		public long poolCredits = 0;		

		public Progressive()
		{
	
		}
	}
}


