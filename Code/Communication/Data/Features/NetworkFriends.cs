using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zynga.Core.Util;

public class NetworkFriends : FeatureBase
{
	public static NetworkFriends instance
	{
		get
		{
			return FeatureDirector.createOrGetFeature<NetworkFriends>("network_friends");
		}
	}

	public const int DEFAULT_FRIEND_LIMIT = 450;
	public const int DEFAULT_FRIEND_REQUEST_LIMIT = 150;
	public const int DEFAULT_TOASTER_COOLDOWN = 14400;
	public const int DEFAULT_MAX_SUGGESTED_FRIENDS = 20;
	private const string INVITE_SENT_KEY = "graph_invite_sent";
	private const string INVITE_DECLINE_KEY = "graph_invite_declined";
	private const string INVITE_ACCEPT_KEY = "graph_invite_accepted";
	private const string INVITE_CANCEL_KEY = "graph_invite_cancelled";
	private const string BLOCK_KEY = "graph_player_blocked";
	private const string UNBLOCK_KEY = "graph_player_unblocked";
	private const string REMOVE_KEY = "graph_friend_removed";
	public const string FRIEND_ACTION_AUDIO = "PointsEarnedAlertNetworkAchievements";

	public delegate void SuggestionCallback(List<SocialMember> memberList);
	public delegate void FriendsEventDelegate(SocialMember member, bool didSucceed, int errorCode);
	public delegate void BadgeUpdatedDelegate();

	
	// Events for Action Callbacks
	public event FriendsEventDelegate onInviteFriend;
	public event FriendsEventDelegate onInviteCancelled;
	public event FriendsEventDelegate onInviteDeclined;
	public event FriendsEventDelegate onInviteAccepted;
	public event FriendsEventDelegate onFriendBlocked;
	public event FriendsEventDelegate onFriendUnblocked;
	
	// Events for Server Events (caused by other users)
	public event FriendsEventDelegate onInviteWithdrawn;
	public event FriendsEventDelegate onInviteRejected;
	public event FriendsEventDelegate onFriendRemoved;
	public event FriendsEventDelegate onInviteReceived;
	public event FriendsEventDelegate onNewFriend;

	// Events for badge updating.
	public event BadgeUpdatedDelegate onNewFriendCountUpdated;
	public event BadgeUpdatedDelegate onNewRequestCountUpdated;


	private PreferencesBase prefs = null;
	private const float TOASTER_WAIT_TIME = 1.0f;

	private HashSet<string> seenFriends;
	private HashSet<string> seenRequests;

	private Dictionary<string, int> eventListenerCounts;

	private List<SocialMember> suggestionList;

	public int friendLimit = 450;
	public int pendingRequestLimit = 150;
	public int toasterCooldown = 14400;
	public int sugggestionDisplayLimit = 20;
	
	public int newFriends 
	{
		get; private set;
	}
	public int newFriendRequests
	{
		get; private set;
	}
	
	private bool isWaitingToShowToaster;

	public long lastToasterTime = 0;

	public bool hasNewFriends
	{
		get
		{
			return newFriends > 0; 
		}
	}

	public bool hasNewFriendRequests
	{
		get
		{
			return newFriendRequests > 0;
		}
	}

	public bool isNewFriend(string zid)
	{
		return !seenFriends.Contains(zid);
	}

	public bool isNewFriendRequest(string zid)
	{
		return !seenRequests.Contains(zid);
	}

	private void addEventListener(string key, EventDelegate func)
	{
		if (eventListenerCounts.ContainsKey(key))
		{
			++eventListenerCounts[key];
		}
		else
		{
			eventListenerCounts.Add(key, 1);
			Server.registerEventDelegate(key, func, true);
		}
	}

	public void inviteFriend(SocialMember member)
	{
		addEventListener(INVITE_SENT_KEY, friendInviteCallback);
		NetworkFriendsAction.inviteFriend(member.zId);
	}

	public void rejectFriend(SocialMember member)
	{
		addEventListener(INVITE_DECLINE_KEY, inviteDeclinedCallback);
		NetworkFriendsAction.rejectFriend(member.zId);
	}

	public void acceptFriend(SocialMember member)
	{
		addEventListener(INVITE_ACCEPT_KEY, friendInviteAccepted);
		NetworkFriendsAction.acceptFriend(member.zId);
	}

