using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using Zap.Automation;

public class FreeSpinPowerup : PowerupBase
{
    protected override void init(JSON data = null)
    {
        base.init(data);
        rarity = Rarity.UNCOMMON;
        name = POWER_UP_FREE_SPINS_KEY;
        uiPrefabName = "PowerUp Icon Free Spin Gift Item";
        actionName = "collect_now";
    }

    public override void doAction()
    {
        base.doAction();
        if (!Scheduler.hasTaskOfType<InboxTask>() && Dialog.instance.findOpenDialogOfType("inbox") == null)
        {
            Scheduler.addTask(new InboxTask(Dict.create(D.KEY, InboxDialog.SPINS_STATE, D.DATA, SchedulerPriority.PriorityType.IMMEDIATE)), SchedulerPriority.PriorityType.IMMEDIATE);
        }
    }
}