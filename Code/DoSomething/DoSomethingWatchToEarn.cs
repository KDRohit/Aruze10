using UnityEngine;
using System.Collections;

public class DoSomethingWatchToEarn : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		WatchToEarn.watchVideo("carousel", true);
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		return WatchToEarn.isEnabled;
	}
}