	public void cancelFriendInvite(SocialMember member)
	{
		addEventListener(INVITE_CANCEL_KEY, inviteCancelledCallback);
		NetworkFriendsAction.cancelPendingRequest(member.zId);
	}

	public void blockFriend(SocialMember member)
	{
		addEventListener(BLOCK_KEY, blockPlayerCallback);
		NetworkFriendsAction.blockFriend(member.zId);
	}

	public void unblockFriend(SocialMember member)
	{
		addEventListener(UNBLOCK_KEY, unblockPlayerCallback);
		NetworkFriendsAction.unblockFriend(member.zId);
	}
	
	public void removeFriend(SocialMember member)
	{
		addEventListener(REMOVE_KEY, removeFriendCallback);
		NetworkFriendsAction.removeFriend(member.zId);
	}

	public void createFeatureInstance()
	{
		// Do nothing we just needed to access this as we spun up our feature director
	}

	protected override void registerEventDelegates()
	{
		Server.registerEventDelegate("graph_invite_received", friendInviteReceieved, true);
		Server.registerEventDelegate("graph_new_friend", newFriendEvent, true);
		Server.registerEventDelegate("graph_invite_withdrawn", inviteWithdrawnEvent, true); // Event when someone cancels and invite to you.
		Server.registerEventDelegate("graph_unfriended", friendRemoved);
		Server.registerEventDelegate("graph_invite_rejected", inviteRejectedEvent, true);

		onFriendRemoved += forgetFriend;
		onInviteAccepted += friendRequestDealtWith;
		onInviteDeclined += friendRequestDealtWith;
		onNewFriend += removeFromSuggestions;
		onFriendBlocked += removeFromSuggestions;
		onInviteAccepted += removeFromSuggestions;
		onInviteFriend += removeFromSuggestions;
	}

	protected override void clearEventDelegates()
	{
		onFriendRemoved -= forgetFriend;
		onInviteAccepted -= friendRequestDealtWith;
		onInviteDeclined -= friendRequestDealtWith;
		onNewFriend -= removeFromSuggestions;
		onFriendBlocked -= removeFromSuggestions;
		onInviteAccepted -= removeFromSuggestions;
		onInviteFriend -= removeFromSuggestions;
	}


	public void parseAndCallEvent(JSON data, FriendsEventDelegate eventName)
	{
		if (null == data)
		{
			string actionName = ((null == eventName)  ? "" : eventName.ToString());
			Debug.LogError("invalid json data for friend action: " + actionName);
			return;
		}
		else if (null == eventName)
		{
			Debug.LogError("Invalid friends delegate");
			return;
		}

		Debug.LogFormat("NetworkFriends.cs -- parseAndCallEvent -- calling {0} with data {1}", eventName.ToString(), data);
		string zid = data.getString("zid", "notyet");
		bool didSucceed = data.getBool("api_success", true);
		SocialMember member = CommonSocial.findOrCreate(zid:zid);
		eventName(member, didSucceed, getErrorCode(data));
	}
	
	/// <summary>
	/// Function to be called when new friends or requests come in
	/// </summary>
	/// <param name="numNewRequests"></param>
	/// <param name="numNewFriends"></param>
	public void onNewFriendData(int numNewRequests, int numNewFriends, string name)
	{
		newFriends += numNewFriends;
		newFriendRequests += numNewRequests;
		if (numNewFriends > 0 && onNewFriendCountUpdated != null)
		{
			onNewFriendCountUpdated();
		}
		if (numNewRequests > 0 && onNewRequestCountUpdated != null)
		{
			onNewRequestCountUpdated();
		}

		scheduleToaster(name);
	}

	public void markFriendSeen(string zid)
	{
		if (!isNewFriend(zid))
		{
			return;
		}
		seenFriends.Add(zid);
		string arrayString = "";
		int index = 0;
		foreach(string seenZId in seenFriends)
		{
			arrayString += (index == 0) ? "" : ",";
			arrayString += seenZId;
			++index;
		}
		prefs.SetString(Prefs.NETWORK_FRIENDS_SEEN_ZIDS, arrayString);
		prefs.Save();
		newFriends--;
		if (newFriends < 0)
		{
			newFriends = 0;
		}
		
		onNewFriendCountUpdated();
	}

