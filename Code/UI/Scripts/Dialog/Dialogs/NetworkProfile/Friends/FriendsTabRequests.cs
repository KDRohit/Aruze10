using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class FriendsTabRequests : MonoBehaviour
{
	[SerializeField] private TextMeshPro pendingRequestBadgeLabel;
	[SerializeField] private GameObject pendingRequestBadge;
	[SerializeField] private GameObject noPendingRequestsParent;
	[SerializeField] private GameObject hasPendingRequestsParent;

	[SerializeField] private UIGrid cardGrid;
	[SerializeField] private GameObject cardPrefab;
	[SerializeField] private SlideController slideController;
	[SerializeField] private TextMeshProMasker tmProMasker;
	[SerializeField] private Transform newMarkerTransform;

    private List<FriendCard> requestsCards;
	private Vector3 originalContentPosition;

	private const float CARD_INTRO_DELAY = 0.1f;
	private const float MINIMUM_CARD_TO_SCROLL = 4;
	private void Awake()
	{
	    originalContentPosition = slideController.content.transform.localPosition;
	}

	public void init()
	{
		pendingRequestBadge.SetActive(NetworkFriends.instance.hasNewFriendRequests);
		pendingRequestBadgeLabel.text = NetworkFriends.instance.newFriendRequests.ToString();

		GameObject cardObject;
		CommonGameObject.destroyChildren(cardGrid.gameObject);
	    requestsCards = new List<FriendCard>();
		FriendCard newCard;
		bool isNew = false;

		int numNew = 0;
		if (SocialMember.invitedByPlayers != null && SocialMember.invitedByPlayers.Count > 0)
		{
			noPendingRequestsParent.SetActive(false);
			hasPendingRequestsParent.SetActive(true);

			for (int i = 0; i < SocialMember.invitedByPlayers.Count; i++)
			{
				// Instantiate a card for each player under the cardGrid.
				cardObject = GameObject.Instantiate(cardPrefab, cardGrid.transform);
				if (cardObject == null)
				{
					Debug.LogErrorFormat("FriendsTabRequests.cs -- init -- instantiated a null object :(");
				}
				// Rename so we keep it sorted.
				cardObject.name = "Requests Card " + i.ToString();
				cardObject.SetActive(false);
				newCard = cardObject.GetComponent<FriendCard>();
				if (newCard != null)
				{
					isNew = NetworkFriends.instance.isNewFriendRequest(SocialMember.invitedByPlayers[i].zId);
					if (isNew)
					{
						numNew++;
					}
					newCard.init(SocialMember.invitedByPlayers[i], FriendCard.CardType.REQUESTED, isNew, false, onCardClicked, slideController, newMarkerTransform, i+1);
					requestsCards.Add(newCard);
					tmProMasker.addObjectArrayToList(newCard.allLabels);
					if (isNew)
					{
						newCard.setContentMovedDelegate(slideController, newMarkerTransform);
					}
				}
				else
				{
					// Otherwise don't add it to any lists and destroy it.
					Debug.LogErrorFormat("FriendsTabRequests.cs -- init -- could not find a FriendCard on the newly created object.");
					Destroy(cardObject);
				}
			}
			setSlideBounds();
		}
		else
		{
			noPendingRequestsParent.SetActive(true);
			hasPendingRequestsParent.SetActive(false);
		}

		NetworkFriends.instance.onInviteWithdrawn += inviteRemoved;
		NetworkFriends.instance.onInviteCancelled += inviteRemoved;
		NetworkFriends.instance.onInviteReceived += inviteAdded;
		NetworkFriends.instance.onNewRequestCountUpdated += updateBadgeLabel;

		// MCC Adding in this error log so we know if this is happening on production.
		if (numNew != NetworkFriends.instance.newFriendRequests)
		{
			Debug.LogErrorFormat("FriendsTabRequests.cs -- init -- mismatched numbers of friend requests. Forcing a recalculate.");
			NetworkFriends.instance.recalculateNewCounts();
		}
		
		slideController.enabled = requestsCards.Count > MINIMUM_CARD_TO_SCROLL;
	}

    private void updateBadgeLabel()
	{
		pendingRequestBadge.SetActive(NetworkFriends.instance.hasNewFriendRequests);
		pendingRequestBadgeLabel.text = NetworkFriends.instance.newFriendRequests.ToString();		
	}
	
	public void show()
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "friends",
			phylum: "friends_tab",
			klass: "friend_requests",
			family: "",
			genus: "view");
		
		if (requestsCards != null)
		{
			slideController.content.transform.localPosition = originalContentPosition;
			NetworkFriendsTab.playCardAnimations(requestsCards, true);
		}
	}
	
	public void hide()
	{
		if (requestsCards != null)
		{
			NetworkFriendsTab.playCardAnimations(requestsCards, false);
		}
	}

	private void setSlideBounds()
	{
		slideController.content.width = cardGrid.cellWidth * requestsCards.Count;
		// Adding a buffer to each bound to give a little buffer so it doesn't start locked.
		float rightBound = cardGrid.cellWidth * (requestsCards.Count - 1.5f);
		float leftBound = -cardGrid.cellWidth * (requestsCards.Count - 2.5f);
		slideController.setBounds(leftBound, rightBound);
	}
	
	private void onCardClicked(FriendCard card)
	{
		// We dont care if it was reject or added here, we just want to handle the tweening.
		StartCoroutine(destroyAndReposition(card));

		if (SlotsPlayer.instance.creditSendLimit <= 0)
		{
			for (int i = 0; i < requestsCards.Count; ++i)
			{
				requestsCards[i].toggleSendGiftButton(false, "limit_reached", "LIMIT REACHED");
			}
		}
	}

	private IEnumerator destroyAndReposition(FriendCard card)
	{
	    requestsCards.Remove(card);
	    Destroy(card.gameObject);
		yield return null; // Wait for a frame to let the object get deleted fully.
	    cardGrid.RepositionTweened();
	}

	private void inviteRemoved(SocialMember member, bool didSucceed, int errorCode)
	{
		if (didSucceed && requestsCards != null)
		{
			List<int> toRemove = new List<int>();
			for (int i = 0; i < requestsCards.Count; i++)
			{
				if(requestsCards[i].member == member)
				{
					// If we found the member to be removed, then kill it.
				    Destroy(requestsCards[i].gameObject);
					if (requestsCards.Count == 0)
					{
						noPendingRequestsParent.SetActive(true);
						hasPendingRequestsParent.SetActive(false);
					}
					// MCC -- HIR-72344 -- removing the return so that if we somehow have created more than one with race conditions they all get removed.
					toRemove.Add(i);
				}
			}
			// MCC -- HIR-72344 -- Removing the cards from the list now since we could possibly have more than one.
			for (int i = 0; i < toRemove.Count; i++)
			{
				if (i >= 0 && i < requestsCards.Count)
				{
					// Add bounds checking just in case.
					requestsCards.RemoveAt(toRemove[i]);
				}
			}
		}
		cardGrid.RepositionTweened();
	}

	private void inviteAdded(SocialMember member, bool didSucceed, int errorCode)
	{
		if (didSucceed && requestsCards != null)
		{
			for (int i = 0; i < requestsCards.Count; i++)
			{
				if(requestsCards[i].member == member)
				{
					// If we found this member already in the created cards then we already created a card for that person so
					// lets get out of here.
					return;
				}
			}
			// Make sure the has requests parent is active.
			noPendingRequestsParent.SetActive(false);
			hasPendingRequestsParent.SetActive(true);
			
			// Instantiate a card for the new player.
			GameObject cardObject = GameObject.Instantiate(cardPrefab, cardGrid.transform);
			if (cardObject == null)
			{
				Debug.LogErrorFormat("FriendsTabAllFriends.cs -- init -- instantiated a null object :(");
				return;
			}
			
			// Rename so we keep it sorted.
			cardObject.name = "Requests Card " + requestsCards.Count.ToString();
			cardObject.SetActive(true); // Turn it to true right away.
			FriendCard newCard = cardObject.GetComponent<FriendCard>();
			if (newCard != null)
			{
				bool isNew = NetworkFriends.instance.isNewFriend(member.zId);
				newCard.init(member, FriendCard.CardType.REQUESTED, isNew, false, onCardClicked, slideController, newMarkerTransform);
				requestsCards.Add(newCard);
				tmProMasker.addObjectArrayToList(newCard.allLabels);
				setSlideBounds();
				cardGrid.RepositionTweened();
				if (isNew)
				{
					newCard.setContentMovedDelegate(slideController, newMarkerTransform);
				}
			}
			else
			{
				// Otherwise don't add it to any lists and destroy it.
				Debug.LogErrorFormat("FriendsTabAllFriends.cs -- init -- could not find a FriendCard on the newly created object.");
				Destroy(cardObject);
			}	
		}

		slideController.enabled = requestsCards.Count > MINIMUM_CARD_TO_SCROLL;
	}	

	void OnDestroy()
	{
		NetworkFriends.instance.onInviteWithdrawn -= inviteRemoved;
		NetworkFriends.instance.onInviteCancelled -= inviteRemoved;
		NetworkFriends.instance.onInviteReceived -= inviteAdded;
		NetworkFriends.instance.onNewRequestCountUpdated -= updateBadgeLabel;
	}
}
