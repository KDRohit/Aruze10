using System.Collections;
using Com.Scheduler;
using UnityEngine;
using RenderHeads.Media.AVProVideo;
using TMPro;

public class VideoDialog : DialogBase 
{
	//m3u8 ios
	//mpd Everything else

	//Need Fallback image path
	[SerializeField] private ButtonHandler closeButton;
	[SerializeField] private ButtonHandler actionButton;
	[SerializeField] private ButtonHandler collectButton;
	[SerializeField] private ButtonHandler skipButton; //Skips to loading our summary screen
	[SerializeField] private ClickHandler playButton; //Only used on WebGl
	[SerializeField] private ClickHandler muteButton; // Mutes the audio for this video only

	[SerializeField] private LabelWrapperComponent ctaButtonLabel;

	[SerializeField] private MediaPlayer mediaPlayer;
	[SerializeField] private GameObject videoContainer;
	[SerializeField] private Renderer summaryScreen;

	[SerializeField] private GameObject coinParent;
	[SerializeField] private GameObject coinTrail;
	[SerializeField] private TextMeshPro coinAmountLabel;

	[SerializeField] private GameObject soundOnParent;
	[SerializeField] private GameObject soundOffParent;
	
	private string videoLocation = "";
	private string ctaAction = "";
	private string ctaText = "";
	private string statPhylum = "";
	private string statClass = "";
	private string closeAction = "";

	private int closeDelay = 0;
	private int skipDelay = 0;

	private GameTimerRange closeButtonDelay = null;
	private GameTimerRange skipButtonDelay = null;

	private bool wasSkipped = false;

	private bool canAutoPlay = true;

	private bool videoTimedOut = true;

	private bool isVideoMuted = false;
	private bool hasVideoStarted = false;
	private Material clonedMaterial = null;

#if !UNITY_IOS && !UNITY_EDITOR
	private const string URL_FILE_EXTENSTION = ".mpd"; //MPEG-Dash
#else
	private const string URL_FILE_EXTENSTION = ".m3u8"; //HLS
#endif

	public const string LIVE_DATA_DISABLE_KEY = "VIDEOS_DISABLED";

