using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class JackpotDaysMOTD : DialogBase
{
	public Renderer backgroundRenderer;
	public TextMeshPro jackpotLabel;
	public TextMeshPro timerLabel;
	public UIAnchor stopWatchAnchor;
	[SerializeField] private ButtonHandler buyButtonHandler;

	private const string BACKROUND_PATH = "motd/jackpotdays_announcement_windowed.png";

	/// Initialization
	public override void init()
	{
		if (timerLabel != null)
		{
			SlotsPlayer.instance.jackpotDaysTimeRemaining.registerLabel(timerLabel, GameTimerRange.TimeFormat.REMAINING, true);
		}

		if (stopWatchAnchor)
		{
			stopWatchAnchor.gameObject.SetActive(true);
		}

		if (jackpotLabel != null)
		{
			jackpotLabel.text = CreditsEconomy.convertCredits(ProgressiveJackpot.buyCreditsJackpot.pool);
		}
		downloadedTextureToRenderer(backgroundRenderer, 0);
		buyButtonHandler.registerEventDelegate(buyNowClicked);
		StatsManager.Instance.LogCount("dialog", "jackpot_days_motd", "", "", "", "view");
	}

	void Update()
	{
		AndroidUtil.checkBackButton(onCloseButtonClicked);
	}

	public void buyNowClicked(Dict args = null)
	{
		Dialog.close();
		BuyCreditsDialog.showDialog("", SchedulerPriority.PriorityType.IMMEDIATE);
		StatsManager.Instance.LogCount("dialog", "jackpot_days_motd", "", "", "buy_page", "click");
	}

	public override void onCloseButtonClicked(Dict args = null)
	{
		Dialog.close();
		StatsManager.Instance.LogCount("dialog", "jackpot_days_motd", "", "", "close", "click");
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	public static bool showDialog()
	{
		string[] backgrounds = {BACKROUND_PATH};
		Dialog.instance.showDialogAfterDownloadingTextures("jackpot_days_motd", backgrounds);
		return true;
	}
}
