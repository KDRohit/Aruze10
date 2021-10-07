using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/**
MultiSlotEngine
Class to manage the spinning of a collection of reels, and report back to the owner what the current reel motion state is.  

Specifically this is the engine for 'Multi Slot Games' such as gwtw01, and hi03 that use 'layers' as a way of differentiating
between different games (that are all visible at once) rather than actually layering behavior.

This class is designed to be usable by either a "base" slot game or a free spin bonus game.  As such it does not handle win results or user input.
It gets referenced by OutcomeDisplayController so that the reel symbols can be animated when wins are being processed.

Contains an array of SlotReel instances.  Each of these manages the movement of a vertical group of SlotSymbols.
SlotGameData is needed from global data because it contains specifiers on symbol size, movement speed, etc.
ReelSetData includes info on how many reels there are and what reel strips to use.  Note that due to the "tiers" payout management system,
the ReelSetData can change during play and should not result in an instantaneous reel instance rebuild.  Rather, the new symbol strips get
fed in during the next spin.

The state progression goes:
Stopped: Nothing is moving.  When entering this state, _reelsStoppedDelegate gets triggered.
BeginSpin: If all reels start spinning simultaneously, this state is skipped.  Otherwise this state processes the delay timer to start spinning each reel.
Spinning: All reels have been told to start spinning.
EndSpin: The reels are told to stop, Remains in this state until every reel has stopped moving.
		 Note that while in this state, a SlamStop will eliminate the delays between when reels are told to stop.  Reel Rollbacks still need to
		 complete before leaving this state.
*/
public class MultiSlotEngine : LayeredSlotEngine
{
	Dictionary<int, int[]> layeredAnticipationSounds;

	public MultiSlotEngine(ReelGame reelGame, string freeSpinsPaytableKey = "") : base(reelGame, true, freeSpinsPaytableKey)
	{
		
	}

	// The visible reels that are in each reel position.
	// getReelArray override's can be costly functions; do NOT call repeatedly in tight loops
	public override SlotReel[] getReelArray()
	{
		if (reelLayers == null || reelLayers.Length == 0)
		{
			Debug.LogError("reelLayers are not set, cannot get reelArray.");
			return null;
		}
		return reelLayers[0].getReelArray();
	}

	// Get the reel array that comprises the specified layer
	// getReelArray override's can be costly functions; do NOT call repeatedly in tight loops
	public override SlotReel[] getReelArrayByLayer(int layer)
	{
		return reelLayers[layer].getReelArray();
	}

	// Override so we can clean up the symbols that aren't visible because a layer above them is them covering up.
	protected override void callReelsStoppedCallback()
	{
		linkedReelsOverride.Clear();
		
		if (_reelsStoppedDelegate != null)
		{
			_reelsStoppedDelegate();
		}
	}


	public override int getVisibleSymbolsCountAt(SlotReel[] reelArray,int reelID, int layer = -1)
	{
		SlotSymbol[] slotsymbols = getVisibleSymbolsAt(reelID, layer);
		return 	(slotsymbols!=null) ? slotsymbols.Length : 0;
	}

	// for now, automatically return lowest layer symbols
	public override SlotSymbol[] getVisibleSymbolsAt(int reelID, int layer = -1)
	{
		if (layer == -1)
		{
			// -1 doesn't make sense in the context of mutiengine games, just defualt to the 0 layer game.
			layer = 0;
		}
		return getReelArrayByLayer(layer)[reelID].visibleSymbols;
	}

	public override void playAnticipationSound(int stoppedReel, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		if (layeredAnticipationSounds.Count > 0)
		{
			stoppedReel++; // ugh. The indexes don't match so just fix this here instead of fucking with layeredAnticipationSounds.
			if (layeredAnticipationSounds.ContainsKey(layer))
			{
				foreach (int anticipationReel in layeredAnticipationSounds[layer])
				{
					if (anticipationReel == stoppedReel)
					{
						if (bonusHit)
						{					
							Audio.play(Audio.soundMap("bonus_symbol_fanfare" + bonusHits));
						}
						
						// Handle Scatter Hit Sounds.
						if  (scatterHit)
						{
							Audio.play(Audio.soundMap("scatter_symbol_fanfare" + _reelGame.engine.scatterHits));
							Audio.play(Audio.soundMap("scatter_symbol_vo_sweetener" + _reelGame.engine.scatterHits), 1, 0, 0.25f);
						}
						
						if (twHIT)
						{
							Audio.play(Audio.soundMap("trigger_symbol"));
						}
					}
				}
			}
		}
		else
		{
			base.playAnticipationSound(stoppedReel, bonusHit, scatterHit, twHIT, layer);
		}
	}

