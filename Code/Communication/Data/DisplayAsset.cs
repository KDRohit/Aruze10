using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Com.Scheduler;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

/*
Static class with functions for loading textures from a standard location,
usually with a folder/filename taken from SCAT data.

This uses a 2-layer caching system:
1. Active memory caching - a texture recently loaded and still in memory is recycled.
2. Disk caching - a texture that has be previously downloaded is saved for reloading.
*/

public class DisplayAsset : IResetGame
{
	private const int MAX_DOWNLOADS = 4;
	private const float EXCESSIVE_WAIT_TIME = 10f;

	private const string LOADING_INDICATOR_PATH = "assets/data/common/bundles/initialization/prefabs/misc/loading circle.prefab";
	private const string BROKEN_IMAGE_PATH = "assets/data/common/bundles/initialization/prefabs/misc/broken image.prefab";
	
	private const string TEXTURE_DOWNLOAD_FLOW_PREFIX = "texture-load-";
	private const float STREAMED_TEXTURE_USERFLOW_DEFAULT_SAMPLE_RATE = 0.01f;

	private class CachedTexture
	{
		public string name;
		public Texture2D texture = null;
		public bool wasStreamed = false;
		private bool isDestroyed = false;
		
		public static CachedTexture success(Texture2D tex, bool streamed)
		{
			CachedTexture cached = new CachedTexture();
			cached.name = tex.name;
			cached.texture = tex;
			cached.wasStreamed = streamed;
			return cached;
		}
		
		public static CachedTexture fail(string primaryPath)
		{
			CachedTexture cached = new CachedTexture();
			cached.name = primaryPath;
			return cached;
		}

		public void destroyTexture()
		{
			if (texture == null || isDestroyed)
			{
				return;
			}

			GameObject.Destroy(texture);
			texture = null;
			isDestroyed = true;
		}
	}

	private static GameObject noPanelLoadingIndicatorPrefab
	{
		get
		{
			if (_noPanelLoadingIndicatorPrefab == null)
			{
				_noPanelLoadingIndicatorPrefab = Resources.Load("Prefabs/Misc/Loading Circle (no panel)") as GameObject;
			}

			return _noPanelLoadingIndicatorPrefab;
		}
	}
	
	private static GameObject loadingIndicatorPrefab
	{
		get
		{
			if (_loadingIndicatorPrefab == null)
			{
				_loadingIndicatorPrefab = SkuResources.getObjectFromMegaBundle<GameObject>(LOADING_INDICATOR_PATH);
			}
			return _loadingIndicatorPrefab;
		}
	}
	private static GameObject _loadingIndicatorPrefab = null;
	private static GameObject _noPanelLoadingIndicatorPrefab = null;

	private static GameObject brokenImagePrefab
	{
		get
		{
			if (_brokenImagePrefab == null)
			{
				_brokenImagePrefab = SkuResources.getObjectFromMegaBundle<GameObject>(BROKEN_IMAGE_PATH);
			}

			return _brokenImagePrefab;
		}
	}
	
	public static int SessionCacheCount => sessionCache.Count;
	
	private static GameObject _brokenImagePrefab = null;
	
	// Keep track of which Transforms have loading indicators, so we don't make more than one per transform.
	private static Dictionary<Transform, bool> imageTransforms = new Dictionary<Transform, bool>();
	
	// This is a cache that keeps track of loaded texture files
	private static Dictionary<string, CachedTexture> sessionCache = new Dictionary<string, CachedTexture>();

	// This is a cache of the pending downloads, so that we can throttle requests.
	private static List<string> pendingDownloads = new List<string>();

	private static Dictionary<string, UnityWebRequest> currentDownloads = new Dictionary<string, UnityWebRequest>();

	private static Dictionary<string, float> trackedDownloads = new Dictionary<string, float>();

	public static void trackAsset(string assetNameOrPath)
	{
		if (!trackedDownloads.ContainsKey(assetNameOrPath))
		{
			trackedDownloads.Add(assetNameOrPath, Time.time);
		}
	}

	// Makes sure a texture is cached before it is actually needed,
	// so there is no delay in waiting for it to download when it needs to be shown.
	public static void preloadTexture(string imagePath, bool skipMapping = false)
	{
		string resourcePath = findResourcePath(imagePath);
		if (SkuResources.loadSkuSpecificResourceImage(resourcePath) != null)
		{
			// A local asset was found, so there is no need to cache.
			// This is usually the situation when an image exists in the project
			// for bundling, but we're running in the editor and not using asset bundles.
			return;
		}

		string bundle = AssetBundleManager.getBundleNameForResource(resourcePath);
		if (!string.IsNullOrEmpty(bundle))
		{
			// Use asset bundle caching for bundled images (cheaper and safer).
			AssetBundleManager.downloadAndCacheBundle(bundle, skipMapping:skipMapping);
		}
		else
		{
			// Use this for non-bundled images.
			RoutineRunner.instance.StartCoroutine(loadTexture(imagePath, preloadCallback));
		}
	}

	/*=========================================================================================
	CONCURRENT QUEUE MANAGEMENT FUNCTIONS
	=========================================================================================*/
	private static void killAllDownloads()
	{
		foreach (KeyValuePair<string, UnityWebRequest> entry in currentDownloads)
		{
			UnityWebRequest request = entry.Value;

			if (request != null)
			{
				// Fixed in Unity 2019.2a3: https://issuetracker.unity3d.com/issues/argumentnullexception-is-thrown-when-yielding-and-disposing-a-www-object
				//request.Dispose();
			}
		}

		currentDownloads = new Dictionary<string, UnityWebRequest>();
	}

