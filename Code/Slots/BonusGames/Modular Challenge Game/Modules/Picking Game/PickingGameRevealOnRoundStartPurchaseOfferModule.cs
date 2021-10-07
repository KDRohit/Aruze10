using System.Collections;
using UnityEngine;

public class PickingGameRevealOnRoundStartPurchaseOfferModule : PickingGameRevealOnStartModule
{
    private RewardPurchaseOffer revealedOffer = null;
    private PickingGameSecondaryButtonPickItem currentPickItemCloseButton;
    protected override bool shouldHandleOutcomeEntry(ModularChallengeGameOutcomeEntry pickData)
    {
        // detect pick type & whether to handle with this module
        if (pickData != null && pickData.meterAction == "bg_purchase")
        {
            return true;
        }

        return false;
    }

    protected override IEnumerator autoRevealPick(PickingGameBasePickItem revealedItem)
    {
        ModularChallengeGameOutcomeEntry currentEntry = pickingVariantParent.getCurrentPickOutcome();

        if (currentEntry.rewardables != null)
        {
            for (int i = 0; i < currentEntry.rewardables.Count; i++)
            {
                if (currentEntry.rewardables[i] is RewardPurchaseOffer purchaseReward)
                {
                    revealedOffer = purchaseReward;
                    PickingGameDicePurchaseOfferPickItem purchaseOfferPickItem = revealedItem.GetComponent<PickingGameDicePurchaseOfferPickItem>();
                    purchaseOfferPickItem.setLabels(purchaseReward);
                    break;
                }
            }
        }

        yield return StartCoroutine(base.autoRevealPick(revealedItem));
        
        currentPickItemCloseButton = revealedItem.GetComponent<PickingGameSecondaryButtonPickItem>();
        if (currentPickItemCloseButton != null)
        {
            yield return StartCoroutine(currentPickItemCloseButton.setClickableCoroutine(true));
        }
        revealedOffer = null;
    }

    protected override IEnumerator collectItem()
    {
        if (currentPickItemCloseButton != null)
        {
            yield return StartCoroutine(currentPickItemCloseButton.setClickableCoroutine(false));
        }
    }

    protected override string collectButtonText()
    {
        string price = "";
        if (revealedOffer != null && revealedOffer.package.purchasePackage != null)
        {
            price = revealedOffer.package.purchasePackage.priceLocalized;
        }

        return Localize.text(buttonTextLocalization, price);
    }
}