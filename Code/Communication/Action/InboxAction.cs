using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InboxAction : ServerAction
{
	public string actionKey = ""; 	// action to be performed by the server on the inbox item
	public string eventId = ""; 	// event id associated with the inbox item
	public string zid = ""; 		// this could be either the current users zid, or the zid of the person who sent the inbox item
	public string gameKey;
	public string bonusGameKey;
	public string message;
	public List<string> zids; // if you're doing a bunch of items to users
	public List<string> eventIds; // if you're doing a bunch of items to users

	public const string GET_INBOX = "inbox_details";
	public const string SEND_CREDITS = "inbox_send_credits";
	public const string SEND_FREESPINS = "inbox_send_free_spins";
	public const string ACCEPT_CREDITS = "inbox_accept_credits";
	public const string ACCEPT_FREESPINS = "inbox_accept_free_spins";
	public const string ACCEPT_SYSTEM_MESSAGE = "inbox_accept_system_messages";
	public const string ASK_FOR_CREDITS = "inbox_ask_for_credits";
	public const string FORCE_RATING_PAST_SPINS = "create_rating_inbox_item_dev";

	public const string NETWORK_SEND_GIFT = "send_gift"; // this is literally just for network profiles. network profiles gets to send gifts without a corresponding event id
	public const string NETWORK_SEND_CREDITS = "send_credits"; // gift type that has to be included for network profiles
	public const string NETWORK_GIFT_TYPE = "gift_type"; // gift type that has to be included for network profiles

	// properties to send
	private const string TO = "to";
	private const string EVENT_ID = "event_id";
	private const string BONUS_GAME = "bonus_game";
	private const string SLOTS_GAME = "slots_game";
	private const string MESSAGE = "message";
	private const string ACTION_KEY = "action_key";
	private const string PRIMARY = "primary";
	private const string CLOSE = "close";

	private InboxAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	public static void addAction(InboxItem inboxItem, bool isClose = false)
	{
		if (inboxItem.itemType == InboxItem.InboxType.FREE_SPINS)
		{
			acceptFreespins(inboxItem.eventId, isClose);
		}
		if (inboxItem.itemType == InboxItem.InboxType.FREE_CREDITS)
		{
			acceptCredits(inboxItem.eventId, isClose);
		}
		if (inboxItem.itemType == InboxItem.InboxType.SEND_CREDITS)
		{
			sendCredits(inboxItem.senderZid, inboxItem.eventId, isClose:isClose);
		}
		if (inboxItem.itemType == InboxItem.InboxType.MESSAGE)
		{
			acceptSystemMessage(inboxItem.eventId, isClose);
		}
	}

	public static void addAction(InboxItem inboxItem, string actionKey)
	{
		if (inboxItem.itemType == InboxItem.InboxType.MESSAGE)
		{
			acceptSystemMessage(inboxItem.eventId, actionKey);
		}
	}
	
	/// <summary>
	/// This version of addAction only applies to a batch of inbox items. Currently this functionality is only supported for free credits
	/// but can be expanded for messages later.
	/// </summary>
	/// <param name="inboxItem"></param>
	/// <param name="isClose"></param>
	public static void addAction(List<InboxItem> inboxItems, bool isClose = false)
	{
		List<string> zids = new List<string>();
		List<string> eventIds = new List<string>();

		bool isAccepting = false;

		for (int i = 0; i < inboxItems.Count; ++i)
		{
			InboxItem inboxItem = inboxItems[i];

			if (inboxItem.itemType == InboxItem.InboxType.FREE_CREDITS)
			{
				isAccepting = true;
				eventIds.Add(inboxItem.eventId);
			}
			if (inboxItem.itemType == InboxItem.InboxType.SEND_CREDITS)
			{
				zids.Add(inboxItem.senderZid);
				eventIds.Add(inboxItem.eventId);
			}
		}

		if (isAccepting)
		{
			acceptCredits(eventIds, isClose);
		}
		else
		{
			sendCredits(zids, eventIds, isClose: isClose);
		}
	}

	public static void getInboxItems(EventDelegate callback = null)
	{
		if (callback != null)
		{
			Server.registerEventDelegate("inbox_items_event", callback, false);
		}
		InboxAction inboxAction = new InboxAction(ActionPriority.IMMEDIATE, GET_INBOX);
	}

	public static void sendNetworkGift(List<string> zids, string message = "")
	{
		InboxAction inboxAction = new InboxAction(ActionPriority.HIGH, NETWORK_SEND_GIFT);
		inboxAction.zids = zids;
		inboxAction.message = message;
	}

	public static void sendCredits(List<string> zids, List<string> eventIds, string message = "", bool isClose = false)
	{
		InboxAction inboxAction = new InboxAction(ActionPriority.HIGH, SEND_CREDITS);
		inboxAction.zids = zids;
		inboxAction.eventIds = eventIds;
		inboxAction.message = message;
		inboxAction.actionKey = setActionKey(isClose);
		AnalyticsManager.Instance.LogPlayerGiftAction(SEND_CREDITS, Glb.GIFTING_CREDITS);
	}

	public static void sendCredits(string zid, string eventId, string message = "", bool isClose = false)
	{
		InboxAction inboxAction = new InboxAction(ActionPriority.HIGH, SEND_CREDITS);
		inboxAction.zid = zid;
		inboxAction.eventId = eventId;
		inboxAction.message = message;
		inboxAction.actionKey = setActionKey(isClose);
		AnalyticsManager.Instance.LogPlayerGiftAction(SEND_CREDITS, Glb.GIFTING_CREDITS);
	}

	public static void sendFreespins(string zid, string gameKey, string bonusGameKey, string paytable, string message = "")
	{
		InboxAction inboxAction = new InboxAction(ActionPriority.HIGH, SEND_FREESPINS);
		inboxAction.gameKey = gameKey;
		inboxAction.bonusGameKey = bonusGameKey;
		inboxAction.zid = zid;
		inboxAction.message = message;
		AnalyticsManager.Instance.LogPlayerGiftAction(SEND_FREESPINS, 1);
	}

	public static void sendFreespins(List<string> zids, string gameKey, string bonusGameKey, string paytable, string message = "")
	{
		InboxAction inboxAction = new InboxAction(ActionPriority.HIGH, SEND_FREESPINS);
		inboxAction.gameKey = gameKey;
		inboxAction.bonusGameKey = bonusGameKey;
		inboxAction.zids = zids;
		AnalyticsManager.Instance.LogPlayerGiftAction(SEND_FREESPINS, 1);
	}

	public static void sendAskForCredits(string zid, string message)
	{
		InboxAction inboxAction = new InboxAction(ActionPriority.HIGH, ASK_FOR_CREDITS);
		inboxAction.zid = zid;
		inboxAction.message = message;
	}

	public static void sendAskForCredits(List<string> zids, string message)
	{
		InboxAction inboxAction = new InboxAction(ActionPriority.HIGH, ASK_FOR_CREDITS);
		inboxAction.zids = zids;
		inboxAction.message = message;
	}

	public static void forceRatingForPastSpins(ActionPriority priority)
	{
		InboxAction inboxAction = new InboxAction(priority, FORCE_RATING_PAST_SPINS);
	}
	
	public static void acceptCredits(List<string> eventIds, bool isClose = false)
	{
		InboxAction inboxAction = new InboxAction(ActionPriority.HIGH, ACCEPT_CREDITS);
		inboxAction.eventIds = eventIds;
		inboxAction.actionKey = setActionKey(isClose);
	}

	public static void acceptCredits(string eventId, bool isClose = false)
	{
		InboxAction inboxAction = new InboxAction(ActionPriority.HIGH, ACCEPT_CREDITS);
		inboxAction.eventId = eventId;
		inboxAction.actionKey = setActionKey(isClose);
	}

	public static void acceptFreespins(string eventId, bool isClose = false)
	{
		InboxAction inboxAction = new InboxAction(ActionPriority.HIGH, ACCEPT_FREESPINS);
		inboxAction.eventId = eventId;
		inboxAction.actionKey = setActionKey(isClose);
	}

	// This function is used for the regular inbox item with just a single primary action.
	public static void acceptSystemMessage(string eventId, bool isClose = false)
	{
		InboxAction inboxAction = new InboxAction(ActionPriority.HIGH, ACCEPT_SYSTEM_MESSAGE);
		inboxAction.eventId = eventId;
		inboxAction.actionKey = setActionKey(isClose);
	}

	// This function is used for the inbox item with more than one primary actions. e.g. inbox slot machine rating has
	// primary options "love", "like" and "dislike".
	// so the caller needs to specify the actionKey to get the correct action involved.
	public static void acceptSystemMessage(string eventId, string actionKey)
	{
		InboxAction inboxAction = new InboxAction(ActionPriority.HIGH, ACCEPT_SYSTEM_MESSAGE);
		inboxAction.eventId = eventId;
		inboxAction.actionKey = actionKey;
	}
	
	private static string setActionKey(bool isClose = false)
	{
		if (isClose)
		{
			return CLOSE;
		}

		return PRIMARY;
	}

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(SEND_CREDITS, new string[] {TO, EVENT_ID, MESSAGE});
				_propertiesLookup.Add(NETWORK_SEND_GIFT, new string[] {TO, MESSAGE, NETWORK_GIFT_TYPE});
				_propertiesLookup.Add(SEND_FREESPINS, new string[] { TO, SLOTS_GAME, BONUS_GAME, MESSAGE });
				_propertiesLookup.Add(ACCEPT_CREDITS, new string[] { EVENT_ID, ACTION_KEY });
				_propertiesLookup.Add(ACCEPT_FREESPINS, new string[] { EVENT_ID, ACTION_KEY });
				_propertiesLookup.Add(ACCEPT_SYSTEM_MESSAGE, new string[] { TO, ACTION_KEY, EVENT_ID });
				_propertiesLookup.Add(ASK_FOR_CREDITS, new string[] { TO, MESSAGE });
				_propertiesLookup.Add(GET_INBOX, new string[]{});
				_propertiesLookup.Add(FORCE_RATING_PAST_SPINS, new string[]{});
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
					if (eventIds != null)
					{
						appendPropertyJSON(builder, property, eventIds);
					}
					else
					{
						appendPropertyJSON(builder, property, eventId);
					}
					break;

				case TO:
					if (zids != null && type != NETWORK_SEND_GIFT)
					{
						appendPropertyJSON(builder, property, zids);
					}
					else if (type != NETWORK_SEND_GIFT)
					{
						appendPropertyJSON(builder, property, zid);
					}
					else
					{
						appendPropertyJSON(builder, property, string.Join(",", zids.ToArray()));
					}
					break;

				case BONUS_GAME:
					appendPropertyJSON(builder, property, bonusGameKey);
					break;

				case SLOTS_GAME:
					appendPropertyJSON(builder, property, gameKey);
					break;

				case MESSAGE:
					appendPropertyJSON(builder, property, message);
					break;

				case ACTION_KEY:
					appendPropertyJSON(builder, property, actionKey);
					break;

				case NETWORK_GIFT_TYPE:
					appendPropertyJSON(builder, property, NETWORK_SEND_CREDITS);
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
	}
}