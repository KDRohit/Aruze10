//
//  AssetBundleManagerInit.cs
//
//  A simple helper class so we can initialize AssetBundleManager via the InitializationManager
//  (needed because the AssetBundleManager instance is a component, rather than a simple class)

using UnityEngine;


public class AssetBundleManagerInit : IDependencyInitializer
{
	public bool hasInitialized = false;

	// Singleton
	private static AssetBundleManagerInit _instance;
	public static AssetBundleManagerInit Instance
	{
		get
		{
			if (_instance == null) { _instance = new AssetBundleManagerInit(); }
			return _instance;
		}
	}

	// Initializes this AssetBundleManagerInit wrapper
	void IDependencyInitializer.Initialize(InitializationManager initMgr)
	{
		Bugsnag.LeaveBreadcrumb("AssetBundleManagerInit::Initialize()");

		// Explicit creation of the AssetBundleManager instance, set the initializedFlag so theres no complaints
		hasInitialized = true;

		if (!AssetBundleManager.hasInstance())
		{
			AssetBundleManager.createInstance();
		}

		// We're done initializing
		initMgr.InitializationComplete(this);
	}

	// Returns a list of packages that we are dependent on
	System.Type[] IDependencyInitializer.GetDependencies()
	{
		// Need BasicInfo so we have access to livedata (for whitelist overrides)
		// Need AuthManager so we have access to user's ZID (for whitelist overrides)
		return new System.Type[] { typeof(ZdkManager) , typeof(AuthManager), typeof(BasicInfoLoader) };
	}

	// Returns a short description of this dependency
	string IDependencyInitializer.description()
	{
		return "AssetBundleManagerInit";
	}
}
