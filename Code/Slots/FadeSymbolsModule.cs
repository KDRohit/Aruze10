using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Used to fade symbols when a slot game starts
//
// used in : (not used yet)
//
// Author : Nick Saito <nsaito@zynga.com>
// Sept 24, 2018
//
public class FadeSymbolsModule : SlotModule
{
	[SerializeField] private FadeSymbolsEffect fadeSymbolsEffect;

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return (fadeSymbolsEffect.fadeOutAtStart || fadeSymbolsEffect.fadeInAtStart) && fadeSymbolsEffect.verifySymbolsReady(ReelGame.activeGame);
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		if (fadeSymbolsEffect.fadeOutAtStart)
		{
			StartCoroutine(fadeSymbolsEffect.fadeOutSymbolsCoroutine());
		}
		else if (fadeSymbolsEffect.fadeInAtStart)
		{
			StartCoroutine(fadeSymbolsEffect.fadeInSymbolsCoroutine());
		}
	}
}
