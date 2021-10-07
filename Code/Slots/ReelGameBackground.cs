using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;

/**
Enforces a standard scale and position for reel game background images, for consistency.
*/

[ExecuteInEditMode]
public class ReelGameBackground : TICoroutineMonoBehaviour
{
	private class CustomSpinPanelInfo
	{
		public bool isUsingCustomSpinPanel = false;
		public bool isCustomPanelOnNGUICamera = false;
		public BoxCollider2D customPanelBound = null;
		public Camera customPanelNguiCamera = null; // special variable made to handle pb03 freespins, which uses a non-standard camera to render its UI elements
	}

	public Transform gameBackground;
	public Transform background;
	public Transform mask;
	public bool isFreeSpins = false;
	public GameObject wingsPrefab = null;       // If specified, create wings instead of using baked in wings.
	public ReelGameWings wings = null;
	public int wingRenderQueueAdjust = 0;       // Adjustment for the render queue of the wings so they can be pushed on top of other things in the background if needed
	public float wingsZOffset = 0.0f;
	[FormerlySerializedAs("layerID"), SerializeField] public Layers.LayerID wingsLayer = Layers.LayerID.ID_SLOT_FRAME;  // which layer to put the wing on


	[SerializeField] private SkuId skuOfOriginalArt = SkuId.UNKNOWN;    // We need to go the game these assets were original set to work for so we can scale appropriately.
	[SerializeField] private SkuId currentSkuOverride = SkuId.UNKNOWN;  // We need to go the game these assets weren orgamefeginal set to work for so we can scale appropriately.
	[SerializeField] public GameSizeOverrideEnum gameSize = GameSizeOverrideEnum.Default;   // Override for the size of the game, used to ensure backgrounds match for transitions
	[SerializeField] public WingTypeOverrideEnum wingType = WingTypeOverrideEnum.Default;   // Override for the type of wings to use, in case you want them custom for a transition to look right
	[SerializeField] public Transform wingParentTransform = null; // Can be used to explicitly set what object the wings should be parented to when initially created (note they will still be forced to the reel game root after being setup once though)
	
	[SerializeField] private Camera backgroundCamera; // It's possible we could dynmaically grab this, but for now we're just going to drag it in.
	[SerializeField] private Camera[] reelViewportSpecificCameras; // Cameras which have specific viewport settings to position them on the screen, these have to adjust with the game in order to continue to render the same after the game is adjusted
	[Tooltip("Place cameras here that you don't want ReelGameBackground to auto size.  This can be useful if you have something like a transition camera with an irregular size.")] 
	[SerializeField] private Camera[] camerasToNotAutosize;
	[Tooltip("This list of cameras will be scaled using the same uniform scaling that the reel game background does.  This is intended for cameras that are used for RenderTextures.  Note: Cameras should use ideally be built with 4:3 aspect forced using the LockCameraAspect script.")]
	[SerializeField] private Camera[] camerasToScaleWithBackground; 
	[SerializeField] public bool hasStaticReelArea = false; // Only effects games that are using the backgroundCamera in ortho mode.

	[SerializeField] private Transform _challengeGameAttachObj = null; // if needed a child object can be specified to handle additional scaling issues from standard reparenting via ChallengeGameReparentUnderReelGameBackgroundScriptModule
	[SerializeField] private BoxCollider2D scalerBounds = null; //Set this if the default background image size isn't what we actually want to use as the scaling bounds
	[Tooltip("Allows the game to adjust cameras for portrait mode when the game detects a switch to a resolution that is in portrait mode.")]
	[SerializeField] private bool isAllowingPortraitCameras = false;
	[Tooltip("Enabling this will allow the UIBounds code to scale the game to under 1, useful if the reels might not fit the screen otherwise.")]
	[SerializeField] private bool scaleToBoundsImmediately;
	[SerializeField] private bool isIgnoringWingBoundsForAutoScaling = false;
	[System.NonSerialized] public bool isCreatingDebugCollidersForForceUsingUIBoundsScaling = false;
	[System.NonSerialized] public Vector2 overlayTopMinUsed;
	[System.NonSerialized] public Vector2 overlayTopMaxUsed;

	private float currentVerticalSpacingModifierAppliedToReelCameras = -1.0f;

	private bool isForcingUpdate = false; // Allows for checks to see if the background has already been set for the resolution to be skipped, useful if something may modify the layout without changing aspect ratio
	
	// Used to track what aspect ratio UIBounds code has already been run for, 
	// this ensures we don't run the code more than we have to if ReelGameBackground 
	// is already setup for the correct aspect ratio.  Some examples when this might
	// happen are sometimes when entering freespins
	private float currentUIBoundsAspectRatio = 0.0f; 

	/// Base games.
	private const float BASE_SCALE_BAKED_WINGS_X = 31.0f;
	private const float BASE_SCALE_X = 22.3f;
	private const float BASE_SCALE_Y = 11.84f;

	private const float BASE_POS_Y_OLD_CENTERING = 1.14f;
	// Base games Ortho camera at 4.1
	private const float ORTHO_CAMERA_SIZE = 4.1f;

	/// Old method.
	/// Free spin games.
	private const float FS_SCALE_BAKED_WINGS_X = 31.0f;
	private const float FS_SCALE_X = 22.3f;
	private const float FS_SCALE_Y = 13.1f;
	private const float FS_POS_Y_OLD_CENTERING = 1.75f;

	// Challenge games
	private const float CHALLENGE_SCALE_BAKED_WINGS_X = 31.0f;
	private const float CHALLENGE_SCALE_X = 22.1f;
	private const float CHALLENGE_SCALE_Y = 16.6f;
	private const float CHALLENGE_POS_Y = 0.04f;

	private const float POS_Z = 10.0f;

	private TICoroutine scaleToUIBoundsWithFrameDelayCoroutine; // check to make sure we don't try to recalculate scale before previous calculation done
	private float _scalePercent = 1.0f;
	private float gameSizeBackgroundScaleAdjustment; // This tracks how the gameSize alterations adjusted the scale of the background, needed to correctly adjust cameras that are scaling with the background
	private GameObject reelSizerCenter;
	private BoxCollider2D reelSizer;
	private float currentCameraSize = ORTHO_CAMERA_SIZE;
	private List<Camera> orthographicCameras = new List<Camera>();

	private List<GameObject> reelBackgroundParents = new List<GameObject>();
	private List<GameObject> cameraParents = new List<GameObject>();
	private GameObject cameraAndBackgroundParent = null;
	private bool areDebugReelSizerCenterObjectsMade = false;
	private List<GameObject> reelSizerCenterDebugObjectsList = new List<GameObject>();
	private Vector2 boundsTestStartingPayBoxSize;

	public bool isFirstUpdateCalled
	{
		get { return _isFirstUpdateCalled; }
	}
	private bool _isFirstUpdateCalled = false;
	
	public bool isScalingComplete
	{
		get { return _isScalingComplete; }
	}
	private bool _isScalingComplete = false;

	public float scalePercent
	{
		get
		{
			return _scalePercent;
		}
	}

	// returns either this GameObject or a special one intended as the challenge game attachment point via ChallengeGameReparentUnderReelGameBackgroundScriptModule
	public Transform challengeGameAttachTransform
	{
		get
		{
			if (_challengeGameAttachObj != null)
			{
				return _challengeGameAttachObj;
			}
			else
			{
				return transform;
			}
		}
	}

	private bool isOverlayAndSpinPanelAspectRatioUpdateRequiredInEditor = false;

	// Controlled by ReelGameBackgroundEditor toggle to allow us to test the UI Bounds scaling while the game isn't running by force creating the UI
	public bool isForceUsingUIBoundsScaling
	{
		get;
		private set;
	}

	// Controlled by ReelGameBackgroundEditor toggle to allow the special win overlay to be toggled on and off while the game is running to verify things appear right
	public bool isForceShowingSpecialWinOverlay
	{
		get;
		private set;
	}

	public bool isUsingReelViewportSpecificCameras
	{
		get
		{
			return isUsingOrthoCameras && backgroundCamera != null && reelViewportSpecificCameras != null && reelViewportSpecificCameras.Length > 0;
		}
	}

	private bool isNewCentering
	{
		get
		{
			if (parentGame != null)
			{
				return parentGame.isNewCentering;
			}
			return false;
		}
	}

	private bool shouldAccountForSpecialOverlay
	{
		get
		{
			if (isForceShowingSpecialWinOverlay)
			{
				// these should already be checked and shouldn't be possible if you toggle this on, but check again just in case
				return
					gameSize == GameSizeOverrideEnum.Basegame &&
					!parentGame.isFreeSpinGame() &&
					!parentGame.isDoingFreespinsInBasegame()&&
					!hasStaticReelArea;
			}

			return
				SpinPanel.instance != null &&
				SpinPanel.instance.isShowingSpecialWinOverlay &&
				gameSize == GameSizeOverrideEnum.Basegame &&
				!parentGame.isFreeSpinGame() &&
				!parentGame.isDoingFreespinsInBasegame()&&
				!hasStaticReelArea;
		}
	}

	private float BASE_POS_Y
	{
		get { return isNewCentering ? 0.0f : BASE_POS_Y_OLD_CENTERING; }
	}

	private float FS_POS_Y
	{
		get { return isNewCentering ? 0.0f : FS_POS_Y_OLD_CENTERING; }
	}

	/// Enum for size overrides
	public enum GameSizeOverrideEnum
	{
		Default = 0,
		Basegame = 1,
		Freespins = 2,
		Fullscreen = 3
	}

	public enum WingTypeOverrideEnum
	{
		Default = 0,
		Basegame = 1,
		Freespins = 2,
		Fullscreen = 3
	}

	/// Cache the applicable values for cleaner code.
	private float gameScaleX = 0;
	private float gameScaleY = 0;
	private float wingScaleX = 0;       // separate value for wings so they can be scaled apart from the scale of the game itself 
	private float wingScaleY = 0;       // separate value for wings so they can be scaled apart from the scale of the game itself 
	private float gamePosY = 0;
	private float wingPosY = 0;         // y position of the wings so they can be positioned to follow an override
	private float wingPosX = 0;         // x position of the wings so they can be positioned to follow an override

	private ReelGame parentGame = null;
	
	private bool keepUpdating = true;

