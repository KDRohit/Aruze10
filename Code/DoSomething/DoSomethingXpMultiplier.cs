using UnityEngine;
using System.Collections;

public class DoSomethingXpMultiplier : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		XPMultiplierDialog.showDialog();
	}
	
	public override GameTimer getTimer(string parameter)
	{
		XPMultiplierEvent xpEvent = XPMultiplierEvent.instance;
		if (xpEvent != null)
		{
			if (xpEvent.featureTimer != null)
			{
				GameTimerRange range = xpEvent.featureTimer.combinedActiveTimeRange;
				if (range != null)
				{
					return range.endTimer;
				}
			}
		}

		return null;
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		return (XPMultiplierEvent.instance.xpMultiplier == getMultiplierFromParameter(parameter));
	}
	
	public override bool getIsValidParameter(string parameter)
	{
		int multiplier = getMultiplierFromParameter(parameter);
		return (multiplier == 2 || multiplier == 3);
	}

	private int getMultiplierFromParameter(string parameter)
	{
		int multiplier = 1;
		
		try
		{
			multiplier = int.Parse(parameter);
		}
		catch
		{
			Debug.LogWarning("Invalid parameter for xp_multiplier action: " + parameter);
		}
		
		return multiplier;
	}
}
