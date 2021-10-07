using Com.Scheduler;
using UnityEngine;
using TMPro;

public class DailyChallengeOver : DialogBase
{
	private const string dialogKey = "daily_challenge_over";
	
	public Renderer backgroundRenderer;
	public TextMeshPro instructionText;
	
	public override void init()
	{
		StatsManager.Instance.LogCount("dialog", "daily_challenge_over", "daily_challenge", DailyChallenge.gameKey, "", "view");
		downloadedTextureToRenderer(backgroundRenderer, 0);
		instructionText.text = DailyChallenge.endDialogMainText;

		Audio.play("minimenuopen0");
		MOTDFramework.markMotdSeen(dialogArgs);			
	}
	
	void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

    protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		// Mark this dialog timestamp as seen (we are using timestamp instead of customPlayerData because
		// web did it weird and we need to not show the dialog twice.
		DailyChallenge.lastSeenOverDialog = GameTimer.currentTime;
		PlayerAction.saveCustomTimestamp(DailyChallenge.LAST_SEEN_OVER_TIMESTAMP_KEY);
	}
	
	public void closeClicked()
	{
		StatsManager.Instance.LogCount("dialog", "daily_challenge_over", "daily_challenge", DailyChallenge.gameKey, "close", "click");
		Audio.play("minimenuclose0");
		Dialog.close();
	}
			
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}
	
	public static void showDialog()
	{
		Dialog.instance.showDialogAfterDownloadingTextures(dialogKey, "404.png", Dict.create(D.PRIORITY, SchedulerPriority.PriorityType.IMMEDIATE));
	}
}
