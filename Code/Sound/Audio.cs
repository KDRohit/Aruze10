using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Core.Util;

/**
This controls the playing of all sounds in the game.
Sounds are bundled in the editor and configured in SCAT.

To add new sounds:
1. Put them in their respective folder under one of:
		Assets/Data/Games/<group>/Resources/Sounds/<group>/<pack>/
		Assets/Data/HIR/Resources/Sounds/<pack>/
2. Run the Zynga -> Build Sounds menu option
3. Commit and build bundles
4. Go to the audio tab in SCAT and attach the generated sound to some "Audio Clip" reference on that tab.
5. If a playlist/soundbank is desired, you must set up all the sounds as "Audio Clip" references, and
   then go to the Playlists tab and setup a "Playlist" entry that references the clips
   
The sound system abstracts the concept of "audio keys" to reference either a playlist or a single audio clip.
When playing an audio item referenced by key, the system first checks for a matching playlist, and if that
fails, then searches for a matching single audio item.

Playlists are used as collections of sounds that can be used in a variety of ways.  When referenced, the
next audio item in the list is played and the playlist is advanced (only one audio item is played).
Specifically, playlists encapsulate the idea of both playlists and soundbanks.
   
To play a sound outside of 3d space:
	Audio.play("some_audio_key");

To play a sound inside of 3d space:
	Audio.play("some_audio_key", position);

Some entities have audio playing wrappers around them, specifically for playing sounds certain ways.
For example, MunchkinAppearance.playAudio() plays an audio clip in the voice of the munchkin and
attaches the audio source to the munchkin so that it moves with him/her.
Also, CharacterAppeance.playAudio() plays an audio clip on the character, attaches the source so that
it moves with the characters, and manages a boolean logic flag (CharacterAppearance.waitingForAudio)
which tells you if the most recently played audio is still playing or not.

To switch the default music playlist (music that always plays when it can):
	Audio.switchMusicKey("some_audio_key");

To play a specific music item once and then resume the default music playlist:
	Audio.playMusic("some_audio_key");
	
To disable music:
	Audio.switchMusicKey("");

This script is to be attached the root "Audio" object in any scene that uses audio.
*/
public class Audio : TICoroutineMonoBehaviour, IResetGame
{
	private const int SOURCE_POOL_SIZE = 42;			// The maximum number of sounds that can play at the same time.
	private const float MUTE_SPEED = 10f;				// The speed at which to mute/unmute audio
	private const int NESTED_COLLECTION_MAX_DEPTH = 3;	// The maximum number of nested collections allowed, this will also prevent someone creating a infinite recursive loop of collections
		
	public const string SOUND_CHANNEL_KEY = "type_sound";	// The key of the channel tag used for all sound effects, for muting purposes
	public const string MUSIC_CHANNEL_KEY = "type_music";	// The key of the channel tag used for all music, for muting purposes
	public const string VO_CHANNEL_KEY = "type_vo";			// The key of the channel tag used for voice overs

	public const string SKIP_IF_VO_TAG = "skip_if_vo";		// A tag that if attached to a sound clip means that sound will not play if something using "type_vo" is already playing
	public const string WAIT_FOR_VO_TAG = "wait_for_vo";	// A tage that causes type_vo audio to queue up if something is already playing using type_vo

	public GameObject audioSourceTemplate;	// Prefab that is used to play sounds
	public bool firstSpin = true;

	private bool optionalLogs = false;

	// frequently used channels for quick reference
	public static AudioChannel soundChannel;
	public static AudioChannel musicChannel;

	public static float maxGlobalVolume = 1.0f; // Value from 0 - 1.

	private static float musicSwitchFadeoutOverride = -1.0f; // this will be used to change the fadeout time of the current music track when looping into the next one, useful if you don't want a fade between tracks for instance
	// will reset to -1.0f and not doing anything after first switch

	private static bool isLoopedMusicACollection = false; // Collections can't be looped normally, they still must restart when the current track ends so the collection can grab a new track

	// scripting properties
	public int globalTimerID { get; private set; } // Used for invalidating pending delayed calls.

	// list used for VO queueing functionality (will store appropriately tagged VO keys - channel tag is "wait_for_vo")
	private static List<string> queuedVOKeys = new List<string>();

	// See HIR-17162
	private static List<string> relativeDelayVOKeys = new List<string>();

	private static PreferencesBase unityPrefs = null;

	private static bool poolIsIntialized = false;

	// enum used for queueing logic
	private enum PlaySoundStatus {SHOULD_PLAY, SHOULD_QUEUE, SHOULD_SKIP};
	
	// The scene's AudioListener
	public static Vector3 listenerPosition
	{
		get
		{
			if (listenerInstance == null)
			{
				listenerInstance = FindObjectOfType(typeof(AudioListener)) as AudioListener;
			}
			if (listenerInstance != null)
			{
				return listenerInstance.transform.position;
			}
			Debug.LogWarning("No active AudioListener exists, at a time when one is needed!");
			return Vector3.zero;
		}
	}
	private static AudioListener listenerInstance = null;
	
	// Use this to make sure that value volume setting is used (important for WebGL where this isn't sanity checked by the engine).
	public static float listenerVolume
	{
		get
		{
			return AudioListener.volume;
		}
		set
		{
			if (value < 0f || float.IsNegativeInfinity(value) || float.IsNaN(value))
			{
				AudioListener.volume = 0f;
			}
			else if (value > 1f || float.IsPositiveInfinity(value))
			{
				AudioListener.volume = 1f;
			}
			else
			{
				AudioListener.volume = value;
			}
		}
	}
	
