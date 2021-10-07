using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

/// <summary>
/// Class that encapsulates a single asset bundle download request.
/// </summary>
public class AssetBundleDownloader : MonoBehaviour, IResetGame
{
	private const string BUNDLE_FLOW_PREFIX = "bundle-load-";

	public struct DownloadResponse
	{
		public string resourcePath;
		public string fileExtension;
		public AssetLoadDelegate successCallback;
		public AssetFailDelegate failCallback;
		public Dict data;
		public object classInstanceRequestingBundle;
	}

	public string bundleName;
	public string bundleUrl;
	public List<DownloadResponse> downloadResponses = new List<DownloadResponse>();
	public AssetBundleMapping assetBundleMapping;
	
	public bool downloadStarted = false;
	public bool isDone = false;
	public bool failed = false;
	public bool cancelled = false;
	public bool paused = false;
	public bool isDisposed = false;
	public UnityWebRequest uwr;
	AssetBundleCreateRequest assetBundleCreateRequest;
	public AssetBundle bundle;
	public bool keepLoaded = false;
	public bool lazyLoaded = false;
	public ulong bytesDownloaded;
	public bool isSkippingMapping = false;
	public bool blockLoadingScreen = true;

	// Dependencies on/from other bundles
	public List<AssetBundleDownloader> dependsOn = new List<AssetBundleDownloader>();
	public List<AssetBundleDownloader> referencedBy = new List<AssetBundleDownloader>();

	public static Dictionary<string, string> embeddedBundlesMap;

	// For testing only
	public bool testForceDelay = false;

	public static string bundleDownloadUserflowLog = "";

	public float timeStarted;
	public int tryCount;
	[SerializeField]
	private int bundleVersion;

	// For checking for stalled progress 
	[SerializeField]
	private ulong lastProgress;
	[SerializeField]
	private float stallTimer;
	private static float STALL_TIMEOUT = 15.0f;
	private static float MAX_STALL_TIMEOUT = 35.0f;
	private static float STALL_TIMEOUT_INCREMENT = 10.0f;

	public static void increaseStallTimeout()
	{
		if (STALL_TIMEOUT >= MAX_STALL_TIMEOUT)
		{
			return;
		}

		STALL_TIMEOUT = Mathf.Min((STALL_TIMEOUT + STALL_TIMEOUT_INCREMENT), MAX_STALL_TIMEOUT);
	}

	public bool isReadyToStart()
	{
		return !downloadStarted && !isDone && !failed && !cancelled && !paused;
	}

	/// <summary>
	/// The downloader starts out paused, this function kicks off the download.
	/// </summary>
	public void startDownload()
	{
		bundleVersion = AssetBundleManager.getBundleVersion(bundleName);
		if (Data.debugMode)
		{
			if (AssetBundleManager.useLocalBundles)
			{
				Debug.Log(string.Format("AssetBundleDownloader start loading local bundle {0}", bundleName));
			}
			else
			{
				Debug.Log(string.Format("AssetBundleDownloader startDownload {0}:{1} at {2}", bundleName, bundleVersion, bundleUrl));
			}
		}
		timeStarted = Time.realtimeSinceStartup;
		lastProgress = 0;
		stallTimer = -1.0f;
		tryCount += 1;
		isDisposed = false;
		StartCoroutine(downloadAssetBundle(bundleUrl, bundleVersion));

		AssetBundleManager.BundleType bundleType = AssetBundleManager.findBundleType(bundleName);
		
		// Only add bundles to the loading screen if it is okay to cancel them.
		if (blockLoadingScreen && (!AssetBundleManager.isBundleCached(bundleName) || tryCount > 1))
		{
			if (AssetBundleManager.hasLazyBundle(bundleName) || lazyLoaded)
			{
				HelpDialogHIR.addDownload(bundleName);
			}
			else
			{
				Loading.addDownload(bundleName);
			}
		}
	}

