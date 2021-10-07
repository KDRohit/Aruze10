using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class FriendsTabAllFriends : MonoBehaviour
{
	[SerializeField] private UICenteredGrid cardGrid;
	[SerializeField] private GameObject cardPrefab;
	[SerializeField] private SlideController slideController;
	[SerializeField] private TextMeshProMasker tmProMasker;
	[SerializeField] private GameObject incentivizeFriendParent;
	[SerializeField] private GameObject noSearchResultsParent;
	[SerializeField] private UIInput searchInput;
	[SerializeField] private ClickHandler clearTextButton;
	[SerializeField] private GameObject searchParent;
	[SerializeField] private GameObject searchXParent;
	[SerializeField] private Transform newMarkerTransform;

	[SerializeField] private TextMeshPro newFriendsBadgeLabel;
	[SerializeField] private GameObject newFriendsBadge;
	[SerializeField] private ImageButtonHandler requestCoinsButton;
	[SerializeField] private ImageButtonHandler giftAllButton;
	[SerializeField] private GameObject giftAllSheen;
	[SerializeField] private UISprite sendGiftIcon;
	[SerializeField] private Color enabledTextColor;
	[SerializeField] private Color disabledTextColor;
		
	private List<FriendCard> allFriendsCards;
	private Vector3 originalContentPosition;

	public const int DEFAULT_ITEMS_VISIBLE = 4;
	private const float CARD_INTRO_DELAY = 0.1f;
	private const int VISIBLE_CARDS = 8;

	private string searchFilter = ""; //current search text
	private bool resetFilterResults = false;
	private List<SocialMember> filteredResults = new List<SocialMember>();  //the current list of filtered friends
	private int totalFriendObjects; //total number of friend objects created
	private Queue<FriendCard> friendCardObjects; //queue of friend objects that are to be recycled.  This prevents constant instantiation when filtering friends
	

	private void Awake()
	{
		originalContentPosition = slideController.content.transform.localPosition;
		cardGrid.onRepositionFinished += onRepositionFinished;
		slideController.onContentMoved += onContentMoved;
	}

	public void onContentMoved(Transform contentTransform, Vector2 delta)
	{
		if (allFriendsCards != null)
		{
			for (int i = 0; i < allFriendsCards.Count; ++i)
			{
				SafeSet.gameObjectActive(allFriendsCards[i].gameObject, slideController.isInView(allFriendsCards[i].gameObject));
			}
		}
	}

	public void updateFriendCardStatus()
	{
		if (allFriendsCards == null)
		{
			return;
		}

		//update the friend cards
		for(int i=0; i<allFriendsCards.Count; ++i)
		{
			//make sure we're not deleting
			if (allFriendsCards[i] == null || allFriendsCards[i].gameObject == null)
			{
				continue;
			}

			allFriendsCards[i].updateStatus();
		}
	}

	private void onGiftAllClick(Dict args = null)
	{
		if (SocialMember.allFriends == null || SocialMember.allFriends.Count <= 0)
		{
			Debug.Log("No friends to send gifts to");
			return;
		}

		if (allFriendsCards != null)
		{
			//update the friend cards
			for (int i = 0; i < allFriendsCards.Count; ++i)
			{
				//make sure we're not deleting
				if (allFriendsCards[i] == null || allFriendsCards[i].gameObject == null ||
				    allFriendsCards[i].isSlotsPlayer)
				{
					continue;
				}

				allFriendsCards[i].playSendGiftAnim();
			}
		}

		List<SocialMember> giftableFriends = new List<SocialMember>();
		for (int i = 0; i < SocialMember.allFriends.Count; ++i)
		{
			if (SocialMember.allFriends[i].canSendCredits)
			{
				giftableFriends.Add(SocialMember.allFriends[i]);
				SocialMember.allFriends[i].canSendCredits = false;
			}
		}

		giftAllButton.SetActive(false);
		giftAllSheen.SetActive(false);
		giftAllButton.sprite.color = Color.grey;
		giftAllButton.label.color = disabledTextColor;

		if (giftableFriends.Count == 0)
		{
			Debug.Log("No friends to send gifts to");
			return;
		}

		string msg = Localize.text("send_to_friends_credit_message_{0}", SlotsPlayer.instance.socialMember.fullName);

		List<string> zids = new List<string>();
		for (int i = 0; i < giftableFriends.Count; ++i)
		{
			// see if a help item already exists for this
			InboxItem inboxItem = InboxInventory.findItemBy(giftableFriends[i].zId, InboxItem.InboxType.SEND_CREDITS);

			if (inboxItem != null)
			{
				InboxAction.sendCredits(giftableFriends[i].zId, inboxItem.eventId);
			}
			else
			{
				zids.Add(giftableFriends[i].zId);
			}
		}

		InboxAction.sendNetworkGift(zids, msg);

		Audio.play(NetworkFriends.FRIEND_ACTION_AUDIO);
		
		StatsManager.Instance.LogCount(
		counterName: "dialog",
		kingdom: "friends",
		phylum: "friends_tab",
		klass: "all_friends",
		family: "gift",
		genus: "all");
		
	}

	private void onRequestCoinsClick(Dict args = null)
	{
		// The experiment validation should have already been done to show this button,
		// so if this is being called, we'll assume it's ok to show the new MFS for it.
		MFSDialog.showDialog(MFSDialog.Mode.ASK, null, SchedulerPriority.PriorityType.IMMEDIATE);

		StatsManager.Instance.LogCount("bottom_nav", "overlay", "friends", "ask_for_credits", "", "click");
	}

	private void udpateGiftAllButton()
	{
		bool canGift = false;
		
		for(int i=0; i < SocialMember.allFriends.Count; ++i)
		{
			if (SocialMember.allFriends[i].canSendCredits)
			{
				canGift = true;
				break;

			}
		}

		bool active = canGift && SlotsPlayer.instance.creditSendLimit > 0;
		giftAllButton.SetActive(active);
		giftAllSheen.SetActive(active);

		if (active)
		{
			giftAllButton.enabled = true;
			giftAllButton.sprite.color = Color.white;
			giftAllButton.label.color = enabledTextColor;
		}
		else
		{
			giftAllButton.enabled = false;
			giftAllButton.sprite.color = Color.grey;
			giftAllButton.label.color = disabledTextColor;
		}
	}

	private void registerEventDelegates()
	{
		NetworkFriends.instance.onFriendRemoved += friendRemoved;
		NetworkFriends.instance.onFriendBlocked += friendRemoved;
		NetworkFriends.instance.onNewFriend += friendAdded;
		NetworkFriends.instance.onInviteAccepted += friendAdded;
		NetworkFriends.instance.onNewFriendCountUpdated += updateBadgeLabel;

		clearTextButton.registerEventDelegate(clearSearchClicked);
		giftAllButton.registerEventDelegate(onGiftAllClick);
		requestCoinsButton.registerEventDelegate(onRequestCoinsClick);
	}

	private void unregisterEventDelegates()
	{
		NetworkFriends.instance.onFriendRemoved -= friendRemoved;
		NetworkFriends.instance.onFriendBlocked -= friendRemoved;
		NetworkFriends.instance.onNewFriend -= friendAdded;
		NetworkFriends.instance.onInviteAccepted -= friendAdded;
		NetworkFriends.instance.onNewFriendCountUpdated -= updateBadgeLabel;

		clearTextButton.unregisterEventDelegate(clearSearchClicked);
		giftAllButton.unregisterEventDelegate(onGiftAllClick);
		requestCoinsButton.unregisterEventDelegate(onRequestCoinsClick);
	}
		
	public void init()
	{
		registerEventDelegates();
		udpateGiftAllButton();
		
		newFriendsBadge.SetActive(NetworkFriends.instance.hasNewFriends);
		newFriendsBadgeLabel.text = NetworkFriends.instance.newFriends.ToString();
		
		friendCardObjects = new Queue<FriendCard>();
		totalFriendObjects = 0;

		filteredResults.AddRange(SocialMember.allFriends); 
		filteredResults.Add(SlotsPlayer.instance.socialMember);
		reset(filteredResults);

		//disable search
		searchParent.gameObject.SetActive(false);
	}

    private void updateBadgeLabel()
	{
		newFriendsBadge.SetActive(NetworkFriends.instance.hasNewFriends);
		newFriendsBadgeLabel.text = NetworkFriends.instance.newFriends.ToString();
	}
	
	// Gets a friend card object from the pool
	private FriendCard getFriendCard(SocialMember member, int friendIndex, int friendRank = 0)
	{

		FriendCard card = null;
		if (null == friendCardObjects || friendCardObjects.Count == 0)
		{
			
			if (totalFriendObjects >= NetworkFriends.instance.friendLimit)
			{
				Debug.LogWarning("Friend limimt reached, cannot create new object");
				return null;
			}

			card = instantiateFriendCard(member, friendIndex, friendRank);	
			++totalFriendObjects;
		}
		else
		{
			card = friendCardObjects.Dequeue();
			onCardCreated(member, card, friendRank);
		}

		return card;

	}

	// Reset the all friends tab with the provided friend list.  If there are no friends available 
	// this funciton will display a message to view the find friends tab.
	private void reset(List<SocialMember> allFriends)
	{
		bool reposition = false;

		if (SocialMember.allFriends.Count == 0)
		{
			// Show the incentivize parent if they are lonely and have no friends.
			incentivizeFriendParent.SetActive(true);
			noSearchResultsParent.SetActive(false);
			requestCoinsButton.gameObject.SetActive(false);
			giftAllButton.gameObject.SetActive(false);
		}
		else if (null != allFriends)
		{
			//turn off no friends message
			incentivizeFriendParent.SetActive(false);

			//index old friends
			Dictionary<string, FriendCard> indexedOldFriends= new Dictionary<string, FriendCard>();
			if (null != allFriendsCards)
			{
				for(int i=0; i<allFriendsCards.Count; ++i)
				{
					indexedOldFriends[allFriendsCards[i].member.zId] = allFriendsCards[i];
				}
			}

			//index new friend list
			Dictionary<string, SocialMember> indexedNewFriends = new Dictionary<string, SocialMember>();
			if (null != allFriends)
			{
				for (int i = 0; i < allFriends.Count; ++i)
				{
					indexedNewFriends[allFriends[i].zId] = allFriends[i];
				}
			}

			//remove firends that aren't valid anymore
			removeOldFriends(indexedNewFriends);

			//add any new friends
			addNewFriends(allFriends, indexedOldFriends);

			//set text display
			if (null != allFriends && allFriends.Count == 0 && !string.IsNullOrEmpty(searchFilter))
			{
				noSearchResultsParent.SetActive(true);
			}
			else
			{
				noSearchResultsParent.SetActive(false);
			}

			requestCoinsButton.gameObject.SetActive(true);

			if (filteredResults != null && filteredResults.Count > 0)
			{
				//can we gift any of these friends
				bool canGift = false;
				for (int i = 0; i < filteredResults.Count; ++i)
				{
					if (filteredResults[i].canSendCredits && filteredResults[i] != SlotsPlayer.instance.socialMember)
					{
						canGift = true;
						break;
					}
				}

				//make button clickable
				
				giftAllButton.SetActive(canGift);
				giftAllButton.gameObject.SetActive(true);
				if (canGift)
				{
					giftAllButton.enabled = true;
					giftAllButton.sprite.color = Color.white;
					giftAllButton.label.color = enabledTextColor;
				}
				else
				{
					giftAllButton.enabled = false;
					giftAllButton.sprite.color = Color.grey;
					giftAllButton.label.color = disabledTextColor;
				}
			}
			else
			{
				giftAllButton.gameObject.SetActive(false);
			}


			//flag to reposition grid
			reposition = true;
		}
		else
		{
			
			removeAllFriends();
			reposition = true;

			// Show the incentivize parent if they are lonely and have no friends.
			incentivizeFriendParent.SetActive(true);
			noSearchResultsParent.SetActive(false);
			requestCoinsButton.gameObject.SetActive(false);
			giftAllButton.gameObject.SetActive(false);
		}

		if (reposition)
		{
			repositionAllFriends();
		}

		scrollToUser();
	}

	private void repositionAllFriends()
	{
		//slide controller culls all the objects not on display.  This causes the card grid to not count
		// all the children when repositioning.  The slide controller will deactivate the objects not in view next frame
		for (int i = 0; i < allFriendsCards.Count; ++i)
		{
			if (allFriendsCards[i] != null && allFriendsCards[i].gameObject != null)
			{
				allFriendsCards[i].gameObject.SetActive(true);
			}
		}
		setSlideBounds(cardGrid.hideInactive);
		cardGrid.RepositionTweened();
	}

	private void onRepositionFinished()
	{
		for (int i = 0; i < allFriendsCards.Count; i++)
		{
			if (allFriendsCards[i] == null || allFriendsCards[i].member == null)
			{
				Debug.LogError("invalid friend card at position " + i);
				continue;
			}
			if (NetworkFriends.instance.isNewFriend(allFriendsCards[i].member.zId))
			{
				allFriendsCards[i].setContentMovedDelegate(slideController, newMarkerTransform);
			}
		}
	}

	private void scrollToUser()
	{
		float offset = cardGrid.cellWidth / 2;
		if (allFriendsCards != null && allFriendsCards.Count > 0)
		{
			for (int i = 0; i < allFriendsCards.Count; i++)
			{
				if (allFriendsCards[i].isSlotsPlayer)
				{
					slideController.safleySetXLocation(-1 * ((cardGrid.cellWidth * i) + offset));
					break;
				}
			}
		}
	}

	// Remove friends from teh all friends panel
	private void removeOldFriends(Dictionary<string, SocialMember> newFriends)
	{
		if (null == allFriendsCards)
		{
			return;
		}

		List<FriendCard> toRemove = new List<FriendCard>();
		int index = 0;
		for(index=0; index<allFriendsCards.Count; ++index)
		{
			if (!newFriends.ContainsKey(allFriendsCards[index].member.zId))
			{
				SafeSet.gameObjectActive(allFriendsCards[index].gameObject, false);
				friendCardObjects.Enqueue(allFriendsCards[index]);
				toRemove.Add(allFriendsCards[index]);
			}
		}

		for(index=0; index<toRemove.Count; ++index)
		{
			allFriendsCards.Remove(toRemove[index]);
		}
	}

	private void removeAllFriends()
	{
		if (null == allFriendsCards)
		{
			return;
		}

		for(int i=0; i<allFriendsCards.Count; ++i)
		{
			if (allFriendsCards[i] == null)
			{
				continue;
			}
			SafeSet.gameObjectActive(allFriendsCards[i].gameObject, false);
			friendCardObjects.Enqueue(allFriendsCards[i]);
		}
		allFriendsCards.Clear();
	}

	// Function to add new friends.  Only adds to the panel if the users pass the current filter
	private void addNewFriends(List<SocialMember> allFriends, Dictionary<string, FriendCard> oldFriends)
	{
		if (null == allFriends || null == oldFriends)
		{
			return;
		}

		int numNew = 0;
		allFriends.Sort(SocialMember.sortCreditsReverse);
		
		for (int i = 0; i < allFriends.Count; i++)
		{
			if (null == allFriends[i])
			{
				continue;
			}

			string zid = allFriends[i].zId;
			if (NetworkFriends.instance.isNewFriend(zid))
			{
				numNew++;
			}
			if (!oldFriends.ContainsKey(zid))
			{
				FriendCard card = getFriendCard(allFriends[i], i, i+1);
				if (allFriendsCards == null)
				{
					allFriendsCards = new List<FriendCard>();
				}
				allFriendsCards.Add(card);
			}
			else
			{
				//rename card for new index
				oldFriends[zid].gameObject.name = "All Friends Card " + i.ToString();
			}
		}

		if (allFriends.Count == SocialMember.allFriends.Count)
		{
			/*
			MCC -- Make sure to only do this check we are going over the all friends list.
			We dont want to force a recalculate if we are using a filtered list.
			While the PMs have removed the filtering functionality for the moment, we still
			support it in code and thus should make sure we support that mode being active and not recalc
			every time we filter.
			*/
			if (numNew != NetworkFriends.instance.newFriends)
			{
				Debug.LogErrorFormat("FriendsTabAllFriends.cs -- addNewFriends -- mismatched numbers of new friends, forcing  a recalculate.");
				NetworkFriends.instance.recalculateNewCounts();
			}
		}
	}

	// Create a new friend card to be displayed.  This function wil first attempt to grab an object from the pool before
	// creating a new object
	private FriendCard instantiateFriendCard(SocialMember member, int friendIndex, int friendRank)
	{
		GameObject cardObject = CommonGameObject.instantiate(cardPrefab, cardGrid.transform) as GameObject;
		if (cardObject == null)
		{
			Debug.LogErrorFormat("FriendsTabAllFriends.cs -- init -- instantiated a null object :(");
			return null;
		}
		cardObject.SetActive(false);
		// Rename so we keep it sorted.
		cardObject.name = "All Friends Card " + friendIndex;
		FriendCard newCard = cardObject.GetComponent<FriendCard>();
		if (newCard != null)
		{
			onCardCreated(member, newCard, friendRank);
		}
		else
		{
			// Otherwise don't add it to any lists and destroy it.
			Debug.LogErrorFormat("FriendsTabAllFriends.cs -- init -- could not find a FriendCard on the newly created object.");
			Destroy(cardObject);
		}

		return newCard;
	}

	private void onCardCreated(SocialMember member, FriendCard newCard, int friendRank = 0)
	{
		bool isNew = NetworkFriends.instance.isNewFriend(member.zId);
		newCard.init(member, FriendCard.CardType.FRIEND, isNew, false, null, slideController, newMarkerTransform, rank:friendRank);
		newCard.gameObject.SetActive(true); // Now turn it on.
		tmProMasker.addObjectArrayToList(newCard.allLabels);
	}

	private int getNumVisibleCards()
	{
		int count = 0;
		if (allFriendsCards != null)
		{
			for (int i = 0; i < allFriendsCards.Count; ++i)
			{
				if (allFriendsCards[i] != null && allFriendsCards[i].gameObject.activeSelf)
				{
					++count;
				}
			}

		}

		return count;
	}
	
	private void setSlideBounds(bool ignoreInactive)
	{
		slideController.content.width = 0;
		int count = ignoreInactive ? getNumVisibleCards() : allFriendsCards.Count;
		if (count > DEFAULT_ITEMS_VISIBLE)
		{
			slideController.setBounds(-1000 - ((count - DEFAULT_ITEMS_VISIBLE) * cardGrid.cellWidth), slideController.rightBound);
		}
		else
		{
			slideController.setBounds(-1000, slideController.rightBound);
		}
	}

	private void friendRemoved(SocialMember member, bool didSucceed, int errorCode)
	{
		if (didSucceed && allFriendsCards != null)
		{
		    FriendCard foundCard = null;
			for (int i = 0; i < allFriendsCards.Count; i++)
			{
				if (allFriendsCards[i].member != null && allFriendsCards[i].member == member)
				{
					foundCard = allFriendsCards[i];
					break;
				}
			}
			if (foundCard != null && foundCard.gameObject != null)
			{
				// If we found the member to be removed, then kill it
				SafeSet.gameObjectActive(foundCard.gameObject, false);
				friendCardObjects.Enqueue(foundCard);
				allFriendsCards.Remove(foundCard);

				//redo the tab labels so we don't skip a number
				for (int i = 0; i < allFriendsCards.Count; ++i)
				{
					if (allFriendsCards[i] == null)
					{
						continue;
					}
					allFriendsCards[i].updateTabText(i+1);
				}
			}
		}

		repositionAllFriends();
	}
	
	private void friendAdded(SocialMember member, bool didSucceed, int errorCode)
	{
		if (didSucceed && allFriendsCards != null)
		{
			if (doesPassFilter(member.fullName.ToLower().Trim(), searchFilter))
			{
				FriendCard newCard = getFriendCard(member, allFriendsCards.Count, allFriendsCards.Count+1);
				if (null == allFriendsCards)
				{
					allFriendsCards = new List<FriendCard>();
				}
				allFriendsCards.Add(newCard);
				if (newCard != null)
				{	
					repositionAllFriends();
				}
			}
			
		}
	}
	
	private static  bool doesPassFilter(string text, string filter)
	{
		if (string.IsNullOrEmpty(filter))
		{
			return true;
		}
		else if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		
		int filterIndex = 0;
		for(int i=0; i<text.Length; ++i)
		{
			if (filterIndex >= filter.Length)
			{
				return false;
			}

			if (text[i] == filter[filterIndex])
			{
				++filterIndex;
				if (filterIndex >= filter.Length)
				{
					return true;
				}
			}
		}

		return false;
	}

	public void show()
	{
		if (allFriendsCards == null || allFriendsCards.Count <= DEFAULT_ITEMS_VISIBLE)
		{
			slideController.scrollBar.gameObject.SetActive(false);
		}
		else
		{
			slideController.scrollBar.gameObject.SetActive(true);
		}
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "friends",
			phylum: "friends_tab",
			klass: "all_friends",
			family: "",
			genus: "view");
		
		if (allFriendsCards != null)
		{
			slideController.content.transform.localPosition = originalContentPosition;
			NetworkFriendsTab.playCardAnimations(allFriendsCards, true);
		}
		slideController.addMomentum(0.001f); // Add a tiny momemntum to fire off the content moved event right away.
		scrollToUser();
	}

	public void hide()
	{
		if (allFriendsCards != null)
		{
			NetworkFriendsTab.playCardAnimations(allFriendsCards, false);
		}
	}

	// Get a list of friends that have the input string in their full name text
	private List<SocialMember> filterFriends(string input)
	{
		// return all friends if the input is empty
		if (string.IsNullOrEmpty(input))
		{
			searchFilter = "";
			filteredResults = SocialMember.allFriends;
			return SocialMember.allFriends;
		}

		input = input.ToLower().Trim(); //remove empty space

		List<SocialMember> newFriends = new List<SocialMember>();
		if (null == filteredResults || resetFilterResults)
		{
			filteredResults = SocialMember.allFriends;
			resetFilterResults = false;
		}

		for(int i=0; i<filteredResults.Count; ++i)
		{
			SocialMember member = filteredResults[i];
			if (member == null)
			{
				continue;
			}

			//look for the text anywhere in the users full name
			if (doesPassFilter(member.fullName.ToLower().Trim(), input))
			{
				newFriends.Add(member);
			}
		}

		filteredResults = newFriends;

		return newFriends;
	}

	// Function called each time the search input is changed.  Does a fuzzy search on the name of the 
	// friends available
	private void OnInputChanged(UIInput input)
	{
		
		// Disable the submit button if the friend code is invalid;
		bool isEmptyText = string.IsNullOrEmpty(input.text);
		searchXParent.SetActive(!isEmptyText);

		int newLength = null == input.text ? 0 : input.text.Length;

		//reset the filter and search all friends if we've removed a character from the text (ie expanded our search)
		if (searchFilter != null && newLength < searchFilter.Length)
		{
			resetFilterResults = true;
		}
		searchFilter = input.text;

		List<SocialMember> newFriends = filterFriends(input.text);
		reset(newFriends);
	}
	
	private void clearSearchClicked(Dict args = null)
	{
		// Clear the text
	    searchInput.text = "";
		searchXParent.SetActive(false);

		//reset
		searchFilter = "";
		resetFilterResults = false;
		reset(SocialMember.allFriends);
		
	}

	void OnDestroy()
	{
		unregisterEventDelegates();
		cardGrid.onRepositionFinished -= onRepositionFinished;
		slideController.onContentMoved -= onContentMoved;
		clearTextButton.unregisterEventDelegate(clearSearchClicked);

	}	
}
