using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Module to handle increasing available picks when revealing a specific symbol that
also iterates through each round to set the variant index.*/
public class PickingGameIncreasePicksModuleWithLinkedRounds : PickingGameIncreasePicksModule
{
    public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
    {
        return true;
    }

    public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
    {
        //Let get our linked round item for this pick
        PickingGameLinkedRoundVariantPickItem linkedRoundVariantPickItem = pickItem.GetComponent<PickingGameLinkedRoundVariantPickItem>();
        if (linkedRoundVariantPickItem == null)
        {
            Debug.LogError("Please make sure the pick items for this round include PickingGameLinkedRoundVariantPickItem or this module won't work as intented.");
            //Early out
            return base.executeOnItemClick(pickItem);
        }

        //We want to iterate though the rounds and set the variant index to the value of the variant we want.
        foreach (ModularChallengeGameRound roundCollection in roundVariantParent.gameParent.pickingRounds)
        {
            roundCollection.variantIndex = System.Array.IndexOf(roundCollection.roundVariants, linkedRoundVariantPickItem.linkedRound);
        }

        return base.executeOnItemClick(pickItem);
    }
}