using UnityEngine;
using System.Collections;

/**
 * Class for holding basic audio information to make use of in derived action classes
 */
public class WheelGameSoundModule : WheelGameModule
{
	[SerializeField] protected string AUDIO_KEY = "";
	[SerializeField] protected bool isUsingAudioMapKey = true;	// This is here to be used ONLY if we can't use audio mappings because it would create an issue for web
	[SerializeField] protected bool waitForFinish = false;
	[SerializeField] protected float DELAY = 0.0f;

	protected IEnumerator playAudio()
	{
		if (isUsingAudioMapKey)
		{
			if (DELAY != 0.0f)
			{
				Audio.playWithDelay(Audio.soundMap(AUDIO_KEY), DELAY);
			}
			else
			{
				Audio.play(Audio.soundMap(AUDIO_KEY));
			}
		}
		else
		{
			if (DELAY != 0.0f)
			{
				Audio.playWithDelay(AUDIO_KEY, DELAY);
			}
			else
			{
				Audio.play(AUDIO_KEY);
			}
		}

		if (waitForFinish)
		{
			yield return new TIWaitForSeconds(Audio.getAudioClipLength(AUDIO_KEY));
		}
	}
}
