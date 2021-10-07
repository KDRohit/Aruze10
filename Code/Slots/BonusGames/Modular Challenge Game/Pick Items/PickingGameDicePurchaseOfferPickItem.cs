using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Class specific to credit reveals on pick items
 */
public class PickingGameDicePurchaseOfferPickItem : PickingGameBasePickItemAccessor
{
    [SerializeField] private LabelWrapperComponent creditsLabel;
    [SerializeField] private LabelWrapperComponent diceLabel;

    private RewardPurchaseOffer currentOffer;

    public void setLabels(RewardPurchaseOffer purchaseOffer)
    {
        int diceAmount = purchaseOffer.boardGameDice;
        creditsLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(purchaseOffer.package.purchasePackage.totalCredits());

        if (diceAmount != 1)
        {
            diceLabel.text = Localize.text("+{0}_roll_plural", diceAmount);
        }
        else
        {
            diceLabel.text = Localize.text("+1_roll");
        }
        currentOffer = purchaseOffer;
    }
    
    public void pickItemPressed(GameObject pickObject)
    {
        if (currentOffer != null)
        {
            currentOffer.package.purchasePackage.makePurchase(purchaseRewardable:currentOffer);
        }

        currentOffer = null;
        basePickItem.pickItemPressed(pickObject);
    }

    public void closeClicked(GameObject pickObject)
    {
        basePickItem.pickItemPressed(pickObject);
        currentOffer = null;
    }
}