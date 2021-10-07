using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This static class handles touch input, especially gesture recognition.
*/
public static class TouchInput
{
#if UNITY_EDITOR
	public const string EDITOR_SCREENSHOT_FILE_PATH = "Assets/-Temporary Storage-/Screenshots/screenshot.png";
	public static bool isTakingScreenshot = false;
#endif

	public static int SWIPE_THRESHOLD = 0;              ///< Is set based on pixel factor in init function.
	private static float DRAG_DISTANCE_THRESHOLD = 0f;  ///< Is set based on pixel factor in init function.

	private static bool didReleaseLastSwipe = true;     ///< Set false when swipe is detected, until the swipe touch is released.

	public static bool didSwipeLeft = false;            ///< True if a swipe left gesture was recognized.
	public static bool didSwipeRight = false;           ///< True if a swipe right gesture was recognized.
	public static bool didPinchZoom = false;
	public static bool didTap = false;
	public static bool isDragging = false;
	public static bool isTouchDown = false;
	public static SwipeArea swipeArea = null;           ///< The object the current swipe touch started on. Set to null upon release.

	public static Vector2int position = Vector2int.zero;
	public static Vector2int downPosition = Vector2int.zero;
	public static Vector2int speed = Vector2int.zero;

	public static List<SwipeArea> allSwipeAreas = new List<SwipeArea>();
	public static GameObject swipeObject
	{
		get
		{
			if (swipeArea == null)
			{
				return null;
			}
			return swipeArea.gameObject;
		}
	}

	/// Returns drag distance horizontally, for 3D wheel scrolling action.
	public static float dragDistanceX
	{
		get
		{
			if (!isDragging)
			{
				return 0;
			}
			return position.x - downPosition.x;
		}
	}

	/// Returns drag distance vertically, for list scrolling action.
	public static float dragDistanceY
	{
		get
		{
			if (!isDragging)
			{
				return 0;
			}
			return position.y - downPosition.y;
		}
	}

	/// Called every time the scene is loaded/reloaded
	public static void init()
	{
		// Must swipe 1 inch to be detected, so use the DPI of the device to determine that.
		SWIPE_THRESHOLD = (int)MobileUIUtil.getDotsPerInch();
		DRAG_DISTANCE_THRESHOLD = NGUIExt.pixelFactor * 20;

#if UNITY_EDITOR
		if (openDialogCloserCoroutine == null || openDialogCloserCoroutine.isFinished)
		{
			openDialogCloserCoroutine = RoutineRunner.instance.StartCoroutine(openDialogCloser());
		}
#endif
	}

