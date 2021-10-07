using UnityEngine;
using System.Collections;
using TMPro;

/**
GenericDialog

This is a dialog that can be used for random Yes/No questions, etc.
You pass in translated strings, then get back whatever option the user selected.
Initially created to confirm the user logging out.
**/
public class DailyBonusReducedTimeMOTD : DialogBase
{	
	public Renderer backgroundRenderer;
	public TextMeshPro timerLabel;
	
	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);

		Audio.play("minimenuopen0");
		MOTDFramework.markMotdSeen(dialogArgs);

		// Stats track for server
		StatsManager.Instance.LogCount("dialog", "reduced_bonus_collection_event", "", "", "", "view");
	}
		
	protected virtual void Update()
	{
	}

	public void closeClicked()
	{
		// Stats track for server
		StatsManager.Instance.LogCount("dialog", "reduced_bonus_collection_event", "", "", "ok", "click");
		Dialog.close();
	}
			
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}
	
	public static bool showDialog(string motdKey = "")
	{
		Dict args = Dict.create(
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.MOTD_KEY, motdKey
		);
		
		string[] urls;
		urls = new string[] {DailyBonusReducedTimeMOTDHIR.BACKGROUND_PATH, DailyBonusReducedTimeMOTDHIR.BACKGROUND_ANIM_PATH};

		Dialog.instance.showDialogAfterDownloadingTextures(
			"daily_bonus_reduced_time",
			urls,
			args
		);
		return true;
	}
}
