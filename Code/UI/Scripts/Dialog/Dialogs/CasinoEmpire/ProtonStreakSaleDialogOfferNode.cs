using System.Collections;
using System.Collections.Generic;
using Com.Rewardables;
using FeatureOrchestrator;
using UnityEngine;

public class ProtonStreakSaleDialogOfferNode : TICoroutineMonoBehaviour
{
    [SerializeField] private LabelWrapperComponent coinsLabel;
    [SerializeField] private LabelWrapperComponent diceLabel;
    [SerializeField] private LabelWrapperComponent priceLabel;
    [SerializeField] private Transform iconParent;
    [SerializeField] private AdjustObjectColorsByFactor dimmer;
    [SerializeField] private ButtonHandler purchaseButton;
    [SerializeField] private string PURCHASE_CLICK_AUDIO = "";
    [SerializeField] private PurchasePerksPanel perksPanel;

    [SerializeField] private AnimationListController.AnimationInformationList lockedIntroAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList activeIntroAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList completedIntroAnimList;

    [SerializeField] private AnimationListController.AnimationInformationList unlockAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList completeAnimList;

    [SerializeField] private ObjectSwapper frameSwapper;
    [SerializeField] private ObjectSwapper panelSwapper;

    private AdjustObjectColorsByFactor extraIconDimmer;
    private RewardPurchaseOffer offer;
    private CreditPackage package;
    private State currentState;
    private int index;
    private List<PurchasePerksPanel.PerkType> perksToCycle;
    private PurchasePerksCycler perksCycler;
    private string statKingdom = "";
    private string statPhylum = "";

    private const string FRAME_STATE_NAME = "frame_type_";
    private const string PANEL_STATE_NAME = "style_";
    
    private enum State
    {
        COMPLETE,
        LOCKED,
        ACTIVE
    }

    public void init(CreditPackage package, RewardPurchaseOffer protonRewardable, int nodeIndex, int currentIndex, List<PurchasePerksPanel.PerkType> cyclingPerks, PurchasePerksCycler cycler, string kingdom, string phylum, string extraIconPrefabPath = "")
    {
        index = nodeIndex;
        frameSwapper.setState(FRAME_STATE_NAME+index);
        panelSwapper.setState(PANEL_STATE_NAME+index);
        this.package = package;
        offer = protonRewardable.getClone();
        offer.cardPackKey = package.collectableDropKeyName;
        perksToCycle = cyclingPerks;
        perksCycler = cycler;
        statKingdom = kingdom;
        statPhylum = phylum;
        setState(currentIndex, true);

        if (!string.IsNullOrEmpty(extraIconPrefabPath))
        {
            AssetBundleManager.load(this, extraIconPrefabPath+nodeIndex, extraIconLoadSuccess, extraIconLoadFailed, isSkippingMapping:true, fileExtension:".prefab");
        }
    }

    private void setState(int currentIndex, bool init)
    {
        if (index == currentIndex)
        {
            currentState = State.ACTIVE;
        }
        else if (index < currentIndex)
        {
            currentState = State.COMPLETE;
        }
        else
        {
            currentState = State.LOCKED;
        }

        switch (currentState)
        {
            case State.COMPLETE:
            case State.ACTIVE:
                coinsLabel.gameObject.SetActive(true);
                perksPanel.gameObject.SetActive(true);
                if (offer != null)
                {
                    coinsLabel.text = CreditsEconomy.convertCredits(package.purchasePackage.totalCredits(package.bonus));
                    diceLabel.gameObject.SetActive(offer.boardGameDice > 0);
                    if (offer.boardGameDice > 0)
                    {
                        diceLabel.text = string.Format("{0} Dice Rolls +", CommonText.formatNumber(offer.boardGameDice));
                    }
                    priceLabel.text = package.purchasePackage.priceLocalized;
                }

                if (currentState == State.ACTIVE)
                {
                    perksPanel.init(-1, package, statKingdom, perksToCycle, null, perksCycler);
                    purchaseButton.registerEventDelegate(purchaseClicked);
                    purchaseButton.enabled = true;
                }
                else
                {
                    if (init)
                    {
                        perksPanel.init(-1, package, statKingdom, perksToCycle);
                    }
                    else
                    {
                        perksCycler.panelsToCycle.Remove(perksPanel);
                    }

                    perksPanel.dimCurrentIcon();
                    perksPanel.openDrawerButton.enabled = false;
                    purchaseButton.enabled = false;
                    dimmer.multiplyColors();
                    priceLabel.gameObject.SetActive(false);
                }

                break;
            case State.LOCKED:
                diceLabel.gameObject.SetActive(false);
                coinsLabel.gameObject.SetActive(false);
                perksPanel.gameObject.SetActive(false);
                purchaseButton.enabled = false;
                dimmer.multiplyColors();
                if (offer != null)
                {
                    priceLabel.text = package.purchasePackage.priceLocalized;
                }
                break;
        }
    }
    

    public IEnumerator playIntro()
    {
        switch (currentState)
        {
            case State.ACTIVE:
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(activeIntroAnimList));
                break;
            case State.COMPLETE:
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(completedIntroAnimList));
                break;
            default:
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(lockedIntroAnimList));
                break;
        }
    }

    public IEnumerator updateState(int currentIndex)
    {
        State oldState = currentState;
        setState(currentIndex, false);
        if (oldState == currentState)
        {
            yield break;
        }

        switch (currentState)
        {
            case State.ACTIVE:
                if (extraIconDimmer != null)
                {
                    extraIconDimmer.restoreColors();
                }
                dimmer.restoreColors();
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(unlockAnimList));
                break;
            case State.COMPLETE:
                dimmer.multiplyColors();
                purchaseButton.clearAllDelegates();
                purchaseButton.enabled = false;
                if (extraIconDimmer != null)
                {
                    extraIconDimmer.multiplyColors();
                }
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(completeAnimList));
                break;
            default:
                purchaseButton.enabled = false;
                purchaseButton.clearAllDelegates(); //This shouldn't have anything registered but clearing it just for safety 
                break;
        }
    }

    private void purchaseClicked(Dict args = null)
    {
        StatsManager.Instance.LogCount(
            counterName:"dialog",
            kingdom: statKingdom,
            phylum: statPhylum,
            genus: "click"
        );
        Audio.play(PURCHASE_CLICK_AUDIO);
        package.purchasePackage.makePurchase(package.bonus, collectablePack:package.collectableDropKeyName,purchaseRewardable:offer);
    }
    
    private void extraIconLoadSuccess(string path, Object obj, Dict args)
    {
        GameObject extraIcon = NGUITools.AddChild(iconParent, obj as GameObject);
        extraIconDimmer = extraIcon.GetComponent<AdjustObjectColorsByFactor>();
        if (currentState == State.LOCKED || currentState == State.COMPLETE)
        {
            if (extraIconDimmer != null)
            {
                extraIconDimmer.multiplyColors();
            }
        }
    }

    public Vector3 buttonPostion
    {
        get
        {
            return purchaseButton.gameObject.transform.position;
        }
        
    }

    private void extraIconLoadFailed(string path, Dict args)
    {
    }
}
