using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiftChestOfferExperiment : EosExperiment
{
	public int cooldown { get; private set; }
	public string package { get; private set; }
	public int bonusPercent { get; private set; }
	public int maxViews { get; private set; }

	public GiftChestOfferExperiment(string name) : base(name)
	{

	}
	protected override void init(JSON data)
	{
		cooldown = getEosVarWithDefault(data, "animation_cooldown", int.MaxValue);
		package = getEosVarWithDefault(data, "coin_package", "coin_package_1");
		bonusPercent = getEosVarWithDefault(data, "bonus_pct", 0);
		maxViews = getEosVarWithDefault(data, "max_views", 0);

	}

	public override void reset()
	{
		cooldown = int.MaxValue;
	}
}