	// Is music muted?
	public static bool muteMusic
	{
		get
		{
			if (unityPrefs == null)
			{
				//set default audio settings
				setDefaultAudioSettings();
			}

			return _muteMusic;
		}
		set
		{
			CustomPlayerData.setValue(CustomPlayerData.MUTE_MUSIC, value);
			_muteMusic = value;
			DevGUIMenuAudio.logAudioMute(value);

			if (unityPrefs != null)
			{
				// write inthe playerpref cache so it's good for next time
				unityPrefs.SetInt(Prefs.MUTE_MUSIC_PREF, value ? 1 : 0);

				// we must also write here because CustomPlayerData.getBool will default to this key in the cache when it calls CustomPlayerData.getData
				// and CustomPlayerData is not loaded yet, which is the case when setDefaultAudioSettings gets called from Awake			
				unityPrefs.SetString(CustomPlayerData.MUTE_MUSIC, value ? "true" : "false");
				unityPrefs.Save();
			}
			else
			{
				Debug.LogWarning("Trying to set sound preferences before preferences initialized");
			}

			if (value)
			{
				stopMusic(0.5f);
			}
			else
			{
				switchMusicKeyImmediate(defaultMusicKey);
			}
		}
	}
	private static bool _muteMusic = false;
	
	// Are sound effect muted?
	public static bool muteSound
	{
		get
		{
			if (unityPrefs == null)
			{
				//set default audio settings
				setDefaultAudioSettings();
			}
			return _muteSound;
		}
		set
		{
			CustomPlayerData.setValue(CustomPlayerData.MUTE_FX, value);
			_muteSound = value;
			
			if (unityPrefs != null)
			{
				// write inthe playerpref cache so it's good for next time
				unityPrefs.SetInt(Prefs.MUTE_SOUND_PREF, value ? 1 : 0);

				// we must also write here because CustomPlayerData.getBool will default to this key in the cache when it calls CustomPlayerData.getData
				// and CustomPlayerData is not loaded yet, which is the case when setDefaultAudioSettings gets called from Awake
				unityPrefs.SetString(CustomPlayerData.MUTE_FX, value ? "true" : "false");
				unityPrefs.Save();
			}
			else
			{
				Debug.LogWarning("Trying to set sound preferences before preferences initialized");
			}
		}
	}
	private static bool _muteSound = true;
	
	public static Audio instance { get; private set; }			// Most recent instance of an Audio object
	public static Transform sourceRoot { get; private set; }	// Transform root for PlayingAudio objects
	
	public static PlayingAudio currentMusicPlayer { get; private set; }	// The current PlayingAudio instance that is playing music
	public static string defaultMusicKey { get; private set; }			// The audio key to use when resuming/continuing background music
	
	public static PlayingAudio[] poolPlaying = new PlayingAudio[SOURCE_POOL_SIZE];	// A pool of valid PlayingAudio instances for use
	private static int playingAudioIndex = 0;
	
	// Initializes the audio system
	public void Awake()
	{
		instance = this;
		sourceRoot = transform;
		globalTimerID = 0;	
	}

	// Setup should be called on startup to reset audio channels and setup default audio data
	public static void setup()
	{
		setDefaultAudioSettings();
		maxGlobalVolume = unityPrefs.GetFloat(Prefs.MAX_SOUND_VOLUME, 1);
		AudioChannel.resetAllChannels();
	}
	
	// Called by setup, and also in case our unityPrefs reference goes null for whatever reason 
	// in muteMusic and muteSound (probably to recover the preference reference below)
	public static void setDefaultAudioSettings()
	{
		if(unityPrefs == null)
		{
			unityPrefs = SlotsPlayer.getPreferences();
		}

		bool defaultMuteMusic = unityPrefs.GetInt(Prefs.MUTE_MUSIC_PREF, 0) == 1;
		bool defaultMuteSound = unityPrefs.GetInt(Prefs.MUTE_SOUND_PREF, 0) == 1;

		muteMusic = CustomPlayerData.getBool(CustomPlayerData.MUTE_MUSIC, defaultMuteMusic);
		muteSound = CustomPlayerData.getBool(CustomPlayerData.MUTE_FX, defaultMuteSound);
	}

	// Mute music and sound temporarily without writing to preferences or CustomPlayerData.  Prefer this when muting as
	// a temporary override of user preferences, so that if the app is killed during the temp mute, it doesn't result in
	// losing the user's sound preferences.
	public static bool tempMuted
	{
		get
		{
			return _tempMuted;
		}
		set
		{
			if (_tempMuted != value)
			{
				if (value)
				{
					DevGUIMenuAudio.logAudioMute(true);
					_muteMusic = true;
					_muteSound = true;
					stopMusic(0.5f);
					stopAll();
				}
				else
				{
					DevGUIMenuAudio.logAudioMute(false);
					setDefaultAudioSettings();
				}
			}
			_tempMuted = value;
		}
	}
	private static bool _tempMuted = false;

	// Updates all the audio that is actively playing, firing events and adjusting fades.
	protected override void LateUpdate()
	{
		base.LateUpdate();

		if (!Data.isGlobalDataSet)
		{
			return;
		}
		
		// Smooth adjust to mute/unmute everything on the sound channel
		if (soundChannel != null)
		{
			if (muteSound)
			{
				if (soundChannel.currentVolume != 0)
				{
					soundChannel.currentVolume = Mathf.Lerp(soundChannel.currentVolume, 0f, Time.deltaTime * MUTE_SPEED);
				}
			}
			else
			{
				if (soundChannel.currentVolume != soundChannel.originalVolume)
				{
					soundChannel.currentVolume = Mathf.Lerp(soundChannel.currentVolume, soundChannel.originalVolume, Time.deltaTime * MUTE_SPEED);
				}
			}	
		}
		else
		{
			Debug.LogError("No sound channel");
		}


		if (musicChannel != null)
		{
			if (muteMusic)
			{
				if (musicChannel.currentVolume != 0)
				{
					musicChannel.currentVolume = Mathf.Lerp(musicChannel.currentVolume, 0f, Time.deltaTime * MUTE_SPEED);
				}
			}
			else
			{
				if (musicChannel.currentVolume != musicChannel.originalVolume)
				{
					musicChannel.currentVolume = Mathf.Lerp(musicChannel.currentVolume, musicChannel.originalVolume, Time.deltaTime * MUTE_SPEED);
				}
			}	
		}
		else
		{
			Debug.LogError("No music channel");
		}
		
		
		if (!poolIsIntialized)
		{
			initPool();
		}
				
		// Update channel ducking levels and abort block flags
		AudioChannel.updateAudioChannels();
		
		// Maintain all of the PlayingAudio objects
		for (int i = 0; i < SOURCE_POOL_SIZE; i++)
		{
			if (poolPlaying[i] == null)
			{
				continue;
			}
			else if (poolPlaying[i].isPlaying)
			{
				// Update the playing audio data
				poolPlaying[i].updatePlayingAudio();
			}
		}
	}

