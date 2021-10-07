using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
Controls the dialog for showing progressive jackpot win celebration and sharing the news about it.
*/

public class JackpotDaysWinDialog : DialogBase
{
	private const string BACKGROUND_PATH = "motd/jackpotdays_winner_windowed.png";
	public TextMeshPro poolAmountLabel;
	public Renderer backgroundRenderer;
	private long credits = 0L;

	public Transform flyingCoinStart;
	public Transform flyingCoinDestination;
	public ButtonHandler collectButton;

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
		StatsManager.Instance.LogCount("dialog", "jackpot_days_winner", "", "", "", "view");

		if (collectButton != null)
		{
			collectButton.registerEventDelegate(closeClicked);
		}
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
	private void closeClicked(Dict args = null)
	{
		StartCoroutine(coinEffect());

	}

	private IEnumerator coinEffect()
	{
		CoinScriptUpdated coin = CoinScriptUpdated.create(
			sizer,
			flyingCoinStart.position,
			new Vector3(0, 0, -100)
		);

		// Calculate the local coordinates of the destination, which is where "coinIconLarge" is positioned relative to "sizer".
		Vector2 destination = NGUIExt.localPositionOfPosition(sizer, flyingCoinDestination.position);

		yield return StartCoroutine(coin.flyTo(destination));

		coin.destroy();

		// Close the dialog as soon as the coin reaches the destination,
		// so there is a clear view of the rollup without the dialog in the way.
		Dialog.close();
		SlotsPlayer.addFeatureCredits(credits, "progressiveWin");
		cancelAutoClose();
		StatsManager.Instance.LogCount("dialog", "jackpot_days_winner", "", "", "ok", "click");
	}

	/// Called by Dialog.close() - do not call directly.
	public override void close()
	{

	}

	public static void showDialog(long credits)
	{
		Dict args = Dict.create(
			D.TOTAL_CREDITS, credits,
			// We must force this dialog so that it can be shown before processing normal spin outcomes:
			D.PRIORITY, SchedulerPriority.PriorityType.IMMEDIATE
		);

		Dialog.instance.showDialogAfterDownloadingTextures("jackpot_days_winner", BACKGROUND_PATH, args);
		Audio.play("BuyPageProgressiveWinFanfare");
	}
}
