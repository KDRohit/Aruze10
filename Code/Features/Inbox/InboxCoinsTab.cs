using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;


public class InboxCoinsTab : InboxTab
{
	public const string TAB_NAME = "coins";

	/// <inheritdoc/>
	public override void setup()
	{
		tabName = TAB_NAME;
		base.setup();

		inboxFooter.setCollectLimit(SlotsPlayer.instance.creditsAcceptLimit.amountRemaining);
		inboxFooter.toggleRequestCoinsButton(SocialMember.friendPlayers.Count > 0);
		inboxFooter.toggleCollectHelpButton(numCoinItems > 0 || numHelpItems > 0);

		setFooterButtons();
	}

	public override void setTabState()
	{
		if (inboxItemDataList.Count > 0 || powerupsItem != null)
		{
			tabStateSwapper.setState("coins_" + FILLED);
		}
		else
		{
			tabStateSwapper.setState("coins_" + EMPTY);
		}
	}

	/// <inheritdoc/>
	protected override void populateItems()
	{
		base.populateItems();
		
		populateItemsByType(InboxItem.InboxType.FREE_CREDITS);
		populateItemsByType(InboxItem.InboxType.SEND_CREDITS);

		notifBubble.SetActive(numberOfListItems > 0);
	}

	/// <inheritdoc/>
	public override void setNotifBubble()
	{
		//these can be null when the dialog is closing
		if (notifBubble == null || notifBubble.gameObject == null || notifText == null || notifText.gameObject == null)
		{
			return;
		}
		
		bool uniqueZids = false;
		if (!hasUniqueZids.TryGetValue(InboxItem.InboxType.FREE_CREDITS, out uniqueZids))
		{
			uniqueZids = false;
		}
		int count = InboxInventory.totalActionItems(InboxItem.InboxType.FREE_CREDITS, true, uniqueZids);
		notifText.text = count.ToString();
		notifBubble.SetActive(count > 0);
	}

	/// <inheritdoc/>
	protected override void onPowerupExpired(Dict args = null, GameTimerRange originalTimer = null)
	{
		base.onPowerupExpired(args, originalTimer);
		inboxFooter.setCollectLimit(SlotsPlayer.instance.creditsAcceptLimit.amountRemaining);
	}

	/*=========================================================================================
	BUTTON/EVENT HANDLING
	=========================================================================================*/
	/// <inheritdoc/>
	public override void onItemRemoved(InboxListItem item, Dict args = null)
	{
		base.onItemRemoved(item, args);
		inboxFooter.setCollectLimit(SlotsPlayer.instance.creditsAcceptLimit.amountRemaining);
		setFooterButtons();
	}

	/// <inheritdoc/>
	public override void onItemAccepted(InboxListItem item)
	{
		base.onItemAccepted(item);
		inboxFooter.setCollectLimit(SlotsPlayer.instance.creditsAcceptLimit.amountRemaining);
		setFooterButtons();
	}

	/// <inheritdoc/>
	protected override void onCollectAll()
	{
		List<InboxItem> inboxItems = new List<InboxItem>();
		
		//Keep track of any items we're collecting that that aren't part of the inboxItemToActiveListItems list.
		//Normally items are removed at the end of their collect animation, but since these aren't on screen, we need to manually remove them.
		List<InboxItem> nonActiveitemsToRemove = new List<InboxItem>();
		for (int i = 0; i < inboxItemDataList.Count; i++)
		{
			InboxItem currentItem = inboxItemDataList[i];
			if (InboxInventory.getLimitRemaining(currentItem) > 0)
			{
				if (currentItem.itemType == InboxItem.InboxType.FREE_CREDITS)
				{
					// Check if we have an active list item that needs to be updated in addition
					// to just handling the server data
					if (inboxItemToActiveListItems.ContainsKey(currentItem))
					{
						InboxListItem itemScript = inboxItemToActiveListItems[currentItem];
						itemScript.playSelect();
						itemScript.toggleCoinBurst(true);
					}
					else
					{
						nonActiveitemsToRemove.Add(currentItem);
					}
					
					if (!currentItem.hasAcceptedItem)
					{
						currentItem.action(); //Only actually call this if we haven't accepted the item
						SlotsPlayer.instance.creditsAcceptLimit.subtract(1);
						inboxItems.Add(currentItem);
					}
				}
			}
		}

		if (inboxItems.Count > 0)
		{
			InboxAction.addAction(inboxItems);
		}
		
		if (nonActiveitemsToRemove.Count > 0)
		{
			inboxFooter.setCollectLimit(SlotsPlayer.instance.creditsAcceptLimit.amountRemaining);
		}

		StatsInbox.logCollectOrHelp
		(
			phylum:"coins",
			klass:"collect_all",
			amount:inboxItems.Count
		);

		setFooterButtons();
	}

