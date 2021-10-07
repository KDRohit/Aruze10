using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataTwoForOneSale : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return
				MOTDFramework.currentSaleCount < MOTDFramework.limitSessionSale &&
				PurchaseFeatureData.isSaleActive &&
				!BuyCreditsDialog.hasSeenSale &&
				!ExperimentWrapper.FirstPurchaseOffer.isInExperiment;
		}
	}
	
	public override string noShowReason
	{
		get
		{
			string result = base.noShowReason;
			if (MOTDFramework.currentSaleCount >= MOTDFramework.limitSessionSale)
			{
				result += "Reached the sale limit.\n";
			}
			if (!PurchaseFeatureData.isSaleActive)
			{
				result += "A sale isn't active.\n";
			}
			if (BuyCreditsDialog.hasSeenSale)
			{
				result += "Have already seen the sale.\n";
			}
			return result;
		}
	}

	public override bool show()
	{
		MOTDFramework.currentSaleCount++;
		return BuyCreditsDialog.showDialog(keyName);

	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
