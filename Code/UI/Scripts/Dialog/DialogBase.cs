using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using CustomLog;

/**
A general-purpose script that goes on every dialog root object.
*/
public abstract class DialogBase : TICoroutineMonoBehaviour
{	
	public Transform sizer;		/// Used for animating size as it appears.

	public delegate void AnswerDelegate(Dict answerArgs);	/// args should have a key named "answer" for a basic answer value.
	public AnswerDelegate answerDelegate = null;

	public delegate void CloseDelegate(Dict closeArgs);
	public CloseDelegate closeDelegate = null;

	public ClickHandler closeButtonHandler;

	[Tooltip("The aspect ratio this dialog was designed for in landscape, used to resize the dialog if it has to be displayed in portrait.")]
	[SerializeField] private float baseLandscapeAspectRatio = DEFAULT_LANDSCAPE_IPAD_ASPECT_RATIO;
	
	[System.NonSerialized] public Dict dialogArgs = null;
	[System.NonSerialized] public DialogType type = null;
	[System.NonSerialized] public Bounds bounds;
	
	[System.NonSerialized] public string economyTrackingName = ""; // Use this name for economy tracking (it defaults to the dialog type in Start, but PMs may want something different).

	[System.NonSerialized] public string userflowKey = ""; // The key that this dialog uses for userflows.
	
	private float idleTime = 0;				/// The system time used for auto-closing after a certain amount of time if necessary.
	private bool didCancelAutoClose = false;/// Set to true if auto-close should no longer happen.

	private bool isTweening = false;

	public abstract void init();		/// Do initial localization, etc. after dialogArgs are assigned. Wait for onFadeInComplete to start animations or other fancy stuff.
	public abstract void close();		/// Force each dialog script to implement a close() method do the Dialog static class can call it.

	protected Texture2D[] downloadedTextures = null;	// Only non-null if a dialog pre-downloaded textures before showing.

	protected Vector3 baseDialogObjectScale = Vector3.one;

	[System.NonSerialized] public bool dialogBaseHasBeenClosed;    // so it can't be queued up if already closing
	[System.NonSerialized] public bool dialogBaseHasBeenOpened;    // so it can't be queued up if already opening

	[System.NonSerialized] public long winAmount = 0;

	private const float DEFAULT_LANDSCAPE_IPAD_ASPECT_RATIO = 4.0f / 3.0f;

	public long startingCreditAmount { get; private set; }
	
	[System.NonSerialized] public DialogTask task;	//Task used to open this dialog. Needed for proper closing in case dialogs with the same key are scheduled


	// UI Designer Jackson Wang decided not to use the "touch outside the box to close it" technique, so it's commented out.
//	public abstract void touchShroud();	/// Force each dialog to implement what happens when tapping outside the dialog. Called by NGUI callback when touching the backmask.
	
	protected virtual void Start()
	{
		baseDialogObjectScale = this.gameObject.transform.localScale;

		adjustScaleForScreenOrientation();
	
		// Use Start() instead of Awake() for starting the fade in effect,
		// so the dialog has already had a chance to initialize everything first.
		startFadeIn();
		
		if (economyTrackingName == "")
		{
			economyTrackingName = GetType().ToString();
		}

		if (closeButtonHandler != null)
		{
			closeButtonHandler.registerEventDelegate(onCloseButtonClicked);
		}
	}

	public virtual void onCloseButtonClicked(Dict args = null)
	{
		Dialog.close(this);
	}

#if ZYNGA_TRAMP || UNITY_EDITOR
	public virtual IEnumerator automate()
	{
		while (this != null &&  Dialog.instance.currentDialog == this && !Dialog.instance.isClosing)
		{
			// If this dialog has its close button hooked up in the base class then use that.
			if (closeButtonHandler != null)
			{
				yield return CommonAutomation.routineRunnerBehaviour.StartCoroutine(CommonAutomation.clickRandomColliderIn(closeButtonHandler.gameObject));
			}
			else
			{
				// Handles the automation of the dialog.
				GameObject closeButton = tryToFindCloseButton();

				if (closeButton != null)
				{
					// If there is a close button click it.
					yield return CommonAutomation.routineRunnerBehaviour.StartCoroutine(CommonAutomation.clickRandomColliderIn(closeButton));
				}
				else
				{
					// Otherwise just click randomly.
					yield return CommonAutomation.routineRunnerBehaviour.StartCoroutine(CommonAutomation.clickRandomColliderIn(gameObject));
				}
			}

		}
	}

