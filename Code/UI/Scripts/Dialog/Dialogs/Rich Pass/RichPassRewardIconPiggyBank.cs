using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RichPassRewardIconPiggyBank : RichPassRewardIcon
{
    [SerializeField] private LabelWrapperComponent amountLabel;
    [SerializeField] private AnimationListController.AnimationInformationList rewardClaimAnimations;

    public override void init(PassReward rewardToAward, RichPassCampaign.RewardTrack tier,
        SlideController parentSlideController)
    {
        base.init(rewardToAward, tier, parentSlideController);
        long amount = rewardToAward.amount;
        if (rewardToAward.type == ChallengeReward.RewardType.BASE_BANK)
        {
            amountLabel.gameObject.SetActive(false);
        }
        else
        {
            amountLabel.text = string.Format("{0}X", CommonText.formatNumber(amount));
        }
    }

    public override void rewardClaimSuccess(JSON data)
    {
        StartCoroutine(playRewardSequence());
    }

    private IEnumerator playRewardSequence()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rewardClaimAnimations));
        iconTexture.color = Color.grey;
        RichPassFeatureDialog.instance.claimPiggyBankAward(reward);
    }
}
