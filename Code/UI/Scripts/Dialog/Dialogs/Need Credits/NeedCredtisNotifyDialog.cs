using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;
using TMPro;

// Tells player they are out of credtis then brings up Buy Credits Dialog
public class NeedCredtisNotifyDialog : DialogBase 
{
	public ButtonHandler closeButton;
	public ButtonHandler getMoreButton;

	private string gameName = "";

	/// Initialization
	public override void init()
	{
		closeButton.registerEventDelegate(closeClicked);
		getMoreButton.registerEventDelegate(buyClicked);

		Audio.play("minimenuopen0");

		gameName = GameState.game != null ? GameState.game.keyName : "";
		StatsManager.Instance.LogCount
		(
			counterName:"dialog",
			kingdom:"out_of_coins",
			phylum:"intermediary_dialog",
			klass:gameName,
			family:"",
			genus:"view"
		);
	}

	void Update()
	{
		AndroidUtil.checkBackButton(closeClicked, "dialog", "android_dialog", "ooc_buy_page", "", "", "back");
	}

	// Not a click delegate but called from the option class.
	protected virtual void buyClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount
		(
			counterName:"dialog",
			kingdom:"out_of_coins",
			phylum:"intermediary_dialog",
			klass:gameName,
			family:"cta",
			genus:"click"
		);

		if (ExperimentWrapper.PopcornSale.isInExperiment && ExperimentWrapper.OutOfCoinsBuyPage.shouldShowSale && STUDSale.isSaleActive(SaleType.POPCORN))
		{
			STUDSaleDialog.showDialog(STUDSale.getSale(SaleType.POPCORN), "");
		}
		else if (ExperimentWrapper.OutOfCoinsBuyPage.isInExperiment)
		{
			string buypageType = "";
			if (ExperimentWrapper.OutOfCoinsBuyPage.shouldShowSale && PurchaseFeatureData.isSaleActive )
			{
				BuyCreditsDialog.showDialog("", statsName:"");
			}
			else
			{
				BuyCreditsDialog.showDialog("", skipOOCTitle:false, statsName:"intermediary_buy_page");
			}
		}
		else
		{
			// Technically this should get caught in the check above, but always good to have a fallback case
			BuyCreditsDialog.showDialog("", skipOOCTitle:false, statsName:"intermediary_buy_page");
		}
		
		Dialog.close();
	}

	// Close Button delegate
	public virtual void closeClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount
		(
			counterName:"dialog",
			kingdom:"out_of_coins",
			phylum:"intermediary_dialog",
			klass:gameName,
			family:"",
			genus:"close"
		);
		Dialog.close();
	}
	
	/// Called by Dialog.close() - do not call directly.
	public override void close()
	{
		// Do special cleanup.
	}
	
	public static void showDialog()
	{
		string dialogKey = "need_credits_v2";

		Scheduler.addDialog(dialogKey, null, SchedulerPriority.PriorityType.IMMEDIATE);
	}
}