	/// <summary>
	/// Cancels download.
	/// </summary>
	/// <remarks>
	/// Ignored if using local asset bundles.
	/// </remarks>
	public void cancelDownload()
	{
		// always cancel without checking isDone , since it may just been marked done on the last frame of the coroutine.
		//  AssetBundleManager.Update may still end up calling the notification callback on next update since it is NOT marked as cancelled causing a crash since all got reset
		// https://app.crittercism.com/developers/crash-details/525d7eb9d0d8f716a9000006/868d58f49aff02130740448d8bdd875455184eb084f9d16e70303e42#tab=breadcrumbs&appVersion=1.7.7234&period=P14D
		// note in breadcrumbs  lobby_v3-hd-4c2454ff.bundlev2 gets loaded, game resets, yet the notification to create the main lobby still gets called resulting in a crash without the bundle load breadcrumb messages that need to happen first
		// being seen again and our old friend "lobby main data is null" breadcrumb shows right before the crash. 
		if (!AssetBundleManager.useLocalBundles /*&& !isDone*/)
		{
			if (Data.debugMode)
			{
				Debug.Log(string.Format("AssetBundleDownloader cancelDownload {0} at {1}", bundleName, bundleUrl));
			}
			cancelled = true;

			safeWWWDispose();

			downloadStarted = false;
			// TODO: cancel all response callbacks.
			foreach (DownloadResponse resp in downloadResponses)
			{
				if (!string.IsNullOrEmpty(resp.resourcePath))
				{
					// TODO: use cancel, not fail
					// if (resp.failCallback != null)
					// {
					// 	resp.failCallback(resp.resourcePath, resp.data);
					// }
				}
			}
			downloadResponses.Clear();
		}
	}

	public void pause(string reason = "None")
	{
		if (uwr != null)
		{
			Debug.LogWarningFormat("Pausing download: {0}", uwr.url);
			downloadStarted = false;
			paused = true;
			safeWWWDispose();
			string flowTransactionName = BUNDLE_FLOW_PREFIX + this.bundleName;
			Userflows.logStep("paused: " + reason, flowTransactionName);
		}
	}

	private bool stallTimerOn
	{
		get { return stallTimer != -1.0f; }
		set
		{
			if (value)
			{
				if (stallTimer == -1.0f)
				{
					stallTimer = Time.realtimeSinceStartup;
				}
			}
			else
			{
				stallTimer = -1.0f;
			}
		}
	}

	public bool isStalled()
	{
#if UNITY_WEBGL
		// WebGL does not get download progress reliably, so stall detection isn't possible.
		return false;
#else
		bool stalled = false;
		if (uwr != null)
		{
			bool noChange = (uwr.downloadedBytes == lastProgress);
			lastProgress = uwr.downloadedBytes;
			if (!stallTimerOn)
			{
				if (noChange)
				{
					stallTimerOn = true;
				}
			}
			else
			{
				if (!noChange)
				{
					stallTimerOn = false;
				}
				else
				{
					if ((Time.realtimeSinceStartup - stallTimer) > STALL_TIMEOUT)
					{
						stalled = true;
					}
				}
			}
		}
		return stalled;
#endif
	}

	public void safeWWWDispose()
	{
		if (!isDisposed)
		{
			isDisposed = true;
			// this is the only place this can be disposed!
			if (uwr != null)
			{
				uwr.Dispose();
				uwr = null;
			}
		}
	}

	public bool isWWWSafeToUse()
	{
		return (isDisposed == false && uwr != null && uwr.isDone);
	}
	
	private IEnumerator safeWWWYield()
	{
		// workaround for unity bug introduced in Unity 2017 which caused yield return www to crash if www.dispose got called elsewhere.
		// using safeWWWYield gives us the same functionality and we can still keep www.dispose and www = null.
		WaitForSeconds shortWait = new WaitForSeconds(0.1f);
		while (!isDisposed && uwr != null && !uwr.isDone)
		{
			yield return shortWait;
		}
	}