	// Will return null if a close button is not detected by its checks
	// Used to simulate what automate function will try to click, so that a debug
	// menu option can be used to determine what will be pressed when trying to automate closing a specific dialog
	// (and deduce why a dialog isn't auto closing as expected)
	public GameObject getCloseButtonGameObjectForDialog()
	{
		if (closeButtonHandler != null)
		{
			return closeButtonHandler.gameObject;
		}
		else
		{
			return tryToFindCloseButton();
		}
	}

	protected GameObject tryToFindCloseButton()
	{
		GameObject close = CommonGameObject.findChild(gameObject, "Close Button", false);
		if (close == null)
		{
			close = CommonGameObject.findChild(gameObject, "Close", false);
		}
		if (close == null)
		{
			close = CommonGameObject.findChild(gameObject, "Close Btn", false);
		}
		if (close == null)
		{
			close = CommonGameObject.findChild(gameObject, "Button Close", false);
		}		
		return close;
	}
#endif // ZYNGA_TRAMP || UNITY_EDITOR
	
	public void setArgs(Dict args)
	{
		dialogArgs = args;
		
		downloadedTextures = dialogArgs.getWithDefault(D.DOWNLOADED_TEXTURES, null) as Texture2D[];
	}

	protected void startFadeIn()
	{
		startingCreditAmount = SlotsPlayer.creditAmount;
		NGUIExt.disableAllMouseInput();

		playOpenSound();

		if (MobileUIUtil.isSlowDevice)
		{
			// Don't slide it in on slow devices. Just put it in the center of the screen immediately.
			CommonTransform.setY(sizer.transform, 0);	// Hopefully all dialogs are created with this at 0 already, but just in case.
			doStuffAfterFadeIn();
		}
		else
		{
			float animTime = animateIn();
			StartCoroutine(delayFadeInComplete(animTime));
		}
		
		resetIdle();
	}

	private IEnumerator delayFadeInComplete(float time)
	{
		yield return new WaitForSeconds(time);

		// Need to verify the tween has finished because it is sometimes lasting longer than the given wait time
		// Checking isOpening to verify we don't try to double call doStuffAfterFadeIn
		if (!isTweening && Dialog.instance.isOpening)
		{
			doStuffAfterFadeIn();
		}
	}

	public virtual float animateOut()
	{
		if (type == null || sizer == null || sizer != null && sizer.gameObject == null)
		{
			return Dialog.animOutTime;
		}

		// Animate away the dialog before actually closing it and removing it from the stack.
		Vector3 moveGoal = Dialog.getAnimPos(type.getAnimOutPos(), sizer.gameObject);
		Vector3 scaleGoal = Dialog.getAnimScale(type.getAnimOutScale());
		iTween.EaseType easeType = Dialog.getAnimEaseType(type.getAnimOutEase(), false);
		float time = type.getAnimOutTime();

		// Always do both animations, to guarantee that the oncomplete stuff happens.
		iTween.MoveTo(sizer.gameObject, iTween.Hash("position", moveGoal, "time", time, "islocal", true, "oncompletetarget", gameObject, "easetype", easeType));
		iTween.ScaleTo(sizer.gameObject, iTween.Hash("scale", scaleGoal, "time", time, "islocal", true, "easetype", easeType));

		return time;
	}

