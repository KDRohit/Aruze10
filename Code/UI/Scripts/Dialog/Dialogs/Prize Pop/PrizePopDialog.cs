using System.Collections;
using System.Collections.Generic;
using Com.Rewardables;
using Com.Scheduler;
using PrizePop;
using UnityEngine;

public class PrizePopDialog : DialogBase, IResetGame
{
    [SerializeField] private UITexture backgroundRenderer;
    [SerializeField] private ClickHandler purchaseButton;
    [SerializeField] private ClickHandler infoButton;
    [SerializeField] private LabelWrapperComponent endTimerLabel;
    [SerializeField] private ObjectSwapper endingSoonSwapper;
    [SerializeField] private LabelWrapperComponent currentJackpotLabel;
    [SerializeField] private LabelWrapperComponent currentRoundLabel;
    [SerializeField] private LabelWrapperComponent extraPicksLabel;
    [SerializeField] private AnimationListController.AnimationInformationList pickAgainSlideOnAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList pickAgainSlideOffAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList extraPicksAnimationList;
    [SerializeField] private AnimationListController.AnimationInformationList newStageAnimationList;

    [SerializeField] private BonusGamePresenter challengePresenter;
    [SerializeField] private ModularChallengeGame challengeGame;
    [SerializeField] private ModularPickingGameVariant pickingGameVariant;
    [SerializeField] private PickingGameJackpotModule jackpotModule;

    [SerializeField] private Transform[] pickObjectParents;
    [SerializeField] private UICenteredGrid pickObjectsGrid;

    [SerializeField] private Transform overlayParent;

    [SerializeField] private UIAnchor[] contentAnchors; 


    public static PrizePopDialog instance;
    
    private ClickHandler[] pickObjects;
    private ModularChallengeGameOutcome outcome;
    private bool isExpectingReward = false;
    private bool hasJackpot = false;
    private PrizePopBoard currentThemesBoard;
    private int numberOfExpectedPicks = 0;
    private bool restartBonusInstantly = false;
    private JSON purchaseData = null;
    private string previousMusicKey = "";
    private bool isExtrasPanelShowing = false;
    private bool needsToUpdatePicksFromPurchase = false;
    private bool activeBonus = false;
    private int objectsRemaining = -1;
    private bool manuallyOpened = false;

    private const string BOARD_PREFAB_PATH = "Features/Prize Pop/Themed Assets/{0}/Prefabs/Prize Pop Board {0}";

    public const string BG_TEXTURE_PATH = "Features/Prize Pop/Themed Assets/{0}/Textures/Feature Dialog Background Image";
    
    //Sound Constants
    private const string BUY_PICKS_CLICKED_AUDIO_KEY = "BuyExtraClickPrizePopCommon";
    private const string BG_MUSIC_AUDIO_KEY = "BgTunePrizePop{0}";
    private const string PICKME_AUDIO_KEY = "PickMePrizePop{0}";
    private const string PICK_SELECT_AUDIO_KEY = "PickSelectPrizePop{0}";
    private const string INFO_CLICKED_AUDIO_KEY = "QuestionMarkPrizePopCommon";
    private const string FINAL_JACKPOT_REVEAL_AUDIO_KEY = "PickBoardCompletePrizePopCommon";
    private const string PICK_OUTRO_AUDIO_KEY = "RevealOthersPrizePopCommon";
    private const string NEW_BOARD_INTRO_AUDIO_KEY = "NewStagePrizePopCommon";