	/// Checks for touch input and looks for gestures.
	public static void update()
	{
		didSwipeLeft = false;
		didSwipeRight = false;

		if (!isTouchDown)
		{
			didReleaseLastSwipe = true;
		}

#if UNITY_EDITOR || UNITY_WSA_10_0 || UNITY_WEBGL
		// Simple keyboard controls for when we have them.
		if (SpinPanel.instance != null)
		{
			bool isBaseGameWebGLInputBlocked = false;
			if (SlotBaseGame.instance != null)
			{
				isBaseGameWebGLInputBlocked = SlotBaseGame.instance.isModuleBlockingWebGLKeyboardInputForSlotGame();
			}
		
			// don't do any of these game spin/bet actions unless nothing is happening
			if (Glb.isNothingHappening && !isBaseGameWebGLInputBlocked)
			{
				if (Input.GetKeyDown(KeyCode.Space))
				{
					if (SpinPanel.instance.isButtonsEnabled)
					{
						SpinPanel.instance.clickSpin();
					}
					else
					{
						SpinPanel.instance.clickStop();
					}
				}
				else if (Input.GetKeyDown(KeyCode.UpArrow))
				{
					SpinPanel.instance.clickBetUp();
				}
				else if (Input.GetKeyDown(KeyCode.DownArrow))
				{
					SpinPanel.instance.clickBetDown();
				}
			}
			else
			{
				//If something is happening then we at least want to be able to stop spins with the spacebar
				if (Input.GetKeyDown(KeyCode.Space))
				{
					if (!SpinPanel.instance.isButtonsEnabled)
					{
						SpinPanel.instance.clickStop();
					}
				}
			}
		}
		else if (ResolutionChangeHandler.instance != null && ResolutionChangeHandler.instance.virtualScreenMode)
		{
			if (Input.GetKeyDown(KeyCode.K))
			{
				// Center the virtual screen on the real screen.
				ResolutionChangeHandler.instance.virtualScreenAlign(0f);
			}
			else if (Input.GetKeyDown(KeyCode.M))
			{
				// Align the virtual screen to one far side of the real screen.
				// This is left on narrow screens, and right on wide screens.
				ResolutionChangeHandler.instance.virtualScreenAlign(-1f);
			}
			else if (Input.GetKeyDown(KeyCode.O))
			{
				// Align the virtual screen to the other far side of the real screen.
				// This is up on narrow screens, and down on wide screens.
				ResolutionChangeHandler.instance.virtualScreenAlign(1f);
			}
		}
#endif

#if UNITY_EDITOR || UNITY_WEBGL || (UNITY_WSA_10_0 && !ZYNGA_PRODUCTION)
		// The dev panel needs to work for WebGL, which doesn't have a target environment.
		if (Data.debugMode && !UICamera.inputHasFocus)
		{
			if (Input.GetKeyUp(KeyCode.D))
			{
				// DevGUI has additional internal logic to stop itself from showing on production.
				DevGUI.isActive = !DevGUI.isActive;
			}
		}
#endif

#if UNITY_EDITOR
		// \ - Take a screenshot
		if (Input.GetKeyUp(KeyCode.Backslash) && !isTakingScreenshot)
		{
			isTakingScreenshot = true;
			RoutineRunner.instance.StartCoroutine(takeScreenshot());
		}
#endif

#if UNITY_EDITOR || ((UNITY_WSA_10_0 || UNITY_WEBGL) && !ZYNGA_PRODUCTION)
		if (Data.debugMode && !UICamera.inputHasFocus)
		{
			if (Input.GetKeyUp(KeyCode.F))
			{
				DebuggingFPSMemoryComponent.Singleton.gameObject.SetActive(!DebuggingFPSMemoryComponent.Singleton.gameObject.activeInHierarchy);
				LoadingHIRV3.hirV3.toggleCamera(DebuggingFPSMemoryComponent.Singleton.gameObject.activeSelf, true);
			}

#if UNITY_EDITOR
			// TEMP HOTKEYS TO TOGGLE above/below reel symbol culling while feature is in development
			if (Input.GetKeyUp(KeyCode.J))
			{
				Glb.autoToggleSymbolCulling = !Glb.autoToggleSymbolCulling & Glb.enableSymbolCullingSystem;
				Debug.Log("Glb.autoToggleSymbolCulling = " + Glb.autoToggleSymbolCulling);
			}
			if (Input.GetKeyUp(KeyCode.K))
			{
				Glb.autoToggleSymbolCulling = false;
				Glb.enableSymbolCulling = !Glb.enableSymbolCulling & Glb.enableSymbolCullingSystem;
				Debug.Log("Glb.enableSymbolCulling = " + Glb.enableSymbolCulling);
			}
			if (Input.GetKeyUp(KeyCode.P))
			{
				WeeklyRaceBoost.showDialog();
			}

			timeScaleSpeedCheck();
			dialogBeGoneModeCheck();
#endif

		}
#endif

		if (Input.touchCount == 3)
		{
			if ((Input.touches[0].phase == TouchPhase.Began || Input.touches[1].phase == TouchPhase.Began || Input.touches[2].phase == TouchPhase.Began) && Data.debugMode)
			{
				DevGUI.isActive = !DevGUI.isActive;
			}
		}

		if (Input.touchCount == 2)
		{
			// Debug && Quality menu access
			if ((Input.touches[0].phase == TouchPhase.Began || Input.touches[1].phase == TouchPhase.Began) && Data.debugMode)
			{
				Vector2 t0pos = Input.touches[0].position;
				Vector2 t1pos = Input.touches[1].position;

				// Slot Base Game menu - touch LR & UL
				if ((t0pos.x < (Screen.width / 5) && t0pos.y > Screen.height - (Screen.height / 5)) &&
				   (t1pos.x > Screen.width - (Screen.width / 5) && t1pos.y > (Screen.height / 5)))
				{
					if (SlotBaseGame.instance != null)
					{
						SlotBaseGame.instance.testGUI = !SlotBaseGame.instance.testGUI;
					}
				}
				else
				{
					// Turned this off since I added a button to the dev panel for showing the log,
					// and testers will probably accidentally do this while trying to reproduce multitouch issues.
					// Log.isActive = true;
				}
			}
		}
		else
		{
			if (DevGUI.isActive)
			{
				//Don't try to detect swiping/dragging if the dev gui is active
				return;
			}
			if (isDragging && didReleaseLastSwipe)
			{
				// Only check for swiping if a swipe has not already been detected, and the touch released,
				// since we can't have two different swipes with a single touch.
				int xDiff = position.x - downPosition.x;

				// If dragging, and the distance that the touch was dragged while
				// down exceeds the threshold, then treat it as a swipe.
				int swipeThreshold = SWIPE_THRESHOLD;

				if (swipeArea != null)
				{
					// If swiping on a particular swipe area,
					// then make sure the threshold is no wider than half the swipe area.
					// This is particularly useful for small swipe area for slider buttons like on the settings dialog.
					int rawThreshold = swipeArea.swipeThreshold;
					if (rawThreshold > 0)
					{
						// Calculate the screen size of the raw threshold amount based on the screen size of the swipe area.
						Rect screenRect = swipeArea.getScreenRect();
						float ratio = (float)rawThreshold / swipeArea.size.x;
						swipeThreshold = (int)Mathf.Min(swipeThreshold, ratio * screenRect.width);
					}
				}

				didSwipeLeft = (xDiff < -swipeThreshold);
				didSwipeRight = (xDiff > swipeThreshold);

				if (didSwipeLeft || didSwipeRight)
				{
					didReleaseLastSwipe = false;
				}
			}
			else if (Input.touchCount == 0)
			{
				didPinchZoom = false;
			}
		}

		// Standard input stuff

		bool lastDown = isTouchDown;
		Vector2int newPosition = Vector2int.zero;
		bool isSimulatedTouch = false;

		if (Input.simulateMouseWithTouches && Input.touchCount > 0)
		{
			isSimulatedTouch = !Input.GetTouch(0).Equals(null);
		}

		if (Input.GetMouseButton(0) || isSimulatedTouch)
		{
			if (Input.touchCount >= 1)
			{
				newPosition = new Vector2int((int)Input.touches[0].position.x, (int)Input.touches[0].position.y);
			}
			else
			{
				newPosition = new Vector2int((int)Input.mousePosition.x, (int)Input.mousePosition.y);
			}
		}
		else
		{
			// Use the previous touch position
			newPosition = position;
		}

		speed = newPosition - position;
		position = newPosition;

		// If we're simulating, Input.GetMouseButton(0) will return false
		// Automated touches on SwipeArea objects will never get registered as a result
		if (Input.simulateMouseWithTouches && Input.touchCount > 0)
		{
			// Cannot compare with null using != operator
			isTouchDown = !Input.GetTouch(0).Equals(null);
		}
		else
		{
			isTouchDown = Input.GetMouseButton(0);
		}

		didTap = lastDown && !isTouchDown;
		if (didTap)
		{
			UserActivityManager.instance.onMouseInput();
		}

		if (!lastDown && isTouchDown)
		{
			downPosition = position;

			// Check for any swipe areas that this new touch might have been in.
			SwipeArea area;
			for (int i = allSwipeAreas.Count - 1; i >= 0; i--)
			{
				area = allSwipeAreas[i];
				
				if (area.enabled && area.gameObject.activeInHierarchy && CommonMath.rectContainsPoint(area.getScreenRect(), position))
				{
					swipeArea = area;
					break;  // Only use the first touched area. Multiple areas shouldn't be set up to overlap anyway.
				}
			}
		}
		else if (lastDown && !isTouchDown)
		{
			swipeArea = null;
		}

		if (isDragging)
		{
			if (!isTouchDown)
			{
				isDragging = false;
			}
		}
		else if (lastDown && isTouchDown)
		{
			if (downPosition.distanceTo(position) > DRAG_DISTANCE_THRESHOLD)
			{
				isDragging = true;
			}
		}
	}

#if UNITY_EDITOR
	private static TICoroutine openDialogCloserCoroutine;
	private static bool dialogBeGoneMode;   // if true dialogs will be closed as soon as they open
	private static float dialogCloserTimer;   // while > 0 all dialogs will be closed
	private static DialogBase lastClosedDialog;

