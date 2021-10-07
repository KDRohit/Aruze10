using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RefreshZisTokenAction : ServerAction
{
	//CONSTANTS

	//SERVER ACTION
	public const string REFRESH_ZIS_TOKEN = "refresh_zis_token";

	//PROPERTIES
	private const string ZIS_TOKEN_KEY = "zis_token";
	private const string INSTALL_CREDENTIALS = "install_credentials";



	private string zisToken = null;
	private string installCredentials = null;

	//Construtor
	private RefreshZisTokenAction (ActionPriority priority, string type) : base(priority, type) 
	{
	}

	public static void RefreshZisToken(string zisToken, string installCredentials)
	{
		RefreshZisTokenAction action = new RefreshZisTokenAction(ActionPriority.IMMEDIATE, REFRESH_ZIS_TOKEN);
		action.zisToken = zisToken;
		action.installCredentials = installCredentials; 
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

				_propertiesLookup.Add(REFRESH_ZIS_TOKEN, new string[] { ZIS_TOKEN_KEY, INSTALL_CREDENTIALS });
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
				case INSTALL_CREDENTIALS:
					appendPropertyJSON(builder, property, installCredentials);
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
