using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JackpotUnlockGameMotd : DialogBase
{
	public UITexture backgroundTexture;
	public ButtonHandler closeBtn;

	private const string BACKGROUND_TEXTURE_FILEPATH = "progressive_jackpot/jackpot_unlock_game_dialogBG.png";
	
	void Update()
	{
		AndroidUtil.checkBackButton(clickClose);
	}
	
	public override void init()
	{
		closeBtn.registerEventDelegate(clickClose);
		downloadedTextureToUITexture(backgroundTexture, 0);
		MOTDFramework.markMotdSeen(dialogArgs);
	}
	
	// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		// Do special cleanup.
	}
	
	private void clickClose(Dict args = null)
	{
		Dialog.close();
	}
	
	private void playNowClicked()
	{
		if (ProgressiveJackpot.allGames != null)
		{
			List<LobbyGame> jackpotUnlockGames = new List<LobbyGame>();
				
			foreach (LobbyGame progressiveGame in ProgressiveJackpot.allGames)
			{
				if (progressiveGame.progressiveJackpots[0].shouldGrantGameUnlock &&
					progressiveGame.vipLevel == null &&
					LobbyOption.activeGameOption(progressiveGame) != null)
				{
					jackpotUnlockGames.Add(progressiveGame);
				}
			}
			if (jackpotUnlockGames.Count > 0)
			{
				int randomIndex = Random.Range(0, jackpotUnlockGames.Count);
				LobbyGame game = jackpotUnlockGames[randomIndex];
				// Use the scripting system the same as the generic MOTD does,
				// which queues the game up so it launches after all dialogs are closed.
				DoSomething.now(DoSomething.GAME_PREFIX, game.keyName);
			}
			else
			{
				Debug.LogError("JackpotUnlockGameMotd -- game not found for random progressive jackpot");
			}
		}

		Dialog.close();
	}
		
	public static bool showDialog(string motdKey = "")
	{
		Dict args = Dict.create(
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.MOTD_KEY, motdKey
		);
		
		Dialog.instance.showDialogAfterDownloadingTextures(
			"jackpot_unlock_game_motd",
			BACKGROUND_TEXTURE_FILEPATH,
			args,
			true
		);
		return true;
	}
}
