using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataAchievementsNoRewards : MOTDDialogData
{
	// MOTDs that prohibit this one from showing in the same session.
	private string[] segregatedMOTDs = new string[]
	{
		// Seperate but equal.
		"network_profile_1.5",
		"achievements_update",
		"achievements",
		"network_profile_tooltip"
	};
	
	public override bool shouldShow
	{
		get
		{
			return MOTDFramework.isValidWithSeenList(segregatedMOTDs) &&
				ExperimentWrapper.NetworkAchievement.isInExperiment && 
				!ExperimentWrapper.NetworkAchievement.enableTrophyRewards &&
				(SlotsPlayer.instance.socialMember.experienceLevel >= Glb.ACHIEVEMENT_MOTD_MIN_LEVEL);
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
			if (ExperimentWrapper.NetworkAchievement.enableTrophyRewards)
			{
				reason += "Experiment rewards are enabled.\n";
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

