using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls the sweepstakes dialog that is shown when the player didn't win.
*/

public class CreditSweepstakesLoser : DialogBase
{
	private const string BACKGROUND_PATH = "misc_dialogs/credit_sweepstakes/megacoinbonanza_loser_windowed.png";

	private const int MAX_WINNERS_SHOWN = 4;

	public ButtonHandler okButton;
	public Renderer backgroundRenderer;
	public UIGrid winnersGrid;
	public GameObject playerDisplayTemplate;
	public TextMeshPro descriptionLabel;
	public TextMeshPro legalLabel;
	public TextMeshPro awardLabel;
	
	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);
		
		descriptionLabel.text = Localize.text("coinsw_lose_desc_{0}", CreditSweepstakes.winnerCount);
		okButton.registerEventDelegate(closeClicked);
		
		int winnersShown = 0;
		List<string> winners = dialogArgs.getWithDefault(D.OPTION, null) as List<string>;
		
		foreach (string imageUrl in winners)
		{
			GameObject go = NGUITools.AddChild(winnersGrid.gameObject, playerDisplayTemplate);
			FacebookFriendInfo friendInfo = go.GetComponent<FacebookFriendInfo>();
			
			friendInfo.member = new FacebookMember(imageUrl);;
			
			winnersShown++;
			if (winnersShown == MAX_WINNERS_SHOWN)
			{
				break;
			}
		}
		
		legalLabel.text = CreditSweepstakes.getLegalText();
		awardLabel.text = CreditsEconomy.convertCredits(CreditSweepstakes.payout, true);

		playerDisplayTemplate.SetActive(false);
		winnersGrid.repositionNow = true;
		StatsManager.Instance.LogCount("dialog", "coin_sweepstakes_winners", "", "", "", "view"); 
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
		okButton.unregisterEventDelegate(closeClicked);
	}
	
	private void closeClicked(Dict args = null)
	{
		closeButtonHandler.enabled = false;
		okButton.enabled = false;
		StatsManager.Instance.LogCount("dialog", "coin_sweepstakes_winners", "", "", "", "close"); 
		Dialog.close();
	}
	
	public static void showDialog(List<string> otherWinners)
	{
		Dict args = Dict.create(D.OPTION, otherWinners);
		
		Dialog.instance.showDialogAfterDownloadingTextures("credit_sweepstakes_loser", BACKGROUND_PATH, args);
	}
}