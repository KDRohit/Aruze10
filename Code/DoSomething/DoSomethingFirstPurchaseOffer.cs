using UnityEngine;
using System.Collections;

public class DoSomethingFirstPurchaseOffer : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		StatsManager.Instance.LogCount(counterName: "dialog", 
			kingdom: "buy_page_v3", 
			phylum:"carousel", 
			klass: "first_purchase_offer",
			genus: "view");
		BuyCreditsDialog.showDialog();
	}

	public override bool getIsValidToSurface(string parameter)
	{
		// Only show the buy page sale carousel if we aren't showing a buy coins slide.
		return ExperimentWrapper.FirstPurchaseOffer.isInExperiment;
	}
}
