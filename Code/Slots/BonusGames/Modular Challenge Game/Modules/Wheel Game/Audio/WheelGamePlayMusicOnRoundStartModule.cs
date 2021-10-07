using UnityEngine;
using System.Collections;

/**
 * Play a specific music key on round start
 */
public class WheelGamePlayMusicOnRoundStartModule : ChallengeGameMusicModule
{
	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		yield return StartCoroutine(playMusic());
		yield return StartCoroutine(base.executeOnRoundStart());
	}
}
