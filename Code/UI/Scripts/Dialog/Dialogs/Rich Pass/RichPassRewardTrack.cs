using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
using UnityEngine;

public class RichPassRewardTrack : MonoBehaviour
{
    [SerializeField] private UIGrid rewardTrackGrid;
    [SerializeField] private RichPassRewardTrackSegment[] meterSegments; //Should be the prefabs for the 3 meter portions (start, middle end). Middle parts will be duplicated as needed
    public SlideController rewardsSlideController;

    private RichPassRewardTrackSegment[] allTrackSegments;

    private const int MIN_REWARDS_SCROLLING = 2;
    
    public void init(long currentPointsAmount, List<long> rewardAmounts, RichPassCampaign.RewardTrack silverTrack, RichPassCampaign.RewardTrack goldTrack)
    {
        allTrackSegments = new RichPassRewardTrackSegment[rewardAmounts.Count];
        int lastCompletedIndex = 0;
        int autoClaimRewardIndex = 0;
        for (int i = 0; i < rewardAmounts.Count; i++)
        {
            if (rewardAmounts[i] <= currentPointsAmount)
            {
                lastCompletedIndex = i;
            }
            
            long nextTierPoints = i < rewardAmounts.Count - 1 ? rewardAmounts[i + 1] : 0;
            GameObject spawnedTrackObject = getSegmentPrefab(i);
            spawnedTrackObject.transform.SetSiblingIndex(i);
            RichPassRewardTrackSegment spawnedTrackSegment = spawnedTrackObject.GetComponent<RichPassRewardTrackSegment>();
            
            List<PassReward> silverRewards = silverTrack.getSingleRewardsList(rewardAmounts[i]);
            List<PassReward> goldRewards = goldTrack.getSingleRewardsList(rewardAmounts[i]);

            if (hasAutoClaimableAward(silverRewards, goldRewards))
            {
                autoClaimRewardIndex = i;
            }
            
            spawnedTrackSegment.init(rewardAmounts[i], currentPointsAmount, nextTierPoints, silverTrack, silverRewards, goldTrack, goldRewards, rewardsSlideController);

            allTrackSegments[i] = spawnedTrackSegment;
        }
        
        rewardTrackGrid.Reposition();
        
        rewardsSlideController.setBounds(-rewardTrackGrid.cellWidth * Mathf.Max(0,rewardAmounts.Count-MIN_REWARDS_SCROLLING), 0);

        int startingIndex = autoClaimRewardIndex > 0 ? autoClaimRewardIndex : lastCompletedIndex; //Center the track either on the reward being autoclaimed or the last completed reward
        if (startingIndex > 0)
        {
            rewardsSlideController.safleySetXLocation(-rewardTrackGrid.cellWidth * (startingIndex - 1)); //Set the position of the reward track so the current completed node is centered
        }
    }

    private GameObject getSegmentPrefab(int index)
    {
        if (index == 0) //Starting Node Prefab
        {
            return CommonGameObject.instantiate(meterSegments[0].gameObject, rewardTrackGrid.gameObject.transform) as GameObject;
        }
        if (index == allTrackSegments.Length-1) //End Node Prefab
        {
            return CommonGameObject.instantiate(meterSegments[2].gameObject, rewardTrackGrid.gameObject.transform) as GameObject;
        }

        return CommonGameObject.instantiate(meterSegments[1].gameObject, rewardTrackGrid.gameObject.transform) as GameObject; //Treat everything else as a middle piece
    }

    public void unlockGoldRewards()
    {
        for (int i = 0; i < allTrackSegments.Length; i++)
        {
            if (allTrackSegments[i].hasGoldRewards)
            {
                allTrackSegments[i].unlockGoldRewards();
            }
        }
    }
    
    private bool hasAutoClaimableAward(List<PassReward> silverRewards, List<PassReward> goldRewards)
    {
        if (silverRewards != null)
        {
            for (int i = 0; i < silverRewards.Count; i++)
            {
                if (silverRewards[i].isAutoClaimable())
                {
                    return true;
                }
            }
        }

        if (goldRewards != null)
        {
            for (int i = 0; i < goldRewards.Count; i++)
            {
                if (goldRewards[i].isAutoClaimable())
                {
                    return true;
                }
            }
        }

        return false;
    }
}
