using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WelcomeJourneyAction : ServerAction
{

	/****** Action Names *****/
	private const string CLAIM_REWARD = "welcome_journey_claim";

	private WelcomeJourneyAction(ActionPriority priority, string type) : base(priority, type) {}

	public static void claimReward()
	{
		WelcomeJourneyAction action = new WelcomeJourneyAction(ActionPriority.HIGH, CLAIM_REWARD);
		ServerAction.processPendingActions(true);
	}

	private static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(CLAIM_REWARD, new string[] {});
			}
			return _propertiesLookup;
		}
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		_propertiesLookup = null;
	}

	private static Dictionary<string, string[]> _propertiesLookup = null;

	/// Appends all the specific action properties to json
	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		if (!propertiesLookup.ContainsKey(type))
		{
			Debug.LogError("No properties defined for action: " + type);
			return;
		}

		foreach (string property in propertiesLookup[type])
		{
			switch (property)
			{
				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}
	}

}
