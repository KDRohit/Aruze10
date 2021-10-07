using UnityEngine;
using TMPro;
using System.Collections;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class DailyChallengeMOTD : DialogBase
{
	public TextMeshPro bodyMessageLabel;
	public Renderer gameIconRenderer;
	
	public override void init()
	{
		StatsManager.Instance.LogCount("dialog", "daily_challenge_motd", "daily_challenge", DailyChallenge.gameKey, "", "view");
		downloadedTextureToRenderer(gameIconRenderer, 0); // Set the background texture to the downloaded image.
		updateTimer();
		MOTDFramework.markMotdSeen(dialogArgs);

		if (!DailyChallenge.challengeActive())
		{
			Debug.LogError("Daily Challenge is not Active!  Time has expired!");
		}
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		DailyChallenge.lastSeenAnnouncementDialog = GameTimer.currentTime;
		PlayerAction.saveCustomTimestamp(DailyChallenge.LAST_SEEN_MOTD_TIMESTAMP_KEY);
	}
	
	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(clickClose);
		updateTimer();
	}

	// NGUI button callback (this should be named with "clicked" and a comment to indicate that it's a button callback)
	public virtual void beginChallengeClicked()
	{
		if (GameState.isMainLobby)
		{
			StatsManager.Instance.LogCount("dialog", "daily_challenge_motd", "daily_challenge", DailyChallenge.gameKey, "okay", "click");
			// Use the DoSomething system because it uses the MOTDFramework's call to action system,
			// which guarantees that only one MOTD call to action is actually acted upon, and only
			// done so after all MOTD's have been shown and closed.
			// This means that if another MOTD offers to play a different game or visit the VIP lobby,
			// and someone touches that after touching this, then the other one will happen instead of this.
			DoSomething.now(DoSomething.GAME_PREFIX, DailyChallenge.gameKey);
		}
		clickClose();	
	}

	// Override it to customize timer update.
	protected virtual void updateTimer()
	{
	}

	public virtual void clickClose()
	{
		Audio.play("minimenuclose0");
		StatsManager.Instance.LogCount("dialog", "daily_challenge_motd", "daily_challenge", DailyChallenge.gameKey, "close", "click");
		Dialog.close();
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
	}

	public static bool showDialog(string motdKey = "")
	{
		Dict args = Dict.create(
			D.CALLBACK, new System.Action<string>(showDialogHelper),
			D.MOTD_KEY, motdKey
		);
		DailyChallenge.getChallengeProgressFromServer(args);
		return true;
	}

	// Note: motdKey is for MOTDFramework.markMotdSeen() call
	private static void showDialogHelper(string motdKey)
	{
		string gameKey = DailyChallenge.gameKey;
		LobbyGame gameInfo = null;

		if (!string.IsNullOrEmpty(gameKey))
		{
			// grab the lobby info for this game
			gameInfo = LobbyGame.find(gameKey);
		}
		else
		{
			// fallback to checking if we are already in a game and can just grab it
			gameInfo = GameState.game;
		}

		if (gameInfo != null)
		{
			// Multiprogressive mode uses a 1X1 image, which is achieved by passing in "".
			string filename = SlotResourceMap.getLobbyImagePath(gameInfo.groupInfo.keyName, gameInfo.keyName);
			Dialog.instance.showDialogAfterDownloadingTextures("daily_challenge_motd", new string[]{DailyChallengeMOTDHIR.BACKGROUND_IMAGE_PATH}, Dict.create(D.MOTD_KEY, motdKey), nonMappedBundledTextures:new string[]{filename});
		}
		else
		{
			Debug.LogErrorFormat("DailyChallengeMOTD.cs -- showDialog -- Trying to show a Daily Challenge MOTD for a game key {0} that does not exist.", gameKey);
		}
	}
}
