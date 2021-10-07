using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataNetworkProfileNew : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return ExperimentWrapper.NetworkProfile.isInExperiment &&
			SlotsPlayer.instance.socialMember.experienceLevel >= Glb.NETWORK_PROFILE_MOTD_MIN_LEVEL;
		}
	}

	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;
			if (!ExperimentWrapper.NetworkProfile.isInExperiment)
			{
				result += "Experiment is off.\n";
			}
			if (SlotsPlayer.instance.socialMember.experienceLevel < Glb.NETWORK_PROFILE_NEW_MOTD_MIN_LEVEL)
			{
				result += string.Format("Player level {0} is not high enough, min level: {1}",
					SlotsPlayer.instance.socialMember.experienceLevel,
					Glb.NETWORK_PROFILE_NEW_MOTD_MIN_LEVEL);
			}
			return result;
		}
	}
	
	public override bool show()
	{
		return NetworkProfileMOTD.showDialog(keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}
}
