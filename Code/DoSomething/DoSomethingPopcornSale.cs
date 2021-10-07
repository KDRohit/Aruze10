using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class DoSomethingPopcornSale : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		STUDSale sale = STUDSale.getActiveSale(SaleType.POPCORN);
		if (sale != null)
		{
			STUDSaleDialog.showDialog(sale);
		}
		else
		{
			// If we aren't in a stud sale, but are trying to show one for some reason
			// then we should show the buy page instead.
			BuyCreditsDialog.showDialog("", SchedulerPriority.PriorityType.IMMEDIATE);
		}
	}
	
	public override GameTimer getTimer(string parameter)
	{
		STUDSale sale = STUDSale.getSaleByAction("popcorn_sale");
		if (sale != null)
		{
			return sale.featureData.timerRange.endTimer;
		}
		return null;
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		bool salesDialogsAreLocked = ExperimentWrapper.SaleDialogLevelGate.isLockingSaleDialogs;
		STUDSale sale = STUDSale.getSale(SaleType.POPCORN);
		return (sale != null && sale.isActive && !salesDialogsAreLocked && !ExperimentWrapper.FirstPurchaseOffer.isInExperiment && !StarterDialog.isActive);
	}
}
