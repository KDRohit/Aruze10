using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
A static library of common functions for slots.
Because of Hyper Economy, it's needed to identify whether the label is to show credit.
If the input label ISN'T to show credits, please set isCredit to false.
*/

public static class SlotUtils
{
	private static float MAX_ROLLUP_TIME = 60.0f;
	
	// Common function for doing an interruptible rollup - UILabel overload.
	public static IEnumerator rollup(	long start, 
										long end, 
										UILabel label, 
										bool playSound = true, 
										float specificRollupTime = 0f, 
										bool shouldSkipOnTouch = true, 
										bool shouldBigWin = true, 
										string rollupOverrideSound = "",
										string rollupTermOverrideSound = "",
										bool isCredit = true,
										string symbolToAppend = "")
	{
		if (label == null)
		{
			Debug.LogWarning("Label is null. Breaking early to prevent an error");
			yield break;
		}

		yield return RoutineRunner.instance.StartCoroutine(rollup(
			start,
			end,
			new LabelWrapper(label),		// LabelWrapper
			null,							// RollupDelegate
			playSound,
			specificRollupTime,
			shouldSkipOnTouch,
			shouldBigWin,
			rollupOverrideSound,
			rollupTermOverrideSound,
			isCredit,
			symbolToAppend
		));
	}

	// Common function for doing an interruptible rollup - TextMeshPro overload.
	public static IEnumerator rollup(	long start, 
										long end, 
										TextMeshPro tmPro, 
										bool playSound = true, 
										float specificRollupTime = 0f, 
										bool shouldSkipOnTouch = true, 
										bool shouldBigWin = true, 
										string rollupOverrideSound = "",
										string rollupTermOverrideSound = "",
										bool isCredit = true,
										string symbolToAppend = "")
	{
		if (tmPro == null)
		{
			Debug.LogWarning("TmPro is null. Breaking early to prevent an error");
			yield break;
		}

		yield return RoutineRunner.instance.StartCoroutine(rollup(
			start,
			end,
			new LabelWrapper(tmPro),	// LabelWrapper
			null,						// RollupDelegate
			playSound,
			specificRollupTime,
			shouldSkipOnTouch,
			shouldBigWin,
			rollupOverrideSound,
			rollupTermOverrideSound,
			isCredit,
			symbolToAppend
		));
	}

	// Common function for doing an interruptible rollup - LabelWrapperComponent overload.
	public static IEnumerator rollup(	long start, 
										long end, 
										LabelWrapperComponent label, 
										bool playSound = true, 
										float specificRollupTime = 0f, 
										bool shouldSkipOnTouch = true, 
										bool shouldBigWin = true, 
										string rollupOverrideSound = "",
										string rollupTermOverrideSound = "",
										bool isCredit = true,
										string symbolToAppend = "")
	{
		if (label == null)
		{
			Debug.LogWarning("Label is null. Breaking early to prevent an error");
			yield break;
		}
		
		yield return RoutineRunner.instance.StartCoroutine(rollup(
			start,
			end,
			label.labelWrapper,				// LabelWrapper
			null,					// RollupDelegate
			playSound,
			specificRollupTime,
			shouldSkipOnTouch,
			shouldBigWin,
			rollupOverrideSound,
			rollupTermOverrideSound,
			isCredit,
			symbolToAppend
		));
	}

	// Common function for doing an interruptible rollup - RollupDelegate overload.
	public static IEnumerator rollup(	long start, 
										long end, 
										LabelWrapper label, 
										bool playSound = true, 
										float specificRollupTime = 0f, 
										bool shouldSkipOnTouch = true, 
										bool shouldBigWin = true, 
										string rollupOverrideSound = "",
										string rollupTermOverrideSound = "",
										bool isCredit = true)
	{
		yield return RoutineRunner.instance.StartCoroutine(rollup(
			start,
			end,
			label,		// LabelWrapper
			null,	// RollupDelegate
			playSound,
			specificRollupTime,
			shouldSkipOnTouch,
			shouldBigWin,
			rollupOverrideSound,
			rollupTermOverrideSound,
			isCredit
		));
	}

	// Common function for doing an interruptible rollup - RollupDelegate overload.
	public static IEnumerator rollup(	long start, 
										long end, 
										RollupDelegate callback, 
										bool playSound = true, 
										float specificRollupTime = 0f, 
										bool shouldSkipOnTouch = true, 
										bool shouldBigWin = true, 
										string rollupOverrideSound = "",
										string rollupTermOverrideSound = "",
										bool isCredit = true,
										string symbolToAppend = "")
	{
		yield return RoutineRunner.instance.StartCoroutine(rollup(
			start,
			end,
			null,		// LabelWrapper
			callback,	// RollupDelegate
			playSound,
			specificRollupTime,
			shouldSkipOnTouch,
			shouldBigWin,
			rollupOverrideSound,
			rollupTermOverrideSound,
			isCredit,
			symbolToAppend
		));
	}
	
