using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataAchievements : MOTDDialogData
{
	// MOTDs that prohibit this one from showing in the same session.
	private string[] segregatedMOTDs = new string[]
	{
		// Seperate but equal.
		"network_profile_1.5",
		"achievements_no_rewards",
		"network_profile_tooltip"
	};

	public override bool shouldShow
	{
		get
		{
			return MOTDFramework.isValidWithSeenList(segregatedMOTDs) &&
				ExperimentWrapper.NetworkAchievement.isInExperiment &&
				(SlotsPlayer.instance.socialMember.experienceLevel >= Glb.ACHIEVEMENT_MOTD_MIN_LEVEL) &&
				!ExperimentWrapper.NetworkAchievement.activeDiscoveryEnabled;
		}
	}

	public override string noShowReason
	{
		get
		{
			string reason = base.noShowReason;
			if (!ExperimentWrapper.NetworkAchievement.isInExperiment)
			{
				reason += "Experiment is Off.\n";
			}
			return reason;
		}
	}

	public override bool show()
	{
		return AchievementsMOTD.showDialog(keyName);
	}

	new public static void resetStaticClassData()
	{
	}
}

