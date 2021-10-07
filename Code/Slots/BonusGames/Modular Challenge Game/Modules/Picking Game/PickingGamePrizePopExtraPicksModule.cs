using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickingGamePrizePopExtraPicksModule : PickingGameRevealModule
{
    [SerializeField] protected string REVEAL_ANIMATION_NAME = "Reveal Extra";
    [SerializeField] protected float REVEAL_ANIMATION_DURATION_OVERRIDE = 0.0f;
    [SerializeField] protected string REVEAL_AUDIO = "PickAwardChancesPrizePopCommon";
    [SerializeField] protected float REVEAL_AUDIO_DELAY = 0.0f; // delay before playing the reveal clip

    public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
	{
		// detect pick type & whether to handle with this module
		if ((pickData != null) && (!pickData.canAdvance) && (pickData.prizePopPicks > 0))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
		
	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		Audio.playSoundMapOrSoundKeyWithDelay(REVEAL_AUDIO, REVEAL_AUDIO_DELAY);
		pickItem.setRevealAnim(REVEAL_ANIMATION_NAME, REVEAL_ANIMATION_DURATION_OVERRIDE);
		yield return StartCoroutine(base.executeOnItemClick(pickItem));
	}
}
