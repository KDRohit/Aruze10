using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FeatureBase
{
	// Delegate type declarations
	public delegate void FeatureEventDelegate();
	public delegate void FeatureBundleGroupEventDelegate(int numFailedBundles);
	public delegate void FeatureBundleEventDelegate(string bundleName, bool didSucceed);

	// Feature Toggling Events
	public event FeatureEventDelegate onEnabledEvent;
	public event FeatureEventDelegate onDisabledEvent;
	public event FeatureEventDelegate onInitEvent;

	// Bundle events
	public event FeatureBundleGroupEventDelegate onAllBundlesDownloadedEvent;
	public event FeatureBundleEventDelegate onBundleDownloadedEvent;

	// Automation Events
	public event FeatureEventDelegate onAutomateStartedEvent;
	public event FeatureEventDelegate onAutomateFinishedEvent;
	
	// Override for features that want the base class to manage downloading bundles.
	protected virtual List<string> bundleNames { get { return new List<string>(); } }
	protected virtual List<string> shouldKeepLoaded { get { return new List<string>(); } }
	protected virtual List<string> shouldLazyLoad { get { return new List<string>(); } }
   
	// Use for force disabling a feature.
	protected bool _isEnabled = true;

	public FeatureBase() {}

	~FeatureBase()
	{
		clearEventDelegates();
	}

	public virtual bool isEnabled
	{
		// Should be overridden by subclasses.
		get
		{
			return _isEnabled;
		}
	}
	
	public virtual void initFeature(JSON data = null)
	{
		/*
			This should generally not get overridden unless you are making a 
			new type of feature. (e.g. EventFeatureBase)
		*/

		initializeWithData(data);
		registerEventDelegates();
		OnInit();

		// After initialization call Enabled/Disable
		if (isEnabled)
		{
			OnEnabled();
		}
		else
		{
			OnDisabled();
		}
	}

	protected virtual void initializeWithData(JSON data)
	{
		// Should be overridden to parse the JSON data for the feature here.
		// While only the relevant data block should be passed in here
		// LiveData setup should still happen here through accessing Data.liveData
	}

	protected virtual void OnEnabled()
	{
		if (onEnabledEvent != null)
		{
			onEnabledEvent();
		}
	}

	protected virtual void OnDisabled()
	{
		if (onDisabledEvent != null)
		{
			onDisabledEvent();
		}
	}

	protected void OnInit()
	{
		if (onInitEvent != null)
		{
			onInitEvent();
		}
	}

	public void OnAutomateStarted()
	{
		if (onAutomateStartedEvent != null)
		{
			onAutomateStartedEvent();
		}
	}

	public void OnAutomateFinished()
	{
		if (onAutomateFinishedEvent != null)
		{
			onAutomateFinishedEvent();
		}
	}

	public void startAutomation()
	{
		RoutineRunner.instance.StartCoroutine(automationWrapperRoutine());
	}

	public virtual void disableFeature()
	{
		_isEnabled = false;
	}

	public virtual void reenableFeature()
	{
		_isEnabled = true;
	}

	public virtual void drawGuts() {}

	protected virtual void registerEventDelegates() {}

	protected virtual void clearEventDelegates() {}

	protected IEnumerator automationWrapperRoutine()
	{
		OnAutomateStarted();
		yield return RoutineRunner.instance.StartCoroutine(runAutomation());
		OnAutomateFinished();
	}

	protected virtual IEnumerator runAutomation()
	{
		// This is where we actually do the automation.
		yield return null;
	}

	private int numBundlesDownloading = 0;
	private int numFailures = 0;
	
	private void downloadAllBundles()
	{
		if (numBundlesDownloading != 0)
		{
			Debug.LogErrorFormat("FeatureBase.cs -- downloadBundles -- still downloading bundles, you cannot call this again.");
			return;
		}
		if (bundleNames != null)
		{
			string bundleName = "";
			for (int i = 0; i < bundleNames.Count; i++)
			{
				bundleName = bundleNames[i];
				if (!string.IsNullOrEmpty(bundleName))
				{
					downloadFeatureBundle(bundleName);
				}

			}
		}
	}

	protected void downloadFeatureBundle(string bundleName)
	{
		AssetBundleManager.downloadAndCacheBundleWithCallback(
			bundleName:bundleName,
			keepLoaded:shouldKeepLoaded.Contains(bundleName),
			lazyLoaded:shouldLazyLoad.Contains(bundleName),
			successCallback:bundleDownloadedCallback,
			failCallback:bundleFailedCallback,
			data: Dict.create(D.BUNDLE_NAME, bundleName));
		numBundlesDownloading++;		
	}

	private void bundleDownloadedCallback(string assetPath, Object obj, Dict data = null)
	{
		if (data != null)
		{
			string bundleName = data.getWithDefault(D.BUNDLE_NAME, "") as string;
			onBundleDownloadedEvent(bundleName, true);
		}

		numBundlesDownloading--;
		if (numBundlesDownloading == 0)
		{
			if (onAllBundlesDownloadedEvent != null)
			{
				onAllBundlesDownloadedEvent(numFailures);
			}
		}
	}

	private void bundleFailedCallback(string assetPath, Dict data = null)
	{
		if (data != null)
		{
			string bundleName = data.getWithDefault(D.BUNDLE_NAME, "") as string;
			onBundleDownloadedEvent(bundleName, false);
		}
		numBundlesDownloading--;
		numFailures++;
		if (numBundlesDownloading == 0)
		{
			if (onAllBundlesDownloadedEvent != null)
			{
				onAllBundlesDownloadedEvent(numFailures);
			}
		}
	}

	public bool areBundlesReady()
	{
		for (int i = 0; i < bundleNames.Count; i++)
		{
			if (!AssetBundleManager.isBundleCached(bundleNames[i]))
			{
				return false;
			}
		}

		return true;
	}
}
