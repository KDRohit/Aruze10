﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Files that exists to set the stop order for this game, since it needs a fairly custom one due to having
 * different types of reel layers.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 8/24/2020
 */
public class Orig004CraftyCoyote : IndependentReelBaseGame 
{
	protected override void setDefaultReelStopOrder()
	{
		stopOrder = new StopInfo[][] 
		{
			new StopInfo[] {new StopInfo(0,0,0),},
			new StopInfo[] {new StopInfo(0,0,1),},
			new StopInfo[] {new StopInfo(0,1,1),},
			new StopInfo[] {new StopInfo(0,2,1),},

			new StopInfo[] {new StopInfo(1,0,0),},
			new StopInfo[] {new StopInfo(1,0,1),},
			new StopInfo[] {new StopInfo(1,1,1),},
			new StopInfo[] {new StopInfo(1,2,1),},

			new StopInfo[] {new StopInfo(2,0,0),},
			new StopInfo[] {new StopInfo(2,0,1),},
			new StopInfo[] {new StopInfo(2,1,1),},
			new StopInfo[] {new StopInfo(2,2,1),}
		};
	}
}
