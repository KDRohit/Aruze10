using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Facebook.Unity;
using Random = UnityEngine.Random;

public class SocialMember : IResetGame
{
#region static_variables
	//user name used for oneself
	public const string BLANK_USER_NAME = "You";
	// List of ALL friends.
	public static List<SocialMember> allFriends = new List<SocialMember>();
	public static List<SocialMember> invitedPlayers = new List<SocialMember>();
	public static List<SocialMember> invitedByPlayers = new List<SocialMember>();
	public static List<SocialMember> blockedPlayers = new List<SocialMember>();

	// List of friends that have played this game.
	public static List<SocialMember> friendPlayers = new List<SocialMember>();

	// List of friends that have not played this game.
	public static List<SocialMember> friendsNonPlayers = new List<SocialMember>();

	// List of friends playing, including the current user.
	public static List<SocialMember> friendPlayersAndMe = new List<SocialMember>();

	// List of friends that are sorted from the server-side.
	public static List<SocialMember> sortedFriends = new List<SocialMember>();

	// List of all friends, indexed by fbId for fast lookup using findFacebookFriend();
	protected static Dictionary<string, SocialMember> _allIndexed = new Dictionary<string, SocialMember>();
	protected static Dictionary<string, SocialMember> _allIndexedByZId = new Dictionary<string, SocialMember>();

	protected static Dictionary<string, SocialMember> _allIndexedByNetworkId = new Dictionary<string, SocialMember>();

	public static bool isFriendsPopulated = false;	// Whether the friends data has been populated.
	public static bool hasInitialized = false;

	public static List<JSON> populatedJson = null;

	private static List<SocialMember> blankImageMembers = new List<SocialMember>();

	public static Dictionary<string, SocialMember> allIndexedByZId
	{
		get
		{
			return _allIndexedByZId;
			}
	}
#endregion

	public long xp;
	public long credits = 0;

	public int experienceLevel = 1;

	// Ranking for the new MFS Display Sorting.
	public int mfsAllFriendsSortRank = 0;
	public int mfsLikesGamesSortRank = 0;
	public int mfsGameFriendsSortRank = 0;

	public string id;
	public string zId;
	public string firstName = "";
	public string lastName = "";
	public string fbFirstName = "";
	public string fbLastName = "";

	// This now stores all of the imageURLs and returns the valid one.
	private PhotoSource _photoSource;
	public PhotoSource photoSource
	{
		get
		{
			if (_photoSource == null)
			{
				_photoSource = new PhotoSource(this);
			}
			return _photoSource;
		}
	}

	public string playedGame; // This is the secondary game that the user plays.
	public bool isUser = false;			/// True if this member object represents the current user.
	public bool isChecked = false;		/// True if this member is checked on a dialog list.
	public bool shouldPlayGiftAnimation = false;		/// True if there should se some sort of gifting animation in UI for this user
	public bool isFemale = true;		/// Determines whether male or female. If no specified, defaults to female.

	public bool likesGames = false;
	public bool canSendCredits = false;
	public bool canChallenge = false;	// This is obsolete.
	public bool canSendBonus = false;
	public bool canSendInvite = false;
	public bool canAskForCredits = false;
	public bool canSendCharms = false;
	public bool isGamePlayer = false;	/// True if this member is a Hit It Rich! player already.
	public int questCollectibles = 0;
	public int weeklyRaceDivision = 0;

	public GameTimerRange canSendCreditsTimer = null;
	public GameTimerRange canSendBonusTimer = null;
	public GameTimerRange canSendInviteTimer = null;
	public GameTimerRange canAskForCreditsTimer = null;

	public virtual bool isFacebookConnected
	{
		get; private set;
	}

	public virtual bool isFacebookFriend
	{
		get
		{
			// Server no longer sends down fb_id if the user is not facebook friends with someone
			// so this is how we tell if they are facebook friends.
			return SocialMember.allFriends != null && SocialMember.allFriends.Contains(this) && !string.IsNullOrEmpty(id) && id != "-1" && !isUser;
		}
	}

	public bool isNetworkFriend
	{
		get
		{
			return friendPlayers != null && friendPlayers.Contains(this);
		}
	}

	public bool isBlocked
	{
		get
		{
			return blockedPlayers != null && blockedPlayers.Contains(this);
		}
	}

	public bool isInvited
	{
		get
		{
			return invitedPlayers != null && invitedPlayers.Contains(this);
		}
	}

	public bool hasInvitedPlayer
	{
		get
		{
			return invitedByPlayers != null && invitedByPlayers.Contains(this);
		}
	}

	public bool isLLConnected = false;

	private Dictionary<ScoreType, long> scoreMap = new Dictionary<ScoreType, long>();

	public SocialMember(
		string id,
		string zId,
		string nid = "",
		string firstName = "",
		string lastName = "",
		int vipLevel = 0,
		string imageUrl = "")
	{
		this.id = id;
		this.zId = zId;
		this.firstName = firstName;
		this.lastName = lastName;
		this.vipLevel = vipLevel;
		if (!string.IsNullOrEmpty(imageUrl))
		{
			photoSource.setUrl(imageUrl, PhotoSource.Source.PROFILE);
		}
		isFacebookConnected = !string.IsNullOrEmpty(id) && "-1" != id;
		if (Application.isPlaying)
		{
		// MCC -- Wrapping this is Application.isPlaying because this borks UnitTests at the moment.
		// TODO we also want to check gifting activation so figure out how to make this work.
			if (CommonSocial.isValidId(zId))
			{
				activateGifting();
			}
		}
		indexUser(zId, id, this);
	}	

