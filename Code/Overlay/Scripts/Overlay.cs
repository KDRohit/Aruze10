using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;

#if UNITY_EDITOR
using UnityEditor;
#endif

/**
Controls the appearance and behavior of the overlay (top and bottom common UI).
*/
public class Overlay : TICoroutineMonoBehaviour, IResetGame
{
	public OverlayTop top;
	public GameObject jackpotMysteryAnchor;
	public GameObject jackpotMysteryPrefab;	// A copy of this gets instantiated as a child of jackpotMysteryAnchor.
	public GameObject shroud;	// Multi-purpose shroud for things like tutorial-like tooltips that must appear over the top of everything.
	public Transform popupAnchor;
	public GameObject backingSprite;
	public UIAnchor contentParentAnchor;

	[SerializeField] private ObjectSwapper swapper;

	// Prefabs for instantiating stuff
	public GameObject spinPanelPrefab;
	private GenericDelegate shroudDelegate = null;
	private bool isFTUEActive = false;

	private AlphaRestoreData overlayAlphaRestoreData = null;
	[HideInInspector] public bool isFaded = false;

	[HideInInspector] public bool pendingTokenBar = false;
	[HideInInspector] public bool isLoadingJackpots = false;

	[HideInInspector] public OverlayJackpotMystery jackpotMystery;

	// SKU-specific references to the parts of the overlay, for SKU-specific control.
	[HideInInspector] public OverlayTopHIR topHIR = null;
	[HideInInspector] public OverlayTopHIRv2 topV2 = null;
	[HideInInspector] public OverlayJackpotMysteryHIR jackpotMysteryHIR = null;
	[System.NonSerialized] public Vector2 overlayTopMax;
	[System.NonSerialized] public Vector2 overlayTopMin;
	[System.NonSerialized] public Camera uiCamera;
	
	private float checkOverlayBoundsChangedTimer = 0.0f;
	private float checkOverlayBoundsTargetTime = CHECK_OVERLAY_BOUNDS_CHANGED_MIN_TIME;
	private const float CHECK_OVERLAY_BOUNDS_CHANGED_MIN_TIME = 1.0f;
	private const float CHECK_OVERLAY_BOUNDS_CHANGED_TIME_INCREASE_MULTIPLIER = 2.0f; // how much the delay time will be multiplied by every time it is increased
	private const float CHECK_OVERLAY_BOUNDS_CHANGED_MAX_TIME = 32.0f;
	private const float TWEEN_POSITION = 200.0f;

	private const string JACKPOT_BUNDLE_NAME = "jackpot_overlays";

	private static bool isLoadingOverlayPrefabInProgress = false;
	public static string OVERLAY_PREFAB_PATH = "Assets/Data/HIR/Bundles/Initialization/Prefabs/Overlay/Top Bar V3.prefab";
	public static GameObject prefab = null;
	
	public static Overlay instance = null;


	public void transitionOut(float time)
	{
		iTween.MoveTo(gameObject, iTween.Hash("y", TWEEN_POSITION, "time", time, "islocal", true, "delay", 0.5f));
	}

	public void transitionIn(float time)
	{
		iTween.MoveTo(gameObject, iTween.Hash("y", 0, "time", time, "islocal", true, "oncompletetarget", gameObject, "oncomplete", "onTweenComplete"));
	}

	private void onTweenComplete()
	{
		if (contentParentAnchor != null)
		{
			contentParentAnchor.enabled = true;
		}
	}

	// It takes a bit to load overlay prefab, we will load and cache it once
	public static void loadOverlayPrefabsAsync()
	{
		// If we have not started loading prefabs yet, do it now.
		// This function might be already called.  We have check here to prevent the code from loading the prefabs multiple times.
		if (prefab == null && !isLoadingOverlayPrefabInProgress)
		{
			isLoadingOverlayPrefabInProgress = true;
			RoutineRunner.instance.StartCoroutine(SkuResources.loadFromMegaBundleWithCallbacksAsync(OVERLAY_PREFAB_PATH,
				onOverlayLoaded, onOverlayFailed));
		}
	}
	public static void onOverlayLoaded(string assetPath, System.Object obj, Dict data = null)
	{
		if (assetPath == OVERLAY_PREFAB_PATH)
		{
			prefab = obj as GameObject;
			isLoadingOverlayPrefabInProgress = false;
		}
	}

