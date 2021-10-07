using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
AudioInfo stores the server provided data for a discrete audio element/clip.
This is referenced and used by PlayingAudio.cs
*/
public class AudioInfo : IResetGame
{
	public AudioClip clip { get; private set; }
	
	public string keyName { get; private set; }
	public string fallbackKey { get; private set; }
	public string clipKey { get; private set; }
	public string bundleKey { get; private set; } /* Used for testing */
	
	public float volume { get; private set; }
	public float pitch { get; private set; }
	public float delay { get; private set; }
	public float noReplayWindow { get; private set; }
	public float rangeIn3d { get; private set; }
	public float maximumUnduckTime { get; private set; }
	public bool isMissingData { get; private set; }
	public float pan {get; private set; }
	public float loops {get; private set; }
	public float beat {get; private set; }
	public float pickup {get; private set; } // Time before the beat should start happening.
	public int playedCount { get; set; } // for testing
	
	public List<AudioChannel> channelTags = null;
	public List<AudioDuckInfo> duckChannels = null;
	public int duckChannelCount = 0;
	public List<AudioAbortInfo> abortChannels = null;
	public List<AudioChannel> blockingChannels = null;
	public List<AudioEvent> audioEvents = null;
	public bool useDirectDownload = false;

	private const string DD_ASSET_PATH = "{0}Sounds/{1}";
	
	private float lastPlayedTime;
	public bool isLoading = false; // used in conjunction with loading audio from a url

	private const string CLONED_CLIP_POSTFIX = "_clone";
	
	// Can this audio play right now? Considers streaming, no-replay window, and aborted channels.
	public bool canPlay
	{
		get
		{
			return
				!isMissingData &&
				clip != null &&
				((Time.realtimeSinceStartup - lastPlayedTime) > noReplayWindow) &&
				!AudioChannel.getIsAborted(blockingChannels) &&
				(!Audio.muteMusic || !hasChannelTag(Audio.MUSIC_CHANNEL_KEY)) &&
				(!Audio.muteSound || !hasChannelTag(Audio.SOUND_CHANNEL_KEY));
		}
	}
	
	// Store the key to AudioInfo in uppercase since clip names ignore casing.
	// When find() is called for a AudioInfo, the passed key is converted to uppercase to find an AudioInfo.
	// This should solve any mismatches in title casing for sound keys -> clip name.
	public static Dictionary<string, AudioInfo> all = new Dictionary<string, AudioInfo>();
	
	// A method that call's populate all and then returns the all dictionary called and cleans up what it used.
	public static Dictionary<string, AudioInfo>  testPopulateAll(JSON[] audioData)
	{
		// Make sure everything is reset.
		resetStaticClassData();
		populateAll(audioData);
		Dictionary<string, AudioInfo> result = new Dictionary<string, AudioInfo>(all);
		// Put it back into the clean state.
		resetStaticClassData();
		return result;
	}