	public SocialMember(JSON member, bool isGamePlayer, bool isInvitableFriend = false)
	{
		if (isInvitableFriend)
		{
			this.isGamePlayer = isGamePlayer;
			id = member.getString("id", "");
			JSON pic = member.getJSON("picture");
			if (pic != null)
			{
				JSON data = pic.getJSON("data");
				if (data != null)
				{
					photoSource.setUrl(data.getString("url", ""), PhotoSource.Source.FB);
				}
			}
			else
			{
				photoSource.setUrl(member.getString("photo_url", ""), PhotoSource.Source.FB);
			}
			firstName = member.getString("first_name", "");
			lastName = member.getString("last_name", "");
			likesGames = member.getBool("likes_games", true);
			canSendCredits = member.getBool("can_send_credits", true);
			canSendBonus = member.getBool("can_send_gift_bonus", true);
			canSendInvite = member.getBool("can_invite", true);
			canAskForCredits = member.getBool("can_ask_for_credits", true);
			canSendCharms = member.getBool("can_send_charm", true);
			zId = "";
			xp = member.getLong("experience", 0);
			experienceLevel = member.getInt("experience_level", 1);
			vipLevel = member.getInt("vip_level", 0);
			credits = member.getLong("credits", 0);
			playedGame = member.getString("other_games", "");
			mfsAllFriendsSortRank = member.getInt("mfs_all_friends", 0);
			mfsLikesGamesSortRank = member.getInt("mfs_likes_games", 0);
			mfsGameFriendsSortRank = member.getInt("mfs_game_friends", 0);

			//TODO: Verify these against server data
			isFacebookConnected = member.getBool("fb_connected", false);
			isLLConnected = member.getBool("ll_connected", false);
		}
		else
		{
			this.isGamePlayer = isGamePlayer;
			firstName = member.getString("first_name", "");
			lastName = member.getString("last_name", "");
			likesGames = member.getBool("likes_games", false);
			canSendCredits = member.getBool("can_send_credits", false);
			canSendBonus = member.getBool("can_send_gift_bonus", false);
			canSendInvite = member.getBool("can_invite", false);
			canAskForCredits = member.getBool("can_ask_for_credits", false);
			canSendCharms = member.getBool("can_send_charm", false);
			id = member.getString("fb_id", "");
			zId = member.getString("id", "");
			xp = member.getLong("experience", 0);
			experienceLevel = member.getInt("experience_level", 1);
			vipLevel = member.getInt("vip_level", 0);
			credits = member.getLong("credits", 0);
			playedGame = member.getString("other_games", "");
			mfsAllFriendsSortRank = member.getInt("mfs_all_friends", 0);
			mfsLikesGamesSortRank = member.getInt("mfs_likes_games", 0);
			mfsGameFriendsSortRank = member.getInt("mfs_game_friends", 0);
			questCollectibles = member.getInt("quest_current_milestone", 0);
			isFacebookConnected = member.getBool("fb_connected", false);
			isLLConnected = member.getBool("ll_connected", false);
			photoSource.setUrl(member.getString("photo_url", ""), PhotoSource.Source.FB);
		}

		weeklyRaceDivision = member.getInt("weekly_race_division", 0);

		initTimers(member);
		indexUser(zId, id, this);
	}

	public void updateValues(
		string id,
		string zId,
		string nid = "",
		string firstName = "",
		string lastName = "",
		int vipLevel = 0,
		string imageUrl = "")
	{
		// Update the member data if we recieved non null values for them.
		if (!string.IsNullOrEmpty(id) && id != this.id) { this.id = id; }
		if (!string.IsNullOrEmpty(zId) && zId != this.zId) { this.zId = zId;}
		if (!string.IsNullOrEmpty(networkID)) { this.networkID = nid;}
		if (!string.IsNullOrEmpty(firstName) && firstName != this.firstName) { this.firstName = firstName;}
		if (!string.IsNullOrEmpty(lastName) && lastName != this.lastName) { this.lastName = lastName;}
		if (!string.IsNullOrEmpty(imageUrl))
		{
			photoSource.setUrl(imageUrl, PhotoSource.Source.PROFILE);
		}
		if (this.vipLevel < vipLevel) { this.vipLevel = vipLevel;}
	}

	public void updateData(JSON member)
	{
		firstName = member.getString("first_name", "");
		lastName = member.getString("last_name", "");
		likesGames = member.getBool("likes_games", false);
		canSendCredits = member.getBool("can_send_credits", false);
		canSendBonus = member.getBool("can_send_gift_bonus", false);
		canSendInvite = member.getBool("can_invite", false);
		canAskForCredits = member.getBool("can_ask_for_credits", false);
		canSendCharms = member.getBool("can_send_charm", false);
		id = member.getString("fb_id", "");
		zId = member.getString("id", "");
		xp = member.getLong("experience", 0);
		experienceLevel = member.getInt("experience_level", 1);
		vipLevel = member.getInt("vip_level", 0);
		credits = member.getLong("credits", 0);
		playedGame = member.getString("other_games", "");
		mfsAllFriendsSortRank = member.getInt("mfs_all_friends", 0);
		mfsLikesGamesSortRank = member.getInt("mfs_likes_games", 0);
		mfsGameFriendsSortRank = member.getInt("mfs_game_friends", 0);
		questCollectibles = member.getInt("quest_current_milestone", 0);
		isFacebookConnected = member.getBool("fb_connected", false);
		isLLConnected = member.getBool("ll_connected", false);
	}

