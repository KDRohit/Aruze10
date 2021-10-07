using UnityEngine;
using System.Collections;

/**
 * Play a specific audio key on spin action
 */
public class WheelGamePlayMusicOnSpinModule : WheelGameMusicModule
{
	public override bool needsToExecuteOnSpin()
	{
		return true;
	}

	public override IEnumerator executeOnSpin()
	{
		yield return StartCoroutine(base.playMusic());
		yield return StartCoroutine(base.executeOnSpin());
	}
}
