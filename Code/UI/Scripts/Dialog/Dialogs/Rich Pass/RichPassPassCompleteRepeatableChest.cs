using System.Collections.Generic;
using Com.Rewardables;
using UnityEngine;

public class RichPassPassCompleteRepeatableChest : RichPassPassCompleteBase
{
    [SerializeField] private LabelWrapperComponent pointsLabel;
    [SerializeField] private UIMeterNGUI meterSprite;
    [SerializeField] private RichPassRewardContainer repeatRewardContainer;
    [SerializeField] private RichPassRewardIconChest chestIcon;
    [SerializeField] private UITexture chestTexture;

    private long currentPointsRequirement = 0;
    private PassReward currentReward;
    private RichPassCampaign rpCampaign;
    private RichPassRewardTrackSegmentEnd currentSegmentEnd;

    private const string CHEST_TEXTURE_PATH = "Features/Rich Pass/Textures/Award Item Assets/Instanced/Award Type Prize Chest {0} 00 Image";

    public override void init(SlideController parentSlider, RichPassRewardTrackSegmentEnd segmentEnd)
    {
        base.init(parentSlider, segmentEnd);
        rpCampaign = CampaignDirector.richPass;
        currentSegmentEnd = segmentEnd;
        currentPointsRequirement = rpCampaign.getCurrentRepeatableChestRequirement();
        setupChest();
        chestIcon.init(currentReward, rpCampaign.goldTrack, parentSlider);
        DisplayAsset.loadTextureToUITexture(chestTexture, string.Format(CHEST_TEXTURE_PATH, getChestRarity()), isExplicitPath:true, skipBundleMapping:true, pathExtension:".png");
    }

    private string getChestRarity()
    {
        if (currentReward != null)
        {
            return currentReward.rarity;
        }

        //If we don't have data for the current reward, default the rarity to the first repeat reward which should be the same for all the repeat chests
        long firstChestRequirement = rpCampaign.maximumPointsRequired + rpCampaign.repeatableRewardsPointsRequired;
        List<PassReward> rewardsList = rpCampaign.goldTrack.getSingleRewardsList(firstChestRequirement);
        if (rewardsList != null)
        {
            for (int i = 0; i < rewardsList.Count; i++)
            {
                if (rewardsList[i].type == ChallengeReward.RewardType.CHEST)
                {
                    return rewardsList[i].rarity;
                }
            }
        }

        return "common";
    }

    private void onRichPassAwardDataReceived(Rewardable rewardable)
    {
        RewardRichPass richPassReward = rewardable as RewardRichPass;
        RewardablesManager.removeEventHandler(onRichPassAwardDataReceived);
        if (richPassReward != null && richPassReward.isRepeatable)
        {
            //If we just received the last chest, load the generic pass complete animation
            if (currentPointsRequirement == rpCampaign.maximumPointsRequired + rpCampaign.maxRepeatableRewards * rpCampaign.repeatableRewardsPointsRequired)
            {
                currentSegmentEnd.loadPassCompleteObject();
                repeatRewardContainer.gameObject.SetActive(false);
            }
            else
            {
                currentPointsRequirement += rpCampaign.repeatableRewardsPointsRequired;
                setupChest();
            }
        }
    }

    private void rewardClaimFailed()
    {
        RewardablesManager.removeEventHandler(onRichPassAwardDataReceived);
        RewardablesManager.removeFailEventHandler(rewardClaimFailed);
    }

    private void setupChest()
    {
        currentReward = null;
        if (currentPointsRequirement > 0)
        {
            List<PassReward> rewardsList = rpCampaign.goldTrack.getSingleRewardsList(currentPointsRequirement);
            if (rewardsList != null)
            {
                for (int i = 0; i < rewardsList.Count; i++)
                {
                    if (!rewardsList[i].claimed)
                    {
                        currentReward = rewardsList[i];
                        break;
                    }
                }
            }

            long prevRequirement = currentPointsRequirement - CampaignDirector.richPass.repeatableRewardsPointsRequired;
            meterSprite.setState(rpCampaign.pointsAcquired-prevRequirement, CampaignDirector.richPass.repeatableRewardsPointsRequired);
            pointsLabel.text = CommonText.formatNumber(currentPointsRequirement);

            repeatRewardContainer.init(currentReward, currentPointsRequirement, CampaignDirector.richPass.goldTrack, chestIcon);
            RewardablesManager.addEventHandler(onRichPassAwardDataReceived);
            RewardablesManager.addFailEventHandler(rewardClaimFailed);
        }
        else
        {
            if (gameObject != null)
            {
                Destroy(gameObject); //Turn ourselves off if for some reason we don't have a valid chest amount
            }
        }
    }

    public override void unlock()
    {
        if (rpCampaign.pointsAcquired >= currentPointsRequirement)
        {
            repeatRewardContainer.unlockReward();
        }
    }

    public void OnDestroy()
    {
        RewardablesManager.removeEventHandler(onRichPassAwardDataReceived);
        RewardablesManager.removeFailEventHandler(rewardClaimFailed);
    }
}