	// Populate all the audio data using provided server data
	public static void populateAll(JSON[] audioData)
	{
		int zeroTagCount = 0;
		int totaladded = 0;

		foreach (JSON audioEntry in audioData)
		{
			//Debug.LogErrorFormat("audioEntry: {0}", audioEntry.ToString());
			string keyName = audioEntry.getString("key_name", "");
			
			// check for duplicate key before we do a whole bunch of memory allocations
			// the constructor used to do this check after all sorts of allocations had already been done
			string keyNameUpper = keyName.ToUpper();	// TBD_OPTIMIZE slow and allocates GC mem
			if (all.ContainsKey(keyNameUpper))
			{
				// this can  happen when loading certain games like TV games and is ok
				// this warning spams the log and causes 2.5 mb in gc memory allocations from the warning alone whenever a game is loaded for the first time
				//Debug.LogError("Duplicate AudioInfo key: " + keyNameUpper);
				continue;
			}

			totaladded++;
			
			string fallbackKey = audioEntry.getString("fallback_key_name", "");
			string bundleKey = audioEntry.getString("asset_index_key", "");
			string clipKey = audioEntry.getString("file_name", "");			
			float beat = 0.0f;
			float pickup = 0.0f;
			float volume = audioEntry.getFloat("volume", 1f);
			float pitch = audioEntry.getFloat("pitch", 0f);
			float delay = audioEntry.getFloat("delay", 0f);
			float noReplayWindow = audioEntry.getFloat("no_replay_window", 0f);
			float rangeIn3d = audioEntry.getFloat("range_in_3d", 400f);
			float pan = audioEntry.getFloat("pan", 0f);
			float loops = 0;
			
			//Debug.Log(("****************************************making audioinfo for " + keyName));			

			// Note: this code is meant to strip strings like this: "SWF/shared/partner_powerup_sounds.swf"
			//       down to just bundlename "partner_powerup_sounds", which will then have "Sounds/" prepended to it
			//       in prepareClip(), because that is where all sounds for that bundle are assumed to be.

			// Quick fix to the bundle key.
			if (bundleKey.Contains('/'))
			{
				bundleKey = bundleKey.Substring(bundleKey.LastIndexOf("/") + 1);
			}
			
			if (bundleKey.Contains("_sounds"))
			{
				bundleKey = bundleKey.Remove(bundleKey.IndexOf("_sounds"));
			}

			if (bundleKey.Contains(".swf"))
			{
				bundleKey = bundleKey.Remove(bundleKey.IndexOf(".swf"));
			}
			
			// Build the channel tag list
			List<AudioChannel> channelTags = new List<AudioChannel>();
			bool isMissingMuteChannel = true;

			int totalTags = 0;
			foreach (string tag in audioEntry.getStringArray("tags"))
			{
				totalTags++;
				channelTags.Add(AudioChannel.findOrCreateChannel(tag));
				if (isMissingMuteChannel && (tag == Audio.SOUND_CHANNEL_KEY || tag == Audio.MUSIC_CHANNEL_KEY))
				{
					isMissingMuteChannel = false;
				}
			}

			if (totalTags == 0)
			{
				zeroTagCount++;
				// TBD_DEVPANEL note the 5 infos out of 2000 that have zero channel tags in dev panel
				// TBD_DEVPANEL log totalAdded to dev panel
				//Debug.LogWarning(("keyName has zero tags  " + keyName));
			}
			
			// If there is no mute channel assigned, then someone forgot to add one
			if (isMissingMuteChannel)
			{
				switch (bundleKey)
				{
					case "resources_audio_music":
						channelTags.Add(AudioChannel.findOrCreateChannel(Audio.MUSIC_CHANNEL_KEY));
						break;
					default:
						channelTags.Add(AudioChannel.findOrCreateChannel(Audio.SOUND_CHANNEL_KEY));
						break;
				}
				// Remove this for now.
				//Debug.LogWarning(string.Format("Audio warning: someone forgot to add a muting channel to {0}!", keyName))
			}				
			
			// Build the ducked channel list
			List<AudioDuckInfo> duckChannels = new List<AudioDuckInfo>();
			foreach (JSON duckInfo in audioEntry.getJsonArray("duck_tags"))
			{
				string tag = duckInfo.getString("tag", "");
				
				AudioChannel duckChannel = AudioChannel.findOrCreateChannel(tag);
				float duckVolume = duckInfo.getFloat("volume", 0.5f);
				float duckStartDuration = duckInfo.getFloat("start_duration", 0f);
				float duckEndDuration = duckInfo.getFloat("end_duration", 0f);
				float duckEndOffset = duckInfo.getFloat("end_offset", 0f);
				
				duckChannels.Add(new AudioDuckInfo(
					duckChannel,
					duckVolume,
					duckStartDuration,
					duckEndDuration,
					duckEndOffset));
			}
			
			// Build the abort channel list
			List<AudioAbortInfo> abortChannels = new List<AudioAbortInfo>();
			foreach (JSON abortInfo in audioEntry.getJsonArray("abort_tags"))
			{
				string tag = abortInfo.getString("tag", "");
				
				AudioChannel abortChannel = AudioChannel.findOrCreateChannel(tag);
				float abortFadeDuration = abortInfo.getFloat("fade_out_duration", 0.5f);
				
				abortChannels.Add(new AudioAbortInfo(
					abortChannel,
					abortFadeDuration));
			}
			
			// Build the blocking channel list, which consists of all the
			// channels from channelTags that are not in abortChannels.
			List<AudioChannel> blockingChannels = new List<AudioChannel>();
			foreach (AudioChannel channel in channelTags)
			{
				bool isBlocking = true;
				foreach (AudioAbortInfo abort in abortChannels)
				{
					if (abort.channel == channel)
					{
						isBlocking = false;
						break;
					}
				}
				
				if (isBlocking)
				{
					blockingChannels.Add(channel);
				}
			}
			
			// Build the audio events list
			List<AudioEvent> audioEvents = new List<AudioEvent>();
			foreach (JSON eventInfo in audioEntry.getJsonArray("events"))
			{
				string eventMessage = eventInfo.getString("key_name", "");
				float eventTime = eventInfo.getFloat("trigger_time", 0f);
				audioEvents.Add(new AudioEvent(eventMessage, eventTime));
				
				if (eventMessage == "queue_loop")
				{
					loops = float.PositiveInfinity;
				}

				if (eventMessage == "beat")
				{
					beat = eventTime;
				}
				else if (eventMessage == "pickup")
				{
					pickup = eventTime;
				}
			}
						
			all.Add(keyNameUpper, new AudioInfo(
					keyName,
					fallbackKey,
					bundleKey + "/" + clipKey,
					bundleKey, /* Used for testing */
					null,
					volume,
					pitch,
					delay,
					noReplayWindow,
					rangeIn3d,
					pan,
					loops,
					beat,
					pickup,
					channelTags,
					duckChannels,
					abortChannels,
					blockingChannels,
					audioEvents));
		}
		
		// Feb 2019 on startup after processing the JSON there are 1468 AudioInfo entries and 226 AudioChannels
		//Debug.Log(("total audio entrys processed in json is " + totaladded + " list size is " + all.Count));
		//Debug.Log(("total channels " + AudioChannel.totalChannels()));

	}
	
