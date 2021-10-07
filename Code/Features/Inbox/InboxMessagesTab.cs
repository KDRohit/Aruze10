using UnityEngine;
using System.Collections.Generic;

public class InboxMessagesTab : InboxTab
{
	[SerializeField] protected GameObject eliteListItemPrefab;
	private GameObjectCacher eliteObjectPool;
	private HashSet<GameObject> eliteItemInstances = new HashSet<GameObject>();

	[SerializeField] protected GameObject oocListItemPrefab;
	private GameObjectCacher oocObjectPool;
	private HashSet<GameObject> oocItemInstances = new HashSet<GameObject>();
	
	[SerializeField] protected GameObject ratingListItemPrefab;
	private GameObjectCacher ratingObjectPool;
	private HashSet<GameObject> ratingItemInstances = new HashSet<GameObject>();
	private const string TAB_NAME = "messages";

	/// <inheritdoc/>
	public override void setup()
	{
		tabName = TAB_NAME;
		if (eliteObjectPool == null)
		{
			eliteObjectPool = new GameObjectCacher(objectPoolParent, eliteListItemPrefab);
		}

		if (oocObjectPool == null)
		{
			oocObjectPool = new GameObjectCacher(objectPoolParent, oocListItemPrefab);
		}
		
		if (ratingObjectPool == null)
		{
			ratingObjectPool = new GameObjectCacher(objectPoolParent, ratingListItemPrefab);
		}

		base.setup();
	}

	protected override GameObject getItemInstance(InboxItem item)
	{
		if (item.primaryCommand is InboxEliteCommand)
		{
			if (eliteObjectPool != null)
			{
				GameObject eliteItem = eliteObjectPool.getInstance();
				eliteItemInstances.Add(eliteItem);
				return eliteItem;
			}
			else
			{
				return null;
			}
		}
		else if (item.feature == "Special OOC")
		{
			if (oocObjectPool != null)
			{
				GameObject oocItem = oocObjectPool.getInstance();
				oocItemInstances.Add(oocItem);
				return oocItem;
			}
			else
			{
				return null;
			}
		}
		else if (item.primaryCommand != null && item.primaryCommand.action.Equals("select_rating"))
		{
			if (ratingObjectPool != null)
			{
				GameObject ratingItem = ratingObjectPool.getInstance();
				oocItemInstances.Add(ratingItem);
				return ratingItem;
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
		if (eliteItemInstances.Contains(item))
		{
			eliteObjectPool.releaseInstance(item);
			eliteItemInstances.Remove(item);
			if (activeInboxListItems.TryGetValue(item, out InboxListItem itemScript))
			{
				inboxItemToActiveListItems.Remove(itemScript.inboxItem);
			}
			activeInboxListItems.Remove(item);
		}
		else if (oocItemInstances.Contains(item))
		{
			oocObjectPool.releaseInstance(item);
			oocItemInstances.Remove(item);
			if (activeInboxListItems.TryGetValue(item, out InboxListItem itemScript))
			{
				inboxItemToActiveListItems.Remove(itemScript.inboxItem);
			}
			activeInboxListItems.Remove(item);
		}
		else if (ratingItemInstances.Contains(item))
		{
			ratingObjectPool.releaseInstance(item);
			ratingItemInstances.Remove(item);
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
		int count = InboxInventory.totalActionItems(InboxItem.InboxType.MESSAGE);
		notifText.text = count.ToString();
		notifBubble.SetActive(count > 0);

		if (InboxInventory.findItemByCommand<InboxEliteCommand>() != null)
		{
			if (notifBubbleElite != null)
			{
				notifBubbleElite.SetActive(count > 0);
			}
		}
		else if (notifBubbleElite != null)
		{
			notifBubbleElite.SetActive(false);
		}
	}
	
	/// <inheritdoc/>
	protected override void populateItems()
	{
		base.populateItems();

		populateItemsByType(InboxItem.InboxType.MESSAGE);
	}

	/// <inheritdoc/>
	public override bool canDisplayItem(InboxItem item)
	{
		return item.itemType == InboxItem.InboxType.MESSAGE &&
		       item.hasPrimaryCommand &&
		       item.primaryCommand.canExecute &&
		       base.canDisplayItem(item);
	}
	
	/// <inheritdoc/>
	protected override void cleanup()
	{
		base.cleanup();

		foreach (GameObject eliteItem in eliteItemInstances)
		{
			Destroy(eliteItem);
		}
		eliteItemInstances.Clear();

		foreach (GameObject oocItem in oocItemInstances)
		{
			Destroy(oocItem);
		}
		oocItemInstances.Clear();

	}
}
