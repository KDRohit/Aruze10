using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Module which keeps track of and populates jackpot ladder style games based on paytable data
 */

[System.Serializable]
public class JackpotLadderAnimationData
{
	[SerializeField] public JackpotLabel[] jackpotLaderRanks;

	// Goes through all labels and increases rank for each unavailable label it finds.
	//	In practice labels becaome unavailable ind ascending order as multipliers are revealed.
	public int getCurrentRankIndex()
	{
		int rank = 0;
		foreach(JackpotLabel label in jackpotLaderRanks)
		{
			if (!label.isAvailable)
			{
				rank++;
			}
			else
			{
				break;
			}
		}
		return rank;
	}

	public JackpotLabel getRankLabel(int rankIndex)
	{
		if (jackpotLaderRanks.Length > 0 && rankIndex < jackpotLaderRanks.Length)
		{
			return jackpotLaderRanks[rankIndex];
		}
		return null;
	}

	public JackpotLabel getCurrentRankLabel()
	{
		int rankIndex = getCurrentRankIndex();
		if (jackpotLaderRanks.Length > 0 && rankIndex < jackpotLaderRanks.Length)
		{
			return jackpotLaderRanks[rankIndex];
		}
		return null;
	}

	public void updateJackpotLabelCredits(int rankIndex, long credits)
	{
		if (jackpotLaderRanks[rankIndex].label != null)
		{
			jackpotLaderRanks[rankIndex].label.text = CreditsEconomy.convertCredits(credits);
		}
	}

	public int getNumRanks()
	{
		return jackpotLaderRanks.Length;
	}
}
