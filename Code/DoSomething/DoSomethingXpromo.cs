using UnityEngine;
using System.Collections;

public class DoSomethingXpromo : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		StatsManager.Instance.LogCount("lobby", "xpromo", "click", "xpromo_creative", "", "");
		MobileXpromo.showXpromo(MobileXpromo.SurfacingPoint.NONE);
		
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		return MobileXpromo.isEnabled(parameter);
	}
}
