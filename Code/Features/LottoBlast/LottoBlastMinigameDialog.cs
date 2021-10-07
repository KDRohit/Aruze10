using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.Scheduler;
using FeatureOrchestrator;
using System.Linq;

public class LottoBlastMinigameDialog : GenericDialogComponentView
{
    [SerializeField] private LottoBlastFreeIntroOverlay lottoBlastFreeIntroOverlay;
    [SerializeField] private LottoBlastFreeOutroOverlay lottoBlastFreeOutroOverlay;
    [SerializeField] private LottoBlastPremiumIntroOverlay lottoBlastPremiumIntroOverlay;
    [SerializeField] private LottoBlastPremiumOutroOverlay lottoBlastPremiumOutroOverlay;
    [SerializeField] private LottoBlastAreYouSureOverlay lottoBlastAreYouSureOverlay;

    [SerializeField] private LottoBlastBall[] balls;
    [SerializeField] private GameObject premiumBallGlow;
    [SerializeField] private GameObject closeButton;
    [SerializeField] private LottoBlastBall chosenBall1, chosenBall2;

    [SerializeField] LottoBlastBallRevealer revealedBall1AnimatorRoot;
    [SerializeField] LottoBlastBallRevealer revealedBall2AnimatorRoot;
    [SerializeField] private GameObject premiumJackpotHeader;
    private Vector3 revealedBall1AnimatorRootStartingPosition;
    
    [SerializeField] private AnimationListController.AnimationInformationList bottomUIOnAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList bottomUIOffAnimList;
    
    [SerializeField] private AnimationListController.AnimationInformationList rollupStartAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList rollupLoopingAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList rollupEndAnimList;
    
    [SerializeField] private AnimationListController.AnimationInformationList freeBallIntroAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList freeGameCollectAnimInfo;
    [SerializeField] private AnimationListController.AnimationInformationList freeGameEndPart2AnimInfo;
    
    [SerializeField] private AnimationListController.AnimationInformationList premiumBallIntroAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList premiumGameCollectAnimInfo;
    [SerializeField] private AnimationListController.AnimationInformationList startWithPremiumGameAnimInfo;
    
    [SerializeField] private AnimationListController.AnimationInformationList ballShuffleAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList normalBallRevealAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList jackpotBallRevealAnimList;

    [SerializeField] private AudioListController.AudioInformationList introSounds;
    
    [SerializeField] private GameObject freeBallSlotItem1;
    [SerializeField] private GameObject premiumBallSlotItem1;
    [SerializeField] private GameObject premiumBallSlotItem2;
    [SerializeField] private GameObject totalWinBox;

    [SerializeField] private LabelWrapperComponent startingBonusLabelFree;
    [SerializeField] private LabelWrapperComponent totalWinLabelFree;
    [SerializeField] private LabelWrapperComponent startingBonusLabelPremium;
    [SerializeField] private LabelWrapperComponent totalWinLabelPremium;
    [SerializeField] private LabelWrapperComponent premiumJackpotLabel;
    public string premiumBuyButtonText;
    public string potentialJackpotAmount;

    private RoyalRushCollectionModule rrMeter;

    private int[] freeMultipliers; //Add in a jackpot value in the free list even though it's not used, to avoid an out of range error.
    private int[] freeMultipliersDefault = { 3, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 7, 7, 10, 10, 15, 15, 25, 50, 1000 };
    private int[] premiumMultipliers;
    private int[] premiumMultipliersDefault = { 15, 15, 15, 15, 15, 20, 20, 20, 20, 25, 25, 25, 35, 35, 50, 50, 75, 75, 125, 250, 1000 };

    private static long seedValueFree = 1000;
    private static long payoutFree = 1000;
    private static int chosenMultiplierFree = 4;
    private static long seedValuePremium = 1000;
    private static long payoutPremium = 1000;
    private static int chosenMultiplier1Premium = 15;
    private static int chosenMultiplier2Premium = 20;
    private static long jackpotMultiplier = 1000;