	private BackgroundSizeData getBackgroundSizeDataForSku(SkuId sku)
	{
		if (skuToBackgroundSizeData == null)
		{
			return null;
		}

		BackgroundSizeData skuSizeData = null;
		if (skuToBackgroundSizeData.ContainsKey(sku))
		{
			skuSizeData = skuToBackgroundSizeData[sku];
		}
		else
		{
			Debug.LogErrorFormat(this, "Size for SKU isn't set up yet for {0}. Defaulting to HIR",
				ReelGame.activeGame != null ? ReelGame.activeGame.name : name);
			if (skuToBackgroundSizeData.ContainsKey(SkuId.HIR))
			{
				skuSizeData = skuToBackgroundSizeData[SkuId.HIR];
			}
			else
			{
				Debug.LogError("There is no HIR data in here. Giving up");
				return null;
			}
		}
		return skuSizeData;
	}

	public bool isUsingOrthoCameras
	{
		get
		{
			return backgroundCamera != null && backgroundCamera.orthographic;
		}
	}

	// Returns info about if a custom spin panel is being used, and if so
	// properties of that custom spin panel needed for UI Bounds adjustment
	private CustomSpinPanelInfo getCustomSpinPanelInfo()
	{
		CustomSpinPanelInfo customSpinPanelInfo = new CustomSpinPanelInfo();

		BoxCollider2D customPanelBound = null;
		for (int i = 0; i < parentGame.cachedAttachedSlotModules.Count; i++)
		{
			if (parentGame.cachedAttachedSlotModules[i].needsToUseACustomSpinPanelSizer())
			{
				customPanelBound = parentGame.cachedAttachedSlotModules[i].getCustomSpinPanelSizer();
				if (customPanelBound != null)
				{
					customSpinPanelInfo.customPanelBound = customPanelBound;

					int layer = customPanelBound.gameObject.layer;
					if (layer != Layers.ID_NGUI &&
						layer != Layers.ID_NGUI_IGNORE_BOUNDS &&
						layer != Layers.ID_NGUI_OVERLAY &&
						layer != Layers.ID_NGUI_LIST_OVERLAY &&
						layer != Layers.ID_NGUI_PERSPECTIVE)
					{
						// custom spin panel is on a reel game camera
						customSpinPanelInfo.isCustomPanelOnNGUICamera = false;
					}
					else
					{
						// custom spin panel is on an NGUI camera
						customSpinPanelInfo.isCustomPanelOnNGUICamera = true;

						// we need to figure out what camera is actually rendering this object
						// in the case of pb03, which is the only game which is using this it is
						// 0 Camera which normally has nothing to do with reel game UI
						customSpinPanelInfo.customPanelNguiCamera = NGUIExt.getObjectCamera(customPanelBound.gameObject);
					}

					customSpinPanelInfo.isUsingCustomSpinPanel = true; //If we're using a custom spin panel then we're not gonna do the conversion into NGUI space
				}
				break; //Breaking now since there should only be one custom spin panel module per game
			}
		}

		return customSpinPanelInfo;
	}

	// Debug funciton to create a debug box with the passed name.  Can be used
	// to create box visulatizations of the spin panel and top overlay
	private GameObject createDebugBox(string name, Vector3 position, Vector2 size)
	{
		GameObject boxObject = new GameObject(name);
		boxObject.transform.position = position;
		BoxCollider2D newCollider = boxObject.AddComponent<BoxCollider2D>();
		newCollider.size = size;
		reelSizerCenterDebugObjectsList.Add(boxObject);
		return boxObject;
	}

	// Debug function to allow the reel sizer to be duplicated so we can view what it looks like
	// and is doing as it is used to determine the reel size based on the UI bounds.
	private GameObject duplicateReelSizerCenter(string cloneName)
	{
		if (reelSizerCenter != null)
		{
			GameObject clonedObject = GameObject.Instantiate(reelSizerCenter);
			clonedObject.transform.parent = reelSizerCenter.transform.parent;
			clonedObject.transform.localScale = reelSizerCenter.transform.localScale;
			clonedObject.transform.parent = null;
			clonedObject.transform.position = reelSizerCenter.transform.position;
			clonedObject.name = cloneName;
			reelSizerCenterDebugObjectsList.Add(clonedObject);
			return clonedObject;
		}
		else
		{
			return null;
		}
	}

