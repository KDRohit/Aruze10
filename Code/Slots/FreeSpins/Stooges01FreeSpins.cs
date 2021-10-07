using UnityEngine;
using System.Collections;

/**
Implements the free spin bonus of stooges01 Real Three Stooges, basically just a couple sound hooks here
for feature implementation look at Stooges01PickAndAddWildsToReelsModule.cs
*/
public class Stooges01FreeSpins : FreeSpinGame 
{
	private const string FREE_SPIN_INTRO_MUSIC = "IntroFreespinStooges";
	private const string PICK_STAGE_MUSIC = "FreespinStooges";

	protected override void beginFreeSpinMusic()
	{
		// play free spin audio and start the spin
		if (!cameFromTransition)
		{
			// Setup the pick music to play looped, but then force the intro music to play before it
			Audio.playMusic(FREE_SPIN_INTRO_MUSIC);
			Audio.switchMusicKey(PICK_STAGE_MUSIC);
		}
	}
}
