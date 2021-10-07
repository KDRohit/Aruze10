using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoSomethingWelcomeBackJourney : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		SevenDayWelcomeDialog.showDialog();
	}

	public override GameTimer getTimer(string parameter)
	{
		if (ExperimentWrapper.WelcomeJourney.isInExperiment && WelcomeJourney.instance != null)
		{
			long nextClaimTime = WelcomeJourney.instance.getNextClaimTime();
			int diff = System.Convert.ToInt32(nextClaimTime - GameTimer.currentTime);
			return new GameTimer(diff);
		}
		else
		{
			Debug.LogError("DoSomethingWelcomeJourney::getTimer - Attempted to welcome journey timer without active campaign");
			return null;
		}
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return WelcomeJourney.instance.isActive() && ExperimentWrapper.WelcomeJourney.isLapsedPlayer;
	}
}
