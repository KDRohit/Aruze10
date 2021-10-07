using UnityEngine;
using System.Collections;

/**
 * Class for overriding rollup audio on a per-round basis.
 */
public class PickingGameRollupSoundModule : PickingGameModule
{
	[SerializeField] protected string ROLLUP_AUDIO_KEY = "";
	[SerializeField] protected string ROLLUP_FINISH_AUDIO_KEY = "";

	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	public override IEnumerator executeOnRoundStart()
	{
		ChallengeGame.instance.rollupSoundOverride = Audio.soundMap(ROLLUP_AUDIO_KEY);
		ChallengeGame.instance.rollupTermOverride = Audio.soundMap(ROLLUP_FINISH_AUDIO_KEY);
		yield break;
	}
}