	public static IEnumerator rollup(	long start, 
										long end, 
										LabelWrapper label, 
										RollupDelegate callback,
										bool playSound = true, 
										float specificRollupTime = 0f, 
										bool shouldSkipOnTouch = true, 
										bool shouldBigWin = true, 
										string rollupOverrideSound = "",  
										string rollupTermOverrideSound = "",
										bool isCredit = true,
										string symbolToAppend = "")
	{
		long payout = end - start;
		if (payout <= 0)
		{
			if (payout < 0)
			{
				Debug.LogError("Doing Rollup with an end value that is lower than the start value... Start: " + start + ", End: " + end);
			}
			yield break;
		}
		
		// If we're muted, don't play sound.
		if (playSound)
		{
			playSound = !Audio.muteSound;
		}
		
		// First determine the length of the rollup, which is partially based on the payout,
		// the bet multiplier (if in a game), and timing to sync with sound if sound is played.
		PlayingAudio rollupSound = null;		
		string soundKey = "rollup_default_loop";
		string endSoundKey = "rollup_default_end";
		long betAmount = 0;
		float rollupTime = specificRollupTime;
		if (SlotBaseGame.instance != null && SlotBaseGame.instance.betAmount > 0)
		{
			betAmount = SlotBaseGame.instance.betAmount;
		}
		else if (GameState.giftedBonus != null)
		{
			// We must be in a gifted bonus game.
			SlotGameData slotGameData = SlotGameData.find(GameState.giftedBonus.slotsGameKey);
			if (slotGameData != null)
			{
				//If we're in a gifted freespins then multiply by the multiplier so that we don't have really long rollup times
				if (FreeSpinGame.instance != null)
				{
					betAmount = slotGameData.baseWager * FreeSpinGame.instance.multiplier;
				}
				else if (ReelGame.activeGame != null)
				{
					betAmount = slotGameData.baseWager * ReelGame.activeGame.multiplier;
				}
				else
				{
					// Leaving this slotGameData.giftBetMultiplier check here, because it is needed to ensure
					// that rollup times don't become impossibly big for games like max01
					// @todo: (Scott) Once we move players fully to the spin time reduction experiment I think we can
					// do away with this.
					if (slotGameData.giftBetMultiplier > 1 && GiftedSpinsVipMultiplier.playerMultiplier > 1)
					{
						betAmount = slotGameData.baseWager * GiftedSpinsVipMultiplier.playerMultiplier; //Gifted challenge game case
					}
					else
					{
						if (end-start > (1000000000/CreditsEconomy.economyMultiplier)) //Need to get 1Billion in pre economy multiplier credits
						{
							betAmount = slotGameData.baseWager * 10000000; //Speed up rollups  over 1 billion
						}
						else
						{
							betAmount = slotGameData.baseWager * 1000000; //Gifted challenge game case
						}
					}
				}
			}
			else
			{
				Debug.LogError("Could not find SlotGameData for " + GameState.giftedBonus.slotsGameKey);
			}
		}
		
		if (betAmount > 0)
		{
			double adjustedBetAmount = betAmount;

			// Need to take into account if a multiplier override is in effect, and if so adjust timing for that
			// since that multiplier could cause very large numbers, but betAmount could be low if the user bet
			// low before triggering whatever is using the multiplier override
			if (BonusGameManager.isBonusGameActive && BonusGameManager.instance.isUsingBetMultiplierOverride())
			{
				// remove the normal game multiplier
				adjustedBetAmount /= ReelGame.activeGame.multiplier;
				// apply the override
				adjustedBetAmount *= BonusGameManager.instance.betMultiplierOverride;
			}
			else if (ReelGame.activeGame != null)
			{
				if (ReelGame.activeGame.reevaluationSpinMultiplierOverride > 0)
				{
					// remove the normal game multiplier
					adjustedBetAmount /= ReelGame.activeGame.multiplier;
					// apply the override
					adjustedBetAmount *= ReelGame.activeGame.reevaluationSpinMultiplierOverride;
				}
				else if (shouldBigWin && ReelGame.activeGame.outcome != null && ReelGame.activeGame.isOverBigWinThreshold((end - start) + ReelGame.activeGame.getCurrentRunningPayoutRollupValue()))
				{
					// for the big win let's check if any multiplier override was used in the payout,
					// and if so use that to calculate the time instead of the current bet amount.
					long largestMultiplierOverride = ReelGame.activeGame.outcome.getLargestBetMultiplierOverrideInOutcome();

					if (largestMultiplierOverride > 0)
					{
						// remove the normal game multiplier
						adjustedBetAmount /= ReelGame.activeGame.multiplier;
						// apply the largest override from the outcome
						adjustedBetAmount *= largestMultiplierOverride;
					}
				}
			}

			// Get our desired time
			rollupTime = getRollupTime(payout, adjustedBetAmount);
			
			// handle getting rollup sound
			if (rollupOverrideSound != "")
			{
				soundKey = rollupOverrideSound;
			}
			else if (ChallengeGame.instance != null)
			{
				soundKey = ChallengeGame.instance.getRollupSound(end - start);
			}
			else if (ReelGame.activeGame != null)
			{
				soundKey = ReelGame.activeGame.getRollupSound((end - start) + ReelGame.activeGame.getCurrentRunningPayoutRollupValue(), shouldBigWin);
			}
			
			// If the parameter is zero, see if we need to use an overrideSoundLength
			if (specificRollupTime == 0.0f)
			{
				float tmpSpecificRollupTime = -1.0f;

				if (ReelGame.activeGame != null)
				{
					tmpSpecificRollupTime = ReelGame.activeGame.getSpecificRollupTime(soundKey);
				}
				
				// The above function will return -1.0f if it has no override sound
				if (tmpSpecificRollupTime > 0.0f)
				{
					specificRollupTime = tmpSpecificRollupTime;
				}
			}
			
			// handle getting rollup term sound
			if (rollupTermOverrideSound != "")
			{
				endSoundKey = rollupTermOverrideSound;
			}
			else if (ChallengeGame.instance != null)
			{
				endSoundKey = ChallengeGame.instance.getRollupTermSound(end - start);
			}
			else if (ReelGame.activeGame != null)
			{
				endSoundKey = ReelGame.activeGame.getRollupTermSound((end - start) + ReelGame.activeGame.getCurrentRunningPayoutRollupValue(), shouldBigWin);
			}
			rollupTime = handleRollupSound(playSound, soundKey, specificRollupTime, rollupTime, out rollupSound);
		}
		else if (GameState.isMainLobby || ChallengeLobby.instance != null)
		{
			// If we are in the lobby use the generic rollup, non sound map call	
			rollupTime = (specificRollupTime > 0.0f ? specificRollupTime : 1.0f);
			soundKey = (rollupOverrideSound != "") ? rollupOverrideSound : "rollup_default_loop";
			endSoundKey = (rollupTermOverrideSound != "") ? rollupTermOverrideSound : "rollup_default_end";
			rollupTime = handleRollupSound(playSound, soundKey, specificRollupTime, rollupTime, out rollupSound);
		}

		// We have determined the rollup time and optionally started the rollup sound,
		// so do the actual rollup.

		float age = 0f;
		while (age < rollupTime)
		{
			int timeMultiplier = 1;
			
			if (TouchInput.isTouchDown)
			{
				timeMultiplier = 20;
			}
			
			age += (Time.deltaTime * timeMultiplier);
			
			yield return null;
						
			if (TouchInput.didTap && shouldSkipOnTouch)
			{
				// Skip the rollup by touching/clicking anywhere.
				age = rollupTime;
			}
			
			long rollupValue = start + (long)System.Math.Floor(Mathf.Clamp01(age / rollupTime) * payout);
			
			if (label != null)
			{
				// Convert credits for Hyper Economy if the label shows credit.
				if (isCredit)
				{
					label.text = CreditsEconomy.convertCredits(rollupValue) + symbolToAppend;
				}
				else
				{
					label.text = CommonText.formatNumber(rollupValue) + symbolToAppend;
				}
			}

			if (callback != null)
			{
				callback(rollupValue);
			}
		}

		// Make sure the value is set correctly, in the label and the callback just to be safe. There could have been some floating point error
		if (label != null)
		{
			// Convert credits for Hyper Economy if the label shows credit.
			if (isCredit)
			{
				label.text = CreditsEconomy.convertCredits(end) + symbolToAppend;
			}
			else
			{
				label.text = CommonText.formatNumber(end) + symbolToAppend;
			}
		}

		if (callback != null)
		{
			callback(end);
		}
		
		if (playSound && rollupSound != null)
		{
			// If the endSoundKey is null or empty by this point, we don't end the rollup sound
			if (!string.IsNullOrEmpty(endSoundKey))
			{
				// Play the end sound. The abort channel ensures the previous clip is ended by the end sound.
				Audio.playSoundMapOrSoundKey(endSoundKey);
				// Ensure the rollupSound stops.
				Audio.stopSound(rollupSound, 0);
			}
		}
	}
	
