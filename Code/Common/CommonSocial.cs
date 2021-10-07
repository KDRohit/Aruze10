using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class CommonSocial
{

	public static bool isValidId(string id)
	{
		return !string.IsNullOrEmpty(id) && id != "null" && id != "-1";
	}

	public static SocialMember findOrCreate(
		string fbid = "",
		string zid = "",
		string nid = "",
		string firstName = "",
		string lastName = "",
		long achievementScore = -1, // Default to -1 so we know if it was never given.
		int vipLevel = 0,
		string imageUrl = "")
	{
		if (SlotsPlayer.instance == null || SlotsPlayer.instance.socialMember == null)
		{
			Debug.LogErrorFormat("CommonSocial.cs -- findOrCreate() -- creating a social member before the current player has been created! This is bad, please don't do that.");
		}
		// If there are no valid ids, then bail here.
		if (!isValidId(fbid) && !isValidId(zid))
		{
			Debug.LogErrorFormat("CommonSocial.cs -- findOrCreate() -- both zid and fbid are invalid, bailing.");
			return null;
		}
		// If we have a valid id, try and find the corresponding social member.
		SocialMember member = SocialMember.find(fbid, zid, nid) as SocialMember;
		if (member != null)
		{
			// If we found a member, then update with any new information we have
			member.updateValues(
				id: fbid,
				zId: zid,
				nid: nid,
				firstName: string.IsNullOrEmpty(member.firstName) ? firstName : member.firstName,
				lastName: string.IsNullOrEmpty(member.lastName) ? lastName : member.lastName,
				vipLevel: vipLevel,
				imageUrl: imageUrl);
			if (achievementScore >= 0 && achievementScore > member.achievementScore)
			{
				// Only update the user's achievement score if it is greater than the current one.
				member.achievementScore = achievementScore;
			}
		}
		else
		{
			bool shouldQueueForProfile = achievementScore == -1;
			if (shouldQueueForProfile)
			{
				// We only needed this at -1 to determine if it was a value from the server, we should always use
				// 0 as the minimum score.
				achievementScore = 0;
			}
			// Otherwise create a member from the information that we have now.
			if (ExperimentWrapper.CasinoFriends.isInExperiment && string.IsNullOrEmpty(fbid))
			{
				// If we are in the casino friends experiment, then we can have a mixture of facebook
				// member and social member friends. If they have a null fbid, make them a social member.
				member = new SocialMember(
					id:fbid,
					zId: zid,
					nid: nid,
					firstName: firstName,
					lastName: lastName,
					vipLevel: vipLevel,
					imageUrl: imageUrl);
			}
			else
			{
				member = new FacebookMember(
					id:fbid,
					zId: zid,
					nid: nid,
					firstName: firstName,
					lastName: lastName,
					vipLevel: vipLevel,
					imageUrl: imageUrl);
			}
			member.networkProfile = new NetworkProfile(nid, imageUrl, achievementScore, member, vipLevel);
			// If we are creating a new member, lets see if we should do a multi-get or not.
			if (shouldQueueForProfile)
			{
				// If we are making a new member, lets get their profile data.
				NetworkProfileFeature.instance.queueDownloadProfile(member);
			}
		}
		return member;
	}

	public static string fbidFromImageUrl(string url)
	{
		string facebookPrefix = "https://graph.facebook.com/";
		if (url.FastStartsWith(facebookPrefix))
		{
			string rest = url.Substring(facebookPrefix.Length);
			string id = rest.Substring(0, rest.IndexOf("/"));
			return id;
		}
		else
		{
			return "-1";
		}
	}

	public static List<long> getZidListFromMemberList(List<SocialMember> members)
	{
		long zid = 0;
		List<long> result = new List<long>();
		for (int i = 0; i < members.Count; i++)
		{
			if (members[i] != null)
			{
				long.TryParse(members[i].zId, out zid);
				if (zid > 0)
				{
					result.Add(zid);
				}				
			}
		}
		return result;
	}
}