	private void initPool()
	{
		for (int i = 0; i < SOURCE_POOL_SIZE; i++)
		{
			// Generate a pool member for this null slot
			GameObject sourceObject = CommonGameObject.instantiate(audioSourceTemplate) as GameObject;
			PlayingAudio source = sourceObject.GetComponent<PlayingAudio>();
			source.initForPool(i);
			poolPlaying[i] = source;
		}
		poolIsIntialized = true;
	}
	
	// This will abort all currently playing sounds.
	public static void stopAll(float fadeout = 0.3f)
	{
		for (int i = 0; i < SOURCE_POOL_SIZE; i++)
		{
			if (poolPlaying[i] != null && poolPlaying[i].isPlaying)
			{
				poolPlaying[i].stop(fadeout);
			}
		}
	}

	// Checks to see if a sound key would be muted based on the type and what the mute settings are.
	public static bool wouldSoundKeyBeMuted(string soundKey)
	{
		if (doesAudioClipHaveChannelTag(soundKey, Audio.MUSIC_CHANNEL_KEY) && muteMusic)
		{
			return true;
		}
		else
		{
			return muteSound;
		}
	}

	// Reset a collection so it starts over from the first track.
	public static void resetCollectionBySoundMapOrSoundKey(string soundKey)
	{
		string soundName = tryConvertSoundKeyToMappedValue(soundKey);
		resetCollection(soundName);
	}

	// Reset a collection so it starts over from the first track.
	private static void resetCollection(string key)
	{
		PlaylistInfo playlistInfo = PlaylistInfo.find(key);
		
		if (playlistInfo != null)
		{
			playlistInfo.reset();
		}
	}

	// Should a collection cycle through its tracks over and over again, or should they play each track once then stop?
	// Hopefully we'll get this from SCAT eventually.
	public static void setCollectionCycling(string key, bool shouldCycle)
	{
		PlaylistInfo playlistInfo = PlaylistInfo.find(key);
		
		if (playlistInfo != null)
		{
			playlistInfo.shouldCycleTracks = shouldCycle;
		}		
	}
	
	// Play some audio without a 3d position via an audio or playlist key
	// THIS WILL RETURN NULL IF THE SOUND CANNOT BE PLAYED!
	public static PlayingAudio play(
		string key,
		float relativeVolume = 1f,
		float relativePitch = 0f,
		float relativeDelay = 0f,
		float loops = 0
		)
	{
		string originalKey = key;

		if (string.IsNullOrEmpty(key))
		{
			return null;
		}

		int depth = 0;
		PlaylistInfo playlistInfo = PlaylistInfo.find(key);
		while (playlistInfo != null && depth < NESTED_COLLECTION_MAX_DEPTH)
		{
			key = playlistInfo.getNextTrack();
			playlistInfo = PlaylistInfo.find(key);
			depth++;
		}

		if ( depth >= NESTED_COLLECTION_MAX_DEPTH)
		{
#if UNITY_EDITOR
			Debug.LogWarning("Audio collection nesting was too deep so giving up on finding an AudioInfoKey, returning null for key: " + originalKey);
#endif

			return null;
		}

		AudioInfo audioInfo = AudioInfo.find(key);
		if (audioInfo != null)
		{
			return play(audioInfo, relativeVolume, relativePitch, relativeDelay, loops);
		}

		if (Data.debugMode && DevGUIMenuAudio.trackAudio)
		{
			DevGUIMenuAudio.logAudioNotFound(key);
		}

#if UNITY_EDITOR
		Debug.LogWarning("Unable to find any matching sound or playlist for key: " + key);
#endif

		return null;
	}
	
	// Play some audio with a 3d position via an audio or playlist key
	// THIS WILL RETURN NULL IF THE SOUND CANNOT BE PLAYED!
	public static PlayingAudio play(
		string key,
		Vector3 position,
		float relativeVolume = 1f,
		float relativePitch = 0f,
		float relativeDelay = 0f,
		float loops = 0)
	{
		string originalKey = key;

		if (string.IsNullOrEmpty(key))
		{
			return null;
		}

		int depth = 0;
		PlaylistInfo playlistInfo = PlaylistInfo.find(key);
		while (playlistInfo != null && depth < NESTED_COLLECTION_MAX_DEPTH)
		{
			key = playlistInfo.getNextTrack();
			playlistInfo = PlaylistInfo.find(key);
			depth++;
		}

		if ( depth >= NESTED_COLLECTION_MAX_DEPTH)
		{
#if UNITY_EDITOR
			Debug.LogWarning("Audio collection nesting was too deep so giving up on finding an AudioInfoKey, returning null for key: " + originalKey);
#endif

			return null;
		}

		AudioInfo audioInfo = AudioInfo.find(key);
		if (audioInfo != null)
		{
			return play(audioInfo, position, relativeVolume, relativePitch, relativeDelay, loops);
		}

		if (Data.debugMode && DevGUIMenuAudio.trackAudio)
		{
			DevGUIMenuAudio.logAudioNotFound(key);
		}
#if UNITY_EDITOR
		Debug.LogWarning("Unable to find any matching sound or playlist for key: " + key);
#endif

		return null;
	}
	
	// Play some audio without a 3d position via a specific AudioInfo reference
	// THIS WILL RETURN NULL IF THE SOUND CANNOT BE PLAYED!
	public static PlayingAudio play(
		AudioInfo audioInfo,
		float relativeVolume = 1f,
		float relativePitch = 0f,
		float relativeDelay = 0f,
		float loops = 0)
	{
		PlayingAudio playing = play(audioInfo, listenerPosition, relativeVolume, relativePitch, relativeDelay, loops);

		if (audioInfo != null && audioInfo.channelTags != null && playing != null)
		{
			foreach (AudioChannel channel in audioInfo.channelTags)
			{
				if (channel.keyName == VO_CHANNEL_KEY)
				{
					// whenever we play a VO add a listener to it that will check the queuedVOKeys list
					playing.addListeners(new AudioEventListener("end", checkVOQueue));
				}
			}

			audioInfo.playedCount++;
		}

#if !UNITY_WEBGL
		if (playing != null)
		{
			// Set non-3d sound parameter
			playing.audioSource.spatialBlend = 0f;
		}
#endif
		return playing;
	}

