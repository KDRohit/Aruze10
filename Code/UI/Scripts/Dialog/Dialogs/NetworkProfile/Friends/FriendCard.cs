using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
using UnityEngine.Serialization;

public class FriendCard : MonoBehaviour
{
	public delegate void clickDelegate(FriendCard card);
	private clickDelegate onClicked;
	public enum CardType
	{
		FRIEND,
		REQUESTED, //user has requested you
		INVITED,  //you invited the user
		ADD_FRIEND,
		FIND_MORE,
		FB_INVITE,  //invite facebook friends
		FB_CONNECT, //connect to facebook
		FRIEND_CODE, //show user friend code
		BLOCKED
	}
	
	public TextMeshPro[] allLabels
	{
		get
		{
			switch (cardType)
			{
				case CardType.FIND_MORE:
					return findMoreLabels;

				case CardType.FB_INVITE:
					return friendInviteLabels;
				
				case CardType.FB_CONNECT:
					return fbConnectLabels;

				case CardType.FRIEND_CODE:
					return friendCodeLabels;

				default:
					return friendCardLabels;
			}
			
		}
	}

	[SerializeField] private TextMeshPro[]  friendCardLabels;
	[SerializeField] private TextMeshPro[] findMoreLabels;
	[SerializeField] private TextMeshPro[] fbConnectLabels;
	[SerializeField] private TextMeshPro[] friendCodeLabels;
	[SerializeField] private TextMeshPro[] friendInviteLabels;

	[SerializeField] private TextMeshPro animationLabel;
	[SerializeField] private TextMeshPro connectBonusLabel;
	
	public Animator animator;
	public SocialMember member {get; private set;}
	private CardType cardType;
	public bool isNew = false;

	[SerializeField] private GameObject friendDisplayParent;
	[SerializeField] private GameObject findMoreParent;
	[SerializeField] private GameObject fbInviteParent;
	[SerializeField] private GameObject friendCodeParent;
	[SerializeField] private TextMeshPro friendCodeLabel;
	[SerializeField] private TextMeshPro coinAmount;
	[SerializeField] private TextMeshPro tabText;
	[SerializeField] private GameObject tabParent;
	[SerializeField] private GameObject fbConnectParent;
	[SerializeField] private GameObject userObjects;
	[SerializeField] private GameObject coinObject;

	[SerializeField] private FacebookFriendInfo friendInfo;
	[SerializeField] private VIPIconHandler vipIcon;
	[SerializeField] private GameObject newTag;

	[SerializeField] private ImageButtonHandler acceptButton;
	[SerializeField] private ImageButtonHandler sendGiftButton;
	[SerializeField] private ImageButtonHandler unblockFriendButton;	
	[SerializeField] private UISprite sendGiftIcon;
	[SerializeField] private ImageButtonHandler findMoreButton;
	[SerializeField] private ClickHandler rejectButton;
	[SerializeField] private ClickHandler viewProfileButton;
	[SerializeField] private ImageButtonHandler facebookConnectButton;
	[SerializeField] private ImageButtonHandler friendCodeButton;
	[SerializeField] private ImageButtonHandler facebookInviteButton;

	[SerializeField] private GameObject addedButtonParent; // Not really a button
	[SerializeField] private GameObject rejectedButtonParent; // Not really a button

	[SerializeField] private TextMeshPro animatedAddedLabel;
	[SerializeField] private GameObject defaultBackground;
	[SerializeField] private GameObject searchBackground;
	[SerializeField] private GameObject fbBackground;
	[SerializeField] private Color enabledTextColor;
	[SerializeField] private Color disabledTextColor;

	[SerializeField] private GameObject addedOverlay;
	[SerializeField] private TextMeshPro addedOverlayText;

	private IEnumerator activeRoutine;

	private SlideController slideController;
	private Transform newMarkerTransform;
	private GameTimer giftTimer;
	public bool isSlotsPlayer = false;

	public void updateTabText(int rank)
	{
		if (rank > 0)
		{
			tabText.text = CommonText.formatNumber(rank);
		}
		else
		{
			tabText.text = "";
		}
	}

