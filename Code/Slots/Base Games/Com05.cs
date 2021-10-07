using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Com05.cs
 * Class for base game of Com05 - Hagar the Horrible
 * Author: Nick Reynolds
 * All this class does is override the removal of symbols functions from TumbleSlotBaseGame
 */ 
public class Com05 : TumbleSlotBaseGame 
{
	protected override IEnumerator animateAllBonusSymbols()
	{
		SlotReel[] reelArray = engine.getReelArray();
		for (int reelIndex = 0; reelIndex < reelArray.Length; reelIndex++)
		{
			for (int symbolIndex = 0; symbolIndex < visibleSymbolClone[reelIndex].Count; symbolIndex++)
			{

				if (visibleSymbolClone[reelIndex][symbolIndex].name == "BN")
				{
					visibleSymbolClone[reelIndex][symbolIndex].animateOutcome();
				}
			}
		}
		yield return new TIWaitForSeconds(4.25f);
	}

}
