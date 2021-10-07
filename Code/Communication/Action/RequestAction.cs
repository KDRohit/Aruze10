using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Sets up request-related actions.
*/

public class RequestAction : ServerAction
{
	public const string ACCEPT_REQUEST = "accept_request";
	public const string ACCEPT_CREDITS = "accept_credits";
	public const string DECLINE_EVENT = "decline_event";

	private string eventId = "";
	private int activateCharm = 0;

	//property names
	private const string EVENT_ID = "event";
	private const string ACTIVATE_CHARM = "activate_charm";

	private static bool TESTING_INBOX = false;	///< This is not a "const" to prevent warnings from compiler.

	public static void acceptRequest(string eventId)
	{
		if (TESTING_INBOX)
		{
			Debug.Log("called acceptRequest(" + eventId + ")");
		}
		else
		{
			RequestAction action = new RequestAction(ActionPriority.IMMEDIATE, ACCEPT_REQUEST);
			action.eventId = eventId;
		}
	}

	public static void acceptCredits(string eventId)
	{
		if (TESTING_INBOX)
		{
			Debug.Log("called acceptCredits(" + eventId + ")");
		}
		else
		{
			RequestAction action = new RequestAction(ActionPriority.IMMEDIATE, ACCEPT_CREDITS);
			action.eventId = eventId;
		}
	}

	public static void acceptCharm(string eventId)
	{
		if (TESTING_INBOX)
		{
			Debug.Log("called acceptCharm(" + eventId + ")");
		}
		else
		{
			RequestAction action = new RequestAction(ActionPriority.IMMEDIATE, ACCEPT_CREDITS);
			action.eventId = eventId;
			action.activateCharm = 1;
		}
	}

	public static void declineEvent(string eventId)
	{
		if (string.IsNullOrEmpty(eventId))
		{
			Debug.LogWarning("Declining empty event, action ignored.");
			return;
		}

		if (TESTING_INBOX)
		{
			Debug.Log("called ignoreRequest(" + eventId + ")");
		}
		else
		{
			RequestAction action = new RequestAction(ActionPriority.IMMEDIATE, DECLINE_EVENT);
			action.eventId = eventId;
		}
	}

	private RequestAction(ActionPriority priority, string type) : base(priority, type)
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
				_propertiesLookup.Add(ACCEPT_REQUEST, new string[] { EVENT_ID });
				_propertiesLookup.Add(ACCEPT_CREDITS, new string[] { EVENT_ID, ACTIVATE_CHARM });
				_propertiesLookup.Add(DECLINE_EVENT, new string[] { EVENT_ID });
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
				case ACTIVATE_CHARM:
					appendPropertyJSON(builder, property, activateCharm);
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
		TESTING_INBOX = false;
	}
}

