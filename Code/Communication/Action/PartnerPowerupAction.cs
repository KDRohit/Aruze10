using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
ServerAction class for handling app-related actions.
*/
public class PartnerPowerupAction : ServerAction
{
	//action name
	private const string GET_PROGRESS = "get_co_op_progress";
	private const string COMPLETE_PROGRESS = "complete_co_op";
	private const string POKE_PARTNER = "co_op_poke"; // Or is this consume_co_op_nudge?
	private const string EVENT_ID = "event";

	private static string eventID = "";
	private static Dictionary<string, string[]> _propertiesLookup = null;

	private PartnerPowerupAction(ActionPriority priority, string type) : base(priority, type)
	{
		// Do I need this?
	}

	public static void pokeUser()
	{
		PartnerPowerupAction action = new PartnerPowerupAction(ActionPriority.HIGH, POKE_PARTNER);
		ServerAction.processPendingActions(true);
	}


	public static void getProgress()
	{
		PartnerPowerupAction action = new PartnerPowerupAction(ActionPriority.HIGH, GET_PROGRESS);
		ServerAction.processPendingActions(true);
	}

	public static void completeCoOp(string eventId)
	{
		Server.registerEventDelegate("co_op_credits", PartnerPowerupCampaign.onGetCredits);
		eventID = eventId;
		PartnerPowerupAction action = new PartnerPowerupAction(ActionPriority.HIGH, COMPLETE_PROGRESS);
		// Expect a callback eventually
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
				_propertiesLookup.Add(POKE_PARTNER, new string[] {});			
				_propertiesLookup.Add(GET_PROGRESS, new string[] {});
				_propertiesLookup.Add(COMPLETE_PROGRESS, new string[] { EVENT_ID });
			}
			return _propertiesLookup;
		}
	}
		
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
				appendPropertyJSON(builder, property, eventID);
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
		// Nothing do to?
	}
}
