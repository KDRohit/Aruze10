using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Sets up progressive jackpot-related actions.
*/

public class ProgressiveJackpotAction : ServerAction
{
	public const string GET_INFO = "get_progressive_jackpot_info";
	public const string RESET = "reset_progressive_jackpots";

	//property names

	public static void getInfo()
	{
		new ProgressiveJackpotAction(ActionPriority.IMMEDIATE, GET_INFO);
	}

	public static void reset()
	{
		new ProgressiveJackpotAction(ActionPriority.IMMEDIATE, RESET);
	}

	private ProgressiveJackpotAction(ActionPriority priority, string type) : base(priority, type)
	{
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
				_propertiesLookup.Add(GET_INFO, new string[] { });
				_propertiesLookup.Add(RESET, new string[] { });
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
				// case EVENT_ID:
				// 	appendPropertyJSON(builder, property, eventId);
				// 	break;
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

