// Author            : changqi.du (duke) <ddu@zynga.com>
// Date              : 22.10.2019
//
// This action whitelist current player into holdout of an experiment

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class HoldoutExperimentAction : ServerAction
{
    private const string HOLDOUT_EXPERIMENT = "holdout_eos_experiment_whitelist";
    private const string HOLDOUT_RESPONSE = "holdout_response";
    private const string EXPERIMENT = "experiment";
    private string experimentName;
    
    private static readonly Dictionary<string, string[]> _propertiesLookup = new Dictionary<string, string[]>()
    {
        {HOLDOUT_EXPERIMENT,  new string[] {EXPERIMENT}}
    };
    
    public static event Action<bool> ProcessResponseEvent;
        
    protected static void processResponse(JSON data)
    {
        bool success = data.getBool("success", false);
        
        if (success)
        {
            JSON variableInfo = data.getJSON("variants");
            string experimentName = data.getString("experiment_name", "");
            ExperimentManager.modifyEosExperiment(experimentName, variableInfo);
        }

        ProcessResponseEvent.Invoke(success);
    }
    
    private HoldoutExperimentAction(ActionPriority priority, string type) : base(priority, type)
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
    
    public static void holdoutExperiment(string experimentName)
    {
        HoldoutExperimentAction action = new HoldoutExperimentAction(ActionPriority.HIGH, HOLDOUT_EXPERIMENT);
        action.experimentName = experimentName; 
        Server.registerEventDelegate(HOLDOUT_RESPONSE, processResponse);
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
