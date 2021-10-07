using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;
using System.IO;

/**
This is part of the Startup scene, which is like a springboard for loading the rest of the game.
*/
public class NGUILoader : MonoBehaviour
{
#if UNITY_ANDROID
   public const string HARDCODED_CDN = "https://socialslots.cdn.zynga.com/android/bundlesv2/";
#elif UNITY_IOS
   public const string HARDCODED_CDN = "https://socialslots.cdn.zynga.com/ios/bundlesv2/";
#elif UNITY_WSA_10_0
   public const string HARDCODED_CDN = "https://socialslots.cdn.zynga.com/wsaplayer/bundlesv2/";
#elif UNITY_WEBGL
   public const string HARDCODED_CDN = "https://socialslots.cdn.zynga.com/webgl/bundlesv2/";
#else
	public const string HARDCODED_CDN = "Expected iOS, Android, Windows, or WebGL platform!";
#endif

	public UIRoot nguiRoot = null;				// The root NGUI object.

	public Transform loadingRoot = null;		// The root at which we will spawn the loading panel prefab.
	public Transform loginRoot = null;			// The root at which we will spawn the login panel prefab.
	public Transform toasterRoot = null;		// The root at which we will spawn the toaster panel prefab.
	
	private InitializationManager _initMgr;
	
	public static NGUILoader instance = null;
	
	private List<GenericDelegate> tapCallbacks = new List<GenericDelegate>();
	[System.NonSerialized] public AssetBundle initialBundle;
	// Sets up the visual quality settings of the game.
	public bool useAssetBundles = true;
	public static void setVisualQuality()
	{
		int deviceTargetFrameRate = MobileUIUtil.deviceTargetFrameRate;
		bool slowDevice = MobileUIUtil.isSlowDevice;

		QualitySettings.pixelLightCount = slowDevice ? 1 : 2;
		QualitySettings.shadowProjection = ShadowProjection.CloseFit;
		QualitySettings.shadowCascades = 0;
		QualitySettings.shadowDistance = 0f;
		QualitySettings.antiAliasing = 0;
		QualitySettings.anisotropicFiltering = slowDevice ? AnisotropicFiltering.Disable : AnisotropicFiltering.Enable;
		QualitySettings.masterTextureLimit = 0;
		
#if !UNITY_WEBGL
		/*
		We don't want to set FPS for webGL if we want it to run as fast as possible;
		"When you don’t want to throttle performance, set this API to the default value of –1,
		rather then to a high value. This allows the browser to adjust the frame rate
		for the smoothest animation in the browser’s render loop, and may produce better
		results than Unity trying to do its own main loop timing to match a target frame rate."
		- https://docs.unity3d.com/Manual/webgl-performance.html
		*/
		QualitySettings.maxQueuedFrames = -1;
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = deviceTargetFrameRate; // Don't set frame rate for WebGL due to how webGL handles frame caps.
#endif

		if (slowDevice)
		{
			QualitySettings.skinWeights = SkinWeights.OneBone;
		}
		else if (deviceTargetFrameRate < 60)
		{
			QualitySettings.skinWeights = SkinWeights.TwoBones;
		}
		else
		{
			QualitySettings.skinWeights = SkinWeights.FourBones;
		}
	}

	void Awake()
	{
#if UNITY_EDITOR
		useAssetBundles = (PlayerPrefsCache.GetInt(DebugPrefs.USE_ASSET_BUNDLES, 0) != 0);
#endif
		Userflows.logWebGlLoadingStep("ngui_loader_startup");
		StartCoroutine(loadBundleAndLoadingScreen());
	}


