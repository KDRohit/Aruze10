using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

public class FacebookConnectHelpDialog : DialogBase
{
	[SerializeField] private LabelWrapperComponent headerLabel;
	[SerializeField] private ButtonHandler closeButton;
	[SerializeField] private ButtonHandler loginButton;
	private bool isDisconnectedVersion = false;

	public const string FB_DISCONNECTED_HEADER_LOCALIZATION = "logged_out";
	public const string FB_CONNECT_FAILED_HEADER_LOCALIZATION = "login_problems";

	// Initialization
	public override void init()
	{
		string headerLocalization = (string)dialogArgs.getWithDefault(D.TITLE, FB_CONNECT_FAILED_HEADER_LOCALIZATION);
		headerLabel.text = Localize.textUpper(headerLocalization);
		closeButton.registerEventDelegate(closeClicked);
		loginButton.registerEventDelegate(loginClicked);
		isDisconnectedVersion = headerLocalization == FB_DISCONNECTED_HEADER_LOCALIZATION;
		if (isDisconnectedVersion)
		{
			StatsManager.Instance.LogCount(
				counterName : "dialog",
				kingdom : "fb_disconnected",
				genus : "view"
			);
		}
	}

	public void loginClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName : "dialog",
			kingdom : "fb_disconnected",
			family : "okay",
			genus : "click"
		);
		// Do the login.
		SlotsPlayer.facebookLogin();
	}

	public void closeClicked(Dict args = null)
	{
		if (isDisconnectedVersion)
		{
			StatsManager.Instance.LogCount(
				counterName : "dialog",
				kingdom : "fb_disconnected",
				family : "close",
				genus : "click"
			);
		}
		Dialog.close();
	}


	// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup. Downloaded textures are automatically destroyed by Dialog class.
	}

	// Static method to show the dialog.
	// This shouldn't be called directly - only by the Scheduler as a replacement dialog.
	public static void showDialog(Dict args = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW)
	{
		Scheduler.addDialog("facebook_connect_help", args, priority);
	}
}