	public virtual float animateIn()
	{
		// Jackson Wang wants to slide it in right from the top.
		// Set starting scale and position before animating into view.
		sizer.transform.localPosition = Dialog.getAnimPos(type.getAnimInPos(), sizer.gameObject);

		float time = type.getAnimInTime();

		// Always do both animations, to guarantee that the oncomplete stuff happens.
		// Add a little delay to this tween, to give time for the dialog UI elements to initialize before animating, so the animation is smoother.
		iTween.EaseType easeType = Dialog.getAnimEaseType(type.getAnimInEase(), true);

		isTweening = true;
		iTween.MoveTo(sizer.gameObject, iTween.Hash("position", Vector3.zero, "time", time, "islocal", true, "oncompletetarget", gameObject, "oncomplete", "tweenComplete", "delay", .1f, "easetype", easeType));

		float totalTime = time + 0.1f; //add in the delay

		if (sizer.GetComponent<AspectRatioScaler>() != null)
		{
			// clearly we are scaling to a different aspect ratio, and dialog base should not be
			// trying to scale anything!!

			AspectRatioScaler scaler = sizer.GetComponent<AspectRatioScaler>();
			if (scaler.goalAspectRatio <= AspectRatioScaler.IPAD_ASPECT)
			{
				sizer.transform.localScale = Dialog.getAnimScale(type.getAnimInScale());
				iTween.ScaleTo(sizer.gameObject, iTween.Hash("scale", Vector3.one, "time", time, "islocal", true, "delay", .1f, "easetype", easeType));
			}
		}
		else
		{
			sizer.transform.localScale = Dialog.getAnimScale(type.getAnimInScale());
			iTween.ScaleTo(sizer.gameObject, iTween.Hash("scale", Vector3.one, "time", time, "islocal", true, "delay", .1f, "easetype", easeType));
		}

		return totalTime;
	}

	private void tweenComplete()
	{
		isTweening = false;
		doStuffAfterFadeIn();
	}
	
	private void doStuffAfterFadeIn()
	{
		isTweening = false;
		onFadeInComplete();
		
		NGUIExt.enableAllMouseInput();

		// Don't make this part of onFadeInComplete() because we need to make sure it is
		// called after any overridden onFadeInComplete() calls,
		// just in case there's a Dialog.close() call in there.
		if (Dialog.instance.closingQueue.Count > 0)
		{
			Dialog.instance.closeQueuedDialog();
		}
		else if (Scheduler.hasTask)
		{
			Scheduler.run();
		}
	}
		
	/// Override this in subclasses to start doing things after the fade in is done.
	protected virtual void onFadeInComplete()
	{
		Dialog.instance.isOpening = false;
	}

	/// Used to hide this and all GameObjects within it.
	public void hide()
	{
		enabled = false;	// Don't let processing happen while hidden, especially Android back button.
		if (isTweening)
		{
			iTween.Stop(gameObject);
		}
		onHide();
		CommonTransform.setY(transform, 10000);
		// Also need to disable any extra cameras on this dialog,
		// which are usually used for overlay effects.
		Camera[] cameras = GetComponentsInChildren<Camera>();
		Camera cam;
		for (int i = 0; i < cameras.Length; i++)
		{
			cam = cameras[i];
			if (cam != null && cam.enabled)
			{
				cam.enabled = false;
				hiddenCams.Add(cam);
			}
		}
	}

    protected virtual void onHide()
	{
		// Should be overriden to do custom hide stuff.
	}
	
	private List<Camera> hiddenCams = new List<Camera>();
	
	/// Used to show this and all GameObjects within it.
	public virtual void show()
	{
		CommonTransform.setY(transform, 0);
		// Re-enable all disabled cameras.
		foreach (Camera cam in hiddenCams)
		{
			cam.enabled = true;
		}
		hiddenCams.Clear();
		enabled = true;
		onShow();		
	}

    protected virtual void onShow()
	{
		// Should be overriden to do custom hide stuff.
	}	
	
