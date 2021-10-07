using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoSomethingWelcomeJourney : DoSomethingAction
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
		switch (parameter)
		{
			case "timer":
				return WelcomeJourney.instance.isActive() && WelcomeJourney.instance.isInCooldown;
			
			case "collect":
				return WelcomeJourney.shouldShow();
			
			default:
				return WelcomeJourney.instance.isActive() && !WelcomeJourney.instance.isFirstLaunch && !ExperimentWrapper.WelcomeJourney.isLapsedPlayer;
		}
		
	}

	public override string getValue(string parameter, string key)
	{
		switch (key)
		{
			case InboxItem.CREDITS:
				if (WelcomeJourney.instance != null)
				{
					return CreditsEconomy.convertCredits(WelcomeJourney.instance.claimAmount);
				}
				return "0";
			
			default:
				return base.getValue(parameter, key);
		}
		
		
	}
}