	public static void onOverlayFailed(string assetPath, Dict data = null)
	{
		isLoadingOverlayPrefabInProgress = false;
		Debug.LogError("Failed to download overlay Prefab: " + assetPath);
	}

	public static IEnumerator createOverlay()
	{
		loadOverlayPrefabsAsync();

		while (prefab == null)
		{
			yield return null;
		}
		if (prefab != null)
		{
			Vector3 localPos = prefab.transform.localPosition;
			GameObject go = CommonGameObject.instantiate(prefab) as GameObject;
			// Drop the prefab immediately after instantiating it to release memory hold by it
			prefab = null;
			
			NGUIExt.attachToAnchor(go, NGUIExt.SceneAnchor.OVERLAY_CENTER, localPos);
			instance.uiCamera = NGUIExt.getObjectCamera(go);
		}
	}
	
	void Awake()
	{
		instance = this;

		topHIR = top as OverlayTopHIR;
		topV2 = top as OverlayTopHIRv2;
	
		if (!ExperimentWrapper.LazyLoadBundles.isInExperiment)
		{
			// might as well load these in, most of our users play games with some feature on it. note this is lazy loaded still
			AssetBundleManager.downloadAndCacheBundle(JACKPOT_BUNDLE_NAME, false);
		}

		if (EliteManager.hasActivePass && !EliteManager.showLobbyTransition)
		{
			enableElite();
		}
		else
		{
			EliteManager.addEventHandler(enableElite);
		}

		// Set the prefab inactive so it doesn't do Awake(),
		// causing it to get out of position before being set to 0,0,0 by NGUITools.AddChild,
		// just in case the prefab was not saved at 0,0,0 position.
		if (jackpotMysteryPrefab != null)
		{
			setupJackpotPrefab();
		}

		if (shroud != null)
		{
			shroud.SetActive(false);
		}
	}

	public void enableElite()
	{
		if (swapper != null)
		{
			swapper.setState("elite");
		}

		EliteManager.removeEventHandler(enableElite);
	}

	public void disableElite()
	{
		if (swapper != null)
		{
			swapper.setState("default");
		}
	}
	
	public void check(Dict args = null)
	{
		Debug.LogError("Success");
	}

	void Update()
	{
		// Non MonoBehaviour classes that we want to call a method on each loop, but without the monobehaviour/coroutine overhead.
		ProgressiveJackpot.update();    // This must be done in each loop, even if not showing the values anywhere at the moment.
		RefreshableData.update();
		CreditSweepstakes.update();
		GameTimerRange.update();
		if (ChallengeGame.instance != null || FreeSpinGame.instance != null)
		{
			// If a bonus game or free spins is happening, don't do anything here,
			// since the overlay isn't shown at this point.
			return;
		}

		if (top.gameObject.activeSelf)
		{
			top.update();
		}
		if (jackpotMystery != null && jackpotMystery.gameObject.activeSelf)
		{
			jackpotMystery.update();
		}

		// Touching anywhere make the shroud go away.
		// Must be on mouse down, not mouse up, to make sure it goes away even if swiping (but not while a dialog is shown).
		if (TouchInput.isTouchDown && shroud != null && shroud.activeSelf && !Dialog.instance.isShowing && !isFTUEActive)
		{
			hideShroud();
		}

		// @note : (Scott) This will only update ReelGame.activeGame which means if more than one
		// ReelGameBackground needed to be updated or we were expecting the Overlay to be able to
		// change and alter different parts of games like Base and Freespins we might have to rethink
		// how this is working.  For now this will work with how we use ReelGameBackground.
		if (ReelGame.activeGame != null && ReelGame.activeGame.reelGameBackground != null)
		{
			// Check if game is doing freespins in base, and ignore overlay changes during that time
			// (since the overlay should not be visible, and we don't want the game to resize from what it
			// was when the overlay was enabled).
			if (!ReelGame.activeGame.isDoingFreespinsInBasegame())
			{
				ReelGameBackground currentReelGamBackground = ReelGame.activeGame.reelGameBackground;

				checkOverlayBoundsChangedTimer += Time.unscaledDeltaTime;

				if (checkOverlayBoundsChangedTimer >= checkOverlayBoundsTargetTime)
				{
					updateReelLimits(currentReelGamBackground.isFreeSpins || currentReelGamBackground.gameSize == ReelGameBackground.GameSizeOverrideEnum.Freespins);

					if (currentReelGamBackground.overlayTopMaxUsed != overlayTopMax || currentReelGamBackground.overlayTopMinUsed != overlayTopMin)
					{
						// The overlay is different so we need to force the game to update
						ReelGame.activeGame.reelGameBackground.forceUpdate();
						checkOverlayBoundsTargetTime = CHECK_OVERLAY_BOUNDS_CHANGED_MIN_TIME;
					}
					else
					{
						// the overlay bounds haven't changed so increase the dealy before
						// we check again, assuming it might already be setup correctly
						checkOverlayBoundsTargetTime *= CHECK_OVERLAY_BOUNDS_CHANGED_TIME_INCREASE_MULTIPLIER;
						if (checkOverlayBoundsTargetTime > CHECK_OVERLAY_BOUNDS_CHANGED_MAX_TIME)
						{
							checkOverlayBoundsTargetTime = CHECK_OVERLAY_BOUNDS_CHANGED_MAX_TIME;
						}
					}

					checkOverlayBoundsChangedTimer = 0.0f;
				}
			}
		}
	}
	
