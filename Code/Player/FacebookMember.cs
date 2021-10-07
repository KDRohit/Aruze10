using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Facebook.Unity;

/**
Data structure for holding information about facebook members.
*/

public class FacebookMember : SocialMember
{
	public const int LARGE_IMAGE_SIZE = 256;

	public override bool isFacebookConnected
	{
		get
		{
			return true;
		}
	}
	
	public override string fullName
	{
		get
		{
			if ((SlotsPlayer.isAnonymous && isUser) || firstName == BLANK_USER_NAME)
			{
				// For whatever reason, when in anonymous mode, the FacebookMember class is used for the current player.
				return anonymousNonFriendName;
			}

			if (firstName.IsNullOrWhiteSpace() && lastName.IsNullOrWhiteSpace())
			{
				firstName = anonymousNonFriendName;
			}
			
			return firstName + " " + lastName;
		}
	}
	
	public override string firstNameLastInitial
	{
		get
		{
			if ((SlotsPlayer.isAnonymous && isUser) || firstName == BLANK_USER_NAME)
			{
				// For whatever reason, when in anonymous mode, the FacebookMember class is used for the current player.
				return anonymousNonFriendName;
			}
			
			if (firstName.IsNullOrWhiteSpace() && lastName.IsNullOrWhiteSpace())
			{
				firstName = anonymousNonFriendName;
			}
			
			return CommonText.firstNameLastInitial(firstName, lastName);
		}
	}

	public FacebookMember(
		string id = "",
			string zId = "",
			string nid = "",
			string firstName = "",
			string lastName = "",
			int vipLevel = 0,
			string imageUrl = "") :
		base(id: id,
			zId: zId,
			nid: nid,
			firstName: firstName,
			lastName: lastName,
			vipLevel: vipLevel,
			imageUrl: imageUrl)
	{
		if (string.IsNullOrEmpty(photoSource.getUrl(PhotoSource.Source.FB)))
		{
			// If we didn't get a facebook image coming down in data, then generate a facebook url here.
			photoSource.setUrl(getImageUrl(id), PhotoSource.Source.FB);
		}
	}

	public FacebookMember(JSON member, bool isGamePlayer, bool isInvitableFriend = false) :
		base(member:member,
			isGamePlayer:isGamePlayer,
			isInvitableFriend: isInvitableFriend)
	{
		if (!isInvitableFriend)
		{
			if (Glb.switchFbCall)
			{
				string imageURL = member.getString("photo_url", "");
				string imageSize = member.getString("photo_size", "");
				int imageSizeInt = 0;

				if (Int32.TryParse(imageSize, out imageSizeInt))
				{
					if (imageSizeInt >= 100)
					{
						photoSource.setUrl(imageURL, PhotoSource.Source.FB_LARGE);
					}
				}
			}
			else 
			{
				photoSource.setUrl(getImageUrl(id), PhotoSource.Source.FB);
				photoSource.setUrl(getImageUrl(id, LARGE_IMAGE_SIZE), PhotoSource.Source.FB_LARGE);
			}
		}
	}

	// Creates the FacebookMember object for the current player.
	public static void createPlayerFacebookMember()
	{
		JSON login = Data.login;	// Shorthand.
		// Create the FacebookMember object that represents the current user.
		FacebookMember member = new FacebookMember(
			id:login.getString("init.fb_id", "-1"),
			zId:login.getString("player.id", ""),
			nid:SlotsPlayer.instance.networkID);
		SlotsPlayer.instance.socialMember = member;
		member.isUser = true;
		member.isGamePlayer = true;
		member.firstName = login.getString("player.first_name", SocialMember.BLANK_USER_NAME);
		string[] name = member.firstName.Split(' ');
		member.firstName = name[0];
		if (name.Length > 1)
		{
			member.lastName = name[1];
		}
		else
		{
			member.lastName = login.getString("player.last_name", SocialMember.BLANK_USER_NAME);
		}
		member.xp = login.getLong("player.experience", 0);
		member.experienceLevel = login.getInt("player.experience_level", 1);
		member.credits = login.getLong("player.credits", 0);
		addToListWithoutDuplicates(friendPlayersAndMe, member);
		if (string.IsNullOrEmpty(member.id) == false && member.id != "0")
		{
			StatsManager.Instance.LogAssociate("fb_connect", Zynga.Slots.ZyngaConstantsGame.deviceAdvertisingID, member.id);
			Bugsnag.AddToTab("HIR", "fbid", SlotsPlayer.instance.facebook.id);
		}
		Debug.Log(string.Format("Created player fb member: {0}, {1}, {2}", member.firstName, member.lastName, member.credits));
	}

