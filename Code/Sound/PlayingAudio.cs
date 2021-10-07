using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This MonoBehaviour sits on an object in the audio source pool (see Audio.cs).
It acts as a controller for played sounds. It is recycled between uses.
*/
[RequireComponent (typeof (AudioSource))]
public class PlayingAudio : TICoroutineMonoBehaviour
{
	public static float DISTANCE_BIAS = 1f;		///< A bias factor for the distance falloff of sounds
	
	private const float MIXER_SAMPLE_RATE = 44100f;	///< The mixer sampling rate, used for seconds-to-samples conversion
	private const float MEANINGFUL_VOLUME_LEVEL = 0.01f;	///< The minimum meaningful volume level (for fadeouts)
	
	public AudioSource audioSource;					///< Inspector-linked AudioSource
	
	[HideInInspector] public bool locked = false;	///< Is this PlayingAudio locked from being recycled?
	
	public int id { get; private set; }				///< A unique id for this PlayingAudio
	public bool isPlaying { get; private set; }		///< Is this audio playing?
	public bool isStopping { get; private set; }	///< Is this audio stopping?
	
	public AudioInfo audioInfo { get; private set; }	///< The AudioInfo currently associated with this player
	public float startTime { get; private set; }		///< The time the sound starts (accounting for delay)
	public float delay { get; private set; }			///< Delay, in seconds, before the sound really starts
	public float endAfter { get; private set; }			///< The elapsed time at which to initiate recycling
	
	private float timesLooped;
	private float lastClipTime;
	private float fadeStartTime;
	private float fadeDuration;

	public bool isLooping
	{
		set { audioSource.loop = value; }
		get { return audioSource.loop; }
	}
	
	/// The time elapsed in the sound.
	public float elapsedTime
	{
		get
		{
			return Time.realtimeSinceStartup - startTime;
		}
	}
	
	public float relativeVolume;	///< Relative volume level (relative to global volume and ducking levels).
	
	/// Relative pitch (relative to pitch set in the audio data).
	public float relativePitch
	{
		get
		{
			return _relativePitch;
		}
		set
		{
			_relativePitch = value;
#if !UNITY_WEBGL
			audioSource.pitch = Mathf.Clamp(_relativePitch + audioInfo.pitch, -3f, 3f);
#endif
		}
	}
	private float _relativePitch;
	
	private List<AudioEvent> pendingEvents = new List<AudioEvent>();
	private List<AudioEventListener> eventListeners = new List<AudioEventListener>();
	
	/// Initializes the PlayingAudio object (should only be called once per instance).
	public void initForPool(int id)
	{
		gameObject.name = string.Format("source{0}", id);
		this.id = id;
		recycle();
	}
	
	/// Sets this up for playing a sound and starts playing it.
	public void setupPlay(
		AudioInfo audioInfo,
		float relativeVolume,
		float relativePitch,
		float loops)
	{
		isPlaying = true;
		
		// Make sure this audio stuff is active and enabled
		if (!gameObject.activeSelf)
		{
			gameObject.SetActive(true);
		}
		if (!audioSource.enabled)
		{
			audioSource.enabled = true;
		}
		if (!enabled)
		{
			enabled = true;
		}
		
		if (loops < 0)
		{
			// Allow for a manual override to disable looping regardless of queue loop events.
			loops = 0;
		}
		else
		{
			// Add the SCAT determined loop value to the client given one.
			loops += audioInfo.loops;
		}
		
		this.audioInfo = audioInfo;
		this.relativeVolume = relativeVolume;
		this.relativePitch = relativePitch;
		delay = Mathf.Max(0f, audioInfo.delay);
		
		startTime = Time.realtimeSinceStartup + delay;
		endAfter = ((loops + 1) * audioInfo.clip.length) + audioInfo.maximumUnduckTime;

		timesLooped = 0;
		lastClipTime = -1;
		audioInfo.markPlaying();
		
		audioSource.clip = audioInfo.clip;
		audioSource.maxDistance = audioInfo.rangeIn3d * DISTANCE_BIAS;
		audioSource.loop = (loops > 0);
		if (delay > 0)
		{
			audioSource.PlayDelayed(delay);
		}
		else
		{
			audioSource.Play();
		}

#if !UNITY_WEBGL
		audioSource.panStereo = audioInfo.pan;
#endif
		
		// Push on channel abort stacks
//		foreach (AudioAbortInfo abortInfo in audioInfo.abortChannels)
//		{
//			abortInfo.channel.pushAbort(this);
//		}
		
		// Push on channel ducking stacks
		foreach (AudioDuckInfo duckInfo in audioInfo.duckChannels)
		{
			duckInfo.channel.pushDuck(this);
		}
	}
	
