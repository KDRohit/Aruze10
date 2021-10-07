
using UnityEngine;
using System.Collections.Generic;

public class ComplianceUrlAction : ServerAction
{

	private const string GET_URL = "get_compliance_portal_url";
	private const string ACTION_TYPE = "compliance_url";
	private const string COMPLIANCE_TYPE = "compliance_type";

	public delegate void GetUrlCallback(string zid, string pin, string url);

	private string complianceType;

	private class ComplianceUrlRunner
	{
		private GetUrlCallback callbackFunc;

		public ComplianceUrlRunner(GetUrlCallback callback)
		{
			callbackFunc = callback;
		}

		public void run()
		{
			Server.registerEventDelegate(ACTION_TYPE, onActionComplete);
			processPendingActions(true);
		}

		private void onActionComplete(JSON json)
		{
			if (json != null)
			{
				string pin = json.getString("pin","");
				string url = json.getString("url", "");

				if (callbackFunc != null)
				{
					callbackFunc(SlotsPlayer.instance.socialMember.zId, pin, url);
				}
			}
		}
	}
	

	private ComplianceUrlAction(ActionPriority priority, string type) : base(priority, type)
	{
		
	}

	private static Dictionary<string, string[]> _propertiesLookup;
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(GET_URL, new string[] { COMPLIANCE_TYPE });
			}
			return _propertiesLookup;
		}
	}

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
				case COMPLIANCE_TYPE:
					if (complianceType != "")
					{
						appendPropertyJSON(builder, property, complianceType);
					}
					break;

				default:
					Debug.LogWarning("Unknown property for action: " + type + ", " + property);
					break;
			}
		}

	}

	public static void GetGDPRUrl(GetUrlCallback callback)
	{
		//instantiate action
		ComplianceUrlAction action = new ComplianceUrlAction(ActionPriority.HIGH, GET_URL);
		action.complianceType = "GDPR";

		//create runner for callback
		ComplianceUrlRunner actionHelper = new ComplianceUrlRunner(callback);
		actionHelper.run();
	}
	
	public static void GetCCPAUrl(GetUrlCallback callback)
	{
		//instantiate action
		ComplianceUrlAction action = new ComplianceUrlAction(ActionPriority.HIGH, GET_URL);
		action.complianceType = "CCPA";

		//create runner for callback 
		ComplianceUrlRunner actionHelper = new ComplianceUrlRunner(callback);
		actionHelper.run();
	}

	public new static void resetStaticClassData()
	{
	}

}