	/**
	Used to play the visual and audio effects that occur when a bonus game has been acquired
	*/
	public override IEnumerator playBonusAcquiredEffects(int layer = -1, bool isPlayingSound = true)
	{
		if (isPlayingSound)
		{
			if (Audio.canSoundBeMapped("bonus_symbol_animate"))
			{
				Audio.play(Audio.soundMap("bonus_symbol_animate"));
			}
			else if (Audio.canSoundBeMapped("bonus_symbol_freespins_animate") && BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.GIFTING))
			{
				Audio.play(Audio.soundMap("bonus_symbol_freespins_animate"));
			}
			else if (Audio.canSoundBeMapped("bonus_symbol_pickem_animate") && BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.CHALLENGE))
			{
				Audio.play(Audio.soundMap("bonus_symbol_pickem_animate"));
			}
		}
			
		int numStartedBonusSymbolAnims = 0;
		numFinishedBonusSymbolAnims = 0;
		
		SlotReel[] reels = getReelArrayByLayer(layer);
		for (int reelIdx = 0; reelIdx < reels.Length; reelIdx++)
		{
			numStartedBonusSymbolAnims += reels[reelIdx].animateBonusSymbols(onBonusSymbolAnimationDone);
		}
		
		// wait for the bonus symbol animations to finish
		while (numFinishedBonusSymbolAnims < numStartedBonusSymbolAnims)
		{
			yield return null;
		}
	} 

	
	// setOutcome - assigns the current SlotOutcome object, and in the process indicates that the system is ready to stop the reels and display the results.
	// This only changes the reel strips on the base layer because everything else gets set differently.
	public override void setOutcome(SlotOutcome slotOutcome)
	{
		_slotOutcome = slotOutcome;
		anticipationTriggers = _slotOutcome.getAnticipationTriggers(); //< used for anticipation animations, only needs to happen at the start of the spin
		layeredAnticipationTriggers = _slotOutcome.getReevaluationAnticipationTriggers();

		Dictionary<int, string> reelStrips = _slotOutcome.getReelStrips();
		layeredAnticipationSounds = _slotOutcome.getReevaluationAnticipationSounds();
		anticipationSounds = _slotOutcome.getAnticipationSounds();
		bool addedLayeredAnticipations = false;
		for (int i = 0; i < anticipationSounds.Length;i++)
		{
			anticipationSounds[i]--;
		}
		
		// Set the offset of each of the reels in the foreground.
		if (reelLayers == null || reelLayers.Length < 2)
		{
			Debug.LogError("reelLayers are not set properly, cannot set new Foreground reels..");
		}
		else
		{
			foreach (ReelLayer layer in reelLayers)
			{
				layer.setReelInfo(slotOutcome);
			}
		}

		foreach (int i in layeredAnticipationSounds.Keys)
		{
			SlotReel[] reellayerArray = reelLayers[i].getReelArray();
			foreach(int j in layeredAnticipationSounds[i])
			{
				reellayerArray[j-1].setAnticipationReel(true);
				addedLayeredAnticipations = true;
			}
		}

		SlotReel[] reelArray = getReelArray();
		for (int i = 0; i < reelArray.Length; i++)
		{
			// Set Anticipation Reels.
			bool foundIndex = false;
			foreach (int anticipationReel in anticipationSounds)
			{
				if (anticipationReel == i)
				{
					foundIndex = true;
				}
			}

			if (!addedLayeredAnticipations)
			{
				reelArray[i].setAnticipationReel(foundIndex);
			}
			
			// Set replacement strips on base reels
			int reelID = i + 1;
			if (reelStrips.ContainsKey(reelID))
			{
				ReelStrip strip = ReelStrip.find(reelStrips[reelID]);
				if (reelLayers != null && reelLayers.Length > 0)
				{
					SlotReel reel = reelLayers[0].getSlotReelAt(i);
					if (reel != null)
					{
						reel.setReplacementStrip(strip);
					}
				}
				else
				{
					Debug.LogError("Layers == null, can't set new reel strips.");
				}
			}
		}
		
		// Apply universal reel strip replacement data if present
		applyUniversalReelStripReplacementData(slotOutcome);
		
		if (_state == EState.Spinning)
		{
			stopReels();
		}
	}

	/// Checks to see if the anticipation effect is needed, and plays it if necessary.

	public override void checkAnticipationEffect(SlotReel stoppedReel)
	{
		if(!playModuleAnticipationEffectOverride(stoppedReel))
		{
			// ensure that this reel thinks it should trigger anticipation effects, as some games with layers will not want them triggered for a specific layer
			if (stoppedReel.shouldPlayAnticipateEffect)
			{
				Dictionary<string,int> anticipationTriggerInfo = null;

				// try to do almost identical logic (as same function in SlotEngine.cs) for layered anticipations (for multi slot games)
				if (layeredAnticipationTriggers != null && layeredAnticipationTriggers.Count > 0)
				{
					// need to convert to a raw id so that indpendent reel games will trigger
					int rawReelID = stoppedReel.getRawReelID();
					if (layeredAnticipationTriggers.ContainsKey(stoppedReel.layer))
					{
						if (layeredAnticipationTriggers[stoppedReel.layer].TryGetValue(rawReelID + 1, out anticipationTriggerInfo))
						{
							int reelToAnimate = -1;
							if (anticipationTriggerInfo.TryGetValue("reel", out reelToAnimate))
							{
								// reelToAnimate may be an independent reels index, so need to try and convert it
								int standardReelIDToAnimate = -1;
								int rowToAnimate = -1;
								rawReelIDToStandardReelID(reelToAnimate - 1, out standardReelIDToAnimate, out rowToAnimate);

								int position = -1;
								// Sometimes a specific position on the reels is passed down. 
								if (anticipationTriggerInfo.TryGetValue("position", out position))
								{
									playAnticipationEffect(standardReelIDToAnimate, rowToAnimate, position, stoppedReel.layer);
								}
								else
								{
									playAnticipationEffect(standardReelIDToAnimate, rowToAnimate, -1, stoppedReel.layer);
								}
							}
						}
					}
				}
			}
		}
	}
}