	/// Starts the fade-out and stop of audio
	public void stop(float fadeDuration = 0.3f)
	{
		// check if audio is already not playing, thus already in the free pool and we will mess it up for next use if we set isStopping true at this point.
		if (!isPlaying)
		{
			string keyName = "Null audioInfo";
			if (audioInfo != null)
			{
				keyName = audioInfo.keyName;
			}

			Debug.LogWarning("Not allowing Attempt to fade stop on audio that is not playing and in the pool free for reuse. " + keyName);
			return;
		}
		isStopping = true;
		fadeStartTime = Time.realtimeSinceStartup;
		this.fadeDuration = fadeDuration;

		if (Data.debugMode && DevGUIMenuAudio.trackAudio)
		{
			DevGUIMenuAudio.trackFadeOut(this, fadeDuration);
			DevGUIMenuAudio.logAudioFadeOut(audioInfo.keyName);
		}
	}
	
	/// Reset all of the audioSource data and put this PlayingAudio into the unused pool.
	public void recycle()
	{
		if (audioInfo != null)
		{
			// Fire off any remaining events
			fireEvents(float.PositiveInfinity);
			
			// Pop channel abort stacks
//			foreach (AudioAbortInfo abortInfo in audioInfo.abortChannels)
//			{
//				abortInfo.channel.popAbort(this);
//			}
			
			// Pop channel ducking stacks
			if (audioInfo.duckChannels != null)
			{
				foreach (AudioDuckInfo duckInfo in audioInfo.duckChannels)
            	{
	                if (duckInfo == null || duckInfo.channel == null)
	                {
		                Debug.LogError("Channel is not valid");
		                continue;
	                }
            		duckInfo.channel.popDuck(this);
            	}
			}
			else
			{
				Debug.LogError("duckChannels don't exist");
			}
			
			
			// Reset some variables
			if (pendingEvents != null)
			{
				pendingEvents.Clear();
			}
			else
			{
				Debug.LogError("Pending events not intialized");
			}

			if (eventListeners != null)
			{
				eventListeners.Clear();	
			}
			else
			{
				Debug.LogError("Event Listeners not initialized");
			}
			
		}
	
		audioInfo = null;
		locked = false;
		isPlaying = false;
		isStopping = false;

		if (Data.debugMode && DevGUIMenuAudio.trackAudio)
		{
			DevGUIMenuAudio.trackStop(this, true);
		}

		if (audioSource != null)
		{
			audioSource.Stop();
			audioSource.clip = null;
			audioSource.volume = 0f;
			audioSource.pitch = 1f;
			audioSource.loop = false;
			audioSource.ignoreListenerVolume = false;
			audioSource.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
			audioSource.mute = false;
			audioSource.minDistance = 0f;
			audioSource.maxDistance = 400f;
			audioSource.rolloffMode = AudioRolloffMode.Linear;
			audioSource.bypassEffects = true;
			audioSource.dopplerLevel = 0f;
			audioSource.spread = 0f;
			audioSource.panStereo = 0f;
#if !UNITY_WEBGL
			audioSource.spatialBlend = 1f;
#else
			audioSource.spatialBlend = 0f;  // solves right-speaker only issue  https://issuetracker.unity3d.com/issues/webgl-chrome-audio-not-played-on-left-channel
#endif
		}
		else
		{
			Debug.LogError("Audio source is null");
		}

		if (this.gameObject != null && this.transform != null)
		{
			transform.parent = Audio.sourceRoot;	
		}
		else
		{
			Debug.LogError("Audio game object is fubar");
		}
		
		
		// Check to see if this item was ejected from the playing pool
		if (Audio.poolPlaying != null && id >= 0 && Audio.poolPlaying.Length > id && Audio.poolPlaying[id] != null && Audio.poolPlaying[id] != this)
		{
			// Commit seppuku
			Destroy(gameObject);
		}
		else if (Audio.poolPlaying != null) 
		{
			if (Audio.poolPlaying.Length <= id)
			{
				Debug.LogError("ID is larger than audio pool array");	
			}

			if (id < 0)
			{
				Debug.LogError("ID is less than zero");
			}
			
		}
		else
		{
			Debug.LogError("Audio pool is null");
		}
	}
	
	/// Add some event listeners.  For best results, be sure to add the event listeners
	/// on the line after an Audio.play() call, otherwise you might miss an event.
	public void addListeners(params AudioEventListener[] newEventListeners)
	{
		// When adding listeners for the first time, populate the events if they are not yet populated.
		if (eventListeners.Count == 0 && pendingEvents.Count == 0)
		{
			foreach (AudioEvent audioEvent in audioInfo.audioEvents)
			{
				pendingEvents.Add(audioEvent);
			}
			pendingEvents.Add(new AudioEvent("start", 0f));
			pendingEvents.Add(new AudioEvent("end", endAfter));
		}
		
		// Add the listeners to the list of listeners
		foreach (AudioEventListener listener in newEventListeners)
		{
			eventListeners.Add(listener);
		}
	}
	
	/// Remove some event listeners.
	public void removeListeners(params AudioEventListener[] deadEventListeners)
	{
		foreach (AudioEventListener listener in deadEventListeners)
		{
			eventListeners.Remove(listener);
		}
	}
	
