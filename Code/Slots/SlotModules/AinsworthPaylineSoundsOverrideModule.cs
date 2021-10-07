using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AinsworthPaylineSoundsOverrideModule : SlotModule
{

	[SerializeField] private string SYMBOL_ANIMATION_BONUS_SOUND_KEY = "symbol_animation_bonus";
	[SerializeField] private string SYMBOL_ANIMATION_WD_SOUND_KEY = "symbol_animation_WD";
	[SerializeField] private float ROLLUP_DELAY = 0.0f;

	private bool foundWild = false;
	private bool foundBonus = false;
	
	public override bool needsToOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		foundWild = false;
		foundBonus = false;
		
		foreach(SlotSymbol sym in slotSymbols)
		{
			// First check to see if its a wild
			if (sym.isWildSymbol)
			{
				foundWild = true;
			}
			// Check to see if it has a W at all (probably a freespin bonus symbol)
			else if (sym.name.Contains('W'))
			{
				foundBonus = true;
			}
		}
		
		return (foundWild || foundBonus);
	}

	public override void executeOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		if (foundBonus)
		{
			if (!string.IsNullOrEmpty(SYMBOL_ANIMATION_BONUS_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(SYMBOL_ANIMATION_BONUS_SOUND_KEY));
				Audio.play(Audio.soundMap(SYMBOL_ANIMATION_WD_SOUND_KEY), 1.0f, 0.0f, 0.6f);
			}
			else
			{
				Audio.play(Audio.soundMap(SYMBOL_ANIMATION_WD_SOUND_KEY));
			}
		}
		else if (foundWild)
		{
			Audio.play(Audio.soundMap(SYMBOL_ANIMATION_WD_SOUND_KEY));
		}
	}

	public override float getRollupDelay()
	{
		return ROLLUP_DELAY;
	}
}
