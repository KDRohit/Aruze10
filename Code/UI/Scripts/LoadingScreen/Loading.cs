using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
Base class for behaviour of loading screen.
*/

public class Loading : MonoBehaviour
{
	private const int LOGIN_TIMEOUT_SECONDS = 20;   // Time to wait before resetting the game if stuck on the loading screen after returning from login.

	private const string FLOW_LOADING_LOBBY_LOGIN = "loading-lobby-login";
	private const string FLOW_LOADING_LOBBY = "loading-lobby";
	private const string FLOW_LOADING_GAME_PREFIX = "loading-game-";
	private const string FLOW_LOADING_SV_LOBBY_PREFIX = "loading-sv-lobby-";

	public GameObject displayParent; // a parent for all the renderable bits we can enable/disable
	public UIMeterNGUI meter;
	public UIPanel loadingPanel;
	public TextMeshPro loadingLabel;
	public ShroudScript shroud;
	public TextMeshPro stageLabel;

	[HideInInspector] public bool isDownloading;
	[HideInInspector] public bool isLoggingIn = false;	// This is set to true when the loading screen is shown before switching over to login on Facebook.
	
	protected float actualProgress = 0;				// Normalized progress of actual downloading.
	protected float displayedProgress = 0;			// Normalized progress that is displayed.
	private GenericDelegate hideCallback = null;	// Optional passed in function to call when finished hiding.
	private List<string> downloadBundles = new List<string>(); // List of bundles being downloaded to reflect in progress.
	private bool shouldHide = false;
	private GameTimer resetTimer = null;
	protected bool isHiding = false;				// Whether the loading screen is in the process of hiding.

	public static Loading instance = null;
	public static LoadingHIRV3 hirV3 = null;
	
	public static bool isLoading { get; private set; }
	public static bool showingCustomLoading = false;

	// Crittercism transaction tracking for loading screen.
	private bool isTransactionActive = false;
	private LoadingTransactionTarget loadingTarget = LoadingTransactionTarget.NONE;
	protected LoadingTransactionResult loadingTransactionResult = LoadingTransactionResult.NONE;
	private string transactionName;
	private bool didDownloadBundles = false; // track if asset bundles were downloaded during this loading phase, will be included in the userflow as an extra field

	// Different parts of the loading screen label text (url, variant, etc), so they can be set at different times
	private string labelUrlText = "";
	private string labelVariantText = "";

	public string loadingUserflowString = "";
	private int downloadBundlesCount = 0;
	
	/// Use init() instead of Awake() to initialize, since this object is inactive by default, and Awake isn't called until it becomes active.
	public virtual void init()
	{
		instance = this;
		hirV3 = this as LoadingHIRV3;
		UnityEngine.Screen.sleepTimeout = SleepTimeout.NeverSleep;
		
		adjustForResolution();
		
		// Make sure it's hidden by default, and active for usage,
		// just in case someone didn't leave it that way in the scene.
		setAlpha(0f);
		gameObject.SetActive(true);
		
		// Make sure this inactive by default. Will be set active if the stage is non-production.
		stageLabel.gameObject.SetActive(false);
		
		if (Data.debugMode)
		{
			enableStageLabel(true);
			setStageUrlLabel(Data.serverUrl);
		}

#if UNITY_WEBGL
		stageLabel.fontSize = 32; // temp - Chuck wanted small StageLabel text for his WebGL demo
#endif
	}

	// Enables/Disables the stage label from appearing
	public void enableStageLabel(bool enabled)
	{
		stageLabel.gameObject.SetActive(enabled);
	}

	// Sets the URL portion of the stage label; this is also what enables the 
	public void setStageUrlLabel(string text)
	{
		labelUrlText = text;
		updateStageLabel();
	}

	// Updates the "variant" portion of the stage label; does not enable/disable the object
	public void setStageVariantLabel(string text)
	{
		labelVariantText = text;
		updateStageLabel();
	}

	// Combine the various stage label pieces of text (url, variant, ...)
	void updateStageLabel()
	{
		stageLabel.text = labelUrlText + "   " + labelVariantText + "\n" + Glb.buildTag;
	}

	public void resolutionChangeHandler()
	{
		if (hirV3 != null && hirV3.dynamicBackgroundTexture != null)
		{
			adjustForResolution(hirV3.dynamicBackgroundTexture.width, hirV3.dynamicBackgroundTexture.height);
		}
		else
		{
			adjustForResolution();	
		}
		
	}
	
	protected virtual void adjustForResolution(float contentWidth = MainLobbyV3.SCREEN_BASE_WIDTH, float contentHeight = MainLobbyV3.SCREEN_BASE_HEIGHT)
	{
	}
	
