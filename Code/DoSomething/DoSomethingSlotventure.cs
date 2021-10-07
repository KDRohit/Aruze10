using UnityEngine;
using System.Collections;

public class DoSomethingSlotventure : DoSomethingAction
{
	private ChallengeLobbyCampaign slotventureCampaign;
	public override void doAction(string parameter)
	{
		ChallengeLobbyCampaign slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as ChallengeLobbyCampaign;
		if (slotventureCampaign == null)
		{
			if (!EueFeatureUnlocks.hasFeatureUnlockData("sv_challenges"))
			{
				//Only log this error if we're not in the feature unlocks experiment where its valid to show this carousel without campaign data
				Debug.LogError("DoSomething -- SlotVenture:  Can't find slotventure campaign");
			}
			return;
		}

		switch(slotventureCampaign.state)
		{
			case ChallengeCampaign.COMPLETE:
				if (slotventureCampaign.canRestart())
				{
					SlotventuresMOTD.showDialog("", SlotventuresMOTD.DialogState.EVENT_RESTART_LOBBY);
				}
				else
				{
					SlotventuresMOTD.showDialog("", SlotventuresMOTD.DialogState.EVENT_COMPLETE);
					return;
				}
				break;
				

			case ChallengeCampaign.INCOMPLETE:
				SlotventuresMOTD.showDialog("", SlotventuresMOTD.DialogState.EVENT_ENDED);
				return;

		}

		Audio.play(SlotventuresLobby.assetData.audioMap[LobbyAssetData.TRANSITION]);

		if (LobbyLoader.instance == null)
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

	// This may become valid at some point
	//public override GameTimer getTimer(string parameter)
	//{
	//	return CampaignDirector.partner.timerRange.endTimer;
	//}

	public override bool getIsValidToSurface(string parameter)
	{
		//Always show if we're in the experiment, it will just say "coming soon"
		bool result = ExperimentWrapper.Slotventures.isInExperiment && 
		              (EueFeatureUnlocks.hasFeatureUnlockData("sv_challenges") || CampaignDirector.isCampaignEnabled(SlotventuresChallengeCampaign.CAMPAIGN_ID));
		
		//Show the lobby card if the feature is level locked, or we have an active campaign
		return result;
	}
}
