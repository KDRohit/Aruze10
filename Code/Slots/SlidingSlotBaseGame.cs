using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SlidingSlotBaseGame : LayeredSlotBaseGame
{
	protected override void setDefaultReelStopOrder()
	{
		stopOrder = new StopInfo[][] 
		{
			new StopInfo[] {new StopInfo(0, 0, 0), new StopInfo(0, 0, 1)},
			new StopInfo[] {new StopInfo(1, 0, 0), new StopInfo(1, 0, 1)},
			new StopInfo[] {new StopInfo(2, 0, 0), new StopInfo(2, 0, 1)},
			new StopInfo[] {new StopInfo(3, 0, 0), new StopInfo(3, 0, 1)},
			new StopInfo[] {new StopInfo(4, 0, 0), new StopInfo(4, 0, 1)},
			new StopInfo[] {new StopInfo(5, 0, 0), new StopInfo(5, 0, 1)},
		};
	}

	protected override void setEngine()
	{
		engine = new SlidingSlotEngine(this, isLinkingLayersEverySpin);
	}

	protected override void Awake()
	{
		base.Awake();
		// Make all of the reelLayers into SlidingLayers
		for (int i = 0; i < reelLayers.Length; i++)
		{
			reelLayers[i] = new SlidingLayer(reelLayers[i]);
		}
	}
}
