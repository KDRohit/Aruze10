using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoSomethingPostPurchaseChallenge : DoSomethingAction
{
	public override bool getIsValidToSurface(string parameter)
	{
		// Only show the Collections carousel if collections is active
		PostPurchaseChallengeCampaign campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
		return campaign != null && campaign.isEarlyEndActive;
	}

	public override void doAction(string parameter)
	{
		if (PostPurchaseChallengeCampaign.isAnyPostPurchaseChallengeActive)
		{
			PostPurchaseChallengeDialog.showDialog();
		}
	}

	public override GameTimer getTimer(string parameter)
	{
		PostPurchaseChallengeCampaign campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
		if (campaign != null && campaign.isEarlyEndActive)
		{
			return new GameTimer(campaign.timerRange.timeRemaining);
		}

		return null;
	}
}
