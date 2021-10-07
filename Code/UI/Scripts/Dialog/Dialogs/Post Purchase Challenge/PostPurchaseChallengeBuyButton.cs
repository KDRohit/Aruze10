using Com.Scheduler;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PostPurchaseChallengeBuyButton : MonoBehaviour
{
    [SerializeField] private ButtonHandler buyButtonHandler;
    [SerializeField] private PostPurchaseChallengeProgressMeter progressMeter;
    [SerializeField] private TextMeshPro timerLabel;
    [SerializeField] private TextMeshPro dealAmountLabel;
    [SerializeField] private GameObject tooltipParent;
    [SerializeField] private MeshRenderer iconRenderer;
    [SerializeField] private MeshRenderer iconOuterRenderer;
    [SerializeField] private AnimationListController.AnimationInformationList fillAnims;
    [SerializeField] private AnimationListController.AnimationInformationList winAnims;

    private PostPurchaseChallengeCampaign campaign;
    private ObjectSwapper swapper;
    private bool playAnim = false;

    private const string PURCHASED_STATE = "purchased";
    private const string NOT_PURCHASED_STATE = "not_purchased";
    private const string BONUS_LABEL = "{0}% More!";

    public void Awake()
    {
        buyButtonHandler.registerEventDelegate(buyButtonClicked);
    }

    public void OnEnable()
    {
        campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
        if (campaign != null)
        { 
            registerHandlers();
            if (!campaign.isLocked)
            {
                if (campaign.isRunning)
                {
                    campaign.registerRunningTimeLabel(timerLabel, GameTimerRange.TimeFormat.REMAINING_HMS_FORMAT);
                }

                if (campaign.hasReminderEvent)
                {
                    campaign.registerReminderCallback(showTooltip);
                }

                DisplayAsset.loadTextureToRenderer(iconRenderer, PostPurchaseChallengeCampaign.ICON_TOP_BAR_BASE_PATH, isExplicitPath: true);
                DisplayAsset.loadTextureToRenderer(iconOuterRenderer, PostPurchaseChallengeCampaign.ICON_TOP_BAR_BASE_PATH, isExplicitPath: true);
            }
            
            if (dealAmountLabel != null)
            {
                dealAmountLabel.text = string.Format(BONUS_LABEL, campaign.getPostPurchaseChallengeMaxBonus());
            }
        }
        setSwapperState();
    }

    public void OnDestroy()
    {
        unRegisterHandlers();
    }

    private void registerHandlers()
    {
        if (campaign != null)
        {
            campaign.postPurchaseChallengeUnlocked += onPostPurchaseChallengeUnlocked;
            campaign.postPurchaseChallengeProgressUpdated += onPostPurchaseChallengeProgressUpdated;
            campaign.postPurchaseChallengeCompleted += onPostPurchaseChallengeCompleted;
            campaign.postPurchaseChallengeMaxBonusUpdated += onMaxBonusUpdated;
        }

        ChallengeCampaign.onShowCampaignComplete += campaignComplete;
        WatchToEarn.adInitialized += onAdInitialized;
    }
    
    private void unRegisterHandlers()
    {
        if (campaign != null)
        {
            campaign.postPurchaseChallengeUnlocked -= onPostPurchaseChallengeUnlocked;
            campaign.postPurchaseChallengeProgressUpdated -= onPostPurchaseChallengeProgressUpdated;
            campaign.postPurchaseChallengeCompleted -= onPostPurchaseChallengeCompleted;
            campaign.postPurchaseChallengeMaxBonusUpdated -= onMaxBonusUpdated;
        }

        ChallengeCampaign.onShowCampaignComplete -= campaignComplete;
        WatchToEarn.adInitialized -= onAdInitialized;
    }

    private void onPostPurchaseChallengeUnlocked(string experiment)
    {
        if (campaign == null)
        {
            return;
        }

        if (campaign.isRunning)
        {
            campaign.registerRunningTimeLabel(timerLabel, GameTimerRange.TimeFormat.REMAINING_HMS_FORMAT);
        }

        if (campaign.hasReminderEvent)
        {
            campaign.registerReminderCallback(showTooltip);
        }
        progressMeter.updateProgress(campaign);
        string iconPath = string.Format(PostPurchaseChallengeCampaign.ICON_TOP_BAR_BASE_PATH, ExperimentWrapper.PostPurchaseChallenge.theme);
        DisplayAsset.loadTextureToRenderer(iconRenderer, iconPath, isExplicitPath: true);
        DisplayAsset.loadTextureToRenderer(iconOuterRenderer, iconPath, isExplicitPath: true);
        setSwapperState();
    }

    private void onPostPurchaseChallengeCompleted()
    {
        setSwapperState();
    }

    private void onPostPurchaseChallengeProgressUpdated()
    {
        // Make the priority IMMEDIATE, so that the progress bar can be updated properly before regular dialogs
        // when the game reaches the main lobby
        Scheduler.addFunction(updateProgess, priority:SchedulerPriority.PriorityType.IMMEDIATE);
    }

    private void updateProgess(Dict args)
    {
        setSwapperState();

        if (progressMeter != null)
        {
            progressMeter.updateProgress(campaign);

            if (playAnim)
            {
                if (campaign.getCurrentAmount() >= campaign.getTargetAmount())
                {
                    StartCoroutine(AnimationListController.playListOfAnimationInformation(winAnims));
                }
                else
                {
                    StartCoroutine(AnimationListController.playListOfAnimationInformation(fillAnims));
                }
            }
            else
            {
                playAnim = true;
            }
        }
    }
    

    private void onMaxBonusUpdated()
    {
        dealAmountLabel.text = string.Format(BONUS_LABEL, campaign.getPostPurchaseChallengeMaxBonus());
    }

    private void onAdInitialized()
    {
        dealAmountLabel.text = string.Format(BONUS_LABEL, campaign.getPostPurchaseChallengeMaxBonus());
    }

    private void showTooltip(Dict args = null, GameTimerRange originalTimer = null)
    {
        AssetBundleManager.load(PostPurchaseChallengeCampaign.REMINDER_TOOLTIP_PREFAB_PATH, assetLoadSuccess, assetLoadFailed);
    }

    private void assetLoadSuccess(string assetPath, Object obj, Dict data = null)
    {
        GameObject tooltip = NGUITools.AddChild(tooltipParent, obj as GameObject, true);
        if (tooltip != null)
        {
            GiftChestOfferTooltip tooltipComponent = tooltip.GetComponent<GiftChestOfferTooltip>();
            if (tooltipComponent != null)
            {
                tooltipComponent.setLabel("Ending Soon!");
            }
        }
    }
    
    private void assetLoadFailed(string assetPath, Dict data = null)
    {
        Bugsnag.LeaveBreadcrumb("Post Purchase Challenge Themed Asset failed to load: " + assetPath);
#if UNITY_EDITOR
        Debug.LogWarning("Post Purchase Challenge Themed Asset failed to load: " + assetPath);			
#endif
    }

    private void campaignComplete(string campaignId, List<JSON> eventData)
    {
        if (campaignId != CampaignDirector.POST_PURCHASE_CHALLENGE)
        {
            return;
        }
        
        setSwapperState();
    }

    private void setSwapperState()
    {
        if (swapper == null)
        {
            swapper = GetComponent<ObjectSwapper>();
        }
        
        if (swapper != null)
        {
            swapper.setState(getStateForChallenge());
        }
    }

    private string getStateForChallenge()
    {
        if (campaign == null)
        {
            campaign = PostPurchaseChallengeCampaign.getActivePostPurchaseChallengeCampaign();
        }
        
        if (campaign == null || campaign.isLocked || campaign.runningTimeRemaining <= 0)
        {
            return NOT_PURCHASED_STATE;
        }

        return PURCHASED_STATE;
    }

    private void buyButtonClicked(Dict args = null)
    {
        PostPurchaseChallengeDialog.showDialog();
    }
}