	public static void killDownload(string path)
	{
		if (!currentDownloads.ContainsKey(path))
		{
			return;
		}

		UnityWebRequest request = currentDownloads[path];

		if (request != null)
		{
			// Fixed in 2019: https://issuetracker.unity3d.com/issues/argumentnullexception-is-thrown-when-yielding-and-disposing-a-www-object
			//request.Dispose();
		}
		currentDownloads.Remove(path);
		pendingDownloads.Remove(path);
	}

	private static void clearDownload(string path, bool shouldPrep = false)
	{
		currentDownloads.Remove(path);
		pendingDownloads.Remove(path);
	}

	private static void prepNextDownload()
	{
		if (pendingDownloads.Count <= 0)
		{
			return;
		}

		string path = pendingDownloads[0];
		pendingDownloads.Remove(path);
		addDownload(path, null);

		if (Data.debugMode)
		{
			Debug.Log("Prepping next download: " + path);
		}
	}

	private static void addDownload(string path, UnityWebRequest loader)
	{
		if (!currentDownloads.ContainsKey(path))
		{
			currentDownloads.Add(path, loader);
		}
	}

	private static void preloadCallback(Texture2D tex, Dict data)
	{
		// This is only safe on a streamed texture that is NOT in an asset bundle.
		Object.Destroy(tex);
	}

	// Attempts to load an image from the asset bundle system first, falling back to a standard loadTexture call with the same params.
	// The path information is a path relative to Resources/Images/, and is treated exactly like the loadTexture() call.
	// We don't actually need to know the bundle name because that is stored in the manifest that AssetBundleManager uses.
	// Note that:
	// By default skipBundleMapping is false which means it will load ALL assets in the bundle, not just the requested one.
	// The requested texture will NOT be stored in the sessionCache unless we fallback to loadTexture()
	public static IEnumerator loadTextureFromBundle
	(
		string primaryPath,
		TextureDelegate callback,
		Dict data = null,
		string secondaryPath = "",
		bool isExplicitPath = false,
		bool loadingPanel = true,
		AssetFailDelegate onDownloadFailed = null, // if you want to handle special cases for download failures
		bool skipBundleMapping = false,
		string pathExtension = "",
		bool showLoadingCircle = true
	)
	{
		bool isWebAsset = primaryPath.FastStartsWith("http");
		Texture2D tex = null;
		Transform imageTransform = null;
		GameObject loadingIndicator = null;

		// Optionally setup the loading indicator
		if (data != null)
		{
			imageTransform = data.getWithDefault(D.IMAGE_TRANSFORM, null) as Transform;

			if (imageTransform != null && imageTransforms.ContainsKey(imageTransform))
			{
				// If this parent is already registered, don't use it again.
				imageTransform = null;
			}

			if (imageTransform != null && showLoadingCircle)
			{
				imageTransforms.Add(imageTransform, true);
				loadingIndicator = addIndicatorIcon(imageTransform, loadingPanel ? loadingIndicatorPrefab : noPanelLoadingIndicatorPrefab, false);
			}
		}
		
#if UNITY_EDITOR
		// A test case - add some time here to simulate slow downloading.
		float streamDelay = PlayerPrefsCache.GetFloat(DebugPrefs.STREAM_TEXTURES_DELAY, 0.0f);
		if (streamDelay > 0.0f)
		{
			yield return new WaitForSeconds(streamDelay);
		}

		// This is a test case just to make sure the game functions when textures fail to load
		if (PlayerPrefsCache.GetInt(DebugPrefs.STREAM_NULL_TEXTURES, 0) != 0)
		{
			if (!sessionCache.ContainsKey(primaryPath))
			{
				sessionCache.Add(primaryPath, CachedTexture.fail(primaryPath));
			}
			else
			{
				Debug.LogWarning("DisplayAsset.loadTextureFromBundle(): Duplicate call for texture " + primaryPath);
			}
			
			yield return null;	// We must yield a frame for the load indicator display and callback to work as expected.
			finishAndCallback(tex, data, loadingIndicator, imageTransform, callback);
			clearDownload(primaryPath);
			yield break;
		}
		
#endif

		bool callbackComplete = false;
		if (!isWebAsset)
		{
			// Setup a local load callback so we can set callbackComplete when done
			AssetLoadDelegate bundleLoadCallback =
				(string dummyPath, Object obj, Dict dummyData) =>
				{
					callbackComplete = true;
					tex = obj as Texture2D;
				};
		
			// Setup a local fail callback so we can set callbackComplete when done
			AssetFailDelegate bundleFailCallback =
				(string dummyPath, Dict dummyData) =>
				{
					if (onDownloadFailed != null)
					{
						onDownloadFailed(dummyPath, dummyData);
					}
					// Adding some allowed cases here to stop spam.
					if (!primaryPath.Contains("lobby_carousel/") && !primaryPath.Contains("lobby_options/") && !primaryPath.Contains("inbox_backgrounds/"))
					{
#if UNITY_EDITOR
						Debug.LogError($"Failed to download tex={primaryPath} via ABM.load() from bundle/resources (might be a streamed texture and should use loadTexture explicitly)");
#else
						Bugsnag.LeaveBreadcrumb($"DisplayAsset tex={primaryPath} loadTextureFromBundle() via ABM.load() couldn't get texture from bundle/resources (might be a streamed texture and should use loadTexture explicitly)");
#endif
					}
					callbackComplete = true;
				};
			
			// Attempt to load from the AssetBundleManager only if it is potentially in a bundle
			string path = isExplicitPath ? primaryPath : findResourcePath(primaryPath); //Lets not append Images/ if we're giving an explicit path
			AssetBundleManager.load(path, bundleLoadCallback, bundleFailCallback, data, false, skipBundleMapping, pathExtension);
		
			// Wait for AssetBundleManager to call the above callbacks
			while (!callbackComplete)
			{
				yield return null;
			}
		}

		// A texture would be assigned to tex by now, assuming no errors
		if (tex != null)
		{
			finishAndCallback(tex, data, loadingIndicator, imageTransform, callback);
		}
		else
		{
			// Clean up loading indicator because another is about to be made
			if (loadingIndicator != null)
			{
				GameObject.Destroy(loadingIndicator);
			}
			
			if (imageTransform != null)
			{
				imageTransforms.Remove(imageTransform);
			}
			
			// Fallback to standard loadTexture() call
			yield return RoutineRunner.instance.StartCoroutine(loadTexture(primaryPath, callback, data, secondaryPath, isExplicitPath, showLoadingCircle: showLoadingCircle));
		}
	}
	
