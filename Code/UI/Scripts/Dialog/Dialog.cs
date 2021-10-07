using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

/*
Class for handling popup dialog related stuff.
*/

public class Dialog : TICoroutineMonoBehaviour, IResetGame
{
	public enum AnimPos
	{
		CENTER,
		TOP,
		BOTTOM,
		LEFT,
		RIGHT,
		COUNT		// Not a valid value, must be last to indicate how many values there are.
	}
	
	public enum AnimScale
	{
		SMALL,
		FULL,
		COUNT		// Not a valid value, must be last to indicate how many values there are.
	}
	
	// A practical subset of the iTween ease types.
	public enum AnimEase
	{
		BACK,
		BOUNCE,
		SMOOTH,
		ELASTIC,
		COUNT		// Not a valid value, must be last to indicate how many values there are.
	}

	public ShroudScript shroud = null;
	public GameObject keyLight = null;

	public UICamera uiCamera;
	public Camera unityCamera;

	[System.NonSerialized] public DialogBase currentDialog = null;	/// When a dialog is displayed, holds the current dialog's script reference for showing/hiding.	
	[System.NonSerialized] public bool isClosing = false; /// Prevent double-closing call
	[System.NonSerialized] public string lastclosedDialogKey;
	[System.NonSerialized] public bool isOpening = false; /// Prevent double-opening calls.
	[System.NonSerialized] public string lastOpenedDialogKey;
	
	private List<DialogBase> dialogStack = new List<DialogBase>();		// The current dialog is always the last one in the list.
	private bool shroudActive = false;

	public List<DialogBase> closingQueue = new List<DialogBase>();	// Queued up dialogs to close by specific dialog instances.

	// A Dialog-global list of textures that were streamed and need to be cleaned up when closing the dialog that requested it.
	// This lets us keep a counter of requests so that it doesn't get destroyed until the last dialog that requested it
	// has finally been closed.
	private Dictionary<Texture2D, StreamedTexture> streamedTextures = new Dictionary<Texture2D, StreamedTexture>();
	
	public const float SHROUD_FADE_ALPHA = .87f;
	private const int MAX_DIALOG_CLONE_COUNT = 10;				// The maximum number of the same dialog we support opening at once.

	public static float animInTime = 0.25f;						// Animation duration when animating in.
	public static float animOutTime = 0.25f;					// Animation duration when animating out.
	public static AnimPos animInPos = AnimPos.TOP;				// The starting position of the dialog when animating in.
	public static AnimPos animOutPos = AnimPos.BOTTOM;			// The ending position of the dialog when animating out.
	public static AnimScale animInScale = AnimScale.FULL;		// The starting scale of the dialog when animating in.
	public static AnimScale animOutScale = AnimScale.FULL;		// The ending scale of the dialog when animating out.
	public static AnimEase animInEase = AnimEase.BACK;			// Ease type when animating in.
	public static AnimEase animOutEase = AnimEase.BACK;			// Ease type when animating out.

	public static Dialog instance = null;
	protected static string backgroundMusicRestoreKey = "";
	private Collider shroudCollider = new Collider();

	public bool isShowing
	{
		get { return currentDialog != null; }
	}

	public bool isBusy
	{
		get
		{
			{
				return isShowing || isClosing || isOpening;
			}
		}
	}

	/// <summary>
	/// Returns true if a dialog is opening or closing
	/// </summary>
	public static bool isTransitioning
	{
		get
		{
			if (instance != null)
			{
				return instance.isClosing || instance.isOpening;
			}

			return false;
		}
	}
	
#if UNITY_EDITOR
	//Reset open/closing tags.  This will not stop the dialog that is running.  Use for debug purposes only
	public static void forceTransitionFinished()
	{
		if (instance != null)
		{
			instance.isClosing = false;
			instance.isOpening = false;
		}
	}
#endif

	public void resetShroud()
	{
		shroud.updateFade(0);
		shroudCollider.enabled = false;
		toggleDialogCamera(false);
	}

	public static void setTransitionData(DialogTransitionExperiment exp)
	{
		if (exp == null || !exp.isInExperiment)
		{
			return;
		}

		animInTime = exp.animInTime;
		animOutTime = exp.animOutTime;
		animInPos = exp.slideInFrom;
		animInScale = exp.scaleInFrom;
		animInEase = exp.animInEaseType;
		animOutPos = exp.slideOutTo;
		animOutScale = exp.scaleOutTo;
		animOutEase = exp.animOutEaseType;
	}
	
	void Awake()
	{
		instance = this;
		shroud.gameObject.SetActive(true);	// We keep it deactivated in the editor so it's not a huge black thing to work around.
		shroud.sprite.alpha = 0;
		shroudCollider = shroud.GetComponent<Collider>();
		shroudCollider.enabled = false;

		currentDialog = null;
		unityCamera = GetComponentInParent<Camera>();
		uiCamera = GetComponentInParent<UICamera>();

		toggleDialogCamera(false);
	}
	
	/// A method to close the current dialog with a given response.
	/// Optionally specify which dialog in the stack to close, rather than the topmost automatically.
	public static void close(DialogBase whatDialog = null)
	{
		// Use a static version of the method for convenience.
		instance.closeMe(whatDialog);
	}

