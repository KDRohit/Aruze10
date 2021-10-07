using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrantFreespinsWithSparkleTrailModule : GrantFreespinsModule 
{
	public AnimatedParticleEffect particleTrail;
	public string BONUS_SYMBOL_NAME = "BN";

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		List<SlotSymbol> visibleSymbols = reelGame.engine.getAllVisibleSymbols();

		TICoroutine bonusSymbolAnimationsCoroutine = null;
		if (isAnimatingBonusSymbols)
		{
			bonusSymbolAnimationsCoroutine = StartCoroutine(playAndWaitOnBonusSymbolAnimations());
		}
		
		foreach (SlotSymbol symbol in visibleSymbols)
		{	
			if(symbol.serverName == BONUS_SYMBOL_NAME)
			{
				yield return StartCoroutine(particleTrail.animateParticleEffect(symbol.transform));
				incrementFreespinCount();
			}
		}

		if (bonusSymbolAnimationsCoroutine != null)
		{
			yield return bonusSymbolAnimationsCoroutine;
		}
	}
}
