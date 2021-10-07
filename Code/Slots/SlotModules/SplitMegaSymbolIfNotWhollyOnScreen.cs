using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// If a mega symbol isn't all the way on screen then it splits it.
// @Author Leo Schnee
public class SplitMegaSymbolIfNotWhollyOnScreen : SlotModule 
{
	public enum SplitTimeEnum
	{
		ON_SPECIFIC_REEL_STOP = 0,
		ON_REEL_STOP_CALLBACK
	}

	[SerializeField] private bool needsToSplitTallSymbols = false;
	[SerializeField] private SplitTimeEnum timeToSplit = SplitTimeEnum.ON_SPECIFIC_REEL_STOP;

	private List<SymbolAnimator> symbolsAlreadySplit = new List<SymbolAnimator>(); // Used to track what symbols have already been split (or added to a list to be split)
	private List<SlotSymbol> symbolsToSplitList = new List<SlotSymbol>(); // list of symbols to split which is kept if we are going to split on reel stop callback, that way we only have to go through the symbols once per reel

	public override bool needsToExecuteOnSpecificReelStop(SlotReel reel)
	{
		return true;
	}
	
	public override IEnumerator executeOnSpecificReelStop(SlotReel reel)
	{
		foreach (SlotSymbol symbol in reel.visibleSymbols)
		{
			if ((symbol.isMegaSymbolPart || (needsToSplitTallSymbols && symbol.isTallSymbolPart)) && !symbol.isWhollyOnScreen && !symbolsAlreadySplit.Contains(symbol.getAnimator()))
			{
				symbolsAlreadySplit.Add(symbol.getAnimator());

				switch (timeToSplit)
				{
					case SplitTimeEnum.ON_SPECIFIC_REEL_STOP:
						symbol.splitSymbol(allowFlattenedSymbolSwap: true);
						break;

					case SplitTimeEnum.ON_REEL_STOP_CALLBACK:
						symbolsToSplitList.Add(symbol);
						break;
				}
			}
		}

		yield break;
	}

// executeOnReelsStoppedCallback() section
// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return true;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		if (timeToSplit == SplitTimeEnum.ON_REEL_STOP_CALLBACK)
		{
			if (symbolsToSplitList.Count > 0)
			{
				for (int i = 0; i < symbolsToSplitList.Count; i++)
				{
					symbolsToSplitList[i].splitSymbol(allowFlattenedSymbolSwap: true);
				}

				// clear the list now that we've split them
				symbolsToSplitList.Clear();
			}
		}

		symbolsAlreadySplit.Clear();

		yield break;
	}
}
