using UnityEngine;
using System.Collections;

public class DoSomethingVipEmeraldGame : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		VIPLobby.highlightGameLevel = (int)VIPLevelEnum.EMERALD;
		MOTDFramework.queueCallToAction(MOTDFramework.VIP_ROOM_CALL_TO_ACTION);
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		// See if there is a game for the emerald level.
		LobbyInfo lobbyInfo = LobbyInfo.find(LobbyInfo.Type.VIP);


		if (lobbyInfo != null && lobbyInfo.allLobbyOptions != null)
		{
			foreach (LobbyOption option in lobbyInfo.allLobbyOptions)
			{
				if (option.game != null &&
				    option.game.vipLevel != null &&
				    option.game.vipLevel.levelNumber == (int)VIPLevelEnum.EMERALD
				)
				{
					return true;
				}
			}
		}
		else
		{
			Debug.LogError("DomSomethingVIPEmeraldGame - Missing lobby info");
		}
		
		return false;
	}
}
