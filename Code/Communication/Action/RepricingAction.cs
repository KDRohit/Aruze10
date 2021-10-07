using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Sets up watch to earn related actions.
*/

public class RepricingAction : ServerAction
{
	// ServerAction names
	public const string REPRICING_SEEN = "economy_multiplier_seen";

	// Properties
	private string eventId; 
	
	// Property names
	private const string EVENT_ID = "event";
	
	private RepricingAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	// Tell the server that we have finished watching the supersonic video and are ready to grant the credits on our end.
	public static void markRepricingFtueSeen(string eventId)
	{
		RepricingAction action = new RepricingAction(ActionPriority.HIGH, REPRICING_SEEN);
		action.eventId = eventId;
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
				_propertiesLookup.Add(REPRICING_SEEN, new string[]{EVENT_ID});
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
				case EVENT_ID:
					appendPropertyJSON(builder, property, eventId);
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
		_propertiesLookup = null;
	}
}