	public static void immediateClose(DialogBase whatDialog)
	{
		instance.closeMe(whatDialog);
		Scheduler.removeTask(whatDialog.task);
		instance.isOpening = false;
	}

	public static bool isDialogShowing
	{
		get
		{
			return (instance != null && instance.currentDialog != null);
		}
	}

	public static bool isSpecifiedDialogShowing(string dialogKey)
	{
		return instance != null && instance.currentDialog != null && instance.currentDialog.type.keyName == dialogKey;
	}
	
	private void closeMe(DialogBase whatDialog = null)
	{
		if (whatDialog == null)
		{
			whatDialog = currentDialog;
		}

		if (whatDialog == null)
		{
			// If no dialog is on the stack to close, then someone probably called Dialog.close() too many times.
			// See if the Scheduler has anything left to do so it doesn't get stuck.
			Debug.LogWarning("Dialog::closeMe - Close called when there was no dialog on the stack to close. Find and eliminate the extra call to Dialog.close().");
			Scheduler.run();
			return;
		}

		if ((isOpening && !whatDialog.dialogBaseHasBeenOpened) || (isClosing && !whatDialog.dialogBaseHasBeenClosed)) 
		{
			// Another dialog is already closing, so queue up this close call.
			closingQueue.Add(whatDialog);

			Debug.Log(string.Format("Dialog::closeMe - Queueing. Closing: {0} Opening: {1} Close Count: {2} Stack: {3}", isClosing.ToString(), isOpening.ToString(), closingQueue.Count, dialogStack.Count));
			return;
		}	

		if (whatDialog.dialogBaseHasBeenClosed)
		{
			// the dialog is probably calling close during update but the tween isn't done yet so just return
			return;
		}		

		whatDialog.dialogBaseHasBeenClosed = true;

		isClosing = true;
		lastclosedDialogKey = whatDialog.type.keyName;
		NGUIExt.disableAllMouseInput();
		
		string msg = string.Format("Dialog::Close - Closing Dialog: {0} Stack: {1}", whatDialog.GetType().ToString(), dialogStack.Count);
		Bugsnag.LeaveBreadcrumb(msg);
						
		if (MobileUIUtil.isSlowDevice || whatDialog != currentDialog)
		{
			// This must be called AFTER the check for dialogStack.Count for turning off the shroud.
			// Delay the finishClosing call for just a tiny amount of time so that taps don't process for stuff coming in behind
			StartCoroutine(slowDeviceDelayedFinishClosing(whatDialog));
		}
		else
		{
			float animTime = whatDialog.animateOut();
			StartCoroutine(delayFinishClosing(animTime, whatDialog));
		}
	}

	private void fadeShroud(DialogBase whatDialog)
	{
		if (dialogStack.Count == 0)
		{
			if (MobileUIUtil.isSlowDevice)
			{
				// This was the last dialog in the stack, so hide the shroud too.
				shroud.updateFade(0);
				shroudCollider.enabled = false;
				toggleDialogCamera(false);
			}
			else
			{
				float animTime = whatDialog.animateOut();
				// This was the last dialog in the stack, so fade out the shroud too.
				iTween.ValueTo(shroud.gameObject, iTween.Hash("from", shroud.sprite.alpha, "to", 0f, "time", animTime, "onupdate", "updateFade","oncompletetarget", gameObject, "oncomplete", "toggleDialogCamera", "oncompleteparams", false));
				shroudCollider.enabled = false;
			}

			//Unload the pet from memory once we're done showing all the dialogs on the stack
			if (VirtualPetController.instance != null && VirtualPetController.instance.currentPet != null)
			{
				VirtualPetController.instance.unloadPet();
			}
		}
	}

	private IEnumerator delayFinishClosing(float time, DialogBase whatDialog)
	{
		yield return new WaitForSeconds(time);
		finishClosing(whatDialog);
		fadeShroud(whatDialog);
	}

	public void toggleDialogCamera(bool isEnabled)
	{
		if (isOpening && !isEnabled)
		{
			return;
		}

		// Disable dialog camera
		if (uiCamera != null)
		{
			uiCamera.enabled = isEnabled;
		}
		if (unityCamera != null)
		{
			unityCamera.enabled = isEnabled;
		}
	}
	
	public static Vector3 getAnimScale(AnimScale scaleEnum)
	{
		float scale = 1.0f;	// Since x and y scale the same, use a single float instead of Vector3 here.
				
		switch (scaleEnum)
		{
			case Dialog.AnimScale.FULL:
				// It's full size by default, so no change.
				break;
			case Dialog.AnimScale.SMALL:
				scale = 0.01f;
				break;
		}	

		return new Vector3(scale, scale, 1.0f);
	}
	
	public static Vector3 getAnimPos(AnimPos posEnum, GameObject dialogGameObject)
	{
		Vector3 pos = Vector3.zero;

		switch (posEnum)
		{
			case Dialog.AnimPos.CENTER:
				// It's centered by default, so no change.
				break;
			case Dialog.AnimPos.TOP:
				pos.y = NGUIExt.effectiveScreenHeight;
				break;
			case Dialog.AnimPos.BOTTOM:
				pos.y = -NGUIExt.effectiveScreenHeight;
				break;
			case Dialog.AnimPos.LEFT:
				pos.x = -NGUIExt.effectiveScreenWidth;
				break;
			case Dialog.AnimPos.RIGHT:
				pos.x = NGUIExt.effectiveScreenWidth;
				break;
		}

		return pos;
	}
	
