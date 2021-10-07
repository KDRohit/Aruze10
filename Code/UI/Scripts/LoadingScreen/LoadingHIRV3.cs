using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using CustomLog;
using TMPro;

/*
Controls behaviour of loading screen for HIR.
*/

public class LoadingHIRV3 : Loading
{
	// =============================
	// PUBLIC
	// =============================
	public ParticleSystem loadingParticleSysSnow; // game objects have a particleSystem member variable, don't override that.
	public Renderer backgroundRenderer;
	
	[HideInInspector] public Texture2D dynamicBackgroundTexture;
	[HideInInspector] public Texture2D dynamicOverlayTexture;
	[HideInInspector] public Texture2D dynamicLogoTexture;

	private string backgroundTextureUrl;
	
	// =============================
	// PRIVATE
	// =============================
	private string randomFact;
	private ParticleSystem loadingParticleSys;		// Game objects have a particleSystem member variable, don't override that.
	
	// Default materials on the textures to use when we dont load anything.
	private Material defaultBackgroundMaterial;
	private Material defaultOverlayMaterial;
	private float currentAlpha = 0;

	private bool isBackgroundTextureLoaded = false;

	private bool isFirstTime = true;			// Whether this is the first time showing the loading screen.
	private bool isHidingOverlay = false;
	private bool isHidingLogo = false;
	
	private LoadingTheme activeTheme = null; // set from livedata, custom "themes" have separate loading/display logic
 
	private UICamera loadingCameraUI;
	private Camera loadingCamera;

	private bool isPreparingToShow = false; // Used to track when the loading screen is about to be shown before the isLoading flag is set to say that it is currently showing
	
	// =============================
	// CONST
	// =============================
	private const int FRAMES_TO_WAIT_AT_LOAD = 2; // The number of frames to wait for the textures to load from the cache.

	private const string ORIGINAL_BG_TEXTURE_PATH = "Textures/generic01_V3_background";
	
	/// Use init() instead of Awake() to initialize, since this object is inactive by default, and Awake isn't called until it becomes active.
	public override void init()
	{
		base.init();
		LoadingScreenData.init();
		loadingCamera = gameObject.GetComponentInParent<Camera>();
		loadingCameraUI = gameObject.GetComponentInParent<UICamera>();
		toggleCamera(false);
	}

	protected override void adjustForResolution(float contentWidth, float contentHeight)
	{
		base.adjustForResolution();

		if (NGUIExt.effectiveScreenWidth > contentWidth)
		{
			float yScale = NGUIExt.effectiveScreenWidth * contentHeight / contentWidth;
			float xScale = NGUIExt.effectiveScreenWidth;
			if (yScale < NGUIExt.effectiveScreenHeight)
			{
				//in the case where the aspect ratio is not quite the same, zoom in a bit so we don't
				//have empty space at the top and bottom
				xScale = xScale * (NGUIExt.effectiveScreenHeight / yScale);
				yScale = NGUIExt.effectiveScreenHeight;

			}
			//scale both ways
			backgroundRenderer.transform.localScale = new Vector3(xScale, yScale, 1);
		}
	}
	
	protected override void Update()
	{
		if (!isLoading || loadingPanel.alpha < 1.0f)
		{
			// Don't do anything here if we aren't showing the loading screen.
			return;
		}

		base.Update();
	}

	public override void setAlpha(float alpha)
	{
		base.setAlpha(alpha);
		
		CommonRenderer.alphaRenderer(backgroundRenderer, alpha);
	}

	public void notifyNewBackgroundIsAvailable(string newUrl)
	{
		if (newUrl == backgroundTextureUrl)
		{
			return;
		}
		
		backgroundTextureUrl = newUrl;
		isBackgroundTextureLoaded = false;
	}

	private void replaceBackgroundTexture(Texture2D tex)
	{
		if (tex == null)
		{
			return;
		}
		
		Material backgroundMaterial = getNewRendererMaterial(backgroundRenderer);
		backgroundMaterial.mainTexture = tex;

		backgroundRenderer.sharedMaterial = backgroundMaterial;

		isBackgroundTextureLoaded = true;
	}
	
	private void onBackgroundTextureLoaded(Texture2D tex, Dict args)
	{
		// Make sure that the loading screen is still showing before swapping the texture,
		// if it isn't we'll just ignore doing the swap.
		if (isLoading || isPreparingToShow)
		{
			if (tex != null)
			{
				replaceBackgroundTexture(tex);
				adjustForResolution(tex.width, tex.height);
			}
		}
		else
		{
			// Flag that we should attempt to download the background texture again the next time the loading screen shows (since
			// it finished downloading after the loading screen had closed). Technically right now this will happen anyways due to us discarding
			// the texture when the loading screen is hidden. But to be safe in case we change how that works I'll make sure that
			// we correctly flag that we should attempt to download the background texture again the next time the loading screen is shown.
			isBackgroundTextureLoaded = false;
			
			// Try and unload any unused assets here as well, since we don't want to hang onto a texture we aren't going to use
			// because the loading screen was already hidden.
			Resources.UnloadUnusedAssets();
		}
	}