	/// Helper function for rollup() to handle playing sound if requested, and setting rollup time.
	private static float handleRollupSound(bool playSound, string soundKey, float specificRollupTime, float desiredRollupTime, out PlayingAudio rollupSound)
	{
		if (specificRollupTime > 0 && !playSound)
		{
			// If not playing a sound, and a specific rollup time was given, use exactly the given time.
			rollupSound = null;
			return specificRollupTime;
		}
		
		if (specificRollupTime > 0f)
		{
			desiredRollupTime = specificRollupTime;
		}

		float rollupTime = 0;
		if (playSound)
		{
			rollupSound = Audio.playSoundMapOrSoundKey(soundKey);

			if (rollupSound != null)
			{
				rollupTime = Mathf.Max(1f, Audio.getBeatDelay(rollupSound.audioInfo, desiredRollupTime));
			}
			else
			{
				// Sound didn't play, so fall back to the passed in value exactly.
				rollupTime = desiredRollupTime;
			}
		}
		else
		{
			rollupTime = Audio.getBeatDelay(soundKey, desiredRollupTime);
			rollupSound = null;
			if (rollupTime == 0)
			{
				// This happens if an AudioInfo object wasn't found for soundKey,
				// or the clip isn't prepared for playing.
				rollupTime = desiredRollupTime;
			}
		}
		
		return rollupTime;
	}

