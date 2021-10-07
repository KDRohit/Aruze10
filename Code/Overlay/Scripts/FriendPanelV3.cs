using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls display of a friend panel.
*/

public class FriendPanelV3 : TICoroutineMonoBehaviour
{
	public AnimationListController.AnimationInformationList notifyAnimations;

	public FacebookFriendInfo friendInfo;
	public GameObject selfBackground;
	public GameObject friendBackground;
	public GameObject fbSend;
	public GameObject sendGiftButton;
	public GameObject sendGiftThanks;
	public GameObject profileButton;
	public GameObject coin;
	public MasterFader fader;

	public ButtonHandler sendGiftLargeCollider; // Used when there are no profiles so the collider is the size of the whole panel.
	public ButtonHandler sendGiftSmallCollider; // Used when profiles are on

	public ButtonHandler openProfileSmallCollider; // Opens the user's profile. (Used when gifting is also available)
	public ButtonHandler openProfileLargeCollider; // Opens the user's profile.

	private SocialMember _member = null;

	public void init(SocialMember member, int rank)
	{
		_member = member;
		friendInfo.rank = rank;
		if (ExperimentWrapper.HyperEconomy.isShowingRepricedVisuals)
		{
			SafeSet.gameObjectActive(friendInfo.creditsTMPro.gameObject, Data.liveData.getBool("DISPLAY_COIN_VALUES_FRIENDS_BAR", true));
		}
		resetGiftNotification();

		refresh();
	}

	// returns true if last time mfsdialog was open player associated with this panel got sent a gift
	public bool shouldPlayGiftAnimation
	{
		get 
		{
			return (friendInfo != null && friendInfo.member != null && friendInfo.member.shouldPlayGiftAnimation);
		}
		set
		{
			if (friendInfo != null && friendInfo.member != null)
			{
				friendInfo.member.shouldPlayGiftAnimation = false;
			}		
		}
	}

	public void playNotifyAnimations()
	{
		StartCoroutine(AnimationListController.playListOfAnimationInformation(notifyAnimations));
	}

	/// Set the gifting button based on whether this friend is giftable
	public void resetGiftNotification()
	{
		SafeSet.gameObjectActive(selfBackground, _member.isUser);
		SafeSet.gameObjectActive(friendBackground, !_member.isUser);
		SafeSet.gameObjectActive(fbSend, !_member.isUser);
		SafeSet.gameObjectActive(profileButton, _member.isUser);

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

	// sets panel Send Gift button active for member unless it is the user
	private void activateGiftNotification()
	{
		if (_member.isUser)
		{
			// Can't send gifts to yourself, so don't activate it for the current player.
			sendGiftSmallCollider.isEnabled = false;
			sendGiftLargeCollider.isEnabled = false;
			
			openProfileSmallCollider.enabled = false;
			openProfileLargeCollider.enabled = NetworkProfileFeature.instance.isEnabled;
			SafeSet.gameObjectActive(sendGiftButton, false);
			SafeSet.gameObjectActive(coin, false);
			SafeSet.gameObjectActive(sendGiftThanks, false);			
			return;
		}

		SafeSet.gameObjectActive(sendGiftButton, true);
		SafeSet.gameObjectActive(coin, true);
		SafeSet.gameObjectActive(sendGiftThanks, false);

		if (NetworkProfileFeature.instance.isEnabled)
		{
			sendGiftLargeCollider.isEnabled = false;
			openProfileLargeCollider.enabled = true;
			sendGiftSmallCollider.isEnabled = true;
			openProfileSmallCollider.enabled = false;
		}
		else
		{
			sendGiftLargeCollider.isEnabled = true;
			sendGiftSmallCollider.isEnabled = false;
			openProfileSmallCollider.enabled = false;
			openProfileLargeCollider.enabled = false;
		}
	}
		
	private void deactivateGiftNotification()
	{
		SafeSet.gameObjectActive(sendGiftButton, false);
		SafeSet.gameObjectActive(coin, false);

		// Turn off all gift colliders and the small profile colliders as these aren't applicable now.
		sendGiftSmallCollider.isEnabled = false;
		sendGiftLargeCollider.isEnabled = false;
		openProfileSmallCollider.enabled = NetworkProfileFeature.instance.isEnabled;

		SafeSet.gameObjectActive(profileButton, NetworkProfileFeature.instance.isEnabled);
		SafeSet.gameObjectActive(sendGiftThanks, !NetworkProfileFeature.instance.isEnabled && !_member.isUser);

		
		// If network is enabled, turn on the large collider
		openProfileLargeCollider.enabled = NetworkProfileFeature.instance.isEnabled;
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

	public void recycle()
	{
		if (fader != null)
		{
			fader.setAlpha(1.0f);
		}
		// recylce it so it can be reused
		friendInfo.recycle();

		_member = null;
	}

	// Used by ListScroller as iTween ValueTo callback.
	public void setAlpha(float alpha)
	{
		fader.setAlpha(alpha);
	}
}
