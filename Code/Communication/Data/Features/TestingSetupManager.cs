using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestingSetupManager : FeatureBase
{
	public  delegate void overrideDelegate(string name, string value, bool success, JSON data);
	public static event overrideDelegate onEosChanged;
	public static event overrideDelegate onLiveDataChanged;

	private Dictionary<string, string> pendingEosList = new Dictionary<string, string>();
	private Dictionary<string, object> pendingLiveDataList = new Dictionary<string, object>();
	private bool shouldReset = false;

	public static TestingSetupManager instance
	{
		get
		{
			return FeatureDirector.createOrGetFeature<TestingSetupManager>("testing_setup_manager");
		}
	}

	private string getServerReadyExp(string experiment)
	{
		if (experiment.StartsWith("hir_"))
		{
			return experiment;
		}
		else
		{
			return "hir_" + experiment;
		}

	}

	public void setEosWhitelist(string experiment, string variant, bool shouldReset = false)
	{
		if (!isEnabled) { return; }
		string serverExperiment = getServerReadyExp(experiment);
		if (pendingEosList.ContainsKey(serverExperiment))
		{
			// If we already have a pending change to that experiment, then throw out this request and yell.
			Debug.LogErrorFormat("TestingSetupManager.cs -- setEosWhitelist() -- already have a pending whitelist for experiment: {0} to variant {1}, bailing..", serverExperiment, variant);
		}
		else
		{
			// Otherwise add it to our pending list.
			pendingEosList[serverExperiment] = variant;
			TestingAction.whitelistIntoEosVariant(serverExperiment, variant);
			shouldReset = true;
		}
	}

	public void setLiveDataOverride(string key, object value, bool shouldReset = false)
	{
		if (!isEnabled) { return; }
		if (pendingLiveDataList.ContainsKey(key))
		{
			// If we already have a pending change to that live data key, then throw out this request and yell.
			Debug.LogErrorFormat("TestingSetupManager.cs -- setLiveDataOverride() -- already have a pending override for live data: {0} to key: {1}, bailing..", key, value.ToString());
		}
		else
		{
			// Otherwise add it to our pending list.
			pendingLiveDataList[key] = value;
			TestingAction.overrideLiveData(key, value);
			shouldReset = true;
		}
	}

	private void liveDataOverrideCallback(JSON data)
	{
		if (data != null)
		{
			string key = data.getString("key", "none");
			string value = data.getString("value", "none");
			bool success = data.getBool("success", false);
			string message = data.getString("message", "none");

			if (onLiveDataChanged != null)
			{
				onLiveDataChanged(key, value, success, data);
			}
			if (pendingLiveDataList.ContainsKey(key))
			{
				// Remove from the list.
				pendingLiveDataList.Remove(key);
				if (success)
				{
					restartIfReady();
				}
				else
				{
					Debug.LogErrorFormat("TestingSetupManager.cs -- liveDataOverrideCallback() -- failed override callback for key: {0} to value: {1}", key, value);
				}
			}
			else
			{
				Debug.LogErrorFormat("TestingSetupManager.cs -- liveDataOverrideCallback() -- received an unexpected override for key: {0} to value: {1}", key, value);
			}
		}
		else
		{
			Debug.LogErrorFormat("TestingSetupManager.cs -- liveDataOverrideCallback() -- data was null :(");
		}
	}

	private void eosWhitelistCallback(JSON data)
	{
		if (data != null)
		{
			string experiment = data.getString("experiment", "none");
			string variant = data.getString("variant", "none");
			bool success = data.getBool("success", false);

			if (onEosChanged != null)
			{
				onEosChanged(experiment, variant, success, data);
			}
			if (pendingEosList.ContainsKey(experiment) && success)
			{
				pendingEosList.Remove(experiment);
				if (success)
				{
					restartIfReady();
				}
				else
				{
					Debug.LogErrorFormat("TestingSetupManager.cs -- eosWhitelistCallback() -- Failed to whitelist into variant: {0} of EOS: {1}", variant, experiment);
				}
			}
			else
			{
				Debug.LogErrorFormat("TestingSetupManager.cs -- eosWhitelistCallback() -- recieved an unexpected or failed whitelist callback for experiment {0} into variant: {1}", experiment, variant);
			}
		}
		else
		{
			Debug.LogErrorFormat("TestingSetupManager.cs -- eosWhitelistCallback() -- data was null :(");
		}
	}

	private void restartIfReady()
	{
		if (shouldReset && // Only reset automatically if one of the requests specified it.
			pendingLiveDataList.Count == 0 &&
			pendingEosList.Count == 0)
		{
			Glb.resetGame("Resetting after EOS/LiveData changes.");
		}
	}

#region feature_base_overrides
	protected override void initializeWithData(JSON data)
	{
		if (isEnabled)
		{
			registerEventDelegates();
		}
	}

	public override bool isEnabled
	{
		get
		{
#if ZYNGA_PRODUCTION
			return false;
#else
			return true;
#endif
		}
	}

	protected override void registerEventDelegates()
	{
		Server.registerEventDelegate("assign_eos_whitelist", eosWhitelistCallback, true);
		Server.registerEventDelegate("set_live_data_override", liveDataOverrideCallback, true);
	}

	protected override void clearEventDelegates()
	{
		Server.unregisterEventDelegate("assign_eos_whitelist", eosWhitelistCallback, true);
		Server.unregisterEventDelegate("set_live_data_override", liveDataOverrideCallback, true);
	}

	#endregion

}
