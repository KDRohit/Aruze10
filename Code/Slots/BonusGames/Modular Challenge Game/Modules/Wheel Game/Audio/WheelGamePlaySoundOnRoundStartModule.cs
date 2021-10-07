using UnityEngine;
using System.Collections;

/**
 * Play a specific audio key on round start
 */
public class WheelGamePlaySoundOnRoundStartModule : ChallengeGameSingleSoundModule 
{
	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		yield return StartCoroutine(playAudio());
		yield return StartCoroutine(base.executeOnRoundStart());
	}
}
