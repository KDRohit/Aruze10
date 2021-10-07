using System.Collections;
using System.Collections.Generic;
using Com.Rewardables;
using UnityEngine;

public class PrizePopDialogOverlayEventEnded : PrizePopDialogOverlay
{
    private const string EVENT_ENDED_AUDIO_KEY = "EventEndedPrizePopCommon";
    public override void init(Rewardable reward, DialogBase parent, Dict overlayArgs)
    {
        base.init(reward, parent, overlayArgs);
        Audio.play(EVENT_ENDED_AUDIO_KEY);
    }
    
    protected override void ctaClicked(Dict args = null)
    {
        StatsPrizePop.logOverlayClose(overlayType);
        Dialog.close();
    }
}
