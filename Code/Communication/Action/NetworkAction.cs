using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* 
 * Server action class for handling all the network actions
 */

public class NetworkAction : ServerAction 
{
	#region ACTION NAMES
	
	//Constant to get what state the network is in Ex: connected, pending, anonymous
	public const string GET_NETWORK_STATE = "network_get_state";

	//Constant to connect to the network
	public const string CONNECT_TO_NETWORK = "network_connect";

	//Constant to disconnect from network
	public const string DISCONNECT_NETWORK = "network_disconnect";

	//Constant to resend network authorization
	public const string RESEND_NETWORK_AUTHORIZATION = "network_resend_authorization";

	//Constant to cancel pending request
	public const string CANCEL_PENDING_NETWORK = "network_cancel_pending_authorization";

	//Constant to get vip status
	public const string GET_VIP_STATUS = "get_vip_status";

	//Constant to check if connected
	public const string CONNECTED = "connected";

	//Constant to check if pending
	public const string PENDING = "pending";

	//Constant to check if connected or not
	public const string NETWORK_STATE = "network_state";

	//Constant to get the game level
	public const string GET_GAME_LEVEL = "get_game_level";

	//Constant to set the game level
	public const string SET_GAME_LEVEL = "set_game_level";

	// Constant to tell the server we are accepting the granted credits.
	public const string ACCEPT_INCENTIVE = "network_connect_accept_incentive";
	#endregion
	
	// Email to which the network is connected
	private string email = "";
	// Level to set the vip too
	private string level = "";
	// The event id that comes down from a server action.
	private string eventId = "";
	
	
	private const string EMAIL = "email";
	private const string LEVEL = "level";
	private const string EVENT = "event";

	/** Constructor */
	private NetworkAction(ActionPriority priority, string type) : base(priority, type) {}

	/** Gets the state the user is in */
	public static void getNetworkState()
	{
		new NetworkAction(ActionPriority.IMMEDIATE, GET_NETWORK_STATE);
		ServerAction.processPendingActions(true);
	}

	/** Connects to the network */
	public static void connectNetwork(string email) 
	{
		NetworkAction connectNetwork = new NetworkAction(ActionPriority.IMMEDIATE, CONNECT_TO_NETWORK);
		connectNetwork.email = email;
		ServerAction.processPendingActions(true);
	}

	/** Disconnect from the network */
	public static void disconnectNetwork()
	{
		new NetworkAction(ActionPriority.IMMEDIATE, DISCONNECT_NETWORK);
		ServerAction.processPendingActions(true);
	}

	/** Resend authorization */
	public static void networkResendAuthorization()
	{
		new NetworkAction(ActionPriority.IMMEDIATE, RESEND_NETWORK_AUTHORIZATION);
		ServerAction.processPendingActions(true);
	}

	/** Cancel pending network request */
	public static void cancelPendingNetwork() 
	{
		new NetworkAction(ActionPriority.IMMEDIATE, CANCEL_PENDING_NETWORK);
		ServerAction.processPendingActions(true);
	}

	/** Get vip status request */
	public static void getVipStatus() 
	{
		new NetworkAction(ActionPriority.IMMEDIATE, GET_VIP_STATUS);
		ServerAction.processPendingActions(true);
	}

	/** Get the required VIP levels */
	public static void getVipLevel(EventDelegate callback) 
	{
		new NetworkAction(ActionPriority.IMMEDIATE, GET_GAME_LEVEL);
		Server.registerEventDelegate ("game_level", callback);
		ServerAction.processPendingActions (true);
	}

	/** Set the required VIP levels */
	public static void setVipLevel(int level) 
	{
		NetworkAction setVipStatus = new NetworkAction(ActionPriority.IMMEDIATE, SET_GAME_LEVEL);
		setVipStatus.level = level.ToString();
		ServerAction.processPendingActions (true);
	}

	public static void acceptIncentiveCredits(string eventId)
	{
		NetworkAction acceptCredits = new NetworkAction(ActionPriority.IMMEDIATE, ACCEPT_INCENTIVE);
		acceptCredits.eventId = eventId;
		ServerAction.processPendingActions (true);
	}

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(GET_NETWORK_STATE, new string[] {});
				_propertiesLookup.Add(CONNECT_TO_NETWORK, new string[] {EMAIL});
				_propertiesLookup.Add(DISCONNECT_NETWORK, new string[] {});
				_propertiesLookup.Add(RESEND_NETWORK_AUTHORIZATION, new string[] {});
				_propertiesLookup.Add(CANCEL_PENDING_NETWORK, new string[] {});
				_propertiesLookup.Add(GET_VIP_STATUS, new string[] {});
				_propertiesLookup.Add(GET_GAME_LEVEL, new string[] {});
				_propertiesLookup.Add(SET_GAME_LEVEL, new string[] {LEVEL});
				_propertiesLookup.Add(ACCEPT_INCENTIVE, new string[] {EVENT});
			}
			return _propertiesLookup;
		}
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		_propertiesLookup = null;
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
				case EMAIL:
					appendPropertyJSON(builder, property, email);
					break;
				case LEVEL:
					appendPropertyJSON (builder, property, level);
					break;
				case EVENT:
					appendPropertyJSON (builder, property, eventId);
					break;
			default:
				Debug.LogWarning("Unknown property for action: " + type + ", " + property);
				break;
			}
		}
	}
}
