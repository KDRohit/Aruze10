using System;
using UnityEngine;
using System.Collections.Generic;

// Just holds relevant info for a rusher. 
public class CollectablePackData
{
	public string keyName;
	public PackConstraint[] constraints;
	public List<int> possibleCardSizes;

	public CollectablePackData(string keyName, JSON data)
	{
		this.keyName = keyName;
		JSON[] constraintsJson = data.getJsonArray("constraints");
		List<PackConstraint> newConstraints = new List<PackConstraint>();
		for (int i = 0; i < constraintsJson.Length; i++)
		{
			newConstraints.Add(new PackConstraint(constraintsJson[i]));
		}

		possibleCardSizes = new List<int>();
		JSON[] packWeights = data.getJsonArray("size_weights", true);
		if (packWeights != null)
		{
			for (int i = 0; i < packWeights.Length; i++)
			{
				possibleCardSizes.Add(packWeights[i].getInt("size", 1));
			}
			possibleCardSizes.Sort();
		}

		constraints = newConstraints.ToArray();
	}
}

public class PackConstraint
{
	public int minRarity = 0;
	public int maxRarity = 0;
	public int guaranteedPicks = 0;

	public PackConstraint(JSON data)
	{
		minRarity = data.getInt("min_rarity", 0);
		maxRarity = data.getInt("max_rarity", 0);
		guaranteedPicks = data.getInt("guaranteed_picks", 0);
	}
}
