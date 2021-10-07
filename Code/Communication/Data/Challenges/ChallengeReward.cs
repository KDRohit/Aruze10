using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ChallengeReward
{
	private delegate void awardFunc(long amount, string reason);
	
	public enum RewardType
	{
		NONE,
		GAME_UNLOCK,
		PASS_POINTS,
		VIP_POINTS,
		CREDITS,
		LEVELS,
		XP,
		SLOTVENTURE_CARD_PACK,
		CARD_PACKS,
		CHEST,
		BASE_BANK,
		BANK_MULTIPLIER,
		POWER_UPS,
		ELITE_POINTS,
		LOOT_BOX
	};
	
    public RewardType type { get; protected set; }

    private static readonly Dictionary<RewardType, awardFunc> collectFunctions;
    private static readonly Dictionary<string, RewardType> typeMap;

    static ChallengeReward()
    {
	    collectFunctions = new Dictionary<RewardType, awardFunc>()
	    {
		    { RewardType.XP, awardXP },
		    { RewardType.VIP_POINTS, awardVIPPoints },
		    { RewardType.PASS_POINTS, awardRichPassPoints },
		    { RewardType.CREDITS, awardCredits }
	    };
	    
	    typeMap = new Dictionary<string, RewardType>()
	    {
		    { "game_unlock", RewardType.GAME_UNLOCK },
		    { "pass_points", RewardType.PASS_POINTS },
		    { "vip_point", RewardType.VIP_POINTS },
		    { "coins", RewardType.CREDITS },
		    { "coin", RewardType.CREDITS },
		    { "credits", RewardType.CREDITS },
		    { "xp", RewardType.XP },
		    { "level", RewardType.LEVELS },
		    { "slotventures_card_packs", RewardType.SLOTVENTURE_CARD_PACK },
		    { "card_packs", RewardType.CARD_PACKS },
		    { "chest", RewardType.CHEST },
		    { "base_bank_coins", RewardType.BASE_BANK },
		    { "bank_coins_multiplier", RewardType.BANK_MULTIPLIER },
		    { "random_powerups", RewardType.POWER_UPS },
		    { "elite_point", RewardType.ELITE_POINTS },
		    { "loot_box", RewardType.LOOT_BOX }
		};
    }

	public ChallengeReward(JSON data = null)
	{
		if (data != null)
		{
			parse(data);
		}
	}

	protected RewardType getTypeFromString(string data)
	{
		if (typeMap.ContainsKey(data) && typeMap[data] != null)
		{
			return typeMap[data];
		}

		Debug.LogWarning("Invalid reward type: " + data);
		return RewardType.NONE;
	}

	public virtual void reset()
	{
		
	}

	public long amount { get; protected set; }
	protected abstract void parse(JSON data);
	
	public virtual void addReplayModifier(float ratio)
	{
		if (ratio > 0)
		{
			long baseAmount = amount;
			amount = baseAmount + System.Convert.ToInt64(baseAmount * ratio);
		}
	}

	public void collect(string rewardSource)
	{
		if (collectFunctions.ContainsKey(type) && collectFunctions[type] != null)
		{
			collectFunctions[type].Invoke(amount, rewardSource);
		}
	}

	private static void awardCredits(long amount, string reason)
	{
		SlotsPlayer.addFeatureCredits(amount, reason);
	}

	private static void awardXP(long amount, string reason)
	{
		SlotsPlayer.instance.xp.add(amount, reason + "xp");
	}

	private static void awardVIPPoints(long amount, string reason)
	{
		SlotsPlayer.instance.addVIPPoints(amount);
	}

	private static void awardRichPassPoints(long amount, string reason)
	{
		if (CampaignDirector.richPass != null)
		{
			CampaignDirector.richPass.incrementPoints(amount);	
		}
	}
	

	public virtual bool isCardPackReward()
	{
		return type == RewardType.CARD_PACKS || type == RewardType.SLOTVENTURE_CARD_PACK || type == RewardType.POWER_UPS;
	}
}
