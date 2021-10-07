using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Override for special behavior.
*/

public class MOTDDialogDataPayerReactivationSale : MOTDDialogData
{
	public override bool shouldShow
	{
		get
		{
			return
				MOTDFramework.currentSaleCount < MOTDFramework.limitSessionSale && 
				STUDSale.isSaleActive(SaleType.PAYER_REACTIVATION);
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
			if (!STUDSale.isSaleActive(SaleType.PAYER_REACTIVATION))
			{
				result += "Reactivation sale isn't active.\n";
			}
			return result;
		}
	}

	public override bool show()
	{
		if (STUDSaleDialog.showDialog(STUDSale.getSale(SaleType.PAYER_REACTIVATION), keyName))
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
