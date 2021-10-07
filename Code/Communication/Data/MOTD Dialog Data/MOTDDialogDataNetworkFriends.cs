using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.EUE;

/*
Override for special behavior.
*/

public class MOTDDialogDataNetworkFriends : MOTDDialogData
{
	// MOTDs that prohibit this one from showing in the same session.
	private string[] segregatedMOTDs = new string[]
	{
		// Seperate but equal.
		"network_profile_1.5",
		"network_profile_tooltip",
		"achievements"
	};
	
	public override bool shouldShow
	{
		get
		{
			return MOTDFramework.isValidWithSeenList(segregatedMOTDs) &&
			       NetworkFriends.instance.isEnabled &&
			       SlotsPlayer.instance.socialMember.experienceLevel >= Glb.NETWORK_FRIENDS_MOTD_MIN_LEVEL &&
			       !ExperimentWrapper.CasinoFriends.activeDiscoveryEnabled;
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
