using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Class for storing/retrieving inbox data items
/// </summary>
public class InboxInventory : IResetGame
{
	public static List<InboxItem> items = new List<InboxItem>();

	private const string INBOX_ITEMS_KEY = "inbox_items";
	private const string SUCCESS = "success";
	private const string FAILED = "failed";

	public static void init()
	{
		InboxCommandGenerator.init();
		registerEvents();
		InboxAction.getInboxItems();
	}

	/*=========================================================================================
	SEVER/EVENT HANDLING
	=========================================================================================*/
	/// <summary>
	/// Response handler for the inbox_details action
	/// </summary>
	/// <param name="inboxData"></param>
	public static void onInboxUpdate(JSON inboxData)
	{
		items = new List<InboxItem>();
		JSON[] inboxItemData = inboxData.getJsonArray(INBOX_ITEMS_KEY);

		if (inboxItemData != null)
		{
			for (int i = 0; i < inboxItemData.Length; ++i)
			{
				if (inboxItemData[i] != null)
				{
					if (!containsItem(inboxItemData[i]))
					{
						InboxItem inboxItem = new InboxItem(inboxItemData[i]);

						if (inboxItem.canBeClaimed)
						{
							items.Add(inboxItem);
						}
					}
				}
			}
		}

		items.Sort(sortBySortOrder);
	}

	public static void registerEvents()
	{
		Server.registerEventDelegate("inbox_items_event", onInboxUpdate, true);
		Server.registerEventDelegate("inbox_handled_event", onItemHandled, true);
	}

	public static void unregisterEvents()
	{
		Server.unregisterEventDelegate("inbox_items_event", onInboxUpdate);
		Server.unregisterEventDelegate("inbox_handled_event", onItemHandled);
	}

	public static void onItemHandled(JSON data)
	{
		string status = data.getString("status", "");
		string eventId = data.getString("event_id", "");
		if (status == FAILED)
		{
			// BY: For now server already logs this, we may want to do something on status failed in the future however
			/*Dictionary<string, string> extraFields = new Dictionary<string, string>();
			extraFields.Add("eventid", data.getString("event_id", ""));
			SplunkEventManager.createSplunkEvent("Inbox - Item Failed", "inbox-failure", extraFields);*/
		}
		else
		{
			Server.removePendingCredits("inbox_" + eventId);
		}

	}

	/// <summary>
	/// Performs the action for the inbox item, typically sending an event to the server, and then issuing the
	/// inbox item's primary command
	/// </summary>
	/// <param name="item"></param>
	public static void action(InboxItem item)
	{
		item.action();
		InboxAction.addAction(item);
	}

	/// <summary>
	/// Performs the action for the inbox item, typically sending an event to the server, and then issuing the
	/// inbox item's primary command
	/// 
	/// This function is used for the inbox item with more than one primary actions. e.g. inbox slot machine rating has
	/// primary options "love", "like" and "dislike".
	/// so the caller needs to specify the actionKey to get the correct action involved.
	/// </summary>
	/// <param name="item"></param>
	public static void action(InboxItem item, string actionKey)
	{
		item.action(actionKey);
		InboxAction.addAction(item, actionKey);
	}

	/// <summary>
	/// Performs the action for all inbox items
	/// inbox item's primary command
	/// </summary>
	/// <param name="item"></param>
	public static void action(List<InboxItem> items)
	{
		for (int i = 0; i < items.Count; ++i)
		{
			items[i].action();
		}

		InboxAction.addAction(items);
	}