	private void scaleToUIBounds()
	{
		float screenAspect = (float)Screen.width / (float)Screen.height;
		
#if UNITY_EDITOR
		if (!Application.isPlaying && isForceUsingUIBoundsScaling)
		{
			// First check if the Spin Panel and Overlay were just created and need to have Awake called
			// as well as running their Update in order to size correctly
			if (isOverlayAndSpinPanelAspectRatioUpdateRequiredInEditor)
			{
				forceRunAspectRatioScalersAndPositioners(Overlay.instance.gameObject, true);
				forceRunAspectRatioScalersAndPositioners(SpinPanel.instance.gameObject, true);
				isOverlayAndSpinPanelAspectRatioUpdateRequiredInEditor = false;
			}
			else if (screenAspect != currentUIBoundsAspectRatio)
			{
				// If the aspect ratio has changed, and we've already made and update the UI,
				// then we should run just the Update to ensure they are the correct size for the new aspect ratio
				forceRunAspectRatioScalersAndPositioners(Overlay.instance.gameObject, false);
				forceRunAspectRatioScalersAndPositioners(SpinPanel.instance.gameObject, false);
			}
		}
#endif
		
		SpinPanel.instance.setBonusPanelToSmallVersion();
		SpinPanel.instance.setNormalPanelToSmallVersion();

		if (wingType != WingTypeOverrideEnum.Fullscreen)
		{
			setWingsTo(WingTypeOverrideEnum.Fullscreen, wingsLayer);
		}

		// Force update the reel area limiters before we do our calculations for dynamic scaling
		Overlay.instance.updateReelLimits(isFreeSpins || gameSize == GameSizeOverrideEnum.Freespins);
		InGameFeatureContainer.updateAllContainerBounds(); 

		if (reelSizerCenter == null)
		{
			reelSizerCenter = new GameObject("Reel Sizer Center");
		}

		if (reelSizer == null)
		{
			reelSizer = reelSizerCenter.AddComponent<BoxCollider2D>();
		}
		else
		{
			reelSizer.enabled = true;
		}

		CustomSpinPanelInfo customSpinPanelInfo = getCustomSpinPanelInfo();
		
		float cameraHeight = backgroundCamera.orthographicSize * 2;

		Bounds bounds = new Bounds(backgroundCamera.transform.position, new Vector3(cameraHeight * screenAspect, cameraHeight, 0));

		overlayTopMinUsed = Overlay.instance.overlayTopMin;
		overlayTopMaxUsed = Overlay.instance.overlayTopMax;

		// Calculate overlay extent points in game camera world space
		Vector3 overlayTopMinScreenPoint = Overlay.instance.uiCamera.WorldToScreenPoint(Overlay.instance.overlayTopMin);
		Vector3 overlayTopMinGameWorldPoint = backgroundCamera.ScreenToWorldPoint(overlayTopMinScreenPoint);
		Vector3 overlayTopMaxScreenPoint = Overlay.instance.uiCamera.WorldToScreenPoint(Overlay.instance.overlayTopMax);
		Vector3 overlayTopMaxGameWorldPoint = backgroundCamera.ScreenToWorldPoint(overlayTopMaxScreenPoint);
		
		// Determine what to use as the top cutoff
		Vector2 screenTopCutoff;
		// isFreespins isn't always manually set so lets also check the gameSize
		if (isFreeSpins || gameSize == GameSizeOverrideEnum.Freespins)
		{
			screenTopCutoff = (Vector2)bounds.max;
			// For the x position make this store the min position so we can use it
			// for the left edge of the screen (for calculations we'll do below)
			screenTopCutoff.x = bounds.min.x;

			// if we are in portrait mode we should check for the Screen safeArea in order to determine
			// what the cut off should be
			if (ResolutionChangeHandler.isInPortraitMode)
			{
				float normalizedPortraitModeSafeAreaOffset = 0.0f;
#if UNITY_EDITOR
				// In the editor we will use the normalizedPortraitModeSafeAreaHeight setting from EditorLoginSettings
				// to simulate the safe area
				normalizedPortraitModeSafeAreaOffset = PlayerPrefsCache.GetFloat(DebugPrefs.NORMALIZED_PORTRAIT_MODE_SAFE_AREA_OFFSET, 0.0f);
#else
				// if we are on 2017 or newer we can use Unity's Screen.safeArea to determine the safe top area
				normalizedPortraitModeSafeAreaOffset = UnityEngine.Screen.safeArea.y / Screen.height;
#endif
				// Adjust down by the top notch area so the top is now within the safe area
				screenTopCutoff.y -= (bounds.extents.y * 2) * normalizedPortraitModeSafeAreaOffset;
			}
		}
		else
		{
			// Not freespins so use the bottom of the UI overlay as the cutoff
			screenTopCutoff = (Vector2)overlayTopMinGameWorldPoint;
		}

		Vector3 spinPanelMax;
		if (customSpinPanelInfo.isUsingCustomSpinPanel && customSpinPanelInfo.customPanelBound != null)
		{
			if (customSpinPanelInfo.isCustomPanelOnNGUICamera)
			{
				// custom spin panel is NGUI space, need to conert it to game space
				Vector3 customSpinPanelMaxInScreenSpace = customSpinPanelInfo.customPanelNguiCamera.WorldToScreenPoint(customSpinPanelInfo.customPanelBound.bounds.max);
				spinPanelMax = backgroundCamera.ScreenToWorldPoint(customSpinPanelMaxInScreenSpace);
			}
			else
			{
				// no conversion needed, custom spin panel is already in game space
				spinPanelMax = customSpinPanelInfo.customPanelBound.bounds.max;
			}
		}
		else
		{
			// using standard NGUI spin panel, so convert it to game space
			Vector3 spinPanelMaxInScreenSpace = SpinPanel.instance.uiCamera.WorldToScreenPoint(SpinPanel.reelBoundsMax);
			spinPanelMax = backgroundCamera.ScreenToWorldPoint(spinPanelMaxInScreenSpace);
		}

		Vector2 newCenter = new Vector2((overlayTopMinGameWorldPoint.x + overlayTopMaxGameWorldPoint.x) / 2, (screenTopCutoff.y + spinPanelMax.y) / 2);

		if (background != null && background.gameObject.activeInHierarchy)
		{
			reelSizerCenter.transform.parent = background;
		}
		else
		{
			reelSizerCenter.transform.parent = this.transform;
		}

		reelSizerCenter.transform.localPosition = Vector3.zero;
		reelSizerCenter.transform.localScale = Vector3.one;
		reelSizer.size = Vector2.one;
		Vector2 scaleToUIBoundsPosOffset = Vector2.zero;

		if (scalerBounds != null)
		{
			scalerBounds.enabled = true;
			reelSizer.size = new Vector2(scalerBounds.bounds.extents.x / reelSizer.bounds.extents.x, scalerBounds.bounds.extents.y / reelSizer.bounds.extents.y);
			// Determine the offset to use from the custom box collider by determining the offset that collider
			// has from the reelSizerCenter which is currently at the local center
			scaleToUIBoundsPosOffset = reelSizerCenter.transform.position - scalerBounds.bounds.center;
		}

		float scaleIncreasePercentageHorizontal = 1;
		
		// Check for the wings and if they exist and aren't hidden then look for our horizontal bounds.
		if (wings != null && wingsLayer != Layers.LayerID.ID_HIDDEN && !isIgnoringWingBoundsForAutoScaling)
		{
			float reelBoundsMinX = reelSizer.bounds.min.x;
			BoxCollider2D wingsBounds = wings.leftMeshFilter.gameObject.AddComponent<BoxCollider2D>();
			float wingBoundsMinX = wingsBounds.bounds.max.x;
			float featurePanelLeft = calculateHorizontalFeaturePanelEdge();
			if (featurePanelLeft > Mathf.NegativeInfinity)
			{
				wingBoundsMinX = Mathf.Max(wingBoundsMinX, featurePanelLeft);
			}
			float distanceToLeft = reelBoundsMinX - wingBoundsMinX;
			scaleIncreasePercentageHorizontal = (distanceToLeft / reelSizer.bounds.extents.x) + 1;
			wingsBounds.enabled = false;
		}

		if (isCreatingDebugCollidersForForceUsingUIBoundsScaling && !areDebugReelSizerCenterObjectsMade)
		{
			duplicateReelSizerCenter("Reel Sizer Center - PRE newCenter");
		}
		
		reelSizerCenter.transform.position = newCenter;

		if (isCreatingDebugCollidersForForceUsingUIBoundsScaling && !areDebugReelSizerCenterObjectsMade)
		{
			duplicateReelSizerCenter("Reel Sizer Center - POST newCenter");
		}
		
		Vector3 reelBoundsMin = reelSizer.bounds.min;
		Vector3 spinPanelTop = spinPanelMax;

		float distanceToBottom = reelBoundsMin.y - spinPanelTop.y; //Since we're centered now, the distance to the spin panel and overlay should be equal
		float scaleIncreasePercentageVertical = (distanceToBottom / reelSizer.bounds.extents.y) + 1;
		
		//Leaving this if check here for now even though it appears to be exact inverse of the wings != null check above
		//Moving this to the else part changes the calculation since we calculate a new centre value for reelSizerCenter
		//which affects the bounds of the reelSizer boxcollider.
		if (wings == null || wingsLayer == Layers.LayerID.ID_HIDDEN || isIgnoringWingBoundsForAutoScaling)
		{
			float screenSideCutoff = screenTopCutoff.x;
			float featurePanelLeft = calculateHorizontalFeaturePanelEdge();
			if (featurePanelLeft > Mathf.NegativeInfinity)
			{
				screenSideCutoff = Mathf.Max(screenSideCutoff, featurePanelLeft);
			}
			float distanceToLeft = reelBoundsMin.x - screenSideCutoff;
			scaleIncreasePercentageHorizontal = (distanceToLeft / reelSizer.bounds.extents.x) + 1;
		}

		float actualScaleIncreasePercent = scaleIncreasePercentageVertical;

		//Take whichever direction's scale is smaller
		if (scaleIncreasePercentageHorizontal < scaleIncreasePercentageVertical)
		{
			actualScaleIncreasePercent = scaleIncreasePercentageHorizontal;
		}
		
		actualScaleIncreasePercent = (float)System.Math.Round(actualScaleIncreasePercent, 3);

		if (scalerBounds != null)
		{
			scalerBounds.enabled = false;
		}
		
		//Only shrink the game if we are taking into account the in game feature panels edge which enabled by DEV_ENABLE_MACHINE_SCALER_UPDATE
		//or if the reelgame has the SwitchGameToPortraitModeOnDeviceModule module which means the game needs to shrink down on Webgl
		bool shouldScaleDown = (parentGame != null && parentGame.hasPortraitModeOnDeviceModule()) || Glb.ENABLE_MACHINE_SCALER_UPDATE;

		if (actualScaleIncreasePercent <= 0)
		{
			Debug.LogError("ReelGameBackground.scaleToUIBounds() - actualScaleIncreasePercent = " + actualScaleIncreasePercent + "; this is less than or equal to zero and will not work correctly.  Forcing the scaling to 1!  Should investigate why scale is broken.");
			// Default to 1 which means the game isn't going to be scaled
			actualScaleIncreasePercent = 1.0f;
		}
		
		// Only do the scaling if our scale actually changed
		// Shrinking the game will be allowed so long as the increase precent is still positive
		//otherwise if the actualScaleIncreasePercent is 1 or greater then just apply the scale regardless of the shouldScaleDown value
		if (actualScaleIncreasePercent != Mathf.Infinity && ((shouldScaleDown && actualScaleIncreasePercent > 0) || actualScaleIncreasePercent >= 1))
		{
			foreach (Camera currentCamera in camerasToScaleWithBackground)
			{
				currentCamera.orthographicSize = ORTHO_CAMERA_SIZE * gameSizeBackgroundScaleAdjustment * actualScaleIncreasePercent;
			}
		
			Vector3 newScale = new Vector3(gameObject.transform.localScale.x * actualScaleIncreasePercent, gameObject.transform.localScale.y * actualScaleIncreasePercent, gameObject.transform.localScale.z);
			gameObject.transform.localScale = newScale;
			float oldScalePercent = _scalePercent;
			_scalePercent = actualScaleIncreasePercent;
			parentGame.updateVerticalSpacingWorld();

			if (Application.isPlaying)
			{
				parentGame.payBoxSize = parentGame.startingPayBoxSize;
			}
			else
			{
				parentGame.payBoxSize = boundsTestStartingPayBoxSize;
			}
			
			parentGame.payBoxSize *= getVerticalSpacingModifier();
			if (isFreeSpins)
			{
				TumbleFreeSpinGame tumbleParentFreespins = parentGame as TumbleFreeSpinGame;
				if (tumbleParentFreespins != null && oldScalePercent != _scalePercent)
				{
					if (tumbleParentFreespins.paylineCamera != null)
					{
						Vector3 currentPaylineCameraPos = tumbleParentFreespins.paylineCamera.transform.localPosition;
						tumbleParentFreespins.paylineCamera.transform.localPosition = new Vector3(currentPaylineCameraPos.x, currentPaylineCameraPos.y * _scalePercent, currentPaylineCameraPos.z);
					}
				}
			}
			else
			{
				TumbleSlotBaseGame tumbleParentBasegame = parentGame as TumbleSlotBaseGame;
				if (tumbleParentBasegame != null && oldScalePercent != _scalePercent)
				{
					if (tumbleParentBasegame.paylineCamera != null)
					{
						Vector3 currentPaylinCameraPos = tumbleParentBasegame.paylineCamera.transform.localPosition;
						tumbleParentBasegame.paylineCamera.transform.localPosition = new Vector3(currentPaylinCameraPos.x, currentPaylinCameraPos.y * _scalePercent, currentPaylinCameraPos.z);
					}
				}
			}
		}
		else
		{
			if (!reelSizerCenter.activeInHierarchy)
			{
				keepUpdating = true;
			}
		}

		if (isCreatingDebugCollidersForForceUsingUIBoundsScaling && !areDebugReelSizerCenterObjectsMade)
		{
			duplicateReelSizerCenter("Reel Sizer Center - POST size change");
		}

		CommonTransform.setX(gameObject.transform, newCenter.x + scaleToUIBoundsPosOffset.x * actualScaleIncreasePercent, Space.World);
		CommonTransform.setY(gameObject.transform, newCenter.y + scaleToUIBoundsPosOffset.y * actualScaleIncreasePercent, Space.World);

		//If we haven't created our hierarchy lists yet then do that
		if (reelBackgroundParents.Count == 0 && cameraParents.Count == 0 && cameraAndBackgroundParent == null)
		{
			Transform currentBackgroundParent = gameObject.transform.parent;
			Transform currentCameraObjectParent = backgroundCamera.gameObject.transform;

			//Keep moving up the hierarchy until we've found a common parent between the background camera and background object
			//or stop once we've ran out of parents on both paths
			while (cameraAndBackgroundParent == null && (currentBackgroundParent != null || currentCameraObjectParent != null))
			{
				if (currentBackgroundParent != null)
				{
					reelBackgroundParents.Add(currentBackgroundParent.gameObject);
					if (cameraParents.Contains(currentBackgroundParent.gameObject))
					{
						cameraAndBackgroundParent = currentBackgroundParent.gameObject;
						break;
					}
				}

				if (currentCameraObjectParent != null)
				{
					cameraParents.Add(currentCameraObjectParent.gameObject);
					if (reelBackgroundParents.Contains(currentCameraObjectParent.gameObject))
					{
						cameraAndBackgroundParent = currentCameraObjectParent.gameObject;
						break;
					}
				}

				//Move upwards if we haven't found a common parent yet
				if (currentBackgroundParent != null)
				{
					currentBackgroundParent = currentBackgroundParent.transform.parent;
				}

				if (currentCameraObjectParent != null)
				{
					currentCameraObjectParent = currentCameraObjectParent.transform.parent;
				}
			}
		}
		
		//Once we know the common parent between the background camera and the background script object, we can trasverse upwards and finding any relevant transform offsets
		if (cameraAndBackgroundParent != null)
		{
			float yCenterOffset = 0.0f;
			float xCenterOffset = 0.0f;
			
			for (int i = 0; i < reelBackgroundParents.Count; i++)
			{
				if (reelBackgroundParents[i] == cameraAndBackgroundParent)
				{
					//Stop once we hit the common parent
					break;
				}
				else
				{
					xCenterOffset += reelBackgroundParents[i].transform.localPosition.x * reelBackgroundParents[i].transform.parent.lossyScale.x;
					yCenterOffset += reelBackgroundParents[i].transform.localPosition.y * reelBackgroundParents[i].transform.parent.lossyScale.y;
				}
			}

			xCenterOffset *= gameObject.transform.parent.lossyScale.x;
			yCenterOffset *= gameObject.transform.parent.lossyScale.y;
			gameObject.transform.position += new Vector3(xCenterOffset, yCenterOffset, 0.0f);
			reelBackgroundParents.Clear();
			cameraParents.Clear();
			cameraAndBackgroundParent = null;
		}

		reelSizerCenter.transform.localPosition = Vector3.zero;
		reelSizer.enabled = false;

		if (isCreatingDebugCollidersForForceUsingUIBoundsScaling)
		{
			areDebugReelSizerCenterObjectsMade = true;
		}

		currentUIBoundsAspectRatio = screenAspect;

		if (Application.isPlaying)
		{
			_isScalingComplete = true;
		}
	}

