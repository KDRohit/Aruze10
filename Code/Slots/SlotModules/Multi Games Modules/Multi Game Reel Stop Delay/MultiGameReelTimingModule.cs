using UnityEngine;
using System.Collections.Generic;

/*
* MultiGameReelTiming.cs
* Module used in gwtw01 and maybe other MultiGames to get the reel
* timing on the first reel stop timing correctly (except in first game - top left)
*/
public class MultiGameReelTimingModule : SlotModule 
{
	[SerializeField] private float INITIAL_REEL_DELAY;

	public override bool shouldReplaceDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		for (int i = 0; i < reelsForStopIndex.Count; i++)
		{
			SlotReel stopReel = reelsForStopIndex[i];
			if (stopReel.reelID == 1 && stopReel.layer != 0)
			{
				return true;
			}
		}
		
		return false;
	}

	public override float getReplaceDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		return INITIAL_REEL_DELAY;
	}
}
