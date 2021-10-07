using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using Zap.Automation;

public class BuyPageBonusPowerup : PowerupBase
{
    public const int SALE_PERCENT = 15;

    protected static int saleOverride;

    protected override void init(JSON data = null)
    {
        base.init(data);
        rarity = Rarity.UNCOMMON;
        name = POWER_UP_BUY_PAGE_KEY;
        uiPrefabName = "PowerUp Icon Coin Purchase Deal Item";
        actionName = "buy_now";

        if (data != null && data.getInt("value", 0) > 0)
        {
            saleOverride = (int)(1.0f - data.getFloat("value", 1.15f) * 100.0f);
        }
    }

    public override void doAction()
    {
        base.doAction();
        OverlayTopHIRv2 overlay = OverlayTopHIRv2.instance as OverlayTopHIRv2;
        if (overlay != null && !Scheduler.hasTaskWith("buy_credits_v5"))
        {
            overlay.clickBuyCredits();
        }
    }

    public static int salePercent
    {
        get { return saleOverride > 0 ? saleOverride : SALE_PERCENT; }
    }
}