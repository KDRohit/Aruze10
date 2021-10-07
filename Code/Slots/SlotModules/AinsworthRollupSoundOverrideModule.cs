using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Class originally made for ainsworth games which implements specific rollup sounds based on whether the payout is over
specific payout threshold brackets

HIR-66593 - Fixing this class' configuration to match actual ainsworth games' rollups correctly
Wager vs. Payout table provided by Ainsworth:
-------------------------------
Payout Return       / Tune
-------------------------------
(0.0x, 0.5x)           / Melody1AW
[0.51x, 1.0x)       / Melody2AW
[1.01x, 1.5x)       / Melody3AW
[1.51x, 2.0x)       / Melody4AW
[2.01x, 3.0x)       / Melody5AW
[3.01x, 4.0x)       / Melody6AW
[4.01x, 5.0x)       / Melody7AW
[5.01x, 6.5x)       / Melody8AW
[6.51x, 7.0x)       / Melody9AW
[7.01x, 8.0x)       / Melody10AW
[8.01x, 9.0x)       / Melody11AW
[9.01x, 10.0x)      / Melody12AW
[10.01x, +infinity] / Melody13AW

Original Author: Mike Cabral
*/
public class AinsworthRollupSoundOverrideModule : SlotModule
{
	// these are a factor of return on wager
	[SerializeField] private float[] payoutThresholds = {
		0.5f,
		1.0f,
		1.5f,
		2.0f,
		3.0f,
		4.0f,
		5.0f,
		6.0f,
		7.0f,
		8.0f,
		9.0f,
		10.0f
	};
	
	[SerializeField] private string[] payoutSoundNames = {
		"Melody1AW",
		"Melody2AW",
		"Melody3AW",
		"Melody4AW",
		"Melody5AW",
		"Melody6AW",
		"Melody7AW",
		"Melody8AW",
		"Melody9AW",
		"Melody10AW",
		"Melody11AW",
		"Melody12AW",
		"Melody13AW"
	};
	
	[SerializeField] private bool useCustomPayoutSoundLengths = true;
	
	private Dictionary<string, float> soundKeysToLengths = new Dictionary<string, float>();
	
	public string getSoundKeyForPayout(long payout)
	{
		if (soundKeysToLengths == null || soundKeysToLengths.Count == 0)
		{
			initSoundLengthLookupDictionary();
		}

		return getSoundKeyForPayoutThresholds(payout);
	}

	// Initialize the sound length lookup table
	private void initSoundLengthLookupDictionary()
	{
		if (soundKeysToLengths == null || soundKeysToLengths.Count == 0)
		{
			for (int i = 0; i < payoutSoundNames.Length; i++)
			{
				string key = payoutSoundNames[i];
				soundKeysToLengths.Add(key, Audio.getAudioClipLength(key));
			}
		}
	}

	// Function to get the specific sound key for the rollup using payout thresholds
	// which are calculated by comparing the wager return against the payoutThresholds
	private string getSoundKeyForPayoutThresholds(long payout)
	{
		string soundKey = "";
		long adjustedPayout = payout;

		if(payoutSoundNames != null && payoutSoundNames.Length > 0 &&
		   payoutThresholds != null && payoutThresholds.Length > 0)
		{
			soundKey = payoutSoundNames[payoutSoundNames.Length - 1]; // default sound, highest level
		
			if (ReelGame.activeGame != null && ReelGame.activeGame.outcome != null)
			{
				int payoutLevel = 0; // final index used to look up sound in payoutSoundNames
				if (ReelGame.activeGame.isFreeSpinGame())
				{
					// in freespins, each spin's payout should be evaluated as opposed to the total running winnings
					adjustedPayout -= BonusGamePresenter.instance.currentPayout;
				}

				// find the correct wager
				float currentWager = 0.0f;
				if (SlotBaseGame.instance != null)
				{
					// use the base game's wager amount
					currentWager = SlotBaseGame.instance.currentWager;
				}
				else
				{
					// if there isn't a base game (gifted spins), look up base wager for game
					SlotGameData gameData = SlotGameData.find(GameState.game.keyName);
					if (gameData != null)
					{
						currentWager = gameData.baseWager * GiftedSpinsVipMultiplier.playerMultiplier;
					}
					else
					{
						Debug.LogWarning("AinsworthRollupSoundOverrideModule:getSoundKeyForPayoutThresholds - No game data found for " + GameState.game.keyName);
					}
				}

				float wagerReturn = (currentWager > 0) ? adjustedPayout / currentWager : 0; // total return on wager
				for (int i = 0; i < payoutThresholds.Length; i++)
				{
					if (wagerReturn < payoutThresholds[i])
					{
						break; // threshold found
					}
					payoutLevel++;
				}

				// make sure the payout level exists
				if (payoutLevel <= payoutSoundNames.Length - 1)
				{
					soundKey = payoutSoundNames[payoutLevel];
				}
				else
				{
					Debug.LogWarning("AinsworthRollupSoundOverrideModule:getSoundKeyForPayoutThresholds - The determined payout level " + payoutLevel + " didn't exist in payoutSoundNames.");
				}
			}
		}
		
		
		return soundKey;
	}
	
	// Get a cached sound length for one of the rollup sounds
	public float getSoundLengthForKey(string soundKey)
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
		return reelGame.isFreeSpinGame();
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
}
