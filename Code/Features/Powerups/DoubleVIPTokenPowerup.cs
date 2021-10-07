using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zap.Automation;

public class DoubleVIPTokenPowerup : PowerupBase
{
    protected override void init(JSON data = null)
    {
        base.init(data);
        rarity = Rarity.RARE;
        name = POWER_UP_DOUBLE_VIP_KEY;
        uiPrefabName = "PowerUp Icon VIP Room Token Item";
        actionName = "play_now";
    }

    public override void doAction()
    {
        base.doAction();
        LobbyInfo.Type targetLobby = LobbyInfo.Type.VIP;
        if (LobbyLoader.lastLobby == targetLobby && GameState.isMainLobby)
        {
            return;
        }
        
        Overlay.instance.hideShroud();

        // Since this can be called from the canvas, we need to do some validation that it's ok to do something.
        if (!GameState.isMainLobby)
        {
            GameState.pop();
        }
        
        NGUIExt.disableAllMouseInput();
        Loading.show(Loading.LoadingTransactionTarget.LOBBY);
        LobbyLoader.lastLobby = targetLobby;
        //This can be accessed through the game, so it is following the same pattern as existing from the game to go to the main lobby.
        Glb.loadLobby();

        // HIR-6846.  If the slot music is faded out, then stop it
        // to make sure it doesn't play for a second on the way back to the lobby.
        if (Audio.currentMusicPlayer != null && Audio.currentMusicPlayer.isPlaying)
        {
            Audio.switchMusicKeyImmediate("");
        }
		
        Audio.stopAll();
        Audio.removeDelays();
        Audio.listenerVolume = Audio.maxGlobalVolume;
        Audio.play("return_to_lobby");
    }
}