    private enum Pick_Type
    {
        BLANK,
        CARD_PACK,
        CREDITS,
        JACKPOT,
        EXTRA_CHANCE,
        NONE
    }
    public override void init()
    {
        if (Audio.maxGlobalVolume > Audio.listenerVolume && SlotBaseGame.instance != null)
        {
            SlotBaseGame.instance.restoreAudio(true);
        }
        
        manuallyOpened = (bool)dialogArgs.getWithDefault(D.MODE, "");
        setupBonusGameThemedSounds();
        previousMusicKey = Audio.defaultMusicKey;
        Audio.switchMusicKeyImmediate(string.Format(BG_MUSIC_AUDIO_KEY, ExperimentWrapper.PrizePop.theme));
        downloadedTextureToUITexture(backgroundRenderer, 0);
        instance = this;
        PrizePopFeature.instance.featureTimer.registerLabel(endTimerLabel.tmProLabel, keepCurrentText:true);
        endingSoonSwapper.setState(PrizePopFeature.instance.isEndingSoon() ? "ends_soon" : "default");
        currentJackpotLabel.text = CreditsEconomy.convertCredits(PrizePopFeature.instance.currentJackpot);
        currentRoundLabel.text = string.Format("Stage {0}/{1}", PrizePopFeature.instance.currentRound + 1, PrizePopFeature.instance.totalRounds);
        extraPicksLabel.text = CommonText.formatNumber(PrizePopFeature.instance.extraPicks);
        outcome = (ModularChallengeGameOutcome)dialogArgs.getWithDefault(D.DATA, null);
        isExpectingReward = (bool) dialogArgs.getWithDefault(D.AMOUNT, false);
        hasJackpot = (bool) dialogArgs.getWithDefault(D.IS_JACKPOT_ELIGIBLE, false);
        if (PrizePopFeature.instance.isEndingSoon())
        {
            purchaseButton.gameObject.SetActive(false);   
        }
        else
        {
            purchaseButton.registerEventDelegate(purchasePackage);
        }
        infoButton.registerEventDelegate(infoClicked);
        loadBoard();
        
    }

    protected override void onFadeInComplete()
    {
        base.onFadeInComplete();
        StartCoroutine(destroyAnchorsWhenReady());
    }

    private IEnumerator destroyAnchorsWhenReady()
    {
        //Issue where if these anchors try to run again when the dialog is hidden they appear on top of whatever dialog is currently showing while this is hidden
        //Destroying these anchors after they anchored the first time since they should be in the position they always need to be in
        foreach (UIAnchor anchor in contentAnchors)
        {
            while (anchor.enabled)
            {
                yield return null;
            }
            
            yield return null;

            Destroy(anchor);
        }
    }

    public void endingSoon()
    {
        endingSoonSwapper.setState("ends_soon");
        purchaseButton.gameObject.SetActive(false);
    }

    private void setupBonusGameThemedSounds()
    {
        //Dynamically setting bonus game sounds
        //These ones are themed so they can't be just set on the prefab directly
        pickingGameVariant.pickmeAnimSoundOverride = string.Format(PICKME_AUDIO_KEY, ExperimentWrapper.PrizePop.theme);
        pickingGameVariant.REVEAL_AUDIO = string.Format(PICK_SELECT_AUDIO_KEY, ExperimentWrapper.PrizePop.theme);
        if (PrizePopFeature.instance.currentRound == PrizePopFeature.instance.totalRounds - 1)
        {
            jackpotModule.REVEAL_AUDIO = FINAL_JACKPOT_REVEAL_AUDIO_KEY;
        }
    }

    private void infoClicked(Dict args = null)
    {
        Audio.play(INFO_CLICKED_AUDIO_KEY);
        if (!string.IsNullOrEmpty(ExperimentWrapper.PrizePop.videoUrl))
        {
            PrizePopFeature.instance.showVideo(false);
        }
    }

    private void loadBoard()
    {
        AssetBundleManager.load(this, string.Format(BOARD_PREFAB_PATH, ExperimentWrapper.PrizePop.theme), boardLoadSuccess, boardLoadFailed, isSkippingMapping:true, fileExtension:".prefab");
    }

