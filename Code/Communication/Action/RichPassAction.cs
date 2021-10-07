using System.Collections.Generic;
using UnityEngine;

//Class used to define the communication actions for the rich pass between the client and server
public class RichPassAction : ServerAction
{
    //action types
    private const string CLAIM_REWARD = "rp_claim_reward";
    private const string GET_INFO = "rp_get_info";
    private const string CLAIM_BANK_REWARD = "rp_claim_bank_reward";
    private const string DEV_INCREMENT_POINTS = "rp_add_pass_points";
    private const string DEV_RESET_PLAYER = "rp_delete_player";
    private const string DEV_SET_PASS_TYPE = "rp_set_player_pass";
    private const string REFRESH_CHALLENGES = "rp_refresh_challenge";
    private const string CLAIM_REPEATABLE_REWARD = "rp_claim_repeatable_reward";
    
    //property names
    private const string ID = "id";
    private const string CUMULATIVE_POINTS = "cumulative_points";
    private const string POINTS = "pass_points";
    private const string PASS_TYPE = "pass_type";
    private const string EVENT_ID = "event";
    

    private int id;
    private long points;
    private string passType;
    private string eventId;
    
    
    private static readonly Dictionary<string, string[]> propertiesLookup;

    static RichPassAction()
    {
        propertiesLookup = new Dictionary<string, string[]>();   
        propertiesLookup.Add(CLAIM_REWARD, new string[] { ID, CUMULATIVE_POINTS, PASS_TYPE });
        propertiesLookup.Add(GET_INFO, new string[] {});
        propertiesLookup.Add(CLAIM_BANK_REWARD, new string[] {EVENT_ID});
        propertiesLookup.Add(DEV_INCREMENT_POINTS, new string[] { POINTS });
        propertiesLookup.Add(DEV_RESET_PLAYER, new string[] {});
        propertiesLookup.Add(DEV_SET_PASS_TYPE, new string[] { PASS_TYPE });
        propertiesLookup.Add(REFRESH_CHALLENGES, new string[] {});
        propertiesLookup.Add(CLAIM_REPEATABLE_REWARD, new string[] { ID, CUMULATIVE_POINTS, PASS_TYPE });
    }
    
    public RichPassAction(ActionPriority actionPriority, string type) : base(actionPriority, type)
    {
        
    }

    public static void addPoints(long pointsToAdd)
    {
        RichPassAction action = new RichPassAction(ActionPriority.IMMEDIATE, DEV_INCREMENT_POINTS);
        action.points = pointsToAdd;
        processPendingActions();
    }

    public static void resetPlayer()
    {
        RichPassAction action = new RichPassAction(ActionPriority.IMMEDIATE, DEV_RESET_PLAYER);
        processPendingActions();
    }

    public static void setPassToGold()
    {
        RichPassAction action = new RichPassAction(ActionPriority.IMMEDIATE, DEV_SET_PASS_TYPE);
        action.passType = "gold";
        processPendingActions(true);
    }

    public static void setPassToSilver()
    {
        RichPassAction action = new RichPassAction(ActionPriority.IMMEDIATE, DEV_SET_PASS_TYPE);
        action.passType = "silver";
        processPendingActions();
    }

    public static void claimReward(string pass, int rewardId, long points)
    {
        RichPassAction action = new RichPassAction(ActionPriority.IMMEDIATE, CLAIM_REWARD);
        action.passType = pass;
        action.id = rewardId;
        action.points = points;
        processPendingActions(true);
    }
    
    public static void claimRepeatableReward(string pass, int rewardId, long points)
    {
        RichPassAction action = new RichPassAction(ActionPriority.IMMEDIATE, CLAIM_REPEATABLE_REWARD);
        action.passType = pass;
        action.id = rewardId;
        action.points = points;
        processPendingActions(true);
    }
    
    public static void getPassInfo()
    {
        //response handler is in feature base
        RichPassAction action = new RichPassAction(ActionPriority.IMMEDIATE, GET_INFO);
        processPendingActions(true);
    }
    
    public static void refreshChallenges()
    {
        RichPassAction action = new RichPassAction(ActionPriority.IMMEDIATE, REFRESH_CHALLENGES);
        processPendingActions();
    }

    public static void claimBankReward(string eventId)
    {
        RichPassAction action = new RichPassAction(ActionPriority.IMMEDIATE, CLAIM_BANK_REWARD);
        action.eventId = eventId;
        processPendingActions(true);
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
                case ID:
                    appendPropertyJSON(builder, property, id);
                    break;
                
                case CUMULATIVE_POINTS:
                    appendPropertyJSON(builder, property, points);
                    break;
                
                case PASS_TYPE:
                    appendPropertyJSON(builder, property, passType);
                    break;
                
                case EVENT_ID:
                    appendPropertyJSON(builder, property, eventId);
                    break;
                
                case POINTS:
                    appendPropertyJSON(builder, property, points);
                    break;
                
                default:
                    Debug.LogWarning("Unknown property for action: " + type + ", " + property);
                    break;
            }
        }
    }
    
}
