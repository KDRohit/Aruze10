using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InboxSpinsTab : InboxTab
{
	[SerializeField] protected GameObject richPassListItemPrefab;
	protected GameObjectCacher richPassObjectPool;
	private HashSet<GameObject> richPassItemInstances = new HashSet<GameObject>();

	public const string TAB_NAME = "spins";

	/// <inheritdoc/>
	public override void setup()
	{
		tabName = TAB_NAME;

		if (richPassObjectPool == null)
		{
			richPassObjectPool = new GameObjectCacher(objectPoolParent, richPassListItemPrefab);
		}

		base.setup();
		
		inboxFooter.setCollectLimit(SlotsPlayer.instance.giftBonusAcceptLimit.amountRemaining, InboxFooter.DAILY_GIFT_LIMIT_SPINS);

		inboxFooter.toggleCollectHelpButton(false);
		inboxFooter.toggleRequestCoinsButton(false);
	}

	protected override GameObject getItemInstance(InboxItem item)
	{
		if (CampaignDirector.isCampaignActive(CampaignDirector.richPass))
		{
			if (richPassObjectPool != null)
			{
				GameObject richPassItem = richPassObjectPool.getInstance();
				richPassItemInstances.Add(richPassItem);
				return richPassItem;
			}
			else
			{
				return null;
			}
		}
		else
		{
			return base.getItemInstance(item);
		}
	}

	protected override void releaseItemInstance(GameObject item)
	{
		if (richPassItemInstances.Contains(item))
		{
			richPassObjectPool.releaseInstance(item);
			richPassItemInstances.Remove(item);
			if (activeInboxListItems.TryGetValue(item, out InboxListItem itemScript))
			{
				inboxItemToActiveListItems.Remove(itemScript.inboxItem);
			}
			activeInboxListItems.Remove(item);
		}
		else
		{
			base.releaseItemInstance(item);
		}
	}
	

	/// <inheritdoc/>
	public override void setTabState()
	{
		if (inboxItemDataList.Count > 0 || powerupsItem != null)
		{
			tabStateSwapper.setState(TAB_NAME + "_" + FILLED);
		}
		else
		{
			tabStateSwapper.setState(TAB_NAME + "_" + EMPTY);
		}
	}

	/// <inheritdoc/>
	public override void setNotifBubble()
	{
		int count = InboxInventory.totalActionItems(InboxItem.InboxType.FREE_SPINS, CampaignDirector.isCampaignActive(CampaignDirector.richPass));
		
		notifText.text = count.ToString();
		notifBubble.SetActive(count > 0);
	}

	/// <inheritdoc/>
	protected override void onPowerupExpired(Dict args = null, GameTimerRange originalTimer = null)
	{
		base.onPowerupExpired();
		inboxFooter.setCollectLimit(SlotsPlayer.instance.giftBonusAcceptLimit.amountRemaining, InboxFooter.DAILY_GIFT_LIMIT_SPINS);
	}

	/// <inheritdoc/>
	protected override void populateItems()
	{
		//remove gold pass items if they're not available anymore
		if (CampaignDirector.richPass == null || !CampaignDirector.richPass.isActive)
		{
			//remove free spins from inbox
			InboxInventory.removeGoldPassSpins();
		}
		
		base.populateItems();

		populateItemsByType(InboxItem.InboxType.FREE_SPINS);

		notifBubble.SetActive(numberOfListItems > 0);
	}

	/// <inheritdoc/>
	public override bool canDisplayItem(InboxItem item)
	{
		if (item.itemType == InboxItem.InboxType.FREE_SPINS)
		{
			return base.canDisplayItem(item) && LobbyOption.activeGameOption(item.gameKey) != null;
		}

		return false;
	}

	/// <inheritdoc/>
	protected override void cleanup()
	{
		base.cleanup();

		foreach (GameObject richPassItem in richPassItemInstances)
		{
			Destroy(richPassItem);
		}
		
		richPassItemInstances.Clear();
	}
}
