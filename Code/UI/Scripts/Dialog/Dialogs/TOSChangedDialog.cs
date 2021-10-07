using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

public class TOSChangedDialog : DialogBase
{

	public ButtonHandler okButton;

	public override void init()
	{
		okButton.registerEventDelegate(closeClicked);
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{

	}


	public void closeClicked(Dict args = null)
	{
		closeDialog();
		StatsManager.Instance.LogCount("dialog", "terms_of_service", "", "", "close", "click");

		// User clicks ok? They've accepted the TOS
		PlayerAction.acceptTermsOfService();
	}

	private void closeDialog()
	{
#if UNITY_WEBGL
		// this dialog shows while lobby is loading and hides the loading screen
		// show it again on closing so other dialogs can't show and players don't see exposed top overlay with blank lobby for a few seconds
		Loading.show(Loading.LoadingTransactionTarget.LOBBY);
#endif
		Dialog.close();
	}

	public static void showDialog(DialogBase.AnswerDelegate callback)
	{
		Scheduler.addDialog("terms_of_service",
			Dict.create(D.CALLBACK, callback),
			SchedulerPriority.PriorityType.IMMEDIATE
		);
	}
}
