using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EmailConnectAction : ServerAction
{
	//CONSTANTS

	//SERVER ACTION
	public const string EMAIL_CONNECT = "email_connect";

	//PROPERTIES
	private const string ZIS_TOKEN_KEY = "zis_token";
	private const string EMAIL = "email";


	private string zisToken = null;
	private string email = null;


	//Constructor
	private EmailConnectAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	// Function to send fbConnect action to server
	public static void emailConnect(string zisToken, string email)
	{
		EmailConnectAction action = new EmailConnectAction(ActionPriority.IMMEDIATE, EMAIL_CONNECT);
		action.zisToken = zisToken;
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

				_propertiesLookup.Add(EMAIL_CONNECT, new string[] { ZIS_TOKEN_KEY, EMAIL });
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
				
				case ZIS_TOKEN_KEY:
					appendPropertyJSON(builder, property, zisToken);
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
