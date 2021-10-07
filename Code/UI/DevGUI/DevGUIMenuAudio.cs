using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DevGUIMenuAudio : DevGUIMenu
{
	public static 	bool trackAudio = false;
	public static 	bool logAllAudio = true;
	public static	bool logToConsole = true;
	public static bool superVerbose = false;
	public static	string audioToTrack = "trigger_symbol";
	private static  string consoleLog = "";
	private static	string audioTrackingLog = "";
	private static	string audioTrackingDisplayLog = "";
	private static  bool breakOnAudioTrackLogging = false;
	private static  bool breakOnAudioAbort = false;
	private static  bool breakOnAudioStop = false;
	private static  bool breakOnAudioFail = false;
	private static  AudioInfo  trackedAudioInfo;
	private static	PlayingAudio trackedPlayingAudio;
	private static float startTime;
	private static bool paused;
	private static bool trackDelayedAudio = false;

	private GUIStyle  toggleStyle;
	private bool firstDraw = true;
	private int logBufferSize;

	private const int AUDIO_CHANNEL_DUCK_VALUES_PER_ROW = 3;

	public  IEnumerator audioWatcher()
	{
		PlayingAudio currentAudio = null;

		while (trackAudio)
		{
			if (trackedPlayingAudio != null && trackedPlayingAudio.audioInfo != null && !trackedPlayingAudio.audioSource.isPlaying)
			{
				logAudioTrackingData("Audio " + trackedPlayingAudio.audioInfo.keyName + " has reached its end at time " + Time.time);
				logAudioTrackingData("Play Duration : " + System.TimeSpan.FromSeconds(Time.time - startTime).ToString() + " seconds.");
				trackedAudioInfo = null;
				trackedPlayingAudio = null;
			}
			yield return null;
		}
	}

	public override void drawGuts()
	{
		initCheck();

		renderMaxVolumeSlider();

		GUILayout.Label("Default Music Key : " + Audio.defaultMusicKey);

		GUILayout.Label("Audio Listener Volume : " + Audio.listenerVolume);

		GUILayout.Label($"Temp mute: {Audio.tempMuted}");

		renderChannelDuckValues();

		renderAudioTextEdit();

		bool prevTracking = trackAudio;

		renderCheckBoxSection();

		updateTrackingState(prevTracking);

		renderPlayState();

		renderButtons();

		drawEmailButtonGuts("Email audio log", handleEmailClick);

		renderLog();
	}

	public void renderChannelDuckValues()
	{
		Dictionary<string, float> channelDuckValues = AudioChannel.getImportantAudioChannelDuckingValues();
		GUILayout.BeginVertical();

		GUILayout.Label("Current Channel Ducking:");
		int rowCount = 0;
		bool isHorizontalGroupEnded = false;
		foreach (KeyValuePair<string, float> channelDuck in channelDuckValues)
		{
			if (rowCount == 0)
			{
				GUILayout.BeginHorizontal();
				isHorizontalGroupEnded = false;
			}
			GUILayout.Label(channelDuck.Key + ".duckingLevel = " + channelDuck.Value);
			rowCount++;

			if (rowCount >= AUDIO_CHANNEL_DUCK_VALUES_PER_ROW)
			{
				GUILayout.EndHorizontal();
				isHorizontalGroupEnded = true;
				rowCount = 0;
			}
		}

		if (!isHorizontalGroupEnded)
		{
			GUILayout.EndHorizontal();
		}

		GUILayout.EndVertical();
	}

	public void renderMaxVolumeSlider()
	{
		GUILayout.BeginHorizontal();
		float oldMaxGlobalVolume = Audio.maxGlobalVolume;
		GUILayout.Label("Max Volume",GUILayout.Width(isHiRes ? 260 : 130));
		if (GUILayout.Button("0.1", GUILayout.Width(isHiRes ? 80 : 40)))
		{
			Audio.maxGlobalVolume = 0.1f;
		}
		if (GUILayout.Button("0.25", GUILayout.Width(isHiRes ? 80 : 40)))
		{
			Audio.maxGlobalVolume = 0.25f;
		}
		if (GUILayout.Button("0.5", GUILayout.Width(isHiRes ? 80 : 40)))
		{
			Audio.maxGlobalVolume = 0.5f;
		}
		if (GUILayout.Button("1", GUILayout.Width(isHiRes ? 80 : 40)))
		{
			Audio.maxGlobalVolume = 1.0f;
		}
		GUILayout.Label(string.Format("{0:0.000}", Audio.maxGlobalVolume), GUILayout.Width(isHiRes ? 100 : 50));
		Audio.maxGlobalVolume = GUILayout.HorizontalSlider(Audio.maxGlobalVolume, 0.0f, 1.0f, GUILayout.Width(isHiRes ? 300 : 150));

		if (Audio.maxGlobalVolume != oldMaxGlobalVolume)
		{
			if (oldMaxGlobalVolume != 0)
			{
				Audio.listenerVolume *= (Audio.maxGlobalVolume / oldMaxGlobalVolume);
			}
			else
			{
				Audio.listenerVolume *= Audio.maxGlobalVolume;
			}
			PlayerPrefsCache.SetFloat(Prefs.MAX_SOUND_VOLUME, Audio.maxGlobalVolume);
		}

		GUILayout.EndHorizontal();
	}

	public void handleEmailClick()
	{
		string subject = "Audio Log for";
		sendDebugEmail(subject, audioTrackingLog);
	}

	private void updateTrackingState(bool prevTracking)
	{
		if (prevTracking && !trackAudio)
		{
			logAudioTrackingData("Tracking Disabled. \n");
		}
		if (!prevTracking && trackAudio && audioToTrack.Length > 0)
		{
			logAudioTrackingData("Tracking enabled!. \n");
			setUpTracking();
		}
	}

	private void renderAudioTextEdit()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Audio asset to track (Enter SCAT ID or the Clip name.)", GUILayout.Width(380));
		audioToTrack =	GUILayout.TextField(audioToTrack);
		GUILayout.EndHorizontal();
	}

	private void initCheck()
	{
		if (firstDraw == true)
		{
			toggleStyle = new GUIStyle(GUI.skin.toggle);
			toggleStyle.normal.textColor = Color.black;


			logAudioTrackingData("Tracking Log\n\n\n");
			audioToTrack =PlayerPrefsCache.GetString(DebugPrefs.LAST_TRACKED_AUDIO);
			firstDraw = false;
		}
	}

	private void renderCheckBoxSection()
	{
		trackAudio = GUILayout.Toggle(trackAudio, "Enable Audio Tracking. This must be enabled for anything to work.", toggleStyle);
		logAllAudio = GUILayout.Toggle(logAllAudio, "Log All Audio", toggleStyle);
		logToConsole = GUILayout.Toggle(logToConsole, "Log To Unity Console", toggleStyle);
		superVerbose = GUILayout.Toggle(superVerbose, "Super Verbose Logging", toggleStyle);
		breakOnAudioTrackLogging = GUILayout.Toggle(breakOnAudioTrackLogging, "Break on Audio Play for tracked audio", toggleStyle);
		breakOnAudioAbort = GUILayout.Toggle(breakOnAudioAbort, "Break on Audio Abort for tracked audio", toggleStyle);
		breakOnAudioStop = GUILayout.Toggle(breakOnAudioStop, "Break on Audio Stop for tracked audio", toggleStyle);
		breakOnAudioFail = GUILayout.Toggle(breakOnAudioFail, "Break on Audio Error for any audio", toggleStyle);

		trackDelayedAudio = GUILayout.Toggle(trackDelayedAudio, "Show audio calls that are delayed", toggleStyle);

	}

	private void renderButtons()
	{
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Play Audio"))
		{
			Audio.listenerVolume = 1.0f;
			Audio.play(audioToTrack);
		}

		if (GUILayout.Button("Clear Log"))
		{
			audioTrackingLog = "";
		}

		GUILayout.EndHorizontal();
	}

	private void renderPlayState()
	{
		string playingStr = "Tracked audio is not currently playing.";
		if (trackedPlayingAudio != null && trackedPlayingAudio.audioInfo == trackedAudioInfo && trackedPlayingAudio.isPlaying)
		{
			playingStr = audioToTrack + " is playing. Volume : " + trackedPlayingAudio.relativeVolume;
		}
		GUILayout.Label(playingStr);
	}

	private void renderLog()
	{
		if (!paused && logBufferSize != audioTrackingLog.Length)
		{
			if (audioTrackingLog.Length > 16000)   // unity gets unhappy if it has to render more than this
			{
				audioTrackingDisplayLog = audioTrackingLog.Substring(audioTrackingLog.Length - 16000);
			}
			else
			{
				audioTrackingDisplayLog = audioTrackingLog;
			}

			logBufferSize = audioTrackingLog.Length;
		}

		GUILayout.BeginHorizontal();
		GUILayout.TextArea(audioTrackingDisplayLog);
		GUILayout.EndHorizontal();
	}

	private void setUpTracking()
	{
		string scatIdToTrack = "";

		PlayerPrefsCache.SetString(DebugPrefs.LAST_TRACKED_AUDIO, audioToTrack);

		trackAudio = true;

		RoutineRunner.instance.StartCoroutine(audioWatcher());

		string gameKey = "";
		if (GameState.game != null)
		{
			gameKey = GameState.game.keyName;
		}

		// Attempt to get the soundmap data for the given gamekey.
		SlotGameData data = null;
		string scatKey = "";

		if (!string.IsNullOrEmpty(gameKey))
		{
			data = SlotGameData.find(gameKey);
		}
		if (data != null)
		{

			if (data.soundMap.ContainsValue(audioToTrack))
			{
				foreach (string key in data.soundMap.Keys)
				{
					if (data.soundMap[key] == audioToTrack)
					{
						scatIdToTrack = key;
						break;
					}
				}
			}
			else if (data.soundMap.ContainsKey(audioToTrack))  // check if scat key was entered
			{
				scatIdToTrack = audioToTrack;
				audioToTrack = data.soundMap[scatIdToTrack];
			}
		}

		if (string.IsNullOrEmpty(scatIdToTrack))
		{
			scatKey = "SCAT ID NOT FOUND ";
		}
		else
		{
			scatKey = "SCAT ID : " + scatIdToTrack;
		}

		AudioInfo audioInfo = AudioInfo.find(audioToTrack);

		if (audioInfo == null)
		{
			logAudioTrackingData("AudioInfo for " + audioToTrack  + " could not be found, don't be suprised when it fails to play!\n");
		}

		logAudioTrackingData("Tracking " + audioToTrack);
		logAudioTrackingData(scatKey);
		logAudioTrackingData("\n");
	}

	public static void trackAudioCheck(PlayingAudio playingAudio)
	{
		if (trackedAudioInfo == playingAudio.audioInfo)
		{
			startTime = Time.time;
			logAudioTrackingData("Audio " + playingAudio.audioInfo.keyName + " has started at " + Time.time + "\n");
			trackedPlayingAudio = playingAudio;
		}
	}

	public static void trackFadeOut(PlayingAudio playingAudio, float fadeDuration)
	{
		if (trackedAudioInfo != null && trackedAudioInfo == playingAudio.audioInfo && superVerbose)
		{

			logAudioTrackingData("Audio " + playingAudio.audioInfo.keyName + " has has started fading out at " + Time.time);
			logAudioTrackingData("Fade Duration : " + fadeDuration + " seconds. \n");
		}
	}

	public static void trackStop(PlayingAudio playingAudio, bool isRecycleStop = false)
	{
		if (trackedAudioInfo != null && trackedAudioInfo == playingAudio.audioInfo && superVerbose)
		{
			if (isRecycleStop)
			{
				logAudioTrackingData("Audio " + playingAudio.audioInfo.keyName + " has Recycle stopped at " + Time.time);
			}
			else
			{
				logAudioTrackingData("Audio " + playingAudio.audioInfo.keyName + " has  stopped at " + Time.time);
			}

			logAudioTrackingData("Play Duration : " + System.TimeSpan.FromSeconds(Time.time - startTime).ToString() + " seconds. \n");
			audioTrackerStoppedBreakCheck();
			trackedAudioInfo = null;
		}
	}

	public static void trackAbort(PlayingAudio playingAudio, PlayingAudio abortedAudio)
	{
		if (trackedAudioInfo == abortedAudio.audioInfo && superVerbose)
		{

			logAudioTrackingData("Audio " + trackedAudioInfo.keyName + " has been aborted by " + playingAudio.audioInfo.keyName);
			logAudioTrackingData("Play Duration : " + System.TimeSpan.FromSeconds(Time.time - startTime).ToString() + " seconds.");
			audioTrackerAbortBreakCheck();
			trackedAudioInfo = null;
		}
	}

	public static void logAudioMute(bool value)
	{
		if (logAllAudio)
		{
			logAudioTrackingData(string.Format("MUTE {0}", value));
		}
	}

	public static void logAudioFadeOut(string key)
	{
		if (logAllAudio || key == audioToTrack)
		{
			logAudioTrackingData("FADEOUT " + key + " at " + Time.time + ".\n");
			if (logToConsole)
			{
				Debug.Log(consoleLog);
				consoleLog = "";
			}
		}
	}

	public static void logAudioNotFound(string key)
	{
		if (logAllAudio || key == audioToTrack)
		{
			logAudioTrackingData("Tried to play " + key + " but it could not be found with AudioInfo.find at " + Time.time + ".\n"
			 + "Timestamp: " + System.DateTime.Now + ".\n");
			audioTrackerFailedToFindAudioBreakCheck(key);
		}
	}

	public static void logAudioError(AudioInfo audioInfo, string error)
	{
		if (logAllAudio || audioInfo.keyName == audioToTrack)
		{
			logAudioTrackingData("AUDIO ERROR! : " + audioInfo.keyName + "\n" + error + "\n");
			audioTrackerFailedToFindAudioBreakCheck(audioInfo.keyName);
		}
	}

	public static void logAudioPlay(
		AudioInfo audioInfo,
		Vector3 position,
		float relativeVolume = 1.0f,
		float relativePitch = 0.0f,
		float relativeDelay = 0.0f,
		float loops = 0)
	{
		if (logAllAudio || audioInfo.keyName == audioToTrack)
		{
			if (!trackDelayedAudio && relativeDelay > 0)
			{
				return;
			}

			if (audioInfo.keyName == audioToTrack)
			{
				trackedAudioInfo = audioInfo;
			}

			logAudioTrackingData("Audio Play called " + audioInfo.keyName + " at " + Time.time);

			if (superVerbose)
			{

				logAudioTrackingData("Position " + position.ToString());
				logAudioTrackingData("Final Volume: " + AudioChannel.calculateVolume(audioInfo, relativeVolume));
				logAudioTrackingData("Audio Info Volume: " + audioInfo.volume);

				// tell the volume for each audio channel associated with this clip
				logAudioTrackingData("Channels:");
				for (int i = 0; i < audioInfo.channelTags.Count; i++)
				{
					AudioChannel channel = audioInfo.channelTags[i];
					logAudioTrackingData("Channel: " + channel.keyName + " - Volume = " + channel.currentVolume + ", Ducking = " + channel.duckingLevel);
				}

				// tell the ducking info for this clip
				if (audioInfo.duckChannels.Count > 0)
				{
					logAudioTrackingData("This Clip is Ducking:");
					for (int i = 0; i < audioInfo.duckChannels.Count; i++)
					{
						AudioDuckInfo duckInfo = audioInfo.duckChannels[i];
						logAudioTrackingData("Ducks Channel: " + duckInfo.channel.keyName + " Volume " + duckInfo.volume);
					}
				}

				logAudioTrackingData("Relative Volume " + relativeVolume);
				logAudioTrackingData("Relative Pitch " + relativePitch);
				logAudioTrackingData("Relative Delay " + relativeDelay);
				logAudioTrackingData("Loops " + loops);
			}
			logAudioTrackingData("Bundle Name " + audioInfo.bundleKey + "\n");
			audioTrackerBreakCheck(audioInfo.keyName);

			if (logToConsole)
			{
				Debug.Log(consoleLog);
				consoleLog = "";
			}

		}
	}

	public static void logAudioTrackingData(string data)
	{
		audioTrackingLog += (data + "\n");

		if (logToConsole)
		{
			consoleLog += (data + "\n");
		}
	}

	private static void audioTrackerStoppedBreakCheck()
	{
		if (breakOnAudioStop)
		{
			Debug.LogWarning("Audio Tracker break for stopped audio " + audioToTrack);
#if UNITY_EDITOR
			Debug.Break();
#endif
		}
	}

	private static void audioTrackerAbortBreakCheck()
	{

		if (breakOnAudioAbort)
		{
			Debug.LogWarning("Audio Tracker break for aborted audio " + audioToTrack);
#if UNITY_EDITOR
			Debug.Break();
#endif
		}
	}

	private static void audioTrackerBreakCheck(string keyName)
	{
		if (breakOnAudioTrackLogging && keyName == audioToTrack)
		{
			Debug.LogWarning("Audio Tracker break for " + audioToTrack);
#if UNITY_EDITOR
			Debug.Break();
#endif
		}

	}

	private static void audioTrackerFailedToFindAudioBreakCheck(string key)
	{

		if (breakOnAudioFail)
		{
			Debug.LogWarning("Audio Tracker break for failed audio " + key);
#if UNITY_EDITOR
			Debug.Break();
#endif
		}
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}

}
