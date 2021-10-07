// Author            : changqi.du (duke) <ddu@zynga.com>
// Date              : 20.10.2019

using System;
using UnityEngine;
using System.Collections.Generic;

public class ClearEosExperimentWhitelistAction : ServerAction
{
    private const string CLEAR_EOS_EXPERIMENT_WHITELIST = "clear_eos_experiment_whitelist";
    private const string CLEAR_EXPERIMENT_RESPONSE = "clear_experiment_response";
    private const string EXPERIMENT= "experiment";
    private string experimentName;
    
    private static readonly Dictionary<string, string[]> _propertiesLookup = new Dictionary<string, string[]>()
    {
        {CLEAR_EOS_EXPERIMENT_WHITELIST,  new string[] {EXPERIMENT}}
    };
    
    public static event Action<bool> ProcessResponseEvent; 
        
    // handle response from Server, and display information accordingly
    protected static void processResponse(JSON data)
    {
        ProcessResponseEvent.Invoke(data.getBool("success", false));
    }
    
    private ClearEosExperimentWhitelistAction(ActionPriority priority, string type) : base(priority, type)
    {
        
    }
    
    // A dictionary of string properties associated with this
    public static Dictionary<string, string[]> propertiesLookup
    {
        get
        {
            return _propertiesLookup;
        }
    }
    
    public static void clearExperimentWhitelist(string experimentName)
    {
        ClearEosExperimentWhitelistAction action = new ClearEosExperimentWhitelistAction(ActionPriority.HIGH, CLEAR_EOS_EXPERIMENT_WHITELIST);
        action.experimentName = experimentName; 
        Server.registerEventDelegate(CLEAR_EXPERIMENT_RESPONSE, processResponse);
    }
    
    public override void appendSpecificJSON(System.Text.StringBuilder builder)
    {
        foreach (string property in propertiesLookup[type])
        {
            switch (property)
            {
                case EXPERIMENT:
                    appendPropertyJSON(builder, property, experimentName);
                    break;
                default:
                    Debug.LogWarning("Unknown property for action: " + type + ", " + property);
                    break;
            }
        }
    }
    
    // Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
    public static void resetStaticClassData()
    {
    }
}
