using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataHappyHourSale : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return
				MOTDFramework.currentSaleCount < MOTDFramework.limitSessionSale && 
				!ExperimentWrapper.FirstPurchaseOffer.isInExperiment && 
				STUDSale.isSaleActive(SaleType.HAPPY_HOUR);
		}
	}
	
	public override bool show()
	{
		MOTDFramework.currentSaleCount++;
		return STUDSaleDialog.showDialog(STUDSale.getSale(SaleType.HAPPY_HOUR), keyName);
	}
	
	new public static void resetStaticClassData()
	{
	}

}