	private IEnumerator loadBundleAndLoadingScreen()
	{
		Bugsnag.LeaveBreadcrumb("NGUILoader is awake.");
		instance = this;

		if (useAssetBundles)
        {
			AssetBundleManifest manifestV2 = new AssetBundleManifest();
			manifestV2.ReadAssetBundleManifestFileV2();
			AssetBundleManager.manifestV2 = manifestV2; //Lets not double read this
			string fullBundleName = manifestV2.getFullBundleNameFromBaseBundleName(AssetBundleManager.INITIALIZATION_BUNDLE_NAME);

            string filePath = Path.Combine(Application.streamingAssetsPath, fullBundleName);
            Debug.Log("initialization bundle filepath = " + filePath); //in editor:  /Users/kkralian/dev/hir-webgl/Unity/Assets/StreamingAssets/initialization

			AssetBundleManager.invalidateOnBundleVersion();

			Debug.Log("~Loading Initialization File Path: " + fullBundleName);
			
#if !UNITY_WEBGL
	        initialBundle = AssetBundle.LoadFromFile(filePath);  //this only works in-editor if you built local bundles
	        Bugsnag.LeaveBreadcrumb("initialization bundle LoadFromFile" + (initialBundle == null ? "null" : "valid"));

	        if (initialBundle == null)
	        {
		        Debug.LogWarning("Local version of bundle not found at path: " + filePath);
	        }
#endif

            // No local bundle found? If running in-editor, Get bundle URL from manifest 
            if (initialBundle == null && Application.isEditor)
            {
                // UGH; don't have basic data yet, so don't know CDN, But any CDN should work in editor (CORS doesnt matter)
                filePath = HARDCODED_CDN + fullBundleName;

                Debug.LogWarning("Using hardcoded Cloudfront CDN to load initialization bundle from server while in editor...");
                Debug.Log("initialization bundle url = " + filePath); //ie:  https://socialslots.cdn.zynga.com/webgl/bundlesv2/initialization-hd-08941da2.bundlev2
            }

#if UNITY_WEBGL
            // WebGL running on canvas? Get base client app URL from javascript, append our streaming folder to it
            if (initialBundle == null && !Application.isEditor)
            {
                // Can't use Application.absoluteURL, because it shows our php-landing page (env2.dev01.hititrich.com)
                // and we need the actual hosted app URL, which the unity loader keeps in a JS gameInstance object
                string appUrl = WebGLFunctions.getGameInstanceUrl();   // "https://socialslots.cdn.zynga.com/webgl/clients/201017/Build/webgl.json"
                if (appUrl != null)
                {
                    filePath = appUrl.Replace("Build/webgl.json", "StreamingAssets/" + fullBundleName);
                    Debug.Log("new initialization bundle filepath = " + filePath);
                }
                else
                {
                    Debug.LogError("WebGL couldn't find GameInstanceURL");
                }
            }
#endif

            // Try to stream it from URL (needed so Android can decompress, and for WebGL to get from server)
            if (initialBundle == null)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                // WebGL shouldn't use Unity caching because it (essentially) leaks memory across sessions
                // Caching is instead performed explicitly via javascript CachedXMLHttpRequest calls
                // (see https://blogs.unity3d.com/2016/09/20/understanding-memory-in-unity-webgl/)
                //using (WWW myWWW = new WWW(filePath))
				using (UnityEngine.Networking.UnityWebRequest initBundleUwr = UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(filePath)) //Don't do caching on WebGl
#else
				//using (WWW myWWW = WWW.LoadFromCacheOrDownload(filePath, 0))
				using (UnityEngine.Networking.UnityWebRequest initBundleUwr = UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(filePath, 0, 0))
#endif
				{
					yield return initBundleUwr.SendWebRequest();
					
					if (!string.IsNullOrEmpty(initBundleUwr.error))
					{
						Debug.LogError("Initialization Bundle failed to load: " + initBundleUwr.error);
					}
					else
					{
						// Get downloaded asset bundle
						initialBundle = UnityEngine.Networking.DownloadHandlerAssetBundle.GetContent(initBundleUwr);
						if (initialBundle == null)
						{
							Debug.LogWarning("Initialization Bundle Failed to load with no error from UWR: " + filePath);
						}
					}
				}
            }

            Debug.Log("initialization bundle = " + (initialBundle == null ? "null" : "valid"));

            if (initialBundle == null)
            {
                Debug.LogError("Couldn't Load the embedded bundle. Falling back to the bundle on the server");
                AssetBundleManager.downloadAndCacheBundle(AssetBundleManager.INITIALIZATION_BUNDLE_NAME);
                Glb.resetGame("Couldn't load the embedded initialization bundle");
            }
        }
		
