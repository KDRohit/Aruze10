using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* 
 * Server action class for handling all the network friends actions.
 */

public class NetworkFriendsAction : ServerAction
{

	/****** Action Names *****/
	public const string FIND_SUGGESTIONS = "get_recommended_friends";
	public const string FIND_FRIEND = "friend_code_finder";
	public const string REMOVE_FRIEND = "remove_graph_friend";
	public const string INVITE_FRIEND = "send_graph_invite";
	public const string DECLINE_INVITE = "decline_graph_invite"; 
	public const string ACCEPT_INVITE = "accept_graph_invite";
	public const string CANCEL_INVITE = "cancel_graph_invite"; 
	public const string BLOCK_PLAYER = "block_graph_player"; 
	public const string UNBLOCK_PLAYER = "unblock_graph_player"; 
	/****** End Action Names *****/


	/****** Action Variables *****/
	private string friendCode = ""; // The friend code for who we want to add.
	private string zid = "";
	/***** End Action Variables *****/

	/***** Property Names *****/
	private const string FRIEND_CODE = "friend_code";
	private const string ZID = "zid";
	/***** End Property Names *****/

	/** Constructor */
	private NetworkFriendsAction(ActionPriority priority, string type) : base(priority, type) {}

	
	/****** Static Methods *****/
	public static void inviteFriend(string zid)
	{
		NetworkFriendsAction action = new NetworkFriendsAction(ActionPriority.IMMEDIATE, INVITE_FRIEND);
		action.zid = zid;
		ServerAction.processPendingActions(true);
		AnalyticsManager.Instance.LogPlayerGraphAction(INVITE_FRIEND);
	}

	public static void acceptFriend(string zid)
	{
		NetworkFriendsAction action = new NetworkFriendsAction(ActionPriority.IMMEDIATE, ACCEPT_INVITE);
		action.zid = zid;
		ServerAction.processPendingActions(true);
		AnalyticsManager.Instance.LogPlayerGraphAction(ACCEPT_INVITE);
	}

	public static void rejectFriend(string zid, EventDelegate callback = null)
	{
		NetworkFriendsAction action = new NetworkFriendsAction(ActionPriority.IMMEDIATE, DECLINE_INVITE);
		action.zid = zid;
		ServerAction.processPendingActions(true);
	}

	public static void findSuggestions(EventDelegateWithParam callback, System.Object param)
	{
		NetworkFriendsAction action = new NetworkFriendsAction(ActionPriority.IMMEDIATE, FIND_SUGGESTIONS);
		action.zid = SlotsPlayer.instance.socialMember.zId;
		if (callback != null)
		{
			Server.registerEventDelegate("player_friends_recommended", callback, param);
		}
		ServerAction.processPendingActions(true);
	}

	public static void findFriendFromCode(string friendCode, EventDelegate callback = null)
	{
		NetworkFriendsAction action = new NetworkFriendsAction(ActionPriority.IMMEDIATE, FIND_FRIEND);
		action.friendCode = friendCode;
		if (callback != null)
		{
			Server.registerEventDelegate("search_by_friend_code", callback);
		}
		ServerAction.processPendingActions(true);
	}

	public static void removeFriend(string zid)
	{
		NetworkFriendsAction action = new NetworkFriendsAction(ActionPriority.IMMEDIATE, REMOVE_FRIEND);
		action.zid = zid;
		ServerAction.processPendingActions(true);
		AnalyticsManager.Instance.LogPlayerGraphAction(REMOVE_FRIEND);
	}

	public static void blockFriend(string zid)
	{
		NetworkFriendsAction action = new NetworkFriendsAction(ActionPriority.IMMEDIATE, BLOCK_PLAYER);
		action.zid = zid;
		ServerAction.processPendingActions(true);
	}

	public static void unblockFriend(string zid)
	{
		NetworkFriendsAction action = new NetworkFriendsAction(ActionPriority.IMMEDIATE, UNBLOCK_PLAYER);
		action.zid = zid;
		ServerAction.processPendingActions(true);
	}
	
	public static void cancelPendingRequest(string zid)
	{
		NetworkFriendsAction action = new NetworkFriendsAction(ActionPriority.IMMEDIATE, CANCEL_INVITE);
		action.zid = zid;
		ServerAction.processPendingActions(true);
	}	

	/****** End Static Methods *****/


	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(INVITE_FRIEND, new string[] {ZID});
				_propertiesLookup.Add(ACCEPT_INVITE, new string[] {ZID});
				_propertiesLookup.Add(BLOCK_PLAYER, new string[] {ZID});
				_propertiesLookup.Add(UNBLOCK_PLAYER, new string[] {ZID});
				_propertiesLookup.Add(CANCEL_INVITE, new string[] {ZID});
				_propertiesLookup.Add(DECLINE_INVITE, new string[] {ZID});
				_propertiesLookup.Add(FIND_SUGGESTIONS, new string[] {});
				_propertiesLookup.Add(FIND_FRIEND, new string[] {FRIEND_CODE});
				_propertiesLookup.Add(REMOVE_FRIEND, new string[] {ZID});
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
				case FRIEND_CODE:
					appendPropertyJSON(builder, property, friendCode);
					break;
				case ZID:
					appendPropertyJSON(builder, property, zid);
					break;
				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}
	}	
}
