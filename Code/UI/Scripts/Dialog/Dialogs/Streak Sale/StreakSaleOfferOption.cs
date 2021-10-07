using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StreakSaleOfferOption : MonoBehaviour
{
    [SerializeField] private ClickHandler buyButtonHandler;
    [SerializeField] private Animator cardAnimator;
    [SerializeField] private Animator buttonLockAnimator;
    [SerializeField] private Animator ribbonLockAnimator;
    [SerializeField] private Animator checkmarkAnimator;
    public TextMeshPro buttonText;
    [SerializeField] private GameObject buttonLockGraphic;
    public TextMeshPro coinAmountLabel;
    public TextMeshPro bonusPercentLabel;
    [SerializeField] protected PurchasePerksPanel perksPanel;
    [SerializeField] private GameObject perksParent;
    public GameObject offerRewardItemContainer;
    [SerializeField] private AdjustObjectColorsByFactor dimmer;
    private AdjustObjectColorsByFactor rewardItemDimmer;
    public bool shouldBeDim = false;
    [SerializeField] private GameObject perksPanelParent;
    private AdjustObjectColorsByFactor[] adjustColorsScriptsInPerksPanel;
    public ObjectSwapper frameStyleSwapper, optionStyleSwapper, lockedStyleSwapper;
    public CreditPackage creditPackage;
    public StreakSalePackage streakSalePackage;
    public UITexture backgroundTexture;
    public UIButton buyButton;
    [SerializeField] private GameObject ribbonLockParent;
    public StreakSaleOfferRewardItem rewardItem;
    private PurchasePerksCycler perksCycler;
    public StreakSalePackage nextPackage;
    [SerializeField] private GameObject bonusRibbonParent;

    private void Awake()
    {
        adjustColorsScriptsInPerksPanel = GetComponentsInChildren<AdjustObjectColorsByFactor>();
    }

    private void Start()
    {
        buyButtonHandler.registerEventDelegate(onBuyButtonClicked);
    }

    public void hide()
    {
        cardAnimator.SetTrigger("Outro");
    }
    public void show()
    {
        cardAnimator.SetTrigger("Intro");
    }

    private void OnDestroy()
    {
        BuyCreditsConfirmationDialog.onClose = null;
    }

    public void onBuyButtonClicked(Dict args = null)
    {
        buyButtonHandler.isEnabled = false;
        StreakSaleDialog.recentPurchaseCardXPos = gameObject.transform.position.x;

        perksPanel.closeDrawerClicked(null);

        BuyCreditsConfirmationDialog.onClose = () =>
        {
            if (StreakSaleDialog.instance != null)
            {
                StreakSaleDialog.instance.purchaseCompleted();
            }
            BuyCreditsConfirmationDialog.onClose = null; //Once the confirmation dialog is closed, make sure this action isn't called when a non-streak-sale purchase is confirmed later.
        };

        if (nextPackage == null) { Debug.LogError("streak_sale -- next package was NULL"); }

        if (nextPackage != null && nextPackage.rewardType.Equals("Free"))
        {
            StreakSaleManager.nextItemIsFree = true;
            StreakSaleManager.freeCoinPackage = nextPackage.coinPackage;
            StreakSaleManager.freeBonusPct = nextPackage.bonusPercent.ToString();
            StreakSaleManager.freeBaseBonusPct = nextPackage.baseBonusPercent.ToString();
            StreakSaleManager.freeCardPack = nextPackage.cardPack;
        }
        else
        {
            StreakSaleManager.nextItemIsFree = false;
        }

        if (streakSalePackage.rewardType.Equals("Free"))
        {
            StreakSaleDialog.recentPurchaseCoinCount = creditPackage.purchasePackage.totalCredits(creditPackage.bonus, false, bonusSalePercent: getAdjustedBonusPercent());
            StreakSaleDialog.instance.purchaseConfirmed = true;
            StreakSaleDialog.instance.purchaseCompleted();
        }
        else
        {
            //Attempt purchase
            if (creditPackage.purchasePackage != null)
            {
                if (streakSalePackage.cardPack.Length > 1)
                {
                    StreakSaleManager.attemptingPurchaseWithCardPack = true;
                }
                creditPackage.purchasePackage.makePurchase(bonusPercent: creditPackage.bonus, saleBonusPercent: getAdjustedBonusPercent(), purchaseType: PurchaseFeatureData.Type.STREAK_SALE, collectablePack: streakSalePackage.cardPack, streakSalePackageIndex: StreakSaleManager.purchaseIndex);
            }
            else
            {
                Debug.LogError("streak_sale -- purchasePackage was NULL");
            }
        }

    }

    private int getAdjustedBonusPercent()
    {
        if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_BUY_PAGE_KEY))
        {
            return streakSalePackage.bonusPercent + BuyPageBonusPowerup.salePercent;
        }
        else
        {
            return streakSalePackage.bonusPercent;
        }
    }

    public void updateUI(PurchaseFeatureData featureData, int packageIndex)
    {
        if (streakSalePackage == null)
        {
            Debug.LogError("streak_sale -- updateUI: streakSalePackage was null");
            return;
        }
        if (creditPackage == null)
        {
            Debug.LogError("streak_sale -- updateUI: creditPackage was null");
            return;
        }

        // create a clone of the credit package so we can override its properties with streak sale data.
        CreditPackage clonedCreditPackage = new CreditPackage(
            creditPackage.purchasePackage,
            creditPackage.bonus,
            false); // isJackpotEligible is always false here. This isn't a buy page feature. 

        // card pack keyname is the only property that needs this override at this time. 
        clonedCreditPackage.collectableDropKeyName = streakSalePackage.cardPack;

        if (clonedCreditPackage.purchasePackage != null)
        {
            coinAmountLabel.text = CreditsEconomy.convertCredits(clonedCreditPackage.purchasePackage.totalCredits(clonedCreditPackage.bonus, false, bonusSalePercent: getAdjustedBonusPercent()));
            if (streakSalePackage.rewardType.Equals("Free"))
            {
                bonusPercentLabel.text = Localize.text("streak_sale_free");
                buttonText.text = Localize.text("streak_sale_claim");
            }
            else
            {
                if (getAdjustedBonusPercent() < 1)
                {
                    bonusRibbonParent.SetActive(false);
                }
                else
                {
                    bonusRibbonParent.SetActive(true);
                    bonusPercentLabel.text = getAdjustedBonusPercent().ToString() + "%" + System.Environment.NewLine + Localize.text("streak_sale_bonus");
                    buttonText.text = clonedCreditPackage.purchasePackage.getLocalizedPrice();
                }
            }
        }
        else
        {
            coinAmountLabel.text = "";
        }

        frameStyleSwapper.setState(streakSalePackage.frameStyle);
        optionStyleSwapper.setState(streakSalePackage.backgroundStyle);

        if (featureData != null && perksCycler == null)
        {
            List<CreditPackage> creditPackageList = new List<CreditPackage>();
            creditPackageList.Add(clonedCreditPackage);
            List<PurchasePerksPanel.PerkType> cyclingPerks = PurchasePerksPanel.getEligiblePerksForPackages(creditPackageList);
            perksCycler = new PurchasePerksCycler(ExperimentWrapper.BuyPageDrawer.delays, cyclingPerks.Count);
            perksPanel.init(packageIndex, clonedCreditPackage, "streak_sale", cyclingPerks, perksCloseButtonHandler, perksCycler);
            perksPanel.openDrawerButton.registerEventDelegate(perksOpenDrawerButtonHandler);
            perksCycler.startCycling();
        }
    }

    public void disablePerksDrawer()
    {
        if (perksPanel != null)
        {
            perksPanel.openDrawerButton.enabled = false;
        }

        if (perksCycler != null)
        {
            perksCycler.pauseCycling();
        }
        else
        {
            return;
        }

        foreach (PurchasePerksPanel ppp in perksCycler.panelsToCycle)
        {
            ppp.dimCurrentIcon();
        }

        BoxCollider[] perkPanelButtonColliders = perksPanel.GetComponentsInChildren<BoxCollider>();

        foreach (BoxCollider bc in perkPanelButtonColliders)
        {
            bc.enabled = false;
        }

    }


    public void perksCloseButtonHandler(Dict args)
    {

    }
    public void perksOpenDrawerButtonHandler(Dict args)
    {
        //The labels are not being aligned when this thing opens, so we'll hunt down all the UIAnchors in the perks panel that may have been cloned in, and enable them.
        refreshPerkPanelAnchors();

        //This is hacky, disgusting actually, but in some cases, refreshing immediately is too early, and we must refresh the anchors again after the drawer has finished opening. Better solution needed.
        StartCoroutine(waitThenRefreshPerkPanelAnchors());
    }
    private IEnumerator waitThenRefreshPerkPanelAnchors()
    {
        yield return new WaitForSeconds(0.15f);
        refreshPerkPanelAnchors();
    }
    private void refreshPerkPanelAnchors()
    {
        Component[] uiAnchors = perksParent.GetComponentsInChildren<UIAnchor>(true);
        foreach (UIAnchor uiAnchor in uiAnchors)
        {
            uiAnchor.enabled = true;
        }
    }

    public void addRewardItemGraphic(GameObject rewardItemGraphicPrefab, string spriteState)
    {
        GameObject rewardItemGraphic = (GameObject)CommonGameObject.instantiate(rewardItemGraphicPrefab, offerRewardItemContainer.transform);
        rewardItemDimmer = rewardItemGraphic.GetComponent<AdjustObjectColorsByFactor>();
        rewardItem = rewardItemGraphic.GetComponent<StreakSaleOfferRewardItem>();

        if(spriteState != null && spriteState.Length > 1)
        {
            rewardItem.spriteSwap.swap(spriteState);
        }
    }
    public void enable(bool enabled)
    {
        StartCoroutine(enableRoutine(enabled));
    }
    private IEnumerator enableRoutine(bool enabled)
    {
        yield return new WaitForSeconds(0.2f); //Wait to make sure awake runs, so that the AdjustObjectColorsByFactor's get initialized.
        if (!enabled)
        {
            dimmer.multiplyColors();
            rewardItemDimmer.multiplyColors();
            if (adjustColorsScriptsInPerksPanel != null)
            {
                foreach (AdjustObjectColorsByFactor adjustObjectColorsByFactor in adjustColorsScriptsInPerksPanel)
                {
                    adjustObjectColorsByFactor.multiplyColors();
                }
            }

            if (rewardItem != null)
            {
                foreach (GameObject game_object in rewardItem.particleEffects)
                {
                    game_object.SetActive(false);
                }
            }

            buyButton.isEnabled = false;

            if (perksPanel != null)
            {
                perksPanel.openDrawerButton.enabled = false;
            }
        }
        else
        {
            dimmer.restoreColors();
            rewardItemDimmer.restoreColors();
            if (adjustColorsScriptsInPerksPanel != null)
            {
                foreach (AdjustObjectColorsByFactor a in adjustColorsScriptsInPerksPanel)
                {
                    a.restoreColors();
                }
            }

            if (rewardItem != null)
            {
                foreach (GameObject g in rewardItem.particleEffects)
                    g.SetActive(true);
            }

            yield return new WaitForSeconds(1.5f);

            //Make sure all lock graphics are hidden and all unlocked graphics are visible.
            ribbonLockParent.SetActive(false);
            buttonLockGraphic.SetActive(false);
            perksPanelParent.SetActive(true);
            coinAmountLabel.gameObject.SetActive(true);

            buttonText.gameObject.SetActive(true);
            buyButton.isEnabled = true;

            if (perksPanel != null)
            {
                perksPanel.openDrawerButton.enabled = true;
            }
        }
    }

    public void showPurchasedCheckmark()
    {
        checkmarkAnimator.SetTrigger("Completed");

        disablePerksDrawer();
    }

    public void unlock()
    {
        if (streakSalePackage.lockStyle.Equals("hide_price"))
        {
            buttonLockAnimator.SetTrigger("Unlock");
        }
        else
        {
            ribbonLockAnimator.SetTrigger("Unlock");
        }
    }

    public void setLockedState(bool locked, bool waitToEnableButtonText, bool bought)
    {
        if (locked)
        {
            // "locked_style_00" is the ribbon style and "locked_style_01" is the lock-on-top-of-the-button style
            if (streakSalePackage.lockStyle.Equals("hide_price"))
            {
                buttonText.gameObject.SetActive(false);
                buttonLockGraphic.SetActive(true);
                lockedStyleSwapper.setState("locked_style_01");
            }
            else
            {
                buttonText.gameObject.SetActive(true);
                buttonLockGraphic.SetActive(false);
                lockedStyleSwapper.setState("locked_style_00");
            }
        }
        else
        {
            if (bought)
            {
                ribbonLockParent.SetActive(false);
                buttonText.gameObject.SetActive(false);
                RoutineRunner.instance.StartCoroutine(waitThenRunCheckmarkAnimation());
            }
            else
            {
                if (waitToEnableButtonText)
                {
                    StartCoroutine(waitThenToggleButtonText(true));
                }
                else
                {
                    buttonText.gameObject.SetActive(true);
                }
            }
            buttonLockGraphic.SetActive(false);
        }

    }
    private IEnumerator waitThenToggleButtonText(bool show)
    {
        //This is to wait for the unlock animation to finish before popping in the button text.
        yield return new WaitForSeconds(1.1f);
        buttonText.gameObject.SetActive(show);
    }
    private IEnumerator waitThenRunCheckmarkAnimation()
    {
        //This is to wait for the unlock animation to finish before popping in the button text.
        yield return new WaitForSeconds(2.5f);
        showPurchasedCheckmark();
    }

}