	/// Resets the idle time if any kind of interaction happened.
	/// Each dialog that uses shouldAutoClose should call this whenever
	/// any kind of interaction is made, so the dialog doesn't close
	/// in the middle of the user doing something like typing a message to share.
	public void resetIdle()
	{
		idleTime = Time.realtimeSinceStartup;		
	}
	
	/// Dialogs that implement auto-close should call this as soon as they start closing the dialog,
	/// to prevent auto-close from trying to close it again.
	public void cancelAutoClose()
	{
		didCancelAutoClose = true;
	}
	
	/// Returns whether the dialog should close after a period of time if auto-spin is active.
	/// It's up to each dialog that implements this to actually close the dialog,
	/// just in case there is special code that needs to be called as part of the closing.
	protected bool shouldAutoClose
	{
		get
		{
			return (
				!didCancelAutoClose &&
				!Dialog.instance.isClosing &&
				!Dialog.instance.isOpening &&
				Dialog.instance.currentDialog == this &&
				SlotBaseGame.instance != null &&
			 	SlotBaseGame.instance.hasAutoSpinsRemaining &&
				Glb.MOBILE_AUTO_CLOSE_DIALOG_SECONDS > 0 &&
				Time.realtimeSinceStartup - idleTime >= Glb.MOBILE_AUTO_CLOSE_DIALOG_SECONDS
			);
		}
	}

	// Dialogs that make purchases may override this method to receive success notification
	// of the purchase, just in case something special needs to be cleaned up or whatever.
	// Returns whether this dialog should be closed after a successful purchase.
	public enum PurchaseSuccessActionType
	{
		closeDialog,
		skipThankYouDialog,
		leaveDialogOpenAndShowThankYouDialog
	};
	public virtual PurchaseSuccessActionType purchaseSucceeded(JSON data, PurchaseFeatureData.Type purchaseType)
	{
		return PurchaseSuccessActionType.closeDialog;
	}

	// Dialogs that make purchases may override this method to receive failure notification
	// of the purchase, just in case something special needs to be cleaned up or whatever.
	public virtual void purchaseFailed(bool timedOut)
	{
	}

	// Dialogs that make purchases may override this method to receive cancellation notification
	// of the purchase, just in case something special needs to be cleaned up or whatever.
	public virtual void purchaseCancelled()
	{
	}

	// Returns the pre-downloaded texture of the given index, or null if not found (maybe the download failed).
	protected Texture2D getDownloadedTexture(int index)
	{
		if (downloadedTextures == null || index >= downloadedTextures.Length || index < 0)
		{
			return null;
		}
		return downloadedTextures[index];
	}
	
	// Handle nullchecking and applying a texture to a UITexture.
	// Returns true if the texture was successfully downloaded, just in case it's useful info.
	protected bool downloadedTextureToUITexture(UITexture uiTexture, int index, bool useDefaultMaterial = false)
	{
		Texture2D tex = getDownloadedTexture(index);
		if (tex == null)
		{
			return false;
		}
		NGUIExt.applyUITexture(uiTexture, tex, useDefaultMaterial);
		return true;
	}

	// Handle nullchecking and applying a texture to a Renderer.
	// Returns true if the texture was successfully downloaded, just in case it's useful info.
	protected bool downloadedTextureToRenderer(Renderer imageRenderer, int index)
	{
		Texture2D tex = getDownloadedTexture(index);
		return textureToRenderer(imageRenderer, tex);
	}
	
	protected bool textureToRenderer(Renderer imageRenderer, Texture2D tex)
	{
		if (imageRenderer == null || tex == null)
		{
			return false;
		}

		Material mat = DisplayAsset.getNewRendererMaterial(imageRenderer);
		mat.mainTexture = tex;
		imageRenderer.sharedMaterial = mat;
		return true;
	}

