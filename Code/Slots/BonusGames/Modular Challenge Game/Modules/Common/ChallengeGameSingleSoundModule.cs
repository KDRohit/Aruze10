using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Interface for making modules that play a single sound.
Note if you want to play mutliple sounds you should make a new version of this that uses AudioListController.
This class only needs to exist for legacy games that use it.

Creation Date: February 14, 2018
Orignal Author: Scott Lepthien
*/
public abstract class ChallengeGameSingleSoundModule : ChallengeGameModule 
{
	[SerializeField] protected string AUDIO_KEY = "";
	[SerializeField] protected bool waitForFinish = false;
	[SerializeField] protected float DELAY = 0.0f;

	protected IEnumerator playAudio()
	{
		if (DELAY != 0.0f)
		{
			Audio.playSoundMapOrSoundKeyWithDelay(AUDIO_KEY, DELAY);
		}
		else
		{
			Audio.playSoundMapOrSoundKey(AUDIO_KEY);
		}

		if (waitForFinish)
		{
			yield return new TIWaitForSeconds(Audio.getAudioClipLength(AUDIO_KEY));
		}
	}
}
