using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Class very similar to AinsworthRollupSoundOverrideModule.cs except uses bet multiplier float values
which are multiplied by the bet amount and then compared with the payout to determine what rollup sound to play

Original Author: Scott Lepthien

Creation Date: April 12, 2017
*/
public class BetMultiplierThresholdRollupSoundOverrideModule : SlotModule
{
	[SerializeField] private float[] multiplierThresholds;
	[SerializeField] private string[] payoutSoundNames;
	[SerializeField] private bool useCustomPayoutSoundLengths = true;
	
	private Dictionary<string, float> soundKeysToLengths = new Dictionary<string, float>();

	public string getSoundKeyForPayout(long payout)
	{
		if (soundKeysToLengths == null || soundKeysToLengths.Count == 0)
		{
			initSoundLengthLookupDictionary();
		}

		// This module does not use cumulative bonus values (ie, in freespins) - correct for that
		payout -= ReelGame.activeGame.getCurrentRunningPayoutRollupValue();
		
		return getSoundKeyForBetMultipliersThresholds(payout);
	}

	// Initialize the sound length lookup table
	private void initSoundLengthLookupDictionary()
	{
		if (soundKeysToLengths == null || soundKeysToLengths.Count == 0)
		{
			if (payoutSoundNames == null)
			{
				Debug.LogError("BetMultiplierThresholdRollupSoundOverrideModule.initSoundLengthLookupDictionary() - payoutSoundNames was null, nothing to init!");
			}
			else
			{
				for (int i = 0; i < payoutSoundNames.Length; i++)
				{
					string key = payoutSoundNames[i];
					soundKeysToLengths.Add(key, Audio.getAudioClipLength(key));
				}
			}
		}
	}

	// Function to get the specific sound key for the rollup using bet multipliers thresholds
	// which are calculated by comparing the payout against the wager amount multiplied by the thresholds
	private string getSoundKeyForBetMultipliersThresholds(long payout)
	{
		if (multiplierThresholds == null || multiplierThresholds.Length == 0)
		{
			Debug.LogError("BetMultiplierThresholdRollupSoundOverrideModule.getSoundKeyForBetMultipliersThresholds() - multiplierThresholds not set!");
			return "";
		}

		string currentSoundName = payoutSoundNames[0];
		
		if (ReelGame.activeGame != null && ReelGame.activeGame.outcome != null)
		{
			// Compare the adjustedPayout to each threshold value in order to determine the correct sound to play.
			for (int i = 0; i < multiplierThresholds.Length; i++)
			{
				if (payout >= (multiplierThresholds[i] * reelGame.betAmount))
				{
					if (i < payoutSoundNames.Length)
					{
						currentSoundName = payoutSoundNames[i];
					}
				}
				else
				{
					return currentSoundName;
				}
			}
		}
		
		return currentSoundName;
	}
	
	// Get a cached sound length for one of the rollup sounds
	private float getSoundLengthForKey(string soundKey)
	{
		if (soundKeysToLengths == null || soundKeysToLengths.Count == 0)
		{
			initSoundLengthLookupDictionary();
		}

		float len = -1.0f;
		soundKeysToLengths.TryGetValue(soundKey, out len);
		return len;
	}
	
	public override bool needsToExecuteRollupSoundOverride(long payout, bool shouldBigWin)
	{
		return true;
	}
	
	public override bool needsToExecuteRollupTermSoundOverride(long payout, bool shouldBigWin)
	{
		return true;
	}

	public override string executeRollupTermSoundOverride(long payout, bool shouldBigWin)
	{
		return "null_fx";
	}
	
	public override bool needsToExecuteRollupSoundLengthOverride()
	{
		return useCustomPayoutSoundLengths;
	}
	
	public override float executeRollupSoundLengthOverride(string soundKey)
	{
		return getSoundLengthForKey(soundKey);
	}

	public override string executeRollupSoundOverride(long payout, bool shouldBigWin)
	{
		return getSoundKeyForPayout(payout);
	}

	public override float executeRollupSoundLengthOverride(long payout)
	{
	    return getSoundLengthForKey(getSoundKeyForPayout(payout));
	}
}
