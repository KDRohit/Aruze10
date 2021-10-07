using System.Collections;
using Com.Rewardables;
using Com.Scheduler;
using UnityEngine;

public class RichPassRewardContainer : MonoBehaviour, IResetGame
{
    [SerializeField] private ObjectSwapper iconStateSwapper;
    [SerializeField] private ButtonHandler collectButton;
    [SerializeField] private ButtonHandler lockedButton;
    
    [SerializeField] private AnimationListController.AnimationInformationList alreadyClaimedAnimations;
    [SerializeField] private AnimationListController.AnimationInformationList claimAnimations;


    private const string CLAIMED = "collected"; //Already been claimed
    private const string LOCKED = "locked"; //Points requirement not met
    private const string UNLOCKED = "unlocked"; //All requirements met, can be claimed
    private const string NON_CLAIMABLE = "non_claimable"; //Unlocked but can't be claimed 

    private PassReward myReward;
    private RichPassCampaign.RewardTrack rewardTier;
    private long requiredPoints = 0;
    private RichPassRewardIcon spawnedIcon;

    private static bool waitingForReward = false;

    public void init(PassReward reward, long requiredPointsToUnlock, RichPassCampaign.RewardTrack rewardTrack, RichPassRewardIcon rewardIcon)
    {
        spawnedIcon = rewardIcon;
        myReward = reward;
        rewardTier = rewardTrack;
        requiredPoints = requiredPointsToUnlock;

        if (myReward == null)
        {
            iconStateSwapper.setState(LOCKED);
        }
        else if (myReward.claimed)
        {
            iconStateSwapper.setState(CLAIMED);
            StartCoroutine(AnimationListController.playListOfAnimationInformation(alreadyClaimedAnimations));
        }
        else if (isItemUnlocked(requiredPointsToUnlock))
        {
            if (isItemClaimable())
            {
                collectButton.registerEventDelegate(collectClicked);
                iconStateSwapper.setState(UNLOCKED);
            }
            else
            {
                lockedButton.registerEventDelegate(lockClicked);
                iconStateSwapper.setState(NON_CLAIMABLE);
            }
        }
        else
        {
            iconStateSwapper.setState(LOCKED);
        }

        if (isItemAutoClaimable())
        {
            autoClaimReward();
        }
    }

    private void autoClaimReward()
    {
        iconStateSwapper.setState(CLAIMED);
        myReward.Claim(); //Auto-Claimed items are already claimed server side, so we're just marking them as claimed on the client once we had the chance to play their animations
        spawnedIcon.rewardClaimSuccess(null);
    }

    public bool isItemAutoClaimable()
    {
        return myReward != null &&
               myReward.isAutoClaimable();
    }

    private bool isItemUnlocked(long requiredPointsToUnlock)
    {
        return myReward.unlocked || CampaignDirector.richPass.pointsAcquired >= requiredPointsToUnlock;
    }

    private bool isItemClaimable()
    {
        if (rewardTier.name == "gold" && CampaignDirector.richPass.passType == "silver") //Will probably want some other way to rank passTypes if we have more than just silver/gold
        {
            return false; //Gold rewards are always locked if we're in the silver tier
        }

        return true;
    }


    private void collectClicked(Dict args = null)
    {
        if (!waitingForReward)
        {
            waitingForReward = true;
            collectButton.unregisterEventDelegate(collectClicked);

            RewardablesManager.addEventHandler(onRichPassAwardDataReceived);
            RewardablesManager.addFailEventHandler(rewardClaimFailed);

            if (Collectables.isActive() && myReward.isCardPackReward())
            {
                Collectables.Instance.registerForPackDrop(spawnedIcon.packDropRecieved, "rich_pass");
            }
            
            rewardTier.claimReward(myReward.id, requiredPoints); //Send up the server event saying we want to claim this. Might need to lock other claim buttons while waiting for response, or handle que of reward sequences

            StartCoroutine(playClaimedAnimation());
        }
    }

    private IEnumerator playClaimedAnimation()
    { 
        iconStateSwapper.setState(CLAIMED);
        if (claimAnimations != null && claimAnimations.Count > 0)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(claimAnimations));
        }

        yield return StartCoroutine(spawnedIcon.playAnticipations());
    }

    public void unlockReward()
    {
        if (!myReward.claimed)
        {
            collectButton.registerEventDelegate(collectClicked);
            iconStateSwapper.setState(UNLOCKED);
        }
    }
    
    private void lockClicked(Dict args = null)
    {
        Audio.play("LockedCollectRichPass");
        RichPassUpgradeToGoldDialog.showDialog("collect_button", SchedulerPriority.PriorityType.IMMEDIATE);
    }

    private void onRichPassAwardDataReceived(Rewardable rewardable)
    {
        RewardRichPass richPassReward = rewardable as RewardRichPass;

        if (richPassReward == null)
        {
            return;
        }

        RewardablesManager.removeEventHandler(
            onRichPassAwardDataReceived); //Need to unregister in case we're collecting multiple awards in a single dialog view
        RewardablesManager.removeFailEventHandler(rewardClaimFailed);
        
        // loot box will be handled differently by LootBoxFeature
        if (string.IsNullOrEmpty(richPassReward.feature) ||
            !richPassReward.feature.Equals(LootBoxFeature.LOOT_BOX, System.StringComparison.InvariantCultureIgnoreCase))
        {
            spawnedIcon.rewardClaimSuccess(richPassReward.data);
        }
        waitingForReward = false;
    }

    private void rewardClaimFailed()
    {
        RewardablesManager.removeFailEventHandler(rewardClaimFailed);
        RewardablesManager.removeEventHandler(onRichPassAwardDataReceived); //Need to unregister in case we're collecting multiple awards in a single dialog view

        if (Collectables.isActive())
        {
            Collectables.Instance.unRegisterForPackDrop(spawnedIcon.packDropRecieved, "rich_pass");
        }
        spawnedIcon.rewardClaimFailed();
        waitingForReward = false;
    }
    


    private void OnDestroy()
    {
        RewardablesManager.removeEventHandler(onRichPassAwardDataReceived);
        RewardablesManager.removeFailEventHandler(rewardClaimFailed);

        if (Collectables.isActive())
        {
            Collectables.Instance.unRegisterForPackDrop(spawnedIcon.packDropRecieved, "rich_pass");
        }
    }
    
    public static void resetStaticClassData()
    {
        waitingForReward = false;
    }
}