	// A special public method just for the loading screen because the AssetBundleManager is not always available when the loading screen is loaded, and the loading screen images are not bundled.
	public static void loadLoadingScreenTexture(string primaryPath, TextureDelegate callback, Dict data = null, string secondaryPath = "", bool isExplicitPath = false)
	{
		RoutineRunner.instance.StartCoroutine(loadTexture(primaryPath, callback, data, secondaryPath, isExplicitPath));
	}

	// A special public method just for the loading screen because the AssetBundleManager is not always available when the loading screen is loaded, and the loading screen images are not bundled.
    public static void loadLoadingScreenTexture(MonoBehaviour loadingScript, string primaryPath, TextureDelegate callback, Dict data = null, string secondaryPath = "", bool isExplicitPath = false)
	{
		loadingScript.StartCoroutine(loadTexture(primaryPath, callback, data, secondaryPath, isExplicitPath));
	}
    
    // A special public method to reload the original Loading screen background, since it comes from the mega initialiation bundle
    public static void loadLoadingScreenDefaultTexture(string primaryPath, TextureDelegate callback, Dict data = null, string secondaryPath = "", bool isExplicitPath = false)
    {
	    RoutineRunner.instance.StartCoroutine(loadTextureFromBundle(primaryPath, callback, data, secondaryPath, isExplicitPath, false, null, true, ".png", false ));
    }
	
