using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InboxUnlockGameCommand : InboxCommand
{
	public const string UNLOCK_GAME = "unlock_game";

	public override void execute(InboxItem inboxItem)
	{
		Dialog.close();
		
		if (!string.IsNullOrEmpty(inboxItem.gameKey))
		{
			LobbyGame game = LobbyGame.find(inboxItem.gameKey);
			if (game != null)
			{
				if (game.isVIPGame)
				{
					LobbyLoader.lastLobby = LobbyInfo.Type.VIP;
				}
				else if (game.isMaxVoltageGame)
				{
					LobbyLoader.lastLobby = LobbyInfo.Type.MAX_VOLTAGE;
				}
				Overlay.instance.top.showLobbyButton();
				GameState.pushGame(game);
				Loading.show(Loading.LoadingTransactionTarget.GAME);
				Glb.loadGame();
			}
		}
	}
	
	/// <inheritdoc/>
	public override string actionName
	{
		get { return UNLOCK_GAME; }
	}	
}
