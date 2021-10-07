using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
An AudioChannel represents an abstraction of tag labels applied to any given audio clip.
Channel specific settings are updated each frame via updateAudioChannels(), which is
called in the Audio MonoBehaviour.
*/
public class AudioChannel
{
	public string keyName;	// The key name of this audio channel tag
	
	public float currentVolume;	// The current volume level of this channel
	public float originalVolume // The volume that this channel starts at and will reset to
	{
		get;
		private set;
	}

	public class DuckData
	{
		public DuckData()
		{
			isUpdated = false;
			level = 0;
			channel = null;
		}
		public bool isUpdated;
		public float level;
		public AudioChannel channel;
	}

	public int lookUpIndex = 0;
	public static int nextLookUpIndex = 0;
	public static DuckData[] duckData = new DuckData[MAX_DUCK_CHANNELS];
	public float duckingLevel { get; private set; }		// The current ducking level applied to this channel
	public bool isAbortBlocked { get; private set; }	// Is this channel currently blocked by abort logic?
	
	/// Dictionary set of PlayingSounds that influence ducking, the Value is meaningless
	/// duckControls is not actually used anywhere so don't be fooled, I think the purpose was to maintain a
	/// list or playing audio for quick iteration when ducking, which is a great idea, we should look into it some more
	private Dictionary<PlayingAudio, bool> duckControls = new Dictionary<PlayingAudio, bool>();
	
	/// Dictionary set of PlayingSounds that influence aborting, the Value is meaningless
	private Dictionary<PlayingAudio, bool> abortControls = new Dictionary<PlayingAudio, bool>();
	
	/// All of the channels, keyed by channel tag
	private static Dictionary<string, AudioChannel> all = new Dictionary<string, AudioChannel>();
	
	/// Cached storage space for ducked channel calculations during updates
	//private static Dictionary<AudioChannel, float> duckLevels = new Dictionary<AudioChannel, float>();

	private const string KEY_NAME_JSON_KEY = "key_name";
	private const string VOLUME_JSON_KEY = "volume";
	private const int MAX_DUCK_CHANNELS = 2000;

	// Populate all channels from global data
	public static void populateAll(JSON[] channelsJson)
	{
		for (int i = 0; i < channelsJson.Length; i++)
		{
			JSON channelData = channelsJson[i];
			string keyName = channelData.getString(KEY_NAME_JSON_KEY, "");
			float originalVolume = channelData.getFloat(VOLUME_JSON_KEY, 1.0f);
			AudioChannel currentChannel = findOrCreateChannel(keyName);
			if (currentChannel != null)
			{
				currentChannel.originalVolume = originalVolume;
				currentChannel.currentVolume = originalVolume;	
			}
			else
			{
				string channelName = string.IsNullOrEmpty(keyName) ? "[null]" : keyName;
				Debug.LogError("Could not create audio channel: " + channelName);
			}
		}
	}

	public static int totalChannels()
	{
		return all.Count;
	}
	
	/// Please create new instances using the factory method findOrCreateChannel()
	private AudioChannel(string keyName, float originalVolume)
	{
		this.keyName = keyName;
		this.originalVolume = originalVolume;
		resetChannel();
		if (Data.debugMode)
		{
			if (all.ContainsKey(keyName))
			{
				Debug.LogError("Duplicate audio channel key: " + keyName);
			}
		}
		all[this.keyName] = this;

		this.lookUpIndex = nextLookUpIndex++;
		
		if (lookUpIndex < MAX_DUCK_CHANNELS)
		{
			if (duckData[lookUpIndex] == null)
			{
				duckData[lookUpIndex] = new DuckData();
			}
			duckData[lookUpIndex].channel = this;
		}
		else
		{
			Debug.LogError("Invalid look up index");
			return;
		}
	
		switch (keyName)
		{
			case Audio.SOUND_CHANNEL_KEY:
				Audio.soundChannel = this;
				break;
			case Audio.MUSIC_CHANNEL_KEY:
				Audio.musicChannel = this;
				break;
		}
		
		//Debug.Log("adding keyname : " + keyName);
	}
	
	/// Resets a channel completely, trashing all channel data.
	/// Use carefully, or don't use at all!
	public void resetChannel()
	{
		currentVolume = originalVolume;
		duckingLevel = 1f;
		isAbortBlocked = false;
		duckControls.Clear();
		abortControls.Clear();
	}
	
