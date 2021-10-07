using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickingGameRevealOnRoundStartMultipliedCreditsModule : PickingGameRevealOnStartModule
{ 
    protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
    {
        // detect pick type & whether to handle with this module
        if (pickData != null && pickData.credits > 0 && pickData.multiplier > 0)
        {
            return true;
        }

        return false;
    }

    protected override IEnumerator autoRevealPick(PickingGameBasePickItem revealedItem)
    {
        ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();

        long creditsAmount = currentPick.credits * currentPick.multiplier;
        PickingGameCreditPickItem creditsRevealItem = PickingGameCreditPickItem.getPickingGameCreditsPickItemForType(revealedItem.gameObject, PickingGameCreditPickItem.CreditsPickItemType.Multiplier);
        creditsRevealItem.setCreditLabels(creditsAmount);

        revealType = PickingGameRevealSpecificLabelRandomTextPickItem.PickItemRevealType.Credits;
        yield return StartCoroutine(base.autoRevealPick(revealedItem));
    }

    protected override IEnumerator collectItem()
    {
        ModularChallengeGameOutcomeEntry currentPick = pickingVariantParent.getCurrentPickOutcome();
        yield return StartCoroutine(base.rollupCredits(currentPick.credits * currentPick.multiplier));
    }
}