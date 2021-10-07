using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using Zap.Automation;

public class DailyBonusCollectPowerup : PowerupBase
{
    protected override void init(JSON data = null)
    {
        base.init(data);
        rarity = Rarity.UNCOMMON;
        name = POWER_UP_DAILY_BONUS_KEY;
        uiPrefabName = "PowerUp Icon Daily Bonus Item";
        actionName = "collect_now";
    }

    public override void onActivated()
    {
      SlotsPlayer.instance.dailyBonusTimer.startTimer(0);
    }

    public override void doAction()
    {
        base.doAction();

        // This is the timestamp of 24 hours into the future when the super streak will expire if not renewed.
       PlayerPrefsCache.SetInt(Prefs.SUPER_STREAK_EXPIRATION_TIME, GameTimer.currentTime + Common.SECONDS_PER_DAY);

        if (SlotsPlayer.instance.dailyBonusTimer.day > 7)
        {
            // We now just randomly pick a day for the user to pick.
            CreditAction.claimTimerCredits(UnityEngine.Random.Range(1,7), ExperimentWrapper.NewDailyBonus.bonusKeyName);// * (7-1) + 1
        }
        else
        {
            CreditAction.claimTimerCredits(-1, ExperimentWrapper.NewDailyBonus.bonusKeyName);
        }
    }

    public override bool canPerformAction
    {
        get { return SlotsPlayer.instance.dailyBonusTimer.isExpired; }
    }
}