	// Loads a texture from either the local Resources folder (findResourcePath), the local web cache, or from the web.
	public static IEnumerator loadTexture
	(
		string primaryPath,
		TextureDelegate callback,
		Dict data = null,
		string secondaryPath = "",
		bool isExplicitPath = false,
		bool loadingPanel = true,
		bool showLoadingCircle = true
	)
	{
		if (string.IsNullOrEmpty(primaryPath) && string.IsNullOrEmpty(secondaryPath))
		{
			// If we are trying to download a texture but both the paths are empty/null, bail here.
#if UNITY_EDITOR
			Debug.LogErrorFormat("DisplayAsset.cs -- loadTexture() -- both url paths are empty");
#endif
			yield break;
		}

		if (string.IsNullOrEmpty(primaryPath))
		{
			// If the primary path is empty, dont bother attempting to download it, just swap the secondary path in.
			primaryPath = secondaryPath;
			secondaryPath = "";
		}

		if (pendingDownloads == null)
		{
			pendingDownloads = new List<string>();
		}

		if (!pendingDownloads.Contains(primaryPath) && !currentDownloads.ContainsKey(primaryPath))
		{
			pendingDownloads.Add(primaryPath);
		}
		
		float callStartTime = Time.realtimeSinceStartup;
		while (!doesFileExistInCache(primaryPath) &&
		       (currentDownloads.Count >= MAX_DOWNLOADS || currentDownloads.ContainsKey(primaryPath)))
		{
			if (Time.realtimeSinceStartup - callStartTime > EXCESSIVE_WAIT_TIME)
			{
				killAllDownloads();
				callStartTime = Time.realtimeSinceStartup;
			}
			else
			{
				yield return null;
			}
		}

		prepNextDownload();
		
		if (callback == null)
		{
			// We waited too long, the callback is dead, abort.
			yield break;
		}
		
		Texture2D tex = null;
		string path = primaryPath;
		
		Transform imageTransform = null;
		GameObject loadingIndicator = null;

		// Optionally setup the loading indicator
		if (data != null)
		{
			imageTransform = data.getWithDefault(D.IMAGE_TRANSFORM, null) as Transform;

			if (imageTransform != null && imageTransforms.ContainsKey(imageTransform))
			{
				// If this parent is already registered, don't use it again.
				imageTransform = null;
			}

			if (imageTransform != null && showLoadingCircle)
			{
				imageTransforms.Add(imageTransform, true);
				loadingIndicator = addIndicatorIcon(imageTransform, loadingPanel ? loadingIndicatorPrefab : noPanelLoadingIndicatorPrefab, false);
			}
		}
		
#if UNITY_EDITOR
		// A test case - add some time here to simulate slow downloading.
		float streamDelay = PlayerPrefsCache.GetFloat(DebugPrefs.STREAM_TEXTURES_DELAY, 0.0f);
		if (streamDelay > 0.0f)
		{
			yield return new WaitForSeconds(streamDelay);
		}

		// This is a test case just to make sure the game functions when textures fail to load
		if (PlayerPrefsCache.GetInt(DebugPrefs.STREAM_NULL_TEXTURES, 0) != 0)
		{
			if (!sessionCache.ContainsKey(primaryPath))
			{
				sessionCache.Add(primaryPath, CachedTexture.fail(primaryPath));
			}
			else
			{
				Debug.LogWarning("DisplayAsset.loadTexture(): Duplicate call for texture " + primaryPath);
			}
			
			yield return null;	// We must yield a frame for the load indicator display and callback to work as expected.

			finishAndCallback(tex, data, loadingIndicator, imageTransform, callback);
			clearDownload(primaryPath);
			prepNextDownload();
			yield break;
		}
		
#endif
				
		string flowTransactionName = TEXTURE_DOWNLOAD_FLOW_PREFIX + primaryPath;
		int size = 0;
		Userflows.flowStart(flowTransactionName, Data.liveData != null ? Data.liveData.getFloat("TEXTURE_DOWNLOAD_FLOW_SAMPLE_RATE", STREAMED_TEXTURE_USERFLOW_DEFAULT_SAMPLE_RATE) : STREAMED_TEXTURE_USERFLOW_DEFAULT_SAMPLE_RATE);
		string textureSource = "";
		
		//
		// See if we already tried to load this image this session, and if that cached image is still available
		//
		if (sessionCache.ContainsKey(primaryPath))
		{
			CachedTexture cached = sessionCache[primaryPath];
			//if (cached.texture != null || cached.hasFailed)
			if (cached.texture != null)
			{
				tex = cached.texture;
				path = "";	// Clear path to skip main loading loop below
				textureSource = "session_cache";
			}
		}
		
		bool wasStreamed = true;

		while (!string.IsNullOrEmpty(path))
		{
			// First try loading from local resources.
			if (isExplicitPath)
			{
				if (path.FastStartsWith("http:") || path.FastStartsWith("https:"))
				{
					//	Debug.LogErrorFormat("downloading from {0}", path);
					using (UnityWebRequest loader = UnityWebRequest.Get(path))
					{
						addDownload(path, loader);

						yield return loader.SendWebRequest();

						if (!loader.isNetworkError && !loader.isHttpError)
						{
							tex = new Texture2D(0, 0, TextureFormat.RGB24, false); //Size & Format values don't matter much here. LoadImage will overwrite the size & format 
							tex.LoadImage(loader.downloadHandler.data, true);

							if (tex != null)
							{
								size = (int)loader.downloadedBytes;
								ridiculousTextureCheck(tex, path, size);
							}

							textureSource = "remote_server";
						}
					}
				}
				else
				{
					string resourcePath = findResourcePath(path);
					
					// First try sku specific resource loading.
					// This only works for PNGs in the correct sku-specific folders.
					tex = SkuResources.loadSkuSpecificResourcePNG(resourcePath);
					if (tex == null)
					{
						// Second try general resource loading, which works for all formats and resources locations.
						tex = Resources.Load(resourcePath) as Texture2D;
					}
					
					if (tex != null)
					{
						wasStreamed = false;
						textureSource = "resources";
					}
					
					// If the texture fails to load when an explicit resource path is provided,
					// there is no fallback to try loading from web. Typically the secondaryPath is used instead.
				}
			}
			else
			{
				tex = Resources.Load(findResourcePath(path)) as Texture2D;
				if (tex != null)
				{
					wasStreamed = false;
					textureSource = "resources";
				}
				
				//
				// If we didn't load it from Resources, attempt to load it from:
				// - the device disk cache (via .version and .data files)
				// - a remote location
				//
				if (tex == null)
				{
					string cachePath = "";
					string versionPath = "";
					string dataPath = "";

#if !UNITY_WEBGL
					// BY: Hi there! Are you looking at this section of code trying to diagnose a problem with
					// textures being null in dialogs? If so, the issue is probably stemming from InbetweenSceneLoader.cs
					// calling DisplayAsset.cleanupSessionCache(). To fix this problem, we added some conditions to cleanupSessionCache()
					// but it doesn't apply to all cases. You may need to add on to those conditions.
					
					//
					// Check if device cache is available
					//
					if (!string.IsNullOrEmpty(FileCache.path))
					{
						// Response caching consists of two files, one that tracks/validates the version and the other that actually stores the data.
						// This prevents us from collecting up dead versions of files over time, since every version of a file uses the same cache path.
						cachePath = findCachedUrl(path);
						versionPath = cachePath + ".version";
						dataPath = cachePath + ".data";

						if (File.Exists(versionPath) && File.Exists(dataPath))
						{
							string version = "";
							
							// It's lazy, but a generic try-catch here makes life easier.
							try
							{
								version = File.ReadAllText(versionPath, System.Text.Encoding.UTF8);
							}
							catch
							{
								version = "";
								Debug.LogWarning("Failed to process read of cached texture version: " + versionPath);
							}
							
							int versionNum = Glb.mobileStreamingAssetsVersion;
							if (versionNum == 0)
							{
								// If we are too early to get the streaming asset version as 0, then we might be too early
								// in the loading cycle to have gotten this from data, so check PlayerPrefs for the last one.
								versionNum = PlayerPrefsCache.GetInt(Prefs.MOBILE_ASSET_STREAMING_VERSION, 0);
							}
							
							if (version != "" && version == versionNum.ToString())
							{
								if (Data.debugMode)
								{
									Debug.LogFormat($"Attempting to load: file://{dataPath}");
								}

								// Attempt to load the cached data.
								using (UnityWebRequest cacheLoader = UnityWebRequest.Get("file://" + dataPath))
								{
									currentDownloads[path] = cacheLoader;
									yield return cacheLoader.SendWebRequest();
								
									if (!cacheLoader.isNetworkError && !cacheLoader.isHttpError)
									{
										tex = new Texture2D(0, 0, TextureFormat.RGB24, mipChain: false);
										tex.LoadImage(cacheLoader.downloadHandler.data, true);

										size = (int)cacheLoader.downloadedBytes;
										textureSource = "device_cache";
									}
									else
									{
										Debug.LogWarning("Failed to process read of cached texture data: " + dataPath);
									}
								}
							}
							
							if (tex == null)
							{
								// The cached data was either outdated or corrupted, delete the files.
								attemptDelete(dataPath);
								attemptDelete(versionPath);
							}
							else
							{
								Debug.Log("Texture was cached file://" + dataPath);
							}
						}
					}
#endif					
					//
					// If it is not cached, try loading from remote web location.
					//
					if (tex == null)
					{
						// If we want to measure time taken, lets set that up here as well
						// as take a peek as to what our spin state was so if we weren't spinning
						// when we started this process, we'll know if this took us so long
						// that the user had time to spin again.
						if (data != null && data.containsKey(D.DIALOG_TYPE))
						{
							data.Add(D.TIME, GameTimer.currentTime);
							data.Add(D.IS_WAITING, Glb.spinTransactionInProgress);
						}
						
						// Start downloading the textures.
						string[] urls = findAllImageUrls(path);
						foreach (string url in urls)
						{
							Debug.Log("Loading from url: " + url);
							using (UnityWebRequest loader = UnityWebRequest.Get(url))
							{
								addDownload(url, loader);
							
								yield return loader.SendWebRequest();

								//Handle error and early continue to try next url
								if (loader.isNetworkError || loader.isHttpError)
								{
									if (Data.debugMode)
									{
										// This is a retry, so the message is a warning
										Debug.LogErrorFormat("Error downloading texture at {0}: {1}", url, loader.error);
										clearDownload(url);
									}

									continue;
								}
								byte[] downloadedBytes = loader.downloadHandler.data;
								
								tex = new Texture2D(0, 0, TextureFormat.RGB24, mipChain: false);
								tex.LoadImage(downloadedBytes, true);

								// Try the next URL
								if (tex == null)
								{
									continue;
								}

								// Check the texture to make sure it isn't ridiculous.
								size = downloadedBytes.Length;
								ridiculousTextureCheck(tex, url, size);
								clearDownload(url);
							
								textureSource = "remote_server";

								// The data is received and assumed good. Do we support caching?
								if (!string.IsNullOrEmpty(FileCache.path))
								{
									// Response caching consists of two files, one that tracks/validates the version and the other that actually stores the data.
									// It's sloppy, but a generic try-catch here makes life easier.
									try
									{
										File.WriteAllText(versionPath, Glb.mobileStreamingAssetsVersion.ToString(),
											System.Text.Encoding.UTF8);
										File.WriteAllBytes(dataPath, downloadedBytes);
									}
									catch
									{
										attemptDelete(dataPath);
										attemptDelete(versionPath);
										Debug.LogWarning("Failed to process write for caching request: " + cachePath);
									}
								}

								break;
							}
						}
					}
				}
			}

			if (tex == null && path == primaryPath && secondaryPath != "")
			{
				// Didn't find an image in the first path, and a secondary path was provided,
				// so check the secondary path now.
				path = secondaryPath;
			}
			else
			{
				// Get out of the loop.
				path = "";
			}
		}
		
		if (tex != null)
		{
			// Manually setting the wrap mode to clamp just in case.
			tex.wrapMode = TextureWrapMode.Clamp;
			tex.name = primaryPath;
		}
		
		// Cache this for the session
		if (sessionCache.ContainsKey(primaryPath))
		{
			CachedTexture cached = sessionCache[primaryPath];
			if (tex != null && cached.texture == null)
			{
				// A destroyed texture is re-cached
				cached.texture = tex;
				cached.wasStreamed = wasStreamed;
			}
		}
		else
		{
			if (tex == null)
			{
				// If we are adding a freshly missing texture to the cache, log an error
				if (Data.debugMode)
				{
					Debug.LogErrorFormat("Could not load texture: '{0}'", primaryPath);
					if (!string.IsNullOrEmpty(secondaryPath))
					{
						Debug.LogErrorFormat("Could not load secondary texture '{0}'", secondaryPath);
					}
				}
				
				// Cache this as a failure
				if (!primaryPath.Contains("loading_screen"))
				{
					sessionCache.Add(primaryPath, CachedTexture.fail(primaryPath));
				}
			}
			else
			{
				// Cache this as a success
				if (wasStreamed && !primaryPath.Contains("loading_screen"))
				{
					sessionCache.Add(primaryPath, CachedTexture.success(tex, wasStreamed));
				}
			}
		}

		if (tex != null)
		{
			Userflows.addExtraFieldToFlow(flowTransactionName, "size", size.ToString());
		}
	
#if UNITY_2018_2_OR_NEWER
		Userflows.addExtraFieldToFlow(flowTransactionName, "totalTextureMemory", Texture.totalTextureMemory.ToString());
#endif
		Userflows.addExtraFieldToFlow(flowTransactionName, "texture_source", textureSource);

		Userflows.flowEnd(flowTransactionName, tex != null);
		finishAndCallback(tex, data, loadingIndicator, imageTransform, callback);
		clearDownload(primaryPath);
	}
	