	private float calculateHorizontalFeaturePanelEdge()
	{
		// We'll ignore this if the flag to use it isn't enabled or if the game is
		// using a separate freespins prefab, since in that case we shouldn't be showing
		// the standard UI and can ignore these bounds.
		if (!Glb.ENABLE_MACHINE_SCALER_UPDATE || parentGame.isFreeSpinGame())
		{
			return Mathf.NegativeInfinity;
		}
		
		InGameFeatureContainer featureContainerRight = InGameFeatureContainer.getFeatureContainer(InGameFeatureContainer.ScreenPosition.RIGHT);
		InGameFeatureContainer featureContainerRightBottom = InGameFeatureContainer.getFeatureContainer(InGameFeatureContainer.ScreenPosition.RIGHT_BOTTOM);
		InGameFeatureContainer featureContainerLeft = InGameFeatureContainer.getFeatureContainer(InGameFeatureContainer.ScreenPosition.LEFT);
		float leftOverlap = Mathf.NegativeInfinity;
		float rightOverlap = Mathf.NegativeInfinity;

		if ((featureContainerRight.featureCount() > 0 && featureContainerRight.isAnyChildActive()) 
			|| (featureContainerRightBottom.featureCount() > 0 && featureContainerRightBottom.isAnyChildActive()))
		{
			Vector3 rightFeaturesMinScreenPoint = Overlay.instance.uiCamera.WorldToScreenPoint(featureContainerRight.bounds);
			float rightScreenOffset = Screen.width - rightFeaturesMinScreenPoint.x;
			Vector3 rightOffset = new Vector3(rightScreenOffset, rightFeaturesMinScreenPoint.y, rightFeaturesMinScreenPoint.z);
			Vector3 rightFeaturesMinGameWorldPoint = backgroundCamera.ScreenToWorldPoint(rightOffset);
			rightOverlap = rightFeaturesMinGameWorldPoint.x;
		}

		if (featureContainerLeft.featureCount() > 0 && featureContainerLeft.isAnyChildActive())
		{
			Vector3 leftFeaturesMaxScreenPoint = Overlay.instance.uiCamera.WorldToScreenPoint(featureContainerLeft.bounds);
			Vector3 leftFeaturesMaxGameWorldPoint = backgroundCamera.ScreenToWorldPoint(leftFeaturesMaxScreenPoint);
			leftOverlap = leftFeaturesMaxGameWorldPoint.x;
		}
		
		return Mathf.Max(leftOverlap, rightOverlap);
	}

	// Helper funciton to be used by ReelGameBackgroundMenuHelper in order to tell what games
	// will have parent x-offsets factored in when scaleToUIBounds is called.
	public float getParentLocalXOffset()
	{
		if (backgroundCamera == null)
		{
			// if no background camera the offset isn't going to be factored in since this game isn't going to auto scale
			return 0.0f;
		}
		
		//If we haven't created our hierarchy lists yet then do that
		if (reelBackgroundParents.Count == 0 && cameraParents.Count == 0 && cameraAndBackgroundParent == null)
		{
			Transform currentBackgroundParent = gameObject.transform.parent;
			Transform currentCameraObjectParent = backgroundCamera.gameObject.transform;

			//Keep moving up the hierarchy until we've found a common parent between the background camera and background object
			//or stop once we've ran out of parents on both paths
			while (cameraAndBackgroundParent == null && (currentBackgroundParent != null || currentCameraObjectParent != null))
			{
				if (currentBackgroundParent != null)
				{
					reelBackgroundParents.Add(currentBackgroundParent.gameObject);
					if (cameraParents.Contains(currentBackgroundParent.gameObject))
					{
						cameraAndBackgroundParent = currentBackgroundParent.gameObject;
						break;
					}
				}

				if (currentCameraObjectParent != null)
				{
					cameraParents.Add(currentCameraObjectParent.gameObject);
					if (reelBackgroundParents.Contains(currentCameraObjectParent.gameObject))
					{
						cameraAndBackgroundParent = currentCameraObjectParent.gameObject;
						break;
					}
				}

				//Move upwards if we haven't found a common parent yet
				if (currentBackgroundParent != null)
				{
					currentBackgroundParent = currentBackgroundParent.transform.parent;
				}

				if (currentCameraObjectParent != null)
				{
					currentCameraObjectParent = currentCameraObjectParent.transform.parent;
				}
			}
		}
		
		//Once we know the common parent between the background camera and the background script object, we can trasverse upwards and finding any relevant transform offsets
		float xCenterOffset = 0.0f;
		if (cameraAndBackgroundParent != null)
		{
			for (int i = 0; i < reelBackgroundParents.Count; i++)
			{
				if (reelBackgroundParents[i] == cameraAndBackgroundParent)
				{
					//Stop once we hit the common parent
					break;
				}
				else
				{
					xCenterOffset += reelBackgroundParents[i].transform.localPosition.x;
				}
			}
		}
		
		reelBackgroundParents.Clear();
		cameraParents.Clear();
		cameraAndBackgroundParent = null;

		return xCenterOffset;
	}

	private IEnumerator scaleToUIBoundsWithFrameDelay()
	{
		// Wait two frames to ensure that UIAnchors and other code has a chance to update with the Camera size changes that might have happened
		// before we perform the UIBounds checks which might rely on anchored elements, like a custom spin panel
		yield return null;
		yield return null;
		scaleToUIBounds();
	}

	void Awake()
	{
		if (background == null)
		{
			Debug.Log("background property is not assigned!", gameObject);
		}

		if (mask == null &&
			SlotBaseGame.instance != null &&
			!SlotBaseGame.instance.isLegacyPlopGame) // we don't need a mask for Plop games because the reels don't actually spin. A mask would only cause problems when plopping and raising them.
		{
			Debug.Log("mask property is not assigned!", gameObject);
		}

		parentGame = CommonGameObject.findComponentInParent("ReelGame", gameObject) as ReelGame;
		if (parentGame != null)
		{
			parentGame.reelGameBackground = this;

			Camera[] parentCameras = parentGame.GetComponentsInChildren<Camera>(true);
			Camera currentCamera;
			for (int i = 0; i < parentCameras.Length; i++)
			{
				currentCamera = parentCameras[i];
			
				bool isViewportSpecificCamera = isUsingReelViewportSpecificCameras && isCameraInReelViewportSpecificCameras(currentCamera);
			
				if (currentCamera.orthographic && !isViewportSpecificCamera && !isCameraInCamerasToNotAutosize(currentCamera) && !isCameraInCamerasToScaleWithBackground(currentCamera))
				{
					orthographicCameras.Add(currentCamera);
				}
			}

			if (parentGame.isNewCentering)
			{
				CommonTransform.setY(transform, 0.0f);
			}
		}
		else
		{
			Debug.LogWarning("Couldn't find a ReelGame to attach this background to.");
		}

		// If a prefab is specified, create wings.
		if (wingsPrefab != null && Application.isPlaying)
		{
			createWingsFromPrefab();
		}

		if (Application.isPlaying)
		{
			// Sets the sizing so we can dynmaically size the art to fit correctly.

			// Get the name of the scene to make sure we're not in the art scenes (They don't want the override to change when it's running)
			UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
			if (activeScene.name.Contains("Art Setup"))
			{
				// Don't change the SKU to make things easier for art.
				currentSkuOverride = skuOfOriginalArt;
			}
			else
			{
				currentSkuOverride = SkuResources.currentSku;
			}

			// register for resolution change events
			if (ResolutionChangeHandler.instance != null)
			{
				ResolutionChangeHandler.instance.addOnResolutionChangeDelegate(onResolutionChange);
			}
		}
		else
		{
			// This will make it easier to work on games in either SKU.
			currentSkuOverride = skuOfOriginalArt;
		}
	}

	void Start()
	{
		// Sometimes we don't need to wait 2 frames before scaling (like in gen84)
		if (Application.isPlaying && scaleToBoundsImmediately)
		{
			scaleToUIBounds();
		}
	}
	
	// Function to generate the wings from the serialized wingsPrefab
	private void createWingsFromPrefab()
	{
		GameObject go = CommonGameObject.instantiate(wingsPrefab) as GameObject;
		wings = go.GetComponent<ReelGameWings>();
		
		// (Scott) This was added as a safer way to handle some special cases where wings would break
		// due to being parented under a reel mover object.  Technically the wings get parented to a different
		// object (the game root) after they are setup once, and it may be safe to just always do that (but to
		// be safe for now I'm going to add this code to allow the setting of an initial wings parent in order
		// to fix games currently having issues with wings being positioned wrong).
		if (wingParentTransform != null)
		{
			wings.transform.parent = wingParentTransform;
		}
		else
		{
			wings.transform.parent = transform.parent;
		}

		wings.transform.localScale = Vector3.one;

		if (Application.isPlaying)
		{
			Renderer[] wingRenderers = wings.gameObject.GetComponentsInChildren<Renderer>();
			foreach (Renderer wingRenderer in wingRenderers)
			{
				wingRenderer.material.renderQueue = wingRenderer.material.renderQueue + wingRenderQueueAdjust;
			}
		}
	}