	/// Add a tracked PlayingAudio which influences ducking
	/// SFS TBDUselessCode this code serves no purpose yet gets called
	/// duckControls is not actually used anywhere so don't be fooled
	public void pushDuck(PlayingAudio playing)
	{
		// uncomment this for debugging some audio problems 
//		if (playing != null)
//		{
//			foreach (AudioDuckInfo duckInfo in playing.audioInfo.duckChannels)
//			{
//				if (duckInfo.channel.keyName == this.keyName)
//				{
//					Debug.LogWarning(string.Format("{0} is ducking the same channel it is on!!!", playing.audioInfo.keyName));
//				}
//			}
//		}
		//Debug.Log(string.Format("{0} is ducking {1}", playing.audioInfo.keyName, this.keyName));
		if (!duckControls.ContainsKey(playing))
		{
			duckControls.Add(playing, true);
		}
	}
	
	/// Remove a tracked PlayingAudio which influences ducking
	/// SFS TBDUselessCode this code serves no purpose yet gets called
	/// duckControls is not actually used anywhere so don't be fooled
	public void popDuck(PlayingAudio playing)
	{
		duckControls.Remove(playing);
	}
	
	/// Add a tracked PlayingAudio which influences aborting
	public void pushAbort(PlayingAudio playing)
	{
		abortControls.Add(playing, true);
	}
	
	/// Remove a tracked PlayingAudio which influences aborting
	public void popAbort(PlayingAudio playing)
	{
		abortControls.Remove(playing);
	}

	/// Iterates over all channels, updating ducking levels and the global abort list
	public static int numChannels = 0;
	public static void updateAudioChannels()
	{
		// First pass over channels sets the blocked status and initializes duck level storage
		int allCount = all.Count;
		if (allCount > MAX_DUCK_CHANNELS)
		{
			allCount = MAX_DUCK_CHANNELS;
			Debug.LogError("Max ducking channels reached.");
		}
		
		for (int i = 0; i < allCount; i++)
		{
			if (duckData[i] == null)
			{
				duckData[i] = new DuckData();
			}
			duckData[i].isUpdated = false;
			duckData[i].level = 1.0f;
		}
		
		// Check each playing audio clip to see where ducking levels should be
		float elapsed;
		float duckBlend;
		float duckLevel;
		float unduckStartTime;

		int numPlayingAudios = Audio.poolPlaying == null ? 0 : Audio.poolPlaying.Length;
		for (int i=0; i < numPlayingAudios; i++)
		{
			PlayingAudio playing = Audio.poolPlaying[i];
			if (playing == null)
			{
				Debug.LogError("Null audio");
				continue;
			}
			if (playing.isPlaying)
			{
				if (playing.audioInfo == null)
				{
					Debug.LogError("Null audio info");
					continue;
				}
				
				elapsed = playing.elapsedTime;
				int channelCount = playing.audioInfo.duckChannelCount;
				
				for (int j=0; j < channelCount; ++j)
				{
					AudioDuckInfo duckInfo = playing.audioInfo.duckChannels[j];
					if (duckInfo == null)
					{
						Debug.LogError("Null duckinfo");
						continue;
					}
					//Debug.Log(("mucking with channel " + duckInfo.channel.keyName));
					if (elapsed < duckInfo.startDuration)
					{
						duckBlend = Mathf.Clamp01(elapsed / duckInfo.startDuration);
						duckLevel = Mathf.Lerp(1.0f, duckInfo.volume, duckBlend);
					}
					else 
					{
						if (playing.audioInfo.clip != null)
						{
							unduckStartTime = playing.audioInfo.clip.length * (playing.audioInfo.loops + 1) +
							                  duckInfo.endOffset;
							if (elapsed > unduckStartTime)
							{
								duckBlend = Mathf.Clamp01((elapsed - unduckStartTime) / duckInfo.endDuration);
								duckLevel = Mathf.Lerp(duckInfo.volume, 1.0f, duckBlend);
							}
							else
							{
								duckLevel = duckInfo.volume;
							}
						}
						else
						{
							duckLevel = duckInfo.volume;
						}
					}

					if (duckInfo.channel == null)
					{
						Debug.LogError("Invalid duckinfo channel");
						continue;
					}
					
					int index = duckInfo.channel.lookUpIndex;
					if (index < 0 || index >= duckData.Length)
					{
						Debug.LogError("Invalid index");
						continue;
					}
					DuckData data = duckData[index];
					if (data != null)
					{
						if (!data.isUpdated)
						{
							data.level = 1.0f;
							data.isUpdated = true;
						}

						if (duckLevel < data.level)
						{
							data.level = duckLevel;
						}
					}
					else
					{
						Debug.LogError("invalid data");
					}
				}
			}
		}
		
		// Finally, set the final ducking levels in each channel
		for (int i = 0; i < allCount; i++)
		{
			if (duckData[i] == null || duckData[i].channel == null)
			{
				Debug.LogError("Channel object is null");
				continue;
			}

			duckData[i].channel.duckingLevel = duckData[i].level;
		}
	}
	
