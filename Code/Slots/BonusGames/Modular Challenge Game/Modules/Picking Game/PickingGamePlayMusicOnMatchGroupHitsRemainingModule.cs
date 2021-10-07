using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Special class made to interface with PickingGameMatchGroupModule and switch the music when a specific number of hits are remaining for a group

Original Author: Scott Lepthien

Creation Date: 4/13/2017
*/
public class PickingGamePlayMusicOnMatchGroupHitsRemainingModule : PickingGameModule 
{
	[SerializeField] private PickingGameMatchGroupModule matchGroupModule = null;
	[SerializeField] private int remainingHitTarget = 1;
	[SerializeField] protected string INTRO_MUSIC_KEY = ""; // intro music track that will play first then proceed into AUDIO_KEY, if isn't set it is ignored
	[SerializeField] protected string MUSIC_KEY = "";
	[SerializeField] protected float FADE_OUT = 1.0f; // fadeout time for immediate music switches

	bool isMusicSwitched = false; // tracks if the music track has been switched, since we can have an intro and a looped track this is easier to track and make sure we switch only once

	public override void Awake()
	{
		base.Awake();

		if (matchGroupModule == null)
		{
			Debug.LogError("PickingGamePlayMusicOnMatchGroupHitsRemainingModule.Awake() - matchGroupModule is NULL!");
			Destroy(this);
		}

		if (string.IsNullOrEmpty(MUSIC_KEY))
		{
			Debug.LogError("PickingGamePlayMusicOnMatchGroupHitsRemainingModule.Awake() - MUSIC_KEY isn't set!");
			Destroy(this);
		}
	}

	// execute when input is enabled (for example, turn on glows when you can pick).
	public override bool needsToExecuteOnInputEnabled()
	{
		if (matchGroupModule == null || MUSIC_KEY == "")
		{
			return false;
		}
		else
		{
			return !isMusicSwitched && matchGroupModule.hasGroupWithHitsRemaining(remainingHitTarget);
		}
	}
	
	public override IEnumerator executeOnInputEnabled()
	{
		if (!string.IsNullOrEmpty(INTRO_MUSIC_KEY))
		{
			Audio.playMusic(Audio.soundMap(INTRO_MUSIC_KEY));
			Audio.switchMusicKey(Audio.soundMap(MUSIC_KEY));
		}
		else
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(MUSIC_KEY), FADE_OUT);
		}
		isMusicSwitched = true;
		yield break;
	}
}
