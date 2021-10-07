using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class NetworkProfile : IResetGame
{
#region STATIC_VARIABLES
	private static int _timeToStaleMinutes = -1;
	private static int timeToStaleMinutes
	{
		get
		{
			if (_timeToStaleMinutes < 0 && Data.liveData != null)
			{
				_timeToStaleMinutes = Data.liveData.getInt("PROFILES_STALE_TIME_MINUTES", 0);
			}
			return _timeToStaleMinutes;
		}
	}
	private static int timeToStale = timeToStaleMinutes * Common.SECONDS_PER_MINUTE;
	#endregion

	public string networkID = "";
	public string zid = "";
	public string name = "";
	public string status = "";
	public string location = "";
	public string gender = "";
	public string friendCode = "";
	public long joinTime = 0;
	public int vipLevel = 0;
	private bool isNameModified = false;
	private bool isPhotoModified = false;
	public Dictionary<string, Dictionary<string, string>> gameStats;

	// We now store all the urls in the PhotoSource inside of socialMember, so dont bother storing it locally,
	// it will just cause potential fragmentation.
	private string photoUrl
	{
		set
		{
			if (member != null && member.photoSource != null)
			{
				member.photoSource.setUrl(value, PhotoSource.Source.PROFILE);
			}
			else
			{
#if UNITY_EDITOR
				Debug.LogErrorFormat("NetworkProfile.cs -- photoUrl -- member or member.photoSource was null, not saving url.");
#else
				Bugsnag.LeaveBreadcrumb("NetworkProfile.cs -- photoUrl  -- member or member.photoSource was null, not saving url.");
#endif
			}
		}
	}

	private int lastUpdatedTime = 0;
	public bool isComplete = false;

/*
	isFullyComplete should only be set to true if we get data from allFields in update(JSON data) instead of partial data from friendPartialFields or achievementsFields
	 we don't have a good way to detect this yet, so this is always false for now, NetworkProfileDialog.showDialog checks this.
	This is a fix for Jira ticket HIR-63871
	1. At startup requests for profile data with only friends fields are made, meaning we don’t get join_time in response along with a bunch of other fields
	2. Server response comes down with partial data within 5 - 30 seconds and we mark network profile as complete even though it’s really not and missing bunch of fields
	3. Player clicks on said profile and show dialog code thinks it is complete and does not make request for full data, so we see 1/70 date and —— text in profile dialog
	4. However if you click to see profile before server response comes down in step 2 then full data request is made and profile dialog is all good.
*/
	public bool isFullyComplete = true;	// is always false until we fix for above reason

	private bool didGenerateUrl = false;
	private SocialMember member;

#region ACHIEVEMENTS
	// Achievements
	public Achievement displayAchievement;
	public long achievementScore;

	private AchievementLevel _achievementLevel;
	public AchievementLevel achievementLevel
	{
		get
		{
			if (_achievementLevel == null)
			{
				_achievementLevel = AchievementLevel.getLevelFromScore(achievementScore);
			}
			return _achievementLevel;
		}
	}
	
#endregion

#region CONSTANTS
	private const string HIR = "hir";
	private const string WONKA = "wonka";
	private const string WOZ = "woz";
	private const string BD = "black_diamond";
	private const string GOT = "got";
#endregion

	public bool isStale
	{
		get
		{
			return !isComplete || ((GameTimer.currentTime - lastUpdatedTime) > timeToStale);
		}
	}

	public NetworkProfile(string networkId, string photoUrl, long achievementScore, SocialMember socialMember, int vipLevel = 0)
	{
		this.member = socialMember;
		this.networkID = networkId;
		SocialMember.addNetworkUser(networkID, member);
		this.photoUrl = photoUrl;
		if (NetworkAchievements.isEnabled)
		{
			this.achievementScore = achievementScore;
		}
		this.vipLevel = vipLevel;
		this.isComplete = false;
	}

	public NetworkProfile(JSON data, SocialMember socialMember)
	{
		member = socialMember;
		networkID = data.getString("network_id", "");
		SocialMember.addNetworkUser(networkID, member);
		friendCode = data.getString("friend_code", "");
		name = data.getString("name", "");
		location = data.getString("location", "");
		photoUrl = data.getString("photo_url", "");
		gender = data.getString("gender", "");
		status = data.getString("status", "");
		vipLevel = data.getInt("vip_level", 0);
		joinTime = data.getLong("join_time", 0);
		JSON statsJSON = data.getJSON("game_stats");

		gameStats = new Dictionary<string, Dictionary<string, string>>();
		if (statsJSON != null)
		{
			gameStats["hir"] = statsJSON.getStringStringDict("hir");
			gameStats["wonka"] = statsJSON.getStringStringDict("wonka");
			gameStats["woz"] = statsJSON.getStringStringDict("woz");
			gameStats["got"] = statsJSON.getStringStringDict("got");
		}
		lastUpdatedTime = GameTimer.currentTime;
		isComplete = true;
		
		string achievementKey = data.getString("display_achievement", "");
		if (!string.IsNullOrEmpty(achievementKey))
		{
			displayAchievement = NetworkAchievements.getAchievement(achievementKey);
		}
		achievementScore = data.getLong("achievement_score", 0);

		// If this is the current user, check if they were affected by the early release of the automatic
		// profile update code, and if so, fix it.
		if (socialMember.isUser)
		{
			checkIfUpdatedIncorrectly(data, socialMember);
		}

		// If we are in Network profiles for everyone, then go through the motions and set the name/photoUrl
		// from facebook data (and update the server profile with those values if this is the current users profile.		
		if (NetworkProfileFeature.instance.isForEveryone)
		{
			checkAndUpdateProfileDetails(data, socialMember, this);
		}
	}

	public bool isWozConnected
	{
		get
		{
			return gameStats != null &&
				gameStats.ContainsKey(WOZ) &&
				gameStats[WOZ].Count > 0;
		}
	}

	public bool isBdConnected
	{
		get
		{
			return gameStats != null &&
				gameStats.ContainsKey(BD) &&
				gameStats[BD].Count > 0;
		}
	}

	public bool isWonkaConnected
	{
		get
		{
			return gameStats != null &&
				gameStats.ContainsKey(WONKA) &&
				gameStats[WONKA].Count > 0;
		}
	}

	public bool isGotConnected
	{
		get
		{
			return gameStats != null &&
			       gameStats.ContainsKey(GOT) &&
			       gameStats[GOT].Count > 0;
		}
	}


	private void checkIfUpdatedIncorrectly(JSON data, SocialMember socialMember)
	{
		if (!CustomPlayerData.getBool(CustomPlayerData.PROFILES_FOR_EVERYONE_HAS_DONE_NAME_FIX_CHECK, false))
		{
			// If we have not already checked the user to see if we messed up their profile before, do it now.
			if (socialMember.isUser && (this.name == socialMember.fullName))
			{
				// If this is the player, and their profile name is equal to their facebook full name.
				// Then we want to tell the server to change their name to the first name last inital instead.
				Dictionary<string, string> updates = new Dictionary<string, string>();
				name = socialMember.firstNameLastInitial;
				isNameModified = true;
				updates.Add("name", socialMember.firstNameLastInitial);
				NetworkProfileAction.updateProfile(networkID, updates, NetworkProfileFeature.instance.updateCallback);
				CustomPlayerData.setValue(CustomPlayerData.PROFILES_FOR_EVERYONE_HAS_DONE_NAME_FIX_CHECK, true);
			}
		}
	}
	
	public void checkAndUpdateProfileDetails(JSON data, SocialMember socialMember, NetworkProfile profile = null)
	{
		if (!socialMember.isUser)
		{
			// We only ever update your own profile.
			return;
		}
		if (data.hasKey("photo_url_is_modified") && data.getString("photo_url_is_modified", null) != null)
		{
			isPhotoModified = data.getBool("photo_url_is_modified", true);
		}
		else if (data.hasKey("is_photo_url_modified") && data.getString("is_photo_url_modified", null) != null)
		{
			isPhotoModified = data.getBool("is_photo_url_modified", true);
		}

		if (data.hasKey("name_is_modified") && data.getString("name_is_modified", null) != null)
		{
			isNameModified = data.getBool("name_is_modified", true);
		}
		else if (data.hasKey("is_name_modified") && data.getString("is_name_modified", null) != null)
		{
			isNameModified = data.getBool("is_name_modified", true);
		}

		// Only really relevant if we're the one being modified here.
		bool changeOccured = false;
		// If we know this is a FB user
		if (!string.IsNullOrEmpty(socialMember.id))
		{
			Dictionary<string, string> updates = new Dictionary<string, string>();
			if (!isPhotoModified)
			{
				updates.Add("photo_url", socialMember.photoSource.getUrl(PhotoSource.Source.FB));
				if (socialMember != null)
				{
					socialMember.photoSource.setUrl(updates["photo_url"], PhotoSource.Source.PROFILE);
				}
				changeOccured = true;
			}
			if (!isNameModified)
			{
				updates.Add("name", socialMember.firstNameLastInitial);
				if (socialMember != null && socialMember.networkProfile != null)
				{
					socialMember.networkProfile.name = socialMember.firstNameLastInitial;
				}
				else if (profile != null)
				{
					// If we are calling this during the profile constructor then the social member might
					// not have a profile yet, so if we passed one in we can use that.
					profile.name = socialMember.firstNameLastInitial;
				}
				changeOccured = true;
			}

			// Set the name and photo of this user to be our FB image and FB name if we haven't modified it yet.
			if (socialMember.isUser && SlotsPlayer.isFacebookUser && changeOccured)
			{
				NetworkProfileAction.updateProfile(networkID, updates, NetworkProfileFeature.instance.updateCallback);
				socialMember.setUpdated(); // We changed some values so mark the member as updated.
			}
		}
	}

	private string updateField(string currentValue, JSON data, string fieldName)
	{
		string newValue = data.getString(fieldName, "notyet");
		if (newValue != "notyet")
		{
			return newValue;
		}
		return currentValue;
	}

	private int updateField(int currentValue, JSON data, string fieldName)
	{
		int newValue = data.getInt(fieldName, 0); // Nothing in the profile should be negative
		if (newValue > 0)
		{
			return newValue;
		}
		return currentValue;
	}

	private long updateField(long currentValue, JSON data, string fieldName)
	{
		long newValue = data.getLong(fieldName, 0); // Nothing in the profile should be negative
		if (newValue > 0)
		{
			return newValue;
		}
		return currentValue;
	}	
	
	public void update(JSON data)
	{
		if (string.IsNullOrEmpty(networkID))
		{
			// If we dont have a networkID set yet, then we identified this profile with other means,
			// and should add a networkId to it.
			networkID = data.getString("network_id", "");
		}
		
		if (networkID != data.getString("network_id", ""))
		{
			// Then this is the wrong profile.
			Debug.LogErrorFormat("NetworkProfile.cs -- update -- trying to update a profile with a mismatching networkID, aborting!");
			return;
		}

		if (NetworkProfileFeature.instance.isForEveryone)
		{
			checkAndUpdateProfileDetails(data, this.member);
		}
		else
		{
			// If we aren't in profile for everyone, just behave normally.
			name = updateField(name, data, "name");
			string newUrl = data.getString("photo_url", "notyet");

			if (newUrl != "notyet")
			{
				member.photoSource.setUrl(newUrl, PhotoSource.Source.PROFILE);
			}
		}

		location = updateField(location, data, "location");

		gender = updateField(gender, data, "gender");
		status = updateField(status, data, "status");
		vipLevel = updateField(vipLevel, data, "vip_level");
		joinTime = updateField(joinTime, data, "join_time");
		
		JSON statsJSON = data.getJSON("game_stats");
		if (statsJSON != null)
		{
			// Only reset the gameStats if it is in the data.
			gameStats = new Dictionary<string, Dictionary<string, string>>();
			gameStats["hir"] = statsJSON.getStringStringDict("hir");
			gameStats["wonka"] = statsJSON.getStringStringDict("wonka");
			gameStats["woz"] = statsJSON.getStringStringDict("woz");
		}
		else if (gameStats == null)
		{
			// If we don't have any existing stats then make this a new one so we don't null out somewhere else.
			gameStats = new Dictionary<string, Dictionary<string, string>>();
		}

		long achievementScore = data.getLong("achievement_score", 0);
		string achievementKey = data.getString("display_achievement", "");
		if (!string.IsNullOrEmpty(achievementKey))
		{
			displayAchievement = NetworkAchievements.getAchievement(achievementKey);
		}

		lastUpdatedTime = GameTimer.currentTime;
		isComplete = true;
	}
	
	// Use this when we send up the action to the server so that we show the change on the client
	// right away, rather than waiting for the next load.
	public void setFavoriteAchievement(Achievement achievement)
	{
		displayAchievement = achievement;
	}

	public static void resetStaticClassData()
	{
		_timeToStaleMinutes = -1;
	}
}