	public void onReceieveSuggestions(JSON data, System.Object param)
	{
		/* Example Payload:
			{
				“type”: “player_friends_recommended”,
				“recommendations”: [
					{
						“zid” : 73362248986
						“name” : “Ankush”,
						“photo_url” :		 “https://www.facebook.com/photo.php?fbid=10206552688403682&l=b03af4804b”,
						“achievement_score” : 321456,
						“Vip_level” : 5
					},
				]
		 */


		SuggestionCallback callback = param as SuggestionCallback;
		Server.unregisterEventDelegate("player_friends_recommended");

		// Check if the data is correctly setup.
		JSON[] friends = data.getJsonArray("recommendations", true);
		if (null == friends)
		{
			return;
		}

		// Parse the members and put them into the queue.
		if (suggestionList == null)
		{
			suggestionList = new List<SocialMember>();
		}
		
		SocialMember member = null;

		for (int i = 0; i < friends.Length; ++i)
		{
			if (friends[i] == null)
			{
				continue;
			}
			string zId = friends[i].getString("zid", "");
			string fbid = "";
			string imageURL = friends[i].getString("photo_url", "");
			string name = friends[i].getString("name", "");
			int vipLevel = friends[i].getInt("vip_level", 0);
			long achievementScore = friends[i].getLong("achievement_score", 0);

			fbid = CommonSocial.fbidFromImageUrl(imageURL);

			member = CommonSocial.findOrCreate(
				fbid: fbid,
				zid: zId,
				firstName: name,
				vipLevel: vipLevel,
				achievementScore: achievementScore,
				imageUrl: imageURL);
			
			if (member != null)
			{
				if (member.networkProfile == null)
				{
					// This user will always be a new user, so we need to create their profile to add their imageURL.
					member.networkProfile = new NetworkProfile("", imageURL, achievementScore, member);
				}
				// Update their VIP level even if they arent a new user as we want up-to-date info whenever possible.
				member.vipLevel = vipLevel;
				
				
				if (!SocialMember.allFriends.Contains(member) &&
					!SocialMember.invitedPlayers.Contains(member) &&
					!SocialMember.invitedByPlayers.Contains(member) &&
					!suggestionList.Contains(member))
				{
					// Only add the member to the suggestion queue list if they arent already in one of our friends lists.
					// Also make sure they aren't already in the list as apparently the server sometimes sends us duplicates.
					suggestionList.Add(member);
				}
				
			}
		}

		if (callback != null)
		{
			callback(getSuggestionList());
		}
	}

	public void findSuggestions(SuggestionCallback callback)
	{
		// We either want to return the maximum number of suggested friends,
		// or however many we have left, whichever is less.
		if (suggestionList == null || suggestionList.Count <= 0)
		{
			// If we dont have any left to give, then lets make a call to the server for some more.
			NetworkFriendsAction.findSuggestions(onReceieveSuggestions, (System.Object)callback);
		}
		else
		{
			// If we have some remaining from a previous fetch, use those.
			callback(getSuggestionList());
		}
	}

	private List<SocialMember> getSuggestionList()
	{
		if (suggestionList == null)
		{
			return null;
		}
		int count = Mathf.Min(sugggestionDisplayLimit, suggestionList.Count);		
		List<SocialMember> result = new List<SocialMember>();
		
		for (int i = 0; i < count; i++)
		{
			result.Add(suggestionList[i]);
		}
		for (int i = 0; i < result.Count; i++)
		{
			suggestionList.Remove(result[i]);
		}
		return result;
	}

	private void forgetFriend(SocialMember member, bool didSucceed, int errorCode)
	{
		if (didSucceed && null != member)
		{
			seenFriends.Remove(member.zId);
			System.Text.StringBuilder arrayString = new System.Text.StringBuilder();
			int index = 0;
			foreach(string seenZId in seenFriends)
			{
				arrayString.Append((index == 0) ? "" : ",");
				arrayString.Append(seenZId);
				++index;
			}

			prefs.SetString(Prefs.NETWORK_FRIENDS_SEEN_ZIDS, arrayString.ToString());
			prefs.Save();
		}
	}

