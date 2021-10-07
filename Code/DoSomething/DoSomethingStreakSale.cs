using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoSomethingStreakSale : DoSomethingAction
{
	public override bool getIsValidToSurface(string parameter)
	{
		return StreakSaleManager.endTimer != null && StreakSaleManager.showInCarousel;
	}

	public override void doAction(string parameter)
	{
		if (StreakSaleManager.streakSaleActive)
		{
			StreakSaleDialog.showDialog();
		}
	}

	public override GameTimer getTimer(string parameter)
	{
		return StreakSaleManager.endTimer != null ? new GameTimer(StreakSaleManager.endTimer.timeRemaining) : null;
	}
}
