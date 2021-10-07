using UnityEngine;
using System.Collections;

public class DoSomethingLuckyDeal : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		StatsManager.Instance.LogCount(counterName: "bottom_nav", 
							kingdom: "carousel", 
							phylum:"carousel_item", 
							klass: "lucky_deal",
							genus: "click");
		
		LuckyDealDialog.showDialog();
	}

	public override GameTimer getTimer(string parameter)
	{
		return LuckyDealDialog.eventTimer.endTimer;
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return 	ExperimentWrapper.WheelDeal.isInExperiment &&
				!LuckyDealDialog.doNotShowUntilRestart &&
				!LuckyDealDialog.eventTimer.isExpired &&
				AssetBundleManager.shouldLazyLoadBundle("lucky_deal");
	}
}