	public void init(SocialMember member, CardType type, bool isNew, bool isSearchResult, clickDelegate onClickCallback = null, SlideController slider = null, Transform newMarker = null, int rank = 0)
	{
		if (member != null)
		{
			isSlotsPlayer = member == SlotsPlayer.instance.socialMember;

			
			coinAmount.text = CommonText.formatNumberAbbreviated(member.credits * CreditsEconomy.economyMultiplier, shouldRoundUp:false, minimumToAbbreviate: CommonText.BILLION);

			updateTabText(rank);
			this.member = member;
			friendInfo.member = member;

			if (isSlotsPlayer && VIPStatusBoostEvent.isEnabled())
			{
				vipIcon.setLevel(VIPStatusBoostEvent.getAdjustedLevel());	
			}
			else
			{
				vipIcon.setLevel(member.vipLevel);	
			}
			
			if (member.canSendCreditsTimer != null)
			{
				member.canSendCreditsTimer.registerFunction(updateStatus);
			}
		}

		setBackground(type, isSearchResult);
		rejectButton.registerEventDelegate(rejectClicked);
		this.isNew = isNew;
		newTag.SetActive(isNew && !isSlotsPlayer);
		
		switch(type)
		{
			case CardType.FRIEND:
				disableNonFriendObjects();
				friendDisplayParent.SetActive(true);
				rejectButton.gameObject.SetActive(false);
				acceptButton.gameObject.SetActive(false);
				unblockFriendButton.gameObject.SetActive(false);				
				findMoreButton.gameObject.SetActive(false);
				sendGiftButton.gameObject.SetActive(!isSlotsPlayer && SlotsPlayer.instance.creditSendLimit > 0);
				sendGiftButton.registerEventDelegate(sendGiftClicked);
				if (member.canSendCredits)
				{
					toggleSendGiftButton(true, "send_gift", "Send Gift");
				}
				else
				{
					toggleSendGiftButton(false, "sent", "Sent");
				}
				
				if (member.credits != 0)
				{
					coinAmount.gameObject.SetActive(true);
					coinObject.SetActive(true);
				}
			
				tabText.gameObject.SetActive(true);
				tabParent.SetActive(true);
				
				break;
			case CardType.REQUESTED:
				disableNonFriendObjects();
				friendDisplayParent.SetActive(true);
			    rejectButton.gameObject.SetActive(true);
				sendGiftButton.gameObject.SetActive(false);
				findMoreButton.gameObject.SetActive(false);
				unblockFriendButton.gameObject.SetActive(false);				
				acceptButton.gameObject.SetActive(true);
				acceptButton.label.gameObject.SetActive(true);
				acceptButton.label.text = Localize.textTitle("accept", "Accept");
				acceptButton.registerEventDelegate(acceptClicked);
				break;
			case CardType.INVITED:
				disableNonFriendObjects();
				friendDisplayParent.SetActive(true);
			    rejectButton.gameObject.SetActive(false);
				sendGiftButton.gameObject.SetActive(false);
				findMoreButton.gameObject.SetActive(false);
				unblockFriendButton.gameObject.SetActive(false);				
				acceptButton.gameObject.SetActive(true);
				acceptButton.label.gameObject.SetActive(true);
				acceptButton.label.text = Localize.textTitle("cancel", "Cancel");
				acceptButton.registerEventDelegate(cancelClicked);
				break;
			case CardType.ADD_FRIEND:
				friendDisplayParent.SetActive(true);
				findMoreParent.SetActive(false);
				rejectButton.gameObject.SetActive(false);
				sendGiftButton.gameObject.SetActive(false);
				findMoreButton.gameObject.SetActive(false);
				unblockFriendButton.gameObject.SetActive(false);				
				acceptButton.gameObject.SetActive(true);
				acceptButton.label.gameObject.SetActive(true);
				acceptButton.label.text = Localize.textTitle("add_friend", "Add Friend");
				acceptButton.registerEventDelegate(addFriendClicked);
				break;
			case CardType.BLOCKED:
				disableNonFriendObjects();
				friendDisplayParent.SetActive(true);
				rejectButton.gameObject.SetActive(false);
				acceptButton.gameObject.SetActive(false);
				sendGiftButton.gameObject.SetActive(false);
				findMoreButton.gameObject.SetActive(false);
				unblockFriendButton.gameObject.SetActive(true);
			    unblockFriendButton.registerEventDelegate(unblockFriendClicked);
				break;
			case CardType.FIND_MORE:
				friendDisplayParent.SetActive(false);
				findMoreParent.SetActive(true);
				fbConnectParent.SetActive(false);
				fbInviteParent.SetActive(false);
				friendCodeParent.SetActive(false);
				rejectButton.gameObject.SetActive(false);
				acceptButton.gameObject.SetActive(false);
				sendGiftButton.gameObject.SetActive(false);
				unblockFriendButton.gameObject.SetActive(false);				
				findMoreButton.gameObject.SetActive(true);
				findMoreButton.registerEventDelegate(findMoreClicked);
				break;

			case CardType.FB_CONNECT:
				friendDisplayParent.SetActive(false);
				findMoreParent.SetActive(false);
				fbConnectParent.SetActive(true);
				fbInviteParent.SetActive(false);
				friendCodeParent.SetActive(false);
				facebookConnectButton.registerEventDelegate(facebookConnectClicked);
				connectBonusLabel.text = CreditsEconomy.convertCredits(SlotsPlayer.instance.mergeBonus);
				break;

			case CardType.FB_INVITE:
				friendDisplayParent.SetActive(false);
				findMoreParent.SetActive(false);
				fbConnectParent.SetActive(false);
				fbInviteParent.SetActive(true);
				friendCodeParent.SetActive(false);
				facebookInviteButton.registerEventDelegate(facebookInviteClicked);
				break;

			case CardType.FRIEND_CODE:
				friendDisplayParent.SetActive(false);
				findMoreParent.SetActive(false);
				fbConnectParent.SetActive(false);
				fbInviteParent.SetActive(false);
				friendCodeParent.SetActive(true);
				SafeSet.labelText(friendCodeLabel, SlotsPlayer.instance.socialMember.networkProfile.friendCode);
				friendCodeButton.registerEventDelegate(friendCodeClicked);
				break;
		}

		viewProfileButton.registerEventDelegate(viewProfileClicked);
		cardType = type;
		// These should never appear on initialization.
		addedButtonParent.SetActive(false);
		rejectedButtonParent.SetActive(false);
	
	    onClicked = onClickCallback;
		newMarkerTransform = newMarker;
		slideController = slider;
	}

