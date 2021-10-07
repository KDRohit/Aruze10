using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
using Zynga.Core.Util;

/*
A simple game unlocked dialog that doesn't show the next unlocked game or do any fancy animation timing.
It's designed to be used with different themes.
*/

public class GameUnlockedSimpleDialog : DialogBase
{
	public TextMeshPro unlockedGameLabel;
	public Renderer unlockedRenderer;
	
	protected LobbyGame unlockedGame = null;
	
	private string theme = "";

	private const string OPEN_SOUND = "UnlockNewGameFanfare";
	private const string CLOSE_SOUND = "UnlockNewGameCollect";

	[SerializeField] private ClickHandler closeHandler;
	[SerializeField] private ClickHandler playHandler;

	public override void init()
	{
		unlockedGame = dialogArgs.getWithDefault(D.OPTION, null) as LobbyGame;
		theme = (string)dialogArgs.getWithDefault(D.THEME, "");
		
		if (unlockedGame == null)
		{
			Dialog.close();
			return;
		}
		
		if (!downloadedTextureToRenderer(unlockedRenderer, 0))
		{
			// If the texture didn't download, then show the name of the game on a label.
			unlockedGameLabel.text = unlockedGame.name;
			unlockedGameLabel.gameObject.SetActive(true);
		}

		closeHandler.registerEventDelegate(closeClicked);
		playHandler.registerEventDelegate(playNowClicked);
		StatsManager.Instance.LogCount("dialog", "simple_game_unlock", "view", "", theme, "view");
	}

	protected override void playOpenSound()
	{
		Audio.play(OPEN_SOUND + theme);	
	}

	public override void playCloseSound()
	{
		Audio.play(CLOSE_SOUND + theme);	
	}
	
	public virtual void Update()
	{
		AndroidUtil.checkBackButton(closeClicked, "dialog", "simple_game_unlock", theme, StatsManager.getGameName(), "back", "click");

		if (shouldAutoClose)
		{
			cancelAutoClose();
			Dialog.close();
		}
	}
	
	// NGUI button callback.
	protected void playNowClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog","simple_game_unlock","", "", theme, "play");

		cancelAutoClose();

		// the button is just a plain old "Play" button so close dialog and play!
		Dialog.close();

		if (unlockedGame.isLOZGame)
		{
			StatsManager.Instance.LogCount("dialog", "loz_unlock_game", unlockedGame.keyName, "tier1", "play_now", "click");
		}
		else if (unlockedGame.isChallengeLobbyGame)
		{
			StatsManager.Instance.LogCount
			(
				"dialog"
				, ChallengeLobbyCampaign.currentCampaign.campaignID
				, "game_unlock"
				, "play_now"
				, ""
				, "click"
			);
		}
		
		// Load the game right now.
		// Tell the lobby which game to launch when finished returning to the lobby.
		PreferencesBase prefs = SlotsPlayer.getPreferences();
		prefs.SetString(Prefs.AUTO_LOAD_GAME_KEY, unlockedGame.keyName);
		prefs.Save();
		
		// First go back to the lobby and go through the common route to launching a game.
		Scheduler.addFunction(LobbyLoader.returnToLobbyAfterDialogCloses);
	}

	/// NGUI button callback.
	protected void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog","game_unlock","close", "", theme, "close");

		if (unlockedGame.isChallengeLobbyGame && ChallengeLobbyCampaign.currentCampaign != null)
		{
			StatsManager.Instance.LogCount
			(
				"dialog"
				, ChallengeLobbyCampaign.currentCampaign.campaignID
				, "game_unlock"
				, "close"
				, ""
				, "click"
			);
		}
		
		Dialog.close();
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}
	
    public static void showDialog(LobbyGame unlockedGame, string theme)
	{
		Dict args = Dict.create(
			D.OPTION, unlockedGame,
			D.THEME, theme
		);

        string filename = SlotResourceMap.getLobbyImagePath(unlockedGame.groupInfo.keyName, unlockedGame.keyName, "1X2");
		
		Dialog.instance.showDialogAfterDownloadingTextures("game_unlocked_simple_" + theme.ToLower(), nonMappedBundledTextures:new string[]{filename}, args:args);

	}
}
