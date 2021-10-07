using UnityEngine;
using System.Collections;
using TMPro;

public class MaxVoltageNewGameDialog : DialogBase
{
	[SerializeField] private Renderer background;
	[SerializeField] private TextMeshPro textLabel;
	[SerializeField] private ButtonHandler playHandler;
	[SerializeField] private GameObject gameLocked;
	[SerializeField] private TextMeshPro gameLockedLabel;

	private string action;
	private LobbyGame game = null;
	private string statName = "";

	public override void init()
	{
		Audio.play("MVNewGameFanfare");
		game = dialogArgs.getWithDefault(D.DATA, null) as LobbyGame;

		downloadedTextureToRenderer(background, 0);
		 
		if (!game.isUnlocked)
		{
			SafeSet.gameObjectActive(gameLocked, false);
		}
		else
		{
			SafeSet.gameObjectActive(gameLocked, true);
			gameLockedLabel.text = "LVL " + game.unlockLevel.ToString();
		}

		string localizationBodyText = string.Format("motd_{0}_body_text", game.keyName);
		textLabel.text = Localize.text(localizationBodyText);
		playHandler.registerEventDelegate(playClicked);
		MOTDFramework.markMotdSeen(dialogArgs);
	}

	public override void onCloseButtonClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "max_voltage_new_game", statName, "close", "click");
		Dialog.close();
	}

	public void playClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "max_voltage_new_game", statName, action, "click");
		Dialog.close();
		DoSomething.now("max_voltage_lobby");
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		MOTDFramework.markMotdSeen(Dict.create(D.MOTD_KEY, "new_game_mvz"));
		StatsManager.Instance.LogCount("dialog", "max_voltage_new_game", "", "", game.keyName, "close");
	}

	public static bool showDialog(string gameKey, string motdKey)
	{
		if (string.IsNullOrEmpty(gameKey))
		{
			Debug.LogErrorFormat("MOTDDialog::showDialog - Empty background texture setting for MOTDDialogData key: {0}", gameKey);
			return false;
		}

		LobbyGame game = LobbyGame.find(gameKey);

		if (game == null)
		{
			Debug.LogError("MaxVoltageNewGameDialog::init - The game we looked up was null");
			return false;
		}
		else
		{
			Dict gameDict = Dict.create(D.DATA, game);
			if (!string.IsNullOrEmpty(motdKey))
			{
				gameDict.Add(D.MOTD_KEY, motdKey);
			}
			string imagePathURL = string.Format(MOTDDialogDataNewGame.NEW_GAME_MOTD_PNG, gameKey);
			Dialog.instance.showDialogAfterDownloadingTextures("max_voltage_new_game", imagePathURL, gameDict);
			return true;
		}
	}
}