	/// <summary>
	/// Download an asset bundle from a url or local asset bundle asynchronously.
	/// </summary>
	private IEnumerator downloadAssetBundle(string url, int version)
	{
		float elapsed = Time.realtimeSinceStartup - timeStarted;
		downloadStarted = true;

		string flowTransactionName = BUNDLE_FLOW_PREFIX + this.bundleName;
		Userflows.flowStart(flowTransactionName);
		
		bool isEmbedded = false;
		string embeddedPath = "";
#if !UNITY_WEBGL

		if (embeddedBundlesMap == null)
		{
			embeddedBundlesMap = new Dictionary<string, string>();
			foreach (string embeddedBundle in AssetBundleManager.embeddedBundlesList)
			{
				string fullName = AssetBundleManager.manifestV2.getFullBundleNameFromBaseBundleName(embeddedBundle);
				embeddedBundlesMap.Add(fullName, embeddedBundle);
			}
		}

		if (embeddedBundlesMap.ContainsKey(bundleName))
		{
			isEmbedded = true;
			embeddedPath = System.IO.Path.Combine(Application.streamingAssetsPath, bundleName);
		}
#endif
		
		Userflows.addExtraFieldToFlow(flowTransactionName, "cdn", getCdnNameFromUrl(url));

		// Wait for the Caching system to be ready.
		while (!Caching.ready)
		{
			yield return null;
		}

		if (!cancelled)
		{
			Userflows.logStep("s-dl", flowTransactionName); // start download
			
			// special case handling/injection of the previously loaded initialization bundle into the system
			if (bundleName.FastStartsWith(AssetBundleManager.INITIALIZATION_BUNDLE_NAME))
			{
				bundle = NGUILoader.instance.initialBundle;
				this.keepLoaded = true;
			}
			
			// First check if it is embedded, and if so try to load it as such.
			if  (isEmbedded)
			{
				if (bundle == null && System.IO.File.Exists(embeddedPath))
				{
					Userflows.logStep("emb", flowTransactionName);
					bundle = AssetBundle.LoadFromFile(embeddedPath);  // This only works in-editor if you built local bundles
				}
			}

			if (bundle == null)
			{
				// Attempt to load bundle from Resources
				var filename =
					System.IO.Path.GetFileName(bundleName); //ie: "shaders_2d84a5a3f91fdccc534ade9fecaddc6f.bundlev2"
				TextAsset resource = Resources.Load(filename) as TextAsset;
				if (resource != null)
				{
					Bugsnag.LeaveBreadcrumb("Using embedded bundle: " + bundleName);

					// load asset bundle from memory
					AssetBundleCreateRequest createRequest = AssetBundle.LoadFromMemoryAsync(resource.bytes);
					this.assetBundleCreateRequest = createRequest;
					yield return createRequest;

					bundle = assetBundleCreateRequest.assetBundle;
					if (bundle != null)
					{
						Debug.Log("Loaded embedded asset bundle: " + filename);
					}
					else
					{
						Debug.LogError("Error loading embedded asset bundle, assetBundle is null: " + filename);
						// This isn't a fatal failure, will fallback to WWW loader next...
					}
				}
			}


			// Not embedded, so use WWW loader. Local & server bundles load the same way (via local or remote URL)
			if (bundle == null)
			{
				Bugsnag.LeaveBreadcrumb("Downloading bundle: " + url);
				
				// use try/finally instead of using so we can work around the www.dispose issue with Unity 2017
				try
				{
#if UNITY_WEBGL && !UNITY_EDITOR
					// WebGL shouldn't use Unity caching because it (essentially) leaks memory across sessions
					// Caching is instead performed explicitly via javascript CachedXMLHttpRequest calls
					// (see https://blogs.unity3d.com/2016/09/20/understanding-memory-in-unity-webgl/)
					using (UnityWebRequest theUwr = UnityWebRequestAssetBundle.GetAssetBundle(url))
#else
					using (UnityWebRequest theUwr = UnityWebRequestAssetBundle.GetAssetBundle(url, (uint) version, 0))
#endif
					{
						this.uwr = theUwr;
						yield return this.uwr.SendWebRequest();
						if (isWWWSafeToUse())
						{
							if (string.IsNullOrEmpty(this.uwr.error))
							{
								try
								{
									bundle = DownloadHandlerAssetBundle.GetContent(this.uwr);
									bytesDownloaded = this.uwr.downloadedBytes; // doesnt match original filesize?
								}
								catch (System.Exception e)
								{
									Debug.LogError("Download asset bundle: " + url + ": unpack error: " + e.Message);
									Debug.LogException(e);
									failed = true;
								}
							}
							else
							{
								Debug.LogError("Download asset bundle: " + url + ": www error: " + this.uwr.error);
								failed = true;
							}
						}
						else
						{
							string failedReason = "";
							if (uwr == null)
							{
								failedReason = "uwr is NULL";
							}
							else if (isDisposed)
							{
								failedReason = "uwr is disposed";
							}
							else if (!uwr.isDone)
							{
								failedReason = "uwr is not done";
							}
							Debug.LogError("Download asset bundle: " + url + ": " + failedReason);
							failed = true;
						}
					}
				}
				finally
				{
					safeWWWDispose();
				}
			}

			Userflows.logStep("e-dl", flowTransactionName); // end download

			int bundleFileSize = extractFileSizeFromBundleFilename(this.bundleName);
			Userflows.addExtraFieldToFlow(flowTransactionName, "size", bundleFileSize.ToString());

			if (!failed)
			{
				// Wait for any bundle dependencies to load before actualizing any assets...
				if (isWaitingOnDependencies())
				{
					Userflows.logStep("s-deps", flowTransactionName); // start waiting on dependencies
					while ( isWaitingOnDependencies() )
					{
						yield return null;
					}
					Userflows.logStep("e-deps", flowTransactionName); // end waiting on dependencies
				}

				if (bundle != null)
				{
					Userflows.logStep("s-map", flowTransactionName); // start bundle mapping
					Bugsnag.LeaveBreadcrumb("Creating bundle mapping for: " + bundleName);

					// V2 bundles stopped embedding AssetBundleMapping as components, now just a regular class
					AssetBundleMapping mapping = new AssetBundleMapping( bundle, isSkippingMapping:isSkippingMapping );

					// Live data controlled list of bundles that can be loaded asynchronously
					if (mapping.isAsync)
					{
						yield return mapping.createAssetBundleMappingAsync(bundle);
					}
					this.assetBundleMapping = mapping;
					failed = mapping.isFailed;

					Userflows.logStep("e-map", flowTransactionName); // end bundle mapping
					Userflows.addExtraFieldToFlow(flowTransactionName, "asyncMapping", mapping.isAsync.ToString());
				}
				else
				{
					Debug.LogError("Download asset bundle: " + url + ": bundle object is null.");
					failed = true;
				}
			}

			isDone = true;
		}

		elapsed = Time.realtimeSinceStartup - timeStarted;
		if (isDone)
		{
			if (Data.debugMode)
			{
				if (AssetBundleManager.useLocalBundles)
				{
					Debug.Log(string.Format("AssetBundleDownloader finished loading local bundle {0}: {2} in {3}s", bundleName, bundleUrl, !failed, elapsed));
				}
				else
				{
					Debug.Log(string.Format("AssetBundleDownloader finished download {0}:{1} at {2}: {3} in {4}s", bundleName, bundleVersion, bundleUrl, !failed, elapsed));
				}
			}
			
#if UNITY_WEBGL
			if (!failed)
			{
				// This is to update the cached status as stored in Javascript for WebGL.
				// See CachedXHRExtensions.jspre for more, including the storage of async results.
				AssetBundleManager.isBundleCached(bundleName);
			}
#endif

			Bugsnag.LeaveBreadcrumb("Finished downloading & mapping bundle: " + bundleName + (failed ? "  (FAILED)" : "  (SUCCESS)"));
		}
		else
		{
			// Must have been cancelled.
			if (Data.debugMode)
			{
				Debug.Log(string.Format("AssetBundleDownloader canceled download {0} at {1}: {2} in {3}s", bundleName, bundleUrl, !failed, elapsed));
			}
		}

		Userflows.addExtraFieldToFlow(flowTransactionName, "lazy", lazyLoaded.ToString());
		Userflows.Userflow userflow = Userflows.flowEnd(flowTransactionName, isDone && !failed);
		if (Data.debugMode)
		{
			string log = flowTransactionName + " Load Time: " + userflow.duration + "\n";
			bundleDownloadUserflowLog += log;
		}
	}