	/// allows the use of arrow keys to speed up and slow down the time scale while running game in the editor
	private static float timeScaleAdjustDelay;

	private static void timeScaleSpeedCheck()
	{
		timeScaleAdjustDelay += Time.deltaTime;

		if (timeScaleAdjustDelay > 0.1f)
		{
			if (UnityEngine.Input.GetKey(KeyCode.LeftShift) && UnityEngine.Input.GetKey(KeyCode.RightBracket))
			{
				if (Time.timeScale < 2.0f)
				{
					Time.timeScale += 0.1f;
				}
				else
				{
					Time.timeScale = Mathf.Min(Time.timeScale + 1.0f, 10.0f);
				}
			}
			else if (UnityEngine.Input.GetKey(KeyCode.RightBracket))
			{
				Time.timeScale = 10.0f;
			}

			if (UnityEngine.Input.GetKey(KeyCode.LeftShift) && UnityEngine.Input.GetKey(KeyCode.LeftBracket))
			{
				if (Time.timeScale > 2.0f)
				{
					Time.timeScale -= 1.0f;
				}
				else
				{
					Time.timeScale = Mathf.Max(Time.timeScale - 0.1f, 0.1f);
				}
			}
			else if (UnityEngine.Input.GetKey(KeyCode.LeftBracket))
			{
				Time.timeScale = 1.0f;
			}

			timeScaleAdjustDelay = 0;
		}
	}

