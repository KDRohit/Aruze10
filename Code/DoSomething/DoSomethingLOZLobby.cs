using UnityEngine;
using System.Collections;

public class DoSomethingLOZLobby : DoSomethingAction
{	
	public override void doAction(string parameter)
	{
		if (LobbyLoader.instance == null)
		{
			// If this is on a cold start and so the game is loading up, then use first time logic.
			LobbyLoader.lastLobby = LobbyInfo.Type.LOZ;
			MainLobby.isFirstTime = false;
		}
		else
		{
			// Otherwise queue up the normal flow.
			// This will either wait until all dialogs are closed,
			// or go immediately if no dialogs are currently opened (like when called from the lobby option).
			MOTDFramework.queueCallToAction
			(
				  MOTDFramework.LOZ_LOBBY_CALL_TO_ACTION
				, Dict.create(D.CAMPAIGN_NAME, CampaignDirector.LOZ_CHALLENGES)
			);
		}
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		LOZCampaign campaign = CampaignDirector.find(CampaignDirector.LOZ_CHALLENGES) as LOZCampaign;
        return campaign != null && ChallengeCampaign.hasActiveInstance(CampaignDirector.LOZ_CHALLENGES);
	}
}
