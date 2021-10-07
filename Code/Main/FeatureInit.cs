using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Initialization;
using Com.LobbyTransitions;
using Com.Rewardables;

public class FeatureInit : IDependencyInitializer
{
	// running list of callbacks to make once this initializes. you can also use the addCallbackOnInit method
	private static List<GenericDelegate> callbacks = new List<GenericDelegate>()
	{
		PowerupBase.preloadAssets,
		InboxInventory.init,
		LobbyTransitioner.init,
		RewardablesManager.init,
		FeatureInitializer.init
	};

	// Singleton
	private static FeatureInit _instance;
	public static FeatureInit Instance
	{
		get
		{
			if (_instance == null) { _instance = new FeatureInit(); }
			return _instance;
		}
	}

	public static void addCallbackOnInit(GenericDelegate callback)
	{
		if (callbacks == null)
		{
			callbacks = new List<GenericDelegate>();
		}

		callbacks.Add(callback);
	}

	// Initializes this AssetBundleManagerInit wrapper
	void IDependencyInitializer.Initialize(InitializationManager initMgr)
	{
		Bugsnag.LeaveBreadcrumb("FeatureInit::Initialize()");

		// We're done initializing
		initMgr.InitializationComplete(this);

		for (int i = 0; i < callbacks.Count; ++i)
		{
			if (callbacks[i] != null)
			{
				callbacks[i]();
			}
		}
	}

	// Returns a list of packages that we are dependent on
	System.Type[] IDependencyInitializer.GetDependencies()
	{
		return new System.Type[] { typeof(AssetBundleManagerInit) };
	}

	// Returns a short description of this dependency
	string IDependencyInitializer.description()
	{
		return "FeatureInit";
	}
}