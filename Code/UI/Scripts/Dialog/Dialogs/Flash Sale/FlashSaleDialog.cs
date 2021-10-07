using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Scheduler;
using TMPro;

public class FlashSaleDialog : DialogBase
{
    public TextMeshPro[] digits;
    public TextMeshPro salesCreditsText, totalCreditsText, bonusPercentageText, buttonText;
    public FlashSaleDigitAnimator[] digitAnimators;
    public ClickHandler buyButtonHandler;

    [SerializeField] private UIAnchor[] perksUiAnchorsToRefresh;
    [SerializeField] protected PurchasePerksPanel perksPanel;
    [SerializeField] private UIAnchor coinSizerAnchor;

    private string lastDigitString = "0000";

    private PurchaseFeatureData featureData;
    private CreditPackage creditPackage = null; // Used in the new payments system.
    private int creditPackageIndex = -1;


    private bool purchaseButtonPressed = false;

    private PurchasablePackage thePurchasablePackage;

    public static FlashSaleDialog instance;

    public override void init()
    {
        StatsManager.Instance.LogCount
        (
            counterName: "dialog",
            kingdom: "flash_sale",
            phylum: "dialog",
            klass: "",
            genus: "view",
            milestone: "",
            val: -1
        );

        updateDigits(getDigitsString());
        StartCoroutine(updatePackageCount());

        //TODO - Currently using the buy page options. Need to make a new set of flash sale options, maybe?
        featureData = PurchaseFeatureData.BuyPage;
        if (featureData != null && featureData.creditPackages != null && featureData.creditPackages.Count > 0)
        {
            creditPackage = featureData.creditPackages[0];
        }

        for (int i = 0; i < featureData.creditPackages.Count; i++)
        {
            if (featureData.creditPackages[i].purchasePackage.keyName.Equals(ExperimentWrapper.FlashSale.package))
            {
                creditPackage = featureData.creditPackages[i];
                creditPackageIndex = i;
            }
        }

        if (creditPackage != null)
        {
            thePurchasablePackage = creditPackage.purchasePackage;
        }
        else
        {
            thePurchasablePackage = PurchasablePackage.find(ExperimentWrapper.FlashSale.package);
        }

        if (creditPackage == null) { creditPackage = new CreditPackage(thePurchasablePackage, 0, false); }

        if (featureData != null)
        {
            List<PurchasePerksPanel.PerkType> cyclingPerks = PurchasePerksPanel.getEligiblePerks(featureData);
            PurchasePerksCycler perksCycler = new PurchasePerksCycler(ExperimentWrapper.BuyPageDrawer.delays, Mathf.Min(ExperimentWrapper.BuyPageDrawer.maxItemsToRotate, cyclingPerks.Count));
            perksPanel.init(creditPackageIndex, creditPackage, "flash_sale", cyclingPerks, perksCloseButtonHandler, perksCycler);
            perksPanel.openDrawerButton.registerEventDelegate(perksOpenDrawerButtonHandler);
            perksCycler.startCycling();
        }

        salesCreditsText.text = CreditsEconomy.convertCredits(thePurchasablePackage.totalCredits(creditPackage.bonus, true));
        totalCreditsText.text = CreditsEconomy.convertCredits(thePurchasablePackage.totalCredits(bonusPercent: creditPackage.bonus, isBuyPage: true, bonusSalePercent: ExperimentWrapper.FlashSale.bonusPercentage));
        bonusPercentageText.text = "+" + ExperimentWrapper.FlashSale.bonusPercentage.ToString() + "%";
        buttonText.text = thePurchasablePackage.getLocalizedPrice();

    }

    public void perksCloseButtonHandler(Dict args)
    {

    }
    public void perksOpenDrawerButtonHandler(Dict args)
    {
        //A few UI Anchors need to be refreshed in the perks drawer panel since they have elements that are added after the anchors are disabled.
        for (int i = 0; i < perksUiAnchorsToRefresh.Length; i++)
        {
            perksUiAnchorsToRefresh[i].enabled = true;
        }
    }

