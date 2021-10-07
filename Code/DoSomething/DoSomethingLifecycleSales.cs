using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class DoSomethingLifecycleSales : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		if (LifecycleDialog.isActive)
		{
			LifecycleDialog.showDialog();
			StatsManager.Instance.LogCount
			(
				counterName: "dialog",
				kingdom: "lapsed_payer_sale_dialog",
				phylum: "lobby",
				klass: "",
				family: "cta",
				genus: "click"
			);
		}
		else
		{
			// If there is no starter pack, show the buy page.
			BuyCreditsDialog.showDialog("", SchedulerPriority.PriorityType.IMMEDIATE);
		}
	}
	
	public override GameTimer getTimer(string parameter)
	{
		if (LifecycleDialog.saleTimer != null)
		{
			return LifecycleDialog.saleTimer.endTimer;
		}

		return null;
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		return LifecycleDialog.isActive;
	}
}