	// Play an audio asset with delay, basically a simplified call to Audio.play() where you only care about the key and the delay
	public static PlayingAudio playWithDelay(string key, float delay)
	{
		return play(key, 1.0f, 0.0f, delay, 0.0f);	
	}

	public static PlayingAudio tryToPlaySoundMap(string soundKey, float relativeVolume = 1.0f)
	{
		if (canSoundBeMapped(soundKey))
		{
			string key = soundMap(soundKey);
			return play(key, relativeVolume);
		}
		
		return null;
	}
	
	public static PlayingAudio tryToPlaySoundMapWithDelay(string soundKey, float delay)
	{
		if (canSoundBeMapped(soundKey))
		{
			string key = soundMap(soundKey);
			return playWithDelay(key, delay);
		}
		
		return null;
	}

	// Check if what is passed can be SoundMapped, if it can't it will return the soundKey it was passed
	public static string tryConvertSoundKeyToMappedValue(string soundKey)
	{
		string soundName = soundKey;

		// We want to play a sound.
		if (Audio.isKeyInSoundMap(soundKey))
		{
			// If it's the name of a mapped sound key lets play that.
			if (Audio.canSoundBeMapped(soundKey))
			{
				// If check to avoid sending play("") messages to Audio.
				soundName = Audio.soundMap(soundKey);
			}
		}

		return soundName;
	}

	// Attempt to play a SoundMap value first, if that fails try playing a sound key and see if that works
	public static PlayingAudio playSoundMapOrSoundKey(string soundKey)
	{
		string soundName = tryConvertSoundKeyToMappedValue(soundKey);
		return play(soundName);
	}

	// Attempt to play a SoundMap value first, if that fails try playing a sound key and see if that works
	public static PlayingAudio playSoundMapOrSoundKeyWithDelay(string soundKey, float delay)
	{
		string soundName = tryConvertSoundKeyToMappedValue(soundKey);
		return playWithDelay(soundName, delay);
	}

	// Get the length of an audio asset
	public static float getAudioClipLength(string key)
	{
		string originalKey = key;

		if (string.IsNullOrEmpty(key))
		{
			Debug.LogWarning("Audio.getAudioClipLength() - Function was passed a null or empty string!  Return 0 length.");
			return 0.0f;
		}

		int depth = 0;
		PlaylistInfo playlistInfo = PlaylistInfo.find(key);
		while (playlistInfo != null && depth < NESTED_COLLECTION_MAX_DEPTH)
		{
			key = playlistInfo.peekNextTrack();
			playlistInfo = PlaylistInfo.find(key);
			depth++;

			if ( depth >= NESTED_COLLECTION_MAX_DEPTH)
			{
#if UNITY_EDITOR
				Debug.LogWarning("Audio.getAudioClipLength() - Audio collection nesting was too deep so giving up on finding an AudioInfoKey, returning null for key: " + originalKey);
#endif

				return 0.0f;
			}
		}

		AudioInfo audioInfo = AudioInfo.find(key);
		if (audioInfo != null)
		{
			return audioInfo.getClipLength();
		}
		else
		{
#if UNITY_EDITOR
			Debug.LogWarning("Audio.getAudioClipLength() - Unable to find any matching sound or playlist for key: " + key);
#endif

			return 0.0f;
		}
	}

	// Check if an audio asset is tagged with a channel, i.e. if it is music
	public static bool doesAudioClipHaveChannelTag(string key, string channelTag)
	{
		string originalKey = key;

		if (string.IsNullOrEmpty(key))
		{
			Debug.LogWarning("Audio.doesAudioClipHaveChannelTag() - Function was passed a null or empty string!");
			return false;
		}

		int depth = 0;
		PlaylistInfo playlistInfo = PlaylistInfo.find(key);
		while (playlistInfo != null && depth < NESTED_COLLECTION_MAX_DEPTH)
		{
			key = playlistInfo.peekNextTrack();
			playlistInfo = PlaylistInfo.find(key);
			depth++;

			if (depth >= NESTED_COLLECTION_MAX_DEPTH)
			{
#if UNITY_EDITOR
				Debug.LogWarning("Audio.doesAudioClipHaveChannelTag() - Audio collection nesting was too deep so giving up on finding an AudioInfoKey, returning false for key: " + originalKey);
#endif

				return false;
			}
		}

		AudioInfo audioInfo = AudioInfo.find(key);
		if (audioInfo != null)
		{
			return audioInfo.hasChannelTag(channelTag);
		}
		else
		{
#if UNITY_EDITOR
			Debug.LogWarning("Audio.doesAudioClipHaveChannelTag() - Unable to find any matching sound or playlist for key: " + key);
#endif

			return false;
		}
	} 

	// Checks if we should play this sound immediately (return true if so)
	// Otherwise we may skip it (return false) or queue it (return false after adding to queue
	private static PlaySoundStatus checkIfShouldPlayOrSkipOrQueue(AudioInfo audioInfo)
	{
		// check if we might need to skip this audio play call if something on type_vo is already playing
		if (audioInfo.hasChannelTag(SKIP_IF_VO_TAG))
		{
			if (Audio.isAudioPlayingByChannel(VO_CHANNEL_KEY))
			{
				return PlaySoundStatus.SHOULD_SKIP;
			}
		}

		// next check if this audio play call should be queued because it uses the wait_for_vo tag 
		if (audioInfo.hasChannelTag(WAIT_FOR_VO_TAG))
		{
			if (Audio.isAudioPlayingByChannel(VO_CHANNEL_KEY))
			{
				return PlaySoundStatus.SHOULD_QUEUE;
			}
		}

		return PlaySoundStatus.SHOULD_PLAY;
	}

