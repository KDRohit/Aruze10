using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Class to handle the stop order for Ainsworth15Freespins, may look into setting stop order from a setup module
instead so we don't have to make classes like this just for stop order

Creation Date: 9/26/2018
Original Author: Scott Lepthien
*/
public class Ainsworth15FreeSpins : MultiSlotFreeSpinGame
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
		};
	}
}
