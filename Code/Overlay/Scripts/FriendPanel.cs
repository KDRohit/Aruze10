using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls display of a friend panel.
*/

public class FriendPanel : TICoroutineMonoBehaviour
{
	public FacebookFriendInfo friendInfo;
	public GameObject selfBackground;
	public GameObject friendBackground;
	public GameObject fbSend;
	public GameObject sendGiftButton;
	public GameObject sendGiftThanks;
	public GameObject coin;
	public MasterFader fader;

	public ButtonHandler sendGiftLargeCollider; // Used when there are no profiles so the collider is the size of the whole panel.
	public ButtonHandler sendGiftSmallCollider; // Used when profiles are on

	public ClickHandler openProfileSmallCollider; // Opens the user's profile. (Used when gifting is also available)
	public ClickHandler openProfileLargeCollider; // Opens the user's profile.


	private SocialMember _member = null;

	public void init(SocialMember member, int rank)
	{
		_member = member;
		if (ExperimentWrapper.HyperEconomy.isShowingRepricedVisuals)
		{
			SafeSet.gameObjectActive(friendInfo.creditsTMPro.gameObject, Data.liveData.getBool("DISPLAY_COIN_VALUES_FRIENDS_BAR", true));
		}
		friendInfo.rank = rank;

		resetGiftNotification();

		refresh();
	}

	/// Set the gifting button based on whether this friend is giftable.
	public void resetGiftNotification()
	{
		SafeSet.gameObjectActive(selfBackground, _member.isUser);
		SafeSet.gameObjectActive(friendBackground, !_member.isUser);
		SafeSet.gameObjectActive(fbSend, !_member.isUser);

		if (_member.canSendCredits)
		{
			activateGiftNotification();
		}
		else
		{
			deactivateGiftNotification();
		}
	}

	public void refresh()
	{
		if (friendInfo != null)
		{
			friendInfo.member = _member;
			sendGiftSmallCollider.registerEventDelegate(sendGiftClicked);
			sendGiftLargeCollider.registerEventDelegate(sendGiftClicked);
			openProfileSmallCollider.registerEventDelegate(openProfileClicked);
			openProfileLargeCollider.registerEventDelegate(openProfileClicked);
		}
		else
		{
			Debug.LogWarning("FriendPanel Refresh Failed");
		}
	}

	private void activateGiftNotification()
	{
		if (_member.isUser)
		{
			// Can't send gifts to yourself, so don't activate it for the current player.
			sendGiftSmallCollider.isEnabled = false;
			sendGiftLargeCollider.isEnabled = false;
			
			openProfileSmallCollider.isEnabled = false;
			openProfileLargeCollider.isEnabled = NetworkProfileFeature.instance.isEnabled;
			return;
		}

		SafeSet.gameObjectActive(sendGiftButton, true);
		SafeSet.gameObjectActive(coin, true);
		SafeSet.gameObjectActive(sendGiftThanks, false);

		if (NetworkProfileFeature.instance.isEnabled)
		{
			sendGiftLargeCollider.isEnabled = false;
			openProfileLargeCollider.isEnabled = false;
			sendGiftSmallCollider.isEnabled = true;
			openProfileSmallCollider.isEnabled = true;
		}
		else
		{
			sendGiftLargeCollider.isEnabled = true;
			sendGiftSmallCollider.isEnabled = false;
			openProfileSmallCollider.isEnabled = false;
			openProfileLargeCollider.isEnabled = false;
		}
	}

	private void deactivateGiftNotification()
	{
		SafeSet.gameObjectActive(sendGiftButton, false);
		SafeSet.gameObjectActive(coin, false);
		SafeSet.gameObjectActive(sendGiftThanks, !_member.isUser);

		// Turn off all gift colliders and the small profile colliders as these aren't applicable now.
		sendGiftSmallCollider.isEnabled = false;
		sendGiftLargeCollider.isEnabled = false;
		openProfileSmallCollider.isEnabled = false;
		
		// If network is enabled, turn on the large collider
		openProfileLargeCollider.isEnabled = NetworkProfileFeature.instance.isEnabled;
	}

	private void sendGiftClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("bottom_nav", "overlay", "friends", "friend_thumbnail", "", "click");
		MFSDialog.showDialog(MFSDialog.Mode.CREDITS, _member);
	}

	private void openProfileClicked(Dict args = null)
	{
		if (friendInfo != null)
		{
			NetworkProfileDialog.showDialog(friendInfo.member);
		}
		else
		{
			Debug.LogErrorFormat("FriendPanel.cs -- openProfileClicked -- tried to click on an incorrect friend panel without a friendInfo");
		}

	}

	// Used by ListScroller as iTween ValueTo callback.
	public void setAlpha(float alpha)
	{
		fader.setAlpha(alpha);
	}
}
