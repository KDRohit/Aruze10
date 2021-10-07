using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelLottoTripleXPBuff : PowerupBase
{
	private static int multiplier;
	private const int MULTIPLER_AMOUNT = 1;

	protected override void init(JSON data = null)
	{
		base.init(data);
		rarity = Rarity.COMMON;
		name = LEVEL_LOTTO_TRIPLE_XP_KEY;
		uiPrefabName = "";
		isDisplayablePowerup = false;
		multiplier = MULTIPLER_AMOUNT;

		if (data != null)
		{
			// we subtract one because clients legacy xp event modifier is additive. meaning triple xp is multiplier 2
			// and double xp is 1. base is 0
			multiplier = data.getInt("value", MULTIPLER_AMOUNT) - 1;
		}
	}

	public override void apply(int totalTime, int durationRemaining)
	{
		base.apply(totalTime, durationRemaining);

		XPMultiplierEvent.instance.onPowerupEnabled(multiplier, durationRemaining,"LevelLottoTripleXPBuff");
	}

	public virtual void remove(Dict args = null, GameTimerRange sender = null)
	{
		base.remove(args, sender);

		if (!PowerupsManager.hasActivePowerupByName(name))
		{
			XPMultiplierEvent.instance.onPowerupDisabled(multiplier);
		}
	}
}
