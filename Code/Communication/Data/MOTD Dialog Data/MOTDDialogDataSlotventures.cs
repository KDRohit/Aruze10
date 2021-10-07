using Com.HitItRich.EUE;
using System.Text;

/*
Override for special behavior.
*/

public class MOTDDialogDataSlotventures : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			ChallengeCampaign slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID);

			// This will get caught in the cases below, but prevent NRE here.
			if (slotventureCampaign != null)
			{
				if (ExperimentWrapper.Slotventures.isEUE)
				{
					return false;
				}

				// This will prevent MOTDs for slotventures from stacking up
				if (keyName == "motd_slotventures")
				{
					if (slotventureCampaign.state != ChallengeCampaign.IN_PROGRESS)
					{
						return false;
					}
				}
				else if (keyName == "motd_slotventures_complete")
				{
					if (slotventureCampaign.state != ChallengeCampaign.COMPLETE)
					{
						return false;
					}
				}
				else if (keyName == "motd_slotventures_ended")
				{
					if (slotventureCampaign.state != ChallengeCampaign.INCOMPLETE)
					{
						return false;
					}
				}
			}

			return EUEManager.isComplete && 
			       ExperimentWrapper.Slotventures.isInExperiment && 
				   SlotsPlayer.instance.socialMember.experienceLevel >= ExperimentWrapper.Slotventures.levelLock && 
				   slotventureCampaign != null;
		}
	}

	public override string noShowReason
	{
		get
		{
			StringBuilder result = new StringBuilder();
			result.Append(base.noShowReason);

			if(!ExperimentWrapper.Slotventures.isInExperiment)
			{
				result.Append(" Not in  experiment");
			}

			if (!EUEManager.isComplete)
			{
				result.Append(" EUE FTUE not complete");
			}

			if (ExperimentWrapper.Slotventures.isEUE)
			{
				result.Append(" User is in EUE slotventures");
			}

			if (SlotsPlayer.instance.socialMember.experienceLevel < ExperimentWrapper.Slotventures.levelLock)
			{
				result.Append(" Not high enough level");
			}

			ChallengeCampaign slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID);
			if(slotventureCampaign == null)
			{
				result.Append(" campaign was null");
			}

			return result.ToString();
		}
	}

	public override bool show()
	{
		ChallengeCampaign slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID);

		if (slotventureCampaign == null)
		{
			UnityEngine.Debug.LogError("slotventureCampaign was null");
			return false;
		}

		int secondsLeft = slotventureCampaign.timerRange.endTimestamp - GameTimer.currentTime;
	
		if (keyName == "motd_slotventures")
		{
			if (slotventureCampaign.replayCount > 0)
			{
				return SlotventuresMOTD.showDialog(keyName, SlotventuresMOTD.DialogState.EVENT_RESTART_LOBBY);
			}
			else
			{
				return SlotventuresMOTD.showDialog(keyName, SlotventuresMOTD.DialogState.MOTD);
			}
		}
		else if (keyName == "motd_slotventures_complete")
		{
			return SlotventuresMOTD.showDialog(keyName, SlotventuresMOTD.DialogState.EVENT_COMPLETE);
		}
		else if (keyName == "motd_slotventures_ended") 
		{
			return SlotventuresMOTD.showDialog(keyName, SlotventuresMOTD.DialogState.EVENT_ENDED);
		}
		return false;
	}

	new public static void resetStaticClassData()
	{
	}

}
