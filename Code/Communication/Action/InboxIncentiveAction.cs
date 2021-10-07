using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
ServerAction class for handling app-related actions.
*/
public class InboxIncentiveAction : ServerAction
{
    //action name
    private const string GIFT_CHEST_OFFER = "gift_chest_offer";

    // Name of field
    private const string OFFER_ACTION_TYPE = "offer_action_type";

    // view or close.
    private static string actionType = "";

    private InboxIncentiveAction(ActionPriority priority, string type) : base(priority, type)
    {
        // Do I need this?
    }

    public static void viewOffer()
    {
        actionType = "view";
        InboxIncentiveAction action = new InboxIncentiveAction(ActionPriority.HIGH, GIFT_CHEST_OFFER);
        ServerAction.processPendingActions(true);
    }

    public static void closeOffer()
    {
        actionType = "close";
        InboxIncentiveAction action = new InboxIncentiveAction(ActionPriority.HIGH, GIFT_CHEST_OFFER);
        ServerAction.processPendingActions(true);
    }

    public static Dictionary<string, string[]> propertiesLookup
    {
        get
        {
            if (_propertiesLookup == null)
            {
                _propertiesLookup = new Dictionary<string, string[]>();
                _propertiesLookup.Add(GIFT_CHEST_OFFER, new string[] {OFFER_ACTION_TYPE});
            }
            return _propertiesLookup;
        }
    }
    private static Dictionary<string, string[]> _propertiesLookup = null;

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
                case OFFER_ACTION_TYPE:
                    appendPropertyJSON(builder, property, actionType);
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
        // Nothing do to?
    }
}