	/// <summary>
	/// Performs the cancel action for the inbox item
	/// </summary>
	/// <param name="item"></param>
	public static void dismiss(InboxItem item)
	{
		item.dismiss();
		InboxAction.addAction(item, true);
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	/// <summary>
	/// Returns true if an InboxItemData already exists with the associated data. This checks against
	/// the event id since that is always unique.
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	public static bool containsItem(JSON data)
	{
		if (items != null && data != null)
		{
			string eventId = data.getString("event_id", "");
			for (int i = 0; i < items.Count; ++i)
			{
				if (items[i].eventId == eventId)
				{
					return true;
				}
			}
		}

		return false;
	}

	public static InboxItem findItemBy(string zid, InboxItem.InboxType itemType)
	{
		for (int i = 0; i < items.Count; ++i)
		{
			if (items[i].senderZid == zid && items[i].itemType == itemType)
			{
				return items[i];
			}
		}

		return null;
	}

	public static InboxItem findItemByCommand<T>()
	{
		for (int i = 0; i < items.Count; ++i)
		{
			if (items[i].hasPrimaryCommand && items[i].primaryCommand is T)
			{
				return items[i];
			}
		}

		return null;
	}

	public static void removeGoldPassSpins()
	{
		List<InboxItem> toRemove = new List<InboxItem>();
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].itemType == InboxItem.InboxType.FREE_SPINS &&
			    RichPassCampaign.goldGameKeys.Contains(items[i].gameKey))
			{
				toRemove.Add(items[i]);
			}
		}

		if (toRemove.Count > 0)
		{
			for(int i = 0; i < toRemove.Count; i++)
			{
				items.Remove(toRemove[i]);
			}	
			
			items.Sort(sortBySortOrder);
		}
		
	}

	/// <summary>
	/// Returns any daily limits based on the inboxitem type
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public static int getLimitRemaining(InboxItem item)
	{
		switch (item.itemType)
		{
			case InboxItem.InboxType.FREE_SPINS:
				if (SlotsPlayer.instance.giftBonusAcceptLimit != null)
				{
					return SlotsPlayer.instance.giftBonusAcceptLimit.amountRemaining;
				}
				break;

			case InboxItem.InboxType.FREE_CREDITS:
				if (SlotsPlayer.instance.creditsAcceptLimit != null)
				{
					return SlotsPlayer.instance.creditsAcceptLimit.amountRemaining;
				}
				break;

			case InboxItem.InboxType.SEND_CREDITS:
				return SlotsPlayer.instance.creditSendLimit;

			default:
				return int.MaxValue;
		}

		return 0;
	}

	public static int getAmountOfType(InboxItem.InboxType type)
	{
		int count = 0;
		for (int i = 0; i < items.Count; ++i)
		{
			if (items[i].itemType == type)
			{
				count++;
			}
		}

		return count;
	}

	/// <summary>
	/// Returns integer value of how many inbox items of the specified type can have their actions performed
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public static int totalActionItems(InboxItem.InboxType type, bool includeGoldPass = true, bool uniqueZids = false)
	{
		int count = 0;
		HashSet<string> zidsUsed = new HashSet<string>();
		for (int i = 0; i < items.Count; ++i)
		{
			InboxItem item = items[i];
			bool goldPassRequired = item.itemType == InboxItem.InboxType.FREE_SPINS && RichPassCampaign.goldGameKeys.Contains(item.gameKey);
			if (item.itemType == type && getLimitRemaining(item) > 0 && item.canBeClaimed && !item.hasAcceptedItem && (includeGoldPass || !goldPassRequired))
			{
				if (!uniqueZids || !zidsUsed.Contains(item.senderZid))
				{
					// only count free spins gifts that have active options in the lobby
					switch (item.itemType)
					{
						case InboxItem.InboxType.FREE_SPINS:
							if (LobbyOption.activeGameOption(item.gameKey) != null)
							{
								count++;
							}
							break;
					
						case InboxItem.InboxType.MESSAGE:
							if (item.hasPrimaryCommand && item.primaryCommand.canExecute)
							{
								count++;
							}
							break;
					
						case InboxItem.InboxType.FREE_CREDITS:
							count++;
							break;
					}

					if (uniqueZids)
					{
						zidsUsed.Add(item.senderZid);	
					}
				}
			}
		}

		return count;
	}

	/// <summary>
	/// Returns integer value of how many inbox items can have their actions performed
	/// </summary>
	/// <param name="includeSendCredits">include total of send credit items, typically we do not display this</param>
	/// <returns></returns>
	public static int totalActionItems(bool includeSendCredits = false, bool includeGoldPassSpins = true)
	{
		int count = totalActionItems(InboxItem.InboxType.MESSAGE);
		count += totalActionItems(InboxItem.InboxType.FREE_SPINS, includeGoldPassSpins);
		count += totalActionItems(InboxItem.InboxType.FREE_CREDITS);

		if (includeSendCredits)
		{
			count += totalActionItems(InboxItem.InboxType.SEND_CREDITS);
		}

		return count;
	}

	private static int sortBySortOrder(InboxItem a, InboxItem b)
	{
		return a.sortOrder - b.sortOrder;
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		items.Clear();
		unregisterEvents();
	}
}
