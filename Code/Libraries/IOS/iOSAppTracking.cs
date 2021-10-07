using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CustomLog;
using Zynga.Core.Tasks;
using System.Runtime.InteropServices;
using Com.HitItRich.IDFA;
using Zynga.Metrics.UserAcquisition;
using Zynga.Zdk.Services.Track;
using Zynga.Core.Util;

public class iOSAppTracking : MonoBehaviour
{
	// =============================
	// PUBLIC
	// =============================
	public delegate void OnTrackingRequestDelegate(bool accepted);

	// =============================
	// PRIVATE
	// =============================
	private static event OnTrackingRequestDelegate AppTrackingCallback;

#if (UNITY_IPHONE || UNITY_IOS) && !UNITY_EDITOR
	[DllImport ("__Internal")]
	private static extern float RequestAppTracking();
	
	[DllImport ("__Internal")]
	private static extern int trackingAuthorizationStatus();
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
	[DllImport ("RequestAppTrack")]
	private static extern float RequestAppTracking();
	
	[DllImport ("RequestAppTrack")]
	private static extern int trackingAuthorizationStatus();
#endif

	// Constants for stats
	private const string STATS_KINGDOM = "idfa_system_prompt";
	
	// https://developer.apple.com/documentation/apptrackingtransparency/attrackingmanager/authorizationstatus
	// Aligned with enum defined by apple
	public enum AuthorizationStatus
	{
		NOT_DETERMINED = 0,
		RESTRICTED = 1,
		DENIED = 2,
		AUTHORIZED = 3
	}

	private AuthorizationStatus status = AuthorizationStatus.NOT_DETERMINED;
	private string eventStatus = ZTrackIosAppTrackingTransparencyEvent.AuthorizationStatusNotDetermined;
	private static iOSAppTracking instance;

	// EOS variables we get back from the two experiments needed for ios conversion values, and "polling" updates for conversion values

	private int cvPollTime = 0;
	private GameTimerRange timer;
	private Task<SKAdConversionValueAdapterBase.ConversionConfiguration> configTask = null;
	private static bool requestingAppAccess = false;

	private static IDFASoftPromptManager.SurfacePoint trackRequestSurfacePoint;
	private static System.Action onRequestFinishCallback;
	
	void Awake()
	{
		instance = this;
		DontDestroyOnLoad(gameObject);
	}

	public static void getConversionValues()
	{
		if (HIRSKAdConversionAdapter.instance != null)
		{
			HIRSKAdConversionAdapter.instance.GetConversionValueAsync();
		}
	}
	
	public static void getAdConfiguration()
	{
		if (instance == null || instance.configTask != null)
		{
			return;
		}
		
		if (HIRSKAdConversionAdapter.instance != null)
		{
			instance.configTask = HIRSKAdConversionAdapter.instance.GetConfigurationAsync();
			if (instance.configTask != null)
			{
				instance.configTask.ContinueWith(instance.onConfigValues);	
			}
		}
		else
		{
			Debug.LogError("Can't get Ad config without SKAdConversionAdapter");
		}
	}
	
	
	public static AuthorizationStatus GetTrackingPreference
	{
		get
		{
			AuthorizationStatus consentValue = AuthorizationStatus.NOT_DETERMINED;
#if (UNITY_IPHONE || UNITY_IOS) && !UNITY_EDITOR
			consentValue = (AuthorizationStatus)trackingAuthorizationStatus();
#endif
			return consentValue;
		}
	}
	
