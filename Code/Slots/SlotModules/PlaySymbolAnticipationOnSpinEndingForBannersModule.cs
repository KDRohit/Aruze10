using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * PlaySymbolAnticipationOnSpinEndingForBannersModule.cs
 * author: Scott Lepthien
 * Plays the anticipation on the specified symbols as the reel comes to a stop.
 * used for TW animations in games that don't flag reels with them as anticipation reels (harvey01 as an example) 
 * but only triggers those animations when banners haven't already been triggered
 */
public class PlaySymbolAnticipationOnSpinEndingForBannersModule : PlaySymbolAnticipatonOnSpinEndingModule 
{
	private HashSet<int> bannersRevealedOnReels = new HashSet<int>();

	public override bool needsToExecuteOnSpinEnding(SlotReel stoppedReel)
	{
		if (!bannersRevealedOnReels.Contains(stoppedReel.reelID - 1))
		{
			// banner not revealed on this reel yet
			return base.needsToExecuteOnSpinEnding(stoppedReel);
		}
		else
		{
			return false;
		}
	}

	public override void executeOnSpinEnding(SlotReel stoppedReel)
	{
		bannersRevealedOnReels.Add(stoppedReel.reelID - 1);
		base.executeOnSpinEnding(stoppedReel);
	}
}