	// We may have VOs queued up that we need to play, now that a different VO has finished
	private static void checkVOQueue(AudioEvent audioEvent, PlayingAudio audioInfo)
	{
		if (queuedVOKeys.Count > 0)
		{
			string soundKey = queuedVOKeys[0];
			RoutineRunner.instance.StartCoroutine(waitTwoFramesThenPlaySound(soundKey));
			queuedVOKeys.RemoveAt(0);
		}
	}

	// Waits 2 frames then class the specied audio clip (which should be a VO, unless we add functionality to queueing system)
	private static IEnumerator waitTwoFramesThenPlaySound(string soundKey)
	{
		yield return null;
		yield return null;
		Audio.play(soundKey);
	}

	public static void playAudioFromURL(string soundKey)
	{
		AudioInfo audioInfo = AudioInfo.find(soundKey, true);

		if (audioInfo != null)
		{
			audioInfo.prepareClip();
			RoutineRunner.instance.StartCoroutine(waitForDynamicAudio(audioInfo));
		}
	}

	private static IEnumerator waitForDynamicAudio(AudioInfo info)
	{
		while (info.isLoading)
		{
			yield return null;
		}

		play(info);
	}
	
	// Play some audio with a 3d position via a specific AudioInfo reference
	// THIS WILL RETURN NULL IF THE SOUND CANNOT BE PLAYED!
	// This is the ultimate end-level version of play() that always gets called.
	public static PlayingAudio play(
		AudioInfo audioInfo,
		Vector3 position,
		float relativeVolume = 1f,
		float relativePitch = 0f,
		float relativeDelay = 0f,
		float loops = 0)
	{
		if (Data.debugMode && DevGUIMenuAudio.trackAudio)
		{
			DevGUIMenuAudio.logAudioPlay(audioInfo, position, relativeVolume, relativePitch, relativeDelay, loops);
		}

		if (relativeDelay > 0)
		{
			// Delay the play for later. Return null since we're not doing anything yet.
			RoutineRunner.instance.StartCoroutine(handleDelayedPlay(audioInfo, position, relativeVolume, relativePitch, relativeDelay, loops));
			return null;
		}
		
		if (instance != null && audioInfo != null)
		{
			// Prepare the clip, and if that fails then abort
			if (!audioInfo.prepareClip())
			{
				if (Data.debugMode && DevGUIMenuAudio.trackAudio)
				{
					DevGUIMenuAudio.logAudioError(audioInfo, "Prepare clip failed");
				}				
				return null;
			}
			
			if (!audioInfo.canPlay)
			{
				if (Data.debugMode && DevGUIMenuAudio.trackAudio)
				{
					DevGUIMenuAudio.logAudioError(audioInfo, "audioInfo.canPlay is false");
				}						
				return null;
			}
			
			// Get a PlayingAudio reference to play the audio on
			PlayingAudio playing = findAvailablePlayingAudio();
			if (playing == null)
			{
				if (Data.debugMode && DevGUIMenuAudio.trackAudio)
				{
					DevGUIMenuAudio.logAudioError(audioInfo, "findAvailablePlayingAudio returned null");
				}
				return null;
			}

			// ensure we should actually play this clip now, or if it should be skipped, or should be queued
			PlaySoundStatus shouldPlayStatus = checkIfShouldPlayOrSkipOrQueue(audioInfo);

			if (shouldPlayStatus == PlaySoundStatus.SHOULD_PLAY)
			{
				// Start up the audio
				playing.transform.position = position;
				playing.setupPlay(audioInfo, relativeVolume, relativePitch, loops);

				if (Data.debugMode && DevGUIMenuAudio.trackAudio)
				{
					DevGUIMenuAudio.trackAudioCheck(playing);
				}
				
				// Abort any audio on listed abort channels (does not abort this audio instance)
				foreach (AudioAbortInfo abortInfo in audioInfo.abortChannels)
				{
					foreach (PlayingAudio otherPlaying in poolPlaying)
					{
						if (otherPlaying != null &&
							otherPlaying != playing &&
							otherPlaying.isPlaying &&
							otherPlaying.audioInfo.hasChannelTag(abortInfo.channel))
						{
							// call this before the aborted sound stops so that trackedAudioInfo isn't cleared
							if (Data.debugMode && DevGUIMenuAudio.trackAudio)
							{
								DevGUIMenuAudio.trackAbort(playing, otherPlaying);
							}
							
							// Special case for stopping music correctly.
							if (otherPlaying == currentMusicPlayer)
							{ 
								switchMusicKey("");					
								stopMusic(abortInfo.fadeDuration);
							}
							else
							{
								otherPlaying.stop(abortInfo.fadeDuration);
							}
						}
					}
				}
				
				soundLog("Playing Sound: " + audioInfo.keyName);
				return playing;
			}
			else if (shouldPlayStatus == PlaySoundStatus.SHOULD_QUEUE)
			{
				if (Data.debugMode && DevGUIMenuAudio.trackAudio)
				{
					DevGUIMenuAudio.logAudioError(audioInfo, "PlaySoundStatus.SHOULD_QUEUE");
				}								
#if UNITY_EDITOR
				Debug.Log("There is already VO playing. Queueing VO: " + audioInfo.keyName);
#endif

				queuedVOKeys.Add(audioInfo.keyName);
			}
			else if (shouldPlayStatus == PlaySoundStatus.SHOULD_SKIP)
			{
				if (Data.debugMode && DevGUIMenuAudio.trackAudio)
				{
					DevGUIMenuAudio.logAudioError(audioInfo, "PlaySoundStatus.SHOULD_SKIP");
				}				
#if UNITY_EDITOR
				soundLog("SKIPPED Sound: " + audioInfo.keyName);
#endif
			}
		}
		return null;
	}
	
	private static IEnumerator handleDelayedPlay(
		AudioInfo audioInfo,
		Vector3 position,
		float relativeVolume = 1f,
		float relativePitch = 0f,
		float relativeDelay = 0f,
		float loops = 0)
	{
		int id = instance.globalTimerID;

		relativeDelayVOKeys.Add(audioInfo.keyName);

		if (relativeDelay > 0)
		{
			yield return new WaitForSeconds(relativeDelay);
		}

		relativeDelayVOKeys.Remove(audioInfo.keyName);

		// Check if our command has been invalidated.
		if (id == instance.globalTimerID)
		{
			// We've waited, now play the sound with a delay of 0.
			play(audioInfo, position, relativeVolume, relativePitch, 0, loops);
		}
	}

