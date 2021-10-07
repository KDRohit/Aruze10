using UnityEngine;
using System.Collections;

//This module will play music on awake and then queue up the next music you want.  
//This is for games like LIS01 where an animation will keep the freespins intro music 
//from playing, and we want to start the music for the game before beginFreeSpinMusic().
public class PlayOneShotMusicOnAwakeModule : SlotModule
{
	[SerializeField] private float musicDelay = 0.0f;
	[SerializeField] private string musicKey = "freespinintro";
	//Leave this blank if you dont want to queue up music
	[SerializeField] private string musicToQueue = "freespin";

	//This can be used if the one shot music and music to queue up need to line up precisely and if queueing the next music is introducing a slight gap	
	[SerializeField] private float queueTime = -1.0f;

	public override void Awake()
	{
		base.Awake();
		StartCoroutine(playMusicOneShot());
	}

	private IEnumerator playMusicOneShot()
	{
		yield return new TIWaitForSeconds(musicDelay);
		Audio.playMusic(Audio.tryConvertSoundKeyToMappedValue(musicKey));
		if (queueTime >= 0.0f)
		{
			yield return new TIWaitForSeconds(queueTime);
			Audio.switchMusicKeyImmediate(Audio.tryConvertSoundKeyToMappedValue(musicToQueue));
		}
		else
		{
			if (!string.IsNullOrEmpty(musicToQueue))
			{
				Audio.switchMusicKey(Audio.tryConvertSoundKeyToMappedValue(musicToQueue));
			}
		}
	}
}