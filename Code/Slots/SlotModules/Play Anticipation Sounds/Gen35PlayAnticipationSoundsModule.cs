using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Gen35PlayAnticipationSoundsModule : SlotModule
{

	public override bool needsToExecuteOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		return true;
	}

	public override IEnumerator executeOnPlayAnticipationSound(int stoppedReel, Dictionary<int, string> anticipationSymbols, int bonusHits, bool bonusHit = false, bool scatterHit = false, bool twHIT = false, int layer = 0)
	{
		if (bonusHit)
		{
			if (bonusHits == 1) 
			{
				Audio.switchMusicKeyImmediate(Audio.soundMap("bonus_portal_bg"), 0);
			}
		}
		yield break;
	}
}
