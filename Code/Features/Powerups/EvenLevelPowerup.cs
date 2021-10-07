using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zap.Automation;

public class EvenLevelPowerup : PowerupBase
{
    protected override void init(JSON data = null)
    {
        base.init(data);
        rarity = Rarity.COMMON;
        name = POWER_UP_EVEN_LEVELS_KEY;
        uiPrefabName = "PowerUp Icon Level Up Even Item";
    }

    public override void apply(int totalTime, int durationRemaining)
    {
        base.apply(totalTime, durationRemaining);

        LevelUpBonus.setLevelPattern("EVEN");

        if (PowerupsManager.getActivePowerup(POWER_UP_ODD_LEVELS_KEY) != null)
        {
            if (durationRemaining > PowerupsManager.getActivePowerup(POWER_UP_ODD_LEVELS_KEY).runningTimer.timeRemaining)
            {
                LevelUpBonus.setLevelPattern("ODD");
            }
        }
    }
}