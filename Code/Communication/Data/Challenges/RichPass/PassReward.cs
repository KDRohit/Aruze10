using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassReward : ChallengeReward
{
    public int id { get; private set; }
    public bool unlocked { get; private set; }
    public bool claimed { get; private set; }
    public string rarity { get; private set; }
    public string image { get; private set; }
    public string cardPackKey { get; private set; }

    private long quantity;
    
    public PassReward(bool isUnlocked, JSON data = null) : base(data)
    {
        unlocked = isUnlocked;
    }

    public virtual void unlock()
    {
        unlocked = true;
    }

    public virtual void Claim()
    {
        claimed = true;
    }
    
    public override void reset()
    {
        unlocked = false;
        claimed = false;
    }
    
    protected override void parse(JSON data)
    {
        id = data.getInt("id", 0);
        type = getTypeFromString(data.getString("type", string.Empty));
        amount = data.getLong("value", 0);
        claimed = data.getBool("is_claimed", false);
        unlocked = data.getBool("is_unlocked", false);
        rarity = data.getString("rarity", "");
        image = data.getString("image", "common");
        cardPackKey = data.getString("value", "");
    }

    public bool isAutoClaimable()
    {
        return !claimed &&
               unlocked &&
               (type == ChallengeReward.RewardType.BASE_BANK || type == ChallengeReward.RewardType.BANK_MULTIPLIER);
    }
}