	/// Updates ducked volume level and any event listeners, and recycles the this player on end.
	public void updatePlayingAudio()
	{
		if (audioSource == null)
		{
			// we shouldn't have to do this, but on static reset audio sources get destroyed
			// and there is a race condition with ambient crowd noise audio that starts playing on pre-awake
			// TBD solve the race condition by allocating all audio sources at once like we do on startup instead on the fly after a reset
			return;
		}
		float actualVolume = AudioChannel.calculateVolume(audioInfo, relativeVolume);
		float actualPitch = Mathf.Clamp(audioInfo.pitch + relativePitch + 1f, -3f, 3f);

		bool isCurrentlyPlaying = audioSource.isPlaying;
		
		// Adjust the startTime to match the current play position of the clip.
		// We do this to make sure that even when the clip is not playing, the
		// relative time is correct.
		if (isCurrentlyPlaying)
		{
			if (lastClipTime > audioSource.time)
			{
				audioInfo.markPlaying(); // We want to make sure the engine know's that we are still playing this sound.
				timesLooped += 1;
			}
			
#if !UNITY_WEBGL  // Seems to be a problem with WebGL & audioSource.time stamps... 
			startTime = Time.realtimeSinceStartup - audioSource.time - (timesLooped * audioSource.clip.length);
#endif
			lastClipTime = audioSource.time;
		}

		// Handle fadeouts
		if (isStopping)
		{
			float fade = 1f - Mathf.Clamp01((Time.realtimeSinceStartup - fadeStartTime) / fadeDuration);
			fade = fade * fade;		// Give it an exponential fade speed to approximate logrithmic fading
			actualVolume *= fade;	// Adjust the actual volume by the fadeout

			if (actualVolume < MEANINGFUL_VOLUME_LEVEL)
			{
				if (Data.debugMode && DevGUIMenuAudio.trackAudio)
				{
					DevGUIMenuAudio.trackStop(this);
				}
				audioSource.Stop();
				isStopping = false;

				// Adjust startTime for ducking/recycle use
				startTime = Mathf.Min(startTime, Time.realtimeSinceStartup - audioSource.clip.length);
			}
		}
		
		if (actualVolume < 0f || float.IsNegativeInfinity(actualVolume) || float.IsNaN(actualVolume))
		{
			actualVolume = 0f;
		}
		else if (actualVolume > 1f || float.IsPositiveInfinity(actualVolume))
		{
			actualVolume = 1f;
		}
		
		// Set the new volume only if it is different
		if (audioSource.volume != actualVolume)
		{
			audioSource.volume = actualVolume;
		}

#if !UNITY_WEBGL
		// Set the new pitch only if it is different
		if (audioSource.pitch != actualPitch)
		{
			audioSource.pitch = actualPitch;
		}
#endif
		
		float elapsed = elapsedTime;
	
		fireEvents(elapsed);
		
		// Debug some specific sounds, uncomment the below block to check something specific.
		// Please remember to re-comment-block i
		/*
		if (audioInfo.keyName.FastStartsWith("reelspinbaseOz00"))
		{
			string debugInfo = "";
			
			debugInfo += "Clip: " + audioInfo.keyName + " ";
			debugInfo += "actualVolume: " + actualVolume + "\n";
			debugInfo += "elapsed: " + elapsed + " ";
			debugInfo += "timesLooped: " + timesLooped + " \n";
			debugInfo += "actualPitch: " + actualPitch + "\n";
			debugInfo += "isStopping: " + isStopping + "\n";
			
			Debug.Log(debugInfo);
		}
		*/
		
		// If the clip is done playing and done stopping and isn't locked, recycle this player
		if (!locked && !(isCurrentlyPlaying || audioSource.clip.loadState==AudioDataLoadState.Loading))
		{
			recycle();
			return;
		}
		
		// If we've hit the end of our time, stop the sound. 
		if (elapsed > endAfter && !isStopping)
		{
			stop(0);
		}
	}
	
	/// Actually does the work for firing off events
	private void fireEvents(float elapsed)
	{
		// Only worry about events if we have event listeners
		if (eventListeners.Count != 0)
		{
			List<AudioEvent> firedEvents = new List<AudioEvent>();
			
			// Check each event to see if it fired
			foreach (AudioEvent audioEvent in pendingEvents)
			{
				if (elapsed >= audioEvent.time)
				{
					// Send the event to each listener
					foreach (AudioEventListener listener in eventListeners)
					{
						if (audioEvent.message == listener.eventType)
						{
							listener.eventDelegate(audioEvent, this);
						}
					}
					firedEvents.Add(audioEvent);
				}
			}
			
			// Remove fired-off events from the pending list
			foreach (AudioEvent audioEvent in firedEvents)
			{
				pendingEvents.Remove(audioEvent);
			}
		}
	}
}

/// A delegate for audio event callbacks
public delegate void AudioEventDelegate(AudioEvent audioEvent, PlayingAudio playingAudio);

/// Simple class for holding a delegate and a trigger
public class AudioEventListener
{
	public string eventType;
	public AudioEventDelegate eventDelegate;
	public AudioEventListener(string eventType, AudioEventDelegate eventDelegate)
	{
		this.eventType = eventType;
		this.eventDelegate = eventDelegate;
	}
}