    private Dictionary<int, int> multiplierToColorIndexTable = new Dictionary<int, int>();
    
    public CreditPackage creditPackage;
    private string variant;

    public static bool jackpotCheatOn = false;
    public static bool skipInit = false;

    private bool paymentConfirmationReceivedWhileDialogWasDisabled = false;

    public static bool openWithPremiumGamePaidFor = false;

    public const string PURCHASE_EVENT = "rewardable_purchased";

    public static LottoBlastMinigameDialog instance;

    public long getFreePayout()
    {
        return payoutFree;
    }
    public long getPremiumPayout()
    {
        return payoutPremium;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (paymentConfirmationReceivedWhileDialogWasDisabled)
        {
            paymentConfirmationReceivedWhileDialogWasDisabled = false;
            lottoBlastPremiumIntroOverlay.showPurchaseSucceededMode();
            closeButton.SetActive(false);
        }
    }

    public override void init()
    {
        if (Audio.maxGlobalVolume > Audio.listenerVolume && SlotBaseGame.instance != null)
        {
            SlotBaseGame.instance.restoreAudio(true);
        }
        instance = this;
        StartCoroutine(AudioListController.playListOfAudioInformation(introSounds));
        base.init();
        
        if (skipInit)
        {
            freeMultipliers = freeMultipliersDefault;
            premiumMultipliers = premiumMultipliersDefault;
        }
        else
        {
            seedValueFree = System.Convert.ToInt64(dialogArgs.getWithDefault(D.OPTION, seedValueFree));
            payoutFree = System.Convert.ToInt64(dialogArgs.getWithDefault(D.PAYOUT_CREDITS, payoutFree));
            object freeMultipliersWinning = dialogArgs.getWithDefault(D.OPTION1, null);
            if (freeMultipliersWinning != null)
            {
                string[] winningMultipliers = ((IEnumerable) freeMultipliersWinning).Cast<string>().ToArray();
                int.TryParse(winningMultipliers[0], out chosenMultiplierFree);
            }

            seedValuePremium = System.Convert.ToInt64(dialogArgs.getWithDefault(D.VALUE, seedValuePremium));
            payoutPremium = System.Convert.ToInt64(dialogArgs.getWithDefault(D.AMOUNT, payoutPremium));
            object premiumMultipliersWinning = dialogArgs.getWithDefault(D.OPTION2, null);
            if (premiumMultipliersWinning != null)
            {
                string[] winningPremiumMultipliers = ((IEnumerable) premiumMultipliersWinning).Cast<string>().ToArray();
                int.TryParse(winningPremiumMultipliers[0], out chosenMultiplier1Premium);
                int.TryParse(winningPremiumMultipliers[1], out chosenMultiplier2Premium);
            }
            
            creditPackage = dialogArgs.getWithDefault(D.PACKAGE, "") as CreditPackage;
            if (creditPackage != null)
            {
                premiumBuyButtonText = creditPackage.purchasePackage.getLocalizedPrice();
            }

            variant = (string) dialogArgs.getWithDefault(D.MODE, "");
        }
        
        if (SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame && SlotBaseGame.instance.tokenBar != null)
        {
            rrMeter = SlotBaseGame.instance.tokenBar as RoyalRushCollectionModule;
            if (rrMeter != null && rrMeter.currentRushInfo.currentState == RoyalRushInfo.STATE.PAUSED)
            {
                rrMeter.pauseTimers();
            }
        }

        closeButton.SetActive(false);
        totalWinBox.SetActive(false);

        premiumJackpotLabel.text = "";
        totalWinLabelFree.text = "";
        totalWinLabelPremium.text = "";

        initBallMultipliers();
        setBallMultiplierValuesAndColors(false);

        startingBonusLabelFree.text = CreditsEconomy.convertCredits(seedValueFree);
        startingBonusLabelPremium.text = CreditsEconomy.convertCredits(seedValuePremium);

        revealedBall1AnimatorRootStartingPosition = revealedBall1AnimatorRoot.transform.position;


        if (openWithPremiumGamePaidFor)
        {
            StartCoroutine(showPremiumGameFromStart());
        }
        else
        {
            lottoBlastFreeIntroOverlay.show();
        }
    }

