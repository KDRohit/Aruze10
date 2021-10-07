using UnityEngine;
using System.Collections;
using TMPro;

public class WatchToEarnFeatureMOTD : DialogBase
{
	public Renderer backgroundRenderer;
	public Renderer playIconRenderer;
	public TextMeshPro rewardLabel;

	// Initialization
	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);
		downloadedTextureToRenderer(playIconRenderer, 1);

		SafeSet.labelText(rewardLabel, Localize.text("w2e_earn_more_coins", CreditsEconomy.multiplyAndFormatNumberAbbreviated(WatchToEarn.rewardAmount)));

		MOTDFramework.markMotdSeen(dialogArgs);

		StatsManager.Instance.LogCount("dialog", "w2e", "motd", "", "", "view");

		Audio.play("W2EFeatureIntroMOTD");
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	/// Used by UIButtonMessage
	public void okClicked()
	{
		StatsManager.Instance.LogCount("dialog", "w2e", "motd", "", "ok", "click");

		Dialog.close();
	}	

	/// Used by UIButtonMessage
	public void closeClicked()
	{
		StatsManager.Instance.LogCount("dialog", "w2e", "motd", "", "close", "click");

		Dialog.close();
	}

	public static void showDialog(string motdKey = "")
	{
		Dict args = Dict.create(
			D.MOTD_KEY, motdKey
		);

		string[] texturePaths = new string[]
		{
			"misc_dialogs/watch_to_earn/MOTD_Art_02.png",
			"misc_dialogs/watch_to_earn/Icon_Play.png"
		};

		Dialog.instance.showDialogAfterDownloadingTextures("watch_to_earn", texturePaths, args);
	}

}
