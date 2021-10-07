using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RichPassPiggyBankInfoDialog : TICoroutineMonoBehaviour
{
    [SerializeField] private LabelWrapperComponent headerLabel;
    [SerializeField] private LabelWrapperComponent tooltipLabel;
    [SerializeField] private LabelWrapperComponent packagePriceLabel;
    [SerializeField] private LabelWrapperComponent bankAmountLabel;

    [SerializeField] private ButtonHandler closeButton;
    [SerializeField] private ButtonHandler lockedButton;
    [SerializeField] private ButtonHandler ctaButton;
    [SerializeField] private ClickHandler openBankButton;

    [SerializeField] private ObjectSwapper buttonSwapper;
    
    [SerializeField] private AnimationListController.AnimationInformationList openBankAnimationList;
    [SerializeField] private AnimatedParticleEffect coinParticle;

    private long rewardAmount = 0;
    private string eventID = "";

    private PIGGY_BANK_STATE currentState = PIGGY_BANK_STATE.LOCKED;

    private RichPassCampaign currentCampaign;
    private SlideController parentSlider;
    private DialogType dialogOpenedFrom;

    private enum PIGGY_BANK_STATE
    {
        LOCKED,
        UNLOCKED,
        TAP_TO_OPEN
    }
    
    private const string RP_UNLOCK_GOLD_LOC = "rp_unlock_gold";
    private const string RP_BANK_INFO_LOCALIZATION = "rp_bank_info_{0}";
    private const string BREAK_BANK_ON_LOCALIZATION = "break_bank_on_{0}";
    private const string COLLECT_BANK_LOCALIZATION = "collect_bank";

    //Used when being opened from the feature dialog to display the current campaign's info
    public void init(SlideController activeSlider, RichPassCampaign campaign, DialogType dialogParent)
    {
        dialogOpenedFrom = dialogParent;
        parentSlider = activeSlider;
        parentSlider.preventScrolling();
        currentCampaign = campaign;
        lockedButton.registerEventDelegate(lockedClicked);
        closeButton.registerEventDelegate(closeClicked);
        
        RichPassPackage goldPackage = campaign.getCurrentPackage();
        string goldPassCost = goldPackage != null && goldPackage.purchasePackage != null ? goldPackage.purchasePackage.getLocalizedPrice() : "";
        packagePriceLabel.text = Localize.text(RP_UNLOCK_GOLD_LOC, goldPassCost);
        
        if (campaign.isPurchased())
        {
            currentState = PIGGY_BANK_STATE.UNLOCKED;
        }
        
        bankAmountLabel.text = CreditsEconomy.convertCredits(campaign.bankCoins);
        tooltipLabel.text = Localize.text(RP_BANK_INFO_LOCALIZATION, CreditsEconomy.multiplyAndFormatNumberTextSuffix(campaign.finalPiggyBankValue, 2, false, false));

        buttonSwapper.setState(currentState.ToString().ToLower());

        switch (currentState)
        {
            case PIGGY_BANK_STATE.LOCKED:
                headerLabel.text = Localize.text(BREAK_BANK_ON_LOCALIZATION, campaign.timerRange.endDate.ToShortDateString());
                break;
            
            case PIGGY_BANK_STATE.UNLOCKED:
                headerLabel.text = Localize.text(BREAK_BANK_ON_LOCALIZATION, campaign.timerRange.endDate.ToShortDateString());
                ctaButton.registerEventDelegate(closeClicked);
                break;
        }
        
        animateInPiggyBank();
    }

    //Used when opened from the reward event to grant the coins to the player for the previous season's bank
    public void init(long bankReward, string id)
    {
        closeButton.gameObject.SetActive(false);
        openBankButton.registerEventDelegate(tapToBreakClicked);
        headerLabel.text = Localize.text(COLLECT_BANK_LOCALIZATION);
        rewardAmount = bankReward;
        bankAmountLabel.text = CreditsEconomy.convertCredits(rewardAmount);
        eventID = id;
        currentState = PIGGY_BANK_STATE.TAP_TO_OPEN;
        bankAmountLabel.text = CreditsEconomy.convertCredits(bankReward);
        buttonSwapper.setState(currentState.ToString().ToLower());
        
        ctaButton.registerEventDelegate(collectClicked);
        ctaButton.text = "Collect";
        Audio.play("PiggyBankRewardIn");
    }

    private void lockedClicked(Dict args = null)
    {
        CampaignDirector.richPass.purchasePackage();
    }
    
    private void closeClicked(Dict args = null)
    {
        if (parentSlider != null)
        {
            parentSlider.enableScrolling();
        }

        if (currentCampaign == null)
        {
            Dialog.close();
        }
        else
        {
            animateOutPiggyBank();
        }
    }
    
    private void tapToBreakClicked(Dict args = null)
    {
        Audio.play("PiggyBankRewardSelect");
        openBankButton.gameObject.SetActive(false);
        StartCoroutine(playOpenBankAnimations());
        if (!string.IsNullOrEmpty(eventID))
        {
            RichPassAction.claimBankReward(eventID);
        }
    }

    private IEnumerator playOpenBankAnimations()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(openBankAnimationList));
        buttonSwapper.setState("open");
    }
    
    private void collectClicked(Dict args = null)
    {
        ctaButton.enabled = false;
        SlotsPlayer.addCredits(rewardAmount, "Rich Pass Piggy Bank");
        Audio.play("CollectCoinsChestRichPass");
        StartCoroutine(playCollectAnimations());
    }

    private IEnumerator playCollectAnimations()
    {
        yield return StartCoroutine(coinParticle.animateParticleEffect());
        closeClicked();
    }
    
    private void animateInPiggyBank()
    {
        Audio.play("PiggyBankRewardIn");
        iTween.EaseType easeType = Dialog.getAnimEaseType(dialogOpenedFrom.getAnimInEase(), true);
        float time = dialogOpenedFrom.getAnimInTime();

        Vector3 currentPosition = transform.localPosition;

        iTween.MoveTo(gameObject, iTween.Hash("position", new Vector3(currentPosition.x, 0, currentPosition.z), "time", time, "islocal", true, "oncompletetarget", gameObject, "easetype", easeType));
    }

    private void animateOutPiggyBank()
    {
        Vector3 moveGoal = Dialog.getAnimPos(dialogOpenedFrom.getAnimOutPos(), gameObject);
        iTween.EaseType easeType = Dialog.getAnimEaseType(dialogOpenedFrom.getAnimOutEase(), false);
        float time = dialogOpenedFrom.getAnimOutTime();
        Vector3 currentPosition = transform.localPosition;

        iTween.MoveTo(gameObject, iTween.Hash("position", new Vector3(currentPosition.x, moveGoal.y, currentPosition.z), "time", time, "islocal", true, "oncompletetarget", gameObject, "easetype", easeType, "oncomplete", "tweenOutComplete"));
    }

    private void tweenOutComplete()
    {
        Destroy(gameObject);
    }
}
