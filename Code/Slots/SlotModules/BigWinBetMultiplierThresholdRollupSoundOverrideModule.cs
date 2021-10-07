using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/**
Class for handling specific big win rollup loop and term sounds based on what the bet amount
was with respect to how much was won. Other than using different sounds the rollup sounds still
function as normal as far as timing.  Originally made for Aruze04 Goddesses Hera.

Original Author: Scott Lepthien

Creation Date: January 4, 2018
*/
public class BigWinBetMultiplierThresholdRollupSoundOverrideModule : SlotModule 
{
	[System.Serializable]
	public class BigWinBetMultiplierThreshold
	{
		public float multiplierThreshold;
		public string rollupLoopSoundName;
		public string rollupTermSoundName;
	}

	// Sorting function for sorting the threshold data values from the highest threshold to the lowest
	private int compareBigWinBetMultiplierThresholds(BigWinBetMultiplierThreshold lhs, BigWinBetMultiplierThreshold rhs)
	{
		return rhs.multiplierThreshold.CompareTo(lhs.multiplierThreshold);
	}

	[SerializeField] private BigWinBetMultiplierThreshold[] thresholds;

	public override void Awake()
	{
		base.Awake();

		// sort the thresholds so that we know that they are in the order we expect
		Array.Sort(thresholds, compareBigWinBetMultiplierThresholds);
	}

	// Get the rollup loop sound for a specific payout
	private string getRollupLoopSoundKeyForPayout(long payout)
	{
		BigWinBetMultiplierThreshold thresholdData = getThresholdDataForPayout(payout);
		
		if (thresholdData != null)
		{
			return thresholdData.rollupLoopSoundName;
		}
		else
		{
			return "";
		}
	}

	// Get the rollup term sound for a specific payout
	private string getRollupTermSoundKeyForPayout(long payout)
	{
		BigWinBetMultiplierThreshold thresholdData = getThresholdDataForPayout(payout);
		
		if (thresholdData != null)
		{
			return thresholdData.rollupTermSoundName;
		}
		else
		{
			return "";
		}
	} 

	// Return the threshold data entry for a specific payout
	private BigWinBetMultiplierThreshold getThresholdDataForPayout(long payout)
	{
		if (thresholds == null || thresholds.Length == 0)
		{
			Debug.LogError("thresholds.getSoundKeyForBetMultipliersThresholds() - multiplierThresholds not set!");
			return null;
		}

		// Compare the adjustedPayout to each threshold value in order to determine the correct sound to play.
		for (int i = 0; i < thresholds.Length; i++)
		{
			if (payout >= (thresholds[i].multiplierThreshold * reelGame.betAmount))
			{
				return thresholds[i];
			}

			// if this is the last entry, then we have gone through all the high thresholds and now have to default to the lowest
			if (i == thresholds.Length - 1)
			{
				return thresholds[i];
			}
		}

		return null;
	}

	public override bool needsToExecuteRollupSoundOverride(long payout, bool shouldBigWin)
	{
		return shouldBigWin;
	}

	public override string executeRollupSoundOverride(long payout, bool shouldBigWin)
	{
		return getRollupLoopSoundKeyForPayout(payout);
	}
	
	public override bool needsToExecuteRollupTermSoundOverride(long payout, bool shouldBigWin)
	{
		return shouldBigWin;
	}

	public override string executeRollupTermSoundOverride(long payout, bool shouldBigWin)
	{
		return getRollupTermSoundKeyForPayout(payout);
	}
}
