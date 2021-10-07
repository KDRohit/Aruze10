using UnityEngine;
using System.Collections;

public class DoSomethingZIS : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		switch (parameter)
		{
			case "ooc":
				//we're coming from out of coins message, open the dialog directly
				ZisSaveYourProgressDialog.showDialog();
				StatsZIS.logSettingsZis();
				break;
			
			default:
				//coming from carousel
				StatsZIS.logCarousel();
				HelpDialogHIR.showDialog();
				break;
		}
		
	}

	public override bool getIsValidToSurface(string parameter)
	{
		return SlotsPlayer.isAnonymous;
	}
}