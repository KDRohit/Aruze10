using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestingAction : ServerAction
{
	private const string LIVE_DATA_OVERRIDE = "set_live_data_override";
	private const string ASSIGN_EOS_WHITELIST = "assign_eos_whitelist";

	// For EOS whitelisting
	private string experiment = "";
	private string variant = "";
	// For Live Data override setting.
	private string key = "";
	private object value = "";

	//Property Names
	private const string KEY = "key";
	private const string VALUE = "value";
	private const string EXPERIMENT = "experiment";
	private const string VARIANT = "variant";


	private TestingAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	public static void whitelistIntoEosVariant(string targetExperiment, string targetVariant)
	{
		TestingAction action = new TestingAction(ActionPriority.HIGH, ASSIGN_EOS_WHITELIST);
		action.experiment = targetExperiment;
		action.variant = targetVariant;
		ServerAction.processPendingActions(true);
	}

	public static void overrideLiveData(string targetKey, object targetValue)
	{
		TestingAction action = new TestingAction(ActionPriority.HIGH, LIVE_DATA_OVERRIDE);
		action.key = targetKey;
		action.value = targetValue;
		ServerAction.processPendingActions(true);
	}

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(LIVE_DATA_OVERRIDE, new string[] {KEY, VALUE});
				_propertiesLookup.Add(ASSIGN_EOS_WHITELIST, new string[] {EXPERIMENT, VARIANT});
			}
			return _propertiesLookup;
		}
	}

	private static Dictionary<string, string[]> _propertiesLookup = null;

	/// Appends all the specific action properties to json
	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		if (!propertiesLookup.ContainsKey(type))
		{
			Debug.LogError("No properties defined for action: " + type);
			return;
		}

		foreach (string property in propertiesLookup[type])
		{
			switch (property)
			{

				case KEY:
					appendPropertyJSON(builder, property, key);
					break;
				case VALUE:
					appendPropertyJSON(builder, property, value);
					break;
				case EXPERIMENT:
					appendPropertyJSON(builder, property, experiment);
					break;
				case VARIANT:
					appendPropertyJSON(builder, property, variant);
					break;
				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		//ServerAction.resetStaticClassData(); NEVER CALL THE BASE CLASS'S RESET!
		_propertiesLookup = null;
	}
}
