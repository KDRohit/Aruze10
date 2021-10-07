using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AccountAction : ServerAction
{
	public const string CONVERT = "convert_account";

	private string fbId = "";
	private string fbAccessToken = "";
	
	//property names
	private const string FB_ID = "fb_id";
	private const string ACCESS_TOKEN = "access_token";
	
	private AccountAction(ActionPriority priority, string type, string fbId, string fbAccessToken) : base(priority, type)
	{
		this.fbId = fbId;
		this.fbAccessToken = fbAccessToken;
	}
	
	/// Convert an anonymous account to use Facebook.
	public static void convert(string fbId, string fbAccessToken)
	{
		Debug.Log(string.Format("AccountAction.convert(fb_id='{0}', token='{1})", fbId, fbAccessToken));
		
		// Disabling unused warning.
		#pragma warning disable 0219
		AccountAction action = new AccountAction(ActionPriority.HIGH, CONVERT, fbId, fbAccessToken);
		#pragma warning restore 0219
		
		ServerAction.processPendingActions(true);
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
				_propertiesLookup.Add(CONVERT, new string[] { FB_ID, ACCESS_TOKEN });
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
				case FB_ID:
					appendPropertyJSON(builder, property, fbId);
					break;
				case ACCESS_TOKEN:
					appendPropertyJSON(builder, property, fbAccessToken);
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

