using UnityEngine;
using System.Collections;
using TMPro;

/*
Handles the deluxe games MOTD dialog display.
*/

public class DeluxeGamesMOTD : DialogBase
{
	private const string BACKGROUND_PATH = "misc_dialogs/deluxe_games/Deluxe_Games_MOTD_BG.png";	// Is in asset bundle.
	
	public Renderer background;
	public Renderer gameIcon;
	
	public override void init()
	{
		downloadedTextureToRenderer(background, 0);
		downloadedTextureToRenderer(gameIcon, 1);

		StatsManager.Instance.LogCount(
			counterName:"dialog",
			kingdom:	"deluxe_games",
			genus:		"view"
		);
		MOTDFramework.markMotdSeen(dialogArgs);
	}
	
	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	// NGUI button callback
	private void visitClicked()
	{
		Dialog.close();
		StatsManager.Instance.LogCount(
			counterName:"dialog",
			kingdom:	"deluxe_games",
			family:		"visit_vip_room",
			genus:		"click"
		);
		
		// Use the scripting system so that the VIP room navigation actually happens after all dialogs are closed.
		DoSomething.now("vip_room");
	}

	// NGUI button callback
	private void closeClicked()
	{
		Dialog.close();
		StatsManager.Instance.LogCount(
			counterName:"dialog",
			kingdom:	"deluxe_games",
			family:		"close",
			genus:		"click"
		);
	}
	
	public static bool showDialog(string motdKey = "")
	{
		// Find a deluxe game to show on the dialog.		
		if (LobbyGame.deluxeGames.Count == 0)
		{
			Debug.LogWarning("No deluxe games found to show on the MOTD.");
			return false;
		}

		// Just use the first game found for now. Maybe some day we'll use a random game from the list.
		LobbyGame game = LobbyGame.deluxeGames[0];
		
		string[] texturePaths = new string[]
		{
			BACKGROUND_PATH,
		};

		string[] gameImagePath = new string[]
		{
			SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName)
		};
		
		Dialog.instance.showDialogAfterDownloadingTextures("deluxe_games_motd", texturePaths, Dict.create(D.MOTD_KEY, motdKey), nonMappedBundledTextures:gameImagePath);
		return true;
	}
	
	// Do NOT call -- called by Dialog.close()
	public override void close()
	{
		// Clean up here.
	}
}
