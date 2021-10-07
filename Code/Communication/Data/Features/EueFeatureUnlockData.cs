using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Controls what features players should be seeing even when locked.
 * Currently EUE features can be XP level & VIP level locked
 * eue_feature_unlocks EOS experiment must be enabled for this data to be received
 */
public class EueFeatureUnlockData : FeatureUnlockData
{
    public int unlockLevel { get; private set; }
    
    public EueFeatureUnlockData(string keyName, JSON data) : 
        base(keyName,
            data.getString("unlock_type", ""),
            data.getInt("lazy_load_value", 0))
    {
        unlockLevel = data.getInt("unlock_value", 0);
        if (unlockType == "level")
        {
            if (unlockLevel > SlotsPlayer.instance.socialMember.experienceLevel)
            {
                LevelUpDialog.registerForLevelUpEvent(unlockLevel, onFeatureUnlocked);
            }
        }
        else if (unlockType == "vip_level")
        {
            if (unlockLevel > SlotsPlayer.instance.adjustedVipLevel)
            {
                LevelUpDialog.registerForVIPLevelUpEvent(unlockLevel, onFeatureUnlocked);
            }
        }
    }

    public override bool isFeatureUnlocked()
    {
        switch (unlockType)
        {
            case "level":
                return unlockLevel <= SlotsPlayer.instance.socialMember.experienceLevel;
            case "vip_level":
                return unlockLevel <= SlotsPlayer.instance.adjustedVipLevel;
            default:
                return base.isFeatureUnlocked();
        }
    }

    protected override void onFeatureUnlocked(int level)
    {
        base.onFeatureUnlocked(level);
        FeatureUnlockAction.getFeatureInfo(featureName);
    }
    
    protected override void logFeatureUnlocked()
    {
        StatsManager.Instance.LogCount
        (
            counterName: "feature_unlock",
            kingdom: featureName,
            val: unlockLevel
        );
    }
}