	// Plays some music, interrupting the background playlist as necessary
	public static void playMusic(string audioKey, float previousFadeout = 2.0f, float nextDelay = 0.0f, bool shouldLoop = false)
	{
		if (muteMusic)
		{
			return;
		}
		
		// Don't kill matching tunes. This lets us call music plays on triggers without ruining anything.
		if (currentMusicPlayer != null && currentMusicPlayer.audioInfo != null && currentMusicPlayer.audioInfo.keyName == audioKey)
		{
			currentMusicPlayer.relativeVolume = 1.0f;
			return;
		}
		
		stopMusic(previousFadeout);
		
		// Always loop our bg music played from a key. Never loop anything else.
		currentMusicPlayer = play(audioKey, 1.0f, 0.0f, nextDelay, (shouldLoop ? float.PositiveInfinity : -1f));
		
		// Listen for the end of the music so that we can nullify currentMusicPlayer
		if (currentMusicPlayer != null)
		{
			currentMusicPlayer.addListeners(new AudioEventListener("end", musicEndCallback));
		}
	}
	
	// Stops the currently playing music, if any
	public static void stopMusic(float fadeout = 2.0f)
	{
		if (currentMusicPlayer != null)
		{
			currentMusicPlayer.stop(fadeout);
			currentMusicPlayer = null;
		}
	}

	// Causes an instant swap of the current music key
	// Prefer this over calling playMusic() and then calling switchMusicKey() on the same key
	// Calling switchMusicKeyImmediate(""); will cancel all music
	// fadeout controls how quickly the currently playing music fades away
	public static void switchMusicKeyImmediate(string audioKey, float fadeout = 2.0f)
	{
		switchMusicKey(audioKey);
		
		if (currentMusicPlayer == null 
			|| currentMusicPlayer.audioInfo == null 
			|| currentMusicPlayer.audioInfo.keyName != audioKey)
		{
			// Do not do the stopMusic call if the desired key is already playing.
			// force the music to stop and refresh with the new key
			stopMusic(fadeout);
		}

		// call the new music call with looping true, this replaces how we used to loop the music via LateUpdate
		if (!muteMusic && currentMusicPlayer == null && !string.IsNullOrEmpty(defaultMusicKey))
		{
			// if the looped music is a collection then we don't want to loop the current track, 
			// since we will call switchMusicKeyImmediate() again when the current music track 
			//from the collection ends so we can loop into the next one. (SOM01 is an example of a game with a looped music collection).
			bool isLoopingMusic = (isLoopedMusicACollection) ? false : true;

			if (musicSwitchFadeoutOverride != -1.0f)
			{
				playMusic(defaultMusicKey, previousFadeout: musicSwitchFadeoutOverride, nextDelay: 0.0f, shouldLoop: isLoopingMusic);
				musicSwitchFadeoutOverride = -1.0f;
			}
			else
			{
				playMusic(defaultMusicKey, previousFadeout: 2.0f, nextDelay: 0.0f, shouldLoop: isLoopingMusic);
			}
		}
	}
	
	// Sets the default music key, queuing the next clip to play once the previous one ends. 
	public static void switchMusicKey(string audioKey, float fadeout = -1.0f)
	{
		// Seems like some serialized fields are passing null in here,
		// we don't really want to ever change to null, so when that
		// happens we will just use the empty string
		if (audioKey == null)
		{
			audioKey = "";
		}

		string prevDefaultMusicKey = defaultMusicKey;

		if (audioKey != defaultMusicKey)
		{
			defaultMusicKey = audioKey;

			if (defaultMusicKey != "")
			{
				isLoopedMusicACollection = isSoundKeyACollection(defaultMusicKey);
			}
			else
			{
				isLoopedMusicACollection = false;
			}

			musicSwitchFadeoutOverride = fadeout;

			if (currentMusicPlayer != null && currentMusicPlayer.audioInfo != null)
			{
				if (currentMusicPlayer.audioInfo.keyName != audioKey)
				{
					currentMusicPlayer.isLooping = false;
				}
				else
				{
					currentMusicPlayer.isLooping = true;
					Debug.LogWarning("Audio.switchMusicKey() - Attempting to switch music key to the music key that was already playing!");
				}
			}
		}

		if (audioKey != "" && prevDefaultMusicKey == "")
		{
			// there wasn't a default music key, so we should just do a switchMusicKeyImmediate here
			switchMusicKeyImmediate(audioKey, fadeout);
		}
	}
	
	// Callback used when a music track ends, which starts up the next track if desired
	private static void musicEndCallback(AudioEvent audioEvent, PlayingAudio playingAudio)
	{
		if (playingAudio == currentMusicPlayer || currentMusicPlayer == null)
		{
			// the defaultMusicKey is different from this key (or a collection), so we need to swap it out now
			switchMusicKeyImmediate(defaultMusicKey);
		}
	}

	public static bool isPlaying(string soundKey)
	{

		// Check and see if the sound is in a collection
		List<string> possibleSounds = getAllSoundNamesInCollection(soundKey);
		if (possibleSounds == null)
		{
			possibleSounds = new List<string>();
			possibleSounds.Add(soundKey);
		}

		// Go through all of the playing audio.
		foreach (PlayingAudio playingAudio in poolPlaying)
		{
			if (playingAudio != null && playingAudio.isPlaying)
			{
				foreach (string soundName in possibleSounds)
				{
					if (playingAudio.audioInfo.keyName == soundName)
					{
						return true;
					}
				}
			}
		}
		// Check the music key as well.
		if (currentMusicPlayer != null && currentMusicPlayer.isPlaying)
		{
			foreach (string soundName in possibleSounds)
			{
				if (currentMusicPlayer.audioInfo.keyName == soundName)
				{
					// This was a LogError, this should NOT be a LogError.
					//Debug.Log("Found audio clip " + soundName + " playing.");
					return true;
				}
			}
		}
		
		return false;

	}
	
