using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using UnityEngine;


public class FeatureUnlockData
{
    public string unlockType { get; private set; }
    public int loadLevel { get; private set; }
    public bool unlockedThisSession{ get; private set; }
    public bool unlockAnimationSeen;
    public bool featureSeen;
    public FeatureEventCallback featureUnlockedEventDelegate;
    public FeatureEventCallback featureLoadEventDelegate; 
    public FeatureInfoCallback getFeatureInfoEventDelegate;
    protected string featureName { get; private set; }
        
    public delegate void FeatureEventCallback();
    public delegate void FeatureInfoCallback(JSON data);

    public FeatureUnlockData(string keyName, string featureUnlockType)
    {
        featureName = keyName;
        unlockType = featureUnlockType;
        loadLevel = -1;
        unlockedThisSession = false;
        unlockAnimationSeen = true;
        //Default to true so existing players don't see features they've already unlocked as new
        //Won't affect players that haven't unlocked the feature since "new" badge only appears once its unlocked
        featureSeen = CustomPlayerData.getBool(featureName + "_feature_seen", true);

        switch (unlockType)
        {
            case "pet_ftue":
                if (VirtualPetsFeature.instance != null)
                {
                    VirtualPetsFeature.instance.registerForFtueSeenEvent(onFeatureUnlocked);
                }
                break;
        }
    }

    public FeatureUnlockData(string keyName, string featureUnlockType, int featureLoadLevel)
    {
        featureName = keyName;
        unlockType = featureUnlockType;
        loadLevel = featureLoadLevel;
        unlockedThisSession = false;
        unlockAnimationSeen = true;
        
        //Default to true so existing players don't see features they've already unlocked as new
        //Won't affect players that haven't unlocked the feature since "new" badge only appears once its unlocked
        featureSeen = CustomPlayerData.getBool(featureName + "_feature_seen", true);
            
        if (loadLevel > SlotsPlayer.instance.socialMember.experienceLevel)
        {
            LevelUpDialog.registerForLevelUpEvent(loadLevel, onFeatureLoadLevelReached);
        }
    }

    public virtual bool isFeatureUnlocked()
    {
        switch (unlockType)
        {
            case "pet_ftue":
                return VirtualPetsFeature.instance != null && VirtualPetsFeature.instance.ftueSeen;
        }

        return false;
    }

    private void onFeatureUnlocked()
    {
        onFeatureUnlocked(-1);
    }
    
    protected virtual void onFeatureUnlocked(int level)
    {
        unlockedThisSession = true;
        unlockAnimationSeen = false;
        CustomPlayerData.setValue(featureName + "_feature_seen", false); //Mark feature as not seen only once its unlocked. Prevents feature from appearing new to existing players
        featureSeen = false;

        if (featureUnlockedEventDelegate != null)
        {
            featureUnlockedEventDelegate();
        }

        logFeatureUnlocked();
    }

    private void onFeatureLoadLevelReached(int level)
    {
        if (featureLoadEventDelegate != null)
        {
            featureLoadEventDelegate();
        }
    }

    protected virtual void logFeatureUnlocked()
    {
        StatsManager.Instance.LogCount
        (
            counterName: "feature_unlock",
            kingdom: featureName
        );
    }
}
