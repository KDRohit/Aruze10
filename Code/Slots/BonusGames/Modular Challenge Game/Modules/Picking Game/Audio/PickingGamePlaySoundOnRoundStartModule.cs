using UnityEngine;
using System.Collections;

/**
 * Play a specific sound key on round start
 */
public class PickingGamePlaySoundOnRoundStartModule : PickingGameSoundModule
{
	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		yield return StartCoroutine(base.playAudio());
		yield return StartCoroutine(base.executeOnRoundStart());
	}
}
