using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class FriendsProfileTab : AchievementsProfileTab
{
	[SerializeField] private Animator profilePictureAnimator;

	[SerializeField] private TextMeshPro facebookName;
	[SerializeField] private GameObject friendCountParent;
	[SerializeField] private TextMeshPro friendCountLabel;
	[SerializeField] private TextMeshPro sendGiftTimerLabel;

	[SerializeField] private GameObject facebookNameParent;
	[SerializeField] private GameObject moreOptionsParent;
	[SerializeField] private GameObject giftCooldownParent;
	[SerializeField] private GameObject addFriendParent;
	[SerializeField] private Transform popupAnchor;

	[SerializeField] private ImageButtonHandler addFriendButton;
	[SerializeField] private ImageButtonHandler sendGiftButton;
	[SerializeField] private ImageButtonHandler friendCodeButton;
	[SerializeField] private ImageButtonHandler moreOptionsButton;
	[SerializeField] private ButtonHandler unfriendButton;
	[SerializeField] private ImageButtonHandler requestPendingButton;
	[SerializeField] private ButtonHandler blockButton;
	[SerializeField] private ImageButtonHandler unblockButton;
	[SerializeField] private ClickHandler moreOptionsShroud;

	[SerializeField] private GameObject addedOverlay;
	[SerializeField] private TextMeshPro addedOverlayText;
	[SerializeField] private Transform moreOptionPopupBackground;

	[SerializeField] private GameObject achievementRankParent;
	[SerializeField] private GameObject statsParent;

	private bool isFriend = false;
	private bool isBlocked = false;
	private bool hasBeenInvited = false;
	private bool hasInvitedYou = false;

	private const float LABEL_REFRESH_TIME = 0.3f; // used when the button is in "..." mode.
	private const float ACTION_OVERLAY_DURATION = 1.5f;
	private const float CONTAINER_THREE_BUTTON_HEIGHT = 830f;
	private const float CONTAINER_TWO_BUTTON_HEIGHT = 590f;
	private const float CONTAINER_ONE_BUTTON_HEIGHT = 356f;

	private bool isWaitingForServerResponse = false;

	public override void init(SocialMember member, NetworkProfileDialog dialog)
	{
		base.init(member, dialog);
		// Register all the button callbacks.
		addFriendButton.registerEventDelegate(addFriendClicked);
		sendGiftButton.registerEventDelegate(sendGiftClicked);
		friendCodeButton.registerEventDelegate(friendCodeClicked);
		moreOptionsButton.registerEventDelegate(moreOptionsClicked);
		unfriendButton.registerEventDelegate(unfriendClicked);
		requestPendingButton.registerEventDelegate(requestPendingClicked);
		blockButton.registerEventDelegate(blockClicked);
		unblockButton.registerEventDelegate(unblockClicked);
		moreOptionsShroud.registerEventDelegate(closeMoreOptions);

		// Register for server events that can change this dialog.
		NetworkFriends.instance.onInviteReceived += inviteReceivedCallback;
		NetworkFriends.instance.onNewFriend += newFriendConnectionCallback;

		if (NetworkAchievements.isEnabled)
		{
			statsParent.SetActive(false);
			achievementRankParent.SetActive(true);
		}
		else
		{
			statsParent.SetActive(true);
			achievementRankParent.SetActive(false);
			if (hirStats != null)
			{
				// This isn't present in the achievements version of the dialog.
				if (member.networkProfile.gameStats != null && member.networkProfile.gameStats.ContainsKey("hir"))
				{
					hirStats.setLabels(member.networkProfile.gameStats["hir"]);
				}
				else
				{
					hirStats.setLabels(null);
				}
			}
		}

		facebookNameParent.SetActive(false); // Turn this off by default.
		if (member.isFacebookFriend)
		{
			StartCoroutine(setFacebookName());
		}

		// Set labels
		if (member.isUser)
		{
			friendCountLabel.text = Localize.text("friends_{0}", SocialMember.allFriends.Count);
		}
		else
		{
			SafeSet.gameObjectActive(friendCountParent.gameObject, false);
		}

		if (SlotsPlayer.instance != null && SlotsPlayer.instance.socialMember != null && SlotsPlayer.instance.socialMember.networkProfile != null)
		{
			string friendCode = SlotsPlayer.instance.socialMember.networkProfile.friendCode;
			friendCodeButton.label.text = friendCode;
		}

		registerTimerFunctions();
		// Toggle items.
		moreOptionsParent.SetActive(false);
		toggleButtons();
	}


	private void registerTimerFunctions()
	{
		if (member != null && member.canSendCreditsTimer != null)
		{
			member.canSendCreditsTimer.removeFunction(onSendCreditsCooldownFinish); // Remove this first so we never double add.
			member.canSendCreditsTimer.registerLabel(sendGiftTimerLabel, GameTimerRange.TimeFormat.REMAINING, false);
			member.canSendCreditsTimer.registerFunction(onSendCreditsCooldownFinish);
		}
	}
	
	private IEnumerator setFacebookName()
	{
		yield return StartCoroutine(member.requestName());

		if (!member.didFailNameRequest && facebookName != null && facebookNameParent != null &&
			!string.IsNullOrEmpty(member.fbFirstName) && !string.IsNullOrEmpty(member.fbLastName))
		{
			// Only show the facebook name if we got the name from facebook,
			// Also null check in case this comes back after they close the dialog.
			facebookName.text = member.fbFirstName + " " + member.fbLastName;
			facebookNameParent.SetActive(true);
		}
	}
	
	private void onSendCreditsCooldownFinish(Dict args = null, GameTimerRange originalTimer = null)
	{
		// Null check these in case the dialog has been closed.
		if (sendGiftButton != null && sendGiftButton.gameObject != null)
		{
			sendGiftButton.gameObject.SetActive(true);
		}
		if (giftCooldownParent != null)
		{
			giftCooldownParent.SetActive(false);
		}
	}

    public void toggleButtons()
	{
		if (member.isUser)
		{
			// Turn ON the member profile only buttons.
			// If for whatever reason we dont have a friend code, just hide the button.
			friendCodeButton.gameObject.SetActive(member != null &&
			    member.networkProfile != null &&
			    !string.IsNullOrEmpty(member.networkProfile.friendCode));

			// Turn OFF the gifting/reporting buttons since you cannot gift to yourself.
			sendGiftButton.gameObject.SetActive(false);
			giftCooldownParent.SetActive(false);
			moreOptionsButton.gameObject.SetActive(false);
			requestPendingButton.gameObject.SetActive(false);
			addFriendButton.gameObject.SetActive(false);
			unfriendButton.gameObject.SetActive(false);
			unblockButton.gameObject.SetActive(false);

			// NOTE - The edit profile button is handled in the base class.
		}
		else
		{
			// Turn OFF the member profile only buttons.
			friendCodeButton.gameObject.SetActive(false);
			moreOptionsButton.gameObject.SetActive(true);

			isFriend = SocialMember.allFriends.Contains(member);
			isBlocked = SocialMember.blockedPlayers.Contains(member);;
			hasBeenInvited = SocialMember.invitedPlayers.Contains(member);
			hasInvitedYou = SocialMember.invitedByPlayers.Contains(member);

			if (isFriend)
			{
				// Turn off add friend
				requestPendingButton.gameObject.SetActive(false);
				addFriendButton.gameObject.SetActive(false);
				unblockButton.gameObject.SetActive(isBlocked);
				sendGiftButton.gameObject.SetActive(!isBlocked && member.canSendCredits);
				giftCooldownParent.SetActive(false);

				toggleUnfriendButton(true);
				toggleBlockButton(!isBlocked);
				
				registerTimerFunctions();

			}
			else if (isBlocked)
			{
				// Turn off add friend
				requestPendingButton.gameObject.SetActive(false);
				addFriendButton.gameObject.SetActive(false);
				unblockButton.gameObject.SetActive(true);

				// Turn off gifting.
				sendGiftButton.gameObject.SetActive(false);
				giftCooldownParent.SetActive(false);
				toggleUnfriendButton(false);
				toggleBlockButton(false);
			}
			else if (hasBeenInvited)
			{
				// Turn off gifting.
				sendGiftButton.gameObject.SetActive(false);
				giftCooldownParent.SetActive(false);
				toggleUnfriendButton(false);

				// Turn on request pending flow.
				requestPendingButton.gameObject.SetActive(true);
				addFriendButton.gameObject.SetActive(false);
				unblockButton.gameObject.SetActive(false);
				toggleBlockButton(true);
			}
			else
			{
				// Turn off gifting.
				sendGiftButton.gameObject.SetActive(false);
				giftCooldownParent.SetActive(false);
				toggleUnfriendButton(false);

				// Turn on add friend flow.
				requestPendingButton.gameObject.SetActive(false);
				addFriendButton.gameObject.SetActive(true);
				if (hasInvitedYou)
				{
					addFriendButton.label.text = Localize.textTitle("accept", "Accept");
				}
				else
				{
					addFriendButton.label.text = Localize.textTitle("add_friend", "Add Friend");
				}
				unblockButton.gameObject.SetActive(false);

				toggleBlockButton(true);
			}
		}
	}

	private void toggleBlockButton(bool isActive)
	{
		blockButton.gameObject.SetActive(isActive);
		bool unfriendActive = unfriendButton.gameObject.activeSelf;

		float containerHeight = CONTAINER_ONE_BUTTON_HEIGHT;
		if (unfriendActive && isActive)
		{
			containerHeight = CONTAINER_THREE_BUTTON_HEIGHT;
		}
		else if (isActive || unfriendActive)
		{
			containerHeight = CONTAINER_TWO_BUTTON_HEIGHT;
		}
		CommonTransform.setHeight(moreOptionPopupBackground, containerHeight);
	}

	private void toggleUnfriendButton(bool isActive)
	{
		// The container for the unfriend button needs to shrink when it has only two values.
		unfriendButton.gameObject.SetActive(isActive);
		bool blockActive = blockButton.gameObject.activeSelf;

		float containerHeight = CONTAINER_ONE_BUTTON_HEIGHT;
		if (blockActive && isActive)
		{
			containerHeight = CONTAINER_THREE_BUTTON_HEIGHT;
		}
		else if (isActive || blockActive)
		{
			containerHeight = CONTAINER_TWO_BUTTON_HEIGHT;
		}
		CommonTransform.setHeight(moreOptionPopupBackground, containerHeight);
	}

	private IEnumerator buttonWaitingRoutine(TextMeshPro buttonLabel)
	{
		string originalText = buttonLabel.text;
		if (isWaitingForServerResponse)
		{
			Debug.LogErrorFormat("FriendsProfileTab.cs -- buttonWaitingRoutine -- we are still waiting for a server action to come back, this shouldn't be happening.");
		}
		isWaitingForServerResponse = true;
		buttonLabel.text = ".";
		while (isWaitingForServerResponse)
		{
			string text = buttonLabel.text;
			text += ".";
			if (text == "......")
			{
				text = ".";
			}
			buttonLabel.text = text;
			yield return new WaitForSeconds(LABEL_REFRESH_TIME);
		}
		buttonLabel.text = originalText; // Set it back to how it was before.
	}

	private void acceptFriendActionCallback(SocialMember member, bool didSucceed, int errorCode)
	{
		isWaitingForServerResponse = false;
		addFriendButton.enabled = true; // re-enable the button
		if (didSucceed)
		{
			StartCoroutine(showOverlayText(Localize.text("added", ""), true));
		}
		else
		{
			// Show the Add Friend button.
			StartCoroutine(showOverlayText(Localize.text("request_failed", ""), false));
		}
		NetworkFriends.instance.onInviteAccepted -= acceptFriendActionCallback;
	}


    private void addFriendActionCallback(SocialMember member, bool didSucceed, int errorCode)
	{
		isWaitingForServerResponse = false;
		if (addFriendButton != null)
		{
			addFriendButton.enabled = true; // re-enable the button
		}

		if (didSucceed)
		{
			StartCoroutine(showOverlayText(Localize.text("request_sent", ""), true));
		}
		else
		{
			StartCoroutine(showOverlayText(Localize.text("request_failed", ""), false));
		}
		NetworkFriends.instance.onInviteFriend -= addFriendActionCallback;
	}

	private void inviteCancelledActionCallback(SocialMember member, bool didSucceed, int errorCode)
	{
		isWaitingForServerResponse = false;
		requestPendingButton.enabled = true; // Re-enable the button
		if (didSucceed)
		{
			StartCoroutine(showOverlayText(Localize.text("request_cancelled", ""), true));
		}
		else
		{
			StartCoroutine(showOverlayText(Localize.text("request_failed", ""), false));
		}
		NetworkFriends.instance.onInviteCancelled -= inviteCancelledActionCallback;
	}

	private void blockedPlayerActionCallback(SocialMember member, bool didSucceed, int errorCode)
	{
		isWaitingForServerResponse = false;
		blockButton.enabled = true;
		if (didSucceed)
		{
			// Turn off add friend
			requestPendingButton.gameObject.SetActive(false);
			addFriendButton.gameObject.SetActive(false);
			unblockButton.gameObject.SetActive(true);

			// Turn off gifting.
			sendGiftButton.gameObject.SetActive(false);
			giftCooldownParent.SetActive(false);

			toggleUnfriendButton(false);
			toggleBlockButton(false);
		}
		NetworkFriends.instance.onFriendBlocked -= blockedPlayerActionCallback;
	}

	private void unblockPlayerActionCallback(SocialMember member, bool didSucceed, int errorCode)
	{
		isWaitingForServerResponse = false;
		unblockButton.enabled = true;
		if (didSucceed)
		{
			// Turn off gifting.
			sendGiftButton.gameObject.SetActive(false);
			giftCooldownParent.SetActive(false);
			toggleUnfriendButton(false);

			// Turn on add friend flow
			requestPendingButton.gameObject.SetActive(false);
			// Once you block someone the friendship is terminated, so we want to always turn this on.
			addFriendButton.gameObject.SetActive(true);
			unblockButton.gameObject.SetActive(false);
			toggleBlockButton(true);
		}
		NetworkFriends.instance.onFriendUnblocked -= unblockPlayerActionCallback;
	}

    private void unfriendPlayerActionCallback(SocialMember member, bool didSucceed, int errorCode)
	{
		isWaitingForServerResponse = false;
		unfriendButton.enabled = true;
		if (didSucceed)
		{
			// Turn off gifting.
			sendGiftButton.gameObject.SetActive(false);
			giftCooldownParent.SetActive(false);
			toggleUnfriendButton(false);

			// Turn on add friend flow
			requestPendingButton.gameObject.SetActive(false);
			// If you unfriended someone, then they are definitely not your friend.
			addFriendButton.gameObject.SetActive(true);
			unblockButton.gameObject.SetActive(false);
			toggleBlockButton(true);
		}
		NetworkFriends.instance.onFriendRemoved -= unfriendPlayerActionCallback;
	}

	private void inviteReceivedCallback(SocialMember invitedMember, bool didSucceed, int errorCode)
	{
		// If we receieved an invite from the currenly viewed member, then change the buttons.
		if (member == invitedMember)
		{
			toggleButtons();
		}
	}

	private void newFriendConnectionCallback(SocialMember newFriend, bool didSucceed, int errorCode)
	{
		// If the curently viewed player accepted the invite, then change the buttons.
		if (member == newFriend)
		{
			toggleButtons();

			// Also update the friends count label.
			friendCountLabel.text = Localize.text("friends_{0}", SocialMember.allFriends.Count);
		}
	}

#region BUTTON_CALLBACKS
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

	private void sendGiftClicked(Dict args = null)
	{
		profilePictureAnimator.Play("Send");
		// TODO send gift.
		string message = "";
		string zTrackSource = "";

		// see if a help item already exists for this
		InboxItem inboxItem = InboxInventory.findItemBy(member.zId, InboxItem.InboxType.SEND_CREDITS);
		string eventId = inboxItem != null ? inboxItem.eventId : "";

		member.canSendCredits = false;
		InboxAction.sendCredits(member.zId, eventId, message);

		// Set the cooldown mode to true on the button.
		toggleButtons();
	}

	private void blockClicked(Dict args = null)
	{
		showConfirmBlockPopup();
	}

	private void unblockClicked(Dict Args = null)
	{
		showConfirmUnblockPopup();
	}

	private void addFriendClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile",
			phylum: "profile",
			klass: "",
			family: "add_friend",
			genus: member.zId);

		if (SocialMember.allFriends.Count >= NetworkFriends.instance.friendLimit)
		{
			// If we are at or above the max friend limit, dont even try to send the request to the server.
			NetworkFriends.instance.showFriendsLimitPopupYours();
		}
		else
		{
			StartCoroutine(buttonWaitingRoutine(addFriendButton.label));
			if (SocialMember.invitedByPlayers.Contains(member))
			{
				// If this is someone that has invited you, then send the accept invite.
				NetworkFriends.instance.acceptFriend(member);
				NetworkFriends.instance.onInviteAccepted += acceptFriendActionCallback;
			}
			else
			{
				// Otherwise just send invite friend.
				NetworkFriends.instance.inviteFriend(member);
				NetworkFriends.instance.onInviteFriend += addFriendActionCallback;
			}
		}

	}

	private IEnumerator showOverlayText(string overlayText, bool didSucceed)
	{
		addedOverlayText.text = overlayText;
		addedOverlay.SetActive(true);
		yield return new WaitForSeconds(ACTION_OVERLAY_DURATION);
		addedOverlay.SetActive(false);

		// Now toggle the correct buttons (at this point SocialMember should have correclty updated
		// all of the relevant lists).
		toggleButtons();
	}

	private void moreOptionsClicked(Dict args = null)
	{
		moreOptionsParent.SetActive(true);
		hideRankTooltip(); // If this is open, hide it.
	}

	public override void rankClicked(Dict args = null)
	{
		base.rankClicked();
		if (moreOptionsParent.activeSelf)
		{
			// If this is open, turn it off.
			moreOptionsParent.SetActive(false);
		}

	}
	
	private void unfriendClicked(Dict args = null)
	{
		showConfirmUnfriendPopup();
	}

	private void requestPendingClicked(Dict args = null)
	{
		NetworkFriends.instance.onInviteCancelled += inviteCancelledActionCallback;
		NetworkFriends.instance.cancelFriendInvite(member);
		StartCoroutine(buttonWaitingRoutine(requestPendingButton.label));
	}

	private void closeMoreOptions(Dict args = null)
	{
		moreOptionsParent.SetActive(false);
	}
