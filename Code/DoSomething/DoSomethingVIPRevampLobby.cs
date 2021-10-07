using UnityEngine;
using System.Collections;

public class DoSomethingVIPRevampLobby : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		if (LobbyLoader.instance == null)
		{
			// If this is on a cold start and so the game is loading up, then use first time logic.
			LobbyLoader.lastLobby = LobbyInfo.Type.VIP;
			MainLobby.isFirstTime = false;
		}
		else
		{
			// Otherwise queue up the normal flow.
			// This will either wait until all dialogs are closed,
			// or go immediately if no dialogs are currently opened (like when called from the lobby option).
			MOTDFramework.queueCallToAction(MOTDFramework.VIP_ROOM_CALL_TO_ACTION);
		}
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
        return ExperimentWrapper.VIPLobbyRevamp.isInExperiment;
	}
}
