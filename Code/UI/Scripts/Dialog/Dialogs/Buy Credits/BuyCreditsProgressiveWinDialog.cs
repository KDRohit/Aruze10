using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
Controls the dialog for showing progressive jackpot win celebration and sharing the news about it.
*/

public class BuyCreditsProgressiveWinDialog : DialogBase
{
	private const string BACKGROUND_PATH = "progressive_jackpot/Progressive_Jackpot_Win.jpg";

	public TextMeshPro poolAmountLabel;
	public Renderer backgroundRenderer;
	private long credits = 0L;
	
	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);
		
		if (ProgressiveJackpot.buyCreditsJackpot == null)
		{
			Debug.LogError("BuyCreditsProgressiveWinDialog -- ProgressiveJackpot.buyCreditsJackpot is null.");
			Dialog.close();
		}
		
	    credits = (long)dialogArgs.getWithDefault(D.TOTAL_CREDITS, 0L);
		
		if (credits == 0L)
		{
			Debug.LogError("BuyCreditsProgressiveWinDialog -- credits is 0.");
			Dialog.close();
		}

		ProgressiveJackpot.buyCreditsJackpot.reset();
		
		poolAmountLabel.text = CreditsEconomy.convertCredits(credits);
		StatsManager.Instance.LogCount("dialog", "buy_page_progressive_win", "", "", "", "view");
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		if (credits == 0L)
		{
			Dialog.close();
		}
	}

	/// NGUI button callback.
	private void closeClicked()
	{
		SlotsPlayer.addCredits(credits, "progressive win");
		cancelAutoClose();
		Dialog.close();
		StatsManager.Instance.LogCount("dialog", "buy_page_progressive_win", "", "", "ok", "click");		
	}

	/// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		// Maybe we'll want to prompt for Rate Me here, but not sure yet.
		//		RateMe.checkAndPrompt(RateMe.RateMeTrigger.BIG_WIN);
		// Do special cleanup.
	}

	public static void showDialog(long credits)
	{
		Dict args = Dict.create(
			D.TOTAL_CREDITS, credits,
			// We must force this dialog so that it can be shown before processing normal spin outcomes:
			D.PRIORITY, SchedulerPriority.PriorityType.IMMEDIATE
		);

		Dialog.instance.showDialogAfterDownloadingTextures("buy_credits_progressive_win", BACKGROUND_PATH, args);
	}
}