    protected override void onShow()
    {
        base.onShow();
        StartCoroutine(refreshLayoutItemsAfterTextHasBeenSet());
        StartCoroutine(waitThenCheckIfSaleIsStillActive()); //This is for the case when the sale ends while the purchase dialog is showing, and the player cancels the purchase. We need to make sure the dialog closes itself as soon as it is brought back.
    }
    private IEnumerator waitThenCheckIfSaleIsStillActive()
    {
        yield return new WaitForSeconds(0.5f);
        if (FlashSaleManager.packagesRemaining < 1 || !FlashSaleManager.flashSaleIsActive)
        {
            Dialog.close();
        }
    }
    public IEnumerator refreshLayoutItemsAfterTextHasBeenSet()
    {
        yield return new WaitForSeconds(0.1f);
        coinSizerAnchor.enabled = true;
        yield return new WaitForSeconds(3f);
        coinSizerAnchor.enabled = true;
    }


    public override void close()
    {
        StatsManager.Instance.LogCount
        (
            counterName: "dialog",
            kingdom: "flash_sale",
            phylum: "dialog",
            klass: "",
            family: "close",
            genus: "click",
            milestone: "",
            val: -1
        );
    }

    private void OnDestroy() { instance = null; }
    private void OnDisable() { instance = null; }
    private void OnEnable() { instance = this; }

    protected override void Start()
    {
        base.Start();

        instance = this;

        buyButtonHandler.registerEventDelegate(onBuyButtonClicked);

        purchaseButtonPressed = false;

        StartCoroutine(refreshLayoutItemsAfterTextHasBeenSet());
    }

    public void onBuyButtonClicked(Dict args = null)
    {
        if (FlashSaleManager.purchaseSucceeded) //Trying to avoid allowing the button to be tapped again while the dialog is closing after a successful purchase:
        {
            return;
        }

        StatsManager.Instance.LogCount
        (
            counterName: "dialog",
            kingdom: "flash_sale",
            phylum: "dialog",
            klass: "",
            family: "cta",
            genus: "click",
            milestone: "",
            val: -1
        );

        purchaseButtonPressed = true;
        thePurchasablePackage.makePurchase(bonusPercent: creditPackage.bonus, saleBonusPercent: ExperimentWrapper.FlashSale.bonusPercentage, purchaseType: PurchaseFeatureData.Type.BUY_PAGE, collectablePack: creditPackage.collectableDropKeyName);
    }

    private IEnumerator updatePackageCount()
    {
        lastDigitString = getDigitsString(); //Otherwise the first animation is incorrect, since getDigitsString gets initialized to "0000".

        while (FlashSaleManager.packagesRemaining > 0 && FlashSaleManager.flashSaleIsActive)
        {
            yield return new WaitForSeconds(Random.Range(2f, 3.75f));

            string newDigitString = getDigitsString();

            for (int i = 0; i < 4; i++)
            {
                if (!newDigitString.Substring(3 - i, 1).Equals(lastDigitString.Substring(3 - i, 1)))
                {
                    digitAnimators[i].startAnimation();
                    yield return new WaitForSeconds(Random.Range(0.01f, 0.04f));
                }
            }

            lastDigitString = newDigitString;
            yield return new WaitForSeconds(0.25f);
            updateDigits(newDigitString);
        }
    }

    public void updateDigits(string newDigits)
    {
        for (int i = 0; i < 4; i++)
        {
            digits[i].text = newDigits.Substring(i, 1);
        }
    }
    private string getDigitsString()
    {
        string fmt = "0000";
        return FlashSaleManager.packagesRemaining.ToString(fmt);
    }

    /*=========================================================================================
		SHOW DIALOG CALL
	=========================================================================================*/
    public static void showDialog(Dict args = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.HIGH)
    {
        Scheduler.addDialog("flash_sale_dialog", args, priority);
    }

    public override PurchaseSuccessActionType purchaseSucceeded(JSON data, PurchaseFeatureData.Type purchaseType)
    {
        if (purchaseButtonPressed) //If not, then it was a non flash sale purchase that was made, and in that case, don't end the flash sale.
        {
            FlashSaleManager.purchaseSucceeded = true;
        }

        return PurchaseSuccessActionType.closeDialog;
    }

}
