using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RichPassRewardIconPowerUps : RichPassRewardIcon
{
    [SerializeField] private AnimationListController.AnimationInformationList alreadyClaimedAnimations;
    
    public override void init(PassReward rewardToAward, RichPassCampaign.RewardTrack tier, SlideController parentSlideController)
    {
        base.init(rewardToAward, tier, parentSlideController);
        if (rewardToAward.claimed)
        {
            StartCoroutine(AnimationListController.playListOfAnimationInformation(alreadyClaimedAnimations));
        }
    }
}
