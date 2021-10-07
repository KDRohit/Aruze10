using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Play an animation as soon as the respin will start, this is useful when you need to do a machine wide animation instead of per symbol animation for example.

NOTE: THIS CLASS SHOULD PROBABLY BE DEPRACATED AND HOPEFULLY REPLACED WITH FORCING THE GAME TO PLAY BONUS SYMBOL ANIMS IF THE DATA IS MISSING FROM THE OUTCOME
SEE: Ainsworth01CarryoverWinningsModule.isHandlingBonusSymbolAnimsInModule for example (and probably the fix you should be using if your game was using this class and Ainsworth01CarryoverWinningsModule)
PREFABS STILL USING THIS: "tvaussie08 Panda King Basegame", "tvaussie10 Dollar Action Basegame", "tvaussie11 Stormin 7s Basegame"
*/
public class AinsworthStartFreespinsModule : SlotModule 
{
	[SerializeField] float waitTimeToEnterFreespins = 1.0f;

	public override bool needsToExecuteOnPlayBonusSymbolsAnimation()
	{
		return true;
	}

	public override IEnumerator executeOnPlayBonusSymbolsAnimation()
	{
		SlotEngine engine = ReelGame.activeGame.engine;

		SlotReel[] reelArray = engine.getReelArray();

		for(int reelIdx = 0; reelIdx < reelArray.Length; reelIdx++)
		{
			SlotReel reel = reelArray[reelIdx];

			for(int i = 0; i < reel.visibleSymbols.Length; i++)
			{
				SlotSymbol symbol = reel.visibleSymbols[i];
			
				// this being an anticipation reel implies that it contains a bonus symbol which triggered the bonus
				if(symbol.isBonusSymbol)
				{
					symbol.animateOutcome();
				}
			}
		}

		yield return new WaitForSeconds(waitTimeToEnterFreespins);
	}
}
