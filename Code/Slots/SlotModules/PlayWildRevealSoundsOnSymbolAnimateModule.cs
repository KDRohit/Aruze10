using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Originally made to handle tumble games like spam01 that have multiplier wild symbols that fall in and make sound effects/vos when they are part of a win.

Original Author: Scott Lepthien
Creation Date: May 11, 2017
*/
public class PlayWildRevealSoundsOnSymbolAnimateModule : SlotModule 
{
	[SerializeField] private string wildSymbolName = "W2";
	[SerializeField] private AudioListController.AudioInformationList soundEffectListToPlay;
	[SerializeField] private AudioListController.AudioInformationList voListToPlay;
	[SerializeField] private bool isPlayingSoundEffectsOncePerSpin = false;
	[SerializeField] private bool delayRollupOnWildWin = false;
	[SerializeField] private float rollupDelay = 0.0f;
	
	private bool isVoAlreadyPlayed = false;
	private bool isPlayedOnceForThisSpin = false;
	private List<SlotSymbol> wildSymbolsAlreadyPlayed = new List<SlotSymbol>();

// executeOnPreSpin() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		isVoAlreadyPlayed = false;
		isPlayedOnceForThisSpin = false;
		wildSymbolsAlreadyPlayed.Clear();
		yield break;
	}

// For example, in wicked01 Free Spins,
// if you get a 2x wild on the pay line,
// play a sound effect every time it animates the 2x wild symbol\
	public override bool needsToPlaySymbolSoundOnAnimateOutcome(SlotSymbol symbol)
	{	
		if (symbol.name.Contains(wildSymbolName) && !wildSymbolsAlreadyPlayed.Contains(symbol))
		{
			if (isPlayingSoundEffectsOncePerSpin && isPlayedOnceForThisSpin)
			{
				return false;
			}
			else
			{
				return true;	
			}
		}
		else
		{
			return false;
		}
		
	}

	public override void executePlaySymbolSoundOnAnimateOutcome(SlotSymbol symbol)
	{
		if (!isVoAlreadyPlayed && voListToPlay != null && voListToPlay.Count > 0)
		{
			StartCoroutine(AudioListController.playListOfAudioInformation(voListToPlay));
		}

		if (soundEffectListToPlay != null && soundEffectListToPlay.Count > 0)
		{
			StartCoroutine(AudioListController.playListOfAudioInformation(soundEffectListToPlay));
			wildSymbolsAlreadyPlayed.Add(symbol);
		}

		isPlayedOnceForThisSpin = true;
	}
	
	public override bool needsToOverrideRollupDelay()
	{
		if (!delayRollupOnWildWin)
		{
			return false;
		}
		else
		{
			// Check for the desired wild symbol present in a payline.
			bool winningWild = false;

			HashSet<SlotSymbol> winningSymbols = reelGame.outcomeDisplayController.getSetOfWinningSymbols(reelGame.outcome);
			foreach (SlotSymbol winningSymbol in winningSymbols)
			{
				if (winningSymbol.name.Contains(wildSymbolName))
				{
					winningWild = true;
				}
			}
			return winningWild;
		}
		
	}

	public override float getRollupDelay()
	{
		return rollupDelay;
	}
}
