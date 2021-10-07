using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogPartnerPowerup : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return ExperimentWrapper.PartnerPowerup.isInExperiment
				&& CampaignDirector.partner != null	
				&& CampaignDirector.partner.isEnabled 
				&& CampaignDirector.partner.timerRange.isActive
				&& CampaignDirector.partner.state != ChallengeCampaign.COMPLETE; // No need to show them this, they already won.
		}
	}

	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;
			if (!ExperimentWrapper.PartnerPowerup.isInExperiment)
			{
				result += "Experiment is off.\n";
			}
			if (CampaignDirector.partner == null)
			{
				result += "The Partner Powerup object is null";

				// No reason to go any further
				return result;
			}
			if (!CampaignDirector.partner.isEnabled)
			{
				result += "Partner powerup not enabled";
			}

			return result;
		}
	}

	public override bool show()
	{
	    Dict args = Dict.create(D.TYPE, "co_op_challenge_motd");

	    int hasSeenStartupAnim = PlayerPrefsCache.GetInt(Prefs.HAS_SHOWN_PPU_START, 0);

	    if (hasSeenStartupAnim == 0)
	    {
	       args.Add(D.DATA, "START");
	    }

	    if (SocialMember.isFriendsPopulated)
		{
			// show intro animation if we have to.
			PartnerPowerupIntroDialog.showDialog(args);
		}
		else
		{
			RoutineRunner.instance.StartCoroutine(waitForSocialMember(args));
		}
		return true;
	}

	public IEnumerator waitForSocialMember(Dict args)
	{
		while (!SocialMember.isFriendsPopulated || CampaignDirector.partner == null)
		{
			yield return null;
		}

		PartnerPowerupIntroDialog.showDialog(args);

		yield return null;
	}

	new public static void resetStaticClassData()
	{
	}
}
