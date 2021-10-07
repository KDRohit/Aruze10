using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
The gen09 freespins class.
 */ 
public class Gen09FreeSpins : TumbleFreeSpinGame 
{
	protected override void beginFreeSpinMusic()
 	{
 		// Even though we came from a transition, we still want to play this sound.
 		// The VO is handled in the base game animateAllBonusSymbols during the transition.
 		Audio.switchMusicKeyImmediate(Audio.soundMap("freespin"), 0.0f);
 	}
}
