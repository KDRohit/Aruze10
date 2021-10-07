using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Used to fade symbols when a challenge game starts
//
// used in : aerosmith02 pick game
//
// Author : Nick Saito <nsaito@zynga.com>
// Sept 24, 2018
//
public class ChallengeGameFadeSymbolsModule : ChallengeGameModule
{
	[SerializeField] private FadeSymbolsEffect fadeSymbolsEffect;

	public override bool needsToExecuteOnRoundInit()
	{
		return (fadeSymbolsEffect.fadeOutAtStart || fadeSymbolsEffect.fadeInAtStart) && fadeSymbolsEffect.verifySymbolsReady(ReelGame.activeGame);
	}

	// Overrides HAVE TO call base.executeOnRoundInit!
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
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
