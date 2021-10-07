using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataPopcornSale : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return
				MOTDFramework.currentSaleCount < MOTDFramework.limitSessionSale &&
				!ExperimentWrapper.FirstPurchaseOffer.isInExperiment && 
				!StarterDialog.isActive &&
				STUDSale.isSaleActive(SaleType.POPCORN);
		}
	}
	
	public override bool show()
	{
		if (STUDSaleDialog.showDialog(STUDSale.getSale(SaleType.POPCORN), keyName))
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
