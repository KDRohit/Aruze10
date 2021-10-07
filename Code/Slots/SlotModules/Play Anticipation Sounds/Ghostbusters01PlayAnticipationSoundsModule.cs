using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ghostbusters01PlayAnticipationSoundsModule : SlotModule {

	private const float anticipationDelay = 0.1f;

	public override bool needsToExecuteOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		return true;
	}

	public override IEnumerator executeOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		if (reelGame.engine.scatterHits == 3 && scatterHit)
		{
			if (anticipationSymbols.Count > 3)
			{
				Audio.play(Audio.soundMap("scatter_symbol_fanfare1"));
			}
			else
			{
				Audio.play(Audio.soundMap("scatter_symbol_fanfare5"));
			}
		}

		if (reelGame.engine.scatterHits == 4 && scatterHit)
		{
			if(anticipationSymbols.Count > 4)
			{
				Audio.play(Audio.soundMap("scatter_symbol_fanfare2"));
			}
			else
			{
				Audio.play(Audio.soundMap("scatter_symbol_fanfare5"));
			}
		}

		if (bonusHits == 3 && reelGame.outcome.isChallenge)
		{
			Audio.play(Audio.soundMap("bonus_symbol_pickem_anticipate"));
		}

		yield break;
	}

	public override bool needsToPlayReelAnticipationSoundFromModule()
	{
		if (reelGame.outcome.isScatter || reelGame.engine.scatterHits >= 2)
		{
			return true;
		}
		return false;
	}

	public override void playReelAnticipationSoundFromModule ()
	{
		Audio.playWithDelay(Audio.soundMap("bonus_anticipate_alternate"), anticipationDelay); //Delaying this a bit so we can hear the SC anticipation sound
	}
}
