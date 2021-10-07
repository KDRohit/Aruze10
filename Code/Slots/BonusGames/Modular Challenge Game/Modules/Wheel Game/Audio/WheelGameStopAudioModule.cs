using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelGameStopAudioModule : WheelGameModule
{
	[SerializeField] private List<AudioStopData> audioStopData;

	public override bool needsToExecuteOnSpin()
	{
		foreach (AudioStopData audioToStop in audioStopData)
		{
			if (audioToStop.animationEventType == AudioStopData.AnimationEventType.executeOnSpin)
			{
				return true;
			}
		}

		return false;
	}

	public override IEnumerator executeOnSpin()
	{
		foreach (AudioStopData audioToStop in audioStopData)
		{
			if (audioToStop.animationEventType == AudioStopData.AnimationEventType.executeOnSpin)
			{
				StartCoroutine(audioToStop.stopAudio());
			}
		}

		yield break;
	}

	// Execute when the wheel has completed spinning
	public override bool needsToExecuteOnSpinComplete()
	{
		foreach (AudioStopData audioToStop in audioStopData)
		{
			if (audioToStop.animationEventType == AudioStopData.AnimationEventType.executeOnSpinComplete)
			{
				return true;
			}
		}

		return false;
	}

	public override IEnumerator executeOnSpinComplete()
	{
		foreach (AudioStopData audioToStop in audioStopData)
		{
			if (audioToStop.animationEventType == AudioStopData.AnimationEventType.executeOnSpinComplete)
			{
				StartCoroutine(audioToStop.stopAudio());
			}
		}

		yield break;
	}

	[Serializable]
	public class AudioStopData
	{
		public string soundName;
		public float callStopDelay;
		public float fadeOutTime;
		public float stopSoundDelay;
		public AnimationEventType animationEventType;

		public enum AnimationEventType
		{
			executeOnSpin,
			executeOnSpinComplete
		}

		public IEnumerator stopAudio()
		{
			yield return new WaitForSeconds(callStopDelay);
			PlayingAudio playingAudio = Audio.findPlayingAudio(soundName);
			Audio.stopSound(Audio.findPlayingAudio(soundName), fadeOutTime, stopSoundDelay);
		}
	}
}