    private void initBallMultipliers()
    {
        //Get ball multiplier values.
        string freePaytableKey = dialogArgs.getWithDefault(D.PAYTABLE_KEY2, "").ToString(); //should be lotto_blast_free
        string premiumPaytableKey = dialogArgs.getWithDefault(D.PAYTABLE_KEY1, "").ToString(); //should be

        JSON paytableFreeJson = BonusGamePaytable.findPaytable(freePaytableKey);
        if (paytableFreeJson != null)
        {
            JSON[] rounds = paytableFreeJson.getJsonArray("rounds");
            JSON[] wins = rounds[0].getJsonArray("wins");
            freeMultipliers = new int[wins.Length];
            for (int i = 0; i < wins.Length; i++)
            {
                freeMultipliers[i] = wins[i].getInt("multiplier", 2);
            }
        }
        else
        {
            freeMultipliers = freeMultipliersDefault;
        }
        
        JSON paytablePremiumJson = BonusGamePaytable.findPaytable(premiumPaytableKey);
        if (paytablePremiumJson != null)
        {
            JSON[] rounds = paytablePremiumJson.getJsonArray("rounds");
            JSON[] wins = rounds[0].getJsonArray("wins");
            premiumMultipliers = new int[wins.Length];
            for (int i = 0; i < wins.Length; i++)
            {
                premiumMultipliers[i] = wins[i].getInt("multiplier", 2);
            }

            jackpotMultiplier = premiumMultipliers[premiumMultipliers.Length - 1];
        }
        else
        {
            premiumMultipliers = premiumMultipliersDefault;
        }
    }
    
    protected void OnDestroy()
    {
        instance = null;
        skipInit = false;
    }

    public static void onPaymentConfirmed(JSON data)
    {
        if (data != null)
        {
            JSON grantDataJson = data.getJSON("grant_data");
            if (grantDataJson != null)
            {
                seedValuePremium = grantDataJson.getLong("seed_value",0);
                payoutPremium = grantDataJson.getLong("payout", 0);

                int[] mulipliers = grantDataJson.getIntArray("multiplier");
                chosenMultiplier1Premium = mulipliers[0];
                chosenMultiplier2Premium = mulipliers[1];
            }
        }

        if (instance == null)
        {
            openWithPremiumGamePaidFor = true;
            showDialog();
        }
        else
        {
            
            if (instance.gameObject.activeInHierarchy)
            {
                //update the UI
                instance.startingBonusLabelPremium.text = CreditsEconomy.convertCredits(seedValuePremium);
                instance.lottoBlastPremiumIntroOverlay.showPurchaseSucceededMode();
                instance.closeButton.SetActive(false);
            }
            else
            {
                Debug.LogError("********* onPaymentConfirmed called from minigame dialog --- minigame gameobject was DISABLED");
                instance.paymentConfirmationReceivedWhileDialogWasDisabled = true;
            }
        }
        
        if (payoutPremium > 0)
        {
            Server.handlePendingCreditsCreated("lottoBlastPremium", payoutPremium);
        }
    }

    public override void onCloseButtonClicked(Dict args = null)
    {
        if (!lottoBlastAreYouSureOverlay.gameObject.activeInHierarchy) //This ends up inactive sometimes to keep it from blocking clicks.
        {
            lottoBlastAreYouSureOverlay.gameObject.SetActive(true);
        }

        lottoBlastAreYouSureOverlay.show();
    }
    public void restorePremiumIntroOverlay()
    {
        closeButton.SetActive(true);
        lottoBlastAreYouSureOverlay.hide();
    }

