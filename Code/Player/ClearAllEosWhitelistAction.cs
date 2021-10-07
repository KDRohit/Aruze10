// Author            : changqi.du (duke) <ddu@zynga.com>
// Date              : 20.10.2019

using System;
using UnityEngine;
using System.Collections.Generic;

public class ClearAllEosWhitelistAction : ServerAction
{
    private static readonly Dictionary<string, string[]> _propertiesLookup = new Dictionary<string, string[]>()
    {
        {CLEAR_ALL_EOS_EXPERIMENTS_WHITELIST,  new string[] {CLIENT_EOS_LIST}}
    };
    
    private const string CLEAR_ALL_EOS_EXPERIMENTS_WHITELIST = "clear_all_eos_experiments_whitelist";
    private const string CLEAR_ALL_EXPERIMENTS_RESPONSE = "clear_all_experiments_response";
    private const string CLIENT_EOS_LIST = "client_eos_list";
    public static event Action<bool, string[]> ProcessResponseEvent; 
    private List<string> clientEosList;
    
    // handle response from Server, and display information accordingly
    protected static void processResponse(JSON data)
    {
        ProcessResponseEvent.Invoke(data.getBool("total_failure", false), data.getStringArray("failed_list"));
    }
    
    private ClearAllEosWhitelistAction(ActionPriority priority, string type) : base(priority, type)
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
                case CLIENT_EOS_LIST:
                    appendPropertyJSON(builder, property, clientEosList);
                    break;
                default:
                    Debug.LogWarning("Unknown property for action: " + type + ", " + property);
                    break;
            }
        }
    }
    
    public static void clearAllExperimentWhitelist(List<string> gameList)
    {
        ClearAllEosWhitelistAction action = new ClearAllEosWhitelistAction(ActionPriority.HIGH, CLEAR_ALL_EOS_EXPERIMENTS_WHITELIST);
        action.clientEosList = gameList;
        Server.registerEventDelegate(CLEAR_ALL_EXPERIMENTS_RESPONSE, processResponse);
    }
    
    // Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
    public static void resetStaticClassData()
    {
 
    }
}
