using UnityEngine;
using System;
using System.Collections.Generic;

// Payline class - contains the definitions for which positions on the reels constitute a win.  Note that a game that uses clustering
//  does not use paylines; the client is informed of the symbol for each win and has to calculate the win positions.
public class Payline : IResetGame
{
	public string keyName;
	public int[] positions;
	
	private static Dictionary<string, Payline> _all = new Dictionary<string, Payline>();
	
	public Payline (JSON data)
	{
		keyName = data.getString("key_name", "");
		positions = data.getIntArray("positions");
	}
	
	public static void populateAll(JSON[] paylines)
	{
		foreach (JSON data in paylines)
		{
			string keyName = data.getString("key_name", "");
			
			if (keyName == "")
			{
				Debug.LogError("Cannot process empty payline key");
				continue;
			}
			else if (_all.ContainsKey(keyName))
			{
				Debug.LogWarning("Payline already exists for key " + keyName);
				continue;
			}
			
			_all[keyName] = new Payline(data);
		}
	}
	
	public static Payline find(string key)
	{
		Payline result = null;
		
		if (!_all.TryGetValue(key, out result))
		{
			Debug.LogError("Failed to find Payline for key " + key);
		}
		
		return result;
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		_all = new Dictionary<string,Payline>();
	}
}
