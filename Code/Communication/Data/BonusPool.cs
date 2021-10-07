using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BonusPool : IResetGame
{
	public string keyName;
	public bool isShowReveals;
	public bool isTriggerGame;
	public BonusPoolItem reevaluationItem { get; private set; }	///< Shortcut getter.
	
	public Dictionary<string, BonusPoolItem> items = new Dictionary<string, BonusPoolItem>();
		
	private static Dictionary<string, BonusPool> _all = new Dictionary<string, BonusPool>();
	
	public static void populateAll(JSON[] array)
	{
		foreach (JSON json in array)
		{
			new BonusPool(json);
		}
	}
	
	public BonusPool(JSON json)
	{
		int tempValue = 0;

		keyName = json.getString("key_name", "");
		isShowReveals = json.getBool("show_reveals", false);
		isTriggerGame = json.getBool("trigger_game", false);
		
		
		// Due to badly formatted json data from server, we need to iterate key/value pairs,
		// since some of the data was used as the keys.
		JSON itemsJson = json.getJSON("items");
		
		if (itemsJson != null)
		{
			foreach (string key in itemsJson.getKeyList())
			{
				BonusPoolItem item = new BonusPoolItem();
			
				item.keyName = key;
				items.Add(item.keyName, item);
			
				// The value could either be a string or another json object with reevaluation data.
				JSON subJson = itemsJson.getJSON(key);
			
				if (subJson != null)
				{
					item.setReevaluations(subJson);
					reevaluationItem = item;
				}
				else
				{
					// Assuming a string that is the item type.
					item.type = itemsJson.getString(key, "");
					
					if (item.type == "bonus_multiplier")
					{
						// This is really bad, but the amount to multiply by is parsed
						// from the key name of the item, and added to the base of 1.
						// So if the item key is "2", then the multiplier is 1 + "2" = 3.
						if (int.TryParse(item.keyName, out tempValue))
						{
							item.multiplier = 1 + tempValue;
						}
						else
						{
							Debug.LogWarning(item.keyName + " isn't an int, check the json");
						}
					}
				}
			}
		}
		
		if (_all.ContainsKey(keyName))
		{
			Debug.LogWarning("Duplicate BonusPool key: " + keyName);
		}
		else
		{
			_all.Add(keyName, this);
		}
	}
	
	/// Standard find method.
	public static BonusPool find(string keyName)
	{
		if (_all.ContainsKey(keyName))
		{
			return _all[keyName];
		}
		return null;
	}
	
	public BonusPoolItem findItem(string keyName)
	{
		if (items.ContainsKey(keyName))
		{
			return items[keyName];
		}
		return null;
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{	
		_all = new Dictionary<string, BonusPool>();
	}
	
}

/// Data structure for an item in a bonus pool.
public class BonusPoolItem
{
	public string keyName;
	public string type;
	public int multiplier = 1;
	public BonusPoolReevaluation reevaluations = null;
	
	public BonusPoolItem()
	{
	}
	
	public void setReevaluations(JSON jsonRoot)
	{
		type = "reevaluation";	// We shouldn't have to hard-code this, but it's not in the included data.
		 						// Assumed that sub json is reevaluation data.
		
		JSON json = jsonRoot.getJSON("reevaluations");
		
		// Assuming there will always be a max of one reevaluation data per item.
		reevaluations = new BonusPoolReevaluation();
		
		if (json == null)
		{
			Debug.LogError("Found no data where expected in reevaluations block of bonus_pools");
			return;
		}
		
		// Since the keys for the json are also dynamic data here (bad technique),
		// we need to get the keys and then retrieve the data using that key. Only expecting 1 key.
		
		List<string> keys = json.getKeyList();
		
		if (keys.Count == 0)
		{
			Debug.LogError("Found no data where expected in reevaluations block of bonus_pools: " + keyName);
			return;
		}
		
		JSON reeval = json.getJSON(keys[0]);
		
		if (reeval == null)
		{
			// This appears to happen regularly now with sharknado data, commenting out the error logging
			// Debug.LogError("Found no data where expected in reevaluations block of bonus_pools: " + keys[0]);
			return;
		}
		
		string[] symbols = reeval.getString("exclude_symbol", "").Split(',');
		reevaluations.excludeSymbols = new List<string>(symbols);
	}
}

/// Data structure for a reevaluation in a bonus pool.
public class BonusPoolReevaluation
{
	public List<string> excludeSymbols = null;
	
	public BonusPoolReevaluation()
	{
	}
}