	protected virtual void Update()
	{	
		if (!isLoading || loadingPanel.alpha < 1.0f || isHiding)
		{
			// Don't do anything here if we aren't showing the loading screen or are in the process of hiding it.
			return;
		}

#if !ZYNGA_PRODUCTION
		TouchInput.update();
#endif
		// If we are downloading asset bundles then have _actualProgress reflect actual download progress.
		if (downloadBundles.Count > 0)
		{
			float bundleLoadProgress = 0f;
			foreach (string dl in downloadBundles)
			{
				bundleLoadProgress += AssetBundleManager.loadProgress(dl);
			}
			bundleLoadProgress /= downloadBundles.Count;
			if (bundleLoadProgress >= 0.999f)
			{
				downloadBundlesCount = downloadBundles.Count;
				downloadBundles.Clear();
				setDownloadingStatus(false);
				isDownloading = false;
			}
			// Debug.Log("bundleLoadProgress: " + bundleLoadProgress);
			actualProgress = Mathf.Min(0.99f, 0.99f * bundleLoadProgress);

			// Change label text
		}
		else
		{
			if (shouldHide)
			{
				actualProgress = 1.0f;
			}
			else if (resetTimer != null && resetTimer.isExpired)
			{
				// If we waited a long time after returning to the game from logging in,
				// then reset the game to prevent being stuck on the loading screen indefinitely.
				Debug.Log ("resetting game from Loading timeout");
				resetTimer = null;

				// stop the gameloader coroutine before we create more error logs
				GameLoader.Instance.abortLoading();
				Glb.resetGame("Loading screen timed out.");
			}
		}		

		// Keep the visual progress updated frequently,
		// even if there is no actual progress.
		// Smooths the meter so it never jumps in big increments.
				
		if (displayedProgress < actualProgress)
		{
			//If displayed progress is behind actual progress, move up to 5% more towards actual progress.
			//Since this is called every frame, try to limit how much of the bar is filled per second assuming 30fps
			float m = Mathf.Min(30, FrameReporter.averageFPS);
			displayedProgress += Mathf.Clamp(actualProgress - displayedProgress, 0, .05f * 30 / m);
			
		}
		else if (displayedProgress < .9f && downloadBundles.Count == 0)
		{
			// Limit simulated displayed progress to 90% unless actual progress is more than displayed progress,
			// or if the hide() call has been made.
			displayedProgress += .00125f;
		}
		
		displayedProgress = Mathf.Min(1f, displayedProgress);

		meter.currentValue = (int)(displayedProgress * 100f);
	}

	// Do this after the normal Update is done, so Update overrides can finish before doing this.
	protected void LateUpdate()
	{
		if (!isLoading || loadingPanel.alpha < 1.0f || isHiding)
		{
			// Don't do anything here if we aren't showing the loading screen or are in the process of hiding it.
			return;
		}

		//This usually gets checked for quite a few frames even after InbetweenSceneLoader has called LoadScene(), since
		//the progress bar visuals are somewhat faked and we do not hide the loading screen until the bar has filled.
		if (actualProgress >= 1f && displayedProgress >= 1f)
		{
			StartCoroutine(displayedProgressFinished());
		}
	}

	public virtual void setAlpha(float alpha)
	{
		loadingPanel.alpha = alpha;
		
		// Handle TextMeshPro labels.
		loadingLabel.alpha = alpha;
		stageLabel.alpha = alpha;

		// enable/disable rendering based on alpha visibility (don't render invisible loading screen)
		displayParent.SetActive(alpha > 0.0f);
	}
	
	/// Shows the loading screen.
	public static void show(LoadingTransactionTarget loadingTarget)
	{
		instance.StartCoroutine(instance.showMe(loadingTarget));
	}
	
	/// Starts hiding the loading screen.
	public static void hide(LoadingTransactionResult result, GenericDelegate callback = null)
	{
		instance.hideMe(result, callback);

		if (Data.isPlayerDataSet)
		{
			LoadingHIRMaxVoltageAssets.isLoadingMiniGame = false;
		}
	}

	public static void addDownload(string bundleName)
	{
		instance.didDownloadBundles = true;
		instance.isDownloading = true;

		if (!instance.downloadBundles.Contains(bundleName))
		{
			instance.downloadBundles.Add(bundleName);
			instance.setDownloadingStatus(true);
		}
	}

	protected virtual IEnumerator showMe(LoadingTransactionTarget loadingTarget)
	{
		if (!isLoading)
		{
			Bugsnag.LeaveBreadcrumb("Loading Screen is starting up");
			beginLoadingTransaction(loadingTarget);
		
			didDownloadBundles = false;
			actualProgress = 0;
			displayedProgress = 0;
			downloadBundles.Clear();
			shouldHide = false;
			setDownloadingStatus(false);
			isLoading = true;
			downloadBundlesCount = 0;

			NGUIExt.enableAllMouseInput();			
		}

		// The loading screen is already showing. Do nothing.
		yield break;
	}
	
