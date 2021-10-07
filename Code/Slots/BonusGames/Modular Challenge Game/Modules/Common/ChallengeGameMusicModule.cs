using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Interface for making modules that play music

Creation Date: February 14, 2018
Orignal Author: Scott Lepthien
*/
public abstract class ChallengeGameMusicModule : ChallengeGameModule 
{
	[SerializeField] protected bool isImmediate = false;
	[Tooltip("Sometimes you may have an intro music clip into a looped music clip (don't use immediate if you want this to work)")]
	[SerializeField] protected string INTRO_AUDIO_KEY = "";
	[Tooltip("The looped music key")]
	[SerializeField] protected string AUDIO_KEY = "";
	[Tooltip("Fadeout time for immediate music switches")]
	[SerializeField] protected float FADE_OUT = 1.0f;

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
