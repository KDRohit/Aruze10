using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zap.Automation;

public class DoubleBigWinsPowerup : PowerupBase
{
    protected override void init(JSON data = null)
    {
        base.init(data);
        rarity = Rarity.EPIC;
        name = POWER_UP_BIG_WINS_KEY;
        uiPrefabName = "PowerUp Icon Double Big Wins Item";
    }
}