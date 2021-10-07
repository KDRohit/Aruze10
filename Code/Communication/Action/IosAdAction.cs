using System;
using System.Collections.Generic;


public class IosAdAction : ServerAction
{
	private string version = "";
	private const string IOS_CONFIGURATION_ACTION = "get_ios_configuration";
	private const string IOS_CONVERSION_VALUE_ACTION = "get_ios_conversion";
	private const string VERSION_KEY = "version";

	private IosAdAction(ActionPriority priority, string type) : base(priority, type) { }

	public static void getConfig(Version iOSVersion)
	{
		IosAdAction action = new IosAdAction(ActionPriority.IMMEDIATE, IOS_CONFIGURATION_ACTION);
		action.version = iOSVersion != null ? iOSVersion.Major.ToString("F") : "14";
		processPendingActions(true);
	}

	public static void getConversionValue(Version iOSVersion)
	{
		IosAdAction action = new IosAdAction(ActionPriority.IMMEDIATE, IOS_CONVERSION_VALUE_ACTION);
		action.version = iOSVersion != null ? iOSVersion.Major.ToString("F") : "14";
		processPendingActions(true);
	}

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(IOS_CONFIGURATION_ACTION, new string[] {VERSION_KEY});
				_propertiesLookup.Add(IOS_CONVERSION_VALUE_ACTION, new string[] {VERSION_KEY});
			}
			return _propertiesLookup;
		}
	}

	private static Dictionary<string, string[]> _propertiesLookup = null;

	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		foreach (string property in propertiesLookup[type])
		{
			switch (property)
			{
				case VERSION_KEY:
					appendPropertyJSON(builder, property, version);
					break;
			}
		}
	}
}