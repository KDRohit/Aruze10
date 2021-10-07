using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This is a script that will add and initialize some sounds for the Audio system.
*/
public class AudioLibrary : TICoroutineMonoBehaviour
{
	[System.Serializable] public class AudioInfoLibEntry
	{
		public string keyName;
		public AudioClip clip;
		public float volume = 1f;
		public float pitch = 0f;
		public float delay = 0f;
		public float noReplayWindow = 0.15f;
		public float rangeIn3d = 50f;
	}

	[System.Serializable] public class PlaylistInfoLibEntry
	{
		public string keyName;
		public string[] audioKeys;
		public bool randomStartTrack = true;
		public bool shuffleTracks = true;
	}

	public AudioInfoLibEntry[] audioInfoItems;
	public PlaylistInfoLibEntry[] playlistInfoItems;
	
	void Awake()
	{
	/* TODO: Fix this up
		foreach (AudioInfoLibEntry entry in audioInfoItems)
		{
			new AudioInfo(
				entry.keyName,
				entry.clip,
				entry.volume,
				entry.pitch,
				entry.delay,
				entry.noReplayWindow,
				entry.rangeIn3d);
		}
	*/	
		foreach (PlaylistInfoLibEntry entry in playlistInfoItems)
		{
			List<string> sampleList = new List<string>();
			foreach (string audioKey in entry.audioKeys)
			{
				AudioInfo audioInfo = AudioInfo.find(audioKey);
				if (audioInfo != null)
				{
					sampleList.Add(audioKey);
				}
				else
				{
					Debug.LogWarning("Missing AudioInfo instance for key: " + audioKey);
				}
			}
			
			if (sampleList.Count > 0)
			{
				new PlaylistInfo(entry.keyName, entry.randomStartTrack, entry.shuffleTracks, sampleList);
			}
			else
			{
				Debug.LogWarning("No valid AudioInfo samples to create PlaylistInfo with key: " + entry.keyName);
			}
		}
	}
	
	void Start()
	{
		// Self destruct once your work is done
		Destroy(this);
	}
}