	// Finds the next available PlayingAudio object to play audio on
	private static PlayingAudio findAvailablePlayingAudio()
	{
		for (int j = 0; j < SOURCE_POOL_SIZE; j++)
		{
			int i = (j + playingAudioIndex) % SOURCE_POOL_SIZE;
			
			if (poolPlaying[i] == null)
			{
				// Generate a pool member for this null slot
				GameObject sourceObject = CommonGameObject.instantiate(instance.audioSourceTemplate) as GameObject;
				PlayingAudio source = sourceObject.GetComponent<PlayingAudio>();
				source.initForPool(i);
				poolPlaying[i] = source;
				playingAudioIndex = i + 1;
				return source;
			}
			else if (!poolPlaying[i].isPlaying)
			{
				playingAudioIndex = i + 1;
				return poolPlaying[i];
			}
		}
		return null;
	}
	
	
	public static void removeDelays()
	{
		// Iterate the global timer Id. This invalidates all previous delayed calls.
		instance.globalTimerID++;
	}

	// Returns a value in seconds equal to the time until the next "beat" after a given delay. 
	public static float getBeatDelay(AudioInfo audioInfo, float minDelayInSeconds)
	{
		if (audioInfo == null)
		{
			Debug.LogError("Trying to find beat delay for a null audioInfo");
			return 0;
		}

		float beatDurationInSeconds = audioInfo.beat;
		if (beatDurationInSeconds == 0.0f)
		{
			beatDurationInSeconds = audioInfo.clip.length;
		}

		if (minDelayInSeconds < audioInfo.pickup)
		{
			minDelayInSeconds = audioInfo.pickup;
		}
		else if (Mathf.Abs(minDelayInSeconds - (audioInfo.pickup + beatDurationInSeconds)) < 0.0001f)
		{
			// the minDelayInSeconds and time to reach the first beat are almost or exactly the same value
			// this means we should only wait a single beat, this was probably because this clip
			// doesn't have a beat defined and minDelayInSeconds was the clip's exact length
			return minDelayInSeconds;
		}
		else
		{
			float requiredWaitTime = audioInfo.pickup;
			
			// Determine how many beats we will need to reach the desired time
			// (and add extra time if we need it to finish off a beat).
			long beatTotal = (long)((minDelayInSeconds - requiredWaitTime) / beatDurationInSeconds);
			if ((minDelayInSeconds - requiredWaitTime) - (beatDurationInSeconds * beatTotal) >= 0.001f)
			{
				// We need one more beat to take care of the excess
				beatTotal += 1;
			}
			requiredWaitTime += beatTotal * beatDurationInSeconds;
			minDelayInSeconds = requiredWaitTime;
		}

		if (minDelayInSeconds > 0 && beatDurationInSeconds > 0)
		{
			float totalDelay = minDelayInSeconds;
			// Find the time of the next "beat" after the delay has passed.
			if (totalDelay % beatDurationInSeconds > 0.0001f) // if we're already almost exactly at a beat, don't add extra time, just use the value we already have, otherwise we would end up adding a full extra beat
			{
				totalDelay += beatDurationInSeconds - (totalDelay % (beatDurationInSeconds));
			}

			// Return the new time in seconds.
			return totalDelay;
		}
		return 0;
	}

	// Get the beat parameter for an audio clip
	public static float getBeat(string key)
	{
		AudioInfo audioInfo = AudioInfo.find(key);
		if (audioInfo == null)
		{
			// This probably shouldn't be expected.
			return 0;
		}
		else if (audioInfo.clip == null)
		{
			// The clip hasn't been prepared, so prepare it now, then bail if not possible.
			if (!audioInfo.prepareClip())
			{
				return 0;
			}
		}

		return audioInfo.beat;
	}

	// Returns a value in seconds equal to the time until the next "beat" after a given delay.
	public static float getBeatDelay(string key, float minDelayInSeconds)
	{	
		AudioInfo audioInfo = AudioInfo.find(key);
		if (audioInfo == null)
		{
			// This probably shouldn't be expected.
			return 0;
		}
		else if (audioInfo.clip == null)
		{
			// The clip hasn't been prepared, so prepare it now, then bail if not possible.
			if (!audioInfo.prepareClip())
			{
				return 0;
			}
		}
		
		if (audioInfo.clip != null)
		{
			// audioInfo.clip really shouldn't be null anymore if we got here after prepareClip(),
			// but double checking just in case.
			return getBeatDelay(audioInfo, minDelayInSeconds);
		}
		
		// Last resort.
		return 0;
	}
	
	// Best used to set a delayed terminate using the playing audio returned by a play().
	public static void stopSound(PlayingAudio sound, float fadeout = 0.3f, float delay = 0f)
	{
		RoutineRunner.instance.StartCoroutine(stopSoundDelayHandler(sound, fadeout, delay));
	}
	
	
	private static IEnumerator stopSoundDelayHandler(PlayingAudio sound, float fadeout = 0.3f, float delay = 0f)
	{
		if (delay > 0)
		{
			yield return new WaitForSeconds(delay);
		}
		// Only stop the sound if it's not already stopped.
		if (sound != null && sound.audioSource != null && sound.audioSource.isPlaying)
		{
			sound.stop(fadeout);
		}
	}
	
	
	public static PlayingAudio findPlayingAudio(string key)
	{
		for (int i = 0; i < SOURCE_POOL_SIZE; i++)
		{
			if (poolPlaying[i] != null && poolPlaying[i].isPlaying)
			{
				if (poolPlaying[i].audioInfo.keyName == key)
				{
					return poolPlaying[i];
				}
			}
		}
		
		return null;
	}

	public static bool isAudioPlayingByChannel(string channelName)
	{
		for (int i = 0; i < SOURCE_POOL_SIZE; i++)
		{
			if (poolPlaying[i] != null && poolPlaying[i].isPlaying)
			{
				AudioInfo audioInfo = poolPlaying[i].audioInfo;

				if (audioInfo.hasChannelTag(channelName))
				{
					return true;
				}
			}
		}
		
		return false;
	}