	private static void finishAndCallback(Texture2D tex, Dict data, GameObject loadingIndicator, Transform imageTransform, TextureDelegate callback)
	{
		if (tex != null && trackedDownloads.ContainsKey(tex.name))
		{
			float dt = Time.time - trackedDownloads[tex.name];
			Debug.Log("~Download time: " + dt);
			trackedDownloads.Remove(tex.name);
		}

		if (loadingIndicator != null)
		{
			GameObject.Destroy(loadingIndicator);
		}
		
		if (tex == null && imageTransform != null)
		{
			// Nullcheck imageTransform again here just in case it was destroyed before getting here.
			// BY: 09/30/2019 - removed this as per eric/noels request
			/*bool shouldHideBroken = (bool)data.getWithDefault(D.SHOULD_HIDE_BROKEN, false);
			if (data != null && !shouldHideBroken)
			{
				// We want to be able to now show the broken image prefab if so desired.
				addIndicatorIcon(imageTransform, brokenImagePrefab, true);	
			}*/
		}

		if (imageTransform != null)
		{
			imageTransforms.Remove(imageTransform);
		}

		if (data != null && data.containsKey(D.TIME) && data.containsKey(D.DIALOG_TYPE))
		{
			bool wasSpinningAtLaunch = true; // Default to true, we only care if were explicitly NOT spinning...
			int timeStamp = (int)data[D.TIME];
			string dialogTypeKey = (string)data[D.DIALOG_TYPE];
			if (data.containsKey(D.IS_WAITING))
			{
				wasSpinningAtLaunch = (bool)data[D.IS_WAITING];
			}

			timeStamp = GameTimer.currentTime - timeStamp;

			string downloadTimeLog = string.Format("Time taken to download dialog images for {0} was {1} second(s)", dialogTypeKey, timeStamp);

			if (Glb.spinTransactionInProgress && !wasSpinningAtLaunch)
			{
				Debug.LogError("Dialog attempted to show after downloads while spinning! " + downloadTimeLog);
			}

#if !ZYNGA_PRODUCTION
			else if (timeStamp != 0)
			{
				Debug.LogError(downloadTimeLog);
			}
#endif
		}

		// It is very possible that in the time it takes to load the image, the callback object has gone away
		if (callback != null)
		{
			callback(tex, data);
		}
	}
	
