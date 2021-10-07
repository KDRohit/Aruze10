using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RichPassRewardIconCoins : RichPassRewardIcon
{
    [SerializeField] private AnimationListController.AnimationInformationList coinClaimedAnimation;
    [SerializeField] private LabelWrapperComponent amountLabel;
    
    public override void init(PassReward rewardToAward, RichPassCampaign.RewardTrack tier, SlideController parentSlideController)
    {
        base.init(rewardToAward, tier, parentSlideController);
        amountLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(rewardToAward.amount, 2, shouldRoundUp:false);
    }

    public override void rewardClaimSuccess(JSON data)
    {
        StartCoroutine(AnimationListController.playListOfAnimationInformation(coinClaimedAnimation));
    }
}
