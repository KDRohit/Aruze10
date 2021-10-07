using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Batman01 : LayeredMultiSlotBaseGame 
{
	
	protected override void setDefaultReelStopOrder()
	{
		stopOrder = new StopInfo[][] 
		{
			new StopInfo[] {new StopInfo(0, 0, 0)},
			new StopInfo[] {new StopInfo(1, 0, 0)},
			new StopInfo[] {new StopInfo(2, 0, 0)},
			new StopInfo[] {new StopInfo(3, 0, 0)},
			new StopInfo[] {new StopInfo(4, 0, 0)},
			new StopInfo[] {new StopInfo(0, 0, 2)},
			new StopInfo[] {new StopInfo(1, 0, 2)},
			new StopInfo[] {new StopInfo(2, 0, 2)},
			new StopInfo[] {new StopInfo(0, 0, 1)},
			new StopInfo[] {new StopInfo(1, 0, 1)},
			new StopInfo[] {new StopInfo(2, 0, 1)},
			new StopInfo[] {new StopInfo(3, 0, 1)},
			new StopInfo[] {new StopInfo(4, 0, 1)},
			new StopInfo[] {new StopInfo(0, 0, 3)},
			new StopInfo[] {new StopInfo(1, 0, 3)},
			new StopInfo[] {new StopInfo(2, 0, 3)},
		};
	}
}
