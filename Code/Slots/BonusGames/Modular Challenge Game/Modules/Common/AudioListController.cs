using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * A wrapper class play a collection of sounds concurrently.
 */
public static class AudioListController
{
	[System.Serializable]
	public class AudioInformation
	{
		[Tooltip("If this sound name is blank then this entry will be ignored. Otherwise it looks for the sound mapped key and then an audio with the same name.")]
		public string SOUND_NAME = "";
		[Tooltip("Delay before the sound gets played or the music is changed.")]
		public float delay = 0.0f;
		[Tooltip("Controls if this sound call will be blocking. (Hidden for isDoingSwitchMusicKey since that can't block)")]
		public bool isBlockingModule = false;
		
#region Music
		[Tooltip("Changes this to switch the looped music key, instead of doing a play sound call.")]
		public bool isDoingSwitchMusicKey = false;
		[Tooltip("When isDoingSwitchMusicKey is in effect, this controls the fade out time of the current music before the new music is played.")]
		public float musicFadeTime = 0.0f;
		[Tooltip("Tells if the switchMusicKey call should be immediate or a standard call which will queue it to play when the current looped music track reaches its end. (Default is to play immediate).")]
		public bool isNotPlayingMusicImmediate = false; // Note: Have to use negative here because Unity doesn't actually read default values for serialized array classes (so having a field like isPlayingMusicImmediate that defaults to true isn't possible)
#endregion

		public AudioInformation(string name, float delay = 0.0f, bool isBlockingModule = false)
		{
			this.SOUND_NAME = name;
			this.delay = delay;
			this.isBlockingModule = isBlockingModule;
		}

		public AudioInformation clone()
		{
			return (AudioInformation) this.MemberwiseClone();
		}
	}

	[System.Serializable]
	public class AudioInformationList
	{
		public List<AudioInformation> audioInfoList = new List<AudioListController.AudioInformation>();

		public AudioInformationList(string soundName = null, float delay = 0.0f, bool isBlocking = false)
		{
			if (!string.IsNullOrEmpty(soundName))
			{
				addSound(soundName, delay, isBlocking);
			}
		}
		
		public int Count
		{
			get { return audioInfoList.Count; }
		}

		public void resetCollection()
		{
			foreach (AudioInformation info in audioInfoList)
			{
				Audio.resetCollectionBySoundMapOrSoundKey(info.SOUND_NAME);
			}
		}

		// Tells if this AudioInformationList will switch the looped music track
		public bool isGoingToSwitchMusicKey()
		{
			foreach (AudioInformation info in audioInfoList)
			{
				if (info.isDoingSwitchMusicKey)
				{
					return true;
				}
			}

			return false;
		}

		public void addSound(string name, float delay = 0.0f, bool isBlockingModule = false)
		{
			audioInfoList.Add(new AudioInformation(name, delay, isBlockingModule));
		}

		public AudioInformationList clone()
		{
			AudioInformationList clone = (AudioInformationList) this.MemberwiseClone();
			clone.audioInfoList = new List<AudioInformation>();

			foreach (AudioInformation audioInformation in audioInfoList)
			{
				clone.audioInfoList.Add(audioInformation.clone());
			}

			return clone;
		}
	}

	// Helper function to see if any of the audio information in the supplied list should be blocking.
	public static bool isAnyOfListBlocking(AudioInformationList infos)
	{
		foreach (AudioInformation info in infos.audioInfoList)
		{
			if (info.isBlockingModule)
			{
				return true;
			}
		}
		return false;
	}

	public static void changeSoundName(AudioInformationList infos, string oldName, string newName)
	{
		foreach (AudioInformation info in infos.audioInfoList)
		{
			if (info.SOUND_NAME == oldName)
			{
				info.SOUND_NAME = newName;
			}
		}		
	}

	// Plays a list of Audio information, if running coroutines are passed to this it waits for all blocking audio in the infos list
	// to finish running and all the coroutines in the runningAnimaitons list to finish running too.
	public static IEnumerator playListOfAudioInformation(AudioInformationList infos, List<TICoroutine> runningCoroutines = null, AnimationListController.AnimationInformationList animInfoList = null)
	{
		if (runningCoroutines == null)
		{
			// If we were not passed any coroutines that were already running then we don't need to worry about waiting for stuff to finish.
			runningCoroutines = new List<TICoroutine>();
		}
		foreach (AudioInformation info in infos.audioInfoList)
		{
			if (info.isBlockingModule)
			{
				runningCoroutines.Add(RoutineRunner.instance.StartCoroutine(playAudioInformation(info)));
			}
			else
			{
				RoutineRunner.instance.StartCoroutine(playAudioInformation(info));
			}
		}
		
		// if a animInfoList was passed in, and it is allowing click to cancel we need to track
		// the coroutines created in here
		if (animInfoList != null && animInfoList.isAllowingTapToSkip)
		{
			animInfoList.coroutineTracker.addTrackedCoroutineList(RoutineRunner.instance, runningCoroutines);
		}

		yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
	}

	public static IEnumerator playAudioInformation(AudioInformation info)
	{
		if (info != null)
		{
			if (!string.IsNullOrEmpty(info.SOUND_NAME))
			{
				if (info.delay > 0.0f)
				{
					yield return new TIWaitForSeconds(info.delay);
				}

				string soundName = Audio.tryConvertSoundKeyToMappedValue(info.SOUND_NAME);

				if (info.isDoingSwitchMusicKey)
				{
					if (info.isNotPlayingMusicImmediate)
					{
						Audio.switchMusicKey(soundName, info.musicFadeTime);
					}
					else
					{
						Audio.switchMusicKeyImmediate(soundName, info.musicFadeTime);
					}
					// No wait here since this is just changing the looped track, so blocking doesn't make sense
				}
				else
				{
					float lengthOfAudioClip = Audio.getAudioClipLength(soundName);
					Audio.play(soundName);
					yield return new TIWaitForSeconds(lengthOfAudioClip);
				}
			}
		}
	}
}
