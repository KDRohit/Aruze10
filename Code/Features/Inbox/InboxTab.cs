using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class InboxTab : MonoBehaviour
{
	// =============================
	// PROTECTED
	// =============================
	[SerializeField] protected ObjectSwapper buttonSwapper;
	[SerializeField] protected ObjectSwapper tabStateSwapper;
	[SerializeField] protected ViewRegionSlideController regionSlideController;
	[SerializeField] protected GameObject listItemParent;
	[SerializeField] protected GameObject listItemPrefab;
	[SerializeField] protected TextMeshPro notifText;
	[SerializeField] protected GameObject notifBubble;
	[SerializeField] protected GameObject notifBubbleElite;
	[SerializeField] protected InboxFooter inboxFooter;
	[SerializeField] protected GameObject powerupsItemPrefab;
	[SerializeField] protected UIStateImageButton tabButton;
	[SerializeField] protected GameObject objectPoolParent;

	protected GameObjectCacher objectPool; // Used to store the pool of message objects for the slide controller

	protected string tabName = "";
	
	protected readonly Dictionary<GameObject, InboxListItem> activeInboxListItems = new Dictionary<GameObject, InboxListItem>();
	protected readonly Dictionary<InboxItem, InboxListItem> inboxItemToActiveListItems = new Dictionary<InboxItem, InboxListItem>();
	protected readonly Dictionary<InboxItem.InboxType, bool> hasUniqueZids = new Dictionary<InboxItem.InboxType, bool>();
	protected readonly  List<InboxItem> inboxItemDataList = new List<InboxItem>();
	protected GameObject powerupsItem;
	

	// =============================
	// PUBLIC
	// =============================
	public delegate void onRemoveItem(InboxListItem item, Dict args = null);
	public delegate void onAcceptItem(InboxListItem item);
	public delegate void onDestroyItem(InboxListItem item);

	// =============================
	// CONST
	// =============================
	protected const string FILLED = "filled";
	protected const string EMPTY = "empty";
	protected const string ACTIVE = "active";
	protected const string INACTIVE = "inactive";
	protected const float CELL_HEIGHT = 374.0f;
	protected const float ADD_ITEM_DELAY = 0.25f;
	protected const int DEFAULT_VISIBLE_ITEMS = 3;
	protected const int PADDED_VISIBLE_ITEMS = 3;

	void Awake()
	{
		setNotifBubble();
	}

	private void OnDestroy()
	{
		StopAllCoroutines();
	}

	/*=========================================================================================
	SETUP/STATE HANDLING
	=========================================================================================*/
	/// <summary>
	/// Basic setup of the tab items that need to be displayed
	/// </summary>
	public virtual void setup()
	{
		listItemParent.transform.localPosition = Vector3.zero;

		if (objectPool == null)
		{
			objectPool = new GameObjectCacher(objectPoolParent, listItemPrefab);
		}

		cleanup();

		populateItems();

		setTabState();

		setNotifBubble();

		setSlideController();

		if (tabButton != null)
		{
			tabButton.SetSelected(true);	
		}
	}

	/// <summary>
	/// Set the current state of the tab based on whether or not it has any items in it
	/// </summary>
	public virtual void setTabState()
	{

	}

	/// <summary>
	/// Enables/disaables the notif bubble, and sets the text based on the number of items in the list
	/// </summary>
	public virtual void setNotifBubble()
	{
		int count = hasApplicablePowerup ? numberOfListItems - 1 : numberOfListItems;
		notifText.text = count.ToString();
		notifBubble.SetActive(count > 0);
	}

	/// <summary>
	/// Sets the slide controller bounds, and shows/hides the scroll bar accordingly
	/// </summary>
	private void setSlideController()
	{
		if (regionSlideController != null)
		{
			updateSlideControllerBounds();
			regionSlideController.init(onGetItemForIndexForSlideController, onIndexChangedForItemInSlideController, onItemRemovedFromSlideController, CELL_HEIGHT);
		}
	}

	/// <summary>
	/// Update the bounds for the slide controller due to a change in the number of elements being displayed
	/// </summary>
	private void updateSlideControllerBounds()
	{
		if (regionSlideController != null)
		{
			regionSlideController.setBounds(Mathf.Max(0, CELL_HEIGHT * numberOfListItems - CELL_HEIGHT * DEFAULT_VISIBLE_ITEMS), 0);
			
			if (regionSlideController.scrollBar != null)
			{
				regionSlideController.scrollBar.gameObject.SetActive(numberOfListItems > DEFAULT_VISIBLE_ITEMS);
			}
		}
	}

	/// <summary>
	/// Get the item that should be displayed at a specific index on the SlideController.
	/// </summary>
	/// <param name="index">The index to use for the data for this new message.</param>
	private GameObject onGetItemForIndexForSlideController(int index)
	{
		float startPosition = powerupsItem != null ? -CELL_HEIGHT : 0.0f;
	
		if (index >= 0 && index < inboxItemDataList.Count)
		{
			return createItem(inboxItemDataList[index], startPosition + (index * -CELL_HEIGHT));
		}
		else
		{
			return null;
		}
	}

	/// <summary>
	/// Update the positioning of an item in the SlideController who's position has been changed
	/// due to getting a new index (probably because an element was removed).
	/// </summary>
	/// <param name="item">The item to update.</param>
	/// <param name="index">The new index of the element.</param>
	private void onIndexChangedForItemInSlideController(GameObject item, int index)
	{
		float startPosition = powerupsItem != null ? -CELL_HEIGHT : 0.0f;
		if (item != null)
		{
			if (activeInboxListItems.TryGetValue(item, out InboxListItem itemScript))
			{
				if (itemScript.inboxItem == null || !itemScript.inboxItem.hasAcceptedItem)
				{
					if (item.transform.localPosition.y != startPosition)
					{
						iTween.Stop(item);
						iTween.MoveTo(item,
							iTween.Hash("y", startPosition + (index * -CELL_HEIGHT), "time", 0.5f, "islocal", true));
					}
				}
			}
		}
	}

	/// <summary>
	/// Release an instance of a message prefab back into a pool of objects we can grab from.
	/// </summary>
	/// <param name="item">The message prefab instance being released.</param>
	protected virtual void releaseItemInstance(GameObject item)
	{
		if (activeInboxListItems.TryGetValue(item, out InboxListItem itemScript))
		{
			inboxItemToActiveListItems.Remove(itemScript.inboxItem);
		}
		
		activeInboxListItems.Remove(item);

		// Kill any iTween movement stuff the item might be doing (since we don't want that to
		// continue when the object is pulled out of the pool).
		iTween.Stop(item);
		
		objectPool.releaseInstance(item);
	}

	/// <summary>
	/// Handles what gets done when the SlideController is trying to release an item it isn't going to display anymore.
	/// </summary>
	private void onItemRemovedFromSlideController(GameObject item)
	{
		releaseItemInstance(item);
	}

	/// <summary>
	/// Obtain an instance of the message prefab to be used for displaying an inbox message.
	/// </summary>
	protected virtual GameObject getItemInstance(InboxItem item)
	{
		GameObject inboxItem = null;
		inboxItem = objectPool.getInstance();

		return inboxItem;
	}

	/// <summary>
	/// Generate a new item to be displayed in the ViewRegionSlideController.
	/// This item will most likely come from a pool.
	/// </summary>
	/// <param name="item">The InboxItem that this new object is supposed to represent.</param>
	/// <param name="position">The location that the element will be placed.</param>
	private GameObject createItem(InboxItem item, float position)
	{
		GameObject inboxItem = getItemInstance(item);
		inboxItem.transform.parent = listItemParent.transform;
		inboxItem.transform.localScale = Vector3.one;
		inboxItem.transform.localPosition = new Vector3(0, position, -1);
		inboxItem.SetActive(true);

		InboxListItem itemScript = inboxItem.GetComponent<InboxListItem>();
		if (itemScript != null)
		{
			itemScript.reset();
			itemScript.init(item, onItemRemoved, onItemAccepted, onItemDestroyed);
			itemScript.setState(tabName);	
		}
		else
		{
			Debug.LogError("Could not find item script on gameobject");
		}
		

		if (!activeInboxListItems.ContainsKey(inboxItem))
		{
			activeInboxListItems.Add(inboxItem, itemScript);
		}

		if (!inboxItemToActiveListItems.ContainsKey(item))
		{
			inboxItemToActiveListItems.Add(item, itemScript);
		}

		return inboxItem;
	}

	/// <summary>
	/// Populate the slide controller with inbox items
	/// </summary>
	protected virtual void populateItems()
	{
		inboxItemDataList.Clear();

		if (hasApplicablePowerup)
		{
			addPowerupItem();

			InboxListItem powerupScript = powerupsItem.GetComponent<InboxListItem>();

			powerupScript.init(null, onItemRemoved, onItemAccepted, onItemDestroyed);
		}
	}

	/// <summary>
	/// Populates gift chest items based on their type. If the type is left blank, it will populate anything that passes
	/// the canDisplayItem() check
	/// </summary>
	/// <param name="startPosition"></param>
	/// <param name="type"></param>
	protected virtual void populateItemsByType(InboxItem.InboxType type = InboxItem.InboxType.NONE, bool requireUniqueZids = false)
	{
		int overflow = 0;
		int itemsAdded = 0;
		hasUniqueZids[type] = requireUniqueZids;
		HashSet<string> zidsUsed = new HashSet<string>();
		for (int i = 0; i < InboxInventory.items.Count; i++)
		{
			InboxItem item = InboxInventory.items[i];
			if (canDisplayItem(item) && (type == InboxItem.InboxType.NONE || item.itemType == type))
			{
				if (!requireUniqueZids || !zidsUsed.Contains(item.senderZid))
				{
					if (requireUniqueZids)
					{
						zidsUsed.Add(item.senderZid);
					}
					inboxItemDataList.Add(item);
				}
			}
		}
	}

	/// <summary>
	/// Enables the buttons
	/// </summary>
	public void enable()
	{
		StartCoroutine(delayEnable());
		registerEvents();
	}

	// UI Image Button will reverse the swap states if this is set to early, we'll wait until that's initialized
	// and then set it active
	private IEnumerator delayEnable()
	{
		yield return null;
		buttonSwapper.setState(ACTIVE);
		if (tabButton != null)
		{
			tabButton.SetSelected(true);	
		}
	}

	/// <summary>
	/// Disables the entire tab, disabling all buttons, and does a cleanup of all list items
	/// </summary>
	public void disable()
	{
		buttonSwapper.setState(INACTIVE);
		unregisterEvents();
		cleanup();
	}

	/*=========================================================================================
	TEMPORARY ITEM METHODS, THESE WILL BE REPLACED WHEN THE NEW INBOX TECH IS PHASED IN
	=========================================================================================*/
	protected virtual void addPowerupItem(float position = 0f)
	{
		powerupsItem = Instantiate(powerupsItemPrefab);
		powerupsItem.transform.parent = listItemParent.transform;
		powerupsItem.transform.localPosition = new Vector3(0, position, -1);
		powerupsItem.transform.localScale = Vector3.one;

		PowerupBase.registerFunctionToPowerup(PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_VIP_BOOSTS_KEY), onPowerupExpired);
		PowerupBase.registerFunctionToPowerup(PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_BUY_PAGE_KEY), onPowerupExpired);
		PowerupBase.registerFunctionToPowerup(PowerupsManager.getActivePowerup(PowerupBase.POWER_UP_FREE_SPINS_KEY), onPowerupExpired);
	}

	/*=========================================================================================
	BUTTON/EVENT HANDLING
	=========================================================================================*/
	/// <summary>
	/// Run action on all the list items
	/// </summary>
	protected virtual void onCollectAll()
	{

	}

	/// <summary>
	/// Run through all the help actions
	/// </summary>
	protected virtual void onHelpAll()
	{

	}

	/// <summary>
	/// Run through all the request actions
	/// </summary>
	protected virtual void onRequestAll()
	{

	}


	protected virtual void onPowerupExpired(Dict args = null, GameTimerRange originalTimer = null)
	{
		if (!hasApplicablePowerup)
		{
			if (powerupsItem != null)
			{
				powerupsItem.GetComponent<InboxListItem>().dismiss();
			}
		}

		setFooterButtons();
		refreshItems();
	}

	/// <summary>
	/// Handler for when a list item is closed or dismissed. This simply shifts the list. Use onItemDestroyed()
	/// to remove fully.
	/// </summary>
	/// <param name="item"></param>
	/// <param name="args"></param>
	public virtual void onItemRemoved(InboxListItem item, Dict args = null)
	{
		refreshItems();
		updateSlideControllerBounds();
		setNotifBubble();
	}

	/// <summary>
	/// Handling updates when an item has been collected. Typically this should adjust the footer limits
	/// </summary>
	/// <param name="item"></param>
	/// <param name="args"></param>
	public virtual void onItemAccepted(InboxListItem item)
	{
		refreshItems();
	}

	/// <summary>
	/// Removes the element from the list, and releases it back to the object pool
	/// </summary>
	/// <param name="item"></param>
	public virtual void onItemDestroyed(InboxListItem item)
	{
		if (item == null)
		{
			return;
		}
		
		//remove item
		if (item.inboxItem != null)
		{
			inboxItemDataList.Remove(item.inboxItem);	
		}
		
		// clear the special offer item reference
		if (item.gameObject == powerupsItem)
		{
			powerupsItem = null;
		}
	
		// Now update the slide controller
		if (regionSlideController != null)
		{
			regionSlideController.removeObject(item.gameObject);
		}

		setNotifBubble();

		if (inboxItemDataList.Count == 0)
		{
			setTabState();
		}
	}

	public void registerEvents()
	{
		if (inboxFooter != null)
		{
			inboxFooter.onHelpEvent += onHelpAll;
			inboxFooter.onCollectEvent += onCollectAll;
			inboxFooter.onRequestEvent += onRequestAll;
		}
	}

	public void unregisterEvents()
	{
		if (inboxFooter != null)
		{
			inboxFooter.onHelpEvent -= onHelpAll;
			inboxFooter.onCollectEvent -= onCollectAll;
			inboxFooter.onRequestEvent -= onRequestAll;
		}
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	/// <summary>
	/// Should be overwritten in subclasses if a tab wants to set the text of the footer. This includes the request,
	/// collect amount, etc.
	/// </summary>
	protected virtual void setFooterButtons()
	{

	}

	/// <summary>
	/// Calls refresh on all of the inbox list items
	/// </summary>
	public void refreshItems()
	{
		if (activeInboxListItems == null || activeInboxListItems.Count == 0)
		{
			return;
		}
		
		foreach (InboxListItem item in activeInboxListItems.Values)
		{
			if (item == null || item.gameObject == null)
			{
				continue;
			}
			item.refresh();
		}
	}

	/// <summary>
	/// Removes all the game object instances created for inbox items
	/// </summary>
	protected virtual void cleanup()
	{
		inboxItemDataList.Clear();
		regionSlideController.clearAllDisplayObjects();
		
		foreach (KeyValuePair<GameObject, InboxListItem> kvp in activeInboxListItems)
		{
			Destroy(kvp.Key);
		}

		if (powerupsItem != null)
		{
			Destroy(powerupsItem);
			powerupsItem = null;
		}

		activeInboxListItems.Clear();
		inboxItemToActiveListItems.Clear();
	}
	
	/// <summary>
	/// Get the number of items that this tab is displaying
	/// NOTE: Could consider changing this to just get the size of inboxItemDataList
	/// </summary>
	protected virtual int numberOfListItems
	{
		get
		{
			int count = hasApplicablePowerup ? 1 : 0;
			Dictionary<InboxItem.InboxType, HashSet<string>> zidsUsed = new Dictionary<InboxItem.InboxType, HashSet<string>>();
			for (int i = 0; i < InboxInventory.items.Count; i++)
			{
				InboxItem item = InboxInventory.items[i];
				if (canDisplayItem(item))
				{
					bool uniqueZids = false;
					if (!hasUniqueZids.TryGetValue(item.itemType, out uniqueZids))
					{
						uniqueZids = false;
					}
					if (uniqueZids)
					{
						HashSet<string> zids = new HashSet<string>();
						if (!zidsUsed.TryGetValue(item.itemType, out zids))
						{
							zids = new HashSet<string>();
							zidsUsed.Add(item.itemType, zids);
						}
						if (zids.Contains(item.senderZid))
						{
							continue;
						}
						else
						{
							zids.Add(item.senderZid);
						}
					}
					count++;
				}
			}

			return count;
		}
	}

	/// <summary>
	/// Returns true if the gift chest item can be added to the inbox dialog based on the current tab
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public virtual bool canDisplayItem(InboxItem item)
	{
		return item.canBeClaimed && !item.hasAcceptedItem && !item.hasDeclinedItem;
	}

	public virtual bool hasItems
	{
		get
		{
			return numberOfListItems > 0;
		}
	}

	protected bool hasApplicablePowerup
	{
		get
		{
			return
				PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_VIP_BOOSTS_KEY) ||
				PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY) ||
				PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_FREE_SPINS_KEY);
		}
	}
}
