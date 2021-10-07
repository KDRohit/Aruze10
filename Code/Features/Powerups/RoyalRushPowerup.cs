using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zap.Automation;

public class RoyalRushPowerup : PowerupBase
{
    protected override void init(JSON data = null)
    {
        base.init(data);
        rarity = Rarity.UNCOMMON;
        name = POWER_UP_ROYAL_RUSH_KEY;
        uiPrefabName = "PowerUp Icon Royal Rush Points Item";
        actionName = "play_now";
    }

    public override void doAction()
    {
        base.doAction();
        List<RoyalRushInfo> rushInfo = RoyalRushEvent.instance.rushInfoList;
        if (rushInfo == null || rushInfo.Count == 0)
        {
            return;
        }

        RoyalRushInfo info = rushInfo[0];
        LobbyGame game = LobbyGame.find(info.gameKey);
        if (game != null && (GameState.game == null || GameState.game != game))
        {
            SlotAction.setLaunchDetails("powerup");
            game.askInitialBetOrTryLaunch(false, true);
        }
    }

    public override bool canPerformAction
    {
        get
        {
            List<RoyalRushInfo> rushInfo = RoyalRushEvent.instance.rushInfoList;
            return rushInfo != null && rushInfo.Count > 0;
        }
    }
}