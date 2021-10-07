using UnityEngine;
using System.Collections;

public class DoSomethingBuyCoinsNew : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		BuyCreditsDialog.showDialog();
	}

	public override bool getIsValidToSurface(string parameter)
	{
		// Only show the buy coins carousel if we aren't showing a buy page sale slide.
		return !PurchaseFeatureData.isSaleActive && !ExperimentWrapper.FirstPurchaseOffer.isInExperiment;
	}
}