	private void initTimers(JSON member)
	{
		// Using the max cooldown as the default timer.
		int canSendAtTime = 0;
		canSendAtTime = member.getInt("can_send_credits_at", GameTimer.currentTime + Glb.SEND_GIFT_COOLDOWN);
		canSendCreditsTimer = new GameTimerRange(GameTimer.currentTime, canSendAtTime);
		canSendCreditsTimer.registerFunction(onSendCreditsTimerCooldownFinished);

		canSendAtTime = member.getInt("can_send_gift_bonus_at", GameTimer.currentTime + Glb.SEND_GIFT_COOLDOWN);
		canSendBonusTimer = new GameTimerRange(GameTimer.currentTime, canSendAtTime);
		canSendBonusTimer.registerFunction(onSendBonusTimerCooldownFinished);

		canSendAtTime = member.getInt("can_invite_at", GameTimer.currentTime + Glb.SEND_GIFT_COOLDOWN);
		canSendInviteTimer = new GameTimerRange(GameTimer.currentTime, canSendAtTime);
		canSendInviteTimer.registerFunction(onSendInviteTimerCooldownFinished);

		canSendAtTime = member.getInt("can_ask_for_credits_at", GameTimer.currentTime + Glb.SEND_GIFT_COOLDOWN);
		canAskForCreditsTimer = new GameTimerRange(GameTimer.currentTime, canSendAtTime);
		canAskForCreditsTimer.registerFunction(onAskForCreditsCooldownFinished);
	}

	private void activateGifting()
	{
		canSendCredits = true;
		canSendCreditsTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime);
		canSendCreditsTimer.registerFunction(onSendCreditsTimerCooldownFinished);

		canSendBonus = true;
		canSendBonusTimer = new GameTimerRange(GameTimer.currentTime,  GameTimer.currentTime);
		canSendBonusTimer.registerFunction(onSendBonusTimerCooldownFinished);
	}


	// GameTimerRange callbacks
	private void onSendCreditsTimerCooldownFinished(Dict args = null, GameTimerRange originalTimer = null)
	{
		canSendCredits = true;
	}

	private void onSendBonusTimerCooldownFinished(Dict args = null, GameTimerRange originalTimer = null)
	{
		canSendBonus = true;
	}

	private void onSendInviteTimerCooldownFinished(Dict args = null, GameTimerRange originalTimer = null)
	{
		canSendInvite = true;
	}

	private void onAskForCreditsCooldownFinished(Dict args = null, GameTimerRange originalTimer = null)
	{
		canAskForCredits = true;
	}

	protected static void addToListWithoutDuplicates(List<SocialMember> list, SocialMember member)
	{
		if (!list.Contains(member))
		{
			list.Add(member);
		}
	}

	#region network_profile

	public NetworkProfile networkProfile
	{
		get { return _networkProfile; }
		set
		{
			_networkProfile = value;
			
			// Make sure isFamale flag is synced with gender defined in _networkProfile
			isFemale = (_networkProfile == null) || string.IsNullOrEmpty(_networkProfile.gender) || (_networkProfile.gender.ToLower() != "male");
		}
	}

	private NetworkProfile _networkProfile;

	public string networkID = "";

	public delegate void onMemberUpdatedDelegate(SocialMember member);
	public event onMemberUpdatedDelegate onMemberUpdated;

	private int _vipLevel = 0;
	public int vipLevel
	{
		get
		{
			if (NetworkProfileFeature.instance.isEnabled &&
				networkProfile != null &&
				// the profile vip level can't be lower than the player one so don't return a lower one.
				networkProfile.vipLevel > _vipLevel)
			{
				return networkProfile.vipLevel;
			}
			return _vipLevel;
		}
		set
		{
			_vipLevel = value;
		}
	}

	public string getLargeImageURL
	{
		get
		{
			string url = photoSource.getPrimaryUrl(true);
			return updateUrlWithFBToken(url);
		}
	}

	private string updateUrlWithFBToken(string url)
	{
		//check if this is graph api call (they require tokens)
		if (url != null && url.Contains("https://graph.facebook.com/"))
		{
			//if access token is already hard coded in the url from the server data don't do anything
			if (url.Contains("access_token"))
			{
				return url;
			}
			else if (AccessToken.CurrentAccessToken != null &&
			         !string.IsNullOrEmpty(AccessToken.CurrentAccessToken.TokenString))
			{
				//append modifer to the url containing access token
				if (url.Contains("?"))
				{
					return url + "&access_token=" + AccessToken.CurrentAccessToken.TokenString;
				}
				else
				{
					return url + "?access_token=" + AccessToken.CurrentAccessToken.TokenString;
				}	
			}
		}
		return url;
	}

	public string getImageURL
	{
		get
		{
			string url = photoSource.getPrimaryUrl();
			return updateUrlWithFBToken(url);
		}
	}
