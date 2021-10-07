using UnityEngine;
using System.Collections;

public class StatsInbox
{
	public static void logDialog(string phylum = "coins", string genus = "view", string powerupState = "power_ups_off")
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "hir_inbox_v2",
			phylum: phylum,
			genus: genus,
			milestone:powerupState
		);
	}

	public static void logCollectOrHelp(string phylum="coins", string klass = "collect", string senderZid = "", string gameName = "", int amount = 0)
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "hir_inbox_v2",
			phylum: phylum,
			klass: klass,
			family: senderZid,
			genus: "click",
			milestone: gameName,
			val: amount
		);
	}

	public static void logAddFriends(string tab="coins")
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "hir_inbox_v2",
			phylum: tab,
			klass: "add_friends",
			genus: "click"
		);
	}

	public static void logDailyLimits(string genus="click", string tabName = "coins")
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "hir_inbox_v2",
			phylum:tabName,
			klass: "daily_limit_button",
			genus: genus
		);
	}

	public static void logRequestCoins()
	{
		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "hir_inbox_v2",
			phylum: "coins",
			klass: "request",
			genus: "click"
		);
	}

	public static void logMessage(InboxItem inboxItem, string state="click")
	{
		string type = "announcement";
		string family = inboxItem.messageKey;
		if (inboxItem.primaryCommand is InboxCollectCreditsCommand)
		{
			type = "gift";
		}
		else if (inboxItem.primaryCommand is InboxSpecialOfferCommand)
		{
			type = "offer";
		}
		else if (inboxItem.primaryCommand is InboxFindFriendsCommand ||
		         inboxItem.primaryCommand is InboxShowAllFriendsCommand)
		{
			type = "friends_link";
		}
		else if(inboxItem.primaryCommand is InboxUnlockGameCommand)
		{
			type = "game_link";
			family = inboxItem.gameKey;
		}

		StatsManager.Instance.LogCount
		(
			counterName: "dialog",
			kingdom: "hir_inbox_v2",
			phylum: "messages",
			family: family,
			klass: type,
			genus: state
		);
	}
}