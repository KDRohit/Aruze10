using UnityEngine;
using System.Collections;
using TMPro;

public class UnlockAllGamesMotd : DialogBase
{
	public const string DO_SOMETHING = "unlock_all_games";

	public ButtonHandler okayButton;
	
	public Renderer backgroundTexture;
	public TextMeshPro timerLabel;
	public UIAnchor stopWatchAnchor;

	private bool hasEventEnded = false;
	
	public override void init()
	{
		hasEventEnded = (bool)dialogArgs.getWithDefault(D.OPTION, false);
		StatsManager.Instance.LogCount("dialog", "all_games_unlocked", "motd", "", "", "view");
		
		if (!downloadedTextureToRenderer(backgroundTexture, 0))
		{
			backgroundTexture.gameObject.SetActive(false);
		}
		okayButton.registerEventDelegate(okayClicked);
		UnlockAllGamesFeature.instance.unlockAllGamesTimer.registerLabel(timerLabel, GameTimerRange.TimeFormat.REMAINING, true);
		Audio.play("minimenuopen0");
		MOTDFramework.markMotdSeen(dialogArgs);

		if (stopWatchAnchor)
		{
			stopWatchAnchor.gameObject.SetActive(true);
		}
	}
	
	public void Update()
	{
		AndroidUtil.checkBackButton(onCloseButtonClicked);
	}

	private void okayClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "all_games_unlocked", "motd", "", "Ok", "click");
		Dialog.close();

		if (hasEventEnded)
		{
			Glb.resetGame("forced game refresh: unlock all games ended");
		}
	}	
	
	public override void onCloseButtonClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount("dialog", "all_games_unlocked", "motd", "", "close", "click");
		Dialog.close();

		if (hasEventEnded)
		{
			Glb.resetGame("forced game refresh: unlock all games ended");
		}
	}
	
	// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		// Do special cleanup.
	}
	
	public static bool showDialog(string motdKey = "", bool isEventOver = false)
	{
		Dict args = Dict.create(
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.MOTD_KEY, motdKey,
			D.OPTION, isEventOver
		);
		
		string filePath = "motd/allgamesopen_start_windowed.png";

		if (isEventOver)
		{
			filePath = "motd/allgamesopen_end_windowed.png";
		}

		Dialog.instance.showDialogAfterDownloadingTextures("unlock_all_games_motd", filePath, args);
		return true;
	}
}
