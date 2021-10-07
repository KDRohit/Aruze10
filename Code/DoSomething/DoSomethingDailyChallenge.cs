using UnityEngine;
using System.Collections;

public class DoSomethingDailyChallenge : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		DailyChallengeMOTD.showDialog();
	}
	
	public override GameTimer getTimer(string parameter)
	{
		if (Quest.activeQuest != null)
		{
			return Quest.activeQuest.timer;
		}
		return null;
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		return DailyChallenge.isActive;
	}
}
