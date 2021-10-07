using UnityEngine;
using System.Collections;

public class Zom01 : SlotBaseGame 
{	
	/// Mutates the symbol on outcome display.
	public override void mutateSymbolOnOutcomeDisplay(string symbol)
	{
		if (symbol.Contains('M'))
		{
			doMutations(symbol);
		}
	}

	/// Loops through reels and mutates appropriate symbols
	private void doMutations(string symbolName)
	{	
		bool consecutiveReels = true;

		SlotReel[] reelArray = engine.getReelArray();

		foreach (SlotReel reel in reelArray)
		{
			bool foundSymbolInThisReel = false;
			foreach (SlotSymbol symbol in reel.visibleSymbols)
			{
				if (SlotUtils.getBaseSymbolName(symbol.name) == symbolName && consecutiveReels)
				{
					// Found a symbol to change.
					doSymbolMutation(symbol, symbolName);
					foundSymbolInThisReel = true;
				}
				else if (SlotUtils.getBaseSymbolName(symbol.name) == "WD")
				{
					foundSymbolInThisReel = true;
				}
			}
			if (!foundSymbolInThisReel)
			{
				consecutiveReels = false;
			}
		}
	}

	/// Actually call the mutation on the symbol
	private void doSymbolMutation(SlotSymbol symbol, string symbolName)
	{
		// Make sure we have a non-null animator that is not currently animating anything
		if (symbol.animator != null && !symbol.animator.isDoingSomething)
		{
			if (symbol.name == symbolName)
			{
				// The symbol is the non-zombie view, so mutate to the zombie
				symbol.mutateTo(symbolName + "-A");
			}
			else
			{
				// The symbol is the zombie view, probably, so switch back to the non-zombie view
				symbol.mutateTo(symbolName);
			}
		}
	}
}
