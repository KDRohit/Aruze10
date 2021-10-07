using UnityEngine;
using System.Collections;
using TMPro;

public class NewGameMOTD : DialogBase
{
	[SerializeField] private Renderer background;
	[SerializeField] private TextMeshPro textLabel;
	[SerializeField] private ButtonHandler closeHandler;
	[SerializeField] private ButtonHandler playHandler;
	[SerializeField] private GameObject gameLocked;
	[SerializeField] private TextMeshPro gameLockedLabel;
	
	private string action;
	private LobbyGame game = null;
	private string statName = "";
	
	public override void init()
	{
		MOTDDialogDataNewGame data = dialogArgs.getWithDefault(D.DATA, null) as MOTDDialogDataNewGame;
		action = data.commandAction1;
		game = data.action1Game;
		statName = data.statName;
		
		downloadedTextureToRenderer(background, 0);
		MOTDDialogDataNewGame newGameData = MOTDDialogData.newGameMotdData;
		if (newGameData != null)
		{
			newGameData.markNewGameSeen(game);
		}

		if (!game.isUnlocked)
		{
			SafeSet.gameObjectActive(gameLocked, true);
			gameLockedLabel.text = "LVL " + game.unlockLevel.ToString();
		}
		else
		{
			SafeSet.gameObjectActive(gameLocked, false);
		}

		textLabel.text = Localize.text(data.locBodyText);
		
		closeHandler.registerEventDelegate(closeClicked);
		playHandler.registerEventDelegate(playClicked);
	}

	public void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "new_game_motd", statName, "close", "click");
		dialogArgs.merge(D.ANSWER, "no");
		Dialog.close();
	}

	public void playClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "new_game_motd", statName, action, "click");
		dialogArgs.merge(D.ANSWER, "1");
		Dialog.close();
		DoSomething.now(action);
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		if (game != null)
		{
			StatsManager.Instance.LogCount("dialog", "new_game_motd", "", "", game.keyName, "close");
		}
	}
	
	public static bool showDialog(MOTDDialogDataNewGame myData)
	{
		Dict args = Dict.create
		(
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.DATA, myData,
			D.MOTD_KEY, myData.keyName
		);
		
		if (string.IsNullOrEmpty(myData.imageBackground))
		{
			Debug.LogErrorFormat("MOTDDialog::showDialog - Empty background texture setting for MOTDDialogData key: {0}", myData.keyName);
			return false;
		}
		else
		{
			Dialog.instance.showDialogAfterDownloadingTextures("motd_new_game", myData.imageBackground, args);
			return true;
		}
	}
}
