using UnityEngine;
using System.Collections.Generic;

public class AdjustReelTimingModule : SlotModule 
{
	// adjustmentAmount gets added to reel timing for every reel in the game
	public float adjustmentAmount = 0.0f;

	public override float getDelaySpecificReelStop(List<SlotReel> reelsForStopIndex)
	{
		return adjustmentAmount;
	}
}
