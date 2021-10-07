using System.Collections;
using UnityEngine;

public class PickingGameRevealOnRoundStartBoardGameBuffModule : PickingGameRevealOnStartModule
{
    protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
    {
        // detect pick type & whether to handle with this module
        if (pickData != null && pickData.meterAction == "bg_buff")
        {
            return true;
        }

        return false;
    }
    
    protected override IEnumerator collectItem()
    {
        //Tell board game to update
        yield break;
    }
}
