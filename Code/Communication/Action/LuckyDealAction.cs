using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
ServerAction class for handling app-related actions.
*/
public class LuckyDealAction : ServerAction
{
	//action name
	private const string GET_DEAL = "get_wheel_deal";

	private LuckyDealAction(ActionPriority priority, string type) : base(priority, type)
	{
		// Do I need this?
	}

	public static void getDeal()
	{
		LuckyDealAction action = new LuckyDealAction(ActionPriority.HIGH, GET_DEAL);
		ServerAction.processPendingActions(true);
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		// Nothing do to?
	}
}

