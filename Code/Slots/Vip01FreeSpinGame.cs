using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Handles basic stuff for all indpendent reel free spin games
*/
public class Vip01FreeSpinGame : IndependentReelFreeSpinGame
{
	protected override void setDefaultReelStopOrder()
	{
		stopOrder = new StopInfo[][] 
		{
			new StopInfo[] {new StopInfo(0,0),},
			new StopInfo[] {new StopInfo(1,1),},
			new StopInfo[] {new StopInfo(1,0),},
			new StopInfo[] {new StopInfo(2,2),},
			new StopInfo[] {new StopInfo(2,1),},
			new StopInfo[] {new StopInfo(2,0),},
			new StopInfo[] {new StopInfo(3,2),},
			new StopInfo[] {new StopInfo(3,1),},
			new StopInfo[] {new StopInfo(3,0),},
			new StopInfo[] {new StopInfo(4,1),},
			new StopInfo[] {new StopInfo(4,0),},
			new StopInfo[] {new StopInfo(5,0),},
		};
	}
}
