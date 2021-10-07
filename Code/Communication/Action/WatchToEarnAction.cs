using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Sets up watch to earn related actions.
*/

public class WatchToEarnAction : ServerAction
{
	// ServerAction names
	public const string W2E_MOTD_SEEN = "w2e_motd_seen";
	public const string W2E_STATS = "w2e_stats";
	public const string W2E_UNITY_ADS_ACCEPT = "w2e_accept_reward_grant";

	// Properties
	private string statType = "";
	private string placementId = "";
	private string eventId;
	private string rewardType = "";
	private string rewardValue = "";
	private string validity = "";
	private string validitySig = "";
	
	// Property names
	private const string STAT_TYPE = "statType";
	private const string EVENT_ID = "event";
	private const string PLACEMENT_ID = "placementId";
	private const string REWARD_TYPE = "rewardType";
	private const string REWARD_VALUE = "rewardValue";
	private const string VALIDITY = "validity";
	private const string VALIDITY_SIG = "validitySig";
	
	private WatchToEarnAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	// Send the watch to earn stats calls that are used to tell the server when to sync with Supersonic.
	public static void statsCall(string statType, UnityAdsManager.PlacementId placementId)
	{
		WatchToEarnAction action = new WatchToEarnAction(ActionPriority.HIGH, W2E_STATS);
		action.statType = statType;
		action.placementId = UnityAdsManager.getStatName(placementId);
		ServerAction.processPendingActions(true);
	}
	
	// Tell the server that we have seen the watch to earn motd, which updates the login data value.
	public static void markMotdSeen()
	{
		new WatchToEarnAction(ActionPriority.HIGH, W2E_MOTD_SEEN);
		ServerAction.processPendingActions(true);
	}
	
	// Tell the server that we have finished watching the supersonic video and are ready to grant the credits on our end.
	public static void acceptCoinGrant(string eventId)
	{
#if UNITY_ADS
		WatchToEarnAction action = new WatchToEarnAction(ActionPriority.HIGH, W2E_UNITY_ADS_ACCEPT);
		action.eventId = eventId;
		ServerAction.processPendingActions(true);
#endif
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
				_propertiesLookup.Add(W2E_UNITY_ADS_ACCEPT, new string[]{EVENT_ID});
				_propertiesLookup.Add(W2E_MOTD_SEEN, new string[]{});
				_propertiesLookup.Add(W2E_STATS, new string[]{STAT_TYPE});
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
				case EVENT_ID:
					appendPropertyJSON(builder, property, eventId);
					break;
				case STAT_TYPE:
					appendPropertyJSON(builder, property, statType);
					break;
				case PLACEMENT_ID:
					appendPropertyJSON(builder, property, placementId);
					break;
				case REWARD_TYPE:
					appendPropertyJSON(builder, property, rewardType);
					break;
				case REWARD_VALUE:
					appendPropertyJSON(builder, property, rewardValue);
					break;
				case VALIDITY:
					appendPropertyJSON(builder, property, validity);
					break;
				case VALIDITY_SIG:
					appendPropertyJSON(builder, property, validitySig);
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

