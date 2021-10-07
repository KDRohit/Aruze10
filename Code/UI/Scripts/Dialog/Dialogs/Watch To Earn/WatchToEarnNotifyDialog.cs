using UnityEngine;
using System.Collections;
using TMPro;

public class WatchToEarnNotifyDialog : DialogBase
{
	public Renderer backgroundRenderer;
	public string	statFamily = "";
	public  string audioToPlay;

	public override void init()
	{
		if (backgroundRenderer != null)
		{
			downloadedTextureToRenderer(backgroundRenderer, 0);
		}

		StatsManager.Instance.LogCount("dialog", statFamily, "", WatchToEarn.lastKnownSrc, "", "view");

		Audio.play(audioToPlay);
	}
	
	void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	public void closeClicked()
	{
		StatsManager.Instance.LogCount("dialog", statFamily, "",  WatchToEarn.lastKnownSrc, "ok", "click");
		Dialog.close();
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	public static void showSorry()
	{
		Dialog.instance.showDialogAfterDownloadingTextures("w2e_sorry", "misc_dialogs/watch_to_earn/W2E_Coins.png");
	}	

	public static void showThanks()
	{
		// BY: 2019-09-03 this dialog no longer exists, the thanks is part of the collect. leaving this here in case we
		// re-add it later
		//Dialog.instance.showDialogAfterDownloadingTextures("w2e_thanks", "misc_dialogs/watch_to_earn/W2E_Coins.png");
	}
}