	/// <summary>
	/// Any callers that requested an Object from this bundle will get success or fail callbacks.
	/// </summary>
	public void notifyCallersThatResourceIsReady()
	{
		foreach (DownloadResponse resp in downloadResponses)
		{
			if (string.IsNullOrEmpty(resp.resourcePath))
			{
				continue;
			}

			string fullPath = resp.resourcePath + resp.fileExtension;
			if (this.assetBundleMapping != null && this.assetBundleMapping.hasAsset(resp.resourcePath, resp.fileExtension))
			{
				Object obj = this.assetBundleMapping.getAsset(resp.resourcePath, resp.fileExtension);
				if (obj != null && !failed && !cancelled)
				{
#if UNITY_EDITOR
					if (AssetBundleManager.useLocalBundles)
					{
						Debug.Log(string.Format("AssetBundleDownloader loading resource {1} from local bundle {0}: SUCCESS", bundleName, fullPath));
					}
					else
					{
						Debug.Log(string.Format("AssetBundleDownloader loading resource {2} from downloaded bundle {0} at {1}: SUCCESS", bundleName, bundleUrl, fullPath));
					}
#endif
					if (resp.successCallback != null && resp.classInstanceRequestingBundle != null)
					{
						if (resp.classInstanceRequestingBundle is Object)
						{								
							//Need to cast to UnityEngine.Object if the caller classInstanceRequestingBundle is one
							//because Unity overrides the == operator to handle UnityEngine.Objects being destroyed
							//and treating them as null Objects
							if ((Object)resp.classInstanceRequestingBundle != null)
							{
								resp.successCallback(fullPath, obj, resp.data);
							}
						}
						else
						{
							resp.successCallback(fullPath, obj, resp.data);
						}
					}
				}
				else
				{
					if (Data.debugMode)
					{
						if (AssetBundleManager.useLocalBundles)
						{
							Debug.LogError(string.Format("AssetBundleDownloader loading resource {1} from local bundle {0}: FAILED", bundleName, fullPath));
						}
						else
						{
							Debug.LogError(string.Format("AssetBundleDownloader loading resource {2} from downloaded bundle {0} at {1}: FAILED", bundleName, bundleUrl, fullPath));
						}
					}
					if (!string.IsNullOrEmpty(fullPath))
					{
						AssetBundleManager.fallbackToLoadFromResources(fullPath, resp.successCallback, resp.failCallback, resp.data);
					}
				}
			}
			else
			{
				if (Data.debugMode)
				{
					if (AssetBundleManager.useLocalBundles)
					{
						Debug.LogError(string.Format("AssetBundleDownloader loading resource {1} from local bundle {0}: FAILED", bundleName, resp.resourcePath));
					}
					else
					{
						Debug.LogError(string.Format("AssetBundleDownloader loading resource {2} from downloaded bundle {0} at {1}: FAILED", bundleName, bundleUrl, resp.resourcePath + resp.fileExtension));
					}
				}
				if (!string.IsNullOrEmpty(resp.resourcePath))
				{
					AssetBundleManager.fallbackToLoadFromResources(resp.resourcePath, resp.successCallback, resp.failCallback, resp.data);
				}
			}
		}
	}

