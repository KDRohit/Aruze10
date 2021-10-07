using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * PlayCustomAmbientAnimationOnReelsStoppedAfterSpecificSymbolLandsModule.cs
 * Author: Joel Gallant
 * Plays a custom animation list on spin ending when a target symbol appears on any reel.
 * Used originally for aruze03 lock anticipation animations. */

public class PlayCustomAmbientAnimationOnReelsStoppedAfterSpecificSymbolLandsModule : PlayCustomAmbientAnimationOnReelsStoppedModule
{
    [SerializeField] private string targetSymbol = "";

    public override bool needsToExecuteOnReelsStoppedCallback()
    {
        foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
        {
            foreach (SlotSymbol symbol in reel.visibleSymbols)
            {
                if (symbol.serverName == targetSymbol)
                {
                    // reel has a symbol we care about in it
                    return true;
                }
            }
        }
        
        return false;
    }

}