	private void onTimerExpired(Dict args = null, GameTimerRange originalTimer = null)
	{	
		//call get conversion values
		getConversionValues();
		
		//re-run timer
		timer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + cvPollTime, true, null);
		timer.registerFunction(onTimerExpired);
	}

	private void onConfigValues(Task<SKAdConversionValueAdapterBase.ConversionConfiguration> task)
	{
		//reset config task
		if (task != null && task.Result != null && !(task.IsCanceled || task.IsFaulted))
		{
			cvPollTime = System.Convert.ToInt32(task.Result.CvPollIntervalSec.TotalSeconds);	
		}
		
		//if we're forcing polling in client (instead of zdk) run a timer
		if (ExperimentWrapper.iOSPrompt.pollInClient && cvPollTime > 0)
		{
			if (timer != null)
			{
				timer.clearEvent();
			}
			timer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + cvPollTime, true, null);
			timer.registerFunction(onTimerExpired);
		}
		
		configTask = null;
	}

	/// <summary>
	/// This function is called by the request app tracking plugin
	/// </summary>
	/// <param name="response"></param>
	public void ATTTrackingManagerCallback(string response)
	{
		Debug.Log("ATTTracking Response: " + response);

		status = ParseResponse(response);

		string state = ZTrackIosAppTrackingTransparencyEvent.SurfaceInApp;
		if (!requestingAppAccess)
		{
			state = ZTrackIosAppTrackingTransparencyEvent.SurfaceOutApp;
		}

		requestingAppAccess = false;
		
		// Log stat
		var statsPhylum = (trackRequestSurfacePoint == IDFASoftPromptManager.SurfacePoint.W2E) ? "pre_w2e" : "app_entry";
		var statsFamily = (status == AuthorizationStatus.DENIED) ? "no" : "yes";
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: STATS_KINGDOM,
			phylum: statsPhylum,
			family: statsFamily,
			genus: "click");
		
		// Splunk log
		Dictionary<string, string> extraFields = new Dictionary<string, string>();
		extraFields.Add("response", response);
		extraFields.Add("status", status.ToString());
		extraFields.Add("state", state);
		SplunkEventManager.createSplunkEvent("iOSAppTracking", "ATTTrackingManagerCallback", extraFields);

		if (HIRSKAdConversionAdapter.instance != null)
		{
			HIRSKAdConversionAdapter.instance.logIosAppTrackingTransparencyConsent(eventStatus, state);	
		}

		// Invoke custom callback function
		if (onRequestFinishCallback != null)
		{
			onRequestFinishCallback();
			onRequestFinishCallback = null;
		}
	}

	private AuthorizationStatus ParseResponse(string response)
	{
		int value = int.Parse(response);

		switch (value)
		{
			case 1:
				eventStatus = ZTrackIosAppTrackingTransparencyEvent.AuthorizationStatusRestricted;
				return AuthorizationStatus.RESTRICTED;

			case 2:
				eventStatus = ZTrackIosAppTrackingTransparencyEvent.AuthorizationStatusDenied;
				return AuthorizationStatus.DENIED;

			case 3:
				eventStatus = ZTrackIosAppTrackingTransparencyEvent.AuthorizationStatusAuthorized;
				return AuthorizationStatus.AUTHORIZED;
		}

		return AuthorizationStatus.NOT_DETERMINED;
	}

	public static void RequestTracking(IDFASoftPromptManager.SurfacePoint surfacePoint, System.Action onRequestFinish)
	{
		trackRequestSurfacePoint = surfacePoint;
		onRequestFinishCallback = onRequestFinish;
		
		requestingAppAccess = true;
		Debug.Log("Making request to allow ad tracking (IDFA)");
	
		// Log stat
		var statsPhylum = (surfacePoint == IDFASoftPromptManager.SurfacePoint.W2E) ? "pre_w2e" : "app_entry";
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: STATS_KINGDOM,
			phylum: statsPhylum,
			genus: "view");
	
		// Splunk log
		SplunkEventManager.createSplunkEvent("iOSAppTracking", "RequestTracking", null);
	
#if (UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_IOS) && !UNITY_EDITOR
		RequestAppTracking();
#else
		// Invoke custom callback function
		if (onRequestFinishCallback != null)
		{
			onRequestFinishCallback();
			onRequestFinishCallback = null;
		}
#endif
	}

	/// <summary>
	/// Register a handler for the app tracking response
	/// </summary>
	/// <param name="callback"></param>
	public static void AddEventCallback(OnTrackingRequestDelegate callback)
	{
		AppTrackingCallback -= callback;
		AppTrackingCallback += callback;
	}

	/// <summary>
	/// Unegister a handler for the app tracking response
	/// </summary>
	/// <param name="callback"></param>
	public static void RemoveEventCallback(OnTrackingRequestDelegate callback)
	{
		AppTrackingCallback -= callback;
	}

	/// <summary>
	/// Pops all the events
	/// </summary>
	private static void DispatchEvents(bool accepted)
	{
		if (AppTrackingCallback != null)
		{
			AppTrackingCallback(accepted);
		}
	}
}