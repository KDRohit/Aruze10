using UnityEngine;
using System.Collections;

public class MultiGameBonusHitsIncrementorModule : SlotModule 
{	
	int totalBonusGames = 0;

	
	// every spin we should reset this
	public override bool needsToExecuteOnPreSpinNoCoroutine ()
	{
		return true;
	}

	public override void executeOnPreSpinNoCoroutine()
	{
		totalBonusGames = 0;
	}


	// every time bonus symbols land (with anticipation data), let's do our special logic
	public override bool needsToExecuteOnBonusHitsIncrement()
	{
		return true;
	}

	// kinda hacky, a 1-off for gwtw01 right now
	public override void executeOnBonusHitsIncrement(int reelID)
	{
		if (reelID == 1)
		{
			reelGame.engine.bonusHits = 1;
		}
		else if (reelID == 3)
		{
			reelGame.engine.bonusHits = 2;
		}
		else
		{
			totalBonusGames++;
			reelGame.engine.bonusHits = (2 + totalBonusGames);
		}
	}
}
