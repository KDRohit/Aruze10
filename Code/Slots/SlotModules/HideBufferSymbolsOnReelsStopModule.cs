using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class can be use to hide any buffer symbols when the reels stop moving.  This is especially helpful in games like ainsworth04 with oversized symbols and independant reels.
public class HideBufferSymbolsOnReelsStopModule : SlotModule
{
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
		{
			foreach (SlotSymbol symbol in reel.symbolList)
			{
				symbol.setSymbolLayerToParentReelLayer();			
			}
		}

		yield break;
	}

	public override bool needsToExecuteOnReelEndRollback(SlotReel reel)
	{
		return true;
	}

	public override IEnumerator executeOnReelEndRollback(SlotReel reel)
	{
		foreach (SlotSymbol symbol in reel.symbolList)
		{
			if (!symbol.isVisible(anyPart: true, relativeToEngine: true))
			{
				CommonGameObject.setLayerRecursively(symbol.gameObject, Layers.ID_HIDDEN);
			}
		}
	
		yield break;
	}
}
