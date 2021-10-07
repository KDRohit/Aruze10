using System.Collections.Generic;
using UnityEngine;

public class EueFeatureUnlocks : FeatureBase
{
    public static EueFeatureUnlocks instance { get; private set; }
    
    public Dictionary<string, FeatureUnlockData> featureUnlocksDict { get; private set; }

    private const string FEATURE_INFO_EVENT = "feature_info";

    public static void instantiateFeature(JSON data)
    {
        if (instance != null)
        {
            instance.clearEventDelegates();
        }
        instance = new EueFeatureUnlocks();
        instance.initFeature(data);
    }

    private void registerServerEvents()
    {
        Server.registerEventDelegate(FEATURE_INFO_EVENT, onGetInfoForFeature, true);
    }

    private void onGetInfoForFeature(JSON data)
    {
        string featureName = data.getString("feature_name", "");
        FeatureUnlockData unlockData;
        if (instance.featureUnlocksDict.TryGetValue(featureName, out unlockData))
        {
            if (unlockData.getFeatureInfoEventDelegate != null)
            {
                JSON featureData = data.getJSON("client_data");
                if (featureData != null)
                {
                    unlockData.getFeatureInfoEventDelegate(featureData);
                }
                else
                {
                    JSON[] featureDataArray = data.getJsonArray("client_data");
                    if (featureDataArray.Length > 0)
                    {
                        for (int i = 0; i < featureDataArray.Length; i++)
                        {
                            unlockData.getFeatureInfoEventDelegate(featureDataArray[i]);
                        }
                    }
                }
            }

        }
    }

    protected override void initializeWithData(JSON data)
    {
        registerServerEvents();
        JSON unlockJson = data.getJSON("eue_unlock_data");
        instance.featureUnlocksDict = new Dictionary<string, FeatureUnlockData>();
        List<string> featureKeys = unlockJson.getKeyList();
        for (int i = 0; i < featureKeys.Count; i++)
        {
            string featureKey = featureKeys[i];
            FeatureUnlockData unlockData = new EueFeatureUnlockData(featureKey, unlockJson.getJSON(featureKey));
            instance.featureUnlocksDict.Add(featureKey, unlockData);
        }
    }
    

    public override bool isEnabled 
    {
        get { return ExperimentWrapper.EUEFeatureUnlocks.isInExperiment && instance != null; }
    }

    public void registerForFeatureUnlockedEvent(string featureKey, FeatureUnlockData.FeatureEventCallback callback)
    {
        //Have buttons register to this to unlock midLobby
        FeatureUnlockData unlockData;
        if (instance.featureUnlocksDict.TryGetValue(featureKey, out unlockData))
        {
            unlockData.featureUnlockedEventDelegate -= callback;
            unlockData.featureUnlockedEventDelegate += callback;
        }
    }
    
    public void unregisterForFeatureUnlockedEvent(string featureKey, FeatureUnlockData.FeatureEventCallback callback)
    {
        FeatureUnlockData unlockData;
        if (instance.featureUnlocksDict.TryGetValue(featureKey, out unlockData))
        {
            unlockData.featureUnlockedEventDelegate -= callback;
        }
    }
    
    public void registerForFeatureLoadEvent(string featureKey, FeatureUnlockData.FeatureEventCallback callback)
    {
        FeatureUnlockData unlockData;
        if (instance.featureUnlocksDict.TryGetValue(featureKey, out unlockData))
        {
            unlockData.featureLoadEventDelegate -= callback;
            unlockData.featureLoadEventDelegate += callback;
        }
    }
    
    public void registerForGetInfoEvent(string featureKey, FeatureUnlockData.FeatureInfoCallback callback)
    {
        //Feature Controllers register to this to get info when feature unlocks
        FeatureUnlockData unlockData;
        if (instance.featureUnlocksDict.TryGetValue(featureKey, out unlockData))
        {
            unlockData.getFeatureInfoEventDelegate -= callback;
            unlockData.getFeatureInfoEventDelegate += callback;
        }
    }
    
    public static bool isFeatureUnlocked(string featureKey)
    {
        if (instance == null || !instance.isEnabled)
        {
            return true;
        }
        
        FeatureUnlockData unlockData;
        if (instance.featureUnlocksDict.TryGetValue(featureKey, out unlockData))
        {
            return unlockData.isFeatureUnlocked();
        }

        return true; //Assume anything feature not specified in the unlock login data isn't locked
    }
    
    public static int getFeatureUnlockLevel(string featureKey)
    {
        if (instance == null || !instance.isEnabled)
        {
            return 0;
        }
        
        FeatureUnlockData unlockData;
        if (instance.featureUnlocksDict.TryGetValue(featureKey, out unlockData))
        {
            EueFeatureUnlockData eueUnlock = unlockData as EueFeatureUnlockData;
            if (eueUnlock != null)
            {
                return eueUnlock.unlockLevel;
            }
        }

        return 0;
    }
    
    public static bool wasFeatureUnlockedThisSession(string featureKey, bool markSeen)
    {
        if (instance == null || !instance.isEnabled)
        {
            return false;
        }
        
        FeatureUnlockData unlockData;
        bool result = false;
        if (instance.featureUnlocksDict.TryGetValue(featureKey, out unlockData))
        {
            result = unlockData.unlockedThisSession;
            if (markSeen)
            {
                unlockData.unlockAnimationSeen = true;
            }
        }

        return result;
    }

    public static string getFeatureUnlockType(string featureKey)
    {
        if (instance == null || !instance.isEnabled)
        {
            return "";
        }
        
        FeatureUnlockData unlockData;
        if (instance.featureUnlocksDict.TryGetValue(featureKey, out unlockData))
        {
            return unlockData.unlockType;
        }

        return "";
    }

    public static void markFeatureSeen(string featureKey)
    {
        if (instance == null || !instance.isEnabled)
        {
            return;
        }
        
        FeatureUnlockData unlockData;
        if (instance.featureUnlocksDict.TryGetValue(featureKey, out unlockData))
        {
            unlockData.featureSeen = true;
            CustomPlayerData.setValue(featureKey + "_feature_seen", true);
        }
    }

    public static bool hasFeatureUnlockData(string featureKey)
    {
        if (instance == null || !instance.isEnabled)
        {
            return false;
        }

        return instance.featureUnlocksDict.ContainsKey(featureKey);
    }

    public static EueFeatureUnlockData getUnlockData(string featureKey)
    {
        if (instance == null || !instance.isEnabled)
        {
            return null;
        }
        
        FeatureUnlockData unlockData;
        if (instance.featureUnlocksDict.TryGetValue(featureKey, out unlockData))
        {
            return unlockData as EueFeatureUnlockData;
        }

        return null;
    }

    public static bool hasFeatureBeenSeen(string featureKey)
    {
        if (instance == null || !instance.isEnabled)
        {
            return true;
        }
        
        FeatureUnlockData unlockData;
        if (instance.featureUnlocksDict.TryGetValue(featureKey, out unlockData))
        {
            return unlockData.featureSeen;
        }

        return true;
    }
}
