using UnityEngine;
using System.Collections.Generic;

public class RobustChallengesAction : ServerAction
{
    // =============================
    // PRIVATE
    // =============================
	private string eventID = "";
    private string campaignID = null;

    // =============================
    // CONST
    // =============================
	private const string CHALLENGE_CAMPAIGN_UPDATE_ACTION = "challenge_campaign_progress";
	private const string CHALLENGE_CAMPAIGN_LOST_SEEN_ACTION = "challenge_campaign_lost_seen";
	private const string CHALLENGE_CLAIM = "challenge_claim";
	private const string CHALLENGE_CAMPAIGN_RESTART_ACTION = "challenge_campaign_update";
	private const string RESET_CHALLENGES = "reset_challenges";

	private const string EVENT_ID = "event";
	private const string EXPERIMENT = "experiment";

	private RobustChallengesAction(ActionPriority priority, string type) : base(priority, type)
	{
	}

	public static void claimReward(string campaignID = "", EventDelegate callback = null)
	{
		RobustChallengesAction action = new RobustChallengesAction(ActionPriority.HIGH, CHALLENGE_CLAIM);
		action.campaignID = campaignID;
		if (callback != null)
		{
			Server.registerEventDelegate("challenge_campaign_progress", callback, false);
		}
		ServerAction.processPendingActions(true);
	}

	public static void getCampaignRestartData(string campaignID = "", EventDelegate callback = null)
	{
		RobustChallengesAction action = new RobustChallengesAction(ActionPriority.HIGH, CHALLENGE_CAMPAIGN_RESTART_ACTION);
		action.campaignID = campaignID;
		if (callback != null)
		{
			Server.registerEventDelegate("challenge_campaign_update_event", callback, false);
		}
		ServerAction.processPendingActions(true);
	}

	public static void getRobustChallengesProgressUpdateInfo( string campaignID = null )
	{
		//adds to pending server on construction
		RobustChallengesAction action = new RobustChallengesAction(ActionPriority.HIGH, CHALLENGE_CAMPAIGN_UPDATE_ACTION);
        action.campaignID = campaignID;
		ServerAction.processPendingActions(true);
	}

	// Response to server that client has seen the ended dialog.
	public static void sendLostSeenResponse(string eventID)
	{
		RobustChallengesAction action = new RobustChallengesAction(ActionPriority.HIGH, CHALLENGE_CAMPAIGN_LOST_SEEN_ACTION);
		action.eventID = eventID;
		ServerAction.processPendingActions(true);
	}

	public static void resetChallenges()
	{
		RobustChallengesAction action = new RobustChallengesAction(ActionPriority.HIGH, RESET_CHALLENGES);
		ServerAction.processPendingActions(true);
	}
	
	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(CHALLENGE_CAMPAIGN_UPDATE_ACTION, new string[] { EXPERIMENT });
				_propertiesLookup.Add(CHALLENGE_CLAIM, new string[] {EXPERIMENT});
				_propertiesLookup.Add(CHALLENGE_CAMPAIGN_RESTART_ACTION, new string[] {EXPERIMENT});
				_propertiesLookup.Add(CHALLENGE_CAMPAIGN_LOST_SEEN_ACTION, new string[] {EVENT_ID});
				_propertiesLookup.Add(RESET_CHALLENGES, new string[] {});
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
					appendPropertyJSON(builder, property, eventID);
					break;
                case EXPERIMENT:
                    if ( !string.IsNullOrEmpty( campaignID ) )
                    {
                        appendPropertyJSON(builder, property, campaignID);
                    }
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
	}
}