	public void hideShroud()
	{
		isFTUEActive = false;
		if (shroudDelegate != null)
		{
			shroudDelegate();
		}
	
		shroudDelegate = null;

		if (shroud != null)
		{
			// Need to nullcheck this since this function is called without knowing whether a shroud actually exists.
			// It doesn't exist on the old Overlay, which has the VIP button instead of Charms.
			shroud.SetActive(false);
		}
	}

	public void hideShroudAndClearDelegate()
	{
		shroudDelegate = null;

		if (shroud != null)
		{
			// Need to nullcheck this since this function is called without knowing whether a shroud actually exists.
			// It doesn't exist on the old Overlay, which has the VIP button instead of Charms.
			shroud.SetActive(false);
		}
	}

	public void createSpinPanel(bool isForcingSpinPanelV2 = false)
	{
		GameObject go = CommonGameObject.instantiate(spinPanelPrefab) as GameObject;
		NGUIExt.attachToAnchor(go, NGUIExt.SceneAnchor.CENTER, spinPanelPrefab.transform.localPosition);
		
		if (!Application.isPlaying)
		{
			// assign SpinPanel.instance and BonusSpinPanel.instance since 
			// it isn't going to happen in Awake while the game isn't running
			SpinPanel.instance = go.GetComponentInChildren<SpinPanel>(true);
			BonusSpinPanel.instance = go.GetComponentInChildren<BonusSpinPanel>(true);
		}

		if (SpinPanel.instance != null)
		{
			// Store out the camera so that other classes that might need to transform based on this camera can
			SpinPanel.instance.uiCamera = NGUIExt.getObjectCamera(go);
		}
			
		// This gets cleaned up when done with the game scene.
		DisposableObject.register(go);

		if (Application.isPlaying && jackpotMystery == null)
		{
			addJackpotOverlay();
		}
	}

	public void addJackpotOverlay()
	{
		if (!isLoadingJackpots && jackpotMysteryPrefab == null)
		{
			isLoadingJackpots = true;
			AssetBundleManager.load(this, OverlayJackpotMystery.PREFAB_PATH, onJackpotOverlayLoad, onJackpotOverlayFailed);
		}
	}

	public void removeJackpotOverlay()
	{
		if (jackpotMysteryHIR != null && jackpotMysteryHIR.tokenBar != null)
		{
			Destroy(jackpotMystery.tokenBar.gameObject);
		}
	}