	// Populate all friends list (not including the current player's FacebookMember object).
	public static new void populateAll(JSON jsonData)
	{
		if (jsonData == null)
		{
			Debug.LogWarning("FacebookMember.populateAll() was provided null jsonData");
			return;
		}
				
		JSON[] activeFriendsData = jsonData.getJsonArray("active_friends");
		
		if (activeFriendsData == null)
		{
			Debug.LogWarning("FacebookMember.populateAll() has no active_friends data");
		}
		else
		{
			// Build the lists of friends that play this game.
			foreach (JSON friend in activeFriendsData)
			{
				FacebookMember member = new FacebookMember(friend, true);
				addToListWithoutDuplicates(friendPlayers, member);
				addToListWithoutDuplicates(friendPlayersAndMe, member);
				addToListWithoutDuplicates(allFriends, member);
			}
		}

		JSON[] invitableFriendsData = jsonData.getJsonArray("invitable_friends");
		if (invitableFriendsData == null)
		{
			Debug.LogWarning("FacebookMember.populateAll() has no invitable_friends data");
		}
		else
		{
			// Build the lists of friends that DON'T play this game.
			foreach (JSON friend in invitableFriendsData)
			{
				FacebookMember member = new FacebookMember(friend, false, true);
				addToListWithoutDuplicates(friendsNonPlayers, member);
				addToListWithoutDuplicates(allFriends, member);
			}
		}
		
		isFriendsPopulated = true;
		
		if (MainLobby.instance != null)
		{
			MainLobby.instance.refreshFriendsList();
		}

		if (StatsManager.Instance != null)
		{
			if (SlotsPlayer.instance != null && SlotsPlayer.instance.socialMember != null)
			{
				StatsManager.Instance.LogCount("start_session","level", "", "", "", "", SlotsPlayer.instance.socialMember.experienceLevel);
			}

			if (SocialManager.IsFacebookAuthenticated)
			{
				if (SocialMember.friendsNonPlayers != null)
				{
					StatsManager.Instance.LogMileStone("num_sn_friends", SocialMember.friendsNonPlayers.Count);	//total fb friends
				}
				if (SocialMember.friendPlayers != null)
				{
					StatsManager.Instance.LogMileStone("num_sn_neighbors", SocialMember.friendPlayers.Count);	//friends who play
				}
			}
		}

		if (NetworkProfileFeature.instance.isEnabled)
		{
			NetworkProfileFeature.instance.queueDownloadProfiles(SocialMember.friendPlayers);
		}

		SocialMember.hasInitialized = true;
	}
	// This is a separate function since we sometimes need to load non-friend facebook images.
	public static string getImageUrl(string id, int size = 100)
	{
		if (string.IsNullOrEmpty(id) || id == "-1" || id == "notyet")
		{
			// If this is not a valid fbid, don't try to get the photo.
			return "";
		}
		return string.Format("https://graph.facebook.com/{0}{1}/picture?height={2}&width={2}&access_token={3}", Zynga.Zdk.ZyngaConstants.FbGraphVersion, id, size, AccessToken.CurrentAccessToken?.TokenString ?? "");
	}

	public static void resetStaticClassData()
	{
	}
}
