using UnityEngine;
using System.Collections;

public class DoSomethingTicketTumbler : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		StatsManager.Instance.LogCount(counterName:"dialog", kingdom:"lottery_day_motd", klass:"carousel_card", genus:"view");
		
		if (TicketTumblerFeature.instance.isEnabled)
		{
			TicketTumblerDialog.showDialog("", null, true);
		}
	}

	public override GameTimer getTimer(string parameter)
	{
		return TicketTumblerFeature.instance.featureTimer.endTimer;
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return TicketTumblerFeature.instance.isEnabled;
	}

	public override void onActivateCarouselSlide(string parameter)
	{
	}
}
