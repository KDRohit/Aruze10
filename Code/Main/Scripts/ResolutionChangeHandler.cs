using UnityEngine;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/*
This is a single entry point for handling all code that needs to fire when the application resolution changes.
This could mean something as simple as the device being rotated from landscape to portrait, or vice-versa.
Put all code and function calls here instead of creating more OnApplicationPause function in other scripts.
*/

public class ResolutionChangeHandler : TICoroutineMonoBehaviour, IResetGame
{
	public enum VirtualScreenOverride
	{
		AUTO,
		FORCE_ON,
		FORCE_OFF
	}

#if UNITY_WEBGL
	public const int WEBGL_MAX_WIDTH = 1150;
	public const int WEBGL_MIN_WIDTH = 854;
	public const int WEBGL_FIXED_HEIGHT = 640;
#endif

	public const float MIN_ASPECT = 1.33f;		// Minimum (narrowest) supported aspect ratio for all content in the game.
	public const float MAX_ASPECT = 2.34f;      // Maximum (widest) supported aspect ratio for all content in the game.

	public const int MAX_WIDTH = 2048;		// Constrained upper bound of the virtual screen width when displaying in landscape mode
	public const int MAX_HEIGHT = 1536;		// Constrained upper bound of the virtual screen height when displayin in landscape mode
	
	public static Vector2 TOUCH_MISS = new Vector2(-1024f, -1024f);

	public static ResolutionChangeHandler instance = null;
	private static List<Camera> gameCameras = new List<Camera>();
	
	private OnResolutionChangeDelegate onResolutionChangeDelegates = null;
	private int realScreenWidth = 0;
	private int realScreenHeight = 0;
	private static DeviceOrientation landscapeOrientationPreference = DeviceOrientation.Unknown;
	private static TICoroutine androidAutoOrientSwapBackCoroutine = null; // variable used to store the coroutine made for handling the swap back to auto orientation
	
	private GameObject renderQuad = null;
	private GameObject virtualRenderRig = null;
	private Camera virtualCamera = null;
	private RenderTexture screenTexture = null;
	private Material screenMaterial = null;
	private MeshCollider touchScreen = null;
	private float screenAspect;
	private float renderAspect;
	
	public bool virtualScreenMode { get; private set; }
	public int virtualWidth { get; private set; }
	public int virtualHeight { get; private set; }
	public VirtualScreenOverride virtualOverrideMode = VirtualScreenOverride.AUTO;
	
	public static bool isInPortraitMode
	{
		get
		{
#if UNITY_EDITOR || UNITY_WEBGL
			// In editor and webgl we will rely on the screen size to determine if the
			// game is in portrait mode
			return (UnityEngine.Screen.height > UnityEngine.Screen.width);
#else
			// On device so check screen orientation instead of size
			return UnityEngine.Screen.orientation == ScreenOrientation.Portrait;
#endif
		}
	}
	
	void Awake()
	{
		instance = this;
		virtualScreenMode = false;
		
#if UNITY_WEBGL
		// window.gameResize() is in the javascript of the canvas and effectively makes
		//  sure that the div that the webgl content is on is sized appropriately.
		Application.ExternalEval("window.gameResize()");
#endif

		// Make sure when first launching the game that we are in Landscape, just in case info
		// about the device orientation is stored and is trying to make the game go into
		// portrait (the game should never start in portrait)
		if (isInPortraitMode)
		{
			switchToLandscape();
		}
	}
	