	/// Calculate the volume of some playing audio
	public static float calculateVolume(AudioInfo audioInfo, float relativeVolume)
	{
		float overallChannelVolume = 1f;
		
		// Scan the tagged channels to find the volume and ducking level
		// TBDOPTIMIZE use array instead of list for channelTags, this gets called a lot
		// and can eat up .2% of a current frames CPU time
		foreach (AudioChannel channel in audioInfo.channelTags)
		{
			overallChannelVolume *= channel.currentVolume * channel.duckingLevel;
			if (overallChannelVolume == 0f)
			{
				// no need to do continue if we at zero
				return 0f;
			}
		}

		if (audioInfo.volume < relativeVolume)
		{
			return (audioInfo.volume * overallChannelVolume);
		}
		
		return (relativeVolume * overallChannelVolume);
	}
	
	/// Check is any of the listed channels are current blocked via aborts
	public static bool getIsAborted(List<AudioChannel> channels)
	{
		foreach (AudioChannel channel in channels)
		{
			if (channel.abortControls.Count != 0)
			{
				return true;
			}
		}
		return false;
	}
	
	/// Cleans up (hard resets) any channel ducking/abort data.
	/// This should be called by Audio.cs when initializing in a new scene.
	public static void resetAllChannels()
	{
		foreach (KeyValuePair<string, AudioChannel> p in all)
		{
			p.Value.resetChannel();
		}
	}
	
	/// Returns a channel with the given key, creating it if necessary
	public static AudioChannel findOrCreateChannel(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			Debug.LogWarning("invalid channel key");
			return null;
		}
		if (all.ContainsKey(key))
		{
			//Debug.Log(("RETURNING  channel " + key));
			return all[key];
		}
		else
		{
			//Debug.Log(("creating  channel " + key));
			AudioChannel channel = new AudioChannel(key, 1.0f);
			return channel;
		}
	}
	
	/// Find a given AudioChannel using its key, returning null if not found
	public static AudioChannel find(string key)
	{
		if (all.TryGetValue(key, out AudioChannel channel))
		{
			return channel;
		}
		else
		{
			return null;
		}
	}

	private static float getDuckingValueForChannel(string key)
	{
		if (all.TryGetValue(key, out AudioChannel channel))
		{
			return channel.duckingLevel;
		}
		else
		{
			Debug.LogWarning("AudioChannel.getDuckingValueForChannel() - Unable to find key = " + key);
			return 0;
		}
	}

	// Get the AudioChannel ducking info so it can be rendered in the audio menu
	public static Dictionary<string, float> getImportantAudioChannelDuckingValues()
	{
		Dictionary<string, float> channelDuckValues = new Dictionary<string, float>();

		channelDuckValues.Add("type_vo", getDuckingValueForChannel("type_vo"));
		channelDuckValues.Add("type_fx", getDuckingValueForChannel("type_fx"));
		channelDuckValues.Add("type_sound", getDuckingValueForChannel("type_sound"));
		channelDuckValues.Add("type_feedback", getDuckingValueForChannel("type_feedback"));
		channelDuckValues.Add("type_music", getDuckingValueForChannel("type_music"));
		channelDuckValues.Add("type_fanfare", getDuckingValueForChannel("type_fanfare"));
		channelDuckValues.Add("type_ambience", getDuckingValueForChannel("type_ambience"));

		return channelDuckValues;
	}
}

