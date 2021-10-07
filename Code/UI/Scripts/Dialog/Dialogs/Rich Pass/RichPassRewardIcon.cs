using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RichPassRewardIcon : TICoroutineMonoBehaviour
{
    [SerializeField] private string claimSilverClickedSound;
    [SerializeField] private string claimGoldClickedSound;
    [SerializeField] protected UITexture iconTexture;
    [SerializeField] private AnimationListController.AnimationInformationList alreadyClaimedAnimationList;

    protected PassReward reward;
    protected RichPassCampaign.RewardTrack rewardTier;
    
    protected SlideController parentSlideController;
    public virtual void init(PassReward rewardToAward, RichPassCampaign.RewardTrack tier, SlideController slideController)
    {
        reward = rewardToAward;
        rewardTier = tier;
        parentSlideController = slideController;

        if (rewardToAward != null && rewardToAward.claimed)
        {
            iconTexture.color = Color.grey;
            if (alreadyClaimedAnimationList != null && alreadyClaimedAnimationList.Count > 0)
            {
                StartCoroutine(AnimationListController.playListOfAnimationInformation(alreadyClaimedAnimationList));
            }
        }
    }
    
    //Animations/Effects that play when the collect button is clicked and before we receive the server event
    public virtual IEnumerator playAnticipations()
    {
        if (rewardTier.name == "gold")
        {
            Audio.play(claimGoldClickedSound);
        }
        else
        {
            Audio.play(claimSilverClickedSound);
        }

        if (iconTexture != null)
        {
            iconTexture.color = Color.grey;
        }

        yield break;
    }
    
    //Animations/Effects that play when the server response is received for claiming the reward
    public virtual void rewardClaimSuccess(JSON data)
    {
        
    }

    public virtual void rewardClaimFailed()
    {
        
    }
    
    public virtual void packDropRecieved(JSON data)
    {
        if (Collectables.isActive())
        {
            Collectables.Instance.unRegisterForPackDrop(packDropRecieved, "rich_pass");
        }
        Collectables.claimPackDropNow(data, Com.Scheduler.SchedulerPriority.PriorityType.IMMEDIATE);
    }
}
