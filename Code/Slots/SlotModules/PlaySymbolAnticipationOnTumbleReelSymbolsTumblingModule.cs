using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Basically a tumbling version of PlaySymbolAnticipatonOnReelStopModule.  If you need the animations to play as the symbols tumble in you'll want to use this module.
See spam01 for an example.

Original Author: Scott Lepthien
Creation Date: May 15, 2017
*/
public class PlaySymbolAnticipationOnTumbleReelSymbolsTumblingModule : SlotModule 
{
	[SerializeField] private string symbolToAnimate = "";
	[SerializeField] private bool includeMegaSymbols = false;
	[SerializeField] private bool shouldBlockReelTumbleCoroutine = false;
	[SerializeField] private string symbolLandSoundKey;
	[Tooltip("Setting this false will cause symbols to animate again every time they tumble.")]
	[SerializeField] private bool isPlayingOnlyOncePerSymbol = true;

	private int numberOfPlayingAnticipationAnimations = 0;
	private List<SlotSymbol> symbolsAlreadyPlayed = new List<SlotSymbol>();

	protected virtual void onAnticipationAnimationDone(SlotSymbol sender)
	{
		numberOfPlayingAnticipationAnimations--;
	}

// executeOnPreSpin() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		symbolsAlreadyPlayed.Clear();
		yield break;
	}

	public override bool needsToExecuteBeforeCleanUpWinningSymbolInTumbleReel(SlotSymbol symbol)
	{
		return symbolsAlreadyPlayed.Contains(symbol);
	}

	public override IEnumerator executeBeforeCleanUpWinningSymbolInTumbleReel(SlotSymbol symbol)
	{
		// remove symbols that are being removed from the reels form the list of ones we've already played
		symbolsAlreadyPlayed.Remove(symbol);
		yield break;
	}

// Function that executes once all symbols are setup to tumble in, used to mimic certain symbol anticipations in tumble games
// where certain reel concepts don't exist/or are a bit different, first used in spam01 for the wild multiplier sounds
	public override bool needsToExecuteOnTumbleReelSymbolsTumbling(TumbleReel reel)
	{
		return true;
	}

	public override IEnumerator executeOnTumbleReelSymbolsTumbling(TumbleReel reel)
	{
		yield return StartCoroutine(handleSymbolAnticipations(reel));
	}

	// Handle playing the symbol anticipations
	protected IEnumerator handleSymbolAnticipations(TumbleReel reel)
	{
		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reel.reelID - 1))
		{
			// NOTE: sub symbols could be an issue with the below check using serverName, 
			// if we ever have sub-symbols that have symbols that will use this module we'll 
			// have to make sure things work as expected.
			if ((symbol.serverName == symbolToAnimate || (includeMegaSymbols == true && symbol.serverName.Contains(symbolToAnimate))) 
				&& !isSymbolBehindStickyWild(symbol))
			{
				// Play the animation
				if (!symbol.isAnimatorDoingSomething)
				{
					// Check if we need to mutate before animating
					string mutatedSymbolName = getSymbolMutateName(reel, symbol.serverName);
					if (mutatedSymbolName != symbol.serverName)
					{
						SymbolInfo mutateSymbolInfo = reelGame.findSymbolInfo(mutatedSymbolName);
						string originalName = symbol.serverName;

						if (mutateSymbolInfo != null)
						{
							symbol.mutateTo(mutatedSymbolName);
							symbol.debugName = originalName;
						}
						else
						{
							Debug.LogWarning("PlaySymbolAnticipatonOnReelStopModule.executeOnSpecificReelStop() - Got mutatedSymbolName = " + mutatedSymbolName + " but couldn't find SymbolInfo for it!");
						}
					}

					// Don't animate something twice.
					if (!isPlayingOnlyOncePerSymbol || !symbolsAlreadyPlayed.Contains(symbol))
					{
						numberOfPlayingAnticipationAnimations++;
						symbol.animateAnticipation(onAnticipationAnimationDone);
						if (!string.IsNullOrEmpty(symbolLandSoundKey) && numberOfPlayingAnticipationAnimations == 1)
						{
							Audio.playSoundMapOrSoundKey(symbolLandSoundKey);
						}

						symbolsAlreadyPlayed.Add(symbol);
					}
				}
			}
		}

		if (shouldBlockReelTumbleCoroutine)
		{
			// wait for the animations to finish
			while (numberOfPlayingAnticipationAnimations > 0)
			{
				yield return null;
			}
		}
	}

	// Allows derived modules to contorl what symbol is mutated to before animating, for instance if the symbol has different animations base on reel location
	protected virtual string getSymbolMutateName(SlotReel stoppedReel, string originalName)
	{
		// base just returns the same name and doesn't mutate
		return originalName;
	}

	private bool isSymbolBehindStickyWild(SlotSymbol symbol)
	{
		return reelGame.isSymbolLocationCovered(symbol.reel, symbol.index);
	}
}
