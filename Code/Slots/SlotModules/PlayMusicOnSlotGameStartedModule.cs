using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Intended to handle cases such as in freespins games where afer a transition
we want to play the freespins music (normally this happens during the transition
but sometimes we want it afterwards, which is blocked from happening by code
in FreeSpinGame).  So we will now have this module to turn on music when the game
starts.
*/
public class PlayMusicOnSlotGameStartedModule : SlotModule 
{
	[SerializeField] private string MUSIC_KEY = "";
	[SerializeField] private float MUSIC_DELAY = 0.0f;
	[SerializeField] private float MUSIC_FADE_TIME = 0.0f;

	//executeOnSlotGameStartedNoCoroutine() section
	//executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		bool hasMusicKey = !string.IsNullOrEmpty(MUSIC_KEY);

		if (hasMusicKey)
		{
			return true;
		}
		else
		{
			Debug.LogWarning("PlayMusicOnSlotGameStartedModule.needsToExecuteOnSlotGameStartedNoCoroutine() - MUSIC_KEY was null or empty, this module isn't going to do anything!");
			return false;
		}
	}

	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		if (MUSIC_DELAY == 0.0f)
		{
			string keyToPlay = Audio.tryConvertSoundKeyToMappedValue(MUSIC_KEY);
			Audio.switchMusicKeyImmediate(keyToPlay, MUSIC_FADE_TIME);
		}
		else
		{
			StartCoroutine(playMusicAfterDelay());
		}
	}
	
	private IEnumerator playMusicAfterDelay()
	{
		yield return new TIWaitForSeconds(MUSIC_DELAY);
		string keyToPlay = Audio.tryConvertSoundKeyToMappedValue(MUSIC_KEY);
		Audio.switchMusicKeyImmediate(keyToPlay, MUSIC_FADE_TIME);
	}
}
