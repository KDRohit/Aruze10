using UnityEngine;
using System.Collections;

public class DoSomethingLinkedVipConnectCoins : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		DoSomethingLinkedVipConnectCoins.commonAction();
	}
	
	// Shares the same action as the rewards version.
	public static void commonAction()
	{
		if (LinkedVipProgram.instance.isPending)
		{
			// If the player is in the pending state, then show the status dialog to tell them this.
			LinkedVipStatusDialog.checkNetworkStateAndShowDialog();
		}
		else
		{
			// Otherwise they have not connected anything yet and should be show the email entry dialog.
			LinkedVipConnectDialog.showDialog();
		}
	}
	
	public override bool getIsValidToSurface(string parameter)
	{
		return (LinkedVipProgram.instance.shouldPromptForConnect && LinkedVipProgram.instance.incentiveCredits > 0);
	}
}
