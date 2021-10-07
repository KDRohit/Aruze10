using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OOCAction : ServerAction, IResetGame
{
    private const string TRIGGER_SPECIAL_OOC_IF_AVAILABLE = "ooc_grant_reward";
    private const string ACCEPT_OOC_REWARD = "ooc_accept_reward_grant";
#if !ZYNGA_PRODUCTION
    private const string DEV_INIT = "ooc_initialize";
#endif
    
    private const string EVENT_ID = "event";

    private string eventId = "";
    
    private static Dictionary<string, string[]> _propertiesLookup = null;
    
    /** Constructor */
    private OOCAction(ActionPriority priority, string type) : base(priority, type) {}

    public static void triggerSpecialOOCEvent()
    {
        //do the action
        OOCAction action = new OOCAction(ActionPriority.IMMEDIATE, TRIGGER_SPECIAL_OOC_IF_AVAILABLE);
        processPendingActions(true);
    }

    public static void acceptRewardGrant(string id)
    {
        //do the action
        OOCAction action = new OOCAction(ActionPriority.IMMEDIATE, ACCEPT_OOC_REWARD);
        action.eventId = id;
        processPendingActions(true);
    }
    
#if !ZYNGA_PRODUCTION
    public static void devTriggerNextSpin()
    {
        OOCAction action = new OOCAction(ActionPriority.IMMEDIATE, DEV_INIT);
        processPendingActions(true);
    }
#endif
    
    
    public static Dictionary<string, string[]> propertiesLookup
    {
        get
        {
            if (_propertiesLookup == null)
            {
                _propertiesLookup = new Dictionary<string, string[]>();
                _propertiesLookup.Add(ACCEPT_OOC_REWARD, new string[] {EVENT_ID});
                _propertiesLookup.Add(TRIGGER_SPECIAL_OOC_IF_AVAILABLE, new string[] {});
#if !ZYNGA_PRODUCTION
                _propertiesLookup.Add(DEV_INIT, new string[] {});
#endif
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
                case EVENT_ID:
                    appendPropertyJSON(builder, property, eventId);
                    break;
                default:
                    Debug.LogWarning("Unknown property for action: " + type + ", " + property);
                    break;
            }
        }
    }
    
    new public static void resetStaticClassData()
    {
        _propertiesLookup = null;
    }
}
