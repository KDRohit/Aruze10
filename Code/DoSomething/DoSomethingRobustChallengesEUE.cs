using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class DoSomethingRobustChallengesEUE : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		if (!Scheduler.hasTaskWith("robust_challenges_motd"))
		{
			if (CampaignDirector.robust != null)
			{
				StatsManager.Instance.LogCount(
					"carousel",
					"robust_challenges_motd",
					ExperimentWrapper.RobustChallengesEos.variantName,
					"carousel_banner",
					(CampaignDirector.robust.currentEventIndex + 1).ToString(),
					"view");
			}

			RobustChallengesObjectivesDialog.showDialog();
		}
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return RobustCampaign.hasActiveEUEChallenges;
	}

	public override bool getIsValidToSurface(string parameter, string eosExperiment="", string[] variantNames=null)
	{
		if (!string.IsNullOrEmpty(eosExperiment) && variantNames != null && variantNames.Length > 0)
		{
			return RobustCampaign.hasActiveEUEChallenges && System.Array.IndexOf(variantNames, CampaignDirector.robust.campaignID) >= 0;
		}
		return RobustCampaign.hasActiveEUEChallenges;
	}

	public override GameTimer getTimer(string parameter)
	{
		if (CampaignDirector.robust != null)
		{
			return CampaignDirector.robust.timerRange.endTimer;
		}
		return null;
	}
}