#endregion

#region POPUP_FUNCTIONS
	public void showConfirmUnfriendPopup()
	{
		GenericPopup.showFriendsPopupAtAnchor(
			popupAnchor,
			Localize.text("are_you_sure_you_want_to_remove_this_friend"),
			true,
			unfriendYesCallback);
	}

	private void unfriendYesCallback(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile",
			phylum: "profile",
			klass: "",
			family: "remove_friend",
			genus: member.zId);
		
		StartCoroutine(buttonWaitingRoutine(unfriendButton.label));
		unfriendButton.enabled = false;
		NetworkFriends.instance.onFriendRemoved += unfriendPlayerActionCallback;
		NetworkFriends.instance.removeFriend(member);
	}

	public void showConfirmBlockPopup()
	{
		string localizationKey = isFriend ? "are_you_sure_you_want_to_block_{0}" : "are_you_sure_you_want_to_block_non_friend_{0}";
		GenericPopup.showFriendsPopupAtAnchor(
			popupAnchor,
			Localize.text(localizationKey, member.fullName),
			true,
			blockYesCallback);
	}

	private void blockYesCallback(Dict args = null)
	{
		StartCoroutine(buttonWaitingRoutine(blockButton.label));
		blockButton.enabled = false; //disable button
		NetworkFriends.instance.onFriendBlocked += blockedPlayerActionCallback;
		NetworkFriends.instance.blockFriend(member);
	}

	public void showConfirmUnblockPopup()
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "friends",
			phylum: "remove_friend_confirmation",
			klass: "",
			family: "",
			genus: "view");
		
		GenericPopup.showFriendsPopupAtAnchor(
			popupAnchor,
			Localize.text("are_you_sure_you_want_to_unblock_this_player"),
			true,
			unblockYesCallback, unblockNoCallback);
	}

	private void unblockYesCallback(Dict args = null)
	{
		StartCoroutine(buttonWaitingRoutine(unblockButton.label));
		unblockButton.enabled = false;
		NetworkFriends.instance.onFriendUnblocked += unblockPlayerActionCallback;
		NetworkFriends.instance.unblockFriend(member);

		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "friends",
			phylum: "remove_friend_confirmation",
			klass: "yes",
			family: "",
			genus: "click");
	}

	private void unblockNoCallback(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "friends",
			phylum: "remove_friend_confirmation",
			klass: "no",
			family: "",
			genus: "click");
	}
#endregion

	void OnDestroy()
	{
		// This shouldn't cause any problems if they have already been removed,
		// and we want to make sure we dont leave these laying aroud if they close the dialog early.
		NetworkFriends.instance.onFriendBlocked -= blockedPlayerActionCallback;
		NetworkFriends.instance.onFriendUnblocked -= unblockPlayerActionCallback;
		
		NetworkFriends.instance.onInviteReceived -= inviteReceivedCallback;
		NetworkFriends.instance.onNewFriend -= newFriendConnectionCallback;
		
		NetworkFriends.instance.onInviteAccepted -= acceptFriendActionCallback;
		NetworkFriends.instance.onInviteFriend -= addFriendActionCallback;
		
		NetworkFriends.instance.onInviteCancelled -= inviteCancelledActionCallback;
		NetworkFriends.instance.onFriendRemoved -= unfriendPlayerActionCallback;
		if (member != null && member.canSendCreditsTimer != null)
		{
			member.canSendCreditsTimer.removeFunction(onSendCreditsCooldownFinish);
		}
	}
}
