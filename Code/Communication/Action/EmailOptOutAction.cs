using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EmailOptOutAction : ServerAction
{

	//SERVER ACTION
	public const string EMAIL_CONSENT = "email_consent";

	//PROPERTIES
	private const string OPT_OUT = "opt_out";
	private const string EMAIL = "email";


	private string optOut = null;
	private string email = null;


	//Constructor
	private EmailOptOutAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	// Function to send emailoptin action to server
	public static void emailOptOut(string optOut, string email)
	{
		EmailOptOutAction action = new EmailOptOutAction(ActionPriority.IMMEDIATE, EMAIL_CONSENT);
		action.optOut = optOut;
		action.email = email;
		processPendingActions(true);
	}


	private static Dictionary<string, string[]> _propertiesLookup = null;
	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();

				_propertiesLookup.Add(EMAIL_CONSENT, new string[] { OPT_OUT, EMAIL });
			}
			return _propertiesLookup;
		}
	}

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
				case OPT_OUT:
					appendPropertyJSON(builder, property, optOut);
					break;
				case EMAIL:
					appendPropertyJSON(builder, property, email);
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
		_propertiesLookup = null;
	}

}