	void Update()
	{
		if (UnityEngine.Screen.width == 0 || UnityEngine.Screen.height == 0 
			|| (isInPortraitMode && UnityEngine.Screen.width > UnityEngine.Screen.height)
			|| (!isInPortraitMode && UnityEngine.Screen.height > UnityEngine.Screen.width))
		{
			// Shouldn't happen, but does on some Android devices.
			// Basically sometimes android reports size 0 or 
			// is able to change size before the orientation is reported correctly
			// Do nothing until the screen resolution is better.
			return;
		}
		
#if !UNITY_WEBGL && !UNITY_EDITOR
		// If the device is in landscape and we haven't stored out a landscape
		// preference to restore to, try and grab it.  This is needed for android
		// because we can't just put android back to auto rotation to force it back
		// into landscape.
		if (!isInPortraitMode && (landscapeOrientationPreference == DeviceOrientation.Unknown || landscapeOrientationPreference != Input.deviceOrientation))
		{
			DeviceOrientation currentDeviceOrientation = Input.deviceOrientation;
			if (currentDeviceOrientation == DeviceOrientation.LandscapeLeft || currentDeviceOrientation == DeviceOrientation.LandscapeRight)
			{
				// Input.deviceOrientation is reporting a landscape orientation, so we can save this
				// out for the purpose of restoring this landscape orientation when returning from portrait mode
				landscapeOrientationPreference = currentDeviceOrientation;
			}
		}
#endif
	
		if (realScreenWidth != UnityEngine.Screen.width || realScreenHeight != UnityEngine.Screen.height)
		{
			float defaultAspect = (float)UnityEngine.Screen.width / (float)UnityEngine.Screen.height;
			
			int newWidth = UnityEngine.Screen.width;
			int newHeight = UnityEngine.Screen.height;
			
			if (isInPortraitMode)
			{
				// if we are in portrait mode then the width and height are swapped so need to invert aspect ratio for our checks
				defaultAspect = 1.0f / defaultAspect;
			}

			// In editor because the player can change resolution on the fly, ensure that we are showing the correct
			// spin panel
#if UNITY_EDITOR
			// Check if we've changed into or out of landscape
			bool wasInPortraitMode = (realScreenHeight > realScreenWidth);
			if (wasInPortraitMode != isInPortraitMode)
			{
				// we've changed over so need to swap the spin panel
				if (isInPortraitMode)
				{
					if (FreeSpinGame.instance != null || (SlotBaseGame.instance != null && SlotBaseGame.instance.isDoingFreespinsInBasegame()))
					{
						BonusSpinPanel.instance.setToPortraitMode();
					}
				}
				else
				{
					if (FreeSpinGame.instance != null || (SlotBaseGame.instance != null && SlotBaseGame.instance.isDoingFreespinsInBasegame()))
					{
						BonusSpinPanel.instance.setToLandscapeMode();
					}
				}
			}
#endif

			if (virtualOverrideMode != VirtualScreenOverride.FORCE_OFF &&
				(virtualOverrideMode == VirtualScreenOverride.FORCE_ON ||
					defaultAspect > MAX_ASPECT || defaultAspect < MIN_ASPECT))
			{
				virtualScreenMode = true;
				Vector2int virtualScreenSize = getVirtualScreenSize(defaultAspect);
				newWidth = virtualScreenSize.x;
				newHeight = virtualScreenSize.y;
			}
			else if (virtualScreenMode)
			{
				// We are currently in virtual screen mode, so get out of it.
				releaseCameras();
				destroyVirtualRig();
				virtualScreenMode = false;
			}
			
			if (virtualScreenMode)
			{
				virtualWidth = newWidth;
				virtualHeight = newHeight;
				prepareVirtualRenderRig();
			}
			
			StartCoroutine(callResolutionChangeHandlers());
			
			// These are for change-detection ONLY
			realScreenWidth = UnityEngine.Screen.width;
			realScreenHeight = UnityEngine.Screen.height;
		}
		else
		{
			// Resolution is unchanged. If we are in virtual mode then inventory the cameras.
			if (virtualScreenMode)
			{
				hijackCameras();
			}
		}
	}
	
	// Tell the editor/device to switch to portrait mode, on WebGL this doesn't do anything
	public static void switchToPortrait()
	{		
		// This function will do nothing on WebGL.
#if !UNITY_WEBGL

#if UNITY_ANDROID
		if (androidAutoOrientSwapBackCoroutine != null)
		{
			// If swapping to portrait and the android switch back to auto isn't done, just cancel it.
			RoutineRunner.instance.StopCoroutine(androidAutoOrientSwapBackCoroutine);
			androidAutoOrientSwapBackCoroutine = null;
		}
#endif

#if UNITY_EDITOR
		bool isForcingAlwaysLandscape = SlotsPlayer.getPreferences().GetBool(DebugPrefs.FORCE_WEBGL_LANDSCAPE_IN_PORTRAIT_MODE_GAMES, false);
		if (isForcingAlwaysLandscape)
		{
			// We aren't allowing switching to Portrait due to the EditorLoginSettings flag being set.
			// So just return so we don't do anything in here.
			return;
		}
		
		if (!isInPortraitMode)
		{
			Zynga.Unity.GameViewSizeUtils.switchToInverse();
		}
#else
		// Force device into portrait mode
		UnityEngine.Screen.orientation = UnityEngine.ScreenOrientation.Portrait;
#endif

		if (FreeSpinGame.instance != null || (SlotBaseGame.instance != null && SlotBaseGame.instance.isDoingFreespinsInBasegame()))
		{
			BonusSpinPanel.instance.setToPortraitMode();
		}
#endif
	}