	private void removeFromSuggestions(SocialMember member, bool didSucceed, int errorCode)
	{
		//check if event can come through on a reload
		if (this == null)
		{
			return;
		}

		//can be called by multiple events
		
		if (didSucceed && member != null && suggestionList != null)
		{
			//we don't need to check if the list contains the member, this is done in the remove function.
			//it returns true if the item is removed, false if not.  No need to duplicate the condition.
			suggestionList.Remove(member);
		}
		
		
	}
	public void markRequestSeen(string zid)
	{
		if (!isNewFriendRequest(zid))
		{
			return;
		}
		seenRequests.Add(zid);
		string arrayString = "";
		int index = 0;
		foreach(string seenZId in seenRequests)
		{
			arrayString += (index == 0) ? "" : ",";
			arrayString += seenZId;
			++index;
		}

		prefs.SetString(Prefs.NETWORK_FRIEND_REQUESTS_SEEN_ZIDS, arrayString);
		prefs.Save();
		newFriendRequests--;
		if (newFriendRequests < 0)
		{
			newFriendRequests = 0;
		}
		onNewRequestCountUpdated();
	}

	public void recalculateNewCounts()
	{
		// MCC -- Adding this functionality to cover any race conditions that might occur that could corrupt this number.
		// If the dialogs detect that the badge number doesnt seem right, it will call this to recalculate, and then redisplay.
		int i = 0;
		SocialMember member;
		int count = 0;
		for (i = 0; i < SocialMember.allFriends.Count; i++)
		{
			member = SocialMember.allFriends[i];
			if (member != null && isNewFriend(member.zId))
			{
				count++;
			}
		}
		newFriends = count;
		onNewFriendCountUpdated();

		count = 0;
		for (i = 0; i < SocialMember.invitedByPlayers.Count; i++)
		{
			member = SocialMember.invitedByPlayers[i];
			if (member != null && isNewFriendRequest(member.zId))
			{
				count++;
			}
		}
		newFriendRequests = count;
		if (onNewRequestCountUpdated != null)
		{
			onNewRequestCountUpdated();
		}

	}
	
	private void friendRequestDealtWith(SocialMember member, bool didSucceed, int errorCode)
	{
		// When we have accepted or rejected a request, remove it from the seen list.
		if (didSucceed && null != member)
		{
			seenRequests.Remove(member.zId);
			System.Text.StringBuilder arrayString = new System.Text.StringBuilder();
			int index = 0;
			foreach(string seenZId in seenRequests)
			{
				arrayString.Append((index == 0) ? "" : ",");
				arrayString.Append(seenZId);
				++index;
			}

			prefs.SetString(Prefs.NETWORK_FRIEND_REQUESTS_SEEN_ZIDS, arrayString.ToString());
			prefs.Save();
		}
	}
	
	private IEnumerator showFriendsToasterRoutine(string friendName)
	{
		yield return new WaitForSeconds(TOASTER_WAIT_TIME);

		Dict args = Dict.create(D.OPTION,  friendName);
		ToasterManager.addToaster(ToasterType.NETWORK_FRIENDS, args);
	}

	public void onToasterClose()
	{
		lastToasterTime = System.DateTime.UtcNow.Ticks;
		isWaitingToShowToaster = false;
	}

	private bool scheduleToaster(string name)
	{
		if (isWaitingToShowToaster || !isToasterValidToSurface(null))
		{
			return false;
		}

		isWaitingToShowToaster = true;
		RoutineRunner.instance.StartCoroutine(showFriendsToasterRoutine(name));

		return true;
	}

	/// <summary>
	/// function used to enforce 1 toaster cooldown. This is called as part of an isvalid call right before toaster surfaces
	/// </summary>
	/// <param name="answerArgs"></param>
	/// <returns></returns>
	private bool isToasterValidToSurface(Dict answerArgs)
	{
		return 0 >= (NetworkFriends.instance.lastToasterTime + (new System.TimeSpan(0, 0, NetworkFriends.instance.toasterCooldown)).Ticks - System.DateTime.UtcNow.Ticks);
	}

	private void friendRemoved(JSON data)
	{
		parseAndCallEvent(data, onFriendRemoved);
	}

