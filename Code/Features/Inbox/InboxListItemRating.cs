using System.Collections.Generic;
using UnityEngine;

public class InboxListItemRating : InboxListItemMultiButton
{
	[SerializeField] protected ButtonHandler buttonLove;
	[SerializeField] protected ButtonHandler buttonLike;
	[SerializeField] protected ButtonHandler buttonDislike;
	
	/*=========================================================================================
	BUTTON/EVENT HANDLING
	=========================================================================================*/
	protected override void registerHandlers()
	{
		base.registerHandlers();
		if (buttonLove != null)
		{
			buttonLove.registerEventDelegate(onSelect, Dict.create(D.OPTION, "love"));
		}
		
		if (buttonLike != null)
		{
			buttonLike.registerEventDelegate(onSelect, Dict.create(D.OPTION, "like"));
		}

		if (closeButton != null)
		{
			buttonDislike.registerEventDelegate(onSelect, Dict.create(D.OPTION, "dislike"));
		}
	}

	protected override void unregisterHandlers()
	{
		if (buttonLove != null)
		{
			buttonLove.unregisterEventDelegate(onSelect);
		}
		
		if (buttonLike != null)
		{
			buttonLike.unregisterEventDelegate(onSelect);
		}

		if (closeButton != null)
		{
			buttonDislike.unregisterEventDelegate(onSelect);
		}
		
		base.unregisterHandlers();
	}

	public override void onSelect(Dict args = null)
	{
		object actionArg = null;
		
		if (args ==null)
		{
			return;
		}

		// disable the clicked button - it then will be greyed out so that we know what option has been picked
		object callingObj = null;
		callingObj = args.getWithDefault(D.CALLING_OBJECT, null);
		if (callingObj != null && callingObj is GameObject callingGameObject)
		{
			ButtonHandler button = callingGameObject.GetComponent<ButtonHandler>();
			if (button != null)
			{
				button.enabled = false;
			}
		}

		// Set input confirmation text
		setMessageLabel(Localize.text("inbox_rating_choice_confirmation"));
		
		// Get action option
		actionArg = args.getWithDefault(D.OPTION, null);
		
		if (!(actionArg is string))
		{
			return;
		}

		foreach (KeyValuePair<string, InboxCommand> command in inboxItem.primaryCommands)
		{
			if (command.Value.args.Equals((string)actionArg))
			{
				onPrimaryButtonSelect(command.Key, command.Value);
				break;
			}
		}
	}

	private void onPrimaryButtonSelect(string actionKey, InboxCommand command)
	{
		Bugsnag.LeaveBreadcrumb($"Inbox item on click {actionKey}" + command.args);
		action(actionKey);
		playSelect();
		unregisterHandlers();
	}
}
