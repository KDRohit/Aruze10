using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zap.Automation;

public class WeeklyRacePowerup : PowerupBase
{
    protected override void init(JSON data = null)
    {
        base.init(data);
        rarity = Rarity.RARE;
        name = POWER_UP_WEEKLY_RACE_KEY;
        uiPrefabName = "PowerUp Icon Weekly Race Points Item";
        actionName = "play_now";
    }

    public override void doAction()
    {
        base.doAction();
        WeeklyRaceLeaderboard.showDialog(Dict.create(D.OBJECT, WeeklyRaceDirector.currentRace));
    }
}