	private void removeEventListener(string key, EventDelegate func)
	{
		int remainingCalls = 0;
		if (eventListenerCounts.ContainsKey(key))
		{
			remainingCalls = eventListenerCounts[key] -1;
			eventListenerCounts[key] = remainingCalls;
		}

		if (remainingCalls <= 0)
		{
			Server.unregisterEventDelegate(key, func, true);
			eventListenerCounts.Remove(key);
		}
	}

	private void friendInviteCallback(JSON data)
	{
		// NOTE this player could already be in the requests list if invited while offline.
		parseAndCallEvent(data, onInviteFriend);
		removeEventListener(INVITE_SENT_KEY, friendInviteCallback);
	}

	private void friendInviteReceieved(JSON data)
	{
		onNewFriendData(1, 0, "");
		parseAndCallEvent(data, onInviteReceived);
	}

	private void friendInviteAccepted(JSON data)
	{
		parseAndCallEvent(data, onInviteAccepted);
		removeEventListener(INVITE_ACCEPT_KEY, friendInviteAccepted);
	}

	private void newFriendEvent(JSON data)
	{
		onNewFriendData(0, 1, "");
		parseAndCallEvent(data, onNewFriend);
	}

	private void inviteDeclinedCallback(JSON data)
	{
		parseAndCallEvent(data, onInviteDeclined);
		removeEventListener(INVITE_DECLINE_KEY, inviteDeclinedCallback);	
		
	}

	private void inviteCancelledCallback(JSON data)
	{
		parseAndCallEvent(data, onInviteCancelled);
		removeEventListener(INVITE_CANCEL_KEY, inviteCancelledCallback);
	}
	
	private void inviteWithdrawnEvent(JSON data)
	{
		parseAndCallEvent(data, onInviteWithdrawn);

	}

	private void inviteRejectedEvent(JSON data)
	{
		parseAndCallEvent(data, onInviteRejected);
	}	

	public void blockPlayerCallback(JSON data)
	{
		parseAndCallEvent(data, onFriendBlocked);
		removeEventListener(BLOCK_KEY, blockPlayerCallback);
		
	}

	public void unblockPlayerCallback(JSON data)
	{
		parseAndCallEvent(data, onFriendUnblocked);
		removeEventListener(UNBLOCK_KEY, unblockPlayerCallback);
	}

	private void removeFriendCallback(JSON data)
	{
		parseAndCallEvent(data, onFriendRemoved);
		removeEventListener(REMOVE_KEY, removeFriendCallback);
		
	}

	private int getErrorCode(JSON data)
	{

		if (null == data)
		{
			return 0;
		}

		int errorCode = data.getInt("error_code", 0);
		switch (errorCode)
		{
			case 0: /* NONE */
				break;
			
			case 1: /* API_FAILURE */
				// Sent when GraphService fails for a request that otherwise should succeed	
				Debug.LogError("NetworkFriends: Graph service api failure");
				break;

			case 2: /* SELF_FRIEND_LIMIT */
				// Sent when the request failed due to the sending player not having room on the friends list
				Debug.LogWarning("Network Friends: User has too many friends");
				showFriendsLimitPopupYours();
				break;

			case 3: /* OTHER_FRIEND_LIMIT */
				// Sent when the request failed due to the receiving player not having room on the friends list
				Debug.LogWarning("NetworkFriends: player has too many friends");
				showFriendsLimitPopupTheirs(data);
				break;

			case 4: /* INVALID_REQUEST */
				// Sent when the request is acting on a resource (invite/player/etc) that does not exist
				Debug.LogError("NetworkFriends: Resource doesn't exist");
				break;	

			default:
				errorCode = 0;
				Debug.LogError("Invalid error code received");
				break;
		}

		return errorCode;
	}


	// These can happen outside of the dialog so we need to attach them to the overlay instead of the dialog.
	public void showFriendsLimitPopupYours()
	{	
		Transform t = getPopupTransform();
		if (t == null)
		{
			Debug.LogError("Can't show popup - no anchor");
			return;
		}
		GenericPopup.showFriendsPopupAtAnchor(t, Localize.text("friend_limit_you"), false, friendsLimitStatFire, friendsLimitStatFire, Dict.create(D.PLAYER, SlotsPlayer.instance.socialMember));
	}

