using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class DoSomethingVIPStatusBoost : DoSomethingAction
{
	public override bool getIsValidToSurface(string parameter)
	{
		return VIPStatusBoostEvent.isEnabled() && !VIPStatusBoostEvent.isEnabledByPowerup();
	}
		
	public override GameTimer getTimer(string parameter)
	{
		return VIPStatusBoostEvent.featureTimer.endTimer;
	}

	public override void doAction(string parameter)
	{
		VIPStatusBoostMOTD.showDialog();
	}
}