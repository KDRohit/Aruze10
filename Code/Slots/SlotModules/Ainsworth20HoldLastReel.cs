using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ainsworth20HoldLastReel : SlotModule
{
    public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
    {
        return true;
    }
    public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
    {
        SlotReel reel = reelGame.engine.getSlotReelAt(4);
        
        if (reel.reelData.reelStrip.keyName == "ainsworth20_reelstrip_fs2_5")
        {
             reel.isLocked = true;
         }
      }
}
