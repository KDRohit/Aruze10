using UnityEngine;
using System.Collections;

public class DoSomethingMaxVoltageLobby : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		if (LobbyLoader.instance == null)
		{
			// If this is on a cold start and so the game is loading up, then use first time logic.
			LobbyLoader.lastLobby = LobbyInfo.Type.MAX_VOLTAGE;
			MainLobby.isFirstTime = false;
		}
		else
		{
			MOTDFramework.queueCallToAction(MOTDFramework.MAX_VOLTAGE_LOBBY_CALL_TO_ACTION);
		}
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
        return true;
	}
}
