using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WildPaylineSoundsOverrideModule : SlotModule
{
	private string WILD_SYMBOL_ANIMATION_SOUND_MAP_KEY = "wild_symbol_animate";
	
	public override bool needsToOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		foreach(SlotSymbol sym in slotSymbols)
		{
			if (sym.isWildSymbol)
			{
				return true;
			}
		}
		
		return false;
	}

	public override void executeOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		Audio.play(Audio.soundMap(WILD_SYMBOL_ANIMATION_SOUND_MAP_KEY));
	}
}
