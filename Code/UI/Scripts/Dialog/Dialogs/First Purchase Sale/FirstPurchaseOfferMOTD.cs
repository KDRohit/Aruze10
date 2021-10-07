using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class FirstPurchaseOfferMOTD : DialogBase
{
	public override void init()
	{
	
	}

	void Update()
	{
		AndroidUtil.checkBackButton(clickClose);
	}

	public void clickClose()
	{
		Dialog.close();
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	public static bool showDialog()
	{
		StatsManager.Instance.LogCount(counterName: "dialog",
			kingdom: "buy_page_v3",
			phylum:"auto_surface",
			klass: "first_purchase_offer",
			genus: "view");
		return BuyCreditsDialog.showDialog("first_purchase_offer_motd", SchedulerPriority.PriorityType.IMMEDIATE);
	}
}
