using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchasePerksIconVipPoints : PurchasePerksIcon
{
    [SerializeField] private LabelWrapperComponent vipPointsLabel;

    private const string TEXT_LOCALIZATION = "vip_purchase_perk";
    public override void init(CreditPackage package, int index, bool isPurchased, RewardPurchaseOffer offer)
    {
        vipPointsLabel.text = Localize.text(TEXT_LOCALIZATION, CommonText.formatNumber(package.purchasePackage.vipPoints()));
        iconDimmer.cacheTextColors(vipPointsLabel.tmProLabel);
    }
}
