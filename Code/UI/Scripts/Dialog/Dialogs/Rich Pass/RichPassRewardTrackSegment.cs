using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RichPassRewardTrackSegment : MonoBehaviour
{
    [SerializeField] private LabelWrapperComponent pointsLabel;

    [SerializeField] private RichPassRewardContainer goldPrizeContainer;
    [SerializeField] private RichPassRewardContainer silverPrizeContainer;
    [SerializeField] private GameObject logoSprite; // Filled/Activated when the required progress is reached

    [SerializeField] private UIMeterNGUI progressBar;
    
    private long requiredPointsToUnlock = 0;
    private SlideController parentSlideController;
    public bool hasGoldRewards { get; protected set; }

    private const string REWARD_ITEM_PATH = "Features/Rich Pass/Prefabs/Instanced Prefabs/Path Assets/Award Items/Challenge Award {0} item";

    public virtual void init(long requiredPoints, long currentPlayerPoints, long nextRewardPoints, RichPassCampaign.RewardTrack silverTrack, List<PassReward> silverRewards, RichPassCampaign.RewardTrack goldTrack, List<PassReward> goldRewards, SlideController slideController)
    {
        parentSlideController = slideController;
        requiredPointsToUnlock = requiredPoints;
        if (requiredPointsToUnlock > 0 && pointsLabel != null)
        {
            pointsLabel.text = CommonText.formatNumber(requiredPointsToUnlock);
        }

        fillInProgressMeter(requiredPointsToUnlock, currentPlayerPoints, nextRewardPoints);
        spawnIcons(requiredPointsToUnlock, silverTrack, silverRewards, goldTrack, goldRewards);
    }

    private void spawnIcons(long requiredPoints, RichPassCampaign.RewardTrack silverTrack, List<PassReward> currNodeSilverRewards, RichPassCampaign.RewardTrack goldTrack, List<PassReward> currNodeGoldRewards)
    {
        currNodeSilverRewards = silverTrack.getSingleRewardsList(requiredPoints);
        if (currNodeSilverRewards != null && currNodeSilverRewards.Count > 0)
        {
            loadPrizeIcons(silverPrizeContainer, currNodeSilverRewards, silverTrack);
        }
        
        currNodeGoldRewards = goldTrack.getSingleRewardsList(requiredPoints);
        if (currNodeGoldRewards != null && currNodeGoldRewards.Count > 0)
        {
            hasGoldRewards = true;
            loadPrizeIcons(goldPrizeContainer, currNodeGoldRewards, goldTrack);
        }
    }

    private void fillInProgressMeter(long requiredPoints, long currentPlayerPoints, long nextRewardPoints)
    {
        SafeSet.gameObjectActive(logoSprite, currentPlayerPoints >= requiredPoints);
        
        //Set the meter length
        if (progressBar != null)
        {
            long pointsDifference = nextRewardPoints - requiredPoints;
            long progressInCurrentSegment = (long)Mathf.Max(currentPlayerPoints - requiredPoints, 0);
            progressBar.setState(progressInCurrentSegment, pointsDifference);
        }
    }

    private void loadPrizeIcons(RichPassRewardContainer prizeParent, List<PassReward> rewards, RichPassCampaign.RewardTrack rewardTrack)
    {
        for (int i = 0; i < rewards.Count; i++)
        {
            string awardTypePrefabName = null;

            switch (rewards[i].type)
            {
                case ChallengeReward.RewardType.CREDITS:
                    awardTypePrefabName = "credits";
                    break;

                case ChallengeReward.RewardType.CHEST:
                    awardTypePrefabName = "chest " + rewards[i].rarity;
                    break;

                case ChallengeReward.RewardType.LOOT_BOX:
                    awardTypePrefabName = "chest " + rewards[i].image;
                    break;

                case ChallengeReward.RewardType.CARD_PACKS:
                    if (!Collectables.isActive())
                    {
                        return; //Don't show the collections icon if the feature isn't active
                    }

                    awardTypePrefabName = "collections card pack";
                    break;
                
                case ChallengeReward.RewardType.BASE_BANK:
                case ChallengeReward.RewardType.BANK_MULTIPLIER:
                    awardTypePrefabName = "piggy bank multiplier";
                    break;
                
                case ChallengeReward.RewardType.POWER_UPS:
                    if (!Collectables.isActive() || !PowerupsManager.isPowerupsEnabled)
                    {
                        return; //Don't show the collections icon if the feature isn't active
                    }

                    awardTypePrefabName = "powerups streak";
                    break;
                
                case ChallengeReward.RewardType.ELITE_POINTS:
                    if (!EliteManager.isActive)
                    {
                        return;//Don't show the elite points icon if the feature isn't active
                    }

                    awardTypePrefabName = "elite points";
                    break;
                
                default:
                    Debug.LogError("Unsupported rich pass reward: " + rewards[i].type.ToString());
                    return;
            }
            
            string rewardPrefabPath = string.Format(REWARD_ITEM_PATH, awardTypePrefabName);
            AssetBundleManager.load(this, rewardPrefabPath, iconLoadSuccess, iconLoadFailed, Dict.create(D.OBJECT, prizeParent, D.DATA, rewards[i], D.TYPE, rewardTrack), isSkippingMapping:true, fileExtension:".prefab");
        }
    }
    
    private void iconLoadSuccess(string assetPath, Object obj, Dict data = null)
    {
        RichPassRewardContainer rewardContainer = (RichPassRewardContainer) data.getWithDefault(D.OBJECT, null);
        PassReward reward = (PassReward) data.getWithDefault(D.DATA, null);
        RichPassCampaign.RewardTrack rewardTrack = (RichPassCampaign.RewardTrack) data.getWithDefault(D.TYPE, null);
        if (rewardContainer == null || reward == null)
        {
            return;
        }
        rewardContainer.gameObject.SetActive(true);
        GameObject rewardIconObj = CommonGameObject.instantiate(obj, rewardContainer.transform) as GameObject;
        RichPassRewardIcon rewardIcon = rewardIconObj.GetComponent<RichPassRewardIcon>();
        rewardIcon.init(reward, rewardTrack, parentSlideController);
        rewardContainer.init(reward, requiredPointsToUnlock, rewardTrack, rewardIcon);
    }

    public virtual void unlockGoldRewards()
    {
        if (CampaignDirector.richPass.pointsAcquired >= requiredPointsToUnlock)
        {
            goldPrizeContainer.unlockReward();
        }
    }

    private void iconLoadFailed(string assetPath, Dict data = null)
    {
        Debug.LogWarningFormat("RichPassIcon {0} failed to load", assetPath);
    }
}
