using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkProfileFeature : FeatureBase
{	
	public static NetworkProfileFeature instance
	{
		get
		{
			return FeatureDirector.createOrGetFeature<NetworkProfileFeature>("network_profile");
		}
	}
	
	public const string OVERLAY_BUTTON_PREFAB_PATH_V2 = "Assets/Data/HIR/Bundles/Initialization/Features/Network Profiles/Profile Overlay Button v2.prefab";
	public const string PROFILE_FOR_EVERYONE_KEY = "PROFILES_DEFAULT_FIELDS_SERVER";

	
	public bool isForEveryone
	{
		get
		{
			return Data.liveData.getBool(PROFILE_FOR_EVERYONE_KEY, false);
		}
	}

	public List<string> avatarList;

	public bool hasPopulatedAvatarList
	{
		get
		{
			return avatarList != null && avatarList.Count > 0;
		}
	}

	// Initialize here for ease of use. We'll be clearing it from time to time, but we may also be frequently adding things to it.
	private List<SocialMember> queuedMemberRequests = new List<SocialMember>();
	private bool queueStarted = false;

	public void populateAvatarList(JSON data)
	{
		if (data == null)
		{
			Debug.LogErrorFormat("NetworkProfile.cs -- populateAvatarList -- data is null");
			return;
		}
		string[] urls = data.getStringArray("avatar_urls");
		avatarList = new List<string>(urls);
		PhotoSource.updateBlankSources();
	}

	public void populateAll(List<SocialMember> friends)
	{
		if (friends == null || friends.Count <= 0)
		{
			// Bail
			Debug.Log("NetworkProfile.cs -- populateAll -- You have no friends so we are gonna bail...");
			return;
		}
		
		if (ExperimentWrapper.NetworkProfile.isInExperiment)
		{
			// If we are in the network profile experiment, then get a list of all the zids from the list of friends and grab profiles for them.
			List<string> zids = new List<string>();			
			for (int i = 0; i < friends.Count; i++)
			{
				if (friends[i].networkProfile == null || friends[i].networkProfile.isStale)
				{
					zids.Add(friends[i].zId);
				}
			}
			if (zids.Count > 0)
			{
				NetworkProfileAction.getProfilesFromZids(zids);
			}

		}		
	}

	public void getAchievementScoresForUsers(List<SocialMember> friends)
	{
		if (friends == null || friends.Count <= 0)
		{
			// Bail
			Debug.Log("NetworkProfile.cs -- getAchievementScoresForUsers -- You have no friends so we are gonna bail...");
			return;
		}
		if (ExperimentWrapper.NetworkProfile.isInExperiment)
		{
			// If we are in the network profile experiment,
			// then get a list of all the zids
			List<string> zids = new List<string>();
			for (int i = 0; i < friends.Count; i++)
			{
				zids.Add(friends[i].zId);
			}

			if (zids.Count > 0)
			{
				NetworkProfileAction.getProfilesFromZids(zids);
			}

		}
	}

	public void parseFriendZids(JSON data)
	{
		Debug.LogFormat("NetworkProfile.cs -- parseAllProfileZids -- parsing data: {0}", data.ToString());
		Dictionary<string, JSON> profileJSONs = data.getStringJSONDict("profiles");
		foreach (KeyValuePair<string, JSON> kvp in profileJSONs)
		{
			JSON profileJSON = kvp.Value;
			string zid = kvp.Key;

			string networkId = kvp.Value.getString("network_id", "");
			SocialMember socialMember = SocialMember.findByZId(zid);
			if (socialMember != null)
			{
				// If we found a friend who has a profile or networkID, then set that here.
				string networkIdentifier = profileJSON.getString("network_id", "");
				socialMember.networkID = networkIdentifier;
				long achievementScore = profileJSON.getLong("achievement_score", 0);

				if (socialMember.networkProfile != null)
				{
					// Don't create a new profile if we have one already.
					socialMember.networkProfile.update(profileJSON);
				}
				else
				{
					socialMember.networkProfile = new NetworkProfile(profileJSON, socialMember);
				}

				socialMember.networkProfile.checkAndUpdateProfileDetails(profileJSON, socialMember);
				socialMember.setUpdated();
			}
		}

		long[] skipped = data.getLongArray("skipped");
		if (skipped != null && skipped.Length > 0)
		{
			List<string> zids = new List<string>();
			for (int i = 0; i < skipped.Length; i++)
			{
				zids.Add(skipped[i].ToString());
			}
			if (zids.Count > 0)
			{
				NetworkProfileAction.getProfilesFromZids(zids);
			}
		}
	}

	// Gets the given players profile and updates it if it exists.
	public void getPlayerProfile(SocialMember member)
	{
		if (member != null)
		{
			NetworkProfileAction.getProfile(member, parsePlayerProfile);
		}
		else
		{
			Debug.LogErrorFormat("NetworkProfile.cs -- getPlayerProfile -- you tried to get a profile for null socialmember, try again.");
		}
	}

	public void queueDownloadProfiles(List<SocialMember> members)
	{
		for (int i = 0; i < members.Count; i++)
		{
			queueDownloadProfile(members[i]);
		}
	}
	
	public void queueDownloadProfile(SocialMember member)
	{
		if (member != null && !queuedMemberRequests.Contains(member))
		{
			queuedMemberRequests.Add(member);

			if (!queueStarted)
			{
				queueStarted = true;
				RoutineRunner.instance.StartCoroutine(sendQueuedMembersOnCycle());
			}
		}
		else if (member == null)
		{
			Debug.LogErrorFormat("NetworkProfile.cs -- queueDownloadProfile -- Tired to queue a social member that was null.");
		}
	}

	private IEnumerator sendQueuedMembersOnCycle()
	{
		// If we reset this will get reset, so we'll stop sending requests at that point.
		while (queueStarted)
		{
			yield return new WaitForSeconds(1f);
			if (queuedMemberRequests.Count > 0)
			{
				populateAll(queuedMemberRequests);
				queuedMemberRequests.Clear();
			}
			else
			{
				queueStarted = false;
			}
		}

		yield return null;
	}

	public SocialMember getSocialMemberFromData(JSON data)
	{
		SocialMember member = null;
		JSON profileData = data.getJSON("profile");

		if (profileData == null)
		{
			Debug.LogErrorFormat("NetworkProfile.cs -- getSocialMemberFromData -- profile data was null from JSON: {0}", data);
			return member;
		}

		string zid = profileData.getString("target_zid", "");
		string fbid = profileData.getString("target_fb_id", "");
		string networkID = profileData.getString("network_id", "");
		
		member = CommonSocial.findOrCreate(
			fbid: fbid,
			zid: zid,
			nid: networkID);
		return member;
	}

	// A callback for grabbing the player profile from the result of the get_profile action.
	public void parsePlayerProfile(JSON data)
	{
		SocialMember member = getSocialMemberFromData(data);
		if (member == null)
		{
			Debug.LogErrorFormat("NetworkProfile.cs -- parsePlayerProfile -- couldn't find a socialmember from data: {0}, aborting process", data);
			return;
		}

		JSON profileJSON = data.getJSON("profile");
		if (profileJSON != null)
		{
		    string networkID = profileJSON.getString("network_id", "");

			if (member.networkProfile == null)
			{
				member.networkProfile = new NetworkProfile(profileJSON, member);
			}
			else
			{
				member.networkProfile.update(profileJSON);
			}

			member.networkProfile.zid = member.zId;
			if (string.IsNullOrEmpty(networkID))
			{
				// If this is a LL-connected member then lets update their networkID.
				member.networkID = networkID;			
			}
			
			//update hud overlay
			if (member.isUser)
			{
				RoutineRunner.instance.StartCoroutine(updateProfileHUD(member));
			}
			member.setUpdated();
		}
		else
		{
			Debug.LogErrorFormat("NetworkProfile.cs -- parsePlayerProfile -- did not receive a profile in the response.");
		}
	}

	private IEnumerator updateProfileHUD(SocialMember member)
	{
		if (!member.isUser)
		{
			yield break;
		}

		//this function is called from loading.  It can be called before the awake function is called on hud which will prevent the button from loading.  Wait until it's available
		System.DateTime timeout = System.DateTime.Now + new System.TimeSpan(0, 2, 0); //2 minute timeout
		while (Overlay.instance == null || System.DateTime.Now > timeout)
		{
			yield return null;
		}

		if (System.DateTime.Now > timeout)
		{
			Debug.LogError("Can't find overlay to update hud");
		}
		else if (Overlay.instance.topHIR == null)
		{
			Debug.LogWarning("no topHIR prefab");
			yield break; 
		}
		else
		{
			RoutineRunner.instance.StartCoroutine(Overlay.instance.topHIR.setProfileButtonVisibility());
		}
	}

	public void updateCallback(JSON data)
	{
		if (data != null && data.getBool("success", false))
		{
			// If we succeeded in updating the profile, then kick off a new get_profile just to be sure we are in sync.
			getPlayerProfile(SlotsPlayer.instance.socialMember);
		}
	}

	public void resetProfile(List<string> fields = null)
	{
		Server.registerEventDelegate("profile_reset", onProfileReset);
		NetworkProfileAction.resetProfile(fields);
	}

	private void onProfileReset(JSON data)
	{
		parsePlayerProfile(data);
	}

#region feature_base_overrides
	protected override void initializeWithData(JSON data)
	{
		// Init at game load.
		// Kick off getting the avatars from the server.
		if (isEnabled)
		{
			NetworkProfileAction.getAvatarURLs(populateAvatarList);
		}
	}

	public override bool isEnabled
	{
		get
		{
			if (Application.isPlaying)
			{
				return base.isEnabled && ExperimentWrapper.NetworkProfile.isInExperiment;
			}
			else // For unit tests
			{
				return true;
			}

		}
	}

	protected override void registerEventDelegates()
	{
		Server.registerEventDelegate("multi_profile_data", NetworkProfileFeature.instance.parseFriendZids, true);
	}

	protected override void clearEventDelegates()
	{
		Server.unregisterEventDelegate("multi_profile_data", NetworkProfileFeature.instance.parseFriendZids, true);
	}
#endregion
}
