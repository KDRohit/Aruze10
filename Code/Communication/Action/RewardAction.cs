using UnityEngine;
using System.Collections;
using System.Collections.Generic;
	/**
	 * ...
	 * @author Nick reynolds
	 */
public class RewardAction : ServerAction
{
	public static string validateRewardAccessKey = "";
	
	private const string REWARD_CREATE = "create_reward_post";
	private const string VALIDATE_FEED = "validate_feed";
	private const string FEED_CLAIM = "claim_feed";
	private const string ZADE_CREDIT_CLAIM = "ixpromo_grant_reward";

	private const string WIZARD_INCENTIVE_CLAIM = "accept_wizard_incentive";
	private const string LIKE_INCENTIVE_CLAIM = "accept_like_incentive";
	private const string GENERIC_REWARD_CLAIM = "reward_claim";

	private string rewardType = "";
	private string accessKey = "";
	private string eventId = "";
	private string zadePayload = "";


	//This string is private and only is set sometimes.  It is inaccessible elsewhere, so it's wasted.  If needed, remove this comment and note 
	//its usage.  Otherwise it is generating an unnecessary error.
	//private string playerId = "";

	//property names
	private const string REWARD_TYPE = "reward_type";
	private const string ACCESS_KEY = "access_key";
	private const string EVENT_ID = "event";
	private const string ZADE_PAYLOAD = "payload";

	public static void requestRewardAccessKey(string rewardType)
	{
		RewardAction action = new RewardAction(ActionPriority.NORMAL, REWARD_CREATE);
		action.rewardType = rewardType;
		//action.playerId = SlotsPlayer.instance.socialMember.zId;
	}

	public static void validateReward(string accessKey)
	{
		RewardAction action = new RewardAction(ActionPriority.IMMEDIATE, VALIDATE_FEED);
		action.accessKey = accessKey;
		
		// The server will not send this to us in the response,
		// but we need to remember it to tell the server that the player got it.
		validateRewardAccessKey = accessKey;
		Debug.LogFormat("PN> RewardAction.validateReward with rewardkey {0}, type = {1}", action.accessKey, action.rewardType);
	}
	
	public static void claimFeedReward(string accessKey)
	{
		RewardAction action = new RewardAction(ActionPriority.IMMEDIATE, FEED_CLAIM);
		action.accessKey = accessKey;
	}

	public static void claimWizardIncentiveReward(string eventId)
	{
		RewardAction action = new RewardAction(ActionPriority.NORMAL, WIZARD_INCENTIVE_CLAIM);
		action.eventId = eventId;
	}

	public static void claimLikeIncentiveReward(string eventId)
	{
		RewardAction action = new RewardAction(ActionPriority.NORMAL, LIKE_INCENTIVE_CLAIM);
		action.eventId = eventId;
	}

	public static void claimZadeInstallCredit(Dictionary<string, object> json)
	{
		RewardAction action = new RewardAction(ActionPriority.NORMAL, ZADE_CREDIT_CLAIM);
		action.zadePayload = Common.zEncodeURL(JSON.createJsonString("", json));
	}

	public static void claimGenericReward(string eventID)
	{
		RewardAction action = new RewardAction(ActionPriority.HIGH, GENERIC_REWARD_CLAIM);
		action.eventId = eventID;
		ServerAction.processPendingActions(true);
	}

	private RewardAction(ActionPriority priority, string type) : base(priority, type)
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
				_propertiesLookup.Add(REWARD_CREATE, new string[] { REWARD_TYPE, "player_id" });
				_propertiesLookup.Add(VALIDATE_FEED, new string[] { ACCESS_KEY });
				_propertiesLookup.Add(FEED_CLAIM, new string[] { ACCESS_KEY });
				_propertiesLookup.Add(WIZARD_INCENTIVE_CLAIM, new string[] { EVENT_ID });
				_propertiesLookup.Add(LIKE_INCENTIVE_CLAIM, new string[] { EVENT_ID });
				_propertiesLookup.Add(ZADE_CREDIT_CLAIM, new string[] {ZADE_PAYLOAD	});
				_propertiesLookup.Add(GENERIC_REWARD_CLAIM, new string[] {EVENT_ID});
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
				case REWARD_TYPE:
					appendPropertyJSON(builder, property, rewardType);
					break;
				case "player_id":
					//appendPropertyJSON(builder, property, playerID);
					break;
				case ACCESS_KEY:
					appendPropertyJSON(builder, property, accessKey);
					break;
				case EVENT_ID:
					appendPropertyJSON(builder, property, eventId);
					break;
				case ZADE_PAYLOAD:
					appendPropertyJSON(builder, property, zadePayload);
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

#if UNITY_EDITOR
	public static void testCreateRewardAction()
	{
		RewardAction action = new RewardAction(ActionPriority.NORMAL, ZADE_CREDIT_CLAIM);
		Dictionary<string,object> json = new Dictionary<string,object>();
		json["testKey"] = "testValue";
		action.zadePayload = Common.zEncodeURL(JSON.createJsonString("", json));
		string template = "{\"sort_order\":2,\"type\":\"ixpromo_grant_reward\",\"payload\":\"%7B%22testKey%22%3A%22testValue%22%7D\"}";
		string output = ServerAction.getBatchStringForTesting(action);
		Debug.Log(string.Format("ServerAction {0}: {1}", action, output));
		if (output != template)
		{
			Debug.LogError(string.Format("ServerAction {0} has invalid serialization {1}: doesn't match {2}", action, output, template));
		}
	}
#endif

}

