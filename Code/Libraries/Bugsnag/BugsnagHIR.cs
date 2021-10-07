using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HIR-specific customization for use of Bugsnag library.  
/// </summary>
public class BugsnagHIR : MonoBehaviour
{
	private static string BugsnagReleaseStageStatic = null;

	void Awake()
	{
		DontDestroyOnLoad(this);
	}

	void Start()
	{
		// BugsnagHIR is attached to a game object (Gameobject "BugSnag") in startup scene, we do it here instead of
		// Awake since there is a race condition that BugsnagUnity.Bugsnag might not get initialized before BugSnagHIR.Awake() is called.
		// we need to do it until all Awake()s run 
		if(BugsnagUnity.Bugsnag.Client == null || BugsnagUnity.Bugsnag.Configuration == null) 
		{
			Debug.LogError("BugsnagHIR.Start(): BugsnagUnity.Bugsnag is not initialized properly");
			return;
		}
		
		// Treat uncaught Unity exceptions as UNHANDLED.
		BugsnagUnity.Bugsnag.Configuration.ReportUncaughtExceptionsAsHandled = false;

		if(BugsnagReleaseStageStatic != null)
		{
			BugsnagUnity.Bugsnag.Configuration.ReleaseStage = BugsnagReleaseStageStatic;
		}
		else
		{
			if (Data.releaseStage == null)
			{
				// Means config hasn't been read yet.
				Data.loadConfig();
			}
			if (Data.releaseStage == "none" || string.IsNullOrEmpty(Data.releaseStage))
			{
				// Wasn't found in config, fall back to querying debugMode from config.
				if (Data.debugMode)
				{
					ReleaseStage = "development";
				}
				else
				{
					ReleaseStage = "prod";
				}
			}
			else
			{
				ReleaseStage = Data.releaseStage;
			}
		}
	}

	void OnEnable ()
	{
		SceneManager.sceneLoaded += SceneLoaded;
	}

	void SceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (GameState.game != null)
		{
			BugsnagUnity.Bugsnag.Configuration.Context = scene.name + " - " + GameState.game.keyName;
		}
		else
		{
			BugsnagUnity.Bugsnag.Configuration.Context = scene.name;
		}
		BugsnagUnity.Bugsnag.LeaveBreadcrumb(string.Format("Scene loaded: Context: {0}", BugsnagUnity.Bugsnag.Configuration.Context));
	}

	public static string ReleaseStage
	{
		set
		{
			if (value == null)
			{
				value = "unknown";
			}
#if UNITY_ANDROID
#if ZYNGA_KINDLE
			value += "-kindle";
#else
			value += "-android";
#endif	// !ZYNGA_KINDLE
#elif UNITY_IPHONE
			value += "-ios";
#elif UNITY_WEBGL
			value += "-webgl";
#else
			value += "-unity"; // Fall back. Shouldn't reach this.
#endif
			Debug.LogFormat(">>> BugsnagHIR ReleaseStage = {0}", value);
			BugsnagReleaseStageStatic = value;
			BugsnagUnity.Bugsnag.Configuration.ReleaseStage = value;
		}
	}

	public static void AddToTab(string tabName, Dictionary<string, object> metadata)
	{
		BugsnagUnity.Bugsnag.BeforeNotify(report => {
			if (report.Metadata.ContainsKey(tabName))
			{
				Dictionary<string, object> oldMetadata = report.Metadata[tabName] as Dictionary<string, object>;
				foreach (KeyValuePair<string, object> item in metadata)
				{
					oldMetadata[item.Key] = item.Value;
				}
			}
			else
			{
				report.Metadata.Add(tabName, metadata);
			}});
	}

	private const string SAMPLE_ERROR_REPORT_LIVEDATA_KEY = "BUGSNAG_ERROR_SAMPLE_RATE";
	private const string SAMPLE_EXCEPTION_REPORT_LIVEDATA_KEY = "BUGSNAG_EXCEPTION_SAMPLE_RATE";
	private static float errorSampleRate = 0.0f;
	private static float exceptionSampleRate = 0.0f;
	private static bool samplingRatesSet = false;
	private static System.Random sampleRandom = new System.Random();

	public static void SetupSampling()
	{
		if (Data.liveData == null)
		{
			return;
		}
		if (samplingRatesSet == false)
		{
			errorSampleRate = Data.liveData.getFloat(SAMPLE_ERROR_REPORT_LIVEDATA_KEY, 0.0f);
			exceptionSampleRate = Data.liveData.getFloat(SAMPLE_EXCEPTION_REPORT_LIVEDATA_KEY, 0.0f);
			samplingRatesSet = true;
		}

		BugsnagUnity.Bugsnag.BeforeNotify(report => {

				bool isException = false;
				foreach (var exception in report.Exceptions)
				{
					if (exception.ErrorClass == "Exception")
					{
						isException = true;
						break;
					}
				}

				float sampleRate = isException ? exceptionSampleRate : errorSampleRate;
				if (sampleRate > 0.0f)
				{
					bool doSample = (sampleRandom.NextDouble() < sampleRate);
					if (doSample)
					{
						report.Ignore();
					}
				}
			});
	}

}
