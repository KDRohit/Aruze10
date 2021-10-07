using UnityEngine;
using System.Collections;

public class DoSomethingBuyCoinsNewSale : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		BuyCreditsDialog.showDialog();
	}

	public override bool getIsValidToSurface(string parameter)
	{
		// Only show the buy page sale carousel if we aren't showing a buy coins slide.
		return PurchaseFeatureData.isSaleActive && !PurchaseFeatureData.isActiveFromPowerup && !ExperimentWrapper.FirstPurchaseOffer.isInExperiment;
	}

	public override GameTimer getTimer(string parameter)
	{
		PurchaseFeatureData featureData = PurchaseFeatureData.BuyPage;
		if (featureData != null && featureData.timerRange != null)
		{
			return featureData.timerRange.endTimer;
		}
		else
		{
			return null;
		}
	}
}
