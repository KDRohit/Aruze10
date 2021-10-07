using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataVIPSale : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return
				MOTDFramework.currentSaleCount < MOTDFramework.limitSessionSale && 
				!ExperimentWrapper.FirstPurchaseOffer.isInExperiment && 
				STUDSale.isSaleActive(SaleType.VIP);
		}
	}
	
	public override bool show()
	{
		if (STUDSaleDialog.showDialog(STUDSale.getSale(SaleType.VIP), keyName))
		{
			MOTDFramework.currentSaleCount++;
			return true;
		}
		return false;
	}
	
	new public static void resetStaticClassData()
	{
	}
	
}