	public AudioInfo(
		string keyName,
		string fallbackKey,
		string clipKey,
		string bundleKey, /* Used for testing */
		AudioClip clip,
		float volume,
		float pitch,
		float delay,
		float noReplayWindow,
		float rangeIn3d,
		float pan,
		float loops,
		float beat,
		float pickup,
		List<AudioChannel> channelTags = null,
		List<AudioDuckInfo> duckChannels = null,
		List<AudioAbortInfo> abortChannels = null,
		List<AudioChannel> blockingChannels = null,
		List<AudioEvent> audioEvents = null)
	{
		this.keyName = keyName;
		this.fallbackKey = fallbackKey;
		this.clipKey = clipKey;
		this.bundleKey = bundleKey; // Used for testing.
		this.clip = clip;
		this.volume = volume;
		this.pitch = pitch;
		this.delay = delay;
		this.noReplayWindow = noReplayWindow;
		this.rangeIn3d = rangeIn3d;
		this.pan = pan;
		this.loops = loops;
		this.beat = beat;
		this.pickup = pickup;
		this.channelTags = channelTags ?? new List<AudioChannel>();
		this.duckChannels = duckChannels ?? new List<AudioDuckInfo>();
		this.abortChannels = abortChannels ?? new List<AudioAbortInfo>();
		this.blockingChannels = blockingChannels ?? new List<AudioChannel>();
		this.audioEvents = audioEvents ?? new List<AudioEvent>();
		
		lastPlayedTime = 0f;
		isMissingData = false;
		
		// Calculate the maximum time needed after the clip plays to unduck all ducked channels
		maximumUnduckTime = 0f;
		foreach (AudioDuckInfo duckInfo in this.duckChannels)
		{
			float deltaTime = duckInfo.endOffset + duckInfo.endDuration;
			if (deltaTime > 0.0f)
			{
				maximumUnduckTime = Mathf.Max(maximumUnduckTime, deltaTime);
			}
		}

		this.duckChannelCount = this.duckChannels.Count;
	}

	// Get the length of an audio clip
	public float getClipLength()
	{
		if (clip == null)
		{
			// try to prepare the clip
			prepareClip();

			if (clip == null)
			{
				Debug.LogWarning("AudioInfo.getClipLength() - Couldn't load the audio clip!  Returning zero length!");
				return 0.0f;
			}
		}

		return clip.length;
	}
	
	// Gets the AudioClip from the associated bundle
	// Return true if the clip is prepared, false otherwise
	public bool prepareClip()
	{
		if (clip != null)
		{
			// If we have the clip, then it is all prepared and ready to play
			return true;
		}
		else if (isMissingData)
		{
			// If we've already flagged this as missing data, return false now
			if (Data.debugMode && DevGUIMenuAudio.trackAudio)
			{
				DevGUIMenuAudio.logAudioError(this, "Prepare Clip : isMissingData = true");
			}				
			return false;
		}

		// assumes clipKey is [bundle_name]/[soundname] and lives under Sounds/[bundle_nam]e dir
		string clipResourcePath = "Sounds/" + clipKey;

	//	Debug.LogErrorFormat("loadSkuSpecificResourceWAV: {0}", clipResourcePath);
		if (AssetBundleManager.hasInstance())
		{
			// AssetBundleManager must be created before checking bundles
			clip = SkuResources.loadSkuSpecificResourceWAV(clipResourcePath);
		}
		else
		{
			// else we only try loading from embedded resources
			clip = SkuResources.loadSkuSpecificEmbeddedResourceWAV(clipResourcePath);
		}
		if (clip != null)
		{
			// Clip is locally available, so use it.
			return true;
		}

		if (useDirectDownload)
		{
			string path = string.Format(DD_ASSET_PATH + ".wav", Glb.mobileStreamingAssetsUrl, clipKey);

			RoutineRunner.instance.StartCoroutine(loadAudioFromURL(path));

			return clip != null;
		}

		// If the clip isn't immediately available, punt for now.  Mostly doesn't make sense to queue up a bunch of
		// sounds to play after downloading a big file.
		if (AssetBundleManager.isAvailable(clipResourcePath, ".wav"))
		{
			AssetBundleManager.load(this, 
				clipResourcePath,
				(string asset, Object obj, Dict data) =>
					{
						clip = obj as AudioClip;
						if (clip == null)
						{
							isMissingData = true;
							Debug.LogWarning("Unable to retrieve data for audio: " + asset);
							if (Data.debugMode && DevGUIMenuAudio.trackAudio)
							{
								DevGUIMenuAudio.logAudioError(this, "Prepare Clip : Load Fail " + clipResourcePath);
							}							
						}
					},
				(string asset, Dict data) =>
					{
						isMissingData = true;
						Debug.LogWarning("Unable to retrieve data for audio: " + asset);
						if (Data.debugMode && DevGUIMenuAudio.trackAudio)
						{
							DevGUIMenuAudio.logAudioError(this, "Prepare Clip : Load Fail " + clipResourcePath);
						}								
					},
					fileExtension:".wav"
				);
			return clip != null;
		}
#if UNITY_WEBGL 
		else 
		{
			// WebGL: if we don't have an IndexedDB a file will never 'be available' via caching (HIR-64982),
			// so just try to load the resource/bundle normally so it'll be available for later calls
			if (AssetBundleManager.useAssetBundles)
			{
				AssetBundleManager.load(this, clipResourcePath, null, null, fileExtension:".wav");
			}
		}
#endif

		if (Data.debugMode && DevGUIMenuAudio.trackAudio)
		{
			DevGUIMenuAudio.logAudioError(this, "Prepare Clip : AssetBundleManager.isAvailable false " + clipResourcePath);
		}		
		return false;
	}

