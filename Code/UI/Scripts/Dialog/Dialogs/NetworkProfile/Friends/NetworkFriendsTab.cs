using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;


public class NetworkFriendsTab : NetworkProfileTabBase
{
	[SerializeField] private TabManager friendTabManager;
	[SerializeField] private FriendsTabAllFriends allFriends;
	[SerializeField] private FriendsTabRequests requests;
	[SerializeField] private FriendsTabFindFriends findFriends;
	[SerializeField] private Transform popupAnchor;
	[SerializeField] private GameObject requestsBadgeParent;
	[SerializeField] private TextMeshPro requestsBadgeLabel;
    

	private int entryTab = -1;
	private const int MAX_VISIBLE_CARDS = 8; // The most cards that will be visible at once on any tab.
	private const float INTRO_ANIMATION_DELAY = 0.1f; // How much to delay between play card intros.
	private const float OUTRO_ANIMATION_DELAY = 0.5f; // How much to delay before turning cards off.
	
	private enum FriendTabTypes:int
	{
		ALL_FRIENDS = 0,
		REQUESTS = 1,
		FIND_FRIENDS = 2	
	}


	public void cleanForFtue()
	{
		//All friends tab does not have elements that will overlap the ftue
		//switch to this tab
		friendTabManager.selectTab((int)FriendTabTypes.ALL_FRIENDS);
	}

	public void updateFriendCards()
	{
		// Force select the allFriends tab when re-entering this tab.
		switch(friendTabManager.currentTab.index)
		{
			case (int)FriendTabTypes.ALL_FRIENDS:
				allFriends.updateFriendCardStatus();
				break;

			default:
				break;
		}
	}

	
	public void init(SocialMember member, int dialogEntryMode)
	{
		this.member = member;
		switch (dialogEntryMode)
		{
			default:
				entryTab = (int)FriendTabTypes.ALL_FRIENDS;
				break;
			case NetworkProfileDialog.MODE_FRIEND_REQUESTS:
				entryTab = (int)FriendTabTypes.REQUESTS;
				break;
			case NetworkProfileDialog.MODE_FIND_FRIENDS:
				entryTab = (int)FriendTabTypes.FIND_FRIENDS;
				break;
		}
		allFriends.init();
		requests.init();
		findFriends.init();		
		friendTabManager.init(typeof(FriendTabTypes), entryTab, onFriendsTabSelect);

		requestsBadgeLabel.text = NetworkFriends.instance.newFriendRequests.ToString();
		requestsBadgeParent.SetActive(NetworkFriends.instance.newFriendRequests > 0);
		NetworkFriends.instance.onNewRequestCountUpdated += updateRequestBadgeCount;
	}

	private void updateRequestBadgeCount()
	{
		requestsBadgeLabel.text = NetworkFriends.instance.newFriendRequests.ToString();
		requestsBadgeParent.SetActive(NetworkFriends.instance.newFriendRequests > 0);
	}

	public override IEnumerator onIntro(NetworkProfileDialog.ProfileDialogState fromState, string extraData = "")
	{
		// Force select the allFriends tab when re-entering this tab.

		if (entryTab > 0)
		{
			friendTabManager.selectTab(entryTab);
			entryTab = -1; // Reset this now that we have used it.
		}
		else if (SocialMember.allFriends != null && SocialMember.allFriends.Count > 0)
		{
			friendTabManager.selectTab((int)FriendTabTypes.ALL_FRIENDS);
		}
		else
		{
			friendTabManager.selectTab((int)FriendTabTypes.FIND_FRIENDS);
		}
		yield return null;
	}


    public override IEnumerator onOutro(NetworkProfileDialog.ProfileDialogState fromState, string extraData = "")
	{
		// Force select the allFriends tab when re-entering this tab.
		switch(friendTabManager.currentTab.index)
		{
			case (int)FriendTabTypes.ALL_FRIENDS:
				allFriends.hide();
				break;
			case (int)FriendTabTypes.REQUESTS:
				requests.hide();
				break;
			case (int)FriendTabTypes.FIND_FRIENDS:
				findFriends.hide();
				break;
		}
		yield return null;
	}
	
	private void onFriendsTabSelect(TabSelector tab)
	{
		// The Tab Manager will handle turning the content on/off, but we need to 
		// make sure we reset/show everything properly when we show each tab.
		switch(tab.index)
		{
			case (int)FriendTabTypes.ALL_FRIENDS:
				allFriends.show();
				break;
			case (int)FriendTabTypes.REQUESTS:
				requests.show();
				break;
			case (int)FriendTabTypes.FIND_FRIENDS:
				findFriends.show();
				break;
		}
	}

	public static void playCardAnimations(List<FriendCard> cardList, bool isIntro, System.Action callback = null)
	{
	    RoutineRunner.instance.StartCoroutine(playCardAnimationsRoutine(cardList, isIntro, callback));
	}
	
	private static IEnumerator playCardAnimationsRoutine(List<FriendCard> cardList, bool isIntro, System.Action callback)
	{
		if (isIntro)
		{
			// Stagger the intro animations.
			yield return new WaitForSeconds(INTRO_ANIMATION_DELAY); // Wait once at the beginning so we dont hide the first one.
			// Play the aniation on the cards.
			for (int i = 0; i < cardList.Count; i++)
			{
				if (cardList[i] == null || cardList[i].gameObject == null)
				{
					// This could happen if closing the dialog.
					continue;
				}
				
				if (cardList[i].gameObject.activeInHierarchy)
				{
					// If it is already on, then play hold.
					cardList[i].animator.Play("hold");
				}
				else
				{
					cardList[i].gameObject.SetActive(true); // Make sure the object is turned on so it can animate.
				}

				yield return new WaitForSeconds(INTRO_ANIMATION_DELAY);
			}
		}
		else
		{
			yield return new WaitForSeconds(OUTRO_ANIMATION_DELAY);
			// Outro isn't guaranteed to be at the front, so just play them all at once.
			for (int i = 0; i < cardList.Count; i++)
			{
				//don't play animation if card is being destroyed
				if (null != cardList[i] && null != cardList[i].gameObject && cardList[i].gameObject.activeSelf)
				{
					cardList[i].animator.Play("outro");
				}
				
			}
		}

		if (callback != null)
		{
			callback();
		}
	}

	void OnDestroy()
	{
		NetworkFriends.instance.onNewRequestCountUpdated -= updateRequestBadgeCount;
	}
	
}
