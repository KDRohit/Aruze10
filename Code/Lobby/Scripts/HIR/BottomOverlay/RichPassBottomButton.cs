
using Com.Rewardables;
using Com.Scheduler;
using UnityEngine;

public class RichPassBottomButton : BottomOverlayButton
{
    [SerializeField] private GameObject unclaimedAwardsBadge;
    [SerializeField] private LabelWrapperComponent awardsAvailableLabel;
    [SerializeField] private ObjectSwapper swapper;

    protected override void Awake()
    {
        base.Awake();
        if (ExperimentWrapper.EUEFeatureUnlocks.isInExperiment)
        {
            sortIndex = 1;
        }
        else
        {
            sortIndex = !EliteManager.isActive ? 1 : 2;
        }
        
        init();
    }
    
    protected override void init()
    {
        base.init();
        if (RichPassCampaign.isLevelLocked())
        {
            initLevelLock(false);
        }
        else
        {
            if (needsToShowUnlockAnimation())
            {
                showUnlockAnimation();
            }
            else if (needsToForceShowFeature())
            {
                onClick(Dict.create(D.OPTION, true));
            }
            else
            {
                toolTipController.toggleNewBadge(!hasViewedFeature);
            }

            if (CampaignDirector.richPass == null || !CampaignDirector.richPass.isEnabled)
            {
                toolTipController.setLockedText(BottomOverlayButtonToolTipController.COMING_SOON_LOC_KEY);
            }
            else
            {
                initNewRewardsAlert();
                initPassType(); 
                RewardablesManager.addEventHandler(onRewardReceived);  
            }
        }
    }

    private void onRewardReceived(Rewardable rewardable)
    {
        RewardRichPass richPassReward = rewardable as RewardRichPass;

        if (richPassReward != null)
        {
            initNewRewardsAlert();
        }
    }


    private void initPassType()
    {
        if (CampaignDirector.richPass == null || !CampaignDirector.richPass.isActive)
        {
            return;
        }

        setPassType(CampaignDirector.richPass.passType);
        CampaignDirector.richPass.onPassTypeChanged += setPassType;
    }

    private void setPassType(string newType)
    {
        switch (newType)
        {
            case "gold":
                swapper.setState("gold");
                break;

            default:
                swapper.setState("silver");
                break;

        }
    }
    protected override void onClick(Dict args = null)
    {
        if (RichPassCampaign.isLevelLocked())
        {
            logLockedClick();
            StartCoroutine(toolTipController.playLockedTooltip());
        }
        else if (CampaignDirector.richPass == null || !CampaignDirector.richPass.isEnabled)
        {
            logComingSoonClick();
            StartCoroutine(toolTipController.playLockedTooltip());
        }
        else
        {
            if (!hasViewedFeature)
            {
                logFirstTimeFeatureEntry(args);
                CampaignDirector.richPass.showVideo();
                markFeatureSeen();
            }
            
            StatsRichPass.logLobbyIconClick();
            showLoadingTooltip("rich_pass_dialog");
            RichPassFeatureDialog.showDialog(CampaignDirector.richPass);
        }
    }
    
    public void initNewRewardsAlert()
    {
        if (null != CampaignDirector.richPass && CampaignDirector.richPass.isActive)
        {
            if (!hasViewedFeature)
            {
                toolTipController.toggleNewBadge(!needsToShowUnlockAnimation() && !playingUnlockAnimation);
                unclaimedAwardsBadge.SetActive(false);
                return;
            }
            
            int unclaimedRewardCount = CampaignDirector.richPass.getNumberOfUnclaimedRewards();
            if (unclaimedRewardCount > 0)
            {
                unclaimedAwardsBadge.SetActive(true);
                awardsAvailableLabel.text = CommonText.formatNumber(unclaimedRewardCount);
            }
            else
            {
                unclaimedAwardsBadge.SetActive(false);
            }
            
        }
        else
        {
            unclaimedAwardsBadge.SetActive(false);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (CampaignDirector.richPass != null)
        {
            CampaignDirector.richPass.onPassTypeChanged -= setPassType;    
        }
        RewardablesManager.removeEventHandler(onRewardReceived);
        
        if (MainLobbyBottomOverlayV4.instance != null)
        {
            MainLobbyBottomOverlayV4.instance.shouldRepositionGrid = true;
        }
    }

    protected override bool needsToForceShowFeature()
    {
        return base.needsToForceShowFeature() && CampaignDirector.richPass != null && CampaignDirector.richPass.isEnabled;
    }
}
