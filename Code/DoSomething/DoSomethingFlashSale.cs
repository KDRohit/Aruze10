using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class DoSomethingFlashSale : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		if (FlashSaleManager.flashSaleIsActive)
		{
			FlashSaleDialog.showDialog();
		}
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return true;
	}
}