	private IEnumerator showCopiedText()
	{
		//We should add animation to this so it doesnt just popup
		addedOverlay.SetActive(true);
		addedOverlayText.text = Localize.text("copied");
		yield return new WaitForSeconds(3.0f);
		if (friendCodeButton.label != null && friendCodeButton.label.gameObject != null)
		{
			addedOverlay.SetActive(false);
		}
	}

	private void setBackground(CardType type, bool isSearchResult)
	{
		switch(type)
		{
			case CardType.FB_INVITE:
				fbBackground.SetActive(true);
				defaultBackground.SetActive(false);
				searchBackground.SetActive(false);
				break;

			default:
				
				fbBackground.SetActive(false);
				defaultBackground.SetActive(!isSearchResult && !isSlotsPlayer);
				userObjects.SetActive(isSlotsPlayer);
				searchBackground.SetActive(isSearchResult);
				break;
		}
		
	}

	private void disableNonFriendObjects()
	{
		//root objects
		fbConnectParent.SetActive(false);
		fbInviteParent.SetActive(false);
		friendCodeParent.SetActive(false);
		findMoreParent.SetActive(false);
	}

	private void Start()
	{
		if (slideController == null || slideController.content == null)
		{
			return;
		}

		checkForSeen(slideController.content.transform, Vector2.zero);
	}
	public void setContentMovedDelegate(SlideController slideController, Transform newMarkerTransform)
	{
		this.newMarkerTransform = newMarkerTransform;
		this.slideController = slideController;
		slideController.onContentMoved += checkForSeen;
	}