	public static iTween.EaseType getAnimEaseType(AnimEase ease, bool isSlidingIn)
	{
		if (isSlidingIn)
		{
			switch (ease)
			{
				case AnimEase.BACK:
					return iTween.EaseType.easeOutBack;
				case AnimEase.BOUNCE:
					return iTween.EaseType.easeOutBounce;
				case AnimEase.SMOOTH:
					return iTween.EaseType.easeOutCubic;
				case AnimEase.ELASTIC:
					return iTween.EaseType.easeOutElastic;
			}
		}
		else
		{
			switch (ease)
			{
				case AnimEase.BACK:
					return iTween.EaseType.easeInBack;
				case AnimEase.BOUNCE:
					return iTween.EaseType.easeOutBounce;	// Intentionally "Out", since "In" looks horrible here.
				case AnimEase.SMOOTH:
					return iTween.EaseType.easeInCubic;
				case AnimEase.ELASTIC:
					return iTween.EaseType.easeInElastic;
			}
		}
		// Should never get here, but we need a return value outside of the switch statement, for the compiler.
		return iTween.EaseType.linear;
	}

	/// Wait for a frame before finishing the dialog close 
	// so that taps don't trigger on stuff behind the dialog like Big Win animations for instance
	private IEnumerator slowDeviceDelayedFinishClosing(DialogBase whatDialog)
	{
		Bugsnag.LeaveBreadcrumb("Dialog.slowDeviceDelayedFinishClosing()");
		yield return null;
		finishClosing(whatDialog);
		fadeShroud(whatDialog);
	}
	
	private void finishClosing(DialogBase whatDialog)
	{
		if (whatDialog == null)
		{
			Debug.LogWarning("Dialog.finishClosing: whatDialog is null");
			return;
		}
		
		Debug.Log(string.Format("Dialog::finishClosing - Finalizing: {0} Stack: {1}", whatDialog.GetType().ToString(), dialogStack.Count));
		DialogBase.CloseDelegate closeDelegate = whatDialog.closeDelegate;
		
		if (closeDelegate != null)
		{
			closeDelegate(whatDialog.dialogArgs);
		}
		
		whatDialog.playCloseSound();
		whatDialog.close();
		whatDialog.destroyDownloadedTextures();
		whatDialog.unloadSourceAssetBundle();
		
		DialogBase.AnswerDelegate answerDelegate = whatDialog.answerDelegate;		
		DialogType closedType = whatDialog.type;

		destroyDialog(whatDialog);

		// Clean up some memory on dialog closes to try to be discrete
		Glb.cleanupMemoryAsync();
		
		if (dialogStack.Count > 0)
		{
			DialogBase topDialog = dialogStack[dialogStack.Count - 1];
			if (topDialog != currentDialog)
			{
				currentDialog = topDialog;
				currentDialog.show();
				bool useShroud = (bool)currentDialog.dialogArgs.getWithDefault(D.SHROUD, true);
				if (useShroud && !shroudActive)
				{
					turnOnShroud();
				}
				else if (!useShroud && shroudActive)
				{
					turnOffShroud();
				}
			}
		}
		else
		{
			currentDialog = null;

			if (!string.IsNullOrEmpty(backgroundMusicRestoreKey))
			{
				Audio.switchMusicKeyImmediate(backgroundMusicRestoreKey);
				backgroundMusicRestoreKey = "";
			}
		}

		if (answerDelegate != null)
		{
			answerDelegate(whatDialog.dialogArgs);
		}
		
		isClosing = false;

		if (closingQueue.Count > 0)
		{
			closeQueuedDialog();
		}
		else if (Scheduler.hasTask)
		{
			// Check the todo list for doing something again after setting isClosing to false.
			Scheduler.run();
		}
		
		NGUIExt.enableAllMouseInput();

		string msg = null;
		if (closedType.keyName == "generic")
		{
			// Generic dialogs don't give enough debug data, so lets also print out their title.
			string title = whatDialog.dialogArgs.getWithDefault(D.TITLE, "NO TITLE") as string;
			msg = string.Format("Dialog::finishClosing - Finalized. Type: {0}, title: {1} Stack Count: {2}", closedType.keyName, title, dialogStack.Count);
		}
		else
		{
			msg = string.Format("Dialog::finishClosing - Finalized. Type: {0} Stack Count: {1}", closedType.keyName, dialogStack.Count);
		}
		
		Bugsnag.LeaveBreadcrumb(msg);

		//Infer the amount the dialog awarded the player based on the player's wallet at the start/end of the dialog if the winAmount isn't already explicitly set
		if (whatDialog.startingCreditAmount != SlotsPlayer.creditAmount && whatDialog.winAmount == 0)
		{
			whatDialog.winAmount = SlotsPlayer.creditAmount - whatDialog.startingCreditAmount;
		}

		if (whatDialog.winAmount != 0)
		{
			Userflows.addExtraFieldToFlow(whatDialog.userflowKey, "win_amount", whatDialog.winAmount.ToString());
		}
		
		
		
		Userflows.flowEnd(whatDialog.userflowKey);
	}

