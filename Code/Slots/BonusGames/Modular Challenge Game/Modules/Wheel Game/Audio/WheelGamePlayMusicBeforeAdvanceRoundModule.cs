using UnityEngine;
using System.Collections;

/**
 * Play a specific audio key before advancing to the next round
 */
public class WheelGamePlayMusicBeforeAdvanceRoundModule : ChallengeGameMusicModule
{
	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		return true;
	}

	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		yield return StartCoroutine(playMusic());
	}
}
