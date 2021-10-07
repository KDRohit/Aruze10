using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class FriendsTabFindFriends : MonoBehaviour
{
	// Searching
	[SerializeField] private UIInput searchInput;
	[SerializeField] private ClickHandler clearTextButton;
	[SerializeField] private ImageButtonHandler backButton;
	[SerializeField] private ImageButtonHandler submitButton;
	[SerializeField] private GameObject shroud;
	[SerializeField] private GameObject playerFoundCardAnchor;
	[SerializeField] private GameObject playerFoundParent;
	[SerializeField] private GameObject playerNotFoundParent;
	[SerializeField] private GameObject searchParent;
	[SerializeField] private TextMeshPro searchFoundLabel;
	[SerializeField] private GameObject	allRequestSeenLabel;

	[SerializeField] private GameObject searchXParent;
	[SerializeField] private GameObject searchLoadingParent;
	[SerializeField] private GameObject invalidInputWarningLabel;

	// Suggested
	[SerializeField] private UIGrid cardGrid;
	[SerializeField] private GameObject cardPrefab;
	[SerializeField] private GameObject findMoreFriendsCardPrefab;
	[SerializeField] private TextMeshProMasker tmProMasker;
	[SerializeField] private SlideController slideController;
	[SerializeField] private TextMeshPro searchingLabel;

	private List<FriendCard> suggestedCards;
	private Vector3 originalContentPosition;
	private bool isFindingMoreFriends = false; // Used for the searching... label coroutine.
	private bool isActive = false;
	private bool didTimeout = false;
	private bool isSearching = false;
	private FriendCard findMoreFriendsCard = null;

	private const float CARD_INTRO_DELAY = 0.1f;
	private const int VISIBLE_CARDS = 5;
	private const float SEARCHING_DOT_TIME = 0.5f;
	private const float SEARCH_TIMEOUT = 5.0f;
	private const float SLIDE_CONTENT_LEFT_POSITION = -730f;
	private const float CONTENT_TWEEN_TIME = 0.5f;

	private void Awake()
	{
		originalContentPosition = slideController.content.transform.localPosition;
	}

	public void init()
	{
		// Setup the Seach Input Area stuff.
		searchInput.defaultText = Localize.text("tap_here_to_enter_friend_code", "");
		clearTextButton.registerEventDelegate(clearSearchClicked);
		submitButton.registerEventDelegate(submitClicked);
		backButton.registerEventDelegate(backClicked);
		searchParent.SetActive(false); // Default this to off.
		searchLoadingParent.SetActive(false);
		allRequestSeenLabel.SetActive(false);

		// Setup the suggested friends.
		CommonGameObject.destroyChildren(cardGrid.gameObject);
		suggestedCards = new List<FriendCard>();

		// Turn on the searching content.
		isFindingMoreFriends = true;
		StartCoroutine(findingMoreFriendsRoutine());

		// Send up a server event to grab more suggested friends.
		NetworkFriends.instance.findSuggestions(showSuggestions);
		
		searchingLabel.gameObject.SetActive(false);
		searchXParent.SetActive(false); // Turn this off by default.

		// Register for events.
		NetworkFriends.instance.onFriendBlocked += removeFromSuggestions;
		NetworkFriends.instance.onNewFriend += removeFromSuggestions;
		NetworkFriends.instance.onInviteAccepted += removeFromSuggestions;
		NetworkFriends.instance.onInviteFriend += removeFromSuggestions;
	}

	public void show()
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "friends",
			phylum: "friends_tab",
			klass: "find_new_friends",
			family: "",
			genus: "view");

		isActive = true;
		// Reset position when we show the tab.
		originalContentPosition = slideController.content.transform.localPosition;
		if (suggestedCards != null)
		{
			searchingLabel.gameObject.SetActive(false);
			slideController.content.transform.localPosition = originalContentPosition;
			NetworkFriendsTab.playCardAnimations(suggestedCards, true);
		}
	}

	public void hide()
	{
		isActive = false;
		if (suggestedCards != null)
		{
			NetworkFriendsTab.playCardAnimations(suggestedCards, false);
		}
	}

	private void setupSuggestions(List<SocialMember> suggestions)
	{
		if (this == null || gameObject == null)
		{
			// If either of these are null then dialog is being destroyed so we can just bail.
			return;
		}
		else if (gameObject.activeInHierarchy)
		{
			// If the object is active in hierarchy then we can use the game object to run the coroutine.
			StartCoroutine(setupSuggestionsRoutine(suggestions));
		}
		else
		{
			// Otherwise if the gameobject is valid, but inactive, then we should use routine runner.
			RoutineRunner.instance.StartCoroutine(setupSuggestionsRoutine(suggestions));
		}
	}
	
	private IEnumerator setupSuggestionsRoutine(List<SocialMember> suggestions)
	{
		if (this == null || gameObject == null || cardGrid == null)
		{
			// Then we are probably closing the dialog and don't want to NRE.
			yield break;
		}
		//MCC -- HIR-73025 -- Removing the code that bails if there are no suggestions since we always create at least one now.
		cardGrid.gameObject.SetActive(true);
		allRequestSeenLabel.SetActive(false);

		suggestedCards.Clear(); // Clear out the list becuase we can have multiple sets of suggestions.

		// BY 02/2020: We have removed facebook invites to non friends (at least temporarily)
		//add facebook invite, or find void OnMouseUpAsButton()
		/*if (SlotsPlayer.isFacebookUser)
		{
			instantiateSuggestedCard("FB Invite Card", null, FriendCard.CardType.FB_INVITE, false, false, null);
		}*/

		if (!SlotsPlayer.isFacebookUser)
		{
			instantiateSuggestedCard("FB Connect Card", null, FriendCard.CardType.FB_CONNECT, false, false, null);
		}


		//add friend code card
		instantiateSuggestedCard("Friend Code Card", null, FriendCard.CardType.FRIEND_CODE, false, false, null);
	
		for (int i = 0; i < suggestions.Count; i++)
		{
			instantiateSuggestedCard("Find Friends Card " + i.ToString(), suggestions[i], FriendCard.CardType.ADD_FRIEND, false, false, addFriendClicked, i+1);
		}

		if (findMoreFriendsCardPrefab != null)
		{
			findMoreFriendsCard = instantiateSuggestedCard( "Find More Friends Card", null, FriendCard.CardType.FIND_MORE, false, false, findMoreClicked);
		}
		yield return null; // Wait for one Update here to make sure everything got deleted.
		repositionGrid(false);
		if (isActive)
		{
			// If this tab is currently active then run the routine to turn them on.
			NetworkFriendsTab.playCardAnimations(suggestedCards, true);
		}
		setSlideBounds();
	}

	private FriendCard instantiateSuggestedCard(string cardName, SocialMember member, FriendCard.CardType cardType, bool isNew, bool isSearchResult, FriendCard.clickDelegate onClickDelegate, int friendRank = 0)
	{
		// Instantiate a card for each player under the cardGrid.
		GameObject cardObject = GameObject.Instantiate(cardPrefab, cardGrid.transform);
		if (cardObject == null)
		{
			Debug.LogErrorFormat("FriendsTabFindFriends.cs -- setupSuggestions -- instantiated a null object :(");
			return null;
		}
		// Rename so we keep it sorted.
		cardObject.name = cardName;
		cardObject.SetActive(false); // Turn it off so that we can let Awake() turn on their animation.
		FriendCard newCard = cardObject.GetComponent<FriendCard>();
		if (newCard != null)
		{
			newCard.init(member, cardType, isNew, isSearchResult, onClickDelegate, slideController, rank:friendRank);
			tmProMasker.addObjectArrayToList(newCard.allLabels);
			suggestedCards.Add(newCard);
		}
		else
		{
			// Otherwise don't add it to any lists and destroy it.
			Debug.LogErrorFormat("FriendsTabAllFriends.cs -- setupSuggestions -- could not find a FriendCard on the newly created object.");
			Destroy(cardObject);
		}

		return newCard;
	}


	// Callback for when the search comes back from the server.
	private void searchCallback(JSON data)
	{
		isSearching = false;
		Server.unregisterEventDelegate("search_by_friend_code", searchCallback);
		if (didTimeout)
		{
			// If we timed-out, then we already showed the failed parent.
			// So just bail here.
			Debug.LogErrorFormat("FriendsTabFindFriends.cs -- searchCallback -- search returned, but we had already timed out.");
			return;
		}
		searchXParent.SetActive(true);
		searchLoadingParent.SetActive(false);

		bool didSucceed = null != data && data.getBool("success", false);
		if (didSucceed)
		{
			// Grab the data from the json blob.
			playerNotFoundParent.SetActive(false);
			playerFoundParent.SetActive(true);
			string zid = data.getString("profile.zid", "notyet");
			string name = data.getString("profile.name", "");
			long achievementScore = data.getLong("profile.achievement_score", 0);
			int vipLevel = data.getInt("profile.vip_level", 0);
			string photo = data.getString("profile.photo_url", "");

			if (zid == "notyet")
			{
				// Just a safety check for bad data.
				Debug.LogErrorFormat("FriendsTabFindFriends.cs -- searchCallback -- all IDs came back as null even though success was true, something is wrong here. The friend code was {0}", searchInput.text);
				// Something went wrong lets show the player not found for a better flow.
				searchFailed();
				return;
			}

			SocialMember foundMember = CommonSocial.findOrCreate(
				fbid:"",
				zid:zid,
				firstName:name,
				lastName:"",
				achievementScore:achievementScore,
				vipLevel:vipLevel,
				imageUrl:photo);

			if (foundMember.isNetworkFriend)
			{
				searchFoundLabel.text = Localize.text("player_found_already_friends_{0}", foundMember.firstNameLastInitial);
			}
			else
			{
				searchFoundLabel.text = Localize.text("player_found_lets_send_request", "");
			}
			foundMember.setUpdated();

			// Create the card
			FriendCard foundPlayerCard;

			GameObject newCardObject = GameObject.Instantiate(cardPrefab, playerFoundCardAnchor.transform);
			if (newCardObject == null)
			{
				Debug.LogErrorFormat("FriendsTabFindFriends.cs -- searchCallback -- could not instantiate the prefab as an object. The friend code was {0}", searchInput.text);
				// Something went wrong lets show the player not found for a better flow.
				playerNotFoundParent.SetActive(true);
				playerFoundParent.SetActive(false);
				return;
			}

			foundPlayerCard = newCardObject.GetComponent<FriendCard>();
			if (foundPlayerCard == null)
			{
				Debug.LogErrorFormat("FriendsTabFindFriends.cs -- searchCallback -- did not find the FriendCard script on the created object. The friend code was {0}", searchInput.text);
				// Something went wrong lets show the player not found for a better flow.
				playerNotFoundParent.SetActive(true);
				playerFoundParent.SetActive(false);
				return;
			}

			FriendCard.CardType cardType = FriendCard.CardType.ADD_FRIEND;
			if (foundMember.isNetworkFriend)
			{
				cardType = FriendCard.CardType.FRIEND;
			}
			else if (foundMember.isInvited)
			{
				cardType = FriendCard.CardType.INVITED;
			}
			else if (foundMember.hasInvitedPlayer)
			{
				cardType = FriendCard.CardType.REQUESTED;
			}
			else if (foundMember.isBlocked)
			{
				cardType = FriendCard.CardType.BLOCKED;
			}
			foundPlayerCard.init(foundMember, cardType, false, true, searchFriendClicked, slideController);

			submitButton.gameObject.SetActive(false); // Turn this off if we found someone.
		}
		else
		{
			searchFailed();
		}
	}

	private void addFriendClicked(FriendCard card)
	{
		// The card has animated already, so destroy destroy it and reposition.
		StartCoroutine(destroyAndReposition(card));
	}

	private IEnumerator destroyAndReposition(FriendCard card)
	{
		if (suggestedCards.Contains(card))
		{
			suggestedCards.Remove(card);
			Destroy(card.gameObject);
			RoutineRunner.instance.StartCoroutine(repositionAfterFrame(cardGrid));
		}
		else
		{
			// Depending on when the server actions come down and in what order,
			// the code to handle adding a friend from another dialog can have alreayd deleted this.
			// If that card doesn't exist anymore, then dont do anything.
			yield break;
		}
	}

	private void searchFriendClicked(FriendCard card)
	{
		// Once we have sent a request lets close the search mode.
		searchParent.SetActive(false);
		CommonGameObject.destroyChildren(playerFoundCardAnchor.gameObject);

		searchInput.text = ""; // Clear the search input.
		searchXParent.SetActive(false); // Turn off the X
	}

	private void findMoreClicked(FriendCard card)
	{
		// Kill ALL the children and reset the bounds/position.
		CommonGameObject.destroyChildren(cardGrid.gameObject);
		setSlideBounds();
		slideController.content.transform.localPosition = originalContentPosition;

		// Turn on the searching content.
		isFindingMoreFriends = true;
		StartCoroutine(findingMoreFriendsRoutine());

		// Send up a server event to grab more suggested friends.
		NetworkFriends.instance.findSuggestions(showSuggestions);
	}

	// Callback from the server when requesting new suggesions.
	private void showSuggestions(List<SocialMember> newSuggestions)
	{
		isFindingMoreFriends = false; // We are done searching so set this to false to end the coroutine.
		if (null != searchingLabel && searchingLabel.gameObject != null)
		{
			// null check this because the friend suggestion call can return after user has closed dialog.
			searchingLabel.gameObject.SetActive(false); // Turn off this label now.
		}
		setupSuggestions(newSuggestions);
		setSlideBounds();
	}

	private bool isValidFriendCode(string inputText)
	{
		string text = inputText.ToUpper();
		// Friend-code length is always 8
		if (text.Length != 8)
		{
			return false;
		}


		for (int i = 0; i < text.Length; i++)
		{
			//test against vowels
			switch(text[i])
			{
				//case 'A':	 //micmurphy -- my friend code had an 'A' in it...
				case 'E':
				case 'I':
				case 'O':
				case 'U':
					return false;
			}
		}
		// If we got here then it is a valid string.
		return true;
	}

	private void showSearchOverlay()
	{
		// Reset the search overlay and show it here.
		searchXParent.SetActive(!string.IsNullOrEmpty(searchInput.text));
		playerNotFoundParent.SetActive(false);
		playerFoundParent.SetActive(false);
		submitButton.gameObject.SetActive(true); // Make sure the submit button is active.
		searchParent.SetActive(true);
	}

	private IEnumerator findingMoreFriendsRoutine()
	{
		searchingLabel.gameObject.SetActive(true);
		int count = 0;
		string labelBaseText = Localize.textTitle("searching", "");
		string dots = "";
		while (isFindingMoreFriends)
		{
			dots = "";
			for (int i = 0; i < count %3 ; i++)
			{
				dots += ".";
			}
			searchingLabel.text = labelBaseText + dots;
			count++;
			yield return new WaitForSeconds(SEARCHING_DOT_TIME);
		}
	}

	private void setSlideBounds()
	{
		// MCC Adding 10f to either side of the bounds to not get locked on startup.
		float rightBound = cardGrid.cellWidth * (suggestedCards.Count - 1.5f);
		float leftBound = -cardGrid.cellWidth * (suggestedCards.Count - 2.5f);
		slideController.setBounds(leftBound, rightBound);
		slideController.content.width = cardGrid.cellWidth * suggestedCards.Count;
	}

	private void repositionGrid(bool shouldTween)
	{
		if (this == null || cardGrid == null)
		{
			return;
		}
		// First reposition the cards.
		if (shouldTween)
		{
			cardGrid.RepositionTweened();
		}
		else
		{
			cardGrid.Reposition();
		}
		// If the slide content is all on screen (and thus not slidable)
		// then we should make sure the content is left-aligned.
		if (suggestedCards != null &&
			suggestedCards.Count < VISIBLE_CARDS &&
			slideController != null &&
			slideController.content != null &&
			slideController.content.transform != null)
		{
			if (shouldTween)
			{
				Transform contentTransform = slideController.content.transform;
				Vector3 newPosition = new Vector3(SLIDE_CONTENT_LEFT_POSITION, contentTransform.localPosition.y, contentTransform.localPosition.z);
				iTween.MoveTo(contentTransform.gameObject, iTween.Hash(
					"x", newPosition.x,
					"y", newPosition.y,
					"z", newPosition.z,
					"time", CONTENT_TWEEN_TIME,
					"islocal", true,
					"easetype", iTween.EaseType.linear));
			}
			else
			{
				CommonTransform.setX(slideController.content.transform, SLIDE_CONTENT_LEFT_POSITION);
			}
		}
	}
	
	private IEnumerator searchRoutine()
	{
		isSearching = true;
		didTimeout = false;
		float duration = 0.0f;
		while (isSearching)
		{
			duration += Time.deltaTime;
			if (duration > SEARCH_TIMEOUT)
			{
				searchFailed();
				didTimeout = true;
				yield break;
			}
			yield return null;
		}
	}

	private void searchFailed()
	{
		// Turn on the search failed content.
		playerNotFoundParent.SetActive(true);
		playerFoundParent.SetActive(false);

		searchLoadingParent.SetActive(false);
		searchXParent.SetActive(true); // Turn this back on.
	}

	private void removeFromSuggestions(SocialMember member, bool didSucceed, int errorCode)
	{
		if (this == null || gameObject == null)
		{
			// Weird state after dialog is closed, bail.
			return;
		}
		
		if (didSucceed && member != null && suggestedCards != null)
		{
			int removeIndex = -1;
			for (int i = 0; i < suggestedCards.Count; i++)
			{
				//see if the cards hasn't been destroyed already (possible if the dialog is being closed while the event comes in)
				if (suggestedCards[i] == null || suggestedCards[i].gameObject == null)
				{
					continue;
				}

				if (suggestedCards[i].member == member)
				{
					// If we added/blocked this player from somewhere else while this was being displayed,
					// we should remove them from the displayed cards.
					Destroy(suggestedCards[i].gameObject);
					removeIndex = i;
					break;
				}
			}
			if (removeIndex >= 0)
			{
				suggestedCards.RemoveAt(removeIndex);
			}
		}
		RoutineRunner.instance.StartCoroutine(repositionAfterFrame(cardGrid));
	}

	private IEnumerator repositionAfterFrame(UIGrid grid)
	{
		yield return null; // Wait for a frame to let the object get deleted/added fully.
		
		if (grid == null || grid.gameObject == null || slideController == null || slideController.scrollBar == null)
		{
			// Dont NRE here.
			yield break;
		}

		repositionGrid(grid.gameObject.activeInHierarchy);
		setSlideBounds();
		if (slideController.scrollBar.scrollValue > 0.99f)
		{
			slideController.scrollBar.onChange.Invoke(slideController.scrollBar);
		}
		
	}
	
	private void OnDestroy()
	{
		// De-register
		NetworkFriends.instance.onFriendBlocked -= removeFromSuggestions;
		NetworkFriends.instance.onNewFriend -= removeFromSuggestions;
		NetworkFriends.instance.onInviteAccepted -= removeFromSuggestions;
		NetworkFriends.instance.onInviteFriend -= removeFromSuggestions;
	}
