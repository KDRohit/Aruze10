using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

public class MOTDDialogDataPostPurchaseChallenge : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			PostPurchaseChallengeCampaign campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
			return campaign != null && campaign.isEarlyEndActive && campaign.isLocked && PostPurchaseChallengeCampaign.pendingLostData == null;
		}
	}

	public override string noShowReason
	{
		get
		{
			// There's several reasons why robust challenges could be inactive,
			// so I created RobustChallenges.notActiveReason to get specific here.
			string reason = "";
			PostPurchaseChallengeCampaign campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
			if (campaign != null)
			{
				reason = campaign.notActiveReason;
			}
			return base.noShowReason + reason;
		}
	}

	public override bool show()
	{
		if (!Scheduler.hasTaskWith(CampaignDirector.POST_PURCHASE_CHALLENGE + "_dialog")) 
		{ 
			PostPurchaseChallengeDialog.showDialog();
			return true;
		}
		return false;
	}

	new public static void resetStaticClassData()
	{
	}
}