	public Transform getPopupTransform()
	{
		if (NetworkProfileDialog.instance != null)
		{
			return NetworkProfileDialog.instance.getPopupAnchor();
		}
		else if (Overlay.instance.popupAnchor != null)
		{
			return Overlay.instance.popupAnchor;
		}
		else
		{
			return Overlay.instance.transform;
		}
	}
	
	public void showFriendsLimitPopupTheirs(JSON data)
	{
		Transform t = getPopupTransform();
		if (t == null)
		{
			Debug.LogError("Can't show popup - no anchor");
			return;
		}

		string zid = data.getString("zid", "notyet");
		SocialMember member = CommonSocial.findOrCreate(zid:zid);
		
		GenericPopup.showFriendsPopupAtAnchor(t, Localize.text("friend_limit_them"), false, friendsLimitStatFire, friendsLimitStatFire, Dict.create(D.PLAYER, member));
	}
	
	public void showRequestsLimitPopupYours()
	{
		Transform t = getPopupTransform();
		if (t == null)
		{
			Debug.LogError("Can't show popup - no anchor");
			return;
		}
		GenericPopup.showFriendsPopupAtAnchor(t, Localize.text("you_have_reached_request_limit_{0}", NetworkFriends.instance.friendLimit), false, requestsLimitStatsFire, requestsLimitStatsFire, Dict.create(D.PLAYER, SlotsPlayer.instance.socialMember));
	}
	
	public void showRequestsLimitPopupTheirs(SocialMember member)
	{
		Transform t = getPopupTransform();
		if (t == null)
		{
			Debug.LogError("Can't show popup - no anchor");
			return;
		}
		GenericPopup.showFriendsPopupAtAnchor(t, Localize.text("friend_has_reached_request_limit_{0}_{1}", member.fullName, NetworkFriends.instance.friendLimit), false, requestsLimitStatsFire, requestsLimitStatsFire, Dict.create(D.PLAYER, member));
	}

	private void friendsLimitStatFire(Dict args = null)
	{
		SocialMember member = (SocialMember)args.getWithDefault(D.PLAYER, null);
		
		string klass = (member != null && member.isUser) ? "self" : "other";
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "friends",
			phylum: "friends_cap",
			klass: klass,
			family: member == null ? "" : member.zId,
			genus: "view");
	}

	private void requestsLimitStatsFire(Dict args = null)
	{
		SocialMember member = (SocialMember)args.getWithDefault(D.PLAYER, null);
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "friends",
			phylum: "requests_cap",
			klass: member == null ? "" : member.zId,
			family: "view",
			genus: "");
	}

#region dev_gui_actions
	public void fakeAddFriend(JSON data)
	{
		newFriendEvent(data);
	}

	public void fakeInvitedByFriend(JSON data)
	{
		friendInviteReceieved(data);
	}

	public void clearSeenRequests()
	{
		prefs.SetString(Prefs.NETWORK_FRIEND_REQUESTS_SEEN_ZIDS, "");
		prefs.Save();
		seenRequests = new HashSet<string>();		
	}

	public void clearSeenFriends()
	{
		prefs.SetString(Prefs.NETWORK_FRIENDS_SEEN_ZIDS, "");
		prefs.Save();
		seenFriends = new HashSet<string>();
	}
#endregion

#region feature_base_overrides

	public override  bool isEnabled
	{
		get
		{
			return base.isEnabled && ExperimentWrapper.CasinoFriends.isInExperiment;
		}
	}
	
	protected override void initializeWithData(JSON data)
	{
		eventListenerCounts = new Dictionary<string, int>();
		prefs = SlotsPlayer.getPreferences();

		// Populate the seen friends array.
		string seenZIdsString = prefs.GetString(Prefs.NETWORK_FRIENDS_SEEN_ZIDS, "");
		string[] seenZIds = seenZIdsString.Split(',');
		seenFriends = new HashSet<string>(seenZIds);

		// Now grab the requests.
		seenZIdsString = prefs.GetString(Prefs.NETWORK_FRIEND_REQUESTS_SEEN_ZIDS, "");
		seenZIds = seenZIdsString.Split(',');
		seenRequests = new HashSet<string>(seenZIds);
	}
#endregion
}