#region NGUI_CALLBACKS
	private void OnShowKeyboard(UIInput input)
	{
		// Enter search view.
		showSearchOverlay();
		invalidInputWarningLabel.SetActive(false);
	}

	private void OnHideKeyboard(UIInput input)
	{
		// Do nothing here, we will only close the search view manually.
	}

	private void OnSubmit(string text)
	{
		submitClicked();
	}

	private void OnInputChanged(UIInput input)
	{
		// Disable the submit button if the friend code is invalid;
		searchXParent.SetActive(!string.IsNullOrEmpty(input.text));
		invalidInputWarningLabel.SetActive(!string.IsNullOrEmpty(input.text) && (input.text.Length < 8));
	}

	private void clearSearchClicked(Dict args = null)
	{
		// Clear the text
		searchInput.text = "";
		searchXParent.SetActive(false);

		// Turn off the search results parents.
		playerFoundParent.SetActive(false);
		playerNotFoundParent.SetActive(false);
		invalidInputWarningLabel.SetActive(false);
		CommonGameObject.destroyChildren(playerFoundCardAnchor.gameObject); // Destroy the card if one is there.
	}

	private void submitClicked(Dict args = null)
	{
		if (isSearching)
		{
			// Dont do anything if they hit the button and we still have a request in flight.
			return;
		}

		// Make sure we destroy an existing card that might be there from a previous search.
		CommonGameObject.destroyChildren(playerFoundCardAnchor.gameObject); // Destroy the card if one is there.


		// Send up server action for that friend code.
		// Show some sort of loading thingy while waiting for the response.
		string friendCode = searchInput.text;
		friendCode = friendCode.ToUpper();

		if (isValidFriendCode(friendCode))
		{
			// Turn off both results parents.
			playerNotFoundParent.SetActive(false);
			playerFoundParent.SetActive(false);

			//set search true
			searchXParent.SetActive(false);
			searchLoadingParent.SetActive(true);

			StartCoroutine(searchRoutine());
			// Call find friend action with this friendCode.
			NetworkFriendsAction.findFriendFromCode(friendCode, searchCallback);
		}
		else
		{
			// Turn on the search failed content.
			playerNotFoundParent.SetActive(true);
			playerFoundParent.SetActive(false);
		}
	}

	private void backClicked(Dict args = null)
	{
		// Clear the text
		searchInput.text = "";
		searchInput.OnSelect(false);
		searchParent.SetActive(false);
	}
#endregion
}
