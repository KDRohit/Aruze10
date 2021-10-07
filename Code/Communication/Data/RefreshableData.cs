/*
 * Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
 * Class: RefreshableData
 * Description: 
 	Class to handle data that can expire before our 24-hour force-refresh. 
	You register a timer (a name ending with '_ttl' that the server will send down as a key 
	for an integer (the number of seconds until it expires) at login. You also attach any
	number of variable names that will be coming down with this refresh timer.
*/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RefreshableData : IResetGame {

	// List of the current timers we have running.
	private static Dictionary<string, RefreshableDataTimer> _timers;
   
	// Getter to ensure that it is initialized when we try to add a new timer.
	private static Dictionary <string, RefreshableDataTimer> timers
	{
		get
		{
			if (_timers == null)
			{
				_timers = new Dictionary <string, RefreshableDataTimer>();
			}
			return _timers;
		}
	}

	// List of the timers that we have sent out to be refreshed but have yet to return.
	private static Dictionary <string, RefreshableDataTimer> _pendingRefreshTimers;
	
	// Getter for above list.
	private static Dictionary <string, RefreshableDataTimer> pendingRefreshTimers
	{
		get
		{
			if(_pendingRefreshTimers == null)
			{
				_pendingRefreshTimers = new Dictionary <string, RefreshableDataTimer>();
			}
			return _pendingRefreshTimers;
		}
	}

	private static GameTimer waitingTimer = null;
	
	// Set a data timer. If one already exists, then we update it with the new time to live, otherwise we make a new one.
	public static void setDataTimer(string name, int timeToLive, string[] vars)
	{
		if (timers.ContainsKey(name))
		{
			timers[name].timer = new GameTimer(timeToLive);
		}
		else
		{
			timers.Add(name, new RefreshableDataTimer(name, timeToLive, vars));
		}
		checkWaitTimer();
	}

	private static void checkWaitTimer()
	{
		if (timers.Count > 0)
		{
			int smallestTimeRemaining = (waitingTimer != null) ? waitingTimer.timeRemaining : int.MaxValue;
			foreach (RefreshableDataTimer dataTimer in timers.Values)
			{
				if (dataTimer.timer.timeRemaining < smallestTimeRemaining)
				{
					smallestTimeRemaining = dataTimer.timer.timeRemaining;
				}
			}

			if (waitingTimer == null)
			{
				waitingTimer = new GameTimer(smallestTimeRemaining);
			}
			else if (smallestTimeRemaining < waitingTimer.timeRemaining)
			{
				waitingTimer.startTimer(smallestTimeRemaining);
			}
		}
		else
		{
			waitingTimer = null;
		}

	}
	// Callback for when we receive the "refresh_data" event from the server in response to the "refresh_data" action.
	public static void onRefreshData(JSON data)
	{
		Server.unregisterEventDelegate(PlayerAction.REFRESH_DATA, onRefreshData);
		
		JSON fieldData = data.getJSON("field_data");
		if (fieldData != null)
		{
			foreach(string key in fieldData.getKeyList())
			{
				if (!key.Contains("_ttl"))
				{
					switch (key)
					{
						// We will have specific cases here for every refreshable data that we implement.
					case "mobile_w2e_inventory":
						int newInventory = fieldData.getInt(key, -1);
						if (newInventory > -1)
						{
							WatchToEarn.inventory = newInventory;
						}
						break;
					case "mobile_w2e_reward_amount":
						long newRewardAmount = fieldData.getLong(key, -1);
						if (newRewardAmount > 1)
						{
							WatchToEarn.rewardAmount = newRewardAmount;
						}
						break;
					default:
						Debug.LogWarning("Receiving data for key,value pair: " + key + "-----" + fieldData.getString(key, ""));
						Debug.LogWarning("There is no specific case for it, so it is not being processed");
						break;
					}
				}
				else
				{
					// Else this is a timer name so we want to find the expired timer and restart it.
					int newTimerValue = fieldData.getInt(key, -1);
					if (newTimerValue > -1)
					{
						if (pendingRefreshTimers.ContainsKey(key))
						{
							RefreshableDataTimer expiredTimer = pendingRefreshTimers[key];
							expiredTimer.timer = new GameTimer(newTimerValue);
							pendingRefreshTimers.Remove(key); // Remove from expired timers.
							if (!timers.ContainsKey(key))
							{
								timers.Add(key, expiredTimer); // Add to currently ticking timers.
							}
							else
							{
								timers[key] = expiredTimer; // If it already exists, overwrite.
							}
						}
						else
						{
							Debug.LogError("for some reason the key: " + key + " was not in the pending list");
						}

					}
				}
			}
			checkWaitTimer();
		}
		else
		{

		}
	}
	
	public static void update()
	{
		if (waitingTimer != null && waitingTimer.isExpired)
		{
			// If our waiting timer is expired, then we go through out refresh timer.
			List<RefreshableDataTimer> refreshTimers = new List<RefreshableDataTimer>();
			List<string> toRefresh = new List<string>();
			foreach (KeyValuePair<string, RefreshableDataTimer> pair in timers)
			{
				RefreshableDataTimer dataTimer = pair.Value;
				if (dataTimer.timer.isExpired)
				{
					toRefresh.Add(dataTimer.name);
					foreach(string field in dataTimer.variables)
					{
						// Add each of the variables for that timer.
						toRefresh.Add(field);
					}
					refreshTimers.Add(dataTimer);
				}
			}

			for (int i = 0; i < refreshTimers.Count; i++)
			{
				// Move the timers we are refreshing to the expired list.
				timers.Remove(refreshTimers[i].name);
				if (!pendingRefreshTimers.ContainsKey(refreshTimers[i].name))
				{
					pendingRefreshTimers.Add(refreshTimers[i].name, refreshTimers[i]);
				}
				else
				{
					// If it already exists, then overwrite it.
					pendingRefreshTimers[refreshTimers[i].name] = refreshTimers[i];
				}
			}
			if (pendingRefreshTimers.Count > 0)
			{
				PlayerAction.refreshData(toRefresh);
			} 
			refreshTimers.Clear();
			checkWaitTimer();
		}
	}
	
	// Search predicate returns true if the timer has expired.
	private static bool hasExpired(RefreshableDataTimer timer)
	{
		return timer.timer.isExpired;
	}
	
	// Private class for ease of sorting and storage of the timers.
	private class RefreshableDataTimer
	{
		public string name; // The name of the data value that is the number of seconds until the data is stale.
		public GameTimer timer; // The game timer we start to keep track of whether the data is stale yet.
		public string[] variables; // A timer may have multiple bits of bata associated with it that come down on the response.
				
		public RefreshableDataTimer(string name, int timeToLive, string[] variables)
		{
			this.name = name;
			this.timer = new GameTimer(timeToLive);
			this.variables = variables;
		}
	}


	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	public static void resetStaticClassData()
	{
		_timers = null;
		//Action.resetStaticClassData(); NEVER CALL THE BASE CLASS'S RESET!
	}
}