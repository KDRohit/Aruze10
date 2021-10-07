using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
ServerAction class for handling app-related actions.
*/
public class AppAction : ServerAction
{
	private const string APP_INSTALLED = "app_installed";

	private string appId = "";

	//property names
	private const string APP_ID = "app_id";

	private AppAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	// Tells the server that a given app is installed on the current device.
	public static void appInstalled(string appId)
	{
		AppAction action = new AppAction(ActionPriority.HIGH, APP_INSTALLED);
		action.appId = appId;
		// Send it immediately since we will be eagerly waiting for the event to let the player choose a game.
		ServerAction.processPendingActions(true);
	}
	
	////////////////////////////////////////////////////////////////////////

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(APP_INSTALLED, new string[] {APP_ID});
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
				case APP_ID:
					appendPropertyJSON(builder, property, appId);
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
