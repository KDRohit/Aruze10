using UnityEngine;
using System;
using System.Collections.Generic;

// Payline class instance - the global data from "payline_sets".  Used to help animation and display of player wins.
public class PaylineSet : IResetGame
{
	public string keyName;
	public Dictionary<string,int> payLines = new Dictionary<string, int>();
	
	public bool usesClustering;
	public bool paysFromLeft;
	public bool paysFromRight;
	
	private static Dictionary<string, PaylineSet> _all = new Dictionary<string, PaylineSet>();
	
	public PaylineSet (JSON data)
	{
		keyName = data.getString("key_name", "");
		
		JSON[] paylineJsonArray = data.getJsonArray("pay_lines");
		foreach (JSON paylineJson in paylineJsonArray)
		{
			int lineNumber = paylineJson.getInt("line_number", 0);
			string payline = paylineJson.getString ("pay_line", "");
			payLines[payline] = lineNumber;
		}
		
		usesClustering = data.getBool("uses_clustering", false);
		paysFromLeft = data.getBool("pays_from_left", false);
		paysFromRight = data.getBool("pays_from_right", false);
	}
	
	public static void populateAll(JSON[] paylineSets)
	{
		foreach (JSON data in paylineSets)
		{
			string key = data.getString("key_name", "");
			
			if (key == "")
			{
				Debug.LogError("Cannot process empty payline set key");
				continue;
			}
			else if (_all.ContainsKey(key))
			{
				Debug.LogError("PaylineSet already exists for key " + key);
				continue;
			}
			
			_all[key] = new PaylineSet(data);
		}
	}
	
	public static PaylineSet find(string keyName)
	{
		PaylineSet result = null;
		
		if (!_all.TryGetValue(keyName, out result))
		{
			Debug.LogError("Failed to find Payline for key " + keyName);
		}
		
		return result;
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		_all = new Dictionary<string,PaylineSet>();
	}
}
