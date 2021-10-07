﻿using UnityEngine;
using System.Collections;

public class DoSomethingPartnerPowerup : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		StatsManager.Instance.LogCount(counterName: "bottom_nav", 
									   kingdom: "carousel", 
									   phylum:"carousel_item", 
									   klass: "co_op_challenge",
									   genus: "click");
		
		PartnerPowerupIntroDialog.showDialog();
	}

	public override GameTimer getTimer(string parameter)
	{
		if (CampaignDirector.partner != null && CampaignDirector.partner.timerRange != null)
		{
			return CampaignDirector.partner.timerRange.endTimer;
		}
		else
		{
			if (CampaignDirector.partner == null)
			{
				Debug.LogError("DoSomethingPartnerPowerup::getTimer - Attempted to get the PPU timer without campaign info");
			}
			else
			{
				Debug.LogError("DoSomethingPartnerPowerup::getTimer - Attempted to get the PPU timer but it was null. Event expired?");
			}

			return null;
		}
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return ExperimentWrapper.PartnerPowerup.isInExperiment &&
			CampaignDirector.partner != null &&
			CampaignDirector.partner.timerRange != null &&
			CampaignDirector.partner.timerRange.isActive &&
			CampaignDirector.partner.state != "PENDING";
	}
}
