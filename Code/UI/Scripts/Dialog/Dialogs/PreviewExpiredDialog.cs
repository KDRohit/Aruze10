using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
Handles display of dialog for telling a player that a touched game is locked.
*/

public class PreviewExpiredDialog : DialogBase
{
	public TextMeshPro subTitle;
	public TextMeshPro description;
	public TextMeshPro levelLabel;
	public Renderer gameTexture;
	
	/// Initialization
	public override void init()
	{
		if (GameState.game == null)
		{
			Debug.LogError("PreviewExpiredDialog: We shouldn't be showing this dialog without a game on the stack.");
		}
		else
		{
			Bugsnag.LeaveBreadcrumb("PreviewExpiredDialog showing for game " + GameState.game.keyName);
			
			if (MainLobby.wasUnlockAllGames && (UnlockAllGamesFeature.instance == null || !UnlockAllGamesFeature.instance.isEnabled))
			{
				subTitle.text = Localize.text("unlock_all_games_expired");
			}
			else
			{
				subTitle.text = Localize.text("preview_expired");
			}
			
			description.text = Localize.text("reach_level_{0}_to_unlock_game", GameState.game.unlockLevel);				
			levelLabel.text = CommonText.formatNumber(GameState.game.unlockLevel);

			downloadedTextureToRenderer(gameTexture, 0);
		}

		Audio.play("minimenuopen0");
	}
	
	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		
		if (GameState.game == null)
		{
			// If no option is assigned, something went wrong, so just close immediately.
			// The specific error message has already been logged in init().
			Dialog.close();
		}
	}
	
	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}
	
	public void lobbyClicked()
	{
		returnToLobby();
	}

	public void closeClicked()
	{
		returnToLobby();
	}
	
	private void returnToLobby()
	{
		Dialog.close();
		if (MainLobby.wasUnlockAllGames)
		{
			// If all games were unlocked, then we need to totally refresh the game
			// in order to refresh the lobby games' locked status properly.
			// Do it directly instead of calling Server.forceGameRefresh() to avoid
			// showing a generic dialog in addition to this one, and also to avoid
			// auto-loading back into this same game, which is what happens whenever
			// that is called while in a game, because it assumes an error happened.
			Glb.resetGame("Unlock all games expired.");
		}
		else
		{
			Scheduler.addFunction(LobbyLoader.returnToLobbyAfterDialogCloses);
		}
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	public static void showDialog()
	{
		string filename = SlotResourceMap.getLobbyImagePath(GameState.game.groupInfo.keyName, GameState.game.keyName, "1X2");
		Dialog.instance.showDialogAfterDownloadingTextures("preview_expired", nonMappedBundledTextures:new string[]{filename});
	}
	
}
