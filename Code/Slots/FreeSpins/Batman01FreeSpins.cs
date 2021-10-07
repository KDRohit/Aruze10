using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Batman01FreeSpins : FreeSpinGame {

	protected override void setDefaultReelStopOrder()
	{
		stopOrder = new StopInfo[][] 
		{
			new StopInfo[] {new StopInfo(0, 0, 0)},
			new StopInfo[] {new StopInfo(1, 0, 0)},
			new StopInfo[] {new StopInfo(2, 0, 0)},
			new StopInfo[] {new StopInfo(3, 0, 0)},
			new StopInfo[] {new StopInfo(4, 0, 0)},
			new StopInfo[] {new StopInfo(5, 0, 0)},
			new StopInfo[] {new StopInfo(6, 0, 0)},
			new StopInfo[] {new StopInfo(7, 0, 0)},
			new StopInfo[] {new StopInfo(8, 0, 0)},
			new StopInfo[] {new StopInfo(9, 0, 0)},
		};
	}
}