	public static IEnumerator openDialogCloser()
	{
		while (true)
		{
			if (dialogBeGoneMode && Dialog.instance != null && Dialog.instance.currentDialog != null && !Dialog.instance.isClosing)
			{
				yield return CommonAutomation.routineRunnerBehaviour.StartCoroutine(Dialog.instance.currentDialog.automate());
			}
			else
			{
				yield return null;
			}
		}
	}

	private static void dialogBeGoneModeCheck()
	{
		if (Input.GetKeyUp(KeyCode.Backspace))
		{
			if (UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl))
			{
				CommonAutomation.loadLastGame();
				dialogBeGoneMode = true;
			}
			else
			{
				dialogBeGoneMode = !dialogBeGoneMode;
			}
		}
	}

	private static IEnumerator takeScreenshot()
	{
		string uniquePath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(EDITOR_SCREENSHOT_FILE_PATH);
		
		// Create the directory if it does not exist
		string directory = System.IO.Path.GetDirectoryName(uniquePath);
		if (!System.IO.Directory.Exists(directory))
		{
			System.IO.Directory.CreateDirectory(directory);
		}
		
		// Take a screenshot and save it out
		ScreenCapture.CaptureScreenshot(uniquePath);
		
		// Wait for screenshot save to finish, it is asynchronous with no completion hooks.
		while (!System.IO.File.Exists(uniquePath))
		{
			Debug.Log("Waiting for the screenshot to be saved to " + uniquePath);
			yield return null;
		}
			
		// Import the new asset so Unity is aware of it
		UnityEditor.AssetDatabase.ImportAsset(uniquePath);

		isTakingScreenshot = false;
	}
#endif
}