	/// Actually just sets a flag and waits for displayed progress to reach 100% before hiding.
	protected virtual void hideMe(LoadingTransactionResult newResult, GenericDelegate callback)
	{
		if (!isLoading)
		{
			// The loading screen isn't showing, so we can't hide it.
			return;
		}
		
		if (isTransactionActive)
		{
			if (loadingTransactionResult != LoadingTransactionResult.NONE
				&& newResult != loadingTransactionResult)
			{
				// Someone wants to change the result.  Allow changing from success to fail, but not the other way
				// around.
				if (loadingTransactionResult != LoadingTransactionResult.SUCCESS)
				{
					Debug.LogWarningFormat("Attempt to change loadingTransactionResult from {0} to {1}", loadingTransactionResult.ToString(), newResult.ToString());
				}
				else
				{
					loadingTransactionResult = newResult;	
				}
			}
			else
			{
				loadingTransactionResult = newResult;
			}
			
			if(loadingTransactionResult==LoadingTransactionResult.FAIL)
			{
				Debug.LogWarningFormat("loadingTransactionResult: {0}", loadingTransactionResult.ToString());
			}
		}
		else
		{
			Debug.LogWarning("Attempt to hide Loading screen without an active loading transaction");
		}

		resetTimer = null;
		actualProgress = 1f;
		shouldHide = true;
		hideCallback = callback;
		setDownloadingStatus(false);
		isLoggingIn = false;
	}
	
	private void beginLoadingTransaction(LoadingTransactionTarget target)
	{
		if (isTransactionActive)
		{
			Debug.LogWarning("Loading transaction beginning twice! old: " + loadingTarget.ToString() + " new: " + target.ToString());
			return;
		}
		
		if (target == LoadingTransactionTarget.NONE)
		{
			Debug.LogWarning("Attempt to begin loading transaction with invalid target (NONE)");
			return;
		}
		
		loadingTarget = target;
		setTransactionName(loadingTarget);
		Userflows.flowStart(transactionName);
		Userflows.addExtraFieldToFlow(transactionName, "lobby", LobbyInfo.currentTypeToString);
		isTransactionActive = true;
		loadingTransactionResult = LoadingTransactionResult.NONE;
	}

	private void endLoadingTransaction()
	{
		if (!isTransactionActive)
		{
			Debug.LogWarning("Attempting to end (success) transaction not in progress! " + loadingTarget.ToString());
			return;
		}
		
		bool isFirstLogin = false;
		// Add extra parameter to mark if we were downloading bundles or not
		if (didDownloadBundles)
		{
			Dictionary<string, string> extraFields = new Dictionary<string, string>();
			extraFields.Add("downloaded_bundles", "true");
			extraFields.Add("downloaded_bundles_count", downloadBundlesCount.ToString());
			string installDateTimeString = SlotsPlayer.getPreferences().GetString(Prefs.FIRST_APP_START_TIME, null);
			isFirstLogin = string.IsNullOrEmpty(installDateTimeString) || NotificationManager.DayZero;
			if (isFirstLogin)
			{
				extraFields.Add("new_user", "true");
			}
			Userflows.addExtraFieldsToFlow(transactionName, extraFields);
		}
		
		Userflows.Userflow userflow = Userflows.flowEnd(transactionName);
		
		string log = transactionName + " Load Time: " + userflow.duration + "\n";
		loadingUserflowString += log;

		if (transactionName == FLOW_LOADING_LOBBY_LOGIN)
		{
			Userflows.finishedInitialLoading();
#if UNITY_WEBGL
			// We are ready for the player to interact with the game.
			Application.ExternalEval("window.endLoadingSlideShow()");
			Application.ExternalEval("window.initPredownloader()");
#endif

			// report this device info via splunk (we wait until now so we don't lock in an initial anon ZID)
			StatsManager.Instance.reportDeviceInfo();
			StatsManager.Instance.LogCount(counterName: "game_actions", kingdom: isFirstLogin ? "first_load_lobby" : "load_lobby", phylum: userflow.duration + "");
		}

		clearTransaction();
	}

	private void failLoadingTransaction(string reason)
	{
		if (!isTransactionActive)
		{
			Debug.LogWarning("Attempting to end (fail) transaction not in progress! " + loadingTarget.ToString());
			return;
		}

		// Add extra parameter to mark if we were downloading bundles or not
		if (didDownloadBundles)
		{
			Dictionary<string, string> extraFields = new Dictionary<string, string>();
			extraFields.Add("downloaded_bundles", "true");
			Userflows.addExtraFieldsToFlow(transactionName, extraFields);
		}

		Userflows.flowEnd(transactionName, false, reason);
		clearTransaction();
	}

