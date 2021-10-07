using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/**
GenericDialog

This is a dialog that can be used for random Yes/No questions, etc.
You pass in translated strings, then get back whatever option the user selected.
Initially created to confirm the user logging out.
**/
public class GenericDialog : DialogBase
{
	public UISprite background;
	public TextMeshPro titleLabel;
	public TextMeshPro messageLabel;

	public TextMeshPro option1Label;
	public TextMeshPro option2Label;

	public GameObject optionButton1;
	public GameObject optionButton2;
	public GameObject closeButton;
	
	public ImageButtonHandler optionButton1Handler;
	public ImageButtonHandler optionButton2Handler;
	public ImageButtonHandler closeImageButtonHandler;

	private bool shouldShowCloseButton = true;

	/// Initialization
	public override void init()
	{
		string title = dialogArgs.getWithDefault(D.TITLE, null) as string;
		string body = dialogArgs.getWithDefault(D.MESSAGE, null) as string;
		string data = dialogArgs.getWithDefault(D.DATA, "") as string;
		string option1 = dialogArgs.getWithDefault(D.OPTION1, null) as string;
		string option2 = dialogArgs.getWithDefault(D.OPTION2, null) as string;
		bool isWaiting = (bool)dialogArgs.getWithDefault(D.IS_WAITING, false);

		// Append the show reason onto the userflow transaction so we know what
		// the purpose of this generic dialog was
		string showReason = dialogArgs.getWithDefault(D.REASON, null) as string;
		if (!string.IsNullOrEmpty(showReason))
		{
			Dictionary<string, string> extraFields = new Dictionary<string, string>();
			extraFields.Add("reason_shown", showReason);

			if (!string.IsNullOrEmpty(data))
			{
				extraFields.Add("additional_data", data);
			}
			Userflows.addExtraFieldsToFlow(userflowKey, extraFields);
		}

		// If the dialog is waiting for something, then don't offer any way to close the dialog
		// since it will get closed programmatically when the waiting is done.
		// We can also hide the close button for any other reason.
		shouldShowCloseButton = !isWaiting && (bool)dialogArgs.getWithDefault(D.SHOW_CLOSE_BUTTON, true);
		
		// Set up the button handlers. 
		optionButton1Handler.registerEventDelegate(clickOption1);
		optionButton2Handler.registerEventDelegate(clickOption2);
		closeImageButtonHandler.registerEventDelegate(clickClose);
		
		if (title != null)
		{
			titleLabel.text = Localize.toUpper(title);
		}
		if (body != null)
		{
			messageLabel.text = body;
			messageLabel.ForceMeshUpdate();	// Force this to update immediately so we can get the new bounds.
		}

		if (option1 != null)
		{
			option1Label.text = Localize.toUpper(option1);
		}

		if (option2 != null)
		{
			option2Label.text = Localize.toUpper(option2);
		}
		else
		{
			// Hide button 2 and center button 1:
			optionButton2.SetActive(false);
			CommonTransform.setX(optionButton1.transform, 0);
		}
		
		// The base height should be set for each sku/platform's prefab.
		float baseHeight = background.transform.localScale.y;

		CommonTransform.setHeight(background.transform, messageLabel.bounds.size.y + baseHeight);

		if (!shouldShowCloseButton)
		{
			closeButton.SetActive(false);
		}
		
		if (isWaiting)
		{
			optionButton1.SetActive(false);
			optionButton2.SetActive(false);
		}

		Audio.play("minimenuopen0");
	}
		
	void Update()
	{
		if (shouldShowCloseButton)
		{
			AndroidUtil.checkBackButton(clickClose);
		}
	}

	public void clickOption1(Dict args = null)
	{
		dialogArgs.merge(D.ANSWER, "1");	// The user selected option 1.
		Dialog.close();
	}

	public void clickOption2(Dict args = null)
	{
		dialogArgs.merge(D.ANSWER, "2");	// The user selected option 2.
		Dialog.close();
	}
	
	public void clickClose(Dict args = null)
	{
		dialogArgs.merge(D.ANSWER, "no");	// The user selected neither option.
		Dialog.close();
	}
			
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}
	
	public static void showDialog(Dict args, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW)
	{
		Scheduler.addDialog("generic", args, priority);
	}
}
