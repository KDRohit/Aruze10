using System.Collections.Generic;
using Zynga.Core.Util;
using Zynga.Core.UnityUtil;
using UnityEngine;
#if UNITY_IOS
using UnityEngine.iOS;
#endif
using System;
using Com.Scheduler;

/// <summary>
/// This class exposes the method used to start all managers and related components needed to load the game
/// </summary>
public class CasinoStartup : TICoroutineMonoBehaviour
{
	/// <summary>
	/// Records the time needed to load the app  //Further comment: Seems kind of useless.  Commenting out for now.
	/// </summary>
	//private double initTime = 0.0f;
	public bool updateDependencyList = false; // inspector variable that we check to force an update of the dependency list

	public List<string> dependencyOrder; // list of dependency descriptions in the order in which they were initialized (when initialization starts)

	public GameObject persistentObjects = null; ///< This object persists throughout the life of the game, until resetting and returning to Startup Logic scene.
	private PreferencesBase preferences = null;

	private void Awake()
	{
		Bugsnag.LeaveBreadcrumb("CasinoStartup - Awake() beginning");
		
		preferences = SlotsPlayer.getPreferences();
		
		// Mark when the user installs the app for the first time.
		string installDate = preferences.GetString(Prefs.FIRST_APP_START_TIME, null);
		if (string.IsNullOrEmpty(installDate))
		{
			string installTime = System.DateTime.Now.ToFileTime().ToString();
			NotificationManager.DayZero = true;
			preferences.SetString(Prefs.FIRST_APP_START_TIME, installTime);
			preferences.Save();
		}
		Bugsnag.LeaveBreadcrumb(string.Format("Installed on: {0}", Glb.installDateTime.ToString()));
		
		PersistentObject.register(persistentObjects);
		Bugsnag.LeaveBreadcrumb("CasinoStartup - Awake() registered persistent objects");
		
		GameLoader.init();
		
		Bugsnag.LeaveBreadcrumb("CasinoStartup - Awake() finished");
	}

	/// <summary>
	/// This method instantiates all the required managers and adds them as dependencies to the
	/// InitializationManager so their initialization order can be controlled/ensured.
	/// </summary>
	private void Start()
	{
		Bugsnag.LeaveBreadcrumb("CasinoStartup - Start() beginning");
		
		// Start the initialization
		InitializationManager initMgr = InitializationManager.Instance;

		Userflows.logWebGlLoadingStep("casino_startup");

		try
		{
			// Add all the items that need to be initialized. Since SVInitializationManager ensures
			// modules are started in the proper order, please add components in alphabetical order here
			// in order to make the code more manageable.
			initMgr.AddDependency(AnalyticsManager.Instance);
			initMgr.AddDependency(AssetBundleManagerInit.Instance);
			initMgr.AddDependency(FeatureInit.Instance);
			initMgr.AddDependency(AuthManager.Instance);
			initMgr.AddDependency(BasicInfoLoader.Instance);
			initMgr.AddDependency(ClientVersionCheck.Instance);
#if UNITY_ADS && (UNITY_IOS || UNITY_ANDROID)
			initMgr.AddDependency(UnityAdsManager.instance);
#endif
#if UNITY_WSA_10_0 && NETFX_CORE
			initMgr.AddDependency(WindowEconomyManager.Instance);
#else
			initMgr.AddDependency(NewEconomyManager.Instance);
#endif
			initMgr.AddDependency(ExperimentManager.Instance);
			initMgr.AddDependency(GameCenterManager.Instance);
			initMgr.AddDependency(GameLoader.Instance);
			initMgr.AddDependency(NotificationManager.Instance);
			initMgr.AddDependency(SocialManager.Instance);
			initMgr.AddDependency(StatsManager.Instance);
			initMgr.AddDependency(UAWrapper.Instance);
			initMgr.AddDependency(URLStartupManager.Instance);
			initMgr.AddDependency(ZdkManager.Instance);

			// Start the initialization
			// Some deps may initialize asynchronously as part of coroutines completing (ie: AuthManager) so
			// even though StartInitialization() may have returned, not all deps are guaranteed to be initialized.
			Bugsnag.LeaveBreadcrumb("CasinoStartup - Start() all Deps added. Priming InitializationManager");
			initMgr.StartInitialization();
			Bugsnag.LeaveBreadcrumb("CasinoStartup - Start() Done priming Dep initialization");

			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				Debug.LogError("No internet connection DAMMIT");
				GenericDialog.showDialog(
					Dict.create(
						D.TITLE, "INTERNET CONNECTION",
						D.MESSAGE, "PLEASE CHECK YOUR INTERNET CONNECTION!",
						D.CALLBACK,
						new DialogBase.AnswerDelegate((args) => { Glb.resetGame("No internet connection detected."); })
					),
					SchedulerPriority.PriorityType.IMMEDIATE
				);
			}
		}
		catch (System.ApplicationException ex)
		{
			Bugsnag.LeaveBreadcrumb("CasinoStartup - Start() caught an ApplicationException");
			Debug.LogError("Failed to init the app due to: " + ex.ToString());
			Debug.LogException(ex);
		}

		Bugsnag.LeaveBreadcrumb("CasinoStartup - Start() finished");
	}

	//Function to check whether the ios version is valid for ZADE
	private bool isIOSVersionValid()
	{
#if UNITY_ANDROID
		return true;
#elif UNITY_IOS
		string[] versionPrefix = Device.systemVersion.Split('.');

		if (versionPrefix[0] != null)
		{
			int prefix = Convert.ToInt32(versionPrefix[0]);
			if (prefix <= 8)
			{
				return false;
			}
		}
		return true;
#endif
		return false;
	}

	// the only thing we do in update is update the dependency list if we're in the editor and the flag is checked
	private void Update()
	{
		if (Application.isEditor)
		{
			if (updateDependencyList)
			{
				dependencyOrder = InitializationManager.Instance.dependencyOrder; // grab the list from InitializationManager
				updateDependencyList = false; // set it to false so we don't do this every frame
			}
		}
	}
}
