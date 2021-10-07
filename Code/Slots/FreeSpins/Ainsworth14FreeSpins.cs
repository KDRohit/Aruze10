using UnityEngine;
using System.Collections;

public class Ainsworth14FreeSpins : IndependentReelFreeSpinGame
{
	protected override void setDefaultReelStopOrder()
	{
		stopOrder = new StopInfo[][]
		{
			new StopInfo[] {new StopInfo(0,0,0), new StopInfo(0,0,1),},

			new StopInfo[]
			{
				new StopInfo(1,0,0), new StopInfo(1,1,0), new StopInfo(1,2,0),
				new StopInfo(1,0,1), new StopInfo(1,1,1), new StopInfo(1,2,1),
			}, // stop independent reel stack together

			new StopInfo[] {new StopInfo(2,0,0), new StopInfo(2,0,1),},

			new StopInfo[]
			{
				new StopInfo(3,0,0), new StopInfo(3,1,0), new StopInfo(3,2,0),
				new StopInfo(3,0,1), new StopInfo(3,1,1), new StopInfo(3,2,1),
			}, // stop independent reel stack together

			new StopInfo[] {new StopInfo(4,0,0), new StopInfo(4,0,1),}
		};
	}
}
