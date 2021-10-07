using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Scheduler;
using TMPro;

public class StreakSaleDialog : DialogBase
{

    [SerializeField] private UITexture backgroundImageTexture;
    [SerializeField] private StreakSaleOfferOption[] streakSaleOfferOptions;
    private int currentPagePurchaseIndex = 0; //This will be 0, 1, or 2. Of the 3 cards showing, one of them will correspond to the current purchase index for the sale, and this number is what we use to keep track of that.
    private PurchaseFeatureData purchaseFeatureData;
    public GameObject rewardCardPackPrefab, rewardCoins00Prefab, rewardCoins01Prefab, rewardCoins02Prefab, rewardNewCardPrefab;
    public UIGrid cardGrid;
    public GameObject offerOptionPrefab;
    [SerializeField] private Animator saleCompleteAnimator;
    [SerializeField] private TextMeshPro timerText;
    [SerializeField] private UIAnchor stopwatchAnchor;
    [SerializeField] private TextMeshPro bottomText;
    public bool purchaseConfirmed = false;
    [SerializeField] private GameObject coinAnimationGameObject;
    [SerializeField] private GameObject coinAnimationStartingPoint;
    [SerializeField] private float addCoinsToTotalDelay = 1f;
    private const int numberOfCardsInDialog = 3;
    public static StreakSaleDialog instance;
    public static float recentPurchaseCardXPos = 0f;
    public static long recentPurchaseCoinCount = 0;

    public override void close()
    {
        StreakSaleManager.attemptingPurchaseWithCardPack = false;
    }

    protected override void Start()
    {
        base.Start();
        instance = this;
    }
    protected void OnDestroy()
    {
        instance = null;
    }

    public override void init()
    {
        purchaseFeatureData = PurchaseFeatureData.StreakSale;

        if (downloadedTextures == null)
        {
            Debug.LogError("streak_sale -- *** downloadedTextures was null ***");
        }
        else if (downloadedTextures.Length < 1)
        {
            Debug.LogError("streak_sale -- *** downloadedTextures size < 1 ***");
        }
        else
        {
            bool bgSetSucccess = downloadedTextureToUITexture(backgroundImageTexture, 0);
            if (bgSetSucccess)
            {
                backgroundImageTexture.gameObject.SetActive(true);
            }
            else
            {
                StartCoroutine(DisplayAsset.loadTexture(ExperimentWrapper.StreakSale.bgImagePath, onBgLoad));
            }
        }

        if (StreakSaleManager.endTimer == null)
        {
            Debug.LogError("streak_sale -- *** StreakSaleManager.endTimer was null ***");
        }
        else
        {
            StreakSaleManager.endTimer.registerLabel(timerText, GameTimerRange.TimeFormat.REMAINING, true);
        }

        bottomText.text = ExperimentWrapper.StreakSale.bottomText;

        configureCurrentPageOfOffers();
        StartCoroutine(showFirst3Cards());
        StartCoroutine(waitThenRefrehAnchors());

        Audio.play("StreakSaleDialogueOpen");
    }

    private void onBgLoad(Texture2D tex, Dict args)
    {
        if (tex != null)
        {
            backgroundImageTexture.material.mainTexture = tex;
            backgroundImageTexture.gameObject.SetActive(true);
        }
    }
    private IEnumerator waitThenRefrehAnchors()
    {
        yield return new WaitForSeconds(0.2f);
        stopwatchAnchor.enabled = true;
    }

