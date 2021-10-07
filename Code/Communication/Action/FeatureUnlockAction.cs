using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeatureUnlockAction : ServerAction
{
    private const string GET_INFO_ACTION = "feature_get_info_action";
    private const string FEATURE_NAME = "feature_name";
    
    private string featureName;
    
    private FeatureUnlockAction(ActionPriority priority, string type) : base(priority, type) {}
    
    private static Dictionary<string, string[]> _propertiesLookup = null;

    public static Dictionary<string, string[]> propertiesLookup
    {
        get
        {
            if (_propertiesLookup == null)
            {
                _propertiesLookup = new Dictionary<string, string[]>();
                _propertiesLookup.Add(GET_INFO_ACTION, new string[] { FEATURE_NAME });
            }
            return _propertiesLookup;
        }
    }

    public static void getFeatureInfo(string feature)
    {
        FeatureUnlockAction action = new FeatureUnlockAction(ActionPriority.IMMEDIATE, GET_INFO_ACTION);
        action.featureName = feature;
        processPendingActions();
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
                case FEATURE_NAME:
                    appendPropertyJSON(builder, property, featureName);
                    break;
                default:
                    Debug.LogWarning($"Unknown property for action={type} {property}");
                    break;
            }
        }
    }
}
