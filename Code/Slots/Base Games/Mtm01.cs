using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Mtm01 : SlotBaseGame 
{	
	private const string BN1_SOUND = "bonus_symbol_freespins_animate";
	private const string BN2_SOUND = "bonus_symbol_pickem_animate";

	public override IEnumerator playBonusAcquiredEffects()
	{		
		// we want to see what symbol landed on reel 4, to find out what sound we should play.
		SlotReel[] reelArray = engine.getReelArray();
		SlotSymbol[] visibleSymbols = reelArray[4].visibleSymbols;
		foreach (SlotSymbol symbol in visibleSymbols)
		{
			if (symbol.serverName == "BN1")
			{
				Audio.play(Audio.soundMap(BN1_SOUND));
			}
			else if (symbol.serverName == "BN2")
			{
				Audio.play(Audio.soundMap(BN2_SOUND));
			}
		}
		yield return StartCoroutine(base.playBonusAcquiredEffects());
	}
}
