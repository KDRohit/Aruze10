using System.Collections;
using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using UnityEngine;

public class PurchasePerksIconPetTreat : PurchasePerksIcon
{
    [SerializeField] private LabelWrapperComponent timeLabel;
    [SerializeField] private UISprite iconSprite;

    private const string DEFAULT_ICON = "Perk Icon Pets Treat 01";
    private const string LOC_KEY = "pet_treat_perk_icon";
    public override void init(CreditPackage package, int index, bool isPurchased, RewardPurchaseOffer offer)
    {
        base.init(package, index, isPurchased, offer);
        VirtualPetTreat treat = null;
        if (VirtualPetsFeature.instance != null)
        {
            treat = VirtualPetsFeature.instance.getTreatTypeForPackage(package.purchasePackage);
        }

        if (treat != null)
        {
            if (timeLabel != null)
            {
                timeLabel.text = Localize.text(LOC_KEY, treat.hyperStateSeconds / Common.SECONDS_PER_MINUTE);
                iconDimmer.cacheTextColors(timeLabel.tmProLabel);
            }

            iconSprite.spriteName = treat.iconSpriteName;
        }
    }
}
