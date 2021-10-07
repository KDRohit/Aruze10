using UnityEngine;
using System.Collections;

public class Ainsworth02FreeSpins : IndependentReelFreeSpinGame 
{
	protected override void setDefaultReelStopOrder()
	{
		stopOrder = new StopInfo[][] 
		{
			new StopInfo[] {new StopInfo(0,0),},
			new StopInfo[] {new StopInfo(0,1),},
			new StopInfo[] {new StopInfo(0,2),},
			
			// new StopInfo[] {new StopInfo(0,3),},
			// new StopInfo[] {new StopInfo(0,4),},
			
			new StopInfo[] {new StopInfo(1,0),},
			new StopInfo[] {new StopInfo(1,1),},
			new StopInfo[] {new StopInfo(1,2),},
			
			// new StopInfo[] {new StopInfo(1,3),},
			// new StopInfo[] {new StopInfo(1,4),},
			
			new StopInfo[] {new StopInfo(2,0),},
			new StopInfo[] {new StopInfo(2,1),},
			new StopInfo[] {new StopInfo(2,2),},
			
			// new StopInfo[] {new StopInfo(2,3),},
			// new StopInfo[] {new StopInfo(2,4),},
			
			new StopInfo[] {new StopInfo(3,0),},
			new StopInfo[] {new StopInfo(3,1),},
			new StopInfo[] {new StopInfo(3,2),},
			
			// new StopInfo[] {new StopInfo(3,3),},
			// new StopInfo[] {new StopInfo(3,4),},
			
			new StopInfo[] {new StopInfo(4,0),},
			new StopInfo[] {new StopInfo(4,1),},
			new StopInfo[] {new StopInfo(4,2),},
			
			// new StopInfo[] {new StopInfo(4,3),},
			// new StopInfo[] {new StopInfo(4,4),},
		};
	}
}