    public void addNewPicks(List<PickemPick> additionalPicks)
    {
        if (additionalPicks.Count > 0)
        {
            PickemOutcome pickGameOutcome = new PickemOutcome();
            pickGameOutcome.reveals = new List<PickemPick>();
            pickGameOutcome.entries = additionalPicks;
            outcome = new ModularChallengeGameOutcome(pickGameOutcome);
            for (int i = 0; i < pickGameOutcome.entries.Count; i++)
            {
                if (pickGameOutcome.entries[i].credits > 0 ||
                    !string.IsNullOrEmpty(pickGameOutcome.entries[i].cardPackKey) ||
                    pickGameOutcome.entries[i].prizePopPicks > 0)
                {
                    if (pickGameOutcome.entries[i].isJackpot)
                    {
                        hasJackpot = true;
                    }
                    isExpectingReward = true;
                }
            }
        }
        
        if (restartBonusInstantly)
        {
            StartCoroutine(addPicksAndContinueBonusGame());
        }
    }

    private void pickClicked(Dict args = null)
    {
        int index = (int)args.getWithDefault(D.INDEX, -1);
        if (index >= 0 && pickingGameVariant.inputEnabled)
        {
            objectsRemaining--;
            ModularChallengeGameOutcomeEntry currentPick = pickingGameVariant.getCurrentPickOutcome();
            string pickValue = "";
            Pick_Type currentPickType = Pick_Type.NONE;
            
            bool hasMorePicks = challengeGame.getTotalPicksMade() + 1 < numberOfExpectedPicks; //+1 because this happens before the challengeGame has processed the new pick
            if (currentPick != null)
            {
                currentPickType = getPickType(currentPick, out pickValue);
                if (!hasMorePicks)
                {
                    purchaseButton.isEnabled = false; //Disable the purchase button since we're expecting some overlay to be shown now
                }
            }
            
            StatsPrizePop.logItemPick(currentPickType.ToString().ToLower(), pickValue, objectsRemaining);

            pickObjects[index].isEnabled = false;
            pickObjects[index] = null; //Null out entry incase we need to add picks after this round ends
            RewardablesManager.addEventHandler(rewardGranted);
            PrizePopFeature.instance.makePick(index);
            extraPicksLabel.text = CommonText.formatNumber(PrizePopFeature.instance.extraPicks);
            
            //Show "Choose Again" message panel if player has extra picks and they didn't reveal an award
            StartCoroutine(animateInExtraPicksPanel(hasMorePicks, true));
        }
        else
        {
            //Spend action, look for next available index
        }
    }

    private Pick_Type getPickType(ModularChallengeGameOutcomeEntry pick, out string value)
    {
        value = "";
        if (!string.IsNullOrEmpty(pick.cardPackKey))
        {
            value = pick.cardPackKey;
            return Pick_Type.CARD_PACK;
        }

        if (pick.credits > 0)
        {
            value = CreditsEconomy.convertCredits(pick.credits);
            if (hasJackpot)
            {
                return Pick_Type.JACKPOT;
            }

            return Pick_Type.CREDITS;
        }

        if (pick.prizePopPicks > 0)
        {
            value = pick.prizePopPicks.ToString();
            return Pick_Type.EXTRA_CHANCE;
        }

        return Pick_Type.BLANK;
    }

    private IEnumerator animateInExtraPicksPanel(bool hasMorePicksToMake, bool turnOffFirst)
    {
        if (turnOffFirst && isExtrasPanelShowing)
        {
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(pickAgainSlideOffAnimationList));
        }
        
        yield return null; //Wait a frame in case the bonus game hasn't process the pick and blocked input yet
        
        while (!pickingGameVariant.inputEnabled && challengePresenter.isGameActive)
        {
            yield return null;
        }
        