	// Private helper function for getting the sound map for a given gameKey
	private static Dictionary<string, string> getSoundMap(string gameKey = null)
	{
		// If no gamekey is given, attempt to find one based on the current game playing.
		if (gameKey == null)
		{
			if (GameState.game != null)
			{
				gameKey = GameState.game.keyName;
			}
		}
		
		// Attempt to get the soundmap data for the given gamekey.
		SlotGameData data = null;
		if (!string.IsNullOrEmpty(gameKey))
		{
			data = SlotGameData.find(gameKey);
		}
		
		// Attempt to get the soundmapped key from the given data.
		if (data != null)
		{
			return data.soundMap;
		}
		return null;
	}
	
	// Determine if a soundKey can be mapped to a sound
	public static bool canSoundBeMapped(string soundKey, string gameKey = null)
	{
		soundKey = trimSoundKey(soundKey);

		// Check if the default sound format exists for this sound key before checking the sound map
		if (defaultSoundFormat(soundKey, gameKey) != null)
		{
			return true;
		}

		string value = null;
		Dictionary<string, string> soundMap = getSoundMap(gameKey);
		
		// Attempt to get the soundmapped key from the given data.
		if (soundMap != null)
		{
			soundMap.TryGetValue(soundKey, out value);
		}
		
		if (value == null || value == "")
		{
			return false;
		}
		else
		{
			return true;
		}
	}

	// Determine if a key exists in the sound map, if it does return true. Otherwise return false
	public static bool isKeyInSoundMap(string soundKey, string gameKey = null)
	{
		Dictionary<string, string> soundMap = getSoundMap(gameKey);

		soundKey = trimSoundKey(soundKey);

		// Check if the default sound format exists for this sound key before checking the sound map
		if (defaultSoundFormat(soundKey, gameKey) != null)
		{
			return true;
		}
		
		if (soundMap != null)
		{
			return soundMap.ContainsKey(soundKey);
		}
		return false;
	}
	
	public static bool isSoundInRelativeDelayList(string soundKey)
	{
		// Check and see if the sond is in a collection
		List<string> possibleSounds = getAllSoundNamesInCollection(soundKey);
		if (possibleSounds != null) {
			foreach (string str in possibleSounds) {
				if (relativeDelayVOKeys.Contains (str)) {
					return true;
				}
			}
		} else {
			return relativeDelayVOKeys.Contains (soundKey);
		}
		return false;
	}

	// Ensure that the soundKeys we get are trimmed
	private static string trimSoundKey(string soundKey)
	{
		if (soundKey == null)
		{
			// If someone tries to trim a null sound give it right back to them.
			return null;
		}

		string trimmedSoundKey = soundKey.Trim();
		if (trimmedSoundKey != soundKey)
		{
			soundKey = trimmedSoundKey;
			Debug.LogWarning("Audio.trimSoundKey() - trimmed soundKey = \"" + soundKey + "\" to trimmedSoundKey = \"" + trimmedSoundKey + "\"");
		}

		return trimmedSoundKey;
	}

	public static string soundMap(string soundKey, string gameKey = null)
	{
		soundKey = trimSoundKey(soundKey);

		string key = null;
		
		// Attempt to get the default sound format for the given sound key.
		key = defaultSoundFormat(soundKey, gameKey);
		if (key != null)
		{
			return key;
		}

		// If no gamekey is given, attempt to find one based on the current game playing.
		if (gameKey == null)
		{
			if (GameState.game != null)
			{
				gameKey = GameState.game.keyName;
			}
		}

		// Attempt to get the soundmap data for the given gamekey.
		SlotGameData data = null;
		if (!string.IsNullOrEmpty(gameKey))
		{
			data = SlotGameData.find(gameKey);
		}
		
		// Attempt to get the soundmapped key from the given data.
		if (data != null)
		{
			// make sure key isn't null here since you can't pass this function null
			key = "";
			bool isMapValueFound = data.soundMap.TryGetValue(soundKey, out key);
			if (!isMapValueFound)
			{
				// didn't actually find a mapped value so set key null again
				key = null;
			}
		}
		
		// Log a problem if there is one because this fails silently otherwise.
		if (key == null)
		{
			soundLog("Could not SoundMap Key: \"" + soundKey + "\" for game: \"" + (string.IsNullOrEmpty(gameKey) ? "null" : gameKey) + "\"");
		}
		return key;
	}

	// Tell if the passed soundKey is a collection of sounds
	private static bool isSoundKeyACollection(string soundKey)
	{
		PlaylistInfo playlistInfo = PlaylistInfo.find(soundKey);
		if (playlistInfo != null)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public static List<string> getAllSoundNamesInCollection(string soundKey)
	{
		List<string> allSoundNames = null;
		PlaylistInfo playlistInfo = PlaylistInfo.find(soundKey);
		if (playlistInfo != null)
		{
			allSoundNames = playlistInfo.tracks;
		}
		return allSoundNames;
	}
	
	public static void soundLog(string log)
	{
		if (Application.isEditor && Audio.instance.optionalLogs)
		{
			Debug.Log(log);
		}
	}		

	// Returns the default sound name format (e.g. BonusAnticipate03Bev03) from a given master sound key (e.g. bonus_anticipate_03)
	// Returns null if sound key does not have a default name format
	// See HIR-37988 Audio Mappings Default Name Formats for more details
	private static string defaultSoundFormat(string soundKey, string gameKey = null)
	{
		if (gameKey == null && GameState.game != null)
		{
			gameKey = SlotResourceMap.getData(GameState.game.keyName).audioKeyPath;
		}

		if (gameKey != null)
		{
			string defaultKey = CommonText.snakeCaseToPascalCase(soundKey);
			defaultKey += char.ToUpper(gameKey[0]) + gameKey.Substring(1);

			// If default sound format exists in the audio dictionaries, return it
			if (PlaylistInfo.find(defaultKey) != null || AudioInfo.find(defaultKey) != null)
			{
				return defaultKey;
			}
		}
	 	
		return null;
	}

	public static void resetStaticClassData()
	{	
		unityPrefs = null;
		poolIsIntialized = false; //PlayingAudio objects are destroyed on reset so we need to reset this flag to reinitialize the pool
	}	
}
