using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class NetworkFriendsFacebookDisconnectDialog : DialogBase
{
	public ImageButtonHandler yesButton;
	public ImageButtonHandler noButton;

	private ClickHandler.onClickDelegate yesCallback;
	private ClickHandler.onClickDelegate noCallback;
	
	public override void init()
	{
		StatsFacebookAuth.logDisconnectView();
	    yesCallback = dialogArgs.getWithDefault(D.CALLBACK, null) as ClickHandler.onClickDelegate;
		noCallback = dialogArgs.getWithDefault(D.SECONDARY_CALLBACK, null) as ClickHandler.onClickDelegate;
		yesButton.registerEventDelegate(yesClicked);
		noButton.registerEventDelegate(noClicked);
	}

	public override void close()
	{
		// Cleanup.
	}

	private void yesClicked(Dict args = null)
	{
		Dialog.close();
		if (yesCallback != null)
		{
			yesCallback(args);
		}
	}

	private void noClicked(Dict args = null)
	{
		Dialog.close();
		if (noCallback != null)
		{
			noCallback(args);
		}
	}

	public static void showDialog(ClickHandler.onClickDelegate yesCallback = null, ClickHandler.onClickDelegate noCallback = null)
	{
		Dict args = Dict.create(D.CALLBACK, yesCallback, D.SECONDARY_CALLBACK, noCallback);
		Scheduler.addDialog("network_friends_facebook_logout", args, SchedulerPriority.PriorityType.IMMEDIATE);
	}
}
