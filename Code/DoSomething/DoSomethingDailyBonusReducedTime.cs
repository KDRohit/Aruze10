using UnityEngine;
using System.Collections;

public class DoSomethingDailyBonusReducedTime : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		DailyBonusReducedTimeMOTD.showDialog();
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		return DailyBonusReducedTimeEvent.isActive;
	}
	
	public override GameTimer getTimer(string parameter)
	{
		return DailyBonusReducedTimeEvent.timerRange.endTimer;
	}
}