	private void destroyDialog(DialogBase whatDialog, bool stopShroudTween = false)
	{
		dialogStack.Remove(whatDialog);

		// For some reason, if we don't set the object inactive before destroying it,
		// part of the dialog's elements may remain visible for another frame or so
		// on slow devices like iPhone 4.
		whatDialog.gameObject.SetActive(false);
		GameObject.Destroy(whatDialog.gameObject);
		
		Scheduler.removeTask(whatDialog.task);

		if (stopShroudTween)
		{
			iTween.Stop(shroud.gameObject);
		}
	}
	
	// Closes the next dialog that was queued up to close.
	public void closeQueuedDialog()
	{
		DialogBase whatDialog = closingQueue[closingQueue.Count - 1];
		closingQueue.Remove(whatDialog);
		Dialog.close(whatDialog);
	}

	/// Creates and shows a dialog of the given type.
	public IEnumerator create(DialogType type, DialogTask task, Dict args, DialogBase.AnswerDelegate answerDelegate = null, DialogBase.CloseDelegate closeDelegate = null, bool useShroud = true, bool showPet = false)
	{
		if (currentDialog == null)
		{
			backgroundMusicRestoreKey = Audio.defaultMusicKey;
		}
		
		string msg = string.Format("Dialog::create - Making: {0}", type.keyName);
		Bugsnag.LeaveBreadcrumb(msg);

		if (type == null)
		{
			// Callback the delegate with a failed message for the special cases where this can happen
			if (answerDelegate != null)
			{
				answerDelegate(markDialogFailed(args));
			}
			Debug.LogError("Attempted to create a dialog with null type.");
			yield break;
		}

		isOpening = true;
		lastOpenedDialogKey = type.keyName;
//		Debug.Log(string.Format("#Attempting to open dialog '{0}'.", dialogName ?? ""));

		string userFlowKey = "dialog-" + type.keyName;
		if (Userflows.isUserflowActive(userFlowKey))
		{
		    string newKey = "";
			// If there is already a flow open with this key, then we want to add an identifier
			for (int i = 0; i < MAX_DIALOG_CLONE_COUNT; i++)
			{
				// Try a finite number of times to get a key that works.
			    newKey = userFlowKey + "-" + i.ToString();
				if (!Userflows.isUserflowActive(newKey))
				{
					// If we find one that isnt being used, then set that as our key.
					userFlowKey = newKey;
					break;
				}
				newKey = "";
			}
			// If we go through all of our options, then something weird is going on, so lets log
			// and just conitnue to use the original one, this will mess up the userflow but better than infinite looping.
			if (string.IsNullOrEmpty(newKey))
			{
				Debug.LogWarningFormat("Dialog.cs -- create -- we tried and failed to create a unique userflow key, defaulting to the base: {0}", userFlowKey);
			}
		}
		
		Userflows.flowStart(userFlowKey);
				
		// If another dialog is already being displayed, hide it while showing the new one.
		if (currentDialog != null)
		{
			currentDialog.hide();
		}

		// Show the appropriate dialog UI elements.
		
		GameObject dialogObject = null;
		if (type.prefab != null)
		{
			dialogObject = CommonGameObject.instantiate(type.prefab) as GameObject;
		}
		
		if (dialogObject != null)
		{
			currentDialog = dialogObject.GetComponent<DialogBase>();
			if (currentDialog != null)
			{
				currentDialog.userflowKey = userFlowKey;
				currentDialog.type = type;
				currentDialog.answerDelegate = answerDelegate;
				currentDialog.closeDelegate = closeDelegate;
				currentDialog.setArgs(args);
				currentDialog.dialogBaseHasBeenOpened = true;
				currentDialog.task = task;
			}
			else
			{
				dialogObject.SetActive(false);
				if (answerDelegate != null)
				{
					answerDelegate(markDialogFailed(args));
				}
				logCrittercismFailure(string.Format("'{0}' dialog prefab does not contain a DialogBase component.", type.keyName), task, userFlowKey);
				isOpening = false;
				isClosing = false;
				yield break;
			}
		}
		else
		{
			if (answerDelegate != null)
			{
				answerDelegate(markDialogFailed(args));
			}
			isOpening = false;
			isClosing = false;
			logCrittercismFailure(string.Format("Failed to load '{0}' dialog prefab.", type.keyName), task, userFlowKey);
			yield break;
		}
		
		NGUIExt.attachToAnchor(currentDialog.gameObject, NGUIExt.SceneAnchor.DIALOG, Vector3.zero);

		dialogStack.Add(currentDialog);

#if UNITY_EDITOR
		currentDialog.init();
#else
		try
		{
			currentDialog.init();
		}
		catch (System.Exception e)
		{
			onCurrentDialogFailed(e.Message, userFlowKey);
			yield break;
		}
#endif

		if (dialogStack.Count == 1)
		{
			iTween.Stop(shroud.gameObject);
			// This is the first dialog in the stack, so show the shroud too.
			if (MobileUIUtil.isSlowDevice)
			{
				// If the device is slow, just show it instead of fading in.
				if (useShroud)
				{
					turnOnShroud();
				}
				else if (shroudActive)
				{
					turnOffShroud();
				}
				
				toggleDialogCamera(true);
			}
			else
			{
				toggleDialogCamera(true);
				if (useShroud)
				{
					turnOnShroud();
				}
				else if (shroudActive)
				{
					turnOffShroud();
				}
				// Enable camera
			}
		}
		else if (useShroud && !shroudActive)
		{
			turnOnShroud();
		}
		else if (!useShroud && shroudActive)
		{
			turnOffShroud();
		}

		if (type.keyName == "generic")
		{
			// Generic dialogs don't give enough debug data, so lets also print out their title.
			string title = currentDialog.dialogArgs.getWithDefault(D.TITLE, "NO TITLE") as string;
			msg = string.Format("Dialog::create - Made: {0}, title: {1} Stack Count: {2}", type.keyName, title, dialogStack.Count);
		}
		else
		{
			msg = string.Format("Dialog::create - Made: {0} Stack Count: {1}", type.keyName, dialogStack.Count);
		}

		Bugsnag.LeaveBreadcrumb(msg);	

		// Make it so the idle timer doesn't stop any sound on our dialogs.
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.idleTimer = float.MaxValue; 
		}

