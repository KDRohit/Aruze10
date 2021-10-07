// Author            : changqi.du (duke) <ddu@zynga.com>
// Date              : 22.10.2019
//
// This action whitelist the user into an assigned variant 

using System;
using UnityEngine;
using System.Collections.Generic;

public class SwitchVariantAction : ServerAction
{
    private static readonly Dictionary<string, string[]> _propertiesLookup = new Dictionary<string, string[]>()
    {
        {ASSIGN_EOS_WHITELIST,  new string[] {EXPERIMENT, VARIANT}}
    };
    
    private const string ASSIGN_EOS_WHITELIST = "assign_eos_whitelist";
    private const string EXPERIMENT= "experiment";
    private const string VARIANT= "variant";
    
    private string experiment;
    private string variant;
    
    private static JSON changedVariables;
    private static string clientExperimentName;

    public static event Action<bool> ProcessResponseEvent;
    
    // handle response from Server, and display information accordingly
    protected static void processResponse(JSON data)
    {
        bool success = data.getBool("success", false);
        
        if (success)
        {
            ExperimentManager.modifyEosExperiment(clientExperimentName, changedVariables);
        }
        
        ProcessResponseEvent.Invoke(success);
    }
    
    private SwitchVariantAction(ActionPriority priority, string type) : base(priority, type)
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
    
    public override void appendSpecificJSON(System.Text.StringBuilder builder)
    {
        foreach (string property in propertiesLookup[type])
        {
            switch (property)
            {
                case EXPERIMENT:
                    appendPropertyJSON(builder, property, experiment);
                    break;
                case VARIANT:
                    appendPropertyJSON(builder, property, variant);
                    break;
                default:
                    Debug.LogWarning("Unknown property for action: " + type + ", " + property);
                    break;
            }
        }
    }
    
    public static void switchVariant(string experimentName, string variantName, JSON variables)
    {
        SwitchVariantAction action = new SwitchVariantAction(ActionPriority.HIGH, ASSIGN_EOS_WHITELIST);
        
        action.experiment = experimentName;
        action.variant = variantName;
        changedVariables = variables;
        clientExperimentName = experimentName;
        
        Server.registerEventDelegate(ASSIGN_EOS_WHITELIST, processResponse);
    }
    
    
    /// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
    public static void resetStaticClassData()
    {
    }
}
