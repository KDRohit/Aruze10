using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zap.Automation;

public class VIPBoostPowerup : PowerupBase
{
    public int boostAmount = 1;
    
    protected override void init(JSON data = null)
    {
        base.init(data);
        rarity = Rarity.VERY_RARE;
        name = POWER_UP_VIP_BOOSTS_KEY;
        uiPrefabName = "PowerUp Icon VIP Boost Item";
        actionName = "buy_now";
    }

    public override void doAction()
    {
        base.doAction();
        OverlayTopHIRv2 overlay = OverlayTopHIRv2.instance as OverlayTopHIRv2;
        if (overlay != null)
        {
            overlay.clickBuyCredits();
        }
    }
}