﻿using UnityEngine;
using System.Collections;

public class DoSomethingRoyalRush : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		// Show standings dialog
		RoyalRushStandingsDialog.showDialog();

	}

	//public override GameTimer getTimer(string parameter)
	//{
	//	//return CampaignDirector.partner.timerRange.endTimer;
	//}

	public override bool getIsValidToSurface(string parameter)
	{
		return true;
	}
}
