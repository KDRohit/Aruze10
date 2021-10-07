using UnityEngine;
using System.Collections;

public class MOTDDialogDataNetworkProfileTooltip : MOTDDialogData
{
	
	public override bool shouldShow
	{
		get
		{
			return ExperimentWrapper.NetworkProfile.isInExperiment &&
				NetworkProfileFeature.instance.isForEveryone &&
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
			if (!NetworkProfileFeature.instance.isForEveryone)
			{
				result += string.Format("liveData ({0})key is off.\n", NetworkProfileFeature.PROFILE_FOR_EVERYONE_KEY);
			}
			if (SlotsPlayer.instance.socialMember.experienceLevel < Glb.NETWORK_PROFILE_MOTD_MIN_LEVEL)
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
		return NetworkProfileMOTDTooltip.showDialog(keyName);
	}

	new public static void resetStaticClassData()
	{

	}
}