#endregion

#region achievements
	public AchievementProgress achievementProgress;

	public void setupAchievements(JSON data, long score)
	{
		if (achievementProgress == null)
		{
			achievementProgress = new AchievementProgress(data, score, this);
		}
		else
		{
			achievementProgress.update(data, score, this);
		}
	}

	public AchievementLevel achievementRank
	{
		get
		{
			if (achievementProgress != null)
			{
				return achievementProgress.rank;
			}
			else if (networkProfile != null)
			{
				return networkProfile.achievementLevel;
			}
			return AchievementLevel.getLevel(0);
		}
	}

	public long achievementScore
	{
		get
		{
			if (achievementProgress != null)
			{
				return achievementProgress.score;
			}
			else if (networkProfile != null)
			{
				return networkProfile.achievementScore;
			}
			return 0L;
		}
		set
		{
			// TODO we probably should only store this in one place.
			if (achievementProgress != null)
			{
				achievementProgress.score = value;
			}

			if (networkProfile != null)
			{
				networkProfile.achievementScore = value;
			}
		}
	}
#endregion

	public bool didFailNameRequest = false;	// Set to true after requesting the name and it failed, so it can only happen once per member.

	public virtual string fullName
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

			if (SlotsPlayer.IsAppleLoggedIn && isUser)
			{
				string[] name = CommonText.splitWhitespace(ZisData.AppleName);

				if (name.Length >= 1)
				{
					if (!CommonText.IsNullOrWhiteSpace(name[0]))
					{
						firstName = name[0];
					}
				}

				if (name.Length >= 2)
				{
					if (!CommonText.IsNullOrWhiteSpace(name[1]))
					{
						lastName = name[1];
					}
				}
			}

			return firstName + " " + lastName;
		}
	}

	public virtual string firstNameLastInitial
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

			if (SlotsPlayer.IsAppleLoggedIn && isUser)
			{
				string[] name = CommonText.splitWhitespace(ZisData.AppleName);
				if (name.Length >= 1)
				{
					if (!CommonText.IsNullOrWhiteSpace(name[0]))
					{
						firstName = name[0];
					}
				}
				if (name.Length >= 2)
				{
					if (!CommonText.IsNullOrWhiteSpace(name[1]))
					{
						lastName = name[1];
					}
				}
			}

			return CommonText.firstNameLastInitial(firstName, lastName);
		}
	}

	public static void populateAll(JSON jsonData)
	{
#if UNITY_EDITOR
		if (jsonData != null)
		{
			if (populatedJson == null)
			{
				populatedJson = new List<JSON>();
			}
			populatedJson.Add(jsonData);
		}
#endif
		if (!ExperimentWrapper.CasinoFriends.isInExperiment)
		{
			FacebookMember.populateAll(jsonData);
			return;
		}

		if (jsonData == null)
		{
			Debug.LogWarning("SocialMember.populateAll() was provided null jsonData");
			return;
		}

		JSON[] activeFriendsData = jsonData.getJsonArray("active_friends");

		List<SocialMember> friendProfilesToGet = new List<SocialMember>();
		if (activeFriendsData == null)
		{
			Debug.LogWarning("SocialMember.populateAll() has no active_friends data");
		}
		else
		{
			string zid = "";
			// Build the lists of friends that play this game.
			foreach (JSON friend in activeFriendsData)
			{
				zid = friend.getString("id", "");
				// Daily race can populate before this call, so lets check for an existing social member first.
				SocialMember member = findByZId(zid);
				if (member == null)
				{
					member = new SocialMember(friend, true);
				}
				else
				{
					member.updateData(friend);
				}
				switch (friend.getString("type", ""))
				{
					case "friend":
						addToListWithoutDuplicates(friendPlayers, member);
						addToListWithoutDuplicates(friendPlayersAndMe, member);
						addToListWithoutDuplicates(allFriends, member);
						break;

					case "invite_sent":
						addToListWithoutDuplicates(invitedPlayers, member);
						break;

					case "invite_received":
						addToListWithoutDuplicates(invitedByPlayers, member);
						break;

					case "blocked":
						addToListWithoutDuplicates(blockedPlayers, member);
						break;
				}
				addToListWithoutDuplicates(friendProfilesToGet, member);
			}
		}

		JSON[] invitableFriendsData = jsonData.getJsonArray("invitable_friends");
		if (invitableFriendsData == null)
		{
			Debug.LogWarning("SocialMember.populateAll() has no invitable_friends data");
		}
		else
		{
			// Build the lists of friends that DON'T play this game.
			foreach (JSON friend in invitableFriendsData)
			{
				SocialMember member = new SocialMember(friend, false, true);
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
			NetworkProfileFeature.instance.queueDownloadProfiles(friendProfilesToGet);
		}

		if (NetworkFriends.instance.isEnabled)
		{
			// Only register these if we are in the experiment.
			NetworkFriends.instance.onInviteFriend += addToInvited;
			NetworkFriends.instance.onInviteCancelled += removeFromInvited;
			NetworkFriends.instance.onInviteAccepted += addToFriends;
			NetworkFriends.instance.onFriendBlocked += addToBlocked;
			NetworkFriends.instance.onFriendUnblocked += removeFromBlocked;
			NetworkFriends.instance.onInviteDeclined += removeFromInvitedBy;
			// Events for Server Events (caused by other users)
			NetworkFriends.instance.onInviteWithdrawn += removeFromInvitedBy;
			NetworkFriends.instance.onFriendRemoved += removeFromFriends;
			NetworkFriends.instance.onInviteReceived += addToInvitedBy;
			NetworkFriends.instance.onNewFriend += addToFriends;
			NetworkFriends.instance.onInviteRejected += removeFromInvited;
		}

		hasInitialized = true;
	}

	public string anonymousName
	{
		get
		{
			return Localize.text("player_anonymous_name");
		}
	}

	// Sometimes we have other non-friends that are shown (like Daily Race),
	// where we can't retrieve their name, so we fall back to using Guest#XXXX.
	public string anonymousNonFriendName
	{
		get
		{
			// Anonymous players should now have the name Slots Lover.
			if (!string.IsNullOrEmpty(zId) && zId != "-1")
			{
				int index = Mathf.Max(0, zId.Length - 4);
				return "Guest#" + zId.ToString().Substring(index);
			}
			else if (!string.IsNullOrEmpty(id) && id != "-1")
			{
				int index = Mathf.Max(0, id.Length - 4);
				return "Guest#" + id.ToString().Substring(index);
			}
			else
			{
				// Use 1234 if we have weird/non-existent ids.
				return "Guest#1234";
			}
		}
	}

	// Override this with a query to the social network to populate the name properties.
	public virtual IEnumerator requestName()
	{
		if (isFacebookConnected &&
			(string.IsNullOrEmpty(fbFirstName) || string.IsNullOrEmpty(fbLastName)))
		{
#if ZYNGA_TRAMP
			firstName = "TRAMP";
			lastName = "TRAMP";
			yield break;
#endif
			if (didFailNameRequest)
			{
				yield break;
			}

			string url = string.Format("https://graph.facebook.com/{0}{1}", Zynga.Zdk.ZyngaConstants.FbGraphVersion, id);
			WWW request = new WWW(url);
			yield return request;
			if (request.error == null)
			{
				JSON json = new JSON(request.text);
				if (json != null && json.isValid)
				{
					// Adding fbFirstName and fbLastName in order to store these values
					// even if the profile overwrites the first/last name.
					firstName = fbFirstName = json.getString("first_name", "");
					lastName = fbLastName = json.getString("last_name", "");
				}
				else
				{
					Debug.LogError("Invalid facebook json: " + request.text);
					didFailNameRequest = true;
				}
			}
			else
			{
				Debug.LogError("Error requesting facebook info: " + request.error + "\n" + url);
				didFailNameRequest = true;
			}
			setUpdated();
		}
		yield break;
	}

	/// All-in-one function for applying this member's pic on a UITexture.
	/// If the pic hasn't yet been downloaded, then it is automatically
	/// requested and applied when finished.
	public IEnumerator setPicOnRenderer(Renderer renderer, bool isLarge = false)
	{
		if (isLarge)
		{
			DisplayAsset.loadTextureToRenderer(renderer, SlotsPlayer.instance.socialMember.getLargeImageURL);
		}
		else
		{
			if (string.IsNullOrEmpty(getImageURL))
			{
				Debug.LogWarning("SocialMember -- setPicOnRenderer -- getImageURL is empty, not attempting to download it");
			}
			else
			{
				DisplayAsset.loadTextureToRenderer(renderer, getImageURL, "", true);
			}
		}
		yield break;
	}

	public void setPicOnUITexture(UITexture texture)
	{
		if (string.IsNullOrEmpty(getImageURL))
		{
			Debug.LogWarning("SocialMember -- setPicOnRenderer -- getImageURL is empty, not attempting to download it");
		}
		else if (texture == null)
		{
			Debug.LogWarning("SocialMember -- setPicOnRenderer -- UITexture is empty, not attempting to download to it");
		}
		else
		{
			DisplayAsset.loadTextureToUITexture(texture, getImageURL, "", true);
		}
	}

	public void applyTextureToRenderer(Texture2D tex, UITexture texture)
	{
		texture.mainTexture = tex;
	}

	// Handles creating a copy of the material to apply the texture to.
	public void applyTextureToRenderer(Texture2D tex, Renderer renderer)
	{
		Material mat = renderer.sharedMaterial;

		if (mat == null)
		{
			// Use the same shader that the lobby options use.
			mat = new Material(LobbyOptionButtonActive.getOptionShader());
		}
		else
		{
			// If there is already a material on this renderer,
			// then use a copy of it, since it may have a special shader we want.
			mat = new Material(mat);
		}

		renderer.sharedMaterial = mat;
		renderer.sharedMaterial.mainTexture = tex;
	}

	public void addScore(ScoreType scoreKey, long scoreValue)
	{
		if (scoreMap == null)
		{
			scoreMap = new Dictionary<ScoreType, long>();
		}

		// Just set it so that we override any existing value for that score when we update.
		scoreMap[scoreKey] = scoreValue;
	}

	public long getScore(ScoreType scoreKey)
	{
		if (scoreMap.ContainsKey(scoreKey))
		{
			return scoreMap[scoreKey];
		}
		return 0L; // Default to a sane score of 0
	}

	public void setUpdated()
	{
		if (onMemberUpdated != null)
		{
			onMemberUpdated(this);
		}
	}

	public bool isValid
	{
		get
		{
			return CommonSocial.isValidId(id) ||
				CommonSocial.isValidId(zId) ||
				CommonSocial.isValidId(networkID);
		}
	}

	public void loadProfileImageToUITexture(UITexture profileImage, System.Action<Material> onNewMaterial = null)
	{
		string fullURL = getImageURL;
		
		//If we're not using an image from the common avatars S3 bucket, download from the full URL
		if (!fullURL.Contains("network_profiles/avatars"))
		{
			DisplayAsset.loadTextureToUITexture(profileImage, fullURL, "", true, newMaterialCallback:onNewMaterial);
		}
		else
		{
			//Attempt to load from the avatars bundle first and fallback to the fullURL
			string localAvatarPath = string.Format(NetworkAvatarSelectPanel.LOCAL_URL_FORMAT, DisplayAsset.textureNameFromRemoteURL(fullURL));
			DisplayAsset.loadTextureToUITexture(profileImage, localAvatarPath, fullURL, false, false, skipBundleMapping:true, pathExtension:".png", newMaterialCallback:onNewMaterial);
		}
	}
	
	public override string ToString()
	{
		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		// Ids
		builder.Append("ZID: " + this.zId + "\n");
		builder.Append("FBID: " + this.id + "\n");
		builder.Append("Network ID: " + this.networkID + "\n");
		// Names
		builder.Append("Full Name: " + this.fullName + "\n");
		builder.Append("First Name: " + this.firstName + "\n");
		builder.Append("Last Name: " + this.lastName + "\n");
		builder.Append("First Name Last Initial: " + this.firstNameLastInitial + "\n");
		builder.Append("Anonymous Name: " + this.anonymousName + "\n");
		builder.Append("Anon None Friend Name: " + this.anonymousNonFriendName + "\n");
		builder.Append("fbFirstName: " + this.fbFirstName + "\n");
		builder.Append("fbLastName: " + this.fbLastName + "\n");

		builder.Append("PhotoSources:: " + this.photoSource.ToString() + "\n");
		builder.Append("VIP Level: " + this.vipLevel + "\n");
		builder.Append("Is Facebook: " + this.isFacebookConnected.ToString() + "\n");
		builder.Append("FB Friend: " + this.isFacebookFriend.ToString() + "\n");
		if (this.networkProfile != null)
		{
			builder.Append("Profile Name: " + this.networkProfile.name + "\n");
			builder.Append("Friend Code: " + this.networkProfile.friendCode + "\n");
			builder.Append("Network ID: " + this.networkID + "\n");
			builder.Append("Network Image URL: " + this.photoSource.getUrl(PhotoSource.Source.PROFILE) + "\n");
			builder.Append("Achievement Score: " + this.networkProfile.achievementScore + "\n");
			builder.Append("Fav. Achievement: " + this.networkProfile.displayAchievement + "\n");
		}
		return builder.ToString();
	}
#region static_methods
	public static SocialMember find(string fbid, string zid, string networkId = "")
	{
		SocialMember member = findByZId(zid);

		if (member == null)
		{
			member = findByNetworkId(networkId);
		}

		if (member == null)
		{
			member = findByFbId(fbid);

			// MCC -- Adding some logic to make sure we dont multi-associate zids of old facebook accounts.
			if (member != null && !string.IsNullOrEmpty(member.zId))
			{
				// If we got here, then we didnt know about this zid, but have this fbid on file.
				// Now check if this member has a zid at all, if they do, then lets not return this member becuase then we will
				// be attaching a different user to the zid. This should cause a new social member with this zid combo to get created.
				member = null;
			}
		}
		return member;
	}

	/// Returns a social member by Zynga id.
	public static SocialMember findByZId(string id)
	{
		if (id != null)
		{
			if (id != "-1" && _allIndexedByZId.ContainsKey(id))
			{
				return _allIndexedByZId[id];
			}
		}
		return null;
	}

	public static SocialMember findByNetworkId(string networkID)
	{
		if (!string.IsNullOrEmpty(networkID) && networkID != "-1" && _allIndexedByNetworkId.ContainsKey(networkID))
		{
			return _allIndexedByNetworkId[networkID];
		}
		return null;
	}

	public static SocialMember findFriend(string fbid, string zId)
	{
		for (int i = 0; i < allFriends.Count; ++i)
		{
			if (allFriends[i].id == fbid)
			{
				return allFriends[i];
			}
			else if (allFriends[i].zId == zId)
			{
				return allFriends[i];
			}
		}

		return null;
	}

	public static void addNetworkUser(string networkID, SocialMember member)
	{
		_allIndexedByNetworkId[networkID] = member;
	}

	public static void indexUser(string zid, string fbid, SocialMember member)
	{
		indexUser(zid, fbid, "-1", member);
	}

	public static void indexUser(string zid, string fbid, string networkId, SocialMember member)
	{
		if (!string.IsNullOrEmpty(fbid) && fbid != "-1")
		{
			if (_allIndexed.ContainsKey(fbid))
			{
				Debug.LogWarningFormat("SocialMember.cs -- indexUser -- we already have a user with fbid: {0}", fbid);
			}
			else
			{
				_allIndexed.Add(fbid, member);
			}
		}
		if (!string.IsNullOrEmpty(zid) && zid != "-1")
		{
			if (_allIndexedByZId.ContainsKey(zid))
			{
				Debug.LogWarningFormat("SocialMember.cs -- indexUser -- we already have a user with zid: {0}", zid);
			}
			else
			{
				_allIndexedByZId.Add(zid, member);
			}

		}
		if (!string.IsNullOrEmpty(networkId) && networkId != "-1")
		{
			if (_allIndexedByNetworkId.ContainsKey(networkId))
			{
				Debug.LogWarningFormat("SocialMember.cs -- indexUser -- we already have a user with networkId: {0}", networkId);
			}
			else
			{
				_allIndexedByNetworkId.Add(networkId, member);
			}
		}
	}

	/// Converts a list of SocialMember objects into the associated list of Zynga id's as longs to send.
	public static List<long> getLongzIds(List<SocialMember> members)
	{
		List<long> ids = new List<long>();

		foreach (SocialMember member in members)
		{
			long zid;
			if (long.TryParse(member.zId, out zid))
			{
				ids.Add(zid);
			}
		}

		return ids;
	}

	/// Converts a list of SocialMember objects into the associated list of Zynga id's as strings.
	public static List<string> getzIds(List<SocialMember> members)
	{
		List<string> ids = new List<string>();

		foreach (SocialMember member in members)
		{
			ids.Add(member.zId);
		}

		return ids;
	}

	/// Get random list of friends.
	/// Player might not have as many friends as you wanted, so make sure you check friendList.Count!
	public static void getRandomFriends(int numFriends,List<SocialMember> friendList)
	{
		int nFriend = 0;

		for (; (nFriend<numFriends) && (nFriend<friendPlayers.Count); nFriend++)
		{
			SocialMember friend;

			do
			{
				int iFriend = Random.Range(0 , allFriends.Count);
				friend = allFriends[iFriend];
			}
			while (friendList.IndexOf(friend) != -1);

			friendList.Add(friend);
		}

		for (; (nFriend<numFriends) && (nFriend<allFriends.Count); nFriend++)
		{
			SocialMember friend;

			do
			{
				int iFriend = Random.Range(0 , allFriends.Count);
				friend = allFriends[iFriend];
			}
			while (friendList.IndexOf(friend) != -1);

			friendList.Add(friend);
		}
	}

	/// Function for sorting friends list by last name.
	public static int sortLastName(SocialMember a, SocialMember b)
	{
		return System.String.Compare(a.lastName, b.lastName);
	}

	// Function for sorting friends list by amount of credits.
	public static int sortCredits(SocialMember a, SocialMember b)
	{
		if (a.credits == b.credits)
		{
			// If credits are equal, sort facebook-connected players below
			// non-facebook-connected players so they show up "higher" in the rankings.
			if (a.id == "-1" && b.id != "-1")
			{
				return -1;
			}
			else if (a.id != "-1" && b.id == "-1")
			{
				return 1;
			}

			// If both players are either facebook-connected or non-facebook-connected,
			// then fallback to sorting by zid for consistency.
			// We can't rely on name for sorting since it may or may not be available as well.
			return a.zId.CompareTo(b.zId);
		}

		return a.credits.CompareTo(b.credits);
	}

	// Function for sorting friends list by amount of credits from highest to lowest.
	public static int sortCreditsReverse(SocialMember b, SocialMember a)
	{
		if (a.credits == b.credits)
		{
			// If both players have the same amount of credits,
			// then fallback to sorting by zid for consistency.
			// We can't rely on name for sorting since it may or may not be available as well.
			return a.zId.CompareTo(b.zId);
		}

		return a.credits.CompareTo(b.credits);
	}

	/// Function for sorting friends list by amount of credits descending.
	public static int sortCreditsDesc(SocialMember a, SocialMember b)
	{
		return b.credits.CompareTo(a.credits);
	}

	/// Function for sorting friends list by amount of xp descending.
	public static int sortXpDesc(SocialMember a, SocialMember b)
	{
		return b.xp.CompareTo(a.xp);
	}

	// Function for sorting friends list by the serer-defined ranking for allFriends.
	public static int sortAllFriendsRanking(SocialMember a, SocialMember b)
	{
		int result =  a.mfsAllFriendsSortRank.CompareTo(b.mfsAllFriendsSortRank);
		if (result == 0)
		{
			result = sortLastName(a, b);
		}
		return result;
	}

	// Function for sorting friends list by the server-defined ranking for likesGamesFriends
	public static int sortLikesGamesRanking(SocialMember a, SocialMember b)
	{
		int result = a.mfsLikesGamesSortRank.CompareTo(b.mfsLikesGamesSortRank);
		if (result == 0)
		{
			result = sortLastName(a, b);
		}
		return result;
	}

	// Function for sroting friends list by the server-defined ranking for HIR-playing Friends.
	public static int sortGameFriendsRanking(SocialMember a, SocialMember b)
	{
		int result = a.mfsGameFriendsSortRank.CompareTo(b.mfsGameFriendsSortRank);
		if (result == 0)
		{
			result = sortLastName(a, b);
		}
		return result;
	}

	/// Returns a facebook member by facebook id.
	public static SocialMember findByFbId(string id)
	{
		if (id == null || id == "-1")
		{
			return null;
		}

		if (_allIndexed.ContainsKey(id))
		{
			return _allIndexed[id];
		}
		// Added a check to allow for finding the current player if nothing was found via the first check.
		else if (SlotsPlayer.instance.facebook != null && id == SlotsPlayer.instance.facebook.id)
		{
			return SlotsPlayer.instance.facebook;
		}
		return null;
	}

#region friend_event_callbacks
	private static void removeMemberFromList(SocialMember member, List<SocialMember> list, bool didSucceed)
	{
		if (didSucceed && list.Contains(member))
		{
			list.Remove(member);
		}
	}

	private static void addMemberToList(SocialMember member, List<SocialMember> list, bool didSucceed)
	{
		if (didSucceed && !list.Contains(member))
		{
			list.Add(member);
		}
	}

	private static void removeFromInvited(SocialMember member, bool didSucceed, int errorCode)
	{ removeMemberFromList(member, invitedPlayers, didSucceed);}

	private static void addToInvited(SocialMember member, bool didSucceed, int errorCode)
	{ addMemberToList(member, invitedPlayers, didSucceed);}

	private static void removeFromInvitedBy(SocialMember member, bool didSucceed, int errorCode)
	{ removeMemberFromList(member, invitedByPlayers, didSucceed);}

	private static void addToInvitedBy(SocialMember member, bool didSucceed, int errorCode)
	{ addMemberToList(member, invitedByPlayers, didSucceed);}

	private static void removeFromFriends(SocialMember member, bool didSucceed, int errorCode)
	{
		removeMemberFromList(member, allFriends, didSucceed);
		removeMemberFromList(member, friendPlayersAndMe, didSucceed);
		removeMemberFromList(member, friendPlayers, didSucceed);
	}

	private static void addToFriends(SocialMember member, bool didSucceed, int errorCode)
	{
		removeMemberFromList(member, invitedPlayers, didSucceed);
		removeMemberFromList(member, invitedByPlayers, didSucceed);
		addMemberToList(member, allFriends, didSucceed);
		addMemberToList(member, friendPlayers, didSucceed);
		addMemberToList(member, friendPlayersAndMe, didSucceed);
	}

	private static void removeFromBlocked(SocialMember member, bool didSucceed, int errorCode)
	{ removeMemberFromList(member, blockedPlayers, didSucceed);}

	private static void addToBlocked(SocialMember member, bool didSucceed, int errorCode)
	{
		removeMemberFromList(member, invitedPlayers, didSucceed);
		removeMemberFromList(member, invitedByPlayers, didSucceed);
		removeMemberFromList(member, allFriends, didSucceed);
		addMemberToList(member, blockedPlayers, didSucceed);
	}
#endregion
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		allFriends = new List<SocialMember>();
		invitedPlayers = new List<SocialMember>();
		invitedByPlayers = new List<SocialMember>();
		blockedPlayers = new List<SocialMember>();
		friendPlayers = new List<SocialMember>();
		friendsNonPlayers = new List<SocialMember>();
		friendPlayersAndMe = new List<SocialMember>();
		sortedFriends = new List<SocialMember>();
		blankImageMembers = new List<SocialMember>();

		_allIndexed = new Dictionary<string, SocialMember>();
		_allIndexedByZId = new Dictionary<string, SocialMember>();
		_allIndexedByNetworkId = new Dictionary<string, SocialMember>();
		isFriendsPopulated = false;
		hasInitialized = false;
		populatedJson = null;

		// Clear up friends events.
		NetworkFriends.instance.onInviteFriend -= addToInvited;
		NetworkFriends.instance.onInviteCancelled -= removeFromInvited;
		NetworkFriends.instance.onInviteAccepted -= addToFriends;
		NetworkFriends.instance.onFriendBlocked -= addToBlocked;
		NetworkFriends.instance.onFriendUnblocked -= removeFromBlocked;
		NetworkFriends.instance.onInviteWithdrawn -= removeFromInvitedBy;
		NetworkFriends.instance.onFriendRemoved -= removeFromFriends;
		NetworkFriends.instance.onInviteReceived -= addToInvitedBy;
		NetworkFriends.instance.onNewFriend -= addToFriends;
		NetworkFriends.instance.onInviteDeclined -= removeFromInvitedBy;
		NetworkFriends.instance.onInviteRejected -= removeFromInvited;
	}
#endregion

#region network_profile_testing

	public static string printAllProfiles()
	{
		string result = "{";
		for (int i = 0; i < friendPlayersAndMe.Count; i++)
		{
			if (friendPlayersAndMe[i].networkProfile != null)
			{
				if (i != 0)
				{
					result += ",";
				}
				result += friendPlayersAndMe[i].networkProfile.ToString();
			}
		}
		result += "}";
		return result;
	}
#endregion

	public enum ScoreType
	{
		NONE,
		DAILY_RACE,
		DAILY_RACE_LAST_WINNER,
		MAX_VOLTAGE_RECENT_WINNER,
		TICKET_TUMBLER_PREVIOUS_WIN,
		TICKET_TUMBLER_WINNER,
		TOURNAMENTS

	}
}