	public override void init()
	{
		closeButton.registerEventDelegate(closeClicked);
		skipButton.registerEventDelegate(skipClicked);
		skipButton.gameObject.SetActive(false); //Hide this until the video is ready
		actionButton.registerEventDelegate(ctaClicked);
		collectButton.registerEventDelegate(ctaClicked);
		videoLocation = (string)dialogArgs.getWithDefault(D.URL, "");
		ctaAction = (string)dialogArgs.getWithDefault(D.ANSWER, "");
		ctaText = (string)dialogArgs.getWithDefault(D.MESSAGE, "");
		statPhylum = (string)dialogArgs.getWithDefault(D.DATA, "");
		statClass = (string)dialogArgs.getWithDefault(D.OPTION, "");
		closeDelay = (int)dialogArgs.getWithDefault(D.CLOSE, 0);
		skipDelay = (int)dialogArgs.getWithDefault(D.CUSTOM_INPUT, 0);
		closeAction = (string) dialogArgs.getWithDefault(D.SECONDARY_CALLBACK, "");
		
#if UNITY_WEBGL
		bool wasAutoPopped = (bool) dialogArgs.getWithDefault(D.MODE, true);
		canAutoPlay = Audio.muteMusic || !wasAutoPopped; //Can successfully autoplay the video its muted or if we manually triggered it
		if (!canAutoPlay)
		{
			playButton.registerEventDelegate(playClicked);
		}
#endif

		StatsManager.Instance.LogCount(
			counterName:"dialog",
			kingdom: "video",
			phylum: statPhylum,
			genus: "view",
			klass: statClass
		);

		//If we have a delay set for this then turn it off and start the timer
		if (closeDelay > 0)
		{
			closeButton.gameObject.SetActive(false);
			closeButtonDelay = GameTimerRange.createWithTimeRemaining(closeDelay);
			closeButtonDelay.registerFunction(turnOnButton, Dict.create(D.OBJECT, closeButton.gameObject));
		}
		else if (closeDelay < 0)
		{
			//If theres a negative delay then leave this off for the whole video
			closeButton.gameObject.SetActive(false);
		}

		if (ctaText != "")
		{
			ctaButtonLabel.text = Localize.textUpper(ctaText);
		}

		videoLocation += URL_FILE_EXTENSTION;

		if (ExperimentWrapper.VideoSoundToggle.isInExperiment)
		{
			mediaPlayer.m_Muted = Audio.muteSound; //Don't play the video sounds if we've muted SOUNDS in the settings
			isVideoMuted = Audio.muteSound;
			muteButton.gameObject.SetActive(true);
			muteButton.registerEventDelegate(muteClicked);
			setMuteButtonState();
		}
		else
		{
			mediaPlayer.m_Muted = Audio.muteMusic; //Don't play the video sounds if we've muted MUSIC in the settings
			muteButton.gameObject.SetActive(false);
		}

		mediaPlayer.OpenVideoFromFile(RenderHeads.Media.AVProVideo.MediaPlayer.FileLocation.AbsolutePathOrURL, videoLocation, false);
		mediaPlayer.Events.AddListener(onVideoEvent);
		
		int videoTimeout = Data.liveData.getInt("VIDEO_TIMEOUT_LENGTH", 20);
		GameTimerRange timeoutTimer = GameTimerRange.createWithTimeRemaining(videoTimeout);
		timeoutTimer.registerFunction(videoExpiredEvent);
		
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "video",
			phylum: statPhylum,
			klass: "",
			family: Audio.muteSound.ToString(),
			genus: isVideoMuted ? "sound_off" : "sound_on");
	}
	
	public void videoExpiredEvent(Dict data = null, GameTimerRange sender = null)
	{
		if (this != null && videoTimedOut)
		{
			wasSkipped = true;
			videoFinished();
		}
	}

	private void turnOnButton(Dict data = null, GameTimerRange sender = null)
	{
		GameObject buttonToTurnOn = (GameObject) data.getWithDefault(D.OBJECT, null);

		if (this != null && this.gameObject != null && buttonToTurnOn != null)
		{
			buttonToTurnOn.gameObject.SetActive(true);
		}
	}

	private void muteMusicAndSound(bool shouldMute)
	{
		Audio.tempMuted = shouldMute;
	}

	public void onVideoEvent(MediaPlayer mp, MediaPlayerEvent.EventType type, ErrorCode errorCode)
	{
		if (Data.debugMode)
		{
			Debug.Log($"VIDEO: {type.ToString()}");
		}
		switch(type)
		{
			case MediaPlayerEvent.EventType.ReadyToPlay:
				//If we have a delay set for this then turn it off and start the timer
				if (!wasSkipped && !hasVideoStarted)
				{
					//Mute All other game sounds when playing the video
					muteMusicAndSound(true);

					if (canAutoPlay)
					{
						mediaPlayer.Control.Play(); //Don't play the video if skip was clicked before it was ready to start playing
					}
					else
					{
						playButton.gameObject.SetActive(true);
					}
				}

				break;
			
			case MediaPlayerEvent.EventType.Started:
				//Mute All other game sounds when playing the video
				muteMusicAndSound(true);
				turnOnSkipButton();
				videoTimedOut = false;
				hasVideoStarted = true; //In some-cases ReadyToPlay is being called after the Started event. Need to this to prevent the playButton from appearing on top of a finished or playing video
				break;

			case MediaPlayerEvent.EventType.FinishedPlaying:

					StatsManager.Instance.LogCount(
						counterName:"dialog",
						kingdom: "video",
						phylum: statPhylum,
						genus: "complete",
						klass: statClass
					);

				videoFinished();
				break;

			case MediaPlayerEvent.EventType.Error:
				videoTimedOut = false;
				//Turn on our close buttons immediately if the video failed
				
				Debug.LogErrorFormat("Video {0} failed to load because of ErrorCode: {1}.", videoLocation,
					errorCode);

				if (SlotsPlayer.instance.reprice2019CreditsGrant > 0)
				{
					collectButton.gameObject.SetActive(true);
				}
				else
				{
					actionButton.gameObject.SetActive(true);
				}
				closeButton.gameObject.SetActive(true);
				videoFinished();
				break;
			default:
				break;
		}
	}
	
	private void turnOnSkipButton()
	{
		if (skipDelay > 0)
		{
			skipButtonDelay = GameTimerRange.createWithTimeRemaining(skipDelay);
			skipButtonDelay.registerFunction(turnOnButton, Dict.create(D.OBJECT, skipButton.gameObject));
		}
		else
		{
			skipButton.gameObject.SetActive(true);
		}
	}

	public void skipClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName:"dialog",
			kingdom: "video",
			phylum: statPhylum,
			genus: "skip",
			klass: statClass
		);

		wasSkipped = true;
		skipButton.gameObject.SetActive(false);
		playButton.gameObject.SetActive(false);
		muteButton.gameObject.SetActive(false);
		videoFinished();
	}

	public void ctaClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName:"dialog",
			kingdom: "video",
			phylum: statPhylum,
			genus: "click",
			klass: statClass
		);
		if (!string.IsNullOrEmpty(ctaAction) && DoSomething.getIsValidToSurface(ctaAction))
		{
			DoSomething.now(ctaAction);
		}

		if (SlotsPlayer.instance.reprice2019CreditsGrant > 0)
		{
			StartCoroutine(doRollupBeforeClose());
		}
		else
		{
			Dialog.close();
		}
	}

	public void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName:"dialog",
			kingdom: "video",
			phylum: statPhylum,
			genus: "close",
			klass: statClass
		);
		
		if (!string.IsNullOrEmpty(ctaAction) && DoSomething.getIsValidToSurface(ctaAction))
		{
			DoSomething.now(closeAction);
		}

		if (SlotsPlayer.instance.reprice2019CreditsGrant > 0)
		{
			StartCoroutine(doRollupBeforeClose());
		}
		else
		{
			Dialog.close();
		}
	}

	public void playClicked(Dict args = null)
	{
		playButton.gameObject.SetActive(false);
		StartCoroutine(CommonGameObject.fadeGameObjectTo(playButton.gameObject, 1.0f, 0.0f, 0.25f, false));
		mediaPlayer.Control.Play();
	}

	private void muteClicked(Dict args = null)
	{
		isVideoMuted = !isVideoMuted; // Toggle the mute bool.
		mediaPlayer.Control.MuteAudio(isVideoMuted);
		string genus = isVideoMuted ? "sound_off" : "sound_on";
		setMuteButtonState();
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "video",
			phylum: statPhylum,
			klass: "",
			family: Audio.muteSound.ToString(),
			genus: genus);
	}

	private void setMuteButtonState()
	{
		soundOnParent.SetActive(!isVideoMuted);
		soundOffParent.SetActive(isVideoMuted);
	}


	private IEnumerator doRollupBeforeClose()
	{
		PlayerPrefsCache.SetString(Prefs.LAST_SEEN_DYNAMIC_VIDEO, ExperimentWrapper.DynamicVideo.url);
		SlotsPlayer.addCredits(SlotsPlayer.instance.reprice2019CreditsGrant, "Reprice 2019 Video FTUE");
		SlotsPlayer.instance.reprice2019CreditsGrant = 0;
		coinTrail.SetActive(true);
		yield return new WaitForSeconds(3.0f);
		Dialog.close();
	}

	private void videoFinished()
	{
		mediaPlayer.enabled = false;
		//Restore sound settings
		muteMusicAndSound(false);

		string imagePath = (string)dialogArgs.getWithDefault(D.IMAGE_PATH, "");
		if (!string.IsNullOrEmpty(imagePath))
		{
			StartCoroutine(DisplayAsset.loadTexture(imagePath, imageTextureLoaded));
		}

		if (SlotsPlayer.instance.reprice2019CreditsGrant > 0)
		{
			coinParent.SetActive(true);
			coinAmountLabel.text = CreditsEconomy.convertCredits(SlotsPlayer.instance.reprice2019CreditsGrant);
		}

		//Turn on our buttons
		if (SlotsPlayer.instance.reprice2019CreditsGrant > 0)
		{
			collectButton.gameObject.SetActive(true);
		}
		else
		{
			actionButton.gameObject.SetActive(true);
		}
		closeButton.gameObject.SetActive(true);
		skipButton.gameObject.SetActive(false);
		playButton.gameObject.SetActive(false);
		muteButton.gameObject.SetActive(false);
	}

	private void imageTextureLoaded(Texture2D tex, Dict data = null)
	{
		if (this != null && this.gameObject != null)
		{
			//Stop the Video and turn it off
			videoContainer.SetActive(false);
			
			//Set the summary screen on
			clonedMaterial = new Material(summaryScreen.material.shader);
			clonedMaterial.mainTexture = tex;
			summaryScreen.material = clonedMaterial;
			summaryScreen.gameObject.SetActive(true);
		}
	}

	public override void close()
	{
		//Restore sound settings
		muteMusicAndSound(false);

		if (!string.IsNullOrEmpty((string)dialogArgs.getWithDefault(D.MOTD_KEY, "")))
		{
			MOTDFramework.markMotdSeen(dialogArgs);
		}

		if (clonedMaterial != null)
		{
			Destroy(clonedMaterial);
		}
	}

	public static bool showDialog
	(
		string videoPath,
		string action = "",
		string actionLabel = "",
		string statName = "",
		int closeButtonDelay = 0,
		int skipButtonDelay = 0,
		string motdKey = "",
		string summaryScreenImage = "",
		bool autoPopped = true,
		string statClass = "",
		string closeAction = "",
		bool topOfList = false,
		SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.BLOCKING //Videos shouldn't be able to be interrupted
	)
	{
		if (Data.liveData.getBool(LIVE_DATA_DISABLE_KEY, false))
		{
			return false;
		}
		
		Dict dialogArgs = Dict.create
		(
			D.URL, videoPath,
			D.ANSWER, action,
			D.MESSAGE, actionLabel,
			D.CLOSE, closeButtonDelay,
			D.CUSTOM_INPUT, skipButtonDelay,
			D.DATA, statName,
			D.MOTD_KEY, motdKey,
			D.IMAGE_PATH, summaryScreenImage,
			D.MODE, autoPopped,
			D.OPTION, statClass,
			D.SECONDARY_CALLBACK, closeAction,
			D.IS_TOP_OF_LIST, topOfList
		);

		Scheduler.addDialog
		(
			"video_dynamic",
			dialogArgs,
			priority 
		);

		return true;
	}

	public static void queueRepriceVideo(bool autoPopped = true)
	{
		showDialog(
			ExperimentWrapper.RepriceVideo.url, 
			ExperimentWrapper.RepriceVideo.action, 
			ExperimentWrapper.RepriceVideo.buttonText, 
			ExperimentWrapper.RepriceVideo.statName, 
			ExperimentWrapper.RepriceVideo.closeButtonDelay,
			ExperimentWrapper.RepriceVideo.skipButtonDelay,
			"",
			ExperimentWrapper.RepriceVideo.imagePath,
			autoPopped
		);
	}

	protected override void onHide()
	{
		base.onHide();
		mediaPlayer.Control.Pause();
	}
	
	protected override void onShow()
	{
		base.onShow();
		mediaPlayer.Control.Play();
	}
}
