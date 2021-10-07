using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FBConnectAction : ServerAction
{
	//CONSTANTS

	//SERVER ACTION
	public const string FB_CONNECT = "fb_connect";

	//PROPERTIES
	private const string FB_TOKEN_KEY = "fb_token";
	private const string ZIS_TOKEN_KEY = "zis_token";
	private const string FB_ZID_KEY = "fb_zid";


	private string fbToken = null;
	private string zisToken = null;
	private string fbZid = null;


	//Constructor
	private FBConnectAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	// Function to send fbConnect action to server
	public static void fbConnect(string fbToken, string zisToken, string fbZid)
	{
		FBConnectAction action = new FBConnectAction(ActionPriority.IMMEDIATE, FB_CONNECT);
		action.fbToken = fbToken;
		action.zisToken = zisToken;
		action.fbZid = fbZid;
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
				
				_propertiesLookup.Add(FB_CONNECT, new string[] { FB_TOKEN_KEY, ZIS_TOKEN_KEY, FB_ZID_KEY});
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
				case FB_TOKEN_KEY:
					appendPropertyJSON(builder, property, fbToken);
					break;
				case ZIS_TOKEN_KEY:
					appendPropertyJSON(builder, property, zisToken);
					break;
				case FB_ZID_KEY:
					appendPropertyJSON(builder, property, fbZid);
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