	// Log errors if the size of a streamed texture it too big in some way.
	private static void ridiculousTextureCheck(Texture2D tex, string url, int byteCount)
	{
		if (!Data.debugMode)
		{
			// Only do this for nonproduction builds.
			return;
		}
		
		if (tex.width > 1024 || tex.height > 1024)
		{
			// All streamed images should be no larger than 1024x1024.
			Debug.LogErrorFormat("Streamed image dimensions are too big at {0}x{1} on url {2}", tex.width, tex.height, url);
		}
		
		int byteLimit = tex.width * tex.height;
		
		// Include a ? because streamed image URL's have a querystring value for the version.
		if (url.ToLower().Contains(".jpg?"))
		{
			byteLimit /= 2;
		}
		else
		{
			// PNG's are typically a bit larger.
			byteLimit = (int)(1.5f * byteLimit);
		}
		
		if (byteCount > byteLimit)
		{
			Debug.LogErrorFormat("Streamed image file size {0}K is too big max size is {4}K for {1}x{2} on url {3}.", byteCount/1024, tex.width, tex.height, url, byteLimit/1024);
		}
	}
		
	// Returns the full Resources path to the given resource's relative path.
	private static string findResourcePath(string path)
	{
		return "Images/" + CommonText.baseFilename(path);
	}

	// Returns all available full URLs to the given resource's relative path.
	private static string[] findAllImageUrls(string path)
	{

		string canonicalUrl = "";
		if (path.FastStartsWith("http:") || path.FastStartsWith("https:"))
		{
			canonicalUrl = path;
		}
		else
		{
			canonicalUrl = string.Format(
				"{0}Images/{1}?version={2}",
				Glb.mobileStreamingAssetsUrl,
				path,
				Glb.mobileStreamingAssetsVersion);
		}

		return Server.findAllStaticUrls(canonicalUrl);
	}

	// Returns whether the given texture is already cached
	public static bool isTextureDataCachedOnDisk(string path)
	{
		string cachePath = findCachedUrl(path);
		string dataPath = cachePath + ".data";
		if (File.Exists(dataPath) || Resources.Load(findResourcePath(path)) != null)
		{
			return true;
		}
		return false;
	}
	
	// Returns the full path to where the resource would be located in the cache (does not guarantee it exists).
	private static string findCachedUrl(string path)
	{
		return FileCache.path + path.Replace("/", ".");
	}
	
