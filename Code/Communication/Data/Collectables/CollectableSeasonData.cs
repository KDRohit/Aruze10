using System;
using UnityEngine;
using System.Collections.Generic;

// Just holds relevant info for collectable season 
public class CollectableSeasonData
{
	public int id;
	public string keyName;
	public List<string> albumsInSeason;


	public CollectableSeasonData(JSON data = null)
	{
		if (data != null)
		{
			id = data.getInt("id", 0);
			keyName = data.getString("key_name", "");
			albumsInSeason = new List<string>();
		}
	}
}
