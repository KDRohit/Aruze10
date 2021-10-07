using UnityEngine;
using System.Collections;

public class DoSomethingJackpotDays : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		BuyCreditsDialog.showDialog();
	}

	public override bool getIsValidToSurface(string parameter)
	{
		// Only show the buy page sale carousel if we aren't showing a buy coins slide.
		return ExperimentWrapper.BuyPageProgressive.jackpotDaysIsActive && !ExperimentWrapper.FirstPurchaseOffer.isInExperiment;
	}

	public override GameTimer getTimer(string parameter)
	{
		return SlotsPlayer.instance.jackpotDaysTimeRemaining.endTimer;
	}
}
