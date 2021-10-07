using UnityEngine;
using System.Collections;

/**
 * Play a specific music key on round start
 */
public class PickingGamePlayMusicOnRoundStartModule : PickingGameMusicModule
{
	[SerializeField] protected float DELAY = 0.0f;
	[SerializeField] protected bool isBlocking = false; // with long delays, we may not want to block

	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		if (isBlocking)
		{
			yield return StartCoroutine(playMusicWithDelay());
		}
		else
		{
			StartCoroutine(playMusicWithDelay());
		}
	}

	private IEnumerator playMusicWithDelay()
	{
		yield return new TIWaitForSeconds(DELAY);
		yield return StartCoroutine(base.playMusic());
		yield return StartCoroutine(base.executeOnRoundStart());
	}
}
