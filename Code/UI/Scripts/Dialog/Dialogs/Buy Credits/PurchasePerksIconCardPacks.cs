using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchasePerksIconCardPacks : PurchasePerksIcon
{
    [SerializeField] private CollectablePack cardPack;

    public override void init(CreditPackage package, int index, bool isPurchased, RewardPurchaseOffer offer)
    {
        if (!string.IsNullOrEmpty(package.collectableDropKeyName))
        {
            cardPack.init(package.collectableDropKeyName);
        }
        else if (!string.IsNullOrEmpty(offer.cardPackKey))
        {
            cardPack.init(offer.cardPackKey);
        }
        else
        {
            return;
        }
        iconDimmer.cacheTextColors(cardPack.minCardsLabel);
    }
}