		// Preload prefabs we need to for game loading to main lobby asynchronously here. so that they will be ready
		// when we need them in GameLoader.finishLoading()
		LobbyLoader.loadMainLobbyPrefabsAsync();        
		Overlay.loadOverlayPrefabsAsync();
		
		setVisualQuality();
		
		// The NGUI structure is to be persistent between scenes,
		// so we only need to set it up in the Startup scene.
		NGUIExt.uiRoot = nguiRoot;

		// Instantiating these game objects that link to things in the scene from Prefabs to support SIR.
		GameObject loadingPrefab = SkuResources.loadSkuSpecificEmbeddedResourcePrefab("prefabs/uiroot/loading panel");
		GameObject loadingObject = CommonGameObject.instantiate(loadingPrefab) as GameObject;
		GameObject loginPrefab = SkuResources.loadSkuSpecificEmbeddedResourcePrefab("prefabs/uiroot/login panel");
		GameObject loginObject = CommonGameObject.instantiate(loginPrefab) as GameObject;
		GameObject toasterPrefab = SkuResources.loadSkuSpecificEmbeddedResourcePrefab("prefabs/uiroot/toaster panel");
		GameObject toasterObject = CommonGameObject.instantiate(toasterPrefab) as GameObject;
		CommonGameObject.instantiate(SkuResources.loadSkuSpecificEmbeddedResourcePrefab("prefabs/uiroot/tmpro font loader"));
		
		// For the objects that need to be in the UIRoot hierarchy, set them up in the correct locations here.
		loadingObject.transform.parent = loadingRoot;
		loadingObject.transform.localScale = loadingPrefab.transform.localScale;
		loadingObject.transform.localPosition = loadingPrefab.transform.localPosition;
		loginObject.transform.parent = loginRoot;
		loginObject.transform.localScale = loginPrefab.transform.localScale;
		loginObject.transform.localPosition = loginPrefab.transform.localPosition;
		loginObject.SetActive(false);
		toasterObject.transform.parent = toasterRoot;
		toasterObject.transform.localScale = toasterPrefab.transform.localScale;
		toasterObject.transform.localPosition = toasterPrefab.transform.localPosition;
		
		Loading loading = loadingObject.GetComponent<Loading>();
		if (loading != null)
		{
			loading.init();
			Loading.show(Loading.LoadingTransactionTarget.LOBBY_LOGIN);			
#if UNITY_WEBGL
			// Hide HTML loading screen now that client can show its own loading screen
			Application.ExternalEval("window.endLoadingSlideShow()");
#endif
		}
		else
		{
			Debug.LogError("Something horrible has gone wrong,and the loading instance is null");
		}


		Login login = loginObject.GetComponent<Login>();
		if (login != null)
		{
			login.init();
		}
		else
		{
			Debug.LogError("Something horrible has gone wrong,and the login instance is null");
		}

		Userflows.logWebGlLoadingStep("ngui_loader_complete");

		yield break;
	}

	void Start()
	{
		DontDestroyOnLoad(nguiRoot.gameObject);
		StartCoroutine(loadStartUpSceneAfterBundleLoad());
	}
	
	private IEnumerator loadStartUpSceneAfterBundleLoad()
	{
		// Would be in Awake(), however Unity 5.3 introduces a crash when scene loads are in Awake()
		//Need to wait for the bundle to actually load to avoid hitting startup code before we're ready
		//which results in us being stuck in a loading screen
		while (useAssetBundles && initialBundle == null)
		{
			yield return null;
		}
		Userflows.logWebGlLoadingStep("loading startup scene");
		SceneManager.LoadScene(Glb.STARTUP_LOGIC_SCENE);
		yield break;
	}
	
	void Update()
	{
		if (instance != null)
		{
			if (tapCallbacks.Count > 0 && TouchInput.didTap)
			{
				// Call all our delegates. 
				foreach (GenericDelegate callback in tapCallbacks)
				{
					if (callback != null)
					{
						callback();
					}
				}
				//clear the list and reenable input.
				tapCallbacks.Clear();
				NGUIExt.enableAllMouseInput();
			}
		}
	}
	
	public static void touchAnywhere(GenericDelegate callback)
	{
		if (instance != null)
		{
			instance.tapCallbacks.Add(callback);
			NGUIExt.disableAllMouseInput();
		}
	}
}