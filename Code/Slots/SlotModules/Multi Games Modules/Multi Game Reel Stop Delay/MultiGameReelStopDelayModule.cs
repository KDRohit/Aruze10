using UnityEngine;
using System.Collections.Generic;

public class MultiGameReelStopDelayModule : SlotModule 
{
	private const float INITIAL_REEL_DELAY = .5f;

	public override float getDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		for (int i = 0; i < reelsForStopIndex.Count; i++)
		{
			SlotReel stopReel = reelsForStopIndex[i];
			if (stopReel.reelID == 1 && stopReel.layer != 0)
			{
				return INITIAL_REEL_DELAY;
			}
		}

		return 0.0f;
	}
}
