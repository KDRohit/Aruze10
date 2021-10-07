using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Plays a differnt stop sound when a mega symbol is going to land.
// @Author Leo Schnee
public class OverrideMegaSymbolReelStopSound : SlotModule 
{
	// If using the big_symbol_lands key variant, don't assume the slot game is layered.
	// Completely overrides the stop sound and assumes the mega symbol fills the reel (not cut off).
	public bool doesNotOverlayAndMustFillReel = false;

	private string MEGA_SYMBOL_LAND_SOUND_BASE_GAME_KEY = "overlay_mega_symbol_fanfare_base";
	private string MEGA_SYMBOL_LAND_SOUND_FREESPINS_KEY = "overlay_mega_symbol_fanfare_freespin";

	// Other sound key set that plays when a mega symbol lands, but does not overlay on top of the existing reelstop sound.
	private string BIG_SYMBOL_LANDS_KEY = "big_symbol_lands";
	private string FREESPIN_BIG_SYMBOL_LANDS_KEY = "freespin_big_symbol_lands";

	public override bool needsToExecuteOnSpecificReelStopping(SlotReel stoppingReel)
	{
		return true;
	}

	public override void executeOnSpecificReelStopping(SlotReel reel)
	{
		if (doesNotOverlayAndMustFillReel)
		{
			string[] stopSymbolNames = reel.getFinalReelStopsSymbolNames();
			foreach (string name in stopSymbolNames)
			{
				if (!SlotSymbol.isLargeSymbolPartFromName(name))
				{
					reel.reelStopSoundOverride = "";
					return;
				}
			}
			if (reelGame is FreeSpinGame)
			{
				reel.reelStopSoundOverride = Audio.soundMap(FREESPIN_BIG_SYMBOL_LANDS_KEY);
			}
			else
			{
				reel.reelStopSoundOverride = Audio.soundMap(BIG_SYMBOL_LANDS_KEY);
			}
		}
		// If we are overlapping the mega symbol sound on top of the existing reel stop sound,
		// play the audio on a different layer. This is for games like bride01
		else if (reel.layer == 1)
		{
			reel.shouldPlayReelStopSound = false;
			if (reel.reelID == 2)
			{
				if (reelGame is FreeSpinGame)
				{
					reel.reelStopSoundOverride = Audio.soundMap(MEGA_SYMBOL_LAND_SOUND_FREESPINS_KEY);
				}
				else
				{
					reel.reelStopSoundOverride = Audio.soundMap(MEGA_SYMBOL_LAND_SOUND_BASE_GAME_KEY);
				}

				string[] stopSymbolNames = reel.getFinalReelStopsSymbolNames();
				foreach (string name in stopSymbolNames)
				{
					if (!SlotSymbol.isBlankSymbolFromName(name))
					{
						// We want to play a sound
						reel.shouldPlayReelStopSound = true;
						break;
					}
				}
			}
		}
	}
}