	public static float getRollupTime(long payout, double betAmount)
	{
		// Get linear rollup time time. (Based on how it's done on web).
		// use this for low win to bet ratios
		float rollupTime = Mathf.Ceil((float)((double)payout / betAmount)) * Glb.ROLLUP_MULTIPLIER;

		if (ExperimentWrapper.SpinTime.isInExperiment)
		{
			// Get a rollup time based on sqrt function to use for larger win to bet ratios.
			// This keeps really big win rollup times from growing out of control.
			// You can find how these variable affect rollupTime using this tool :
			// https://www.desmos.com/calculator/dj9od4swdf
			//
			// sqrtRollupTime = a * sqrt(b * x) + k
			//
			// k = rollupTimeModifier (EOS controlled)
			// b = payoutRatioModifier (EOS controlled)
			// a = Glb.ROLLUP_MULTIPLIER (conrolled in scat)
			// x = payoutRatio
			//
			// the experiment can be found here :
			// https://eos.zynga.com/development/#/experiment/projects/hit+it+rich/edit/hir_spin_time
			float payoutRatio = Mathf.Ceil((float)((double)payout / betAmount));
			float rollupTimeModifier = ExperimentWrapper.SpinTime.rollupTimeModifier;
			float payoutRatioModifier = ExperimentWrapper.SpinTime.payoutRatioModifier;
			float sqrtRollupTime = Glb.ROLLUP_MULTIPLIER * Mathf.Sqrt(payoutRatioModifier * payoutRatio);

			// linear rollup time will be faster for small win to bet ratio, so this will
			// keep small wins snappy. But as the ratio grows, sqrtRollupTime takes over
			// and keeps rollup times reasonable.
			if (sqrtRollupTime > 0 && sqrtRollupTime < rollupTime)
			{
				rollupTime = sqrtRollupTime;
			}
		}

		if (ExperimentWrapper.SpinTime.isInExperiment)
		{
			rollupTime *= (ExperimentWrapper.SpinTime.rollupTimePercentage / 100.0f);
		}
		
		// Add a sanity check to make sure this number never exceeds something
		// which would be completely unreasonable for the game to do (i.e. would
		// take so long that the player would just skip it anyways)
		if (rollupTime > MAX_ROLLUP_TIME)
		{
			rollupTime = MAX_ROLLUP_TIME;
		}

		return rollupTime;
	}
	
	// Sometime symbol names have extra info for multi-row usage.
	// This returns just the base symbol name if that extra info is passed in.
	public static string getBaseSymbolName(string symbolName)
	{
		if (symbolName.Contains('-'))
		{
			string[] parts = symbolName.Split('-');
			return parts[0];
		}
		return symbolName;
	}
}
