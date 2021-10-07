using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * PlaySymbolAnticipatonOnReelStopModule.cs
 * author: Leo Schnee
 * Plays the anticipation on the specified symbols when the reel it is on stops.
 * This is used for effects like the boob jiggle in Elvira04, to play a differnt animation when the symobls land,
 * because the server isn't sending down that we should play the jiggle on every spin.
 */
public class PlaySymbolAnticipatonOnReelStopModule : SlotModule 
{
	[SerializeField] private string symbolToAnimate = "";
	[Tooltip("true will use symbol names that contain the text symbolToAnimate  ex. the 'TW' in 'TWL' & 'TWR'")]
	[SerializeField] private bool includeSymbolNameContains = false;
	[SerializeField] private bool includeMegaSymbols = false;
	[SerializeField] private bool shouldBlockNextSpin = false;
	[Tooltip("Some anticipations might want to be played during the rollback instead of once the reel fully stops")]
	[SerializeField] private bool shouldExecuteOnEndRollback = false;
	[Tooltip("Seperate TW symbols might have different sounds that are ok to play overlapping ex. suicide01")]
	[SerializeField] private bool allowOverlappingSounds = false;
	[SerializeField] protected string symbolLandSoundKey;

	private int numberOfPlayingAnticipationAnimations = 0;

	protected virtual void onAnticipationAnimationDone(SlotSymbol sender)
	{
		numberOfPlayingAnticipationAnimations--;
	}
		
// executeOnSpecificReelStopping() section
// Functions here are executed during the OnSpecificReelStopping (in reelGame) as soon as stop is called, but before the reels completely to a stop.
	public override bool needsToExecuteOnSpecificReelStop(SlotReel stoppedReel)
	{
		return !shouldExecuteOnEndRollback;
	}
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel stoppedReel)
	{
		shouldPlaySymbolAnticipations(stoppedReel);
		yield break;
	}

// executeOnReelEndRollback() section
	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		return shouldExecuteOnEndRollback;
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		// This function doesn't block the spin from ending. Be wary of using it.
		shouldPlaySymbolAnticipations(reel);
		yield break;
	}

	// Handle playing the symbol anticipations
	protected void handleSymbolAnticipations(SlotReel reel)
	{
		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reel.reelID - 1))
		{
			if ((symbol.serverName == symbolToAnimate || ((includeMegaSymbols == true || includeSymbolNameContains == true) && symbol.serverName.Contains(symbolToAnimate))) 
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
					numberOfPlayingAnticipationAnimations++;
					symbol.animateAnticipation(onAnticipationAnimationDone);

					if (allowOverlappingSounds || numberOfPlayingAnticipationAnimations == 1)
					{	
						playSymbolLandSound(symbol);
					}
				}

			}
		}
	}

	// Allows derived modules to have control
	protected virtual void shouldPlaySymbolAnticipations(SlotReel reel)
	{
		handleSymbolAnticipations(reel);
	}
		
	// Allows derived modules to control what symbol is mutated to before animating, for instance if the symbol has different animations base on reel location
	protected virtual string getSymbolMutateName(SlotReel stoppedReel, string originalName)
	{
		// base just returns the same name and doesn't mutate
		return originalName;
	}

	// Allows derived modules to have control
	protected virtual void playSymbolLandSound(SlotSymbol symbol)
	{
		if (!string.IsNullOrEmpty(symbolLandSoundKey))
		{
			Audio.playSoundMapOrSoundKey(symbolLandSoundKey);
		}
	}

	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		while (shouldBlockNextSpin && numberOfPlayingAnticipationAnimations > 0)
		{
			yield return null;
		}
	}

	private bool isSymbolBehindStickyWild(SlotSymbol symbol)
	{
		return reelGame.isSymbolLocationCovered(symbol.reel, symbol.index);
	}
}