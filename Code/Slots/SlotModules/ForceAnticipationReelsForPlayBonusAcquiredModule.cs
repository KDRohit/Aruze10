using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Module which will force reels containing bonus symbols that are triggering a specific bonus to be flagged as anticipation reels after the spin ends
that way the bonus acquired effects can play correctly, this module was first used for aruze02 Extreme Dragon.
NOTE: Since this will flag the reels after the reels stop, this will not make the symbols do actual anticipation stuff, and 
this module is mainly intended for games taht don't do anticipations, but still need to play bonus acquired effects

Original Author: Scott Lepthien
Creation Date: 3/29/2017
*/
public class ForceAnticipationReelsForPlayBonusAcquiredModule : SlotModule 
{
	[SerializeField] private bool isTriggeringForFreespins = false;
	[SerializeField] private bool isTriggeringForChallengeGame = false;
	[SerializeField] private bool isTriggeringForPortal = false;
	[SerializeField] private string bonusSymbolName = "BN";
	[SerializeField] private bool isPlayingBonusAcquiredEffectsInThisModule = false;
	[SerializeField] private string bonusAcquiredAudioOverride = ""; // since bonus_symbol_animate key plays in freespin init, override here and disable there to play before animations (feature bell)

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		if (reelGame.outcome != null)
		{
			return (reelGame.outcome.isGifting && isTriggeringForFreespins) 
				|| (reelGame.outcome.isChallenge && isTriggeringForChallengeGame) 
				|| (reelGame.outcome.isPortal && isTriggeringForPortal);
		}
		else
		{
			return false;
		}
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		// go through all of the reels and flag any with bonus symbols as being anticipation reels
		changeReelsWithBonusSymbolToAnticipationReels();

		if (!string.IsNullOrEmpty(bonusAcquiredAudioOverride))
		{
			Audio.playSoundMapOrSoundKey(bonusAcquiredAudioOverride);
		}

		yield break;
	}

// executeOnContinueToBasegameFreespins() section
// functions in this section are executed when SlotBaseGame.continueToBasegameFreespins() is called to start freespins in base
	public override bool needsToExecuteOnContinueToBasegameFreespins()
	{
		if (reelGame.outcome != null)
		{
			// only check for freespins for freespin in base
			return (reelGame.outcome.isGifting && isTriggeringForFreespins);
		}
		else
		{
			return false;
		}
	}

	public override IEnumerator executeOnContinueToBasegameFreespins()
	{
		// go through all of the reels and flag any with bonus symbols as being anticipation reels
		changeReelsWithBonusSymbolToAnticipationReels();

		if (isPlayingBonusAcquiredEffectsInThisModule && reelGame is SlotBaseGame)
		{
			SlotBaseGame baseGame = reelGame as SlotBaseGame;
			yield return StartCoroutine(baseGame.doPlayBonusAcquiredEffects());
		}
		else
		{
			yield break;
		}
	}

	// Changes reels into anticipation reels if they have a matching bonus symbol on them
	private void changeReelsWithBonusSymbolToAnticipationReels()
	{
		for (int reelIndex = 0; reelIndex < reelGame.engine.getReelRootsLength(); reelIndex++)
		{
			SlotReel reel = reelGame.engine.getSlotReelAt(reelIndex);
			checkIfReelShouldBeAnticipationReel(reel);
		}
	}

	// checks if a reel should be an anticipation reel, returns if it turned the reel into on or not
	private bool checkIfReelShouldBeAnticipationReel(SlotReel reel)
	{
		for (int i = 0; i < reel.visibleSymbols.Length; i++)
		{
			SlotSymbol symbol = reel.visibleSymbols[i];
			if (symbol.shortServerName == bonusSymbolName)
			{
				reel.setAnticipationReel(true);
				return true;
			}
		}

		return false;
	}
}
