using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
ServerAction called before a payment is initiated, to send bonuses, etc.

{
  "actions": [
    {
      "sort_order": -1,
      "type": "pre_payment",
      "package_name": "coin_package_20",
      "ref_str": {
        "p": "one_click_buy",
        "bonus_percent": 100
      }
    }
  ]
}
*/


public class PrePaymentAction : ServerAction 
{
	/// ServerAction type.
	public const string PRE_PAYMENT = "pre_payment";
	
	private string package_name = null;
	private string ref_str = null;

	//property names
	private const string PACKAGE_NAME = "package_name";
	private const string REF_STR = "ref_str";

	public static void prePayment(string package, string reference_json)
	{
		PrePaymentAction action = new PrePaymentAction(ActionPriority.IMMEDIATE, PRE_PAYMENT);

		action.package_name = package;
		action.ref_str = reference_json;

		// Send this immediately since we're also about to fire off a purchase momentarily:
		ServerAction.processPendingActions(true);
	}

	private PrePaymentAction(ActionPriority priority, string type) : base(priority, type)
	{
	}


	////////////////////////////////////////////////////////////////////////

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(PRE_PAYMENT, new string[] {PACKAGE_NAME, REF_STR});
			}
			return _propertiesLookup;
		}
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
				case PACKAGE_NAME:
					appendPropertyJSON(builder, property, package_name);
					break;
				case REF_STR:
					appendPropertyLiteralJSON(builder, property, ref_str);
					break;
				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		//ServerAction.resetStaticClassData(); NEVER CALL THE BASE CLASS'S RESET!
		_propertiesLookup = null;
	}
}

