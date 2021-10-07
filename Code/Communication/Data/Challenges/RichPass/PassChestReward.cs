using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassChestReward : PassReward
{
    public class ChestItem
    {
        public string type;
        public long value;
        public ChestItem(string itemType, long itemValue)
        {
            type = itemType;
            value = itemValue;
        }
    }
    
    public PassChestReward(bool isUnlocked, JSON data = null) : base(isUnlocked, data)
    {
    }

    public List<ChestItem> rewards;
    
    protected override void parse(JSON data)
    {
        base.parse(data);
        JSON[] chestData = data.getJsonArray("rewards");
        if (chestData != null && chestData.Length > 0)
        {
            rewards = new List<ChestItem>(chestData.Length);
            for (int i = 0; i < chestData.Length; i++)
            {
                if (chestData[i] == null)
                {
                    Debug.LogWarning("Invalid chest data");
                    continue;
                }

                string itemType = chestData[i].getString("type", "");
                if (string.IsNullOrEmpty(itemType))
                {
                    Debug.LogError("Invalid item type");
                    continue;
                }
                
                long amount = chestData[i].getLong("value", 0);
                rewards.Add(new ChestItem(itemType, amount));
            }
        }
    }

    public override void addReplayModifier(float ratio)
    {
        //apply modifier to each item
        if (rewards == null)
        {
            return;
        }

        for (int i = 0; i < rewards.Count; i++)
        {
            rewards[i].value += System.Convert.ToInt64(rewards[i].value * ratio);
        }
    }

    public override bool isCardPackReward()
    {
        for (int i = 0; i < rewards.Count; i++)
        {
            RewardType chestRewardType = getTypeFromString(rewards[i].type);
            if (chestRewardType == RewardType.CARD_PACKS ||
                chestRewardType == RewardType.SLOTVENTURE_CARD_PACK ||
                chestRewardType == RewardType.POWER_UPS)
            {
                return true;
            }
        }

        return false;
    }
}
