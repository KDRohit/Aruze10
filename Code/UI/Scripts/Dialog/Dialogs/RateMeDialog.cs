using UnityEngine;
using System.Collections;
using Com.Scheduler;

/**
The Rate Me dialog is displayed to users at certain points in the game where
we think they might be willing to give our application a good rating.
**/

public class RateMeDialog : DialogBase
{

	[SerializeField] private ClickHandler okayButton;
	[SerializeField] private ClickHandler notNowButton;
	
	
	/// Initialization
	public override void init()
	{
		okayButton.registerEventDelegate(okClicked);
		notNowButton.registerEventDelegate(notNowClicked);
	}

	void Update()
	{
		if (shouldAutoClose)
		{
			notNowClicked();
		}
		AndroidUtil.checkBackButton(notNowClicked, "dialog", "app_rating_prompt", "tap", "", "", "back");
	}

	public void okClicked(Dict args = null)
	{
		cancelAutoClose();
		dialogArgs.merge(D.ANSWER, "yes");	// Just in case something needs to know if ok or close was clicked.
		Dialog.close();
	}

	public void notNowClicked(Dict args = null)
	{
		cancelAutoClose();
		dialogArgs.merge(D.ANSWER, "no");	// Just in case something needs to know if ok or close was clicked.
		Dialog.close();
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}
	
	public static void showDialog(Dict args)
	{
		Scheduler.addDialog("rate_me", args);
	}
}
