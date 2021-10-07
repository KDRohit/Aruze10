using UnityEngine;
using System.Collections;

public class PlayAmbientSoundsModule : SlotModule
{
	[SerializeField] protected float minInterval = 1.5f;             // Minimum time an animation might take to play next
	[SerializeField] protected float maxInterval = 4.0f;             // Maximum time an animation might take to play next	
	[SerializeField] protected string ambientSoundName = "";

	protected CoroutineRepeater ambientSoundController;

	public override void Awake()
	{
		base.Awake();
		ambientSoundController = new CoroutineRepeater(minInterval, maxInterval, soundCallback);
	}

	private void Update()
	{
		ambientSoundController.update();
	}

	protected virtual IEnumerator soundCallback()
	{		
		if (ambientSoundName != "")
		{
			PlayingAudio ambientSound = Audio.playSoundMapOrSoundKey(ambientSoundName);
			if (ambientSound != null)
			{
				yield return new TIWaitForSeconds(Audio.getAudioClipLength(ambientSound.audioInfo.keyName));
			}
		}
		yield break;
	}
}
