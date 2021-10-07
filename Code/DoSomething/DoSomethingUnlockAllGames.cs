using UnityEngine;
using System.Collections;

public class DoSomethingUnlockAllGames : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		UnlockAllGamesMotd.showDialog();		
	}
	
	public override GameTimer getTimer(string parameter)
	{
		// For Sync Top & Bottom Countdown timer
		if (UnlockAllGamesFeature.instance != null)
		{
			GameTimerRange range = UnlockAllGamesFeature.instance.unlockAllGamesTimer;
			if (range != null)
			{
				return range.endTimer;
			}
		}
		
		return null;
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		return UnlockAllGamesFeature.instance != null && UnlockAllGamesFeature.instance.isEnabled;
	}
}
