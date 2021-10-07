using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PurchasePerksIconPostPurchaseChallenge : PurchasePerksIcon
{
    [SerializeField] private LabelWrapperComponent descriptionLabel;

    private const string RESET_TIMER_LOC = "post_purchase_challenge_perk_reset";
    private const string NOT_ACTIVE_LOC = "post_purchase_challenge_perk_not_active";
    private const string PURCHASED_LOC = "post_purchase_challenge_{0}_perk_purchased";

    public override void init(CreditPackage package, int index, bool isPurchase, RewardPurchaseOffer offer)
    {
        PostPurchaseChallengeCampaign campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
        if (campaign == null)
        {
            return;
        }

        if (isPurchase)
        {
            string theme = ExperimentWrapper.PostPurchaseChallenge.theme.ToLower();
            descriptionLabel.text = Localize.text(string.Format(PURCHASED_LOC, theme));
        }
        else if (campaign.isLocked)
        {
            descriptionLabel.text = Localize.text(NOT_ACTIVE_LOC, campaign.getPostPurchaseChallengeBonus(index));
        }
        else
        {
            descriptionLabel.text = Localize.text(RESET_TIMER_LOC);
        }
        
        iconDimmer.cacheTextColors(descriptionLabel.tmProLabel);
    }
}
