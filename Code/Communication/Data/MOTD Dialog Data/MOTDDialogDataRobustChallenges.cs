using UnityEngine;
using System.Collections;

/*
Override for special behavior.
*/
public class MOTDDialogDataRobustChallenges : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
            return RobustCampaign.hasActiveRobustCampaignInstance;
		}
	}

	public override string noShowReason
	{
		get
		{
			// There's several reasons why robust challenges could be inactive,
			// so I created RobustChallenges.notActiveReason to get specific here.
			string reason = "";
			if (CampaignDirector.robust != null)
			{
				reason = CampaignDirector.robust.notActiveReason;
			}
			return base.noShowReason + reason;
		}
	}

	public override bool show()
	{
        if (CampaignDirector.robust != null)
        {
            StatsManager.Instance.LogCount(
				"dialog",
				"robust_challenges_motd",
				CampaignDirector.robust.variant,
				"main_lobby",
				(CampaignDirector.robust.currentEventIndex + 1).ToString(),
				"view"
            );

			return RobustChallengesObjectivesDialog.showDialog(keyName);
        }
		return false;
	}

	new public static void resetStaticClassData()
	{
	}
}