	private void OnDestroy()
	{
		if (Application.isPlaying)
		{
			// unregister for resolution change events
			if (ResolutionChangeHandler.instance != null)
			{
				ResolutionChangeHandler.instance.removeOnResolutionChangeDelegate(onResolutionChange);
			}
		}
		else
		{
			// Make sure we clean up the UI elements we generated for testing
			if (isForceUsingUIBoundsScaling)
			{
				toggleForceUsingUIBoundsScaling(false);
			}
		}
	}

	public void onResolutionChange()
	{
		// force the ReelGameBackground script to do a single update
		keepUpdating = true;
	}

	public float getVerticalSpacingModifier()
	{
		if (isUsingOrthoCameras)
		{
			GameType type = (GameType)(int)gameSize;
			BackgroundSizeData from = getBackgroundSizeDataForSku(skuOfOriginalArt);
			BackgroundSizeData to = getBackgroundSizeDataForSku(currentSkuOverride);

			// Attempt to fix NRE from https://app.crittercism.com/developers/crash-details/5616f9118d4d8c0a00d07cf0/f3a07daa3a0c71a28958c8a076cffb15ca3c5a35a73a612f604cd6dd
			if (from == null || to == null)
			{
				Debug.LogWarning("ReelGameBackground.getVerticalSpacingModifier() has null to/from data.");
				return 1;
			}

			BackgroundSizeData.SizeData originalGameTypeSizeData = from.getSKUConvertedBackgroundSizeData(from, type, false);
			BackgroundSizeData.SizeData specialOverlayGameTypeSizeData = from.getSKUConvertedBackgroundSizeData(to, type, shouldAccountForSpecialOverlay);
			return (specialOverlayGameTypeSizeData.scale.y / originalGameTypeSizeData.scale.y) * _scalePercent;
		}
		else
		{
			return 1;
		}
	}

	public void updateGameSize(GameSizeOverrideEnum gameSize)
	{
		this.gameSize = gameSize;

		if (gameSize == GameSizeOverrideEnum.Default)
		{
			if (isFreeSpins)
			{
				updateGameSize(GameSizeOverrideEnum.Freespins);
			}
			else
			{
				updateGameSize(GameSizeOverrideEnum.Basegame);
			}
			return;
		}

		if (isUsingOrthoCameras)
		{
			// If we're in the new system.
			// Get the data for the gameType.
			GameType type = (GameType)(int)gameSize;

			BackgroundSizeData from = getBackgroundSizeDataForSku(skuOfOriginalArt);
			BackgroundSizeData to = getBackgroundSizeDataForSku(currentSkuOverride);
			BackgroundSizeData.SizeData gameTypeSizeData = from.getSKUConvertedBackgroundSizeData(to, type, shouldAccountForSpecialOverlay);
			
			// Save out the scaling adjustment we are performing here, so that cameras being scaled with the background can also factor it in
			BackgroundSizeData.SizeData fromSkuSizeData = from.getGameTypeSizeData(type);
			gameSizeBackgroundScaleAdjustment = gameTypeSizeData.scale.x / fromSkuSizeData.scale.x;
			
			// Set the data.
			gameScaleX = gameTypeSizeData.scale.x;
			gameScaleY = gameTypeSizeData.scale.y;
			gamePosY = gameTypeSizeData.position.y;
		}
		else
		{
			gameSizeBackgroundScaleAdjustment = 1.0f;
		
			if (gameSize == GameSizeOverrideEnum.Freespins)
			{
				gameScaleX = (wingsPrefab == null) ? FS_SCALE_BAKED_WINGS_X : FS_SCALE_X;
				gameScaleY = FS_SCALE_Y;
				gamePosY = FS_POS_Y;
			}
			else if (gameSize == GameSizeOverrideEnum.Basegame)
			{
				gameScaleX = (wingsPrefab == null) ? BASE_SCALE_BAKED_WINGS_X : BASE_SCALE_X;
				gameScaleY = BASE_SCALE_Y;
				gamePosY = BASE_POS_Y;
			}
			else
			{
				Debug.LogError("Not sure what to do with the size " + gameSize + ". Giving up.");
				return;
			}
		}

		if (transform.localPosition.y != gamePosY || transform.localPosition.x != 0)
		{
			// Fix the parent position.
			if (isUsingOrthoCameras)
			{
				transform.localPosition = new Vector3(0, gamePosY, transform.localPosition.z);
			}
			else
			{
				transform.localPosition = new Vector3(0, gamePosY, POS_Z);
			}
		}

		if (transform.localScale.x != gameScaleX || transform.localScale.y != gameScaleY)
		{
			// Fix the parent scale.
			transform.localScale = new Vector3(gameScaleX, gameScaleY, 1);
		}

		// after the game has been rescaled we now need to update the viewport y-locations of any of the viewport specific cameras
		if (isUsingOrthoCameras)
		{
			if (reelViewportSpecificCameras != null && backgroundCamera != null)
			{
				float verticalSpacingModifier = getVerticalSpacingModifier();

				for (int i = 0; i < reelViewportSpecificCameras.Length; i++)
				{
					Camera currentCamera = reelViewportSpecificCameras[i];
					if (currentCamera != null)
					{
						// first we need to handle the orthographic size change of the camera
						if (currentVerticalSpacingModifierAppliedToReelCameras != -1.0f)
						{
							// reverse any scaling we've already done
							float inverseVerticalSpacingModifier = 1.0f / currentVerticalSpacingModifierAppliedToReelCameras;
							currentCamera.orthographicSize *= inverseVerticalSpacingModifier;
						}

						// now adjust the camera to the new orthographic size
						currentCamera.orthographicSize *= verticalSpacingModifier;

						Rect currentViewportRect = currentCamera.rect;
						BoxCollider cameraCollider = currentCamera.GetComponent<BoxCollider>();

						// calculate the new y-position using the previous bottom of the camera 
						// recalculated from the collider attached to the camera project into 
						// the new viewport space of the background camera
						if (cameraCollider != null)
						{
							// make sure all the camera colliders are disabled so they don't function as actual colliders
							cameraCollider.enabled = false;

							// if we are in the default view that this game was made for, 
							// then update the camera size with the collider, otherwise it will
							// be changed above by the math used to calculate the new size of the camera
							if (verticalSpacingModifier == 1.0f)
							{
								currentCamera.orthographicSize = cameraCollider.size.y / 2.0f;
							}

							// Ensure that the camera remains centered on the box collider if it has moved 
							// by applying the collider's center to the transform, and then zeroing out the center
							if (cameraCollider.center != Vector3.zero)
							{
								currentCamera.transform.localPosition += cameraCollider.center;
								cameraCollider.center = Vector3.zero;
							}

							// find the bottom middle point of the collider
							Vector3 bottomLeftBoxPos = cameraCollider.center - new Vector3(cameraCollider.size.x / 2.0f, cameraCollider.size.y / 2.0f, 0.0f);
							Vector3 topRightBoxPos = cameraCollider.center + new Vector3(cameraCollider.size.x / 2.0f, cameraCollider.size.y / 2.0f, 0.0f);
							// convert to world space
							bottomLeftBoxPos = currentCamera.transform.TransformPoint(bottomLeftBoxPos);
							topRightBoxPos = currentCamera.transform.TransformPoint(topRightBoxPos);
							// next convert world to viewport space
							Vector3 bottomLeftViewportPoint = backgroundCamera.WorldToViewportPoint(bottomLeftBoxPos);
							Vector3 topRightViewportPoint = backgroundCamera.WorldToViewportPoint(topRightBoxPos);
							float width = topRightViewportPoint.x - bottomLeftViewportPoint.x;
							float height = topRightViewportPoint.y - bottomLeftViewportPoint.y;
							currentCamera.rect = new Rect(bottomLeftViewportPoint.x, bottomLeftViewportPoint.y, width, height);
						}
						else
						{
							Debug.LogError("ReelGameBackground.updateGameSize() - Trying to update a viewport specific camera without an attached BoxCollider!");
						}
					}
				}

				currentVerticalSpacingModifierAppliedToReelCameras = verticalSpacingModifier;
			}
		}

		if (parentGame != null)
		{
			parentGame.updateVerticalSpacingWorld();
			if (Application.isPlaying)
			{
				// We may have scaled the game to fit the screen, lets set up the size of the payboxes in the game. (Still using a werid world space)
				parentGame.payBoxSize = parentGame.startingPayBoxSize;
				parentGame.payBoxSize *= getVerticalSpacingModifier();
			}
		}
	}
	
	// Update function intended to trick the game into running an Update loop
	// when the game isn't running, by modifying something in the scene and then
	// restoring it
	public void forceRunUpdateWhenGameIsNotRunning()
	{
		if (!Application.isPlaying)
		{
			// Force the Update() to run as it would normally by tricking the game into thinking something in the scene was changed
			string prevName = gameObject.name;
			gameObject.name = "UPDATE";
			gameObject.name = prevName;
		}
		else
		{
			Debug.LogError("ReelGameBackground.forceRunUpdate() - This should not be called except when the game isn't running!");
		}
	}

