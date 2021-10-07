using UnityEngine;
using System.Collections;

public class DoSomethingVipRoom : DoSomethingAction
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
			// Otheriwse queue up the normal flow.
			MOTDFramework.queueCallToAction(MOTDFramework.VIP_ROOM_CALL_TO_ACTION);
		}
	}
}