	public virtual void unloadSourceAssetBundle()
	{
		if (!type.shouldUnloadBundleOnClose ||
		    AssetBundleManager.isResourceInInitializationBundle(type.dialogPrefabPath))
		{
			return;
		}
		
		string assetBundleName = AssetBundleManager.getBundleNameForResource(type.dialogPrefabPath);
		AssetBundleManager.unloadBundle(assetBundleName);
	}
	
	// Automatically called by Dialog when a dialog is closed, to clean up downloaded textures.
	public virtual void destroyDownloadedTextures()
	{
		if (downloadedTextures == null)
		{
			return;
		}
		
		foreach (Texture2D tex in downloadedTextures)
		{
			Dialog.instance.destroyStreamedNonLobbyOptionTexture(tex);
		}
		
		downloadedTextures = null;
	}
	
	// Allow the dialog to adjust scale based on what the screen orientation is
	// if in landscape it is what it is normally set to, in Portrait it will be
	// uniformly scaled down.  This function is also virtual if you don't want
	// to do a straight uniform scale down of the root dialog object.
	protected virtual void adjustScaleForScreenOrientation()
	{
		// Perform calculation based upon aspect ratio, resizing the screen appropriately
		Camera targetCamera = this.gameObject.GetComponent<Camera>();
		float currentAspectRatio = (float)Screen.width / (float)Screen.height;
		
		if (ResolutionChangeHandler.isInPortraitMode)
		{
			float adjustmentMultiplier = currentAspectRatio / baseLandscapeAspectRatio;
			this.gameObject.transform.localScale = new Vector3(
				baseDialogObjectScale.x * adjustmentMultiplier,
				baseDialogObjectScale.y * adjustmentMultiplier,
				baseDialogObjectScale.z
				);
		}
		else
		{
			this.gameObject.transform.localScale = baseDialogObjectScale;
		}
	}
	
	// Any dialog may implement this if its needs to.
	public virtual void resolutionChangeHandler()
	{
		adjustScaleForScreenOrientation();
	}

	//Helper functions for tracking basic stat calls for dialogs, including, view, click etc.
	protected const string TRACK_COUNTER = "dialog";
	private const string TRACK_KINGDOM = "simple_msg";

	protected const string TRACK_CLASS_CLICK = "click";
	protected const string TRACK_CLASS_VIEW = "view";

	protected const string TRACK_FAM_OKAY = "okay"; 
	protected const string TRACK_FAM_CLOSE = "close";
	protected const string TRACK_FAM_LOBBY = "lobby";
	protected const string TRACK_FAM_PLAY = "play";

	private void track(string phylum, string klass, string family = "") 
	{
		StatsManager.Instance.LogCount(TRACK_COUNTER, TRACK_KINGDOM, phylum, klass, family);
	}

	protected void trackView(string phylum) 
	{
		track(phylum, TRACK_CLASS_VIEW);
	}

	protected void trackClose(string phylum) 
	{
		track(phylum, TRACK_CLASS_CLICK, TRACK_FAM_CLOSE);
	}

	protected void trackOkay(string phylum) 
	{
		track(phylum, TRACK_CLASS_CLICK, TRACK_FAM_OKAY);
	}

	protected void trackLobby(string phylum) 
	{
		track(phylum, TRACK_CLASS_CLICK, TRACK_FAM_LOBBY);
	}

	protected void trackPlay(string phylum) 
	{
		track(phylum, TRACK_CLASS_CLICK, TRACK_FAM_PLAY);
	}
	
	// Automatically play an open sound for every dialog.
	// If a dialog needs a different or no sound,
	// then override this method.
	protected virtual void playOpenSound()
	{
		Audio.play("minimenuopen0");
	}

	// Automatically play a close sound for every dialog.
	// If a dialog needs a different or no sound,
	// then override this method.
	public virtual void playCloseSound()
	{
		Audio.play("minimenuclose0");
	}

	public static void playAudioFromEos(string audioName = "")
	{
		if (!string.IsNullOrEmpty(audioName))
		{
			Audio.playAudioFromURL(audioName);
		}
	}
}