    private IEnumerator showPremiumGameFromStart()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(startWithPremiumGameAnimInfo));

        //Calculate and show the the jackpot amount:
        potentialJackpotAmount = CreditsEconomy.convertCredits(seedValuePremium * jackpotMultiplier);
        premiumJackpotLabel.text = potentialJackpotAmount;
        startingBonusLabelPremium.text = CreditsEconomy.convertCredits(seedValuePremium);
        premiumBallGlow.SetActive(true);
        totalWinBox.SetActive(false);
        revealedBall1AnimatorRoot.transform.localPosition = revealedBall1AnimatorRootStartingPosition;
        setBallMultiplierValuesAndColors(true);

        lottoBlastPremiumIntroOverlay.show();
        lottoBlastPremiumIntroOverlay.showPurchaseSucceededMode();
        closeButton.SetActive(true);
    }

    public long getFreePotentialJackpotAmount()
    {
        setBallMultiplierValuesAndColors(false);
        
        return balls[balls.Length - 2].multiplierAmount * seedValueFree;
    }


    private void setBallMultiplierValuesAndColors(bool premium)
    {
        multiplierToColorIndexTable.Clear();
        int[] ballMultipliers = premium ? premiumMultipliers : freeMultipliers;
        
        int lastAssignedColorMultiplier = ballMultipliers[0];
        multiplierToColorIndexTable.Add(lastAssignedColorMultiplier, 0);
        
        int colorIndex = 1;
        int maxColorIndex = balls[0].getColorListSize() - 1;
        for (int i = 0; i < balls.Length; i++)
        {
            int ballColor = 0;
            if (i < ballMultipliers.Length)
            {
                int multiplier = ballMultipliers[i];
                balls[i].multiplierAmount = multiplier;
                
                //Get color from dictionary if multiplier already has color value
                //If not, get new colorIndex and add to multiplier -> colors dictionary
                if (!multiplierToColorIndexTable.TryGetValue(multiplier, out ballColor))
                {
                    if (colorIndex > maxColorIndex) //If we run out of colors, we'll just use the last color.
                    {
                        colorIndex = maxColorIndex;
                    }
                    
                    multiplierToColorIndexTable.Add(multiplier, colorIndex);
                    ballColor = colorIndex;
                    colorIndex++;
                }
                
                StartCoroutine(balls[i].setColor(ballColor, premium, multiplier == jackpotMultiplier));
                balls[i].setMultiplierText(true, balls[i].multiplierAmount);
            }
            else
            {
                balls[i].multiplierAmount = 1; //Default to multiplier of 1 if we have more balls than multipliers
                balls[i].setMultiplierText(false, balls[i].multiplierAmount); //Don't show the ball if it didn't have a valid multiplier
            }

        }
    }

    private void setBallColor(LottoBlastBall ball, int colorIndex, bool isPremium, bool isJackpot)
    {
        if (ball.gameObject == null)
        {
            return;
        }
        
        StartCoroutine(ball.setColor(colorIndex, isPremium, isJackpot));
    }

    public void freeStartButtonPressed()
    {
        StartCoroutine(playFreeGame());
    }
    public void freeCollectButtonPressed()
    {
        //Grant free game payout
        SlotsPlayer.addFeatureCredits(payoutFree, "lottoBlastFree");
        StatsLottoBlast.logCollectReward("free_game", chosenBall1.multiplierAmount, 0, seedValueFree, payoutFree);
        // Tell the server the player saw the bonus summary screen.
        if (BonusGamePresenter.HasBonusGameIdentifier())
        {
            SlotAction.seenBonusSummaryScreen(BonusGamePresenter.NextBonusGameIdentifier());
        }
        StartCoroutine(collectFreeRewardAndShowPremiumPurchaseUI());
    }
    public void startPremiumGameRoutine()
    {
        StartCoroutine(playPremiumGame());
    }
    public void premiumGameEnded()
    {
        StatsLottoBlast.logCollectReward("paid_game", chosenBall1.multiplierAmount, chosenBall2.multiplierAmount, seedValuePremium, payoutPremium);
        if (BonusGamePresenter.HasBonusGameIdentifier())
        {
            SlotAction.seenBonusSummaryScreen(BonusGamePresenter.NextBonusGameIdentifier());
        }
        StartCoroutine(concludePremiumGame());
    }

    private IEnumerator playFreeGame()
    {
        long finalWinAmount = (seedValueFree * chosenMultiplierFree);
        
        //Play animations for balls mixing around and flowing through tube
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(freeBallIntroAnimList));
        yield return StartCoroutine(selectBall(revealedBall1AnimatorRoot, chosenMultiplierFree, false, freeBallSlotItem1.transform.position));
        
        //Start rollup once ball is in resting place
        yield return StartCoroutine(revealedBall1AnimatorRoot.playCelebration(chosenMultiplierFree == jackpotMultiplier));
        yield return StartCoroutine(increaseTotalWinAmountAndUpdateLabel(totalWinLabelFree, seedValueFree, finalWinAmount, false));

        //Turn off UI when rollup completes
        yield return StartCoroutine(toggleBottomUI(false));

        lottoBlastFreeOutroOverlay.show();
    }

    private IEnumerator selectBall(LottoBlastBallRevealer chosenBall, int multiplier, bool isPremium, Vector3 tweenEndPos)
    {
        //Setup ball to be revealed
        bool isJackpot = multiplier == jackpotMultiplier;
        chosenBall.lottoBall.setMultiplierText(true, multiplier);
        setBallColor(chosenBall.lottoBall,multiplierToColorIndexTable[multiplier], isPremium, isJackpot);
        
        //Play animations for balls mixing around and flowing through tube
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(ballShuffleAnimList));
        if (isJackpot)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotBallRevealAnimList));
        }
        else
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(normalBallRevealAnimList));
        }
        
        //TODO: move ball reveal animation & tween to be played with AnimatedParticleEffect
        yield return StartCoroutine(chosenBall.playReveal(isJackpot));
        yield return new TITweenYieldInstruction(iTween.MoveTo(chosenBall.gameObject, tweenEndPos, 1.1f));
    }
    private IEnumerator collectFreeRewardAndShowPremiumPurchaseUI()
    {
        //Calculate and show the the jackpot amount:
        potentialJackpotAmount = CreditsEconomy.convertCredits(seedValuePremium * jackpotMultiplier);
        premiumJackpotLabel.text = potentialJackpotAmount;
        
        yield return StartCoroutine(toggleBottomUI(true));
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(freeGameCollectAnimInfo));
        
        premiumBallGlow.SetActive(true);
        totalWinBox.SetActive(false);
        closeButton.SetActive(true);

        setBallMultiplierValuesAndColors(true);

        lottoBlastPremiumIntroOverlay.show();
    }
    private IEnumerator playPremiumGame()
    {
        revealedBall1AnimatorRoot.transform.position = revealedBall1AnimatorRootStartingPosition;
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(freeBallIntroAnimList));
        yield return StartCoroutine(selectBall(revealedBall1AnimatorRoot, chosenMultiplier1Premium, true, premiumBallSlotItem1.transform.position));
        yield return StartCoroutine(selectBall(revealedBall2AnimatorRoot, chosenMultiplier2Premium, true, premiumBallSlotItem2.transform.position));

        
        //Start rollup after balls are selected
        long premiumWinAmountFromBall1 = seedValuePremium * chosenMultiplier1Premium;
        yield return StartCoroutine(revealedBall1AnimatorRoot.playCelebration(chosenMultiplier1Premium == jackpotMultiplier));
        yield return StartCoroutine(increaseTotalWinAmountAndUpdateLabel(totalWinLabelPremium, seedValuePremium, premiumWinAmountFromBall1, false));
        
        
        //Rollup ball 2
        long premiumWinAmountFromBall2 = seedValuePremium * chosenMultiplier2Premium;
        yield return StartCoroutine(revealedBall2AnimatorRoot.playCelebration(chosenMultiplier2Premium == jackpotMultiplier));
        yield return StartCoroutine(increaseTotalWinAmountAndUpdateLabel(totalWinLabelPremium, premiumWinAmountFromBall1, premiumWinAmountFromBall1 + premiumWinAmountFromBall2, true));
        
        yield return StartCoroutine(toggleBottomUI(false));
        lottoBlastPremiumOutroOverlay.show();
        premiumJackpotHeader.SetActive(false);
    }
    
    private IEnumerator concludePremiumGame()
    {
        //Grant premium game payout
        SlotsPlayer.addFeatureCredits(payoutPremium, "lottoBlastPremium");
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(premiumGameCollectAnimInfo));
        yield return StartCoroutine(toggleBottomUI(true));
        Dialog.close(this);
    }

    private IEnumerator increaseTotalWinAmountAndUpdateLabel(LabelWrapperComponent tmpToUpdate, long startingAmount, long finalAmount, bool skipIntro)
    {
        if (!skipIntro)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rollupStartAnimList));
        }

        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rollupLoopingAnimList));
        yield return StartCoroutine(SlotUtils.rollup(startingAmount, finalAmount, tmpToUpdate, false, shouldSkipOnTouch: false, shouldBigWin: false));
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(rollupEndAnimList));
    }

    private IEnumerator toggleBottomUI(bool shouldShow)
    {
        yield return StartCoroutine(chosenBall1.toggle(shouldShow));
        yield return StartCoroutine(chosenBall2.toggle(shouldShow));
        if (!shouldShow)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(bottomUIOffAnimList));

        }
        else
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(bottomUIOnAnimList));
        }
    }

    protected override void onShow()
    {
        base.onShow();
    }

    public override void close()
    {
        if (SlotBaseGame.instance != null && SlotBaseGame.instance.isRoyalRushGame)
        {
            if (rrMeter != null && rrMeter.currentRushInfo.currentState == RoyalRushInfo.STATE.PAUSED)
            {
                if (GameState.game != null)
                {
                    //Since the mini game shows up on completing the level we need to call unpause here
                    RoyalRushAction.unPauseLevelUpEvent(GameState.game.keyName);
                }
            }
        }
    }

    /*=========================================================================================
		SHOW DIALOG CALL
	=========================================================================================*/
    public static void showDialog(Dict args = null, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.HIGH)
    {
        Scheduler.addDialog("level_lotto_minigame_dialog", args, priority);
    }

    public void attemptPremiumGamePurchase()
    {
        if (skipInit) //For testing in the editor. This will be true if minigame is opened from the debug panel -> dialogs
        {
            lottoBlastPremiumIntroOverlay.showPurchaseSucceededMode();
            closeButton.SetActive(false);
        }
        else if (creditPackage != null)
        {
            if (creditPackage.purchasePackage != null)
            {
                //PurchaseFeatureData.Type purchaseType = featureData != null ? featureData.type : PurchaseFeatureData.Type.NONE;
                creditPackage.purchasePackage.makePurchase(packageClass: "BonusGamePackage", 
                    purchaseType: PurchaseFeatureData.Type.BONUS_GAME, 
                    collectablePack: creditPackage.collectableDropKeyName, 
                    economyTrackingNameOverride: "lotto_blast_purchase", 
                    seedValue: seedValuePremium,
                    lottoBlastKey: creditPackage.purchasePackage.keyName + "_" + variant, 
                    themeName:"lotto_blast_purchase");
            }
            else
            {
                Debug.LogError("LOTTO BLAST ERROR - creditPackage.purchasePackage was null");
            }
        }
        else
        {
            Debug.LogError("LOTTO BLAST ERROR - creditPackage was null");
        }

    }

    public override PurchaseSuccessActionType purchaseSucceeded(JSON data, PurchaseFeatureData.Type purchaseType)
    {
        return PurchaseSuccessActionType.skipThankYouDialog;
    }

    public override void purchaseFailed(bool timedOut)
    {
        //Restore the premium intro state
        closeButton.SetActive(true);
        lottoBlastAreYouSureOverlay.hide();

        if (timedOut)
        {
            Dialog.close(this);
        }
    }

}
