using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class SlotventuresEUETask : SchedulerTask
{
	public override void execute()
	{
		base.execute();

		// Resetting this here so I can stop explaining how to do it from the dev panel if a user does the reset process wrong
		if (GameExperience.totalSpinCount == 0)
		{
			CustomPlayerData.setValue(CustomPlayerData.SLOTVENTURES_CURRENT_LOBBY_LOAD, 0);
			CustomPlayerData.setValue(CustomPlayerData.SLOTVENTURES_HAS_SEEN_EUE, false);
		}

		SlotventuresChallengeCampaign campaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as SlotventuresChallengeCampaign;

		if (campaign != null && ExperimentWrapper.Slotventures.isEUE && !campaign.isComplete)
		{
			if (ExperimentWrapper.Slotventures.useDirectToMachine || ExperimentWrapper.Slotventures.useDirectToLobby)
			{
				int currentLoads = CustomPlayerData.getInt(CustomPlayerData.SLOTVENTURES_CURRENT_LOBBY_LOAD, 0);
				if
				(
					ExperimentWrapper.Slotventures.maxDirectToLobbyLoads == 0 ||
					currentLoads < ExperimentWrapper.Slotventures.maxDirectToLobbyLoads
				)
				{
					currentLoads++;
					CustomPlayerData.setValue(CustomPlayerData.SLOTVENTURES_CURRENT_LOBBY_LOAD, currentLoads);

					if (LobbyLoader.instance == null || ExperimentWrapper.Slotventures.useDirectToMachine)
					{
						// If this is on a cold start and so the game is loading up, then use first time logic.
						LobbyLoader.lastLobby = LobbyInfo.Type.SLOTVENTURE;
						MainLobby.isFirstTime = false;
					}
					else
					{
						MOTDFramework.queueCallToAction(MOTDFramework.SLOTVENTURE_LOBBY_CALL_TO_ACTION);
					}
				}

				if (GameExperience.totalSpinCount == 0 && ExperimentWrapper.Slotventures.useDirectToMachine && campaign.currentMission != null)
				{
					DoSomething.now("game", campaign.currentMission.currentObjective.game);
				}
			}
		}

		Scheduler.removeTask(this);
	}
}