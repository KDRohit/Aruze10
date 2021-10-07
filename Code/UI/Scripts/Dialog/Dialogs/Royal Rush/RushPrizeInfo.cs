﻿using System;
using UnityEngine;
using System.Collections.Generic;

// This class contains information about a prize for royal rush

public class RoyalPrizeInfo
{
	public int rankMin; // the lowest number rank. So rank 1 - 12 this would be 1.
	public int rankMax; // The highest number ranked. 1-12 this would be 12
	public long creditsAwardAmount;

	public RoyalPrizeInfo(JSON prizeInfo)
	{
		// These are base 0, so add 1.
		rankMin = prizeInfo.getInt("rank_high", 0) + 1;
		rankMax = prizeInfo.getInt("rank_low", 0) + 1;
		creditsAwardAmount = prizeInfo.getLong("credits", 0);
	}

}
