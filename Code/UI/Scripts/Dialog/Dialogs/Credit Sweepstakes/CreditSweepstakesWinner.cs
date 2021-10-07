using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls the sweepstakes dialog that is shown when the player didn't win.
*/

public class CreditSweepstakesWinner : DialogBase
{
	private const string BACKGROUND_PATH = "misc_dialogs/credit_sweepstakes/megacoinbonanza_winner_windowed.png";

	public ButtonHandler okButton;
	public Renderer backgroundRenderer;
	public TextMeshPro winAmountLabel;
	public FacebookFriendInfo playerIcon;
	public TextMeshPro legalLabel;
	public TextMeshPro awardLabel;
	
	private long sweepstakesWinAmount = 0L;
	private int version = 0;
	private bool sentAction = false;
	
	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);
				
		sweepstakesWinAmount = (long)dialogArgs.getWithDefault(D.TOTAL_CREDITS, 0L);
		version = (int)dialogArgs.getWithDefault(D.OPTION, 0);
		
		if (SlotsPlayer.isAnonymous && string.IsNullOrEmpty(SlotsPlayer.instance.socialMember.firstName))
		{
			// MCC Adding in a check for if name is empty, if they have a first name we should just say it here.
			winAmountLabel.text = Localize.text("coinsw_win_desc_anon_{0}", CreditsEconomy.convertCredits(sweepstakesWinAmount));
		}
		else
		{
			playerIcon.member = SlotsPlayer.instance.socialMember;
			winAmountLabel.text = Localize.text("coinsw_win_desc_{0}_{1}", SlotsPlayer.instance.socialMember.firstName, CreditsEconomy.convertCredits(sweepstakesWinAmount));
		}
		
		legalLabel.text = CreditSweepstakes.getLegalText();
		awardLabel.text = CreditsEconomy.convertCredits(CreditSweepstakes.payout, true);

		okButton.registerEventDelegate(onOkClicked);

		StatsManager.Instance.LogCount("dialog", "coin_sweepstakes_collect", "", "", "", "view"); 
		MOTDFramework.markMotdSeen(dialogArgs);
	}

	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	// Called by Dialog.close() - do not call directly. 
	public override void close()
	{
		// So Special cleanup.
		okButton.unregisterEventDelegate(onOkClicked);
	}

	private void onOkClicked(Dict args)
	{
		okButton.enabled = false;
		closeClicked();
	}
	
	private void closeClicked()
	{
		if (!sentAction)
		{
			if (version > -1)   // -1 is passed in for dialog testing
			{
				SlotsPlayer.addCredits(sweepstakesWinAmount, "credit sweepstakes");
				StatsManager.Instance.LogCount("dialog", "coin_sweepstakes_collect", "", "", "collect", "view");
				PlayerAction.collectCreditSweepstakes(version);
			}
			StatsManager.Instance.LogCount("dialog", "coin_sweepstakes_collect", "", "", "close", "view");
			sentAction = true;
		}
		Dialog.close();
	}
	
	public static void showDialog(long winAmount, int version)
	{
		Dict args = Dict.create(
			D.TOTAL_CREDITS, winAmount,
			D.OPTION, version
		);

		Dialog.instance.showDialogAfterDownloadingTextures("credit_sweepstakes_winner", BACKGROUND_PATH, args);
	}
}