		if (showPet)
		{
			if (VirtualPetController.instance != null)
			{
				VirtualPetController.instance.showPet(currentDialog.sizer);
			}
			else
			{
				VirtualPetController.createPetController(currentDialog.sizer);
			}		
		}
	}

	public void turnOnShroud()
	{
		shroudActive = true;
		shroud.gameObject.SetActive(true);
		shroudCollider.enabled = true;
		if (MobileUIUtil.isSlowDevice)
		{
			shroud.updateFade(SHROUD_FADE_ALPHA);
		}
		else
		{
			iTween.ValueTo(shroud.gameObject, iTween.Hash("from", shroud.sprite.alpha, "to", SHROUD_FADE_ALPHA, "time", .125f, "onupdate", "updateFade"));
		}
	}

	public void turnOffShroud()
	{
		shroud.updateFade(0);
		shroud.gameObject.SetActive(false);
		shroudActive = false;
		shroudCollider.enabled = false;
	}

	/// <summary>
	/// When currentDialog.init() fails, this method is called. It destroys, removes, and runs the scheduler
	/// again
	/// </summary>
	/// <param name="reason"></param>
	/// <param name="userFlowKey"></param>
	private void onCurrentDialogFailed(string reason, string userFlowKey)
	{
		Debug.LogError("Dialog failed to open: " + reason);
		logCrittercismFailure(reason, currentDialog.task, userFlowKey);
		destroyDialog(currentDialog, true);
		currentDialog = null;
		isOpening = false;
		isClosing = false;
		Scheduler.run();
	}
		
	// Reusable function for logging a crittercism failure and showing the message in the console.
	private void logCrittercismFailure(string failedMsg, DialogTask task, string userflowKey, bool isWarning = false)
	{
		Scheduler.removeTask(task);	// Make sure this dialog doesn't keep trying to open.
		if (isWarning)
		{
			Debug.LogWarning(failedMsg);
		}
		else
		{
			Debug.LogError(failedMsg);
		}
		Userflows.flowEnd(userflowKey, false, "failed");
	}


	public DialogBase findOpenDialogOfType(string dialogTypeKey)
	{
		foreach (DialogBase dialog in dialogStack)
		{
			if (dialog.type.keyName == dialogTypeKey)
			{
				return dialog;
			}
		}
		return null;
	}
	
	public List<DialogBase> findOpenDialogsOfType(string dialogTypeKey)
	{
		List<DialogBase> results = new List<DialogBase>();
		foreach (DialogBase dialog in dialogStack)
		{
			if (dialog.type.keyName == dialogTypeKey)
			{
				results.Add(dialog);
			}
		}
		return results;
	}
	
	public List<DialogBase> findOpenDialogsOfTypes(HashSet<string> dialogTypeKeys)
	{
		List<DialogBase> results = null;
		foreach (DialogBase dialog in dialogStack)
		{
			if (dialogTypeKeys.Contains(dialog.type.keyName))
			{
				if (results == null)
				{
					results = new List<DialogBase>();
				}
				results.Add(dialog);
			}
		}
		return results;
	}

	/// Creates or appends answer args with a failed message
	private static Dict markDialogFailed(Dict args)
	{
		if (args == null)
		{
			args = Dict.create(D.ANSWER, "failed");
		}
		else
		{
			// This key might exist, it might not, but we want it to say failed regardless
			args.merge(D.ANSWER, "failed");
		}
		return args;
	}
	
	// Called whenever the screen resolution changes.
	public void resolutionChangeHandler()
	{
		// Each dialog may or may not implement a resolution change handler.
		foreach (DialogBase dialog in dialogStack)
		{
			dialog.resolutionChangeHandler();
		}
	}

	/////////////////////////////////////////////////////////////////////////////////////////////////////////	
	// Some dialogs need to wait until textures are finished downloading before showing the dialog.
	// Here's a class to hold onto a bunch of info about a dialog that's downloading textures before showing.
	private class DelayedTextureDialog
	{
		public DialogType type;
		public string[] textureUrls = null;
		public Dict args = null;
		public int textureCountToDownload = 0;
		// Some dialogs are better to not be shown at all if the texture(s) fail to download.
		public bool shouldAbortOnTextureFail = false;
		public bool doAbortDialog = false;
	}
	
	private int delayedDialogId = 0;	// Increment this for each dialog that uses this. Used as the key in textureDialogs dictionary.
	private Dictionary<int, DelayedTextureDialog> pendingDelayedDialogs = new Dictionary<int, DelayedTextureDialog>();

	public bool isDownloadingTexture
	{
		get { return (totalTexturesDownloading > 0); }
	}
	private int totalTexturesDownloading = 0;

	// Download the specified textures then show the specified dialog with the specified args.
	// Overload for a single texture, for convenience. Most dialogs will use this one.
	public void showDialogAfterDownloadingTextures
	(
		string dialogTypeKey,
		string textureUrl,
		Dict args = null,
		bool shouldAbortOnFail = false,
		SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW,
		bool isExplicitPath = false,
		bool isPersistent = false,
		AssetFailDelegate onDownloadFailed = null,
		bool skipBundleMapping = false
	)
	{
		if (!skipBundleMapping)
		{
			showDialogAfterDownloadingTextures(dialogTypeKey, new string[] {textureUrl}, args, shouldAbortOnFail,
				priorityType, isExplicitPath, isPersistent, onDownloadFailed);
		}
		else
		{
			showDialogAfterDownloadingTextures(dialogTypeKey, null, args, shouldAbortOnFail,
				priorityType, isExplicitPath, isPersistent, onDownloadFailed, new string[] {textureUrl});
		}
	}

	// Overload for multiple textures.
	public void showDialogAfterDownloadingTextures(
		string dialogTypeKey,
		string[] textureUrls = null,
		Dict args = null,
		bool shouldAbortOnFail = false,
		SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW,
		bool isExplicitPath = false,
		bool isPersistent = false,
		AssetFailDelegate onDownloadFailed = null,
		string[] nonMappedBundledTextures = null)
	{
		MOTDFramework.notifyOnShow(args);

		DialogType type = DialogType.find(dialogTypeKey);
		if (type == null)
		{
			return;
		}

		if (args != null && args.containsKey(D.THEME) && !string.IsNullOrEmpty((string)args[D.THEME]))
		{
			type.setThemePath((string)args[D.THEME]);
		}

		if (textureUrls == null)
		{
			textureUrls = new string[0];
		}

		// If there is already a request to download the same textures for this dialog type,
		// then ignore this call so we don't get multiple instances of the dialog
		// when the texture finally finishes downloading.
		// It's possible to call the same dialog type multiple times with different texture downloads,
		// such as with the generic MOTD dialog.
		foreach (DelayedTextureDialog pendingDialog in pendingDelayedDialogs.Values)
		{
			if (pendingDialog.type != type)
			{
				continue;
			}

			// Found a matching dialog type, but we only care if it's using one of the same textures.
			foreach (string pendingTextureUrl in pendingDialog.textureUrls)
			{
				foreach (var desiredTextureUrl in textureUrls)
				{
					if (pendingTextureUrl == desiredTextureUrl)
					{
						// Found an existing texture download for the same dialog type, so bail.
						return;
					}
				}

				if (nonMappedBundledTextures == null)
				{
					continue;
				}
					
				foreach (var desiredTextureUrl in nonMappedBundledTextures)
				{
					if (pendingTextureUrl == desiredTextureUrl)
					{
						// Found an existing texture download for the same dialog type, so bail.
						return;
					}
				}
			}
		}
		
		if (delayedDialogId == int.MaxValue)
		{
			// If somehow we reach the maximum id value in a single session, start over at 0.
			delayedDialogId = 0;
		}
		
		delayedDialogId++;
		int newDelayedDialogId = delayedDialogId;
		
		if (args == null)
		{
			// We need args to pass the downloaded texture references to the dialog.
			args = Dict.create();
		}

		int nonMappedBundledTexturesLength = nonMappedBundledTextures != null ? nonMappedBundledTextures.Length : 0;
		args.merge(D.DOWNLOADED_TEXTURES, new Texture2D[textureUrls.Length + nonMappedBundledTexturesLength]);

		DelayedTextureDialog delayedTextureDialog = new DelayedTextureDialog();
		pendingDelayedDialogs[newDelayedDialogId] = delayedTextureDialog;
		if (nonMappedBundledTexturesLength > 0)
		{
			delayedTextureDialog.textureUrls = new string[textureUrls.Length + nonMappedBundledTexturesLength];
			textureUrls.CopyTo(delayedTextureDialog.textureUrls, 0);
			nonMappedBundledTextures.CopyTo(delayedTextureDialog.textureUrls, textureUrls.Length);
		}
		else
		{
			delayedTextureDialog.textureUrls = textureUrls;
		}
		
		delayedTextureDialog.type = type;
		delayedTextureDialog.args = args;
		delayedTextureDialog.textureCountToDownload = textureUrls.Length + nonMappedBundledTexturesLength;
		delayedTextureDialog.shouldAbortOnTextureFail = shouldAbortOnFail;
		
		storeCallHistory(type);
					
		totalTexturesDownloading += textureUrls.Length + nonMappedBundledTexturesLength;

		//If we're not actually downloading any textures then go straight to the dialog
		if (delayedTextureDialog.textureCountToDownload == 0)
		{
			Scheduler.addDialog(type, args);
			return;
		}
		
		for (int i = 0; i < textureUrls.Length; i++)
		{
			if (string.IsNullOrEmpty(textureUrls[i]))
			{
				Debug.LogErrorFormat("showDialogAfterDownloadingTextures passed empty textureUrl at index {0}!", i);
				// note downloadTextureCallback must be called for each texture in the list, even if it is empty/null or the totalTexturesDownloading count will get out of sync and the dialog will never show, no skipping!
			}
			
			Dict textureArgs = Dict.create(
				D.IMAGE_PATH, textureUrls[i],
				D.OPTION, i,
				D.DELAYED_DIALOG_ID, newDelayedDialogId,
				D.PRIORITY, priorityType,
				D.IS_PERSISTENT_TEXTURE, isPersistent,
				D.DIALOG_TYPE, dialogTypeKey
			);

			//!! TODO: czablocki - should skip bundle mapping be true here?
			StartCoroutine (
				DisplayAsset.loadTextureFromBundle (
					primaryPath: textureUrls[i],
					callback: downloadTextureCallback,
					data: textureArgs,
					secondaryPath: "",
					isExplicitPath: isExplicitPath,
					loadingPanel: true,
					onDownloadFailed: onDownloadFailed
				)
			);
		}

		if (nonMappedBundledTexturesLength <= 0)
		{
			return;
		}
		
		for (int i = 0; i < nonMappedBundledTextures.Length; i++)
		{
			if (string.IsNullOrEmpty(nonMappedBundledTextures[i]))
			{
				Debug.LogErrorFormat("showDialogAfterDownloadingTextures passed empty nonMappedBundledTexture at index {0}!", i);
				// note downloadTextureCallback must be called for each texture in the list, even if it is empty/null or the totalTexturesDownloading count will get out of sync and the dialog will never show, no skipping!
			}
			
			Dict textureArgs = Dict.create(
				D.IMAGE_PATH, nonMappedBundledTextures[i],
				D.OPTION, i + textureUrls.Length,
				D.DELAYED_DIALOG_ID, newDelayedDialogId,
				D.PRIORITY, priorityType,
				D.IS_PERSISTENT_TEXTURE, isPersistent
			);

			StartCoroutine (
				DisplayAsset.loadTextureFromBundle(
					primaryPath: nonMappedBundledTextures[i],
					callback: downloadTextureCallback,
					data: textureArgs,
					secondaryPath: "",
					isExplicitPath: isExplicitPath,
					loadingPanel: true,
					onDownloadFailed: onDownloadFailed,
					skipBundleMapping:true,
					pathExtension:".png"
				)
			);
		}
	}

	// Static TextureDelegate callback for the loadTexture call.
	// Shows the Antisocial dialog on a successful download.
	public void downloadTextureCallback(Texture2D tex, Dict args)
	{
		int dialogId = (int)args.getWithDefault(D.DELAYED_DIALOG_ID, -1);
		DelayedTextureDialog delayedDialog = pendingDelayedDialogs[dialogId];
		
		DialogType type = delayedDialog.type;
		int index = (int)args.getWithDefault(D.OPTION, 0);
		bool isPersistent = (bool)args.getWithDefault(D.IS_PERSISTENT_TEXTURE, false);
		Dict dialogArgs = delayedDialog.args;
		Texture2D[] textures = dialogArgs.getWithDefault(D.DOWNLOADED_TEXTURES, null) as Texture2D[];

		SchedulerPriority.PriorityType priority = (SchedulerPriority.PriorityType)args.getWithDefault(D.PRIORITY, SchedulerPriority.PriorityType.LOW);
		
		delayedDialog.textureCountToDownload--; // Mark the download as complete.
		totalTexturesDownloading--;

		if (tex != null)
		{
			textures[index] = tex;
			string path = (string)args.getWithDefault(D.IMAGE_PATH, "");
			if (DisplayAsset.wasStreamedNonLobbyOptionTexture(tex))	// Only save this for destruction later if was streamed and is not a lobby option.
			{
				// Keep track of which textures were streamed in and how many times it was requested, for destruction purposes later.
				StreamedTexture streamed = findStreamedTexture(tex);
				if (streamed == null)
				{	
					streamed = new StreamedTexture(path, tex, isPersistent);
					streamedTextures.Add(tex, streamed);
				}
				streamed.useCount++;
			}
		}
		else
		{
			string texPath = (string)args.getWithDefault(D.IMAGE_PATH, "unknown name");
			if (delayedDialog.shouldAbortOnTextureFail)
			{
				delayedDialog.doAbortDialog = true;
			}
			Debug.LogErrorFormat("Could not download background texture '{0}' for dialog {1}", texPath, type.keyName);
		}

		if (delayedDialog.textureCountToDownload != 0)
		{
			return;
		}

		// All textures finished downloading or finished failing.
		if (!delayedDialog.doAbortDialog && SlotsPlayer.isLoggedIn)
		{
			// Don't abort even if textures failed, so show the dialog now
			// if the player is still logged in after downloading finished.
			Scheduler.addDialog(type, dialogArgs, priority);
		}
		else
		{
			// If the dialog isn't shown, make sure any downloaded textures are
			// cleaned up immediately instead of when the dialog was supposed to close.
			foreach (Texture2D texToDestroy in textures)
			{
				destroyStreamedNonLobbyOptionTexture(texToDestroy);
			}

			// If we failed to show a dialog becuase of loading textures, make sure we tell the MOTD system.
			MOTDFramework.alertToDialogShowCall(dialogArgs);
		}
		
		pendingDelayedDialogs.Remove(dialogId);
	}

	private class TextureDialogHistoryItem
	{
		public DialogType dialogType;
		public string callStack;
		public bool showCallStack;
	}

	private List<TextureDialogHistoryItem> textureDialogHistory = new List<TextureDialogHistoryItem>();
	private bool showTextureDialogHistory = false;
	private const int textureDialogHistoryMaxSize = 20;

	private void storeCallHistory(DialogType dialogType)
	{
#if !UNITY_WEBGL
		string rawStack = System.Environment.StackTrace;
		// Remove useless frames for StackTrace, this function, and immediate caller (showDialogAfterDownloadingTextures).
		List<string> frames = new List<string>(rawStack.Split(new char[] {'\n'}));

		frames.RemoveRange(0, 3);
		string stackTrace = string.Join("\n", frames.ToArray());

		TextureDialogHistoryItem historyItem = new TextureDialogHistoryItem();
		historyItem.callStack = stackTrace;
		historyItem.showCallStack = false;
		historyItem.dialogType = dialogType;

		textureDialogHistory.Add(historyItem);
		if (textureDialogHistory.Count > textureDialogHistoryMaxSize)
		{
			int toRemoveCount = textureDialogHistory.Count - textureDialogHistoryMaxSize;
			textureDialogHistory.RemoveRange(0, toRemoveCount);
		}
#endif
	}

	// Draw debug info for devgui and editor window debugging tool.
	public static void drawDebugInfo()
	{
		// Ensure label style will show multiple lines for stack traces.
		var stackTraceLabelStyle = new GUIStyle(GUI.skin.label);
		stackTraceLabelStyle.wordWrap = true;
		stackTraceLabelStyle.stretchHeight = true;
		stackTraceLabelStyle.fixedHeight = 0;
		stackTraceLabelStyle.normal.background = null;

		GUILayout.BeginHorizontal();
		{
			if (instance.showTextureDialogHistory)
			{
				GUILayout.Label("TextureDialog Call history:");
				if (GUILayout.Button("Hide"))
				{
					instance.showTextureDialogHistory = false;
				}
				for (int i = instance.textureDialogHistory.Count - 1; i >= 0 ; i--)
				{
					var historyItem = instance.textureDialogHistory[i];
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					GUILayout.Label(historyItem.dialogType.keyName);
					if (historyItem.showCallStack)
					{
						if (GUILayout.Button("Hide call stack"))
						{
							historyItem.showCallStack = false;
						}
						else
						{
							GUILayout.EndHorizontal();
							GUILayout.BeginHorizontal();
							GUILayout.Label(historyItem.callStack, stackTraceLabelStyle);
						}
					}
					else if (GUILayout.Button("Show call stack"))
					{
						historyItem.showCallStack = true;
					}
				}
			}
			else if (GUILayout.Button("Show TextureDialog call history"))
			{
				instance.showTextureDialogHistory = true;
			}
		}
		GUILayout.EndHorizontal();
	}

	// Destroy texture or decrement the usage count.
	public void destroyStreamedNonLobbyOptionTexture(Texture2D tex)
	{
		if (tex == null)
		{
			return;
		}
		
		StreamedTexture streamed = findStreamedTexture(tex);
		if (streamed == null)
		{
			// This can happen for lobby option textures. Hopefully it ONLY happens for those, or else we have a problem.
			return;
		}
		
		// Decrement the stream count.
		// If reaching 0, then no more sources need it, so it's safe to clean it up.
		// MCC -- unless it is a persistent texture used elsewhere in the game.
		streamed.useCount--;
		if (streamed.useCount == 0 && !streamed.isPersistent)
		{
			streamedTextures.Remove(tex);
			Object.Destroy(tex);
		}
	}
	
	private StreamedTexture findStreamedTexture(Texture2D tex)
	{
		if (streamedTextures.ContainsKey(tex))
		{
			return streamedTextures[tex];
		}
		return null;
	}

	private class StreamedTexture
	{
		public Texture2D texture;
		public string path = "";
		public int useCount = 0;
		public bool isPersistent = false;
		
		public StreamedTexture(string path, Texture2D texture, bool isPersistent = false)
		{
			this.texture = texture;
			this.path = path;
			this.isPersistent = isPersistent;
		}
	}

	// End of downloaded texture stuff.
	/////////////////////////////////////////////////////////////////////////////////////////////////////////


	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		// When resetting, make sure there are no lingering open dialogs.
		while (instance.isShowing)
		{				
			if (instance.dialogStack.Count > 0)
			{
				// cleanup instance.dialogStack[0]
				DialogBase dialog = instance.dialogStack[0];
				GameObject.Destroy(dialog.gameObject);
				Scheduler.removeTask(dialog.task);
				instance.dialogStack.RemoveAt(0);	
			}
			else
			{
				instance.currentDialog = null;
			}
		}

		instance.isClosing = false;
		instance.isOpening = false;
		instance.shroudCollider.enabled = false;
		// In certain cases a dialog is created right as a reset happens and it tweens in the shroud
		iTween.Stop(instance.shroud.gameObject);
		instance.shroud.updateFade(0);
	}
}