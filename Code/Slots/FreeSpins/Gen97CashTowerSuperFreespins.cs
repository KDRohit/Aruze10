using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Class to define the stop order for gen97 Cash Tower which technically has 3 areas of 3x5 independent reels,
 * but the way that the server is treating it is as a 9x5 game.
 *
 * Creation Date: 1/21/2020
 * Original Author: Scott Lepthien
 */
public class Gen97CashTowerSuperFreespins : IndependentReelFreeSpinGame
{
	protected override void setDefaultReelStopOrder()
	{
		stopOrder = new StopInfo[][] 
		{
			// Top 3x5
			new StopInfo[] {new StopInfo(0,0,0),},
			new StopInfo[] {new StopInfo(0,1,0),},
			new StopInfo[] {new StopInfo(0,2,0),},

			new StopInfo[] {new StopInfo(1,0,0),},
			new StopInfo[] {new StopInfo(1,1,0),},
			new StopInfo[] {new StopInfo(1,2,0),},

			new StopInfo[] {new StopInfo(2,0,0),},
			new StopInfo[] {new StopInfo(2,1,0),},
			new StopInfo[] {new StopInfo(2,2,0),},

			new StopInfo[] {new StopInfo(3,0,0),},
			new StopInfo[] {new StopInfo(3,1,0),},
			new StopInfo[] {new StopInfo(3,2,0),},

			new StopInfo[] {new StopInfo(4,0,0),},
			new StopInfo[] {new StopInfo(4,1,0),},
			new StopInfo[] {new StopInfo(4,2,0),},

			// Middle 3x5
			new StopInfo[] {new StopInfo(0,3,0),},
			new StopInfo[] {new StopInfo(0,4,0),},
			new StopInfo[] {new StopInfo(0,5,0),},

			new StopInfo[] {new StopInfo(1,3,0),},
			new StopInfo[] {new StopInfo(1,4,0),},
			new StopInfo[] {new StopInfo(1,5,0),},

			new StopInfo[] {new StopInfo(2,3,0),},
			new StopInfo[] {new StopInfo(2,4,0),},
			new StopInfo[] {new StopInfo(2,5,0),},

			new StopInfo[] {new StopInfo(3,3,0),},
			new StopInfo[] {new StopInfo(3,4,0),},
			new StopInfo[] {new StopInfo(3,5,0),},

			new StopInfo[] {new StopInfo(4,3,0),},
			new StopInfo[] {new StopInfo(4,4,0),},
			new StopInfo[] {new StopInfo(4,5,0),},
			
			// Bottom 3x5
			new StopInfo[] {new StopInfo(0,6,0),},
			new StopInfo[] {new StopInfo(0,7,0),},
			new StopInfo[] {new StopInfo(0,8,0),},

			new StopInfo[] {new StopInfo(1,6,0),},
			new StopInfo[] {new StopInfo(1,7,0),},
			new StopInfo[] {new StopInfo(1,8,0),},

			new StopInfo[] {new StopInfo(2,6,0),},
			new StopInfo[] {new StopInfo(2,7,0),},
			new StopInfo[] {new StopInfo(2,8,0),},

			new StopInfo[] {new StopInfo(3,6,0),},
			new StopInfo[] {new StopInfo(3,7,0),},
			new StopInfo[] {new StopInfo(3,8,0),},

			new StopInfo[] {new StopInfo(4,6,0),},
			new StopInfo[] {new StopInfo(4,7,0),},
			new StopInfo[] {new StopInfo(4,8,0),},
		};
	}
}