	protected virtual void Update()
	{
		if (!_isFirstUpdateCalled)
		{
			_isFirstUpdateCalled = true;
		}

		// If we are supposed to be updating but cameras are disabled we'll wait till they are enabled to ensure
		// that if we are going to scale to UI bounds that we can actually use the camera's to determine the
		// area to scale the reel area inside of
		if (keepUpdating && backgroundCamera != null && !backgroundCamera.gameObject.activeInHierarchy)
		{
			// we are trying to update but cameras aren't active, delay the update until they are
			return;
		}

		// When the game is actually running and we aren't setting things up we should cancel
		// the update if we have already setup the game for the screen aspect we are in.
		// This prevents the game from running pointless updates which can look weird due
		// to the couple of frame delay before the changes fully take effect.
		if (Application.isPlaying)
		{
			if (!isForcingUpdate)
			{
				float screenAspect = (float) Screen.width / (float) Screen.height;
				if (keepUpdating && Mathf.Abs(screenAspect - currentUIBoundsAspectRatio) <= Mathf.Epsilon)
				{
					// We've already setup the bounds for this aspect ratio so
					// we shouldn't need to run another update.
					keepUpdating = false;
				}
				}
			else
			{
				// Force only works for a single update and then is reset so that the background will do standard checks to ensure it doesn't needlessly update
				isForcingUpdate = false;
			}
		}
	
		if (keepUpdating)
		{
			if (isUsingOrthoCameras)
			{
				if (isAllowingPortraitCameras && ResolutionChangeHandler.isInPortraitMode)
				{
					currentCameraSize = ORTHO_CAMERA_SIZE * (UnityEngine.Screen.height / (float)UnityEngine.Screen.width);
				}
				else
				{
					currentCameraSize = ORTHO_CAMERA_SIZE;
				}
			}
		
			if (hasStaticReelArea)
			{
				// We don't want to resize the reels at all for anything.
				currentSkuOverride = skuOfOriginalArt;
			}
			
			if (skuToBackgroundSizeData == null)
			{
				setBackgroundSizeData();
			}
			
			// handle game scale and position
			if (isFreeSpins)
			{
				gameSize = GameSizeOverrideEnum.Freespins;
			}
			updateGameSize(gameSize);

			enforceDefaultTransform(background);
			enforceDefaultTransform(mask);
			if (gameBackground != null)
			{
				// Make sure that this background isn't attached to this script.
				if (CommonGameObject.isGameObjectChildOf(gameBackground.gameObject, gameObject))
				{
					Debug.LogError("The game background should not be a child of the ReelGameBackground script. Attaching to ReelGame.");
					if (parentGame != null)
					{
						Debug.Log("Attached gameBackground to ReelGame");
						gameBackground.parent = parentGame.transform;
					}
					else
					{
						Debug.LogError("Couldn't attach gameBackround to reelGame.");
					}
				}
				else
				{
					gameBackground.localPosition = new Vector3(0.0f, 0.0f, gameBackground.localPosition.z);
					gameBackground.localScale = new Vector3(
						BackgroundSizeData.fullscreenXSize,
						BackgroundSizeData.fullscreenYSize,
						1.0f);
				}

			}

			if (wings != null)
			{
				setWingsTo(wingType, wingsLayer);
				//Parenting the wings to the ReelGame after we've determined the scale and position because
				//PickMajor freespins are set up differently.
				if (parentGame != null)
				{
					wings.transform.parent = parentGame.transform;
				}
			}

			if (isUsingOrthoCameras)
			{
				// We want to enforce the size of the orthographic camera.
				for (int i = 0; i < orthographicCameras.Count; i++)
				{
					if (orthographicCameras[i] != null)
					{
						orthographicCameras[i].orthographicSize = currentCameraSize;
					}
				}
			}
			
			if (Application.isPlaying)
			{
				//Updating this before the final scaling because there might be a chance this object is turned off and then our box won't scale correctly
				keepUpdating = false;

				// Only need to do this once at runtime.
				if (isUsingOrthoCameras && !isNewCentering && parentGame.gameObject.activeSelf && !isUsingReelViewportSpecificCameras && !hasStaticReelArea) //Should only do this when the application is playing since we're relying on the spin panel and overlay as our bounds
				{
					// a previous update may have already called this coroutine, make sure we wait for it to complete before calling again
					// otherwise we can see strange behavior especially in tumble games if scale changes while initializing
					// see https://jira.corp.zynga.com/browse/HIR-83084
					if (scaleToUIBoundsWithFrameDelayCoroutine == null || scaleToUIBoundsWithFrameDelayCoroutine.isFinished)
					{
						scaleToUIBoundsWithFrameDelayCoroutine = StartCoroutine(scaleToUIBoundsWithFrameDelay());
					}
				}
				else if (isNewCentering)
				{
					if (isFreeSpins || gameSize == GameSizeOverrideEnum.Freespins)
					{
						SpinPanel.instance.setBonusPanelToSmallVersion();
					}
					else
					{
						SpinPanel.instance.setNormalPanelToSmallVersion();
					}

					_isScalingComplete = true;
				}
				else
				{
					if (isFreeSpins || gameSize == GameSizeOverrideEnum.Freespins)
					{
						SpinPanel.instance.freeSpinsBackgroundWingsWidth.gameObject.SetActive(true);
						BonusSpinPanel.instance.holdPaylineMessageBox();
					}
					else
					{
						SpinPanel.instance.backgroundWingsWidth.gameObject.SetActive(true);
						SpinPanel.instance.holdPaylineMessageBox();

						if (Overlay.instance != null)
						{
							Overlay.instance.setBackingSpriteVisible(true);
						}
					}

					_isScalingComplete = true;
				}
			}
			else
			{
				if (isForceUsingUIBoundsScaling)
				{
					if (isUsingOrthoCameras && !isNewCentering && parentGame.gameObject.activeSelf && !isUsingReelViewportSpecificCameras && !hasStaticReelArea) //Should only do this when the application is playing since we're relying on the spin panel and overlay as our bounds
					{
						scaleToUIBounds();							
					}
				}
			}
		}
	}
	
	/// Make sure the given transform is using default scale and position values.
	private void enforceDefaultTransform(Transform trans)
	{
		if (trans != null)
		{
			if (trans.localScale != Vector3.one)
			{
				trans.localScale = Vector3.one;
			}

			if (trans.localPosition != Vector3.zero)
			{
				trans.localPosition = Vector3.zero;
			}
		}
	}

	// Makes the wings tween to be the size they should be based off the wingType.
	public IEnumerator tweenWingsTo(WingTypeOverrideEnum wingType, float duration = 0, iTween.EaseType easeType = iTween.EaseType.linear)
	{
		if (wings != null)
		{
			// Get the new scale of the wings.
			Vector3 newWingScale = Vector3.one;
			Vector3 newWingPos = Vector3.one;
			float parentY = 0;
			if (isUsingOrthoCameras)
			{
				GameType type = (GameType)(int) wingType;
				BackgroundSizeData.SizeData wingTypeSizeData = getBackgroundSizeDataForSku(currentSkuOverride).getWingTypeSizeData(type);
				newWingScale = new Vector3(wingTypeSizeData.scale.x, wingTypeSizeData.scale.y, 1.0f);
				newWingPos = new Vector3(wingTypeSizeData.position.x, 0, 0);
				parentY = wingTypeSizeData.position.y;
			}
			else if (wingType == WingTypeOverrideEnum.Default || wingType == WingTypeOverrideEnum.Basegame)
			{
				newWingScale = new Vector3(BASE_SCALE_Y * .5f, BASE_SCALE_Y, 1f);
				float offset = newWingScale.x * 0.5f + BASE_SCALE_X * 0.5f;
				newWingPos = new Vector3(offset, 0, 0);
				parentY = BASE_POS_Y;
			}
			else if (wingType == WingTypeOverrideEnum.Freespins)
			{
				newWingScale = new Vector3(FS_SCALE_Y * .5f, FS_SCALE_Y, 1f);
				float offset = newWingScale.x * 0.5f + FS_SCALE_X * 0.5f;
				newWingPos = new Vector3(offset, 0, 0);
				parentY = FS_POS_Y;
			}
			else if (wingType == WingTypeOverrideEnum.Fullscreen)
			{
				newWingScale = new Vector3(CHALLENGE_SCALE_Y * .5f, CHALLENGE_SCALE_Y, 1f);
				float offset = newWingScale.x * 0.5f + CHALLENGE_SCALE_X * 0.5f;
				newWingPos = new Vector3(offset, 0, 0);
				parentY = CHALLENGE_POS_Y;
			}
			else
			{
				Debug.LogWarning("WingTypeOverrideEnum unreconized.");
			}

			// Set the right wing.
			iTween.ScaleTo(wings.rightMeshFilter.gameObject, iTween.Hash("scale", newWingScale, "time", duration, "islocal", true, "easetype", easeType));
			iTween.MoveTo(wings.rightMeshFilter.gameObject, iTween.Hash("position", newWingPos, "time", duration, "islocal", true, "easetype", easeType));
			newWingPos.x = -newWingPos.x;
			iTween.ScaleTo(wings.leftMeshFilter.gameObject, iTween.Hash("scale", newWingScale, "time", duration, "islocal", true, "easetype", easeType));
			iTween.MoveTo(wings.leftMeshFilter.gameObject, iTween.Hash("position", newWingPos, "time", duration, "islocal", true, "easetype", easeType));
			// Move the parent object too FS_POS_Y
			iTween.MoveTo(wings.gameObject, iTween.Hash("y", parentY, "time", duration, "islocal", true, "easetype", easeType));
			yield return new TIWaitForSeconds(duration);
		}
	}

	/// Force a reload of the wing textures, since wing loading has become a total mess and you can't trust the game to load the correct wings by default
	public void forceShowFreeSpinWings(int wingVariant = 1)
	{
		if (wingVariant == 1)
		{
			BonusGameWings.forceLoadFreeSpinTextures(wings.leftMeshFilter, wings.rightMeshFilter);
		}
		else if (wingVariant == 2)
		{
			BonusGameWings.forceLoadFreeSpinVariantTwoTextures(wings.leftMeshFilter, wings.rightMeshFilter);
		}
		else
		{
			BonusGameWings.forceLoadFreeSpinVariantThreeTextures(wings.leftMeshFilter, wings.rightMeshFilter);
		}
	}

