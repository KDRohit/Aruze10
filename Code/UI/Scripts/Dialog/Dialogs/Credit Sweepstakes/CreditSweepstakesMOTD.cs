using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class CreditSweepstakesMOTD : DialogBase
{
	private const string BACKGROUND_PATH = "misc_dialogs/credit_sweepstakes/Sweepstakes MOTD BG.png";

	public ButtonHandler buyButton;
	public Renderer backgroundRenderer;
	public TextMeshPro timerLabel;
	public TextMeshPro winnerCountLabel;
	public TextMeshPro descriptionLabel;
	public TextMeshPro legalLabel;
	public TextMeshPro amountLabel;
	public TextMeshPro prizesAwardedLabel;
	
	private bool isClosing = false;
	
	public override void init()
	{
		if (backgroundRenderer.gameObject != null && downloadedTextureToRenderer(backgroundRenderer, 0))
		{
			backgroundRenderer.gameObject.SetActive(true);
		}
		
		winnerCountLabel.text = CommonText.formatNumber(CreditSweepstakes.winnerCount);
		descriptionLabel.text = Localize.text("coinsw_motd_desc");
		amountLabel.text = CreditsEconomy.convertCredits(CreditSweepstakes.payout, true);
		legalLabel.text = CreditSweepstakes.getLegalText();
		timerLabel.text = Localize.text("ends_in") + " "; //timer will append to current text
		prizesAwardedLabel.text = CreditSweepstakes.winnerCount > 1
			? Localize.text("coinsw_motd_num_winners_title")
			: Localize.text("coinsw_motd_winner_title");

		CreditSweepstakes.timeRange.registerLabel(timerLabel, GameTimerRange.TimeFormat.REMAINING, true);

		buyButton.registerEventDelegate(buyNowClicked);

		StatsManager.Instance.LogCount("dialog", "coin_sweepstakes", "", "", "", "view");
		MOTDFramework.markMotdSeen(dialogArgs);
	}

	public void Update()
	{
		if (!CreditSweepstakes.isActive)
		{
			// We need to make sure the sweepstakes is active, since it can become inactive,
			// including the timers being nulled out, when the game is reset while the dialog is shown.
			if (!isClosing)
			{
				isClosing = true;	// Using Dialog.instance.isClosing is funky here, so don't use it.
				Dialog.close();
			}
			return;
		}
		
		AndroidUtil.checkBackButton(closeClicked);
	}

	private void buyNowClicked(Dict args)
	{
		Dialog.close();
		buyButton.unregisterEventDelegate(buyNowClicked);
		BuyCreditsDialog.showDialog("", SchedulerPriority.PriorityType.IMMEDIATE);
		StatsManager.Instance.LogCount("dialog", "coin_sweepstakes", "", "", "buy_page", "click");
	}

	private void closeClicked()
	{
		StatsManager.Instance.LogCount("dialog", "coin_sweepstakes", "", "", "", "close");
		Dialog.close();
	}

	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// So Special cleanup.
		buyButton.unregisterEventDelegate(buyNowClicked);
	}	
	
	public static bool showDialog(string motdKey = "")
	{
		Dialog.instance.showDialogAfterDownloadingTextures("credit_sweepstakes_motd", BACKGROUND_PATH, Dict.create(D.MOTD_KEY, motdKey));
		return true;
	}
}