	private void onJackpotOverlayLoad(string path, UnityEngine.Object obj, Dict args = null)
	{
		isLoadingJackpots = false;
		jackpotMysteryPrefab = obj as GameObject;

		if (jackpotMysteryPrefab != null)
		{
			setupJackpotPrefab();
		}

		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.onJackpotBarsLoaded();
		}
	}

	private void onJackpotOverlayFailed(string path, Dict args = null)
	{
		isLoadingJackpots = false;
		Debug.LogError("Overlay: Failed to load jackpot overlay.");

		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.onJackpotBarsLoadFailed();
		}
	}

	private void setupJackpotPrefab()
	{
		jackpotMysteryPrefab.SetActive(false);
		
		GameObject go = NGUITools.AddChild(jackpotMysteryAnchor, jackpotMysteryPrefab);
		jackpotMystery = go.GetComponent<OverlayJackpotMystery>();

		jackpotMysteryHIR = jackpotMystery as OverlayJackpotMysteryHIR;

		jackpotMystery.gameObject.SetActive(true);
		
		// Set this back to true so we don't have a changed prefab in git.
		jackpotMysteryPrefab.SetActive(true);

		jackpotMystery.setQualifiedStatus();

		if (pendingTokenBar)
		{
			jackpotMysteryHIR.setUpTokenBar();
			pendingTokenBar = false;
		}
	}

	private void setCustomSpinPanel(string prefabPath)
	{
		if (Application.isPlaying)
		{
			AssetBundleManager.load(this, prefabPath, customPanelLoadSuccess, customPanelLoadFailure);
		}
#if UNITY_EDITOR
		else
		{
			GameObject spinPanelInstance = AssetDatabase.LoadAssetAtPath("Assets/Data/HIR/Bundles/" + prefabPath + ".prefab", typeof(GameObject)) as GameObject;
			customPanelLoadSuccess(prefabPath, spinPanelInstance);
		}
#endif
	}

	private void customPanelLoadSuccess(string path, UnityEngine.Object obj, Dict args = null)
	{
		GameObject prefab = obj as GameObject;
		GameObject go = CommonGameObject.instantiate(prefab) as GameObject;
		NGUIExt.attachToAnchor(go, NGUIExt.SceneAnchor.CENTER, spinPanelPrefab.transform.localPosition);

		if (Application.isPlaying)
		{
			// This gets cleaned up when done with the game scene.
			DisposableObject.register(go);
		}
		else
		{
			// Set this as the spin panel instance, since that isn't going to happen through Awake()
			SpinPanel.instance = go.GetComponent<SpinPanel>();
			SpinPanel.hir = go.GetComponent<SpinPanelHIR>();
			BonusSpinPanel.instance = SpinPanel.instance.bonusSpinPanel.GetComponent<BonusSpinPanel>();
		}
		
		if (SpinPanel.instance != null)
		{
			// Store out the camera so that other classes that might need to transform based on this camera can
			SpinPanel.instance.uiCamera = NGUIExt.getObjectCamera(go);
		}
	}

	private void customPanelLoadFailure(string path, Dict args = null)
	{
		Debug.LogErrorFormat("Overlay.cs -- customPanelLoadFailure -- failed to load prefab at path: {0}", path);
		//Kick back out to the lobby if the spin panel didn't load succussfully for some reason
		GenericDialog.showDialog(
			Dict.create(
				D.TITLE, Localize.textUpper("check_connection_title"),
				// Don't localize the message, since it's useless to the player but may be helpful to devs if reported.
				D.MESSAGE, Localize.text("download_error_message"),
				D.REASON, "slot-startup-download-error",
				D.CALLBACK, new DialogBase.AnswerDelegate((noargs) => 
					{ 
						GameState.pop();
						Loading.show(Loading.LoadingTransactionTarget.LOBBY); 
						Glb.loadLobby(); 
					})
			),
			SchedulerPriority.PriorityType.IMMEDIATE
		);
	}
	
	public void showShroud(GenericDelegate clickDelegate, float z = -1100f, bool isFTUE = false)
	{
		isFTUEActive = isFTUE;
		Vector3 pos = shroud.transform.localPosition;
		shroud.transform.localPosition = new Vector3(pos.x, pos.y, z);
		shroud.SetActive(true);
		shroudDelegate = clickDelegate;
	}

	public IEnumerator showJackpotMysteryWhenNotNull()
	{
		while (jackpotMystery == null)
		{
			yield return null;
		}

		jackpotMystery.show();
	}
	
	public void showJackpotMystery()
	{
		if (jackpotMystery != null)
		{
			jackpotMystery.show();
		}
	}

	public IEnumerator hideJackpotMysteryWhenNotNull()
	{
		while (jackpotMystery == null)
		{
			yield return null;
		}

		jackpotMystery.hide();
	}

	public void hideJackpotMystery()
	{
		if (jackpotMystery != null)
		{
			jackpotMystery.hide();
		}
	}

	// Sets all overlay and spin panel buttons enabled or disabled.
	public void setButtons(bool isEnabled)
	{
		InGameFeatureContainer.setButtonsEnabled(isEnabled);
		
		if (SpinPanel.instance != null)
		{
			SpinPanel.instance.setButtons(isEnabled);
		}
		
		if (this.jackpotMystery != null)
		{
			jackpotMystery.setButtons(isEnabled);
		}

		if (this.top != null)
		{
			top.setButtons(isEnabled);
		}
		
	}
	
	public IEnumerator fadeOut(float fadeDur)
	{
		if (!isFaded)
		{
			overlayAlphaRestoreData = CommonGameObject.getAlphaRestoreDataForGameObject(gameObject);
		}
		
		isFaded = true;
		
		// Fade out the rest of the game objects.
		
		float elapsedTime = 0;
		
		while (elapsedTime < fadeDur)
		{
			elapsedTime += Time.deltaTime;
			setAlphaOnOverlayGameObjects(1 - (elapsedTime / fadeDur));
			yield return null;
		}

		setAlphaOnOverlayGameObjects(0f);
	}

	public void fadeOutNow()
	{
		if (!isFaded)
		{
			overlayAlphaRestoreData = CommonGameObject.getAlphaRestoreDataForGameObject(gameObject);
		}
		
		isFaded = true;
		setAlphaOnOverlayGameObjects(0f);
	}

	private void setAlphaOnOverlayGameObjects(float alpha)
	{
		CommonGameObject.alphaGameObject(gameObject, alpha);
		NGUIExt.fadeGameObject(gameObject, alpha);
		TMProFunctions.fadeGameObject(gameObject, alpha);
	}

	public IEnumerator fadeIn(float fadeDur)
	{
		isFaded = false;
		yield return StartCoroutine(CommonGameObject.fadeGameObjectToOriginalAlpha(gameObject, overlayAlphaRestoreData, fadeDur));
	}

	public void fadeInNow()
	{
		isFaded = false;
		
		if (overlayAlphaRestoreData != null)
		{
			CommonGameObject.fadeGameObjectToOriginalAlpha(gameObject, overlayAlphaRestoreData);
		}
	}
	
	public static void resetStaticClassData()
	{
		if (Overlay.instance != null)
		{
			Destroy(Overlay.instance.gameObject);
		}
	}

	public void setBackingSpriteVisible(bool isVisible)
	{
		SafeSet.gameObjectActive(backingSprite, isVisible);
	}

	public void updateReelLimits(bool isFreeSpins)
	{
		// top including its token anchor need to be active for ReelGameBackground to correctly resize and position
		// reels, especially in freespins. This is kinda lame, but this is how it was working
		// before the top nav was reworked. So putting this fix in until we can get ReelGameBackground
		// to make it's own calculations.
		bool deactivateTop = false;
		bool jackpotTokenAnchorActiveStatusCache = false;
		
		if (jackpotMystery != null)
		{
			jackpotMystery.tokenAnchor.SetActive(jackpotMystery.tokenAnchor.activeInHierarchy);
		}
		
		
		if (!top.gameObject.activeInHierarchy)
		{
			top.gameObject.SetActive(true);
			deactivateTop = true;
		}

		Bounds reelBounds = top.getBounds(jackpotMystery, jackpotMysteryAnchor, isFreeSpins);
		overlayTopMax = reelBounds.max;
		overlayTopMin = reelBounds.min;

		if (deactivateTop)
		{
			top.gameObject.SetActive(false);
		}
	}

	private void OnDestroy()
	{
		EliteManager.removeEventHandler(enableElite);
	}
}
