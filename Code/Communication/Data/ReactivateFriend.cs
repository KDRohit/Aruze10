using UnityEngine;
using System.Collections;

// Reactivate Friend Wiki
// https://wiki.corp.zynga.com/display/hititrich/Reactivate+Friend

public class ReactivateFriend : IResetGame
{
	public const string CAROUSEL_ACTION = "reactivate_friend_sender_offer";

	// Server events
	private const string REACTIVATE_FRIEND_OFFER = "reactivate_friend_offer";
	private const string REACTIVATE_FRIEND_INVITED = "reactivate_friend_invited";
	private const string REACTIVATE_FRIEND_SUCCESSFUL = "reactivate_friend_successful";

	// Store it since the dialog can called by carousel.
	public static JSON offerData = null;

	// The coin amount needed to show in carousel.
	public static long rewardAmount = 0;

	public static bool isActive
	{
		get
		{
			return offerData != null;
		}
	}

	public static void registerReactivateFriendDelegates()
	{
		Server.registerEventDelegate(REACTIVATE_FRIEND_OFFER, processOfferEventData);
		Server.registerEventDelegate(REACTIVATE_FRIEND_INVITED, processInvitedEventData);
		Server.registerEventDelegate(REACTIVATE_FRIEND_SUCCESSFUL, processSuccessfulEventData, true);
	}

	private static void processOfferEventData(JSON response)
	{
		// Don't show the dialog if zid is invalid.
		string zid = response.getString("friend_id", "");
		SocialMember member = CommonSocial.findOrCreate(fbid:"", zid:zid);
		if (member == null)
		{
			Debug.LogError("ReactivateFriend.processOfferEventData: zid is invalid.");
			return;
		}

		offerData = response;
		rewardAmount = offerData.getLong("bounty", 0);
		if (SlotsPlayer.isFacebookUser)
		{
			loadAudio();
			ReactivateFriendSenderOfferDialog.showDialog(offerData);

			// Activate the carousel.
			CarouselData slide = CarouselData.findInactiveByAction(CAROUSEL_ACTION);
			if (slide != null)
			{
				slide.activate();
			}
		}
	}

	private static void processInvitedEventData(JSON response)
	{
		// Don't show the dialog if zid is invalid.
		string zid = response.getString("sender", "");
		SocialMember member = CommonSocial.findOrCreate(
			fbid: "",
			zid: zid);
		if (member == null)
		{
			Debug.LogError("ReactivateFriend.processInvitedEventData: zid is invalid.");
			return;
		}

		if (SlotsPlayer.isFacebookUser)
		{
			loadAudio();
			ReactivateFriendReceiverInviteDialog.showDialog(response);
		}
	}

	private static void processSuccessfulEventData(JSON response)
	{
		// Don't show the dialog if zid is invalid.
		string zid = response.getString("recipient", "");
		SocialMember member = CommonSocial.findOrCreate(fbid:"", zid:zid);
		if (member == null)
		{
			Debug.LogError("ReactivateFriend.processSuccessfulEventData: zid is invalid.");
			return;
		}

		if (SlotsPlayer.isFacebookUser)
		{
			loadAudio();
			ReactivateFriendSenderRewardDialog.showDialog(response);
		}
	}

	private static void loadAudio()
	{
		AssetBundleManager.downloadAndCacheBundle("main_snd_reactivate_a_friend", true);
	}

	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		offerData = null;
		rewardAmount = 0;
	}
}
