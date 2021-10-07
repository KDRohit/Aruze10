using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VIPPhoneCollectAction : ServerAction
{
	// Type value
	public const string SUBMIT_VIP_DATA = "update_vip_data";
	public const string ACCEPT_REWARD = "accept_reward_for_vip_phone_num";

	// Argument for Accept reward
	private string eventID = "";

	// Argument for Submit VIP data
	private string phoneNumber = "";
	private string firstName = "";
	private string lastName = "";
	private string sendSMS = "";

	private const string EVENT_ID = "event";
	private const string PHONE_NUM = "phone_num";
	private const string FIRST_NAME = "first_name";
	private const string LAST_NAME = "last_name";
	private const string SEND_SMS = "send_sms";

	private VIPPhoneCollectAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	public static void vipSubmitInformation(string phoneNumber, string firstName, string lastName, string sendsms)
	{
		VIPPhoneCollectAction action = new VIPPhoneCollectAction(ActionPriority.HIGH, SUBMIT_VIP_DATA);
		action.phoneNumber = phoneNumber;
		action.firstName = firstName;
		action.lastName = lastName;
		action.sendSMS = sendsms;
	}

	public static void vipAcceptReward(string eventID)
	{
		VIPPhoneCollectAction action = new VIPPhoneCollectAction(ActionPriority.HIGH, ACCEPT_REWARD);
		action.eventID = eventID;
		ServerAction.processPendingActions(true);
	}

	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(SUBMIT_VIP_DATA, new string[] { PHONE_NUM, FIRST_NAME, LAST_NAME, SEND_SMS });
				_propertiesLookup.Add(ACCEPT_REWARD, new string[] { EVENT_ID });
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
				case PHONE_NUM:
					appendPropertyJSON(builder, property, phoneNumber);
					break;
				case FIRST_NAME:
					appendPropertyJSON(builder, property, firstName);
					break;
				case LAST_NAME:
					appendPropertyJSON(builder, property, lastName);
					break;
				case SEND_SMS:
					appendPropertyJSON(builder, property, sendSMS);
					break;
				case EVENT_ID:
					appendPropertyJSON(builder, property, eventID);
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