	public IEnumerator loadAudioFromURL(string path)
	{
		isLoading = true;
		var www = new WWW(path);

		yield return www;

		if (string.IsNullOrEmpty(www.error))
		{
			clip = www.GetAudioClip();
		}

		isLoading = false;
	}
	
	// Does this audio exist on the given channel?
	public bool hasChannelTag(string channelKey)
	{
		return hasChannelTag(AudioChannel.findOrCreateChannel(channelKey));
	}
	
	// Does this audio exist on the given channel?
	public bool hasChannelTag(AudioChannel channel)
	{
		if (channelTags != null)
		{
			foreach (AudioChannel checkChannel in channelTags)
			{
				if (channel == checkChannel)
				{
					return true;
				}
			}
		}
		
		return false;
	}
	
	// Mark this audio as played for purposes of no-replay0window rules
	public void markPlaying()
	{
		lastPlayedTime = Time.realtimeSinceStartup;
	}
	
	// Find a given AudioInfo using its key
	// Convert the key to uppercase to find the AudioInfo in the dictionary.
	public static AudioInfo find(string key, bool useDirectDownload = false)
	{
		if (key != null)
		{
			string keyUpper = key.ToUpper(); // uppercase the key since sound clip names won't care about casing.
			AudioInfo info;
			if (all.TryGetValue(keyUpper, out info))
			{
				info.useDirectDownload = useDirectDownload;
				return info;
			}
			else if (all.TryGetValue(keyUpper + CLONED_CLIP_POSTFIX.ToUpper(), out info))
			{
				// the audio for gen39 was cloned and has all the same mappings + _clone so we'll check for that as well
				// as a fallback if the initial mapping doesn't work
				info.useDirectDownload = useDirectDownload;
				return info;
			}
			else if (all.TryGetValue(keyUpper + CLONED_CLIP_POSTFIX, out info))
			{
				// Handle the case where the clone postfix may not be uppercase, though it should be.
				info.useDirectDownload = useDirectDownload;
				return info;
			}
		}
		return null;
	}
	
	// Implements IResetGame
	public static void resetStaticClassData()
	{
		all = new Dictionary<string, AudioInfo>();
		Audio.switchMusicKey("");
	}
}

// A simply holder class for sound-specific channel ducking info
public class AudioDuckInfo
{
	public AudioChannel channel;
	public float volume;
	public float startDuration;
	public float endDuration;
	public float endOffset;
	
	public AudioDuckInfo(
		AudioChannel channel,
		float volume,
		float startDuration,
		float endDuration,
		float endOffset)
	{
		this.channel = channel;
		this.volume = volume;
		this.startDuration = startDuration;
		this.endDuration = endDuration;
		this.endOffset = endOffset;
	}
}

// A simply holder class for sound-specific channel aborting info
public class AudioAbortInfo
{
	public AudioChannel channel;
	public float fadeDuration;
	
	public AudioAbortInfo(AudioChannel channel, float fadeDuration)
	{
		this.channel = channel;
		this.fadeDuration = fadeDuration;
	}
}

// A simply holder class for audio event definitions
public class AudioEvent
{
	public string message { get; private set; }
	public float time { get; private set; }
	
	public AudioEvent(string message, float time)
	{
		this.message = message;
		this.time = time;
	}
}

