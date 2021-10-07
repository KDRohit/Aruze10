using UnityEngine;
using System.Collections.Generic;

public class ReactivateFriendAction : ServerAction 
{
	private const string REACTIVATE_FRIEND_SEND = "reactivate_friend_send";
	private const string REACTIVATE_FRIEND_ACCEPTED = "reactivate_friend_accepted";
	private const string REACTIVATE_FRIEND_FINISH = "reactivate_friend_finish";
	private string eventID = string.Empty;
	private string playerID = string.Empty;
	private const string EVENT_ID = "event";
	private const string PLAYER_ID = "player_id";

	private ReactivateFriendAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	// Response to server that client has confirmed to invite friend.
	public static void confirmSend(string eventID, string playerID)
	{
		ReactivateFriendAction action = new ReactivateFriendAction(ActionPriority.HIGH, REACTIVATE_FRIEND_SEND);
		action.eventID = eventID;
		action.playerID = playerID;
		ServerAction.processPendingActions(true);
	}

	// Response to server that client has accepted the invite.
	public static void confirmAccept(string eventID)
	{
		ReactivateFriendAction action = new ReactivateFriendAction(ActionPriority.HIGH, REACTIVATE_FRIEND_ACCEPTED);
		action.eventID = eventID;
		ServerAction.processPendingActions(true);
	}

	public static void confirmFinish(string eventID)
	{
		ReactivateFriendAction action = new ReactivateFriendAction(ActionPriority.HIGH, REACTIVATE_FRIEND_FINISH);
		action.eventID = eventID;
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
				_propertiesLookup.Add(REACTIVATE_FRIEND_SEND, new string[] {PLAYER_ID});
				_propertiesLookup.Add(REACTIVATE_FRIEND_ACCEPTED, new string[] {EVENT_ID});
				_propertiesLookup.Add(REACTIVATE_FRIEND_FINISH, new string[] {EVENT_ID});
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
					appendPropertyJSON(builder, property, eventID);
					break;
				case PLAYER_ID:
					appendPropertyJSON(builder, property, playerID);
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
	}
}
