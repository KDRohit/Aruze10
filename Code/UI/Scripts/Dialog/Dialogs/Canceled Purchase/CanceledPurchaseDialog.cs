using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;
/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class CanceledPurchaseDialog : DialogBase
{	
	public ButtonHandler contactSupportButton;
	public ButtonHandler closeButton;
	
	/// Initialization
	public override void init()
	{
		contactSupportButton.registerEventDelegate(contactSupportClicked);
		closeButton.registerEventDelegate(closeClicked);
		
		//Enable controls, as it was disabled while making the asyc call from BuyCreditsOption.
		NGUIExt.enableAllMouseInput();
		
		StatsManager.Instance.LogCount("dialog", "purchase_canceled", "", "", "", "view");
	}

	public void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}
	
	public void contactSupportClicked(Dict args = null) 
	{
		closeDialog();
		Common.openSupportUrl(Data.liveData.getString("BILLING_HELP_URL", ""));

		StatsManager.Instance.LogCount("dialog", "purchase_canceled", "", "", "contact_support", "click");
	}
	
	public void closeClicked(Dict args = null)
	{
		closeDialog();
		StatsManager.Instance.LogCount("dialog", "purchase_canceled", "", "", "close", "click");
	}
	
	protected void closeDialog()
	{
		Dialog.close();
	}
			
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
	}
	
	public static void showDialog()
	{
		string dialogKey = "canceled_purchase";
		bool showImmediately = true;
		Dict args = null;
		
		// We need this check because there are several (sometimes overlapping) ways 
		// that a purchase can fail but we only want to show this dialog once
		if (!Scheduler.hasTaskWith(dialogKey))
		{
			Scheduler.addDialog(dialogKey, args, SchedulerPriority.PriorityType.IMMEDIATE);
		}
	}
}