	// MCC -- making this public for when we need to manually call this (when there aren't enough cards to scroll)
	public void checkForSeen(Transform contentTransform, Vector2 delta)
	{
		if (gameObject != null && newMarkerTransform != null && gameObject.activeInHierarchy)
		{			
			if (cardType == CardType.REQUESTED)
			{
				if (transform.position.x < newMarkerTransform.position.x)
				{
					// If this is above the view marker, then mark it as seen and unregister this function from the event.
					slideController.onContentMoved -= checkForSeen;
					NetworkFriends.instance.markRequestSeen(member.zId);
				}
			}
			else if (cardType == CardType.FRIEND)
			{
				if (transform.position.y > newMarkerTransform.position.y)
				{
					// If this is above the view marker, then mark it as seen and unregister this function from the event.
					slideController.onContentMoved -= checkForSeen;
					NetworkFriends.instance.markFriendSeen(member.zId);
				}
			}
		}
	}

	private void facebookConnectClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("friend_card", "fbAuth", "", "", "", "click");
		if (SlotsPlayer.IsAppleLoggedIn)
		{
			Dialog.close(Dialog.instance.currentDialog);
			SocialManager.Instance.FBConnect();
		}
		else
		{
			SlotsPlayer.facebookLogin();
		}
	}

	private void facebookInviteClicked(Dict args = null)
	{
		if (!NetworkFriends.instance.isEnabled || !SlotsPlayer.isFacebookUser)
		{
			Debug.LogError("Cannot invite users without facebook");
			return;
		}

		MFSDialog.inviteFacebookNonAppFriends();

		StatsManager.Instance.LogCount("friend_card", "overlay", "friends", "invite_friends", "", "click");
	}

	private void friendCodeClicked(Dict args = null)
	{
		string friendCodeShareText = "";
#if UNITY_EDITOR
		StartCoroutine(showCopiedText());
#endif

	// FRIEND_CODE_SHARING_URL is a bit.ly link that directs to app.adjust.com. These are used to do User Acquisition tracking.
	// It directs to the mobile landing page. We use the mobile landing page to get users to the right platform app store.
	string url = Data.liveData.getString("FRIEND_CODE_SHARING_URL", "");
	if (!string.IsNullOrEmpty(url))
	{
		friendCodeShareText = Localize.text("friend_code_share_with_link_{0}_{1}", SlotsPlayer.instance.socialMember.networkProfile.friendCode, url);
	}
	else
	{
		friendCodeShareText = Localize.text("friend_code_share_{0}", SlotsPlayer.instance.socialMember.networkProfile.friendCode);
	}

#if !UNITY_EDITOR
#if UNITY_WEBGL 
		WebGLFunctions.copyTextToClipboard(friendCodeShareText);
		StartCoroutine(showCopiedText());
#else
		NativeBindings.ShareContent(
			subject:"Friend Code",
			body:friendCodeShareText,
			imagePath:"",
			url:"");
#endif
#else
		Debug.LogError("Sharing code: " + friendCodeShareText);
#endif
	}
	private void viewProfileClicked(Dict args = null)
	{
		NetworkProfileDialog.showDialog(member, SchedulerPriority.PriorityType.IMMEDIATE);
	}
	
	private void rejectClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "friends",
			phylum: "friends_tab",
			klass: "friend_requests",
			family: "deny",
			genus: member.zId);

		NetworkFriends.instance.onInviteDeclined += onRejectCallback;
		NetworkFriends.instance.rejectFriend(member);
		Audio.play(NetworkFriends.FRIEND_ACTION_AUDIO);
		StartCoroutine(playAnimThenReposition("reject"));
	}

	private void onRejectCallback(SocialMember networkMember, bool didSucceed, int errorCode)
	{
		NetworkFriends.instance.onInviteDeclined -= onRejectCallback;
		if (!didSucceed)
		{
			Debug.LogWarning("Reject invite failed: " + errorCode.ToString());
		}
	}

	private void cancelClicked(Dict args = null)
	{
		acceptButton.enabled = false;
		NetworkFriends.instance.onInviteWithdrawn += onCancelCallback;
		NetworkFriends.instance.cancelFriendInvite(member);
	}

	private void onCancelCallback(SocialMember networkMember, bool didSucceed, int errorCode)
	{
		NetworkFriends.instance.onInviteWithdrawn -= onCancelCallback;
		if (!didSucceed)
		{
			Debug.LogWarning("Cancel invite failed: " + errorCode);
		}

		if (this != null && this.gameObject != null)
		{
			StartCoroutine(playAnimThenReposition("add"));
			Audio.play(NetworkFriends.FRIEND_ACTION_AUDIO);
		}
	}

	private void acceptClicked(Dict args = null)
	{
		if (SocialMember.allFriends.Count >= NetworkFriends.instance.friendLimit)
		{
			// If we are at the friend limit, and we know it already
			// then just show the limit popup now and dont show the animation.
			NetworkFriends.instance.showFriendsLimitPopupYours();
		}
		else
		{
			StatsManager.Instance.LogCount(
				counterName: "dialog",
				kingdom: "friends",
				phylum: "friends_tab",
				klass: "friend_requests",
				family: "accept",
				genus: member.zId);

			acceptButton.enabled = false;
			animationLabel.text = Localize.text("added_ex", "Added!");
			StartCoroutine(playAnimThenReposition("add"));
			Audio.play(NetworkFriends.FRIEND_ACTION_AUDIO);

			NetworkFriends.instance.onInviteAccepted += onAcceptCallback;
			NetworkFriends.instance.acceptFriend(member);
		}
	}

	private void onAcceptCallback(SocialMember networkMember, bool didSucceed, int errorCode)
	{
		NetworkFriends.instance.onInviteAccepted -= onAcceptCallback;
		if (!didSucceed)
		{
			Debug.LogWarning("accept invite failed: " + errorCode);
			return;
		}
	}

	private void addFriendClicked(Dict args = null)
	{
		if (SocialMember.allFriends.Count >= NetworkFriends.instance.friendLimit)
		{
			// If we are at the friend limit, and we know it already
			// then just show the limit popup now and dont show the animation.
			NetworkFriends.instance.showFriendsLimitPopupYours();
		}
		else
		{
			StatsManager.Instance.LogCount(
				counterName: "dialog",
				kingdom: "friends",
				phylum: "friends_tab",
				klass: "find_new_friends",
				family: "send_friend_request",
				genus: member.zId);

			acceptButton.enabled = false;
			animationLabel.text = Localize.text("sent_ex", "Sent!");
			StartCoroutine(playAnimThenReposition("add"));
			Audio.play(NetworkFriends.FRIEND_ACTION_AUDIO);
		
			NetworkFriends.instance.onInviteFriend += onAddCallback;
			NetworkFriends.instance.inviteFriend(member);
		}

	}

	private void onAddCallback(SocialMember networkMember, bool didSucceed, int errorCode)
	{
		NetworkFriends.instance.onInviteFriend -= onAddCallback;
		if (!didSucceed)
		{
			Debug.LogWarning("add friend failed: " + errorCode);
		}
	}

	private void findMoreClicked(Dict args = null)
	{
		clickCallback();
	}

	private void unblockFriendClicked(Dict args = null)
	{
		NetworkFriends.instance.onFriendUnblocked += onUnblockCallback;		
		animationLabel.text = Localize.text("unblocked_ex", "Unblocked!");
		StartCoroutine(playAnimThenReposition("add"));
		NetworkFriends.instance.unblockFriend(member);
	}

	private void onUnblockCallback(SocialMember networkMember, bool didSucceed, int errorCode)
	{
		NetworkFriends.instance.onFriendUnblocked -= onUnblockCallback;
		if (!didSucceed)
		{
			Debug.LogWarning("Unblock Friend failed: " + errorCode);
		}
	}

	private void clickCallback()
	{
	    if (onClicked != null)
		{
			onClicked(this);
		}
		else
		{
			Debug.LogErrorFormat("FriendCard.cs -- clickCallback -- no callback was set.");
		}
	}

	private IEnumerator playAnimThenReposition(string animationName)
	{
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(animator, animationName));
		clickCallback();// Now do the callback
		
	}

	public void updateStatus(Dict args = null, GameTimerRange originalTimer = null)
	{
		switch(cardType)
		{
			case CardType.FRIEND:			
				if(sendGiftButton.enabled == false && member.canSendCredits)
				{
					toggleSendGiftButton(true, "send_gift", "Send Gift");
				}
				else if (sendGiftButton.enabled == true && !member.canSendCredits)
				{
					toggleSendGiftButton(false, "sent", "Sent");
				}
				break;
		}
	}

	public void toggleSendGiftButton(bool enabled, string textKey, string defaultText)
	{
		if (enabled)
		{
			sendGiftButton.enabled = true;
			sendGiftButton.sprite.color = Color.white;
			sendGiftIcon.color = Color.white;
			sendGiftButton.label.color = enabledTextColor;
		}
		else
		{
			sendGiftButton.enabled = false;
			sendGiftButton.sprite.color = Color.grey;
			sendGiftIcon.color = Color.grey;
			sendGiftButton.label.color = disabledTextColor;
		}
		sendGiftButton.label.text = Localize.textTitle(textKey, defaultText);
		
	}

	public void playSendGiftAnim()
	{
		if (member == null)
		{
			Debug.LogErrorFormat("FriendCard.cs -- sendGiftClicked -- calling send gift on a friend card that has been initialized with a null member. This is not allowed.");
			return;
		}

		if (!member.canSendCredits)
		{
			//don't play for ones that we've already sent to, but still turn the button off
			toggleSendGiftButton(false, "sent", "Sent");
			return;
		}
		
		if (animator != null)
		{
			animator.Play("gift");
		}
		else
		{
			Debug.LogErrorFormat("FriendCard.cs -- sendGiftClicked -- animator is null!!");
		}

		toggleSendGiftButton(false, "sent", "Sent");
	}

	private void sendGiftClicked(Dict args = null)
	{
		if (member == null)
		{
			Debug.LogErrorFormat("FriendCard.cs -- sendGiftClicked -- calling send gift on a friend card that has been initialized with a null member. This is not allowed.");
			return;
		}

		if (SlotsPlayer.instance == null || SlotsPlayer.instance.socialMember == null)
		{
			Debug.LogErrorFormat("FriendCard.cs -- sendGiftCLicked -- something weird here with the current slots player being null, bailing.");
		}
		
		if (animator != null)
		{
			animator.Play("gift");
		}
		else
		{
			Debug.LogErrorFormat("FriendCard.cs -- sendGiftClicked -- animator is null!!");
		}
		
		string msg = Localize.text("send_to_friends_credit_message_{0}", SlotsPlayer.instance.socialMember.fullName);

		// see if a help item already exists for this
		InboxItem inboxItem = InboxInventory.findItemBy(member.zId, InboxItem.InboxType.SEND_CREDITS);
		string eventId = inboxItem != null ? inboxItem.eventId : "";

		member.canSendCredits = false;
		InboxAction.sendCredits(member.zId, eventId, msg);

		Audio.play(NetworkFriends.FRIEND_ACTION_AUDIO);

		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "friends",
			phylum: "friends_tab",
			klass: "all_friends",
			family: "gift",
			genus: member.zId);

		toggleSendGiftButton(false, "sent", "Sent");
	}

	public string getZid()
	{
		return member == null ? "" : member.id;
	}

	private void OnDestroy()
	{
		//remove any active callbacks that haven't fired
		NetworkFriends.instance.onInviteFriend -= onAddCallback;
		NetworkFriends.instance.onInviteAccepted -= onAcceptCallback;
		NetworkFriends.instance.onInviteWithdrawn -= onCancelCallback;
		NetworkFriends.instance.onInviteDeclined -= onRejectCallback;

		if (member != null && member.canSendCreditsTimer != null)
		{
			member.canSendCreditsTimer.removeFunction(updateStatus);
		}

		if (null != slideController)
		{
			slideController.onContentMoved -= checkForSeen;
		}
		
		//stop coroutine
		StopAllCoroutines();
	}
}
