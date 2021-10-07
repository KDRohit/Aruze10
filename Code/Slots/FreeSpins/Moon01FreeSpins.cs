using UnityEngine;
using System.Collections;

/**
Implements the free spin bonus of moon01 Moon Pies, basically just a couple sound hooks here
for feature implementation look at Moon01PickAndAddWildsToReelsModule.cs
*/
public class Moon01FreeSpins : FreeSpinGame 
{
	private const string BONUS_SUMMARY_VO_SOUND_KEY = "FreespinSummaryVOMoonpies";
	
	// play the summary sound and end the game
	protected override void gameEnded()
	{
		Audio.play(BONUS_SUMMARY_VO_SOUND_KEY);
		base.gameEnded();
	}
}