	private void setTransactionName(LoadingTransactionTarget target)
	{
		switch (target)
		{
			case LoadingTransactionTarget.LOBBY:
				transactionName = FLOW_LOADING_LOBBY;
				break;
			case LoadingTransactionTarget.LOBBY_LOGIN:
				transactionName = FLOW_LOADING_LOBBY_LOGIN;
				break;
			case LoadingTransactionTarget.GAME:
				if (GameState.game != null)
				{
					transactionName = FLOW_LOADING_GAME_PREFIX + GameState.game.keyName;
				}
				else
				{
					Debug.LogWarning("Loading transaction to game starting before game is set");
					transactionName = FLOW_LOADING_GAME_PREFIX + "unknown";
				}
				break;
			case LoadingTransactionTarget.SLOTVENTURE_LOBBY:
				transactionName = FLOW_LOADING_SV_LOBBY_PREFIX + SlotventuresLobby.assetData.themeName;
				break;
			default:
				transactionName = "loading-unknown";
				break;
		}
	}

	private void clearTransaction()
	{
		isTransactionActive = false;
		loadingTransactionResult = LoadingTransactionResult.NONE;
		transactionName = null;
	}

	// This is an overridable coroutine just in case we want to do some kind of animation
	// or something after loading is finished, but before hiding the loading screen.
	protected virtual IEnumerator displayedProgressFinished()
	{
		isHiding = true;
		
		//Give the progress meter one extra frame to update itself
		yield return null;
		
		finishHiding();
	}
	
	protected virtual void finishHiding()
	{
		if (Overlay.instance != null)
		{
			// Make sure the overlay positioning is up to date,
			// to prevent weird race conditions with NGUI elements like UIAnchor upon Awake.
			Overlay.instance.top.adjustForResolution();
		}
		
		setAlpha(0f);
		
		if (hideCallback != null)
		{
			hideCallback();
			hideCallback = null;
		}
		
		// This function can potentially be spammed, make sure this stuff only happens once
		if (isLoading)
		{
			isLoading = false;
		
			// See if anything was waiting to happen while loading.
			Scheduler.run();
		
			Bugsnag.LeaveBreadcrumb("Loading Screen is hidden now");

			// Time to load an actual game scene
			if (GameState.game != null)
			{
				//StatsManager.Instance.LogCount("timing", "GameLoadComplete", StatsManager.getGameTheme(), StatsManager.getGameName(), "", "" , StatsManager.getTime(false));
				Debug.LogFormat("GAME LOAD TIME: {0} seconds", StatsManager.getTime(false).ToString());
			}
			else
			{
				Debug.LogFormat("Initial time to lobby LOAD TIME: {0} seconds", StatsManager.getTime(false).ToString());
			}
		}
		
		if (isTransactionActive)
		{
			// Result of SUCCESS or NONE (finished without anyone calling hide()) are successful.
			if (loadingTransactionResult != LoadingTransactionResult.FAIL)
			{
				endLoadingTransaction();
			}
			else
			{
				failLoadingTransaction("failed");
			}
		}
		
		isHiding = false;
	}
	
	public virtual void setDownloadingStatus(bool downloading, string loadingMessage = "")
	{
		if (!downloading || !Data.isGlobalDataSet)
		{
			return;
		}

		// Only localize after the global data has been loaded,
		// which means the very first loading screen will always say "Loading" in English.
		if (loadingMessage == "")
		{
			loadingMessage = Localize.text("downloading_game");
		}
		
		Debug.LogWarning("loadingMessage: " + loadingMessage);

		if (instance == null)
		{
			Debug.LogWarning("Critical reference missing Loading.instance");
		}
		else
		{
			if (instance.loadingLabel == null)
			{
				Debug.LogError("Loading Labels are null");
			}
			else
			{
				instance.loadingLabel.text = loadingMessage;
			}
		}
	
	}
	
	// Do Loading screen-related stuff when the game is paused or resumed.	
	public void pauseHandler(bool isPaused)
	{
		if (!isPaused && isLoggingIn)
		{
			// If returning to the game after seeing the facebook login screen,
			// set a timeout to automatically reset the game just in
			// case the login process was aborted or something, so we don't get
			// stuck on the loading screen forever.
			// Since the loading screen is deactivated when hidden,
			// we can't use itself to host the coroutine, so use RoutineRunner.
			resetTimer = new GameTimer(LOGIN_TIMEOUT_SECONDS);
			isLoggingIn = false;
		}
	}

	/// <summary>
	/// User logged in succesfully, do not keep this timer running it actually freezes the login screen
	/// </summary>
	public void clearResetTimer()
	{
		resetTimer = null;
	}

	public enum LoadingTransactionTarget
	{
		NONE,
		LOBBY_LOGIN,
		LOBBY,
		GAME,
		SLOTVENTURE_LOBBY
	}

	public enum LoadingTransactionResult
	{
		NONE,
		SUCCESS,
		FAIL
	}
}