	// Returns true if this download is waiting on any dependent bundles to download
	private bool isWaitingOnDependencies()
	{
		//var deps = download.dependsOn.Select( dl => (dl != null) ? dl.bundleName : "null" ).ToArray();
		//Debug.Log("download '" + download.bundleName + "'  waiting on: " + string.Join(", ", deps));
		//return this.dependsOn.Any( downloads => downloads != null );

		// Is this download still waiting on any dependencies to download?
		foreach(var downloader in this.dependsOn)
		{
			if (downloader != null)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Subscribe to notification on a resource from this bundle when it is done being downloaded.
	/// </summary>
	public void addNotify(string resourcePath, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict data, object caller = null, string fileExtension = "")
	{
		DownloadResponse resp = new DownloadResponse();
		resp.resourcePath = resourcePath;
		resp.fileExtension = fileExtension;
		resp.successCallback = successCallback;
		resp.failCallback = failCallback;
		resp.data = data;
		resp.classInstanceRequestingBundle = caller == null ? this : caller;
		downloadResponses.Add(resp);
	}

	/// <returns>Value between 0.0 and 1.0 denoting progress of this download.</returns>
	public float loadProgress()
	{
		if (!downloadStarted)
		{
			return 0.0f;
		}

		if (isDone)
		{
			return 1.0f;
		}

		if (uwr != null)
		{
			return uwr.downloadProgress;
		}
		
		if (assetBundleCreateRequest != null)
		{
			return assetBundleCreateRequest.progress;
		}

		return 0.0f;
	}

	public string getResponseHeaders()
	{
		Dictionary<string, string> responseHeaders = uwr.GetResponseHeaders();
		if (responseHeaders == null || responseHeaders.Count == 0)
			return "";
		
		StringBuilder respHeadersBuilder = new StringBuilder();
		foreach (KeyValuePair<string, string> entry in responseHeaders)
		{
			respHeadersBuilder.AppendFormat("{0} = {1}; ",entry.Value, entry.Key);
		}

		return respHeadersBuilder.ToString();
	}

	private string getCdnNameFromUrl(string url)
	{
		string cdnName = "unknown";
		if (url.Contains("akamaihd.net"))
		{
			cdnName = "akamai";
		}
		else if (url.Contains("socialslots.cdn"))
		{
			cdnName = "cloudfront";
		}
		else if (url.Contains("s3.amazonaws.com"))
		{
			cdnName = "s3";
		}

		return cdnName;
	}

	// Returns the file size that we keep in the bundle file name (or 0 if not found)
	// File size is embedded in bundle filenames with a "-sz" prefix, ala:  wonka01-hd-0e1d73ba-sz13434.bundlev2
	private int extractFileSizeFromBundleFilename(string filename)
	{
		int size = 0;
		int index = filename.IndexOf(AssetBundleManager.bundleSizePrefix);
		if (index == -1)
		{
			return size;
		}

		index += AssetBundleManager.bundleSizePrefix.Length;
		
		while (index < filename.Length && char.IsDigit(filename[index]))
		{
			size = size * 10 + (int)char.GetNumericValue(filename[index]);
			index++;
		}
		return size;
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		embeddedBundlesMap?.Clear();
		embeddedBundlesMap = null;
	}

}

