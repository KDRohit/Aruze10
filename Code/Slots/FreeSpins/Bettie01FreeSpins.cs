using UnityEngine;
using System.Collections;

// The class that handles the specific ablilites of the Elvira02FreeSpins.
// A Sliding Slot game that keeps spinning as long as there are paylines.
// Once a payline is missed the reels slide back over to the LHS and the multipler goes down to 1.

public class Bettie01FreeSpins : SlidingSlotFreeSpinGame 
{
	// Sound names
	private const string INTRO_VO_COLLECTION = "FreespinIntroVOBettie01";
	private const string BEACH_SOUND = "BeachAmbience";

	public override void initFreespins()
	{
		cameFromTransition = true;
		base.initFreespins();
		Audio.play(BEACH_SOUND);
		Audio.play(Audio.soundMap(INTRO_VO_COLLECTION));
	}
}