        if (hasMorePicksToMake)
        {
            isExtrasPanelShowing = true;
            yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(pickAgainSlideOnAnimationList));
        }
    }

    private void rewardGranted(Rewardable rewardable)
    {
        StartCoroutine(claimRewardAfterGameEnds(rewardable));
    }

    private IEnumerator claimRewardAfterGameEnds(Rewardable rewardable)
    {
        while (challengePresenter.isGameActive && (rewardable.type != RewardPrizePopPicks.TYPE || !pickingGameVariant.inputEnabled))
        {
            yield return null;
        }
        rewardable.consume();
        if (rewardable.feature == "prize_pop")
        {
            loadDialogOverlay(rewardable);
        }
    }

    private void loadDialogOverlay(PrizePopFeature.PrizePopOverlayType type, Dict args = null)
    {
        switch (type)
        {
            case PrizePopFeature.PrizePopOverlayType.KEEP_SPINNING:
                PrizePopDialogOverlay.loadKeepSpinningOverlay(this, overlayLoadSuccess, overlayLoadFailed);
                break;
            case PrizePopFeature.PrizePopOverlayType.BUY_EXTRA_PICKS:
                showBuyMorePicksOverlay(false, "in_dialog");
                break;
            case PrizePopFeature.PrizePopOverlayType.NEW_STAGE:
                PrizePopDialogOverlay.loadNewStageOverlay(this, overlayLoadSuccess, overlayLoadFailed);
                break;
        }
    }
    
    private void loadDialogOverlay(Rewardable rewardable)
    {
        if (rewardable == null)
        {
            if (CustomPlayerData.getInt(CustomPlayerData.LAST_SEEN_PRIZE_POP_OOP_TIME, 0) != PrizePopFeature.instance.featureTimer.startTimestamp || PrizePopFeature.instance.isEndingSoon())
            {
                if (CustomPlayerData.getInt(CustomPlayerData.LAST_SEEN_PRIZE_POP_OOP_TIME, 0) !=
                    PrizePopFeature.instance.featureTimer.startTimestamp)
                {
                    CustomPlayerData.setValue(CustomPlayerData.LAST_SEEN_PRIZE_POP_OOP_TIME, PrizePopFeature.instance.featureTimer.startTimestamp);
                }

                PrizePopDialogOverlay.loadOutOfPicksOverlay(this, overlayLoadSuccess, overlayLoadFailed);
            }
            else
            {
                showBuyMorePicksOverlay(true, "out_of_pops");
            }
        }
        else
        {
            Dict overlayArgs = Dict.create(D.DATA, rewardable);
            switch (rewardable.type)
            {
                case RewardCardPack.TYPE:
                    //Pop Card Pack Overlay
                    PrizePopDialogOverlay.loadCardRewardOverlay(this, overlayLoadSuccess, overlayLoadFailed, overlayArgs);
                    break;
                case RewardCoins.TYPE:
                    if (!hasJackpot)
                    {
                        PrizePopDialogOverlay.loadCoinRewardOverlay(this, overlayLoadSuccess, overlayLoadFailed, overlayArgs);
                    }
                    else
                    {
                        if (PrizePopFeature.instance.currentRound == PrizePopFeature.instance.totalRounds-1)
                        {
                            PrizePopDialogOverlay.loadFinalJackpotRewardOverlay(this, overlayLoadSuccess, overlayLoadFailed, overlayArgs);
                        }
                        else
                        {
                            PrizePopDialogOverlay.loadJackpotRewardOverlay(this, overlayLoadSuccess, overlayLoadFailed, overlayArgs);
                        }
            
                        PrizePopFeature.instance.advanceToNextRound();
                    }
                    RewardCoins coinReward = rewardable as RewardCoins;
                    if (coinReward != null)
                    {
                        SlotsPlayer.addCredits(coinReward.amount, "Prize Pop");
                        BonusGameManager.instance.finalPayout = 0; //0 this out because the slot uses it when rolling up line wins
                    }

                    break;
                case RewardPrizePopPicks.TYPE:
                    //Add extra picks overlay
                    PrizePopDialogOverlay.loadExtraPicksRewardOverlay(this, overlayLoadSuccess, overlayLoadFailed, overlayArgs);
                    break;
                default:
                    Debug.LogWarning("Unexpected reward type in Prize Pop Pick: " + rewardable.type);
                    break;
            }
        }
    }
    
    private void startBonusGame()
    {
        if (challengePresenter.isGameActive)
        {
            return;
        }
        //challengeGame
        pickingGameVariant.pickAnchors.Clear();
        for (int i = 0; i < pickObjects.Length; i++)
        {
            if (pickObjects[i] != null)
            {
                pickObjects[i].isEnabled = true;
                pickObjects[i].clearAllDelegates();
                pickObjects[i].registerEventDelegate(pickClicked, Dict.create(D.INDEX, i));
                pickingGameVariant.pickAnchors.Add(pickObjects[i].gameObject.transform.parent.gameObject);
            }
        }
        
        challengePresenter.gameObject.SetActive(true);
        BonusGamePresenter.instance = challengePresenter;
        challengePresenter.isReturningToBaseGameWhenDone = false;
        challengePresenter.init(isCheckingReelGameCarryOverValue:false);

        List<ModularChallengeGameOutcome> variantOutcomeList = new List<ModularChallengeGameOutcome>();

        // since each variant will use the same outcome we need to add as many outcomes as there are variants setup
        for (int m = 0; m < challengeGame.pickingRounds[0].roundVariants.Length; m++)
        {
            variantOutcomeList.Add(outcome);
        }

        challengeGame.addVariantOutcomeOverrideListForRound(0, variantOutcomeList);
        challengeGame.init();
        numberOfExpectedPicks += outcome.getCurrentRound().entryCount;
        StartCoroutine(waitForGameToFinish());
        outcome = null;
    }

    private IEnumerator waitForGameToFinish()
    {
        bool showBuyMorePicksOnGameEnd = !isExpectingReward;
        isExpectingReward = false; //Reset this now in case we get new picks mid game
        restartBonusInstantly = false;
        closeButtonHandler.gameObject.SetActive(false); //Make sure this off when playing the bonus game
        while (challengePresenter.isGameActive)
        {
            yield return null;
        }
        challengeGame.reset();

        for (int i = 0; i < pickObjects.Length; i++)
        {
            if (pickObjects[i] != null)
            {
                pickObjects[i].clearAllDelegates();
                pickObjects[i].isEnabled = false;
            }
        }

        if (showBuyMorePicksOnGameEnd && outcome == null)
        {
            closeButtonHandler.gameObject.SetActive(true);
            loadDialogOverlay(null);
#if UNITY_EDITOR            
            restartBonusInstantly = true;
#endif
        }
        else if (outcome != null && restartBonusInstantly)
        {
            yield return StartCoroutine(animateInExtraPicksPanel(true, false));
            startBonusGame();
        }
    }

    private void purchasePackage(Dict args = null)
    {
        restartBonusInstantly = true;
        Audio.play(BUY_PICKS_CLICKED_AUDIO_KEY);
        loadDialogOverlay(PrizePopFeature.PrizePopOverlayType.BUY_EXTRA_PICKS, Dict.create(D.CLOSE, !challengePresenter.isGameActive));
    }

    public override void close()
    {
        //update the in game meter
        InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.PRIZE_POP_KEY);
        instance = null;
        RewardablesManager.removeEventHandler(rewardGranted);
        PrizePopFeature.instance.featureTimer.removeLabel(endTimerLabel.tmProLabel);
        Audio.switchMusicKeyImmediate(previousMusicKey);
        StatsPrizePop.logCloseBoardDialog(PrizePopFeature.instance.currentRound+1, activeBonus, manuallyOpened, objectsRemaining);
    }

    public static void showDialog(bool manualStart, ModularChallengeGameOutcome outcome = null, bool isJackpot = false,bool hasReward = false, PrizePopFeature.PrizePopOverlayType startingOverlay = PrizePopFeature.PrizePopOverlayType.NONE)
    {
        Dict args = Dict.create(D.DATA, outcome, D.IS_JACKPOT_ELIGIBLE, isJackpot, D.AMOUNT, hasReward, D.TYPE, startingOverlay, D.MODE, manualStart);
        string backgroundTexturePath = string.Format(BG_TEXTURE_PATH, ExperimentWrapper.PrizePop.theme);
        
        //Needs to be blocking if we're actually playing the pick game so user's can't spin before the bonus has had a chance to complete 
        SchedulerPriority.PriorityType priority = outcome == null ? SchedulerPriority.PriorityType.MEDIUM : SchedulerPriority.PriorityType.HIGH;
        Dialog.instance.showDialogAfterDownloadingTextures("prize_pop", backgroundTexturePath, args, isExplicitPath: true, skipBundleMapping: true, priorityType:priority);
    }

    private void boardLoadSuccess(string path, Object obj, Dict args)
    {
        currentThemesBoard = (obj as GameObject).GetComponent<PrizePopBoard>();
        setupCurrentBoard();

        activeBonus = outcome != null;
        StatsPrizePop.logViewBoardDialog(PrizePopFeature.instance.currentRound+1, activeBonus, manuallyOpened, objectsRemaining);

        //If we pass an outcome to the dialogArgs, start up the bonus game
        if (outcome != null)
        {
            startBonusGame();
        }
        else
        {
            closeButtonHandler.gameObject.SetActive(true);
            for (int i = 0; i < pickObjects.Length; i++)
            {
                if (pickObjects[i] != null)
                {
                    pickObjects[i].boxCollider.enabled = false;
                }
            }

            PrizePopFeature.PrizePopOverlayType startingOverlay = (PrizePopFeature.PrizePopOverlayType) dialogArgs.getWithDefault(D.TYPE, PrizePopFeature.PrizePopOverlayType.NONE);
            if (startingOverlay != PrizePopFeature.PrizePopOverlayType.NONE)
            {
                loadDialogOverlay(startingOverlay);
            }
        }
    }

    private void setupCurrentBoard()
    {
        List<PrizePopFeature.PrizePopPickData> previousPicks = PrizePopFeature.instance.getCurrentRoundPicks();
        GameObject[] spawnedPickObjects = currentThemesBoard.generateBoard(PrizePopFeature.instance.currentRound, pickObjectParents, pickObjectsGrid.maxPerLine, previousPicks);
        pickObjects = new ClickHandler[spawnedPickObjects.Length];
        objectsRemaining = 0;
        for (int i = 0; i < spawnedPickObjects.Length; i++)
        {
            if (spawnedPickObjects[i] != null)
            {
                pickObjects[i] = spawnedPickObjects[i].GetComponent<ClickHandler>();
                objectsRemaining++;
            }
        }
    }

    private void boardLoadFailed(string path, Dict args)
    {
        
    }
    
    private void overlayLoadSuccess(string path, Object obj, Dict args)
    {
        GameObject overlayObject = NGUITools.AddChild(overlayParent, obj as GameObject);
        PrizePopDialogOverlay overlay = overlayObject.GetComponent<PrizePopDialogOverlay>();
        Rewardable reward = null;
        if (args != null)
        {
            reward = (Rewardable) args.getWithDefault(D.DATA, null);
        }

        overlay.init(reward, this, args);
    }

    private void overlayLoadFailed(string path, Dict args)
    {
        
    }

    public void onJackpotOverlayClosed()
    {
        StartCoroutine(transitionToNextRound());
    }

    private IEnumerator transitionToNextRound()
    {
        for (int i = 0; i < pickObjects.Length; i++)
        {
            if (pickObjects[i] != null)
            {
                Animator handlerAnimator = pickObjects[i].GetComponent<Animator>();
                if (handlerAnimator != null)
                {
                    handlerAnimator.Play("Outro");
                    Audio.play(PICK_OUTRO_AUDIO_KEY);
                    yield return new WaitForSeconds(0.2f); //stagger the outros
                }
            }
        }
        
        setupCurrentBoard();
        Audio.play(NEW_BOARD_INTRO_AUDIO_KEY);

        for (int i = 0; i < pickObjects.Length; i++)
        {
            if (pickObjects[i] != null)
            {
                pickObjects[i].boxCollider.enabled = false;
            }
        }
        
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(newStageAnimationList));
        currentJackpotLabel.text = CreditsEconomy.convertCredits(PrizePopFeature.instance.currentJackpot);
        currentRoundLabel.text = string.Format("Stage {0}/{1}", PrizePopFeature.instance.currentRound + 1, PrizePopFeature.instance.totalRounds);
        
        yield return new WaitForSeconds(0.5f); //Pause on the new level for a bit before we show the new stage overlay

        loadDialogOverlay(PrizePopFeature.PrizePopOverlayType.NEW_STAGE);
    }

    public void showBuyMorePicksOverlay(bool autoOpened, string source)
    {
        PrizePopDialogOverlay.loadBuyExtraPicksRewardOverlay(this, overlayLoadSuccess, overlayLoadFailed, Dict.create(D.CLOSE, autoOpened, D.TYPE, source));
    }

    public static void resetStaticClassData()
    {
        instance = null;
    }

    protected override void onShow()
    {
        if (purchaseData != null)
        {
            showPurchaseSucceeded();
            purchaseData = null;
        }
        else if (needsToUpdatePicksFromPurchase)
        {
            StartCoroutine(addPicksAndContinueBonusGame());
            needsToUpdatePicksFromPurchase = false;
        }
    }

    private void showPurchaseSucceeded()
    {
        int vipAdded = purchaseData.getInt("vip_points", 0);
        long vipCredits = purchaseData.getLong("vip_credits", 0);
        long baseCredits = purchaseData.getLong("premium_credits", 0);
        string packageKey = purchaseData.getString("popcorn_package_key_name", ""); // This ?might? contain the ID of the credits package we just purchased.
        long creditsAdded = purchaseData.getLong("credits", 0);

        // Percentages for BuyPage v3
        int bonusPercent = purchaseData.getInt("bonus_pct", 0);
        int saleBonusPercent = purchaseData.getInt("sale_bonus_pct", 0);
        int vipBonusPercent = purchaseData.getInt("vip_bonus_pct", 0);
        bool isJackpotEligible = false;
        Dict args = Dict.create(
            D.BONUS_CREDITS, vipCredits,
            D.TOTAL_CREDITS, creditsAdded,
            D.PACKAGE_KEY, packageKey,
            D.VIP_POINTS, vipAdded,
            D.DATA, purchaseData,
            D.BASE_CREDITS, baseCredits,
            D.BONUS_PERCENT, bonusPercent,
            D.SALE_BONUS_PERCENT, saleBonusPercent,
            D.VIP_BONUS_PERCENT, vipBonusPercent,
            D.IS_JACKPOT_ELIGIBLE, isJackpotEligible,
            D.TYPE, PurchaseFeatureData.Type.PRIZE_POP
        );
        
        SlotsPlayer.addCredits(creditsAdded, "purchase", true, false);
        BuyCreditsConfirmationDialog.showDialog(args);
    }

    public IEnumerator addPicksAndContinueBonusGame()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(extraPicksAnimationList));
        extraPicksLabel.text = CommonText.formatNumber(PrizePopFeature.instance.extraPicks);
        StartCoroutine(animateInExtraPicksPanel(true, false));
        if (outcome != null)
        {
            if (!challengePresenter.isGameActive)
            {
                startBonusGame(); //start game if current game is already ended
            }
            else
            {
                restartBonusInstantly = true; //start picks from new outcome as soon as current bonus ends
            }
        }

        purchaseButton.isEnabled = true; //Reenable button in case they bought picks to continue the bonus game and want to buy more
    }

    public override void purchaseCancelled()
    {
        purchaseButton.isEnabled = true; //Reenable in case they cancelled but want to purchase
    }
    
    public override void purchaseFailed(bool timedOut)
    {
        purchaseButton.isEnabled = true;
    }

    public void updateJackpotAmount()
    {
        currentJackpotLabel.text = CreditsEconomy.convertCredits(PrizePopFeature.instance.currentJackpot);
    }

    public override PurchaseSuccessActionType purchaseSucceeded(JSON data, PurchaseFeatureData.Type purchaseType)
    {
        if (purchaseType == PurchaseFeatureData.Type.PRIZE_POP)
        {
            //Need to override this so we can not auto-close this dialog after a successful purchase incase the player has picks to make
            purchaseData = data;
            needsToUpdatePicksFromPurchase = true;
            return PurchaseSuccessActionType.skipThankYouDialog;
        }

        return PurchaseSuccessActionType.closeDialog;
    }
}
