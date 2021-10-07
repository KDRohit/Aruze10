using UnityEngine;
using Zap.Automation;
using System.Collections.Generic;

public class TripleXPPowerUp : PowerupBase
{
    private static int multiplier;
    public const int MULTIPLER_AMOUNT = 2;

    protected override void init(JSON data = null)
    {
        base.init(data);
        rarity = Rarity.COMMON;
        aliasKeys = new List<string>
        {
            "triplexp_"
        };
        name = POWER_UP_TRIPLE_XP_KEY;
        uiPrefabName = "PowerUp Icon Triple XP Item";
        
        multiplier = MULTIPLER_AMOUNT;

        if (data != null)
        {
            // we subtract one because clients legacy xp event modifier is additive. meaning triple xp is multiplier 2
            // and double xp is 1. base is 0
            multiplier = data.getInt("value", MULTIPLER_AMOUNT + 1) - 1;
        }
    }
    
    public override void onPowerupCreate(string key)
    {
        if(key != POWER_UP_TRIPLE_XP_KEY)
        {
           isDisplayablePowerup = false;
        }
    }
    public override void apply(int totalTime, int durationRemaining)
    {
        base.apply(totalTime, durationRemaining);

        XPMultiplierEvent.instance.onPowerupEnabled(multiplier, durationRemaining,"TripleXPPowerUp");
    }

    public override void remove(Dict args = null, GameTimerRange sender = null)
    {
        base.remove(args, sender);

        if (!PowerupsManager.hasActivePowerupByName(name))
        {
            XPMultiplierEvent.instance.onPowerupDisabled(multiplier);
        }
    }
}