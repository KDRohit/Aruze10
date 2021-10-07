using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchasePerksIconDice : PurchasePerksIcon
{
    [SerializeField] private LabelWrapperComponent rollsLabel;

    private const string LOC_KEY = "bg_dice_perk_icon";
    public override void init(CreditPackage package, int index, bool isPurchased, RewardPurchaseOffer offer)
    {
        int diceAmount = offer != null ? offer.boardGameDice : 0;
        rollsLabel.text = Localize.text(LOC_KEY,CommonText.formatNumber(diceAmount));
    }
}
