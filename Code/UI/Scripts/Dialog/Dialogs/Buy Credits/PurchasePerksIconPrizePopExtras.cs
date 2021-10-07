using System.Collections;
using System.Collections.Generic;
using PrizePop;
using UnityEngine;

public class PurchasePerksIconPrizePopExtras : PurchasePerksIcon
{
    [SerializeField] private LabelWrapperComponent extrasLabel;
    public override void init(CreditPackage package, int index, bool isPurchased, RewardPurchaseOffer offer)
    {
        base.init(package, index, isPurchased, offer);
        extrasLabel.text = string.Format("<#ffe96f>Extra Chance</color>\n+{0}",CommonText.formatNumber(PrizePopFeature.instance.currentPackagePicks));
    }
}