	/// <inheritdoc/>
	protected override void onHelpAll()
	{
		List<InboxItem> inboxItems = new List<InboxItem>();
		
		for (int i = 0; i < inboxItemDataList.Count; i++)
		{
			InboxItem currentItem = inboxItemDataList[i];
			if (InboxInventory.getLimitRemaining(currentItem) > 0)
			{
				if (currentItem.itemType == InboxItem.InboxType.SEND_CREDITS)
				{
					inboxItems.Add(currentItem);
					
					// Check if we have an active list item that needs to be updated in addition
					// to just handling the server data
					if (inboxItemToActiveListItems.ContainsKey(currentItem))
					{
						InboxListItem itemScript = inboxItemToActiveListItems[currentItem];
						itemScript.playSelect();
						itemScript.toggleHelpCoinBurst(true);
					}
					
					currentItem.action();
					SlotsPlayer.instance.creditSendLimit--;
				}
			}
		}

		if (inboxItems.Count > 0)
		{
			InboxAction.addAction(inboxItems);
		}

		StatsInbox.logCollectOrHelp
		(
			phylum:"coins",
			klass:"help_all",
			amount:inboxItems.Count
		);

		setFooterButtons();
	}

	/// <inheritdoc/>
	protected override void onRequestAll()
	{
		Dialog.close();
		MFSDialog.showDialog(Dict.create(D.TYPE, MFSDialog.Mode.ASK));
	}

	/// <inheritdoc/>
	protected override void setFooterButtons()
	{
		int collections = Mathf.Min(SlotsPlayer.instance.creditsAcceptLimit.amountRemaining, numCoinItems);
		if (collections > 0)
		{
			inboxFooter.setCollectOrHelp(CommonText.formatNumber(collections), true);
		}
		else if (numHelpItems > 0 || (numCoinItems != 0 && collections == 0))
		{
			inboxFooter.setCollectOrHelp(Localize.text("help_all"));
		}
		else if (numCoinItems == 0)
		{
			inboxFooter.toggleCollectHelpButton(numHelpItems > 0);
		}
	}

	/// <inheritdoc/>
	public override bool canDisplayItem(InboxItem item)
	{
		if (item.itemType == InboxItem.InboxType.FREE_CREDITS || item.itemType == InboxItem.InboxType.SEND_CREDITS)
		{
			// we are now hiding help items when the user reaches their limit
			if (item.itemType == InboxItem.InboxType.SEND_CREDITS && InboxInventory.getLimitRemaining(item) == 0)
			{
				return false;
			}
			return base.canDisplayItem(item);
		}

		return false;
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	/// <summary>
	/// Returns the number of coin gifts in the list
	/// </summary>
	protected int numCoinItems
	{
		get
		{
			int count = 0;
			for (int i = 0; i < InboxInventory.items.Count; i++)
			{
				InboxItem item = InboxInventory.items[i];
				if (canDisplayItem(item))
				{
					if (item.itemType == InboxItem.InboxType.FREE_CREDITS)
					{
						count++;
					}
				}
			}

			return count;
		}
	}

	/// <summary>
	/// Returns the number of help items in the list
	/// </summary>
	protected int numHelpItems
	{
		get
		{
			int count = 0;
			for (int i = 0; i < InboxInventory.items.Count; i++)
			{
				InboxItem item = InboxInventory.items[i];
				if (canDisplayItem(item) && InboxInventory.getLimitRemaining(item) > 0)
				{
					if (item.itemType == InboxItem.InboxType.SEND_CREDITS)
					{
						count++;
					}
				}
			}

			return count;
		}
	}

	protected override int numberOfListItems
	{
		get
		{
			int powerupCount = hasApplicablePowerup ? 1 : 0;
			return numCoinItems + numHelpItems + powerupCount;
		}
	}
}