	// Simple method to make file deletes shorter elsewhere.
	private static void attemptDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch
		{
			// Nothing to catch here
		}
	}

	public static void forceClearSessionCache()
	{
		if (sessionCache == null)
		{
			return;
		}
		
		foreach (KeyValuePair<string, CachedTexture> kvp in sessionCache)
		{
			CachedTexture cached = kvp.Value;
			if (cached == null)
			{
				continue;
			}
			cached.destroyTexture();
		}
				
		sessionCache.Clear();			
		sessionCache = new Dictionary<string, CachedTexture>();
	}

	public static void outputSessionCacheStats()
	{
		long unityCacheSize = 0;
		
		StringBuilder assetInfoBuilder = new StringBuilder();
		foreach (KeyValuePair<string, CachedTexture> kvp in sessionCache)
		{
			if (kvp.Key == null)
			{
				assetInfoBuilder.Append("sessionCache key is null ");	
			}
			
			CachedTexture cachedTex = kvp.Value;
			if (cachedTex == null)
			{
				assetInfoBuilder.AppendLine($"CachedTexture object is null for key {kvp.Key ?? "null"}");
				continue;
			}

			Texture2D tex = cachedTex.texture;
			if (tex == null)
			{
				assetInfoBuilder.AppendLine($"CachedTexture's Texture2D is null for {cachedTex.name ?? "unnamed"}");
				continue;
			}

			//Note: tex.GetRawTextureData() does not give accurate info on-device if Tex is not read/write enabled
			//Estimating memory use based on dimensions and bit depth is inaccurate, Unity's reported runtime
			//memory use is often higher, sometimes double
			long unitySize = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex);
			unityCacheSize += unitySize;

			string dimensionSnippet = $"({tex.width}x{tex.height})";
			string formatSnippet = $"[{tex.format}]";
			assetInfoBuilder.AppendLine($"{dimensionSnippet,11} {formatSnippet,9} {unitySize/1024,6}kb {tex.name ?? "unnamed"}");
		}
		
		StringBuilder summaryInfoBuilder = new StringBuilder();
		summaryInfoBuilder.AppendLine($"DisplayAsset sessionCache count: {sessionCache.Count}");
		summaryInfoBuilder.AppendLine($"Total cache Unity runtime size: {unityCacheSize:N0}b");
		summaryInfoBuilder.Append(assetInfoBuilder);
		
		Debug.Log(summaryInfoBuilder.ToString());
	}

	// Cleans up the session cache to free up memory and/or guarantee 
	// that a newly loaded texture is freshly loaded.
	public static void cleanupSessionCache()
	{
		if (sessionCache == null)
		{
			return;
		}

		outputSessionCacheStats();
		
		Dictionary<string, CachedTexture> newSessionCache = new Dictionary<string, CachedTexture>();
		foreach (KeyValuePair<string, CachedTexture> p in sessionCache)
		{
			if (p.Key != null)
			{
				if (p.Key.Contains("facebook") || p.Key.Contains("profile"))
				{
					// If this is a profile image, then don't remove it during cleanup.
					newSessionCache.Add(p.Key, p.Value);
					continue;
				}
				
				// don't clear any motd textures if the Scheduler is waiting to show something
				// it could be what's needed. this will get cleared when going back into a game
				// so it's pretty safe
				if (p.Key.Contains("motd") && Scheduler.hasTask)
				{
					continue;
				}
			}

			if (p.Value == null)
			{
				continue;
			}

			CachedTexture cached = p.Value;
			if (cached.texture != null && cached.wasStreamed)
			{
				Debug.Log($"Evicted {p.Key}: {cached.texture.name} from DisplayAsset session cache");
				cached.destroyTexture();
			}
		}
		
		sessionCache.Clear();	// Clear out the cache entries to remove any lingering entries
		sessionCache = newSessionCache;	// Free up the cache object reference for garbage collection
	}
	
	// Returns whether a texture was streamed and is not a lobby option image.
	// False if loaded from a local resource or not found in the cache.
	public static bool wasStreamedNonLobbyOptionTexture(Texture2D tex)
	{
		if (tex == null)
		{
			return false;
		}
		
		foreach (KeyValuePair<string, CachedTexture> kvp in sessionCache)
		{
			string path = kvp.Key;
			CachedTexture cached = kvp.Value;
			
			if (cached.texture == tex && !path.FastStartsWith("lobby_options/"))
			{
				return cached.wasStreamed;
			}
		}
		return false;
	}
	
	private static GameObject addIndicatorIcon(Transform imageTransform, GameObject prefab, bool isLowerPosition)
	{
		// Creates an indicator that is positioned in front of the given parent.
		// The parent should typically be the Transform of UITexture or Renderer that the loading texture will be applied to.
		// The positioning of the indicator assumes that the texture parent is centered on its own Transfor (mainly applies to UITextures).
		if (prefab == null)
		{
			Debug.LogError("Trying to add a null indicator icon");
			return null;
		}
		GameObject indicator = NGUITools.AddChild(imageTransform.parent.gameObject, prefab);

		UISprite sprite = indicator.GetComponent<UISprite>();
		if (sprite != null)
		{
			sprite.MakePixelPerfect();
		}
		
		// Position the indicator just in front of the parent.
		// if the indicator has its own UIPanel it should layer correctly -- otherwise pass in an appropriate image transform
		if (isLowerPosition)
		{
			// We want to position this in the lower left corner of the image.
			indicator.transform.localPosition = new Vector3(
				imageTransform.localPosition.x,
				imageTransform.localPosition.y - imageTransform.localScale.y * 0.5f + sprite.transform.localScale.y * 0.5f + 10.0f,
				-1.0f
			);
		}
		else
		{
			// If not in the lower corner, then center it.
			indicator.transform.localPosition = imageTransform.localPosition;
			CommonTransform.setZ(indicator.transform, -1.0f);
		}

		return indicator;
	}

	// Loads a texture and applies it to the given UITexture.
	// Pass null for uiTexture if preloading.
	public static void loadTextureToUITexture
	(
		UITexture uiTexture,
		string url,
		string fallbackUrl = "",
		bool isExplicitPath = false,
		bool shouldShowBrokenImage = true,
		bool skipBundleMapping = false,
		string pathExtension = "",
		bool showLoadingCircle = true,
		System.Action<Material> newMaterialCallback = null
	)
	{
		if (uiTexture == null)
		{
			Debug.LogWarning("DisplayAsset.cs -- loadTextureToUITexture -- uiTexture is null, not starting download");
		}
		Dict args = Dict.create(D.IMAGE_TRANSFORM, (uiTexture != null ? uiTexture.transform : null));
		args.Add(D.TEXTURE, uiTexture);
		if (newMaterialCallback != null)
		{
			args.Add(D.CALLBACK, newMaterialCallback);
		}

		if (!shouldShowBrokenImage)
		{
			args.Add(D.SHOULD_HIDE_BROKEN, true);
		}

		RoutineRunner.instance.StartCoroutine(loadTextureFromBundle(
			url,
			loadTextureToUITextureCallback,
			args,
			fallbackUrl,
			isExplicitPath,
			true,
			null,
			skipBundleMapping,
			pathExtension,
			showLoadingCircle
		));		
	}

	public static void loadTextureToUITextureCallback(Texture2D tex, Dict texData)
	{
		if (tex != null && texData != null)
		{
			UITexture uiTexture = texData.getWithDefault(D.TEXTURE, null) as UITexture;
			if (uiTexture == null)
			{
			    Transform uiTransform = texData.getWithDefault(D.IMAGE_TRANSFORM, null) as Transform;
				if (uiTransform != null)
				{
					uiTexture = uiTransform.GetComponent<UITexture>();
					if (uiTexture == null)
					{
						// MCC -- the dialog might have closed by the time this happens so
						//lets stop yelling about it since it is a valid case.
						Bugsnag.LeaveBreadcrumb("UITexture was null after downloading the texture, bailing.");
						return;
					}
				}
				else
				{
					// Need to return here as well since uiTexture is still null
					// MCC -- the dialog might have closed by the time this happens so
					//lets stop yelling about it since it is a valid case.
					Bugsnag.LeaveBreadcrumb("Transform was null after downloading the texture, bailing.");
					return;
				}
			}

			Material newMat = new Material(uiTexture.material);
			uiTexture.gameObject.SetActive(false);
			uiTexture.material = newMat;
			uiTexture.mainTexture = tex;
			uiTexture.gameObject.SetActive(true);

			System.Action<Material> matFunc = (System.Action<Material>)texData.getWithDefault(D.CALLBACK, null);
			if (matFunc != null)
			{
				matFunc(newMat);
			}
		}
	}

	// Loads a texture and applies it to the given renderer.
	// Pass null for imageRenderer if preloading.
	public static void loadStreamedTextureToRenderer(Renderer imageRenderer, string url, string fallbackUrl = "", bool isExplicitPath = false, bool shouldShowBrokenImage = true)
	{
		if (imageRenderer != null && shouldShowBrokenImage)
		{
			imageRenderer.sharedMaterial = getNewRendererMaterial(imageRenderer);
			imageRenderer.sharedMaterial.color = Color.black;	// Default to black until texture is loaded.
		}

		Dict args = Dict.create(D.IMAGE_TRANSFORM, (imageRenderer != null ? imageRenderer.transform : null));

		if (!shouldShowBrokenImage)
		{
			args.Add(D.SHOULD_HIDE_BROKEN, true);
		}
		
		RoutineRunner.instance.StartCoroutine(loadTexture(
			url,
			loadTextureToRendererCallback,
			args,
			fallbackUrl,
			isExplicitPath
		));
	}
	
	// Loads a texture and applies it to the given renderer.
	// Pass null for imageRenderer if preloading.
	public static void loadTextureToRenderer(Renderer imageRenderer, string url, string fallbackUrl = "", bool isExplicitPath = false, bool shouldShowBrokenImage = true, bool skipBundleMapping = false, string pathExtension = "")
	{
		if (imageRenderer != null && shouldShowBrokenImage)
		{
			imageRenderer.sharedMaterial = getNewRendererMaterial(imageRenderer);
			imageRenderer.sharedMaterial.color = Color.black;	// Default to black until texture is loaded.
		}

		Dict args = Dict.create(D.IMAGE_TRANSFORM, (imageRenderer != null ? imageRenderer.transform : null));

		if (!shouldShowBrokenImage)
		{
			args.Add(D.SHOULD_HIDE_BROKEN, true);
		}
		
		RoutineRunner.instance.StartCoroutine(loadTextureFromBundle(
			url,
			loadTextureToRendererCallback,
			args,
			fallbackUrl,
			isExplicitPath,
			loadingPanel:true,
			onDownloadFailed:null,
			skipBundleMapping:skipBundleMapping,
			pathExtension:pathExtension
		));
	}

	// Texture loaded callback for loadTexture().
	public static void loadTextureToRendererCallback(Texture2D tex, Dict texData)
	{
		if (tex == null)
		{
			return;
		}

		Transform imageTransform = texData.getWithDefault(D.IMAGE_TRANSFORM, null) as Transform;

		if (imageTransform == null)
		{
			// This could happen if preloading an image.
			return;
		}

		Renderer imageRenderer = imageTransform.gameObject.GetComponent<Renderer>();
		imageRenderer.sharedMaterial = getNewRendererMaterial(imageRenderer);

		if (imageRenderer == null)
		{
			// This should never happen.
			Debug.LogError("DialogBase.textureCallback: No imageRenderer data found.");
			return;
		}
		imageRenderer.sharedMaterial.mainTexture = tex;
		imageRenderer.sharedMaterial.color = Color.white;
		imageRenderer.gameObject.SetActive(true);	// Just in case we deactivated the texture object while loading.
	}
	
	public static Material getNewRendererMaterial(Renderer imageRenderer, bool useLobbyShader = false)
	{
		if (imageRenderer == null)
		{
			return null;
		}

		Material mat = imageRenderer.sharedMaterial;
		
		if (mat == null || useLobbyShader)
		{
			// Use the same shader that the lobby options use.
			Shader shader = LobbyOptionButtonActive.getOptionShader();
			if (shader != null)
			{
				mat = new Material(shader);	
			}
			else
			{
				//we couldn't load the shader, there is a bigger problem
				Debug.LogError("Could not load shader from shader cache");
				mat = null;
			}
			
		}
		else
		{
			CustomLog.Log.log("using existing material: " + mat.name);
			// If there is already a material on this renderer,
			// then use a copy of it, since it may have a special shader we want.
			mat = new Material(mat);
		}	
		return mat;
	}

	/** returns true if the path exists in session or device cache */
	private static bool doesFileExistInCache(string path)
	{
		if (sessionCache != null && sessionCache.ContainsKey(path))
		{
			return true;
		}

		if (string.IsNullOrEmpty(FileCache.path))
		{
			return false;
		}

		// Response caching consists of two files, one that tracks/validates the version and the other that actually stores the data.
		// This prevents us from collecting up dead versions of files over time, since every version of a file uses the same cache path.
		string cachePath = findCachedUrl(path);
		string versionPath = cachePath + ".version";
		string dataPath = cachePath + ".data";

		return File.Exists(versionPath) && File.Exists(dataPath);
	}

	//Strips away the path and returns the final <name>.png/jpg
	public static string textureNameFromRemoteURL(string remoteURL)
	{
		int lastToken = remoteURL.LastIndexOf("/");
		if (lastToken > 0 && remoteURL.Length > lastToken + 1)
		{
			// Grab the final token and then process it.
			return remoteURL.Substring(lastToken + 1);
		}
		else
		{
			Debug.LogWarningFormat("DisplayAsset.cs -- textureNameFromRemoteURL -- didnt find any / in the url: {0}, so we couldn't parse out the texture name.", remoteURL);
			return "";
		}		
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		cleanupSessionCache();
		imageTransforms = new Dictionary<Transform, bool>();
		pendingDownloads = new List<string>();
		currentDownloads = new Dictionary<string, UnityWebRequest>();
	}
}

public delegate void TextureDelegate(Texture2D tex, Dict data);
