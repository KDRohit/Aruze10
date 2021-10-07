using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostPurchaseChallengeExperiment : EosExperiment
{
	public string theme { get; private set; }
	public string bannerInactivePath { get; private set; }
	public string bannerActivePath { get; private set; }
	public int[] purchaseIndexBonusAmounts { get; private set; }

	public PostPurchaseChallengeExperiment(string name) : base(name)
	{
	}

	protected override void init(JSON data)
	{
		theme = getEosVarWithDefault(data, "theme", "");
		bannerInactivePath = getEosVarWithDefault(data, "banner_inactive_path", "");
		bannerActivePath = getEosVarWithDefault(data, "banner_active_path", "");
		purchaseIndexBonusAmounts = new int[6];
		purchaseIndexBonusAmounts[0] = getEosVarWithDefault(data, "purchase_index_1_reward_scalar", 0);
		purchaseIndexBonusAmounts[1] = getEosVarWithDefault(data, "purchase_index_2_reward_scalar", 0);
		purchaseIndexBonusAmounts[2]= getEosVarWithDefault(data, "purchase_index_3_reward_scalar", 0);
		purchaseIndexBonusAmounts[3]= getEosVarWithDefault(data, "purchase_index_4_reward_scalar", 0);
		purchaseIndexBonusAmounts[4]= getEosVarWithDefault(data, "purchase_index_5_reward_scalar", 0);
		purchaseIndexBonusAmounts[5]= getEosVarWithDefault(data, "purchase_index_6_reward_scalar", 0);
	}

	public override void reset()
	{
		theme = "";
		bannerInactivePath = "";
		bannerActivePath = "";
		purchaseIndexBonusAmounts = null;
	}
}