    private IEnumerator showFirst3Cards()
    {
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < numberOfCardsInDialog; i++)
        {

            if (streakSaleOfferOptions != null && streakSaleOfferOptions.Length > i)
            {
                streakSaleOfferOptions[i].gameObject.SetActive(true);
                yield return new WaitForSeconds(0.2f);

                if (streakSaleOfferOptions[i].shouldBeDim)
                {
                    streakSaleOfferOptions[i].enable(false);
                }
            }
        }
        yield return new WaitForSeconds(0.3f);
        for (int i = 1; i < 4; i++)
        {
            Audio.play("StreakSaleCardsPopulate" + i.ToString());
            yield return new WaitForSeconds(0.2f);
        }
    }

    public IEnumerator coinAnimationRoutine(float xPos, long creditCountToAdd)
    {
        coinAnimationGameObject.SetActive(false);
        yield return null;
        coinAnimationStartingPoint.transform.position = new Vector3(xPos, coinAnimationStartingPoint.transform.position.y, coinAnimationStartingPoint.transform.position.z);
        yield return null;
        coinAnimationGameObject.SetActive(true);
        yield return new WaitForSeconds(addCoinsToTotalDelay);
        
        SlotsPlayer.addCredits(creditCountToAdd, "streak_sale - granting free reward", true, true, true);
    }

    public void purchaseCompleted()
    {
        purchaseConfirmed = false; //Wait for another confirmation before allowing the animation to run again.

        RoutineRunner.instance.StartCoroutine(coinAnimationRoutine(recentPurchaseCardXPos, recentPurchaseCoinCount));
        recentPurchaseCoinCount = 0;

        int indexOfCardToUnlockWithAnimations = -1;
        for (int i = 0; i < numberOfCardsInDialog; i++)
        {
            if (streakSaleOfferOptions[i] != null && streakSaleOfferOptions[i].streakSalePackage.indexInOfferList == StreakSaleManager.purchaseIndex)
            {
                //This is the offer option that was just purchased.
                streakSaleOfferOptions[i].showPurchasedCheckmark();
                streakSaleOfferOptions[i].buttonText.gameObject.SetActive(false);
                streakSaleOfferOptions[i].enable(false);
            }

            if (streakSaleOfferOptions[i] != null && streakSaleOfferOptions[i].streakSalePackage.indexInOfferList == StreakSaleManager.purchaseIndex + 1)
            {
                //This is the offer option to unlock (with unlock animations) if we are still on the first page of three offers.
                indexOfCardToUnlockWithAnimations = i;
            }
        }

        StreakSaleManager.purchaseIndex++;

        if (StreakSaleManager.purchaseIndex >= StreakSaleManager.streakSalePackages.Count)
        {
            //The last package was purchased. Show the ending animation and close the dialog
            StartCoroutine(showSaleCompleteRoutine());
        }
        else if (StreakSaleManager.purchaseIndex > 2)
        {
            //In this case, the third offer has been purchased, so we will, from now on, delete the first card, slide the cards over to the left, and then clone in a new third card.
            StartCoroutine(showNextOfferRoutine());
        }
        else if (indexOfCardToUnlockWithAnimations > 0) //Sanity check
        {
            //In this case, we are still on the first page of three offers, and we will not do any deleting or sliding. We will simply unlock the next offer.

            streakSaleOfferOptions[indexOfCardToUnlockWithAnimations].unlock();
            streakSaleOfferOptions[indexOfCardToUnlockWithAnimations].enable(true);
        }
        StartCoroutine(waitThenPlayPurchaseCompleteSound());
    }
    private IEnumerator waitThenPlayPurchaseCompleteSound()
    {
        yield return new WaitForSeconds(0.15f);
        Audio.play("StreakSaleClaim");
    }
    private IEnumerator waitThenPlayCardIntroSound()
    {
        yield return new WaitForSeconds(0.675f);
        Audio.play("StreakSaleCardsPopulate1");
    }

    private IEnumerator showSaleCompleteRoutine()
    {
        yield return new WaitForSeconds(2f);

        for (int i = 0; i < numberOfCardsInDialog; i++)
        {
            yield return new WaitForSeconds(0.3f);
            streakSaleOfferOptions[i].hide();
            Audio.play("StreakSaleCardExit");
        }

        yield return new WaitForSeconds(2f);

        saleCompleteAnimator.SetTrigger("Show");

        StreakSaleManager.streakSaleActive = StreakSaleManager.showInCarousel = false;
        StreakSaleManager.updateBuyButtonManager();

        yield return new WaitForSeconds(5f);

        Dialog.close();
    }

    private IEnumerator showNextOfferRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        streakSaleOfferOptions[0].hide();
        Audio.play("StreakSaleCardExit");

        yield return new WaitForSeconds(1f);

        Destroy(streakSaleOfferOptions[0].gameObject);
        yield return null;
        cardGrid.RepositionTweened();

        yield return new WaitForSeconds(0.6f);

        streakSaleOfferOptions[0] = streakSaleOfferOptions[1];
        streakSaleOfferOptions[1] = streakSaleOfferOptions[2];

        GameObject newCard = (GameObject)CommonGameObject.instantiate(offerOptionPrefab, cardGrid.transform);
        newCard.transform.localPosition = new Vector3(99999, 0, 0);
        cardGrid.repositionNow = true;
        StartCoroutine(waitThenPlayCardIntroSound());

        streakSaleOfferOptions[2] = newCard.GetComponent<StreakSaleOfferOption>();
        streakSaleOfferOptions[2].streakSalePackage = StreakSaleManager.streakSalePackages[StreakSaleManager.purchaseIndex];
        streakSaleOfferOptions[2].nextPackage = StreakSaleManager.streakSalePackages.Count > StreakSaleManager.purchaseIndex + 1 ? StreakSaleManager.streakSalePackages[StreakSaleManager.purchaseIndex + 1] : null;
        clearRewardItem(streakSaleOfferOptions[2].offerRewardItemContainer);
        CreditPackage creditPackage = getCreditPackageByName(streakSaleOfferOptions[2].streakSalePackage.coinPackage, out int creditPackageIndex);
        streakSaleOfferOptions[2].creditPackage = creditPackage;
        streakSaleOfferOptions[2].updateUI(purchaseFeatureData, creditPackageIndex);
        streakSaleOfferOptions[2].setLockedState(false, false, false);

        addRewardItemPrefab(streakSaleOfferOptions[2]);
    }

    private void addRewardItemPrefab(StreakSaleOfferOption streakSaleOfferOption)
    {
        GameObject rewardItemPrefab;
        string spriteState = "";
        if (streakSaleOfferOption.streakSalePackage.nodeArt.Equals("offer_coin_00"))
        {
            rewardItemPrefab = rewardCoins00Prefab;
        }
        else if (streakSaleOfferOption.streakSalePackage.nodeArt.Equals("offer_coin_01"))
        {
            rewardItemPrefab = rewardCoins01Prefab;
        }
        else if (streakSaleOfferOption.streakSalePackage.nodeArt.Equals("offer_coin_02"))
        {
            rewardItemPrefab = rewardCoins02Prefab;
        }
        else if (streakSaleOfferOption.streakSalePackage.nodeArt.Equals("offer_wildcard_00"))
        {
            rewardItemPrefab = rewardNewCardPrefab;
            spriteState = "wild";
        }
        else
        {
            rewardItemPrefab = rewardCardPackPrefab;
        }
        streakSaleOfferOption.addRewardItemGraphic(rewardItemPrefab, spriteState);
    }

    private void configureCurrentPageOfOffers()
    {
        if (streakSaleOfferOptions == null)
        {
            Debug.LogError("streak_sale -- treakSaleOfferOptions NULL!");
            return;
        }
        if (streakSaleOfferOptions.Length < 3)
        {
            Debug.LogError("streak_sale -- streakSaleOfferOptions Length < 3!");
            return;
        }


        //Starting index is the index in the sale offer list that corresponds to the left-most card on the current page.
        int startingIndex = StreakSaleManager.purchaseIndex < 2 ? 0 : StreakSaleManager.purchaseIndex - 2;

        for (int i = 0; i < numberOfCardsInDialog; i++)
        {
            if (i + startingIndex == StreakSaleManager.purchaseIndex)
            {
                //This is to keep track of which one of the visible cards corresponds to the current purchase index of the sale.
                currentPagePurchaseIndex = i;
            }

            clearRewardItem(streakSaleOfferOptions[i].offerRewardItemContainer);

            if (i + startingIndex >= StreakSaleManager.streakSalePackages.Count)
            {
                Debug.LogError("streak_sale -- ERROR: i + startingIndex >= StreakSaleManager.streakSalePackages.Count");
                return;
            }

            CreditPackage creditPackage = getCreditPackageByName(StreakSaleManager.streakSalePackages[i + startingIndex].coinPackage, out int creditPackageIndex);
            streakSaleOfferOptions[i].creditPackage = creditPackage;
            streakSaleOfferOptions[i].streakSalePackage = StreakSaleManager.streakSalePackages[i + startingIndex];
            streakSaleOfferOptions[i].nextPackage = StreakSaleManager.streakSalePackages.Count > i + startingIndex + 1 ? StreakSaleManager.streakSalePackages[i + startingIndex + 1] : null;
            streakSaleOfferOptions[i].updateUI(purchaseFeatureData, creditPackageIndex);
            addRewardItemPrefab(streakSaleOfferOptions[i]);


            //If this offer is already bought, or is still locked, dim its card
            if (i + startingIndex < StreakSaleManager.purchaseIndex)
            {
                //already bought
                streakSaleOfferOptions[i].shouldBeDim = true;
                streakSaleOfferOptions[i].setLockedState(false, false, true);
            }
            else if (i + startingIndex == StreakSaleManager.purchaseIndex)
            {
                //current offer available
                streakSaleOfferOptions[i].shouldBeDim = false;
                streakSaleOfferOptions[i].setLockedState(false, false, false);
            }
            else
            {
                //locked offer
                streakSaleOfferOptions[i].shouldBeDim = true;
                streakSaleOfferOptions[i].setLockedState(true, false, false);
            }
        }

    }

    private CreditPackage getCreditPackageByName(string creditPackageName, out int creditPackageIndex)
    {
        try
        {
            for (int i = 0; i < purchaseFeatureData.creditPackages.Count; i++)
            {
                if (purchaseFeatureData.creditPackages[i].purchasePackage.keyName.Equals(creditPackageName))
                {
                    creditPackageIndex = i;
                    return purchaseFeatureData.creditPackages[i];
                }
            }

            Debug.LogError("streak_sale -- getCreditPackageByName - CREDIT PACKAGE NOT FOUND: " + creditPackageName);
            creditPackageIndex = 0;
            return new CreditPackage(null, 0, false);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("streak_sale -- getCreditPackageByName - CREDIT PACKAGE NOT FOUND: " + creditPackageName);
            creditPackageIndex = 0;
            return new CreditPackage(null, 0, false);
        }
    }

    private void clearRewardItem(GameObject rewardItemHolder)
    {
        foreach (Transform child in rewardItemHolder.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public override PurchaseSuccessActionType purchaseSucceeded(JSON data, PurchaseFeatureData.Type purchaseType)
    {
        purchaseConfirmed = true;
        return PurchaseSuccessActionType.leaveDialogOpenAndShowThankYouDialog; //Don't close the dialog. They may want to purchase the next package, or we may need to show the wow-cool-you-bought-everything message.
    }

    /*=========================================================================================
		SHOW DIALOG CALL
	=========================================================================================*/
    public static void showDialog(Dict args = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.HIGH)
    {
        Dialog.instance.showDialogAfterDownloadingTextures("streak_sale_dialog", ExperimentWrapper.StreakSale.bgImagePath, priorityType: priority);
    }


}
