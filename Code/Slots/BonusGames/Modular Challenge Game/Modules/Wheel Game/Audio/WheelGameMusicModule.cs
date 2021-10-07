
using UnityEngine;
using System.Collections;

/**
 * Class for holding basic music information to make use of in derived action classes
 */
public class WheelGameMusicModule : WheelGameModule
{
	[SerializeField] protected bool isImmediate = false;
	[SerializeField] protected string INTRO_AUDIO_KEY = "";	// Sometimes you may have an intro music clip into a looped music clip (don't use immediate if you want this to work)
	[SerializeField] protected string AUDIO_KEY = "";		// the looped music key
	[SerializeField] protected float FADE_OUT = 1.0f; // fadeout time for immediate music switches

	protected IEnumerator playMusic()
	{
		if (isImmediate)
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(AUDIO_KEY), FADE_OUT);
		}
		else
		{
			Audio.switchMusicKey(Audio.soundMap(AUDIO_KEY));
			if (INTRO_AUDIO_KEY != "")
			{
				Audio.playMusic(Audio.soundMap(INTRO_AUDIO_KEY));
			}
		}

		yield break;
	}
}
