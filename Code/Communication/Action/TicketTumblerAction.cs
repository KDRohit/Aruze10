using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
ServerAction class for handling app-related actions.
*/
public class TicketTumblerAction : ServerAction
{
	//action name
	private const string GET_INFO = "get_lottery_info";
	private string key = "";

	private const string KEY = "key";


	private TicketTumblerAction(ActionPriority priority, string type) : base(priority, type)
	{
		// Do I need this?
	}

	public static void getInfo(string lotteryKey)
	{
		TicketTumblerAction action = new TicketTumblerAction(ActionPriority.IMMEDIATE, GET_INFO);
		action.key = lotteryKey;
		ServerAction.processPendingActions(true);
	}

	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		if (!string.IsNullOrEmpty(key))
		{
			appendPropertyJSON(builder, KEY, key);
		}
	}	

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		// Nothing do to?
	}
}

