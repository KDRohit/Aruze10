using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickingGameJackpotIgnoreHighestCreditsModule : PickingGameJackpotModule
{
    
    //Similar check to PickingGameJackpotModule but ignores the check to see if the pickData's credits is equal to the
    //value of the largest credit revels
    //
    //Used in pick games where the reveal object might have the same value as the highest possible reveal, but isn't actually a jackpot
    protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
    {
        if (pickData != null &&
            (pickData.pickemPick == null || pickData.pickemPick.isJackpot) &&
            (pickData.corePickData == null || pickData.corePickData.isJackpot)
            )
        {
            return true;
        }

        return false;
    }
}
