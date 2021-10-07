using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Allows customizing audio for tv01
public class TV02SlotModule : SlotModule
{
	// executeOnPlayAnticipationSound() section
	// Functions here are executed in SpinReel where SlotEngine.playAnticipationSound is called
	public override bool needsToExecuteOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, 
		bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		// if W2 -- this is not considered a bonus symbol or bonus outcome, but design calls for it to play anticipations
		string symbol = anticipationSymbols[stoppedReel + 1];
		return (symbol == "W2");
	}

	public override IEnumerator executeOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, 
		bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		// Fanfare
		Audio.play(Audio.soundMap("bonus_symbol_fanfare3"));

		yield return null;
	}
}