	// Tell the editor/device to switch back to Landscape mode, on WebGL this doesn't do anything
	public static void switchToLandscape()
	{
		// This function will do nothing on WebGL.
#if !UNITY_WEBGL
#if UNITY_EDITOR
		// When switching back check if we are already in a landscape resolution, since
		// someone could have changed the resolution dropdown
		if (isInPortraitMode)
		{
			Zynga.Unity.GameViewSizeUtils.switchToInverse();
		}
#elif UNITY_ANDROID
		if (androidAutoOrientSwapBackCoroutine != null)
		{
			// Kill the already going coroutine
			RoutineRunner.instance.StopCoroutine(androidAutoOrientSwapBackCoroutine);
			androidAutoOrientSwapBackCoroutine = null;
		}

		if (landscapeOrientationPreference != DeviceOrientation.Unknown)
		{
			if (landscapeOrientationPreference == DeviceOrientation.LandscapeLeft)
			{
				UnityEngine.Screen.orientation = ScreenOrientation.LandscapeLeft;
			}
			else
			{
				UnityEngine.Screen.orientation = ScreenOrientation.LandscapeRight;
			}
		}
		else
		{
			// We don't know what the user's prefered orientation was for landscape, so just force it to one
			UnityEngine.Screen.orientation = ScreenOrientation.LandscapeLeft;
		}
		androidAutoOrientSwapBackCoroutine = RoutineRunner.instance.StartCoroutine(waitForLandscapeOrientationSwapThenSetToAutoRotation());
#else
		// Restore to auto rotation for what we have configured for the app
		UnityEngine.Screen.orientation = UnityEngine.ScreenOrientation.AutoRotation;
#endif
		if (FreeSpinGame.instance != null || (SlotBaseGame.instance != null && SlotBaseGame.instance.isDoingFreespinsInBasegame()))
		{
			BonusSpinPanel.instance.setToLandscapeMode();
		}
#endif
	}

	// Special function to handle android which doesn't force the game back into Landscape when set to
	// UnityEngine.ScreenOrientation.AutoRotation (with just Landscape orientation set for auto) after being in portrait.
	// So instead, we will force the game into an actual landscape orientation and then set it to auto once we know
	// the game isn't in portrait anymore.
	private static IEnumerator waitForLandscapeOrientationSwapThenSetToAutoRotation()
	{
		// Wait until we detect that we are out of portrait and back into some landscape orientation.
		// Don't use the isInPortraitMode property, since we actually want to verify that the resolution
		// change has occured.
		bool isResolutionLandscape = (UnityEngine.Screen.width > UnityEngine.Screen.height);

		while (!isResolutionLandscape)
		{
			yield return null;
			// keep waiting until we are in landscape
			isResolutionLandscape = (UnityEngine.Screen.width > UnityEngine.Screen.height);
		}
		
		// Now we are in landscape change the Screen.orientation to be auto again
		UnityEngine.Screen.orientation = UnityEngine.ScreenOrientation.AutoRotation;

		androidAutoOrientSwapBackCoroutine = null;
	}
	
	// Get the legacy width and height of the virtual screen (before changes made to support portrait mode)
	private Vector2int getLegacyVirtualScreenSize(float defaultAspect)
	{
		int newWidth = UnityEngine.Screen.width;
		int newHeight = UnityEngine.Screen.height;
	
		if (defaultAspect > MAX_ASPECT)
		{
			// The screen is too wide for the game to work, so set height to the real screen and then calculate width.
			newHeight = UnityEngine.Screen.height - (UnityEngine.Screen.height % 8);
			if (newHeight > MAX_HEIGHT)
			{
				// This is the maximum height which guarantees a width no larger than 2048 at 1.33 aspect
				newHeight = MAX_HEIGHT;
			}
			newWidth = (int)Mathf.Floor((float)newHeight * MAX_ASPECT); // Calculate naive width based on the max aspect ratio.
			newWidth = newWidth - (newWidth % 8); // Shrink width to the next even multiple of 8 for hw niceness.
		}
		else
		{
			// The screen is too tall for the game to work
			newWidth = UnityEngine.Screen.width - (UnityEngine.Screen.width % 8);
			if (newWidth > MAX_WIDTH)
			{
				// Maximum width of 2048, for texture size constraint
				newWidth = MAX_WIDTH;
			}
			newHeight = (int)Mathf.Floor((float)newWidth / MIN_ASPECT); // Calculate naive height based on the min aspect ratio.
			newHeight = newHeight - (newHeight % 8); // Shrink height to the next even multiple of 8 for hw niceness.
		}
		
		return new Vector2int(newWidth, newHeight);
	}
	
	// Get the width and height of the virtual screen
	private Vector2int getVirtualScreenSize(float defaultAspect)
	{
		int newWidth = UnityEngine.Screen.width;
		int newHeight = UnityEngine.Screen.height;

		int maxWidth = MAX_WIDTH;
		int maxHeight = MAX_HEIGHT;
		
		if (isInPortraitMode)
		{
			maxWidth = MAX_HEIGHT;
			maxHeight = MAX_WIDTH;
		}
	
		if (defaultAspect > MAX_ASPECT)
		{
			if (isInPortraitMode)
			{
				// The screen is too tall for the game to work, so set width to the real screen and then calculate height.
				newWidth = UnityEngine.Screen.width - (UnityEngine.Screen.width % 8);
				if (newWidth > maxWidth)
				{
					newWidth = maxWidth;
				}
				newHeight = (int)Mathf.Floor((float)newWidth * MAX_ASPECT); // Calculate naive height based on the min aspect ratio.
				newHeight = newHeight - (newHeight % 8); // Shrink height to the next even multiple of 8 for hw niceness.
			}
			else
			{
				// The screen is too wide for the game to work, so set height to the real screen and then calculate width.
				newHeight = UnityEngine.Screen.height - (UnityEngine.Screen.height % 8);
				if (newHeight > maxHeight)
				{
					// This is the maximum height which guarantees a width no larger than 2048 at 1.33 aspect
					newHeight = maxHeight;
				}
				newWidth = (int)Mathf.Floor((float)newHeight * MAX_ASPECT); // Calculate naive width based on the max aspect ratio.
				newWidth = newWidth - (newWidth % 8); // Shrink width to the next even multiple of 8 for hw niceness.
			}
		}
		else
		{
			if (isInPortraitMode)
			{
				// The screen is too wide for the game to work, so set height to the real screen and then calculate width.
				newHeight = UnityEngine.Screen.height - (UnityEngine.Screen.height % 8);
				if (newHeight > maxHeight)
				{
					// This is the maximum height which guarantees a width no larger than 2048 at 1.33 aspect
					newHeight = maxHeight;
				}
				newWidth = (int)Mathf.Floor((float)newHeight / MIN_ASPECT); // Calculate naive width based on the max aspect ratio.
				newWidth = newWidth - (newWidth % 8); // Shrink width to the next even multiple of 8 for hw niceness.
			}
			else
			{
				// The screen is too tall for the game to work
				newWidth = UnityEngine.Screen.width - (UnityEngine.Screen.width % 8);
				if (newWidth > maxWidth)
				{
					// Maximum width of 2048, for texture size constraint
					newWidth = maxWidth;
				}
				newHeight = (int)Mathf.Floor((float)newWidth / MIN_ASPECT); // Calculate naive height based on the min aspect ratio.
				newHeight = newHeight - (newHeight % 8); // Shrink height to the next even multiple of 8 for hw niceness.
			}
		}

		return new Vector2int(newWidth, newHeight);
	}
	
	public static IEnumerator callResolutionChangeHandlers()
	{
		if (instance != null && instance.virtualScreenMode)
		{
			// Wait for the virtual rig to be fully created and registered.
			yield return null;
		}
		
		// Enable all components of these script types that were run once and were disabled, so they run again at the new resolution.
		System.Type[] typesToEnable = new System.Type[]
		{
			typeof(UIAnchor),
			typeof(UIStretch),
			typeof(AspectRatioScaler),
			typeof(AspectRatioPositioner),
			typeof(AspectRatioRelativeStretch),
			typeof(AspectRatioRelativeAnchor),
			typeof(SmallDeviceSpriteScaler),
			typeof(OrthoCameraAspectAdjuster)
		};
		
		foreach (System.Type type in typesToEnable)
		{
			foreach (MonoBehaviour script in FindObjectsOfType(type))
			{
				script.enabled = true;
			}
		}

		// Wait to the end of this frame and after the next one so that anchors have time to do their thing.
		yield return null;
		yield return null;
		
		if (Overlay.instance != null && Overlay.instance.top != null)
		{
			Overlay.instance.top.resolutionChangeHandler();
		}
		
		if (MainLobby.instance != null)
		{
			instance.StartCoroutine(MainLobby.instance.resolutionChangeHandler());
		}
		
		if (Loading.instance != null)
		{
			Loading.instance.resolutionChangeHandler();
		}
		
		if (Dialog.instance != null)
		{
			Dialog.instance.resolutionChangeHandler();
		}

		List<System.Type> types = Common.getAllClassTypes("FullcreenScaler");
		for (int i = types.Count; --i >= 0; )
		{
			MethodInfo mi = types[i].GetMethod("adjustToResolution");
			if (mi != null)
			{
				try
				{
					mi.Invoke(null, null);
				}
				catch (System.Exception ex)
				{
					Debug.LogErrorFormat("callResolutionChangeHandlers() : Unable to adjust fullscreen instance {0} : {1}", types[i].Name, ex.ToString());
				}
			}
		}
		
		if (instance != null && instance.onResolutionChangeDelegates != null)
		{
			instance.onResolutionChangeDelegates();
		}
	}

	// Add a single OnResolutionChangeDelegate
	public void addOnResolutionChangeDelegate(OnResolutionChangeDelegate newDelegate)
	{
		onResolutionChangeDelegates += newDelegate;
	}

	// Remove a single OnResolutionChangeDelegate
	public void removeOnResolutionChangeDelegate(OnResolutionChangeDelegate delegateToRemove)
	{
		onResolutionChangeDelegates -= delegateToRemove;
	}

	// Clear all of the OnResolutionChangeDelegates
	public void clearOnResolutionChangeDelegates()
	{
		onResolutionChangeDelegates = null;
	}
	
	// Reset the screen assumed data to force resize code to trigger.
	public void forceResize()
	{
		realScreenWidth = 0;
		realScreenHeight = 0;
	}
	
	// If in virtual screen mode, align the display accordingly.
	// The direction is the offset along the free axis to offset the quad for display,
	// where -1 is all the way aligned to one side, and +1 is all the way aligned to the other.
	public void virtualScreenAlign(float direction = 0f)
	{
		if (renderQuad != null)
		{
			Vector3 newPosition = Vector3.zero;
			if (screenAspect < renderAspect)
			{
				// Screen is too tall
				newPosition.y = ((renderAspect / screenAspect) - 1f) * 0.5f * direction;
			}
			else
			{
				// Screen is too wide
				newPosition.x = (screenAspect - renderAspect) * 0.5f * direction;
			}
			renderQuad.transform.localPosition = newPosition;
		}
	}
	
	// Used by Input.cs to get the virtual mouse position.
	public Vector3 getMousePosition()
	{
		Vector3 pos = UnityEngine.Input.mousePosition;
		Vector2 updatedPos = convertPosition(new Vector2(pos.x, pos.y));
		pos.x = updatedPos.x;
		pos.y = updatedPos.y;
		return pos;
	}
	
	// Used by Input.cs to get the virtual touch position.
	public Touch getTouch(int index)
	{
		Touch touch = new Touch(UnityEngine.Input.GetTouch(index));
		touch.overridePosition(convertPosition(touch.position));
		if (touch.position == TOUCH_MISS)
		{
			touch.cancel();
		}
		return touch;
	}
	
	// Project a point on the MeshCollider representing the virtual screen into the real game content.
	private Vector2 convertPosition(Vector2 pos)
	{
		Ray ray = virtualCamera.ScreenPointToRay(pos);
		RaycastHit hit;
		
		if (touchScreen.Raycast(ray, out hit, Mathf.Infinity))
		{
			pos = hit.textureCoord;
			pos.x *= screenTexture.width;
			pos.y *= screenTexture.height;
		}
		else
		{
			pos = TOUCH_MISS;
		}
		
		return pos;
	}
	
	private void OnDestroy()
	{
		releaseCameras();
		destroyVirtualRig();
	}

	// Prepares the virtual rendering rig
	private void prepareVirtualRenderRig()
	{
		destroyVirtualRig();
		
		screenAspect = (float)UnityEngine.Screen.width / (float)UnityEngine.Screen.height;
		renderAspect = (float)virtualWidth / (float)virtualHeight;

		// Setup render texture material
		screenTexture = new RenderTexture(virtualWidth, virtualHeight, 24);
		screenMaterial = new Material(ShaderCache.find("Zindagi/GUI Texture Opaque")); // Chosen for simplicity out of what we already have in the game.
		screenMaterial.SetTexture("_MainTex", screenTexture);
		
		// Setup camera
		virtualRenderRig = new GameObject("Virtual Rendering");
		virtualRenderRig.transform.parent = this.transform;
		virtualRenderRig.transform.localPosition = Vector3.zero;
		virtualRenderRig.transform.localRotation = Quaternion.identity;
		virtualRenderRig.layer = Layers.ID_VIRTUAL_RENDERING;
		virtualCamera = virtualRenderRig.AddComponent<Camera>();
		virtualCamera.allowHDR = false;
		virtualCamera.allowMSAA = false;
		virtualCamera.backgroundColor = Color.black;
		virtualCamera.clearFlags = CameraClearFlags.SolidColor;
		virtualCamera.cullingMask = Layers.FLAG_VIRTUAL_RENDERING;
		virtualCamera.farClipPlane = 1f;
		virtualCamera.nearClipPlane = -1f;
		virtualCamera.orthographic = true;
		virtualCamera.renderingPath = RenderingPath.VertexLit;
		virtualCamera.useOcclusionCulling = false;
		
		// Setup quad for virtual rendering display
		renderQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		renderQuad.transform.parent = virtualRenderRig.transform;
		renderQuad.transform.localPosition = Vector3.zero;
		renderQuad.transform.localRotation = Quaternion.identity;
		renderQuad.transform.localScale = new Vector3(renderAspect, 1f, 1f);
		renderQuad.layer = Layers.ID_VIRTUAL_RENDERING;
		MeshRenderer quadRenderer = renderQuad.GetComponent<MeshRenderer>();
		quadRenderer.receiveShadows = false;
		quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		quadRenderer.material = screenMaterial;
		touchScreen = renderQuad.GetComponent<MeshCollider>();
		
		// Adjust the quad and the camera to optimally fit the display area
		if (screenAspect < renderAspect)
		{
			// Screen is too tall, arrange so that black bars are on the top/bottom.
			virtualCamera.orthographicSize = (renderAspect / screenAspect) * 0.5f;
		}
		else
		{
			// Screen is too wide, arrange so that black bars are on the left/right.
			virtualCamera.orthographicSize = 0.5f;
		}
		
		hijackCameras();
	}
	
	// Destroys the virtual rendering rig if it exists
	private void destroyVirtualRig()
	{
		releaseCameras();
		
		if (renderQuad != null)
		{
			Destroy(renderQuad);
		}
		if (virtualRenderRig != null)
		{
			Destroy(virtualRenderRig);
		}
		if (screenMaterial != null)
		{
			Destroy(screenMaterial);
		}
		if (screenTexture != null)
		{
			Destroy(screenTexture);
		}
		
		renderQuad = null;
		virtualRenderRig = null;
		virtualCamera = null;
		touchScreen = null;
		screenMaterial = null;
		screenTexture = null;
	}
	
	// Takes all cameras that would be rendering to the screen and hooks them up to the render texture.
	private void hijackCameras()
	{
		foreach (Camera cam in Camera.allCameras)
		{
			if (cam != virtualCamera && cam.targetTexture == null)
			{
				cam.targetTexture = screenTexture;
				gameCameras.Add(cam);
			}
		}
	}
	
	// Releases all cameras that were hijacked so that they once again render directly to the real screen.
	private void releaseCameras()
	{
		foreach (Camera cam in gameCameras)
		{
			if (cam != null)
			{
				cam.targetTexture = null;
			}
		}
		gameCameras.Clear();
	}

#if UNITY_WEBGL
	public void browserResize(int newWidth)
	{
		if (UnityEngine.Screen.fullScreen)
		{
			// Don't resize if we are full screen.
			return;
		}
		
		// Sanity check the new width.
		if (newWidth > WEBGL_MAX_WIDTH)
		{
			newWidth = WEBGL_MAX_WIDTH;
		}
		else if (newWidth < WEBGL_MIN_WIDTH)
		{
			newWidth = WEBGL_MIN_WIDTH;
		}
		
		if (newWidth != UnityEngine.Screen.width)
		{
			// Set the new width to match the containing div
			UnityEngine.Screen.SetResolution(newWidth, WEBGL_FIXED_HEIGHT, false);
		}
	}
#endif

	// Make sure we get the game back into landscape on a reset
	// otherwise if a reset happens in the middle of a portrait section
	// the game could reload into that
	public static void resetStaticClassData()
	{
		if (isInPortraitMode)
		{
			switchToLandscape();
		}
	}
	
	public delegate void OnResolutionChangeDelegate();
}