	// Instantly set the wings to be a certain size.
	public void setWingsTo(WingTypeOverrideEnum wingType, Layers.LayerID layer = Layers.LayerID.ID_SLOT_FRAME)
	{
		if (wings != null)
		{

			// handle the wing scale and position
			if (wingType == WingTypeOverrideEnum.Default)
			{
				if (isFreeSpins)
				{
					setWingsTo(WingTypeOverrideEnum.Freespins, layer);
				}
				else
				{
					setWingsTo(WingTypeOverrideEnum.Basegame, layer);
				}
				return;
			}

			if (isUsingOrthoCameras && getBackgroundSizeDataForSku(currentSkuOverride) != null)
			{
				// If we're in the new system.
				// Get the data for the gameType.
				GameType type = (GameType)(int) wingType;

				BackgroundSizeData.SizeData wingTypeSizeData = getBackgroundSizeDataForSku(currentSkuOverride).getWingTypeSizeData(type);
				// Set the data.
				wingScaleX = wingTypeSizeData.scale.x;
				wingScaleY = wingTypeSizeData.scale.y;
				wingPosX = wingTypeSizeData.position.x;
				wingPosY = wingTypeSizeData.position.y;
			}
			else
			{

				if (wingType == WingTypeOverrideEnum.Freespins)
				{
					wingScaleX = FS_SCALE_Y * .5f;
					wingScaleY = FS_SCALE_Y;
					wingPosX = wingScaleX * 0.5f + FS_SCALE_X * 0.5f;
					wingPosY = FS_POS_Y;
				}
				else if (wingType == WingTypeOverrideEnum.Basegame)
				{
					wingScaleX = BASE_SCALE_Y * 0.5f;
					wingScaleY = BASE_SCALE_Y;
					wingPosX = wingScaleX * 0.5f + BASE_SCALE_X * 0.5f;
					wingPosY = BASE_POS_Y;
				}
				else if (wingType == WingTypeOverrideEnum.Fullscreen)
				{
					wingScaleX = (wingsPrefab == null) ? CHALLENGE_SCALE_BAKED_WINGS_X : CHALLENGE_SCALE_X;
					wingScaleY = CHALLENGE_SCALE_Y;
					wingPosX = wingScaleX * .5f + wingScaleY * .5f;
					wingPosY = CHALLENGE_POS_Y;
				}
				else
				{
					Debug.LogError("Not sure what to do with the size " + wingType + ". Giving up.");
					return;
				}
			}

			// Get the new scale of the wings.
			Vector3 newWingScale = new Vector3(wingScaleX, wingScaleY, 1);
			Vector3 newWingPos = new Vector3(wingPosX, 0, 0);
			Vector3 newWinParentPos = new Vector3(0, wingPosY, POS_Z);
			if (isUsingOrthoCameras)
			{
				// Setting this to the position of the background to keep things consistent.
				newWinParentPos.z = transform.localPosition.z;
			}
			
			// At this point offset the newWinParentPos.z position by the wingsZOffset
			newWinParentPos.z += wingsZOffset;

			// Scale the wings.
			wings.leftMeshFilter.transform.localScale = newWingScale;
			wings.rightMeshFilter.transform.localScale = newWingScale;
			
			// Position the wings.
			wings.rightMeshFilter.transform.localPosition = newWingPos;
			newWingPos.x = -newWingPos.x;
			wings.leftMeshFilter.transform.localPosition = newWingPos;
			wings.transform.localPosition = newWinParentPos;

			// Set some of the wing data in the scene so it's easier to understand what's happening.
			string wingsBaseName = "Reel Game Wings";
			if (wingsPrefab != null)
			{
				wingsBaseName = wingsPrefab.name;
			}
			this.wingType = wingType;
			wings.name = wingsBaseName + " - " + wingType + " Size";

			setWingLayer((int)layer);

		}
	}

	// Allows modification to the wing layer, used to control layering if needed
	public void setWingLayer(int layer)
	{
		if (wings != null)
		{
			CommonGameObject.setLayerRecursively(wings.gameObject, layer);
		}
	}

	// Helper function for force running these components on the generated UI elements
	// for when the game isn't running to ensure that the UI is correctly setup for the
	// debug auto scaling to work as expected.
	private static void forceRunAspectRatioScalersAndPositioners(GameObject passedObject, bool isCallingAwake)
	{
		AspectRatioScaler[] aspectRatioScalers = passedObject.GetComponentsInChildren<AspectRatioScaler>();
		foreach (AspectRatioScaler scaler in aspectRatioScalers)
		{
			if (isCallingAwake)
			{
				scaler.Awake();
			}

			scaler.Update();
		}
		
		AspectRatioPositioner[] aspectRatioPositioners = passedObject.GetComponentsInChildren<AspectRatioPositioner>();
		foreach (AspectRatioPositioner positioner in aspectRatioPositioners)
		{
			if (isCallingAwake)
			{
				positioner.Awake();
			}

			positioner.Update();
		}
	}
	
	// Used by ReelGameBackgroundEditor to force the game to generate UI elements that we can use to test scaling the game
	// for UI Bounds and adjust them without having to run the game
	public void toggleForceUsingUIBoundsScaling(bool isForcing)
	{
		isForceUsingUIBoundsScaling = isForcing;
	
		if (isForcing)
		{
			// We need to generate the wings to correctly test sizing, so generate them
			if (wingsPrefab != null)
			{
				createWingsFromPrefab();
			}
		
			// create Overlay and Spin Panel
			GameObject overlayPrefab = Overlay.prefab;
			Vector3 localPos = overlayPrefab.transform.localPosition;
			GameObject go = CommonGameObject.instantiate(overlayPrefab) as GameObject;
			Overlay createdOverlay = go.GetComponent<Overlay>();
			NGUIExt.attachToAnchor(go, NGUIExt.SceneAnchor.OVERLAY_CENTER, localPos);
			// Assign this instance ourselves since Awake isn't going to be called
			Overlay.instance = createdOverlay;
			Overlay.instance.uiCamera = NGUIExt.getObjectCamera(go);

			createdOverlay.createSpinPanel(isForcingSpinPanelV2:true);

			// Handle displaying the right thing if this is freespins
			if (parentGame.isFreeSpinGame())
			{
				createdOverlay.top.show(false);
				SpinPanel.instance.bonusSpinPanel.SetActive(true);
				SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
			}

			// cache the modules out since the game isn't running so they aren't cached
			// this will allow us to handle custom spin panels that can affect the UI Bounds
			parentGame.cacheAttachedSlotModules();
			
			// Check if we are using a custom spin panel and hide the spin panel if so
			CustomSpinPanelInfo customSpinPanelInfo = getCustomSpinPanelInfo();
			if (customSpinPanelInfo.isUsingCustomSpinPanel)
			{
				SpinPanel.instance.gameObject.SetActive(false);
			}
			
			// Mark that we should force update the AspectRatioPositioners and AspectRatioScalers on
			// the Overlay and the Spin Panel to make sure they are the correct size for the current
			// aspect ratio
			isOverlayAndSpinPanelAspectRatioUpdateRequiredInEditor = true;

			// Save out the current paybox size so that we can restore it correctly when the testing is disabled
			boundsTestStartingPayBoxSize = parentGame.payBoxSize;
		}
		else
		{
			if (wingsPrefab != null)
			{
				if (wings != null)
				{
					DestroyImmediate(wings.gameObject);
					wings = null;
				}
			}
		
			if (Overlay.instance != null)
			{
				DestroyImmediate(Overlay.instance.gameObject);
				Overlay.instance = null;
			}

			if (SpinPanel.instance != null)
			{
				DestroyImmediate(SpinPanel.instance.gameObject);
				SpinPanel.instance = null;
				BonusSpinPanel.instance = null;
			}
			
			// Make sure that we clean up reelSizerCenter as well since that is generated by the process of toggeling this on
			if (reelSizerCenter != null)
			{
				DestroyImmediate(reelSizerCenter);
				reelSizerCenter = null;
			}

			destroyDebugColliders();
			
			// Restore the saved out size which was serialized and was modified during the test
			parentGame.payBoxSize = boundsTestStartingPayBoxSize;
		}
	}
	
	// Cleanup the debug colliders
	public void destroyDebugColliders()
	{
		for (int i = 0; i < reelSizerCenterDebugObjectsList.Count; i++)
		{
			DestroyImmediate(reelSizerCenterDebugObjectsList[i]);
		}
		reelSizerCenterDebugObjectsList.Clear();
		areDebugReelSizerCenterObjectsMade = false;
		isCreatingDebugCollidersForForceUsingUIBoundsScaling = false;
	}

	// Used by ReelGameBackgroundEditor to force the special win overlay on for visual inspection in the editor
	public void toggleForceShowSpecialWinOverlay(bool isForcing)
	{
#if !ZYNGA_PRODUCTION
		isForceShowingSpecialWinOverlay = isForcing;

		if (isForcing)
		{
			// make sure we force the jackpot UI to show so we can see how it 
			if (Overlay.instance != null)
			{
				// Editor only function
				Overlay.instance.jackpotMystery.forceShowInEditor();
			}
		}
		else
		{
			if (Overlay.instance != null)
			{
				// we aren't forcing the editor version on anymore so hide the special overlay
				Overlay.instance.jackpotMystery.hide();
			}
		}

		// make sure we update at least once even if the game is running to have these changes take effect
		keepUpdating = true;
#endif
	}
	
	// Tells if a camera is in the reelViewportSpecificCameras list, this is used
	// to cull out the cameras which shouldn't be auto sized to the current camera
	// size (since reelViewportSpecificCameras have custom sizes)
	private bool isCameraInReelViewportSpecificCameras(Camera cameraToCheckFor)
	{
		if (reelViewportSpecificCameras != null)
		{
			for (int i = 0; i < reelViewportSpecificCameras.Length; i++)
			{
				if (cameraToCheckFor == reelViewportSpecificCameras[i])
				{
					return true;
				}
			}
		}

		return false;			
	}
	
	// Tells if a camera is in the camerasNotToAutosize list, in which
	// case we will cull the camera out of the auto size list so ReelGameBackground
	// will not update it.
	private bool isCameraInCamerasToNotAutosize(Camera cameraToCheckFor)
	{
		if (camerasToNotAutosize != null)
		{
			for (int i = 0; i < camerasToNotAutosize.Length; i++)
			{
				if (cameraToCheckFor == camerasToNotAutosize[i])
				{
					return true;
				}
			}
		}

		return false;
	}
	
	// Tells if a camera is going to be scaled with the background, in which
	// case we will remove the camera from the auto size list so that the scaling
	// code will have full control over it (rather than having it be auto forced to a size)
	private bool isCameraInCamerasToScaleWithBackground(Camera cameraToCheckFor)
	{
		if (camerasToScaleWithBackground != null)
			{
				for (int i = 0; i < camerasToScaleWithBackground.Length; i++)
				{
					if (cameraToCheckFor == camerasToScaleWithBackground[i])
					{
						return true;
					}
				}
			}
	
			return false;
	}
	
