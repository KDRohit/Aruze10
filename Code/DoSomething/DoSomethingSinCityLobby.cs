using UnityEngine;
using System.Collections;

public class DoSomethingSinCityLobby : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		if (LobbyLoader.instance == null)
		{
			// If this is on a cold start and so the game is loading up, then use first time logic.
			LobbyLoader.lastLobby = LobbyInfo.Type.SIN_CITY;
			MainLobby.isFirstTime = false;
		}
		else
		{
			MOTDFramework.queueCallToAction
			(
				  MOTDFramework.CHALLENGE_LOBBY_CALL_TO_ACTION
				, Dict.create(D.CAMPAIGN_NAME, CampaignDirector.SIN_CITY,
							  D.DATA, SinCityLobby.BUNDLE,
							  D.IS_WAITING, SinCityLobby.isBeingLazilyLoaded,
				              D.ANSWER, (LobbyLoader.onLobbyLoad)SinCityLobby.onReload)
			);
		}
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
        ChallengeLobbyCampaign campaign = CampaignDirector.find(CampaignDirector.SIN_CITY) as ChallengeLobbyCampaign;
        return campaign != null && ChallengeCampaign.hasActiveInstance(CampaignDirector.SIN_CITY);
	}
}
