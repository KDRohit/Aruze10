using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataNetworkFriendsReminder : MOTDDialogData
{
	// MOTDs that prohibit this one from showing in the same session.
	private string[] segregatedMOTDs = new string[]
	{
		// Seperate but equal.
		"network_profile_1.5",
		"network_profile_tooltip",
		"achievements",
		"network_friends"};
	
	public override bool shouldShow
	{
		get
		{
			return MOTDFramework.isValidWithSeenList(segregatedMOTDs) &&
				NetworkFriends.instance.isEnabled &&
				!ExperimentWrapper.CasinoFriends.activeDiscoveryEnabled &&
				SlotsPlayer.instance.socialMember.experienceLevel >= Glb.NETWORK_FRIENDS_MOTD_MIN_LEVEL &&
				SocialMember.allFriends != null &&
				SocialMember.allFriends.Count == 0;// Only surface if they have no friends.
		}
	}

	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;
			if (!ExperimentWrapper.CasinoFriends.isInExperiment)
			{
				result += "Experiment is off.\n";
			}
			if (SlotsPlayer.instance.socialMember.experienceLevel < Glb.NETWORK_FRIENDS_MOTD_MIN_LEVEL)
			{
				result += string.Format("Player level {0} is not high enough, min level: {1}",
					SlotsPlayer.instance.socialMember.experienceLevel,
					Glb.NETWORK_FRIENDS_MOTD_MIN_LEVEL);
			}

			if (SocialMember.allFriends != null && SocialMember.allFriends.Count > 0)
			{
				result += "Player has existing friends.\n";
			}

			if (MOTDFramework.seenThisSession.Contains("network_friends"))
			{
				result += "Network Friends MOTD is still in the queue.\n";
			}
			return result;
		}
	}
	
	public override bool show()
	{
		return NetworkFriendsMOTDDialog.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}
}