	// Forces ReelGameBackground to update even if it has already done
	// the layout for the current aspect ratio, useful if there is a reason
	// why things may have changed independent of the standard checks and you
	// want to force a total refresh of the layout calculations.
	public void forceUpdate()
	{
		isForcingUpdate = true;
		keepUpdating = true;
	}

	private static Dictionary<SkuId, BackgroundSizeData> skuToBackgroundSizeData;
	// Sets the background data for all the SKUs. We need to know what differnt skus values are so we move games from
	// One SKU to the next.
	// These numbers should come from SpinPanel's getNormalizedReelsAreaCenter() and getNormalizedReelsAreaHeight();
	// yPos = getNormalizedReelsAreaCenter() * ORTHO_CAMERA_SIZE * 2
	// yScale = getNormalizedReelsAreaHeight() * ORTHO_CAMERA_SIZE * 2
	private void setBackgroundSizeData()
	{
		skuToBackgroundSizeData = new Dictionary<SkuId, BackgroundSizeData>();
		// HIR data
			SkuId sku = SkuId.HIR;
			BackgroundSizeData backgroundSizeData = new BackgroundSizeData();
			backgroundSizeData.specialOverlaySize = (108.0f / 1536.0f) * (currentCameraSize * 2); // Magic number from looking in the scene.
			// Basegame
				float yPos = 0.559578f;
				float yScale = 5.840364f;
				backgroundSizeData.addGameTypeData(GameType.Basegame, yPos, yScale);
			// Freespins
				yPos = 0.8701822f;
				yScale = 6.459635f;
				backgroundSizeData.addGameTypeData(GameType.Freespins, yPos, yScale);
			// Full Screen
				yPos = 0.0f;
				yScale = ORTHO_CAMERA_SIZE * 2;
				backgroundSizeData.addGameTypeData(GameType.Fullscreen, yPos, yScale);
			skuToBackgroundSizeData.Add(sku, backgroundSizeData);
		// SIR data
			sku = SkuId.SIR;
			backgroundSizeData = new BackgroundSizeData();
			backgroundSizeData.specialOverlaySize = (108.0f / 1536.0f) * (currentCameraSize * 2); // Magic number from looking in the scene.
			// Basegame
				yPos = 0.3391928f;
				yScale = 1208.0f / 1536.0f * currentCameraSize * 2;
				backgroundSizeData.addGameTypeData(GameType.Basegame, yPos, yScale);
			// Freespins
				yPos = 0.6085938f;
				yScale = 6.982812f;
				backgroundSizeData.addGameTypeData(GameType.Freespins, yPos, yScale);
			// Full Screen
				yPos = 0.0f;
				yScale = currentCameraSize * 2;
				backgroundSizeData.addGameTypeData(GameType.Fullscreen, yPos, yScale);
			skuToBackgroundSizeData.Add(sku, backgroundSizeData);
	}

	/// Enum for size overrides
	public enum GameType
	{
		Basegame = 1,
		Freespins = 2,
		Fullscreen = 3
	}

	// Data for backgrounds and wings.
	private class BackgroundSizeData
	{
		public float specialOverlaySize; 

		private const float wingsAspectRatio = 512.0f / 1024.0f; // 0.5f;
		private const float iPadAspectRatio = 2048.0f / 1536.0f; // 1.3333f
		// @todo : Need to deal with these numbers being swapped for portrait mode
		public const float fullscreenYSize = ORTHO_CAMERA_SIZE * 2;
		public const float fullscreenXSize = fullscreenYSize * iPadAspectRatio;
		private const float fullscreenWingsXSize = fullscreenYSize * wingsAspectRatio;

		
		public void addGameTypeData(GameType type, float yPosition, float yScale)
		{
			// We should only need the y position and the y size to get where everything should go.
			if (!gameTypeToSizeData.ContainsKey(type) && !wingTypeToSizeData.ContainsKey(type))
			{
				// Calculate the position of the background:
				Vector2 backgroundPosition = new Vector2(0.0f, yPosition); 
				Vector2 backgroundScale = new Vector2(fullscreenXSize, yScale);
				// Calculate the position of the wings.
				float backgroundScaleX = yScale * wingsAspectRatio;
				Vector2 wingsScale = new Vector2(backgroundScaleX, yScale);
				Vector2 wingsPosition = new Vector2((fullscreenXSize + backgroundScaleX) / 2.0f, yPosition);
				addGameSizeData(type, backgroundPosition, backgroundScale);
				addWingSizeData(type, wingsPosition, wingsScale);
			}
			else
			{
				Debug.LogError("Trying to add a key to dictionary that already exists");
			}
		}

		private Dictionary<GameType, SizeData> gameTypeToSizeData = new Dictionary<GameType, SizeData>();
		private Dictionary<GameType, SizeData> wingTypeToSizeData = new Dictionary<GameType, SizeData>();

		// Returns a copy of the Size data that have been converted from this BackgroundSizeData's SKU to another BackgroundSizeData's SKU.
		// If the SKU's are the same then it doesn't attempt to modify them.
		public SizeData getSKUConvertedBackgroundSizeData(BackgroundSizeData to, GameType type, bool shouldAccountForSpecialOverlay)
		{
			if (to == null)
			{
				Debug.LogError("Trying to convert to null data"); 
				SizeData sizeData = getGameTypeSizeData(type);
				if (shouldAccountForSpecialOverlay)
				{
					modifySizeBasedOffOverlay(sizeData, specialOverlaySize);
				}
				return sizeData;
			}
			// We want to go from this size to the next size.
			if (to == this)
			{
				SizeData sizeData = getGameTypeSizeData(type);
				if (shouldAccountForSpecialOverlay)
				{
					modifySizeBasedOffOverlay(sizeData, specialOverlaySize);
				}
				return sizeData;
			}

			SizeData fromSkuSizeData = getGameTypeSizeData(type);
			SizeData toSkuSizeData = to.getGameTypeSizeData(type);

			// Lets get this working without the special overlays first.
			if (fromSkuSizeData == null || toSkuSizeData == null)
			{
				Debug.LogError("One of the target Skus was null");
				return null;
			}

			SizeData adjustedSizeData = new SizeData();

			// Get the ratio
			float scalePercentage = toSkuSizeData.scale.y / fromSkuSizeData.scale.y; // The size in the x direction should be the same
			if (scalePercentage < 1.0f)
			{
				// We need to scale the game down.
				adjustedSizeData.scale = fromSkuSizeData.scale * scalePercentage;
				adjustedSizeData.position = toSkuSizeData.position;
				if (shouldAccountForSpecialOverlay)
				{
					modifySizeBasedOffOverlay(adjustedSizeData, to.specialOverlaySize);
				}
			}
			else
			{
				// If the game we're moving into has more space than the original game then we shouldn't scale up the game at all
				// or the game area could move off the screen.
				adjustedSizeData.scale = fromSkuSizeData.scale;
				adjustedSizeData.position = toSkuSizeData.position;
				if (shouldAccountForSpecialOverlay)
				{
					// We want to scale the game down as little as possible here.
					float sizeDifference = toSkuSizeData.scale.y - fromSkuSizeData.scale.y;
					if (sizeDifference > to.specialOverlaySize)
					{
						// We are smaller than we need to be to fit, so we can just adjust the position.
						adjustedSizeData.position.y = adjustedSizeData.position.y - to.specialOverlaySize / 2;
					}
					else
					{
						// We want to move down by the difference in the two SKU sizes.
						adjustedSizeData.position.y = adjustedSizeData.position.y - sizeDifference / 2;
						float remainingSpecialOverlaySize = to.specialOverlaySize - sizeDifference;
						modifySizeBasedOffOverlay(adjustedSizeData, remainingSpecialOverlaySize);
					}
				}
			}
			return adjustedSizeData;


		}

		// Returns a copy of the games size data for a specific type.
		public SizeData getGameTypeSizeData(GameType type)
		{
			SizeData gameTypeSizeData = null;
			if (gameTypeToSizeData.ContainsKey(type))
			{
				gameTypeSizeData = gameTypeToSizeData[type];
			}
			else
			{
				Debug.LogError("The data for this type size isn't defined for this SKU. Giving up.");
				return null;
			}
			gameTypeSizeData = new SizeData(gameTypeSizeData);
			return gameTypeSizeData;
		}

		// Returns a copy of the wings size data for a specific type.
		public SizeData getWingTypeSizeData(GameType type)
		{
			SizeData wingTypeSizeData = null;
			if (wingTypeToSizeData.ContainsKey(type))
			{
				wingTypeSizeData = wingTypeToSizeData[type];
			}
			else
			{
				Debug.LogError("The data for this type size isn't defined for this SKU. Giving up.");
				return null;
			}
			wingTypeSizeData = new SizeData(wingTypeSizeData);
			return wingTypeSizeData;
		}

		private void addGameSizeData(GameType type, Vector2 position, Vector2 scale)
		{
			if (!gameTypeToSizeData.ContainsKey(type))
			{
				SizeData data = new SizeData(position, scale);
				gameTypeToSizeData.Add(type, data);
			}
			else
			{
				Debug.LogError("Trying to add a key to the wings dictionary that already exists");
			}
		}

		private void addWingSizeData(GameType type, Vector2 position, Vector2 scale)
		{
			if (!wingTypeToSizeData.ContainsKey(type))
			{
				SizeData data = new SizeData(position, scale);
				wingTypeToSizeData.Add(type, data);
			}
			else
			{
				Debug.LogError("Trying to add a key to the wings dictionary that already exists");
			}
		}

		private void modifySizeBasedOffOverlay(SizeData sizeData, float specialOverlaySize)
		{
			// The special overlay comes down from the top of all the games right now, so we're going to assume that's the case.
			float amountOfBackgroundOverlayCovers = specialOverlaySize / sizeData.scale.y;
			sizeData.position.y = sizeData.position.y - specialOverlaySize / 2;
			sizeData.scale *= (1.0f - amountOfBackgroundOverlayCovers);
		}

		public class SizeData
		{
			public Vector2 position;
			public Vector2 scale;

			public SizeData()
			{
			}

			public SizeData(Vector2 position, Vector2 scale)
			{
				this.position = position;
				this.scale = scale;
			}

			public SizeData(SizeData other)
			{
				position = other.position;
				scale = other.scale;
			}
		}
	}

}