	protected override IEnumerator showMe(LoadingTransactionTarget loadingTarget)
	{
		toggleCamera(true);

		if (isLoading)
		{
			// The loading screen is already showing. Do nothing.
			yield break;
		}

		// Flag that we are preparing to show the loading screen so we can correctly handle texture loading
		isPreparingToShow = true;
		
		// If the material or texture have been cleaned up, attempt to reload using either the asset LoadingScreenData
		// has told us about or our original material which should be in the initialization mega bundle.
		if (!isBackgroundTextureLoaded)
		{
			// Always load the default texture in first, so we for sure have something to show when it first displays.
			// Since even if the dynamic one is cached to disk it can still take a second for it to be loaded and ready
			// (this prevents seeing a blank background during the loading screen).
			loadDefaultTexture();

			if (!string.IsNullOrEmpty(backgroundTextureUrl))
			{
				// Attempt to load a dynamic texture, will just ignore it if the loading screen is disabled by the time it finishes
				DisplayAsset.loadLoadingScreenTexture(this, backgroundTextureUrl, onBackgroundTextureLoaded);
			}
		}

		showTheme();
		showWidgets();

		setAlpha(1.0f);
		
		yield return StartCoroutine(base.showMe(loadingTarget));
		// At this point the loading screen is actually showing and isLoading is set, so we can mark isPreparingToShow false
		isPreparingToShow = false;
		
		meter.currentValue = 0;
		setRandomFact();
		
		if (loadingParticleSys != null)
		{
			loadingParticleSys.gameObject.SetActive(true);
			loadingParticleSys.Play();
		}
	}

	protected override void finishHiding()
	{
		base.finishHiding();
				
		if (loadingParticleSys != null)
		{
			loadingParticleSys.Clear();
			loadingParticleSys.Stop();
			if (loadingParticleSys.gameObject != null)
			{
				loadingParticleSys.gameObject.SetActive(false);
			}
		}
		
		if (isFirstTime &&
			Data.liveData != null &&
			Data.liveData.getBool("USE_LIVE_DATA_LOADING_SCREEN", true))
		{
			LoadingScreenData.checkForUpdates();
			isFirstTime = false;
		}

		if (!Scheduler.hasTaskOfType<LobbyTransitionTask>())
		{
			toggleCamera(false);
		}

		//Clean up after ourselves; keeping background textures in memory can use 16+ MB and they're pretty fast to load.
		//We are part of a DontDestroyOnLoad hierarchy, and since we hide based on progress bar animation, this gets
		//run after InbetweenSceneLoader Awake+Start and SceneManager.LoadScene() have been called
		backgroundRenderer.sharedMaterial.mainTexture = null;
		Resources.UnloadUnusedAssets();
		isBackgroundTextureLoaded = false;
	}

	private void loadDefaultTexture()
	{
		Texture2D defaultTexture = SkuResources.loadSkuSpecificEmbeddedResource<Texture2D>(ORIGINAL_BG_TEXTURE_PATH, ".png");
		
		if (defaultTexture != null)
		{
			replaceBackgroundTexture(defaultTexture);
		}
	}

	private void setRandomFact()
	{
		loadingLabel.gameObject.SetActive(!LoadingHIRMaxVoltageAssets.isLoadingMiniGame);
		
		if (LoadingScreenData.currentData != null &&
			LoadingScreenData.currentData.tips != null &&
			LoadingScreenData.currentData.tips.Length > 0)
		{
			int index = Random.Range(0, LoadingScreenData.currentData.tips.Length);
			string factLoc = LoadingScreenData.currentData.tips[index];
			randomFact = Localize.text(factLoc);
		}
		else if (Glb.NUMBER_OF_RANDOM_FACTS > 0)
		{
			randomFact = Localize.text(string.Format("random_fact_{0}",Random.Range(0, Glb.NUMBER_OF_RANDOM_FACTS)));
		}
		// Could not find any random facts in SCAT, it's possible that Glb just hasn't loaded it yet.
		else
		{
			randomFact = Localize.text("loading");
		}
		// If something went wrong, we can just fake it by putting an english "Loading..." here.
		if (randomFact == "")
		{
			randomFact = "Loading...";
		}
	}

	private Material getNewRendererMaterial(Renderer imageRenderer, bool useUnlit = false)
	{
		if (imageRenderer == null)
		{
			return null;
		}
		
		Material mat = imageRenderer.sharedMaterial;
		
		if (mat == null || useUnlit)
		{
			mat = new Material(ShaderCache.find("Unlit/GUI Texture"));
		}
		else
		{
			// If there is already a material on this renderer,
			// then use a copy of it, since it may have a special shader we want.
			mat = new Material(mat);
		}
		return mat;
	}

	public void toggleCamera(bool isEnabled, bool forceValue = false)
	{
#if UNITY_EDITOR || ((UNITY_WSA_10_0 || UNITY_WEBGL) && !ZYNGA_PRODUCTION)
		//Don't turn off the camera if we're tracking FPS #'s unless we want to force our original isEnabled value
		if (DebuggingFPSMemoryComponent.Singleton != null && DebuggingFPSMemoryComponent.Singleton.gameObject.activeSelf && !forceValue)
		{
			isEnabled = true;
		}
#endif
		if (loadingCameraUI != null)
		{
			loadingCameraUI.enabled = isEnabled;
		}
		if (loadingCamera != null)
		{
			loadingCamera.enabled = isEnabled;
		}
	}
	public void showTheme()
	{
		if (LoadingFactory.themeObject != null)
		{
			LoadingFactory.themeObject.show();
		}
	}

	public void showWidgets()
	{
		foreach (KeyValuePair<string, LoadingWidget> widget in LoadingFactory.widgets)
		{
			widget.Value.show();
		}
	}

	public override void setDownloadingStatus(bool downloading, string loadingMessage = "")
	{
		base.setDownloadingStatus(downloading, loadingMessage);
		
		if (!downloading && Data.isGlobalDataSet)
		{
			// Only localize after the global data has been loaded,
			// which means the very first loading screen will always say "Loading" in English.
			loadingLabel.text = randomFact;
		}
	}
}
