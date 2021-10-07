using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class DoSomethingStarterPack : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		if (StarterDialog.isActive)
		{
			StarterDialog.showDialog();
		}
		else
		{
			// If there is no starter pack, show the buy page.
			BuyCreditsDialog.showDialog("", SchedulerPriority.PriorityType.IMMEDIATE);
		}
	}
	
	public override GameTimer getTimer(string parameter)
	{
		if (StarterDialog.saleTimer != null)
		{
			return StarterDialog.saleTimer.endTimer;
		}

		return null;
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		return
			StarterDialog.isActive &&
			parameter == ExperimentWrapper.StarterPackEos.artPackage;
	}
}
