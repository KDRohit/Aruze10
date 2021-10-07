using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Special version of PlaySymbolSoundOnReelStopModule made for the aruze games with WB symbols which can match BN symbols
basically tracks the number of bonus symbols hit and plays BN sounds only when they are valid
*/
public class Aruze10PlayBonusSymbolSoundOnReelStopModule : PlaySymbolSoundOnReelStopModule 
{
	[SerializeField] private string[] wildBonusMatchingSymbols; // a list of symbols which aren't BN symbols but can match them as wilds, for instance in aruze02 a WB symbol
	[SerializeField] private int minNumberBonusHitsRequiredForBonus = 3; // the minimum number of bonus hits required to trigger the bonus game

	[SerializeField] private bool muteReelStopOnBonusSymbol = false; // set true to disable the default reel stop and only play bonus landing (azure02)
	[SerializeField] private float LANDING_AUDIO_DELAY = 0.5f; // the desired audio trigger lies *after* the reels "stopping" and *before* the reels "stopped"

	[SerializeField] private float ROLLUP_DELAY_ON_BONUS = 1.25f; // delay the rollup by a custom amount to allow for the feature bell timing

	private int numReelsStopped = 0; // number of reels stopped so far
	private int breakOnReel = -1; // in aruze the bonus symbols have to start from the left and count up, if a reel doesn't have a bonus symbol on it we can stop counting
	private bool bonusTriggered = false; // track whether we've hit an adequate number of symbols to trigger a bonus

	public override IEnumerator executeOnPreSpin()
	{
		hitCount = 0;
		numReelsStopped = 0;
		breakOnReel = -1;
		bonusTriggered = false;
		yield break;
	}

	public override bool needsToExecuteOnSpecificReelStopping(SlotReel reel)
	{
		numReelsStopped++;
		bool bonusSymbolLanding = false;
		bool isNumBonusReelsIncrementedForReel = false;

		// only match BN symbols on the first reel, include WD on others
		bool matchWildSymbols = reel.reelID > 0;

		string[] stopSymbolNames = reel.getFinalReelStopsSymbolNames();
		foreach (string symbolName in stopSymbolNames)
		{
			if (symbolNameMatchesAnyBonusSymbol(symbolName, matchWildSymbols))
			{
				hitCount++;
				isNumBonusReelsIncrementedForReel = true;
				bonusSymbolLanding = true;
				break;
			}
		}

		if (!isNumBonusReelsIncrementedForReel && breakOnReel < 0)
		{
			//breakOnReel = reel.reelID;
		}

		return bonusSymbolLanding;
	}

	public override void executeOnSpecificReelStopping(SlotReel reel)
	{
		if (muteReelStopOnBonusSymbol && shouldPlayBonusSound(reel))
		{
			reel.shouldPlayReelStopSound = false;
			reel.reelStopSoundOverride = "";
		}

		handleBonusSymbolAudio(reel);
	}

	protected override void handleSymbolAnticipations(SlotReel reel)
	{
		// do nothing, sounds should be played on reels "stopping" - avoid extra hitCount increments
	}

	// Handle playing the symbol landing audio
	protected void handleBonusSymbolAudio(SlotReel reel)
	{
		bool isSoundPlayed = false;

		// only match BN symbols on the first reel, include WD on others
		bool matchWildSymbols = reel.reelID > 0;

		string[] stopSymbolNames = reel.getFinalReelStopsSymbolNames();
		foreach (string symbolName in stopSymbolNames)
		{
			if (symbolNameMatchesAnyBonusSymbol(symbolName, matchWildSymbols))
			{
				if (!string.IsNullOrEmpty(symbolLandSoundKey) && shouldPlayBonusSound(reel))
				{
					if (!isSoundPlayed)
					{
						string currentLandSoundKey = symbolLandSoundKey;
						if (isAddingHitCountToSound)
						{
							currentLandSoundKey = hitCount+"_"+currentLandSoundKey;
						}

						Audio.playSoundMapOrSoundKeyWithDelay(currentLandSoundKey, LANDING_AUDIO_DELAY);

						// only play one sound per reel
						isSoundPlayed = true;
					}
				}
			}
		}
	}

	private bool symbolNameMatchesAnyBonusSymbol(string symbolServerName, bool matchWildSymbols)
	{
		if (symbolServerName == symbolToPlaySoundFor || (includeMegaSymbols == true && symbolServerName.Contains(symbolToPlaySoundFor)))
		{
			return true;
		}

		if (matchWildSymbols)
		{
			// see if the name matches any of the wild bonus matching symbols
			for (int i = 0; i < wildBonusMatchingSymbols.Length; i++)
			{
				string wildMatchSymbolName = wildBonusMatchingSymbols[i];

				if (symbolServerName == wildMatchSymbolName || (includeMegaSymbols == true && symbolServerName.Contains(wildMatchSymbolName)))
				{
					return true;
				}
			}
		}

		return false;
	}

	// Checks if the bonus sound should be played based on how many hits have occured so far and how many reels remain to be stopped
	private bool shouldPlayBonusSound(SlotReel reel)
	{
		if (breakOnReel >= 0 && reel.reelID >= breakOnReel)
		{
			return false;
		}
		else
		{
			SlotReel[] reelArray = reelGame.engine.getReelArray();
			int reelCount = reelArray.Length;
			int remainingReels = reelCount - numReelsStopped;

			// determine if we are going to trigger a bonus
			if (hitCount >= minNumberBonusHitsRequiredForBonus)
			{
				bonusTriggered = true;
			}

			// determine if the current symbol should have bonus landing sound
			if (hitCount + remainingReels >= minNumberBonusHitsRequiredForBonus)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}

	public override bool needsToOverrideRollupDelay()
	{
		return bonusTriggered;
	}

	public override float getRollupDelay()
	{
		return ROLLUP_DELAY_ON_BONUS;
	}
}
