using UnityEngine;
using System;
using System.Collections.Generic;

// GlobalSkuData - contains the definitions for economy multiplier
public class GlobalSkuData : IResetGame
{
	public string keyName { get; private set; }
	public string id { get; private set; }
	public string servernamespace { get; private set; }
	public string fbapppageid { get; private set; }

	private static Dictionary<string, GlobalSkuData> _all = new Dictionary<string, GlobalSkuData>();

	public GlobalSkuData (JSON data) {
		keyName = data.getString("key_name", "");
		id = data.getString ("id", "");
		servernamespace = data.getString("namespace", "");
		fbapppageid = data.getString("fb_app_page_id", "");
	}

	public static void populateAll(JSON[] globalSkuData)
	{
		foreach (JSON data in globalSkuData)
		{
			string keyName = data.getString("key_name", "");
		
			if (keyName == "")
			{
				Debug.LogError("Cannot process empty GlobalSkuData key");
				continue;
			}
			else if (_all.ContainsKey(keyName))
			{
				Debug.LogWarning("GlobalSkuData already exists for key " + keyName);
				continue;
			}

			_all[keyName] = new GlobalSkuData(data);
		}
	}

	public static GlobalSkuData find(string key)
	{
		GlobalSkuData result = null;

		if (!_all.TryGetValue(key, out result))
		{
			Debug.LogError("Failed to find GlobalSkuData for key " + key);
		}

		return result;
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		_all = new Dictionary<string,GlobalSkuData>();
	}
}
