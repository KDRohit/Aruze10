using UnityEngine;
using System.Collections;

/**
 * Class for holding basic music information to make use of in derived action classes
 */
public class PickingGameMusicModule : PickingGameModule
{
	[SerializeField] protected bool isImmediate = false;
	[SerializeField] protected string INTRO_AUDIO_KEY = ""; // intro music track that will play first then proceed into AUDIO_KEY, if isn't set it is ignored
	[SerializeField] protected string AUDIO_KEY = "";
	[SerializeField] protected float FADE_OUT = 1.0f; // fadeout time for immediate music switches

	protected IEnumerator playMusic()
	{
		if (isImmediate)
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(AUDIO_KEY), FADE_OUT);
		}
		else
		{
			// only play the intro sounds if immediate isn't set, because that would override anything we played
			playIntroMusic();
			Audio.switchMusicKey(Audio.soundMap(AUDIO_KEY));
		}

		yield break;
	}

	// play intro music if set before looping into music, only if isImmediate isn't set
	private void playIntroMusic()
	{
		if (INTRO_AUDIO_KEY != "")
		{
			Audio.playMusic(Audio.soundMap(INTRO_AUDIO_KEY));
		}
	}
}
