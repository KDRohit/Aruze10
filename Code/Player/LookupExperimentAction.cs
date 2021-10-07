// Author            : changqi.du (duke) <ddu@zynga.com>
// Date              : 22.10.2019
//
// This action lookup the detail of an experiment

using System;
using UnityEngine;
using System.Collections.Generic;

public class LookupExperimentAction : ServerAction
{
    private const string LOOKUP_EXPERIMENT = "get_specific_eos_experiment";
    private const string EXPERIMENT_RESPONSE = "experiment_response";
    private const string EXPERIMENT_NAME = "experiment_name";
    private const string CLIENT_VARIANT_NAME = "client_variant_name";
    
    private string experimentName;
    private string clientVariantName;
    
    
    private static readonly Dictionary<string, string[]> _propertiesLookup = new Dictionary<string, string[]>()
    {
        {LOOKUP_EXPERIMENT,  new string[] {EXPERIMENT_NAME, CLIENT_VARIANT_NAME}}
    };
    
    public static event Action<bool, string, JSON[]> ProcessResponseEvent;
        
    protected static void processResponse(JSON data)
    {
        bool success = data.getBool("success", false);
        JSON[] items = data.getJsonArray("variants");
        string playerGroup = data.getString("playerGroup", "");
        ProcessResponseEvent.Invoke(success, playerGroup, items);
    }
    
    private LookupExperimentAction(ActionPriority priority, string type) : base(priority, type)
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
    
    public static void lookupExperiment(string experimentName, string clientVariantName)
    {
        LookupExperimentAction action = new LookupExperimentAction(ActionPriority.HIGH, LOOKUP_EXPERIMENT);
        action.experimentName = experimentName;
        action.clientVariantName = clientVariantName;
        Server.registerEventDelegate(EXPERIMENT_RESPONSE, processResponse);
    }
    
    public override void appendSpecificJSON(System.Text.StringBuilder builder)
    {
        foreach (string property in propertiesLookup[type])
        {
            switch (property)
            {
                case EXPERIMENT_NAME:
                    appendPropertyJSON(builder, property, experimentName);
                    break;
                case CLIENT_VARIANT_NAME:
                    appendPropertyJSON(builder, property, clientVariantName);
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
