using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoSomethingMoreCards : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		BuyCreditsDialog.showDialog();
	}

	public override bool getIsValidToSurface(string parameter)
	{
		PurchaseFeatureData featureData = PurchaseFeatureData.BuyPage;

		if (featureData != null && featureData.creditPackages.Count > 0)
		{
			for (int i = 0; i < featureData.creditPackages.Count; i++)
			{
				if (featureData.creditPackages[i].activeEvent == CreditPackage.CreditEvent.MORE_CARDS)
				{
					return true;
				}
			}
		}

		return false;
	}
}
