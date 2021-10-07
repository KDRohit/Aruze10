 using System.Collections;
 using UnityEngine;
 using System.Collections.Generic;
 using System.Linq;
 using Com.HitItRich.Feature.VirtualPets;
 using Com.Scheduler;
 using Zynga.Core.Util;

 public class RichPassFeatureDialog : DialogBase , IResetGame
{
    [SerializeField] private AnimationListController.AnimationInformationList upgradeToGoldAnimationList;

    [SerializeField] private RichPassRewardTrack rewardTrack;
    [SerializeField] private RichPassSeasonalChallengesDisplay seasonalChallengesDisplay;
    [SerializeField] private LabelWrapperComponent currentPointsLabel;
    [SerializeField] private ButtonHandler viewChangerButton; //Swap from challenges & reward views
    [SerializeField] private SlideController viewSlider;
    [SerializeField] private LabelWrapperComponent endDateLabel;
    [SerializeField] private LabelWrapperComponent goldPassPriceLabel;
    [SerializeField] private ButtonHandler infoButton;
    [SerializeField] private ButtonHandler buyGoldButton;
    [SerializeField] private ButtonHandler piggyBankButton;
    [SerializeField] private GameObject piggyBankParent;
    [SerializeField] private LabelWrapperComponent bankLabel;
    [SerializeField] private GameObject piggyBankLock;
    [SerializeField] private Transform piggyInfoParent;
    [SerializeField] private AnimationListController.AnimationInformationList piggyBankClaimAnimationList;
    [SerializeField] private ObjectSwapper passTypeSwapper;

    [SerializeField] private ObjectSwapper periodicChallengePanel;
    [SerializeField] private LabelWrapperComponent periodicChallengeLabel;
    [SerializeField] private LabelWrapperComponent nextPeriodicChallengeLabel;
    [SerializeField] private LabelWrapperComponent periodicChallengePointsLabel;
    [SerializeField] private LabelWrapperComponent periodicProgressText;
    [SerializeField] private UIMeterNGUI periodicProgressMeter;
    [SerializeField] private GameObject newSeasonalChallengesBadge;
    
    [SerializeField] private ObjectSwapper goldGameSwapper;
    [SerializeField] private UITexture goldGameRenderer;
    [SerializeField] private ClickHandler goldGameButton;
    [SerializeField] private ObjectSwapper goldGameFrameSwapper;
    
    private string previousTier = "";

    private string goldGameKey = "";

    private DIALOG_VIEWS currentView = DIALOG_VIEWS.REWARDS;
    private RichPassPiggyBankInfoDialog openBankInfoPage;

    private const string COMPLETE_STATE = "complete";
    private const string INPROGRESS_STATE = "inprogress";
    private const string INPROGRESS_PETS_STATE = "inprogress_pets";
    
    private const string GOLD_GAME_ON_STATE = "gold_game_on";
    private const string GOLD_GAME_OFF_STATE = "gold_game_off";

    private const string REWARDS_LOCALIZATION = "rewards";
    private const string CHALLENGES_LOCALIZATION = "challenges";
    private const string ENDS_LOCALIZATION = "ends_{0}";
    private const string DAILY_CHALLENGE_COMING_SOON_LOC = "rp_daily_challenge_coming_soon";

    private const string PIGGY_BANK_PATH = "Features/Rich Pass/Prefabs/Rich Pass Bank Info Dialog"; 
    public static RichPassFeatureDialog instance { get; private set; }    

    private const string SEEN_UNCLAIMED_PRIZES_PREF_ID = "rp_seen_unclaimed_prizes";
    static private int _seenUnclaimedPrizes = 0;
    static private PreferencesBase _unityPrefs = null;

    private static PreferencesBase UnityPrefs
    {
        get
        {
            if (_unityPrefs == null)
                _unityPrefs = SlotsPlayer.getPreferences();

            return _unityPrefs;
        }
        
    }

    private static string getSeenUnclaimedPreKey()
    {
        string preRichPassKey = "";
        if (CampaignDirector.richPass != null)
        { 
            preRichPassKey = CampaignDirector.richPass.timerRange.startTimestamp.ToString();
        }
        return preRichPassKey;
    }

    private static string seenUnclaimedKey()
    {
        string debugStr =  getSeenUnclaimedPreKey() + "_" + SEEN_UNCLAIMED_PRIZES_PREF_ID;
        return debugStr;
    }
    
    //static to allow caching of _seenUnclaimedPrizes and allowing calling of even when the dialog is not shown / alive
    static public int SeenUnclaimedPrizes
    {
        get
        {
            _seenUnclaimedPrizes = UnityPrefs.GetInt( seenUnclaimedKey(), 0);
            return _seenUnclaimedPrizes;
        }
        protected set
        {
            _seenUnclaimedPrizes = value;
            string preRicPassKey = getSeenUnclaimedPreKey();
            if (preRicPassKey != "")
            {
                UnityPrefs.SetInt(seenUnclaimedKey(), _seenUnclaimedPrizes);
                UnityPrefs.Save();
            }
        }
    }



    private enum DIALOG_VIEWS
    {
        REWARDS,
        CHALLENGES
    }

    private RichPassCampaign campaignToShow;

    public override void init()
    {
        campaignToShow = (RichPassCampaign) dialogArgs.getWithDefault(D.CAMPAIGN_NAME, null);

        instance = this;

        if (campaignToShow == null)
        {
            Dialog.close();
        }
        else
        {
            Audio.play("DialogueOpenRichPass");
            passTypeSwapper.setState(campaignToShow.passType);
            currentPointsLabel.text = CommonText.formatNumber(campaignToShow.pointsAcquired);
            RichPassCampaign.RewardTrack silverTrack = campaignToShow.silverTrack;
            RichPassCampaign.RewardTrack goldTrack = campaignToShow.goldTrack;
            rewardTrack.init(campaignToShow.pointsAcquired, campaignToShow.allRewardKeys, silverTrack, goldTrack);
            seasonalChallengesDisplay.init(campaignToShow.seasonMissions, downloadedTextures, campaignToShow.lockedSeasonMissions);
            seasonalChallengesDisplay.gameObject.SetActive(false);
            setPeriodicChallengeDisplay();

            viewChangerButton.registerEventDelegate(viewChangeClicked);
            viewSlider.onEndAnimation += stateSlideComplete;
            seasonalChallengesDisplay.challengesSlider.onEndAnimation += challengesSlideComplete;

            if (campaignToShow.timerRange.timeRemaining < Common.SECONDS_PER_DAY)
            {
                //Show actual amount of time remaining if less than 24 hours
                endDateLabel.text = Localize.text("ends_in");
                campaignToShow.timerRange.registerLabel(endDateLabel.tmProLabel, keepCurrentText:true);
            }
            else
            {
                endDateLabel.text = Localize.text(ENDS_LOCALIZATION, campaignToShow.timerRange.endDate.ToShortDateString());
            }
            RichPassPackage goldPackage = CampaignDirector.richPass.getCurrentPackage();
            string goldPassCost = goldPackage != null && goldPackage.purchasePackage != null ? goldPackage.purchasePackage.getLocalizedPrice() : "";
            goldPassPriceLabel.text = string.Format("Gold {0}", goldPassCost);
            bankLabel.text = CreditsEconomy.convertCredits(campaignToShow.bankCoins);
            infoButton.registerEventDelegate(infoClicked);
            buyGoldButton.registerEventDelegate(buyGoldClicked);
            piggyBankButton.registerEventDelegate(piggyBankClicked);
            piggyBankParent.SetActive(campaignToShow.bankCoins > 0);
            piggyBankLock.SetActive(!campaignToShow.isPurchased());
            setUpGoldGame();

            setViewState(campaignToShow.getNumberOfUnclaimedRewards(false) > 0 ? DIALOG_VIEWS.REWARDS : DIALOG_VIEWS.CHALLENGES);

            bool forceUpgradeAnimation = (bool)dialogArgs.getWithDefault(D.MODE, false);
            if (forceUpgradeAnimation)
            {
                upgradeDialog();
            }
        }
    }
    
    public static void resetStaticClassData()
    {
        _seenUnclaimedPrizes = 0;
        _unityPrefs = null;
    }
    
    private void setUpGoldGame()
    {
        goldGameKey = (string)dialogArgs.getWithDefault(D.GAME_KEY, "");
        if (!string.IsNullOrEmpty(goldGameKey))
        {
            goldGameSwapper.setState(GOLD_GAME_ON_STATE);
            goldGameFrameSwapper.setState(campaignToShow.isPurchased() ? "unlocked" : "locked");
            Material gameMaterial = new Material(goldGameRenderer.material);
            gameMaterial.mainTexture = getDownloadedTexture(0);
            goldGameRenderer.material = gameMaterial;
            goldGameButton.registerEventDelegate(goldGameClicked);
        }
    }

    private void goldGameClicked(Dict args = null)
    {
        if (campaignToShow.isPurchased())
        {
            // Load the game right now.
            // Tell the lobby which game to launch when finished returning to the lobby.
            PreferencesBase prefs = SlotsPlayer.getPreferences();
            prefs.SetString(Prefs.AUTO_LOAD_GAME_KEY, goldGameKey);
            prefs.Save();

            SlotAction.setLaunchDetails("rich_pass");

            if (GameState.isMainLobby)
            {
                // Refresh the lobby if already in it during game unlock,
                // so the unlocked game will appear unlocked, and so we
                // actually launch into that game
                Scheduler.addFunction(MainLobby.refresh);
            }
            else
            {
                // Currently in a game.
                // First go back to the lobby and go through the common route to launching a game.
                Scheduler.addFunction(LobbyLoader.returnToLobbyAfterDialogCloses);
            }
		
            Dialog.close();
        }
        else
        {
            RichPassUpgradeToGoldDialog.showDialog("gold_game", SchedulerPriority.PriorityType.IMMEDIATE);
        }
    }

    private void setViewState(DIALOG_VIEWS view)
    {
        currentView = view;
        StatsRichPass.logFeatureDialogStateView(currentView.ToString().ToLower());
        switch (view)
        {
            case DIALOG_VIEWS.CHALLENGES:
                viewSlider.safleySetYLocation(1200);
                seasonalChallengesDisplay.gameObject.SetActive(true);
                rewardTrack.gameObject.SetActive(false);
                viewChangerButton.label.text = Localize.text(REWARDS_LOCALIZATION);
                seasonalChallengesDisplay.challengesSlider.enableScrolling();
                break;
            
            case DIALOG_VIEWS.REWARDS:
                viewSlider.safleySetYLocation(0);
                seasonalChallengesDisplay.gameObject.SetActive(false);
                rewardTrack.gameObject.SetActive(true);
                rewardTrack.rewardsSlideController.enableScrolling();
                viewChangerButton.label.text = Localize.text(CHALLENGES_LOCALIZATION);
                newSeasonalChallengesBadge.SetActive(campaignToShow.hasNewChallenges && !campaignToShow.hasNewPass); //Don't show the new badge when the whole pass is brand new
                break;
        }
    }

    private void stateSlideComplete(Dict args = null)
    {
        viewSlider.enabled = false;
        StatsRichPass.logFeatureDialogStateView(currentView.ToString().ToLower());
        if (currentView == DIALOG_VIEWS.CHALLENGES)
        {
            seasonalChallengesDisplay.challengesSlider.enableScrolling();
            rewardTrack.gameObject.SetActive(false);
        }
        else
        {
            rewardTrack.rewardsSlideController.enableScrolling();
            seasonalChallengesDisplay.gameObject.SetActive(false);
        }
    }

    private void challengesSlideComplete(Dict args = null)
    {
        viewSlider.scrollToAbsoluteVerticalPosition(0, -50f, isForced:true);
    }

    private void viewChangeClicked(Dict args = null)
    {
        viewSlider.enabled = true;
        if (currentView == DIALOG_VIEWS.REWARDS)
        {
            currentView = DIALOG_VIEWS.CHALLENGES;

            Audio.play("ButtonChallengesRichPass");
            newSeasonalChallengesBadge.SetActive(false); //Don't show the badge here anymore once being clicked on
            seasonalChallengesDisplay.challengesSlider.preventScrolling();
            rewardTrack.rewardsSlideController.preventScrolling();
            viewSlider.scrollToAbsoluteVerticalPosition(1200, 50f, isForced:true);
            seasonalChallengesDisplay.gameObject.SetActive(true);
            viewChangerButton.label.text = Localize.text(REWARDS_LOCALIZATION);
            StatsRichPass.logFeatureDialogStateClick("challenges");
        }
        else
        {
            currentView = DIALOG_VIEWS.REWARDS;

            Audio.play("ButtonRewardsRichPass");
            seasonalChallengesDisplay.challengesSlider.preventScrolling();
            rewardTrack.rewardsSlideController.preventScrolling();
            if (seasonalChallengesDisplay.challengesSlider.isActiveAndEnabled) //Might be deactivated if there aren't enough challenges to have slide enabled
            {
                seasonalChallengesDisplay.challengesSlider.scrollToAbsoluteVerticalPosition(0, -100f, isForced:true);
            }
            else
            {
                challengesSlideComplete();
            }
            
            rewardTrack.gameObject.SetActive(true);
            viewChangerButton.label.text = Localize.text(CHALLENGES_LOCALIZATION);
            StatsRichPass.logFeatureDialogStateClick("rewards");
        }
    }

    private void infoClicked(Dict args = null)
    {
        Audio.play("ButtonConfirm");
        StatsRichPass.logInfoClick();
        campaignToShow.showVideo(topOfList: true);
    }

    public override void onCloseButtonClicked(Dict args = null)
    {
        base.onCloseButtonClicked(args);
        Audio.play("ButtonConfirm");
        SeenUnclaimedPrizes = campaignToShow.getNumberOfUnclaimedRewards();
    }

    private void buyGoldClicked(Dict args = null)
    {
        Audio.play("ButtonBuyRichPass");
        StatsRichPass.logFeatureDialogGoldUpgrade();
        CampaignDirector.richPass.purchasePackage();
    }

    public void upgradeDialog()
    {
        StartCoroutine(playUpgradeAnimations());
    }

    private IEnumerator playUpgradeAnimations()
    {
        yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(upgradeToGoldAnimationList));
        //Unlock all previously locked gold things that can be unlocked
        passTypeSwapper.setState(campaignToShow.passType);
        rewardTrack.unlockGoldRewards();
        seasonalChallengesDisplay.upgradeToGold();
        piggyBankLock.SetActive(false);
        goldGameFrameSwapper.setState("unlocked");
    }
    
    private void piggyBankClicked(Dict args = null)
    {
        AssetBundleManager.load(PIGGY_BANK_PATH, piggyBankLoadSuccess, piggyBankLoadFailed, isSkippingMapping:true, fileExtension:".prefab");
    }

    private void piggyBankLoadSuccess(string path, Object obj, Dict args)
    {
        GameObject piggyBank = NGUITools.AddChild(piggyInfoParent, obj as GameObject);
        Vector3 animPos = Dialog.getAnimPos(type.getAnimInPos(), piggyBank);
        piggyBank.transform.localPosition = new Vector3(0, animPos.y, 0);
        openBankInfoPage = piggyBank.GetComponent<RichPassPiggyBankInfoDialog>();
        SlideController activeSlideController = currentView == DIALOG_VIEWS.REWARDS ? rewardTrack.rewardsSlideController : seasonalChallengesDisplay.challengesSlider;
        openBankInfoPage.init(activeSlideController, campaignToShow, type);
    }
    
    private void piggyBankLoadFailed(string path, Dict args)
    {
        Debug.LogWarning("Piggy bank failed to load:" + path);
    }

    public void setPeriodicChallengeDisplay()
    {
        Objective objectiveToDisplay = campaignToShow.getCurrentPeriodicObjective();
        if (!campaignToShow.periodChallengesEnd.isExpired && objectiveToDisplay != null && !objectiveToDisplay.isComplete)
        {
            periodicChallengePointsLabel.text = CommonText.formatNumber(objectiveToDisplay.getRewardAmount(ChallengeReward.RewardType.PASS_POINTS));
            periodicChallengeLabel.text = objectiveToDisplay.type == XinYObjective.X_COINS_IN_Y ? objectiveToDisplay.getInProgressText() : objectiveToDisplay.description;

            periodicProgressMeter.setState(objectiveToDisplay.currentAmount, objectiveToDisplay.amountNeeded);

            if (objectiveToDisplay.amountNeeded < 1)
            {
                Debug.LogError("Amount needed was less than 1.");
            }

            int percent = objectiveToDisplay.amountNeeded > 0 ? Mathf.RoundToInt((100 * objectiveToDisplay.currentAmount) / objectiveToDisplay.amountNeeded) : 0; //If amountNeeded is zero, we have a division by zero exception. Instead of crashing, we'll make the percent zero in this case.
            percent = Mathf.Min(percent, 100); //If we've completed the challenge and we're over 100%, only display 100%
            periodicProgressText.text = Localize.text("{0}_percent", CommonText.formatNumber(percent));
            bool petsEnabled = VirtualPetsFeature.instance != null && VirtualPetsFeature.instance.isEnabled;
            periodicChallengePanel.setState(petsEnabled ? INPROGRESS_PETS_STATE : INPROGRESS_STATE);
        }
        else
        {
            periodicChallengePanel.setState(COMPLETE_STATE);
            if (!campaignToShow.periodChallengesEnd.isExpired && campaignToShow.periodChallengesEnd.endTimestamp < campaignToShow.timerRange.endTimestamp) //Make sure the next periodic challenge would actually start before the campaign ends
            {
                campaignToShow.periodChallengesEnd.registerLabel(nextPeriodicChallengeLabel.tmProLabel, keepCurrentText: true);
            }
            else
            {
                nextPeriodicChallengeLabel.text = Localize.text(DAILY_CHALLENGE_COMING_SOON_LOC); //Generic text for when a periodic challenge isn't configured and we don't know when the next one starts
            }
        }
    }

    public override void close()
    {
        if (campaignToShow != null)
        {
            campaignToShow.periodChallengesEnd.removeLabel(nextPeriodicChallengeLabel.tmProLabel);
            campaignToShow.timerRange.removeLabel(endDateLabel.tmProLabel);
            campaignToShow.markChallengesSeen();
        }

        instance = null;
    }

    public static void showDialog(RichPassCampaign campaignToShow, CloseDelegate closeDelegate)
    {
        showDialog(campaignToShow, SchedulerPriority.PriorityType.LOW,0,"",false,closeDelegate);   
    }
    
    
    public static void showDialog(RichPassCampaign campaignToShow, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW, long bankGrant = 0, string eventId = "", bool forceUpgradeAnimation = false, CloseDelegate closeDelegate = null)
    {
        List<string> gameOptionImages = new List<string>();
        string goldGameToShow = "";
        if (campaignToShow != null)
        {
            if (RichPassCampaign.goldGameKeys.Count > 0)
            {
                goldGameToShow = RichPassCampaign.goldGameKeys.Contains(LoLa.priorityGameKey) ? 
                    LoLa.priorityGameKey :
                    RichPassCampaign.goldGameKeys.Count > 0 ? RichPassCampaign.goldGameKeys.First() : "";
                LobbyGame goldGame = LobbyGame.find(goldGameToShow);
                if (goldGame != null)
                {
                    gameOptionImages.Add(SlotResourceMap.getLobbyImagePath(goldGame.groupInfo.keyName, goldGame.keyName));
                }
            }
            
            foreach (KeyValuePair<int, List<Mission>> kvp in campaignToShow.seasonMissions)
            {
                for (int missionIndex = 0; missionIndex < kvp.Value.Count; missionIndex++)
                {
                    for (int objectiveIndex = 0;
                        objectiveIndex < kvp.Value[missionIndex].objectives.Count;
                        objectiveIndex++)
                    {
                        Objective objective = kvp.Value[missionIndex].objectives[objectiveIndex];
                        string gameKey = objective.game;
                        if (!string.IsNullOrEmpty(objective.game))
                        {
                            LobbyGame gameInfo = null;
                            gameInfo = LobbyGame.find(gameKey);
                            if (gameInfo != null)
                            {
                                gameOptionImages.Add(SlotResourceMap.getLobbyImagePath(gameInfo.groupInfo.keyName,
                                    gameInfo.keyName));
                            }
                            else
                            {
                                Debug.LogError("No game info for objective index: " + objectiveIndex);
                                continue;
                            }
                        }
                        else if (objective.type == Objective.PACKS_COLLECTED ||
                                 objective.type == Objective.CARDS_COLLECTED)
                        {
                            gameOptionImages.Add("robust_challenges/collections pack challenge card 1x1");
                        }

                        else if (Objective.addGameOptionImage(objective, gameOptionImages))
                        {
                            //Objective.addGameOptionImage will load a new path into gameOptionImages if a match was found, otherwise it will return false.
                        }
                        else { gameOptionImages.Add("robust_challenges/generic hir win challenge card 1x1"); }

                    }
                }
            }
        }

        Dialog.instance.showDialogAfterDownloadingTextures("rich_pass_dialog", null, Dict.create(D.CAMPAIGN_NAME, campaignToShow, D.GAME_KEY, goldGameToShow, D.MODE, forceUpgradeAnimation,D.CLOSE,closeDelegate), nonMappedBundledTextures:gameOptionImages.ToArray(), priorityType:priority);
        
        if (!AssetBundleManager.isBundleCached("features/common_chests"))
        {
            AssetBundleManager.downloadAndCacheBundle("features/common_chests", skipMapping:true);
        }
    }

    public void claimPiggyBankAward(PassReward piggyBankAward)
    {
        piggyBankParent.SetActive(true);
        long oldBankAmount = campaignToShow.bankCoins;
        if (piggyBankAward.type == ChallengeReward.RewardType.BASE_BANK)
        {
            campaignToShow.bankCoins += piggyBankAward.amount;
        }
        else if (piggyBankAward.type == ChallengeReward.RewardType.BANK_MULTIPLIER)
        {
            campaignToShow.bankCoins *= piggyBankAward.amount;
        }
        else
        {
            Debug.LogWarning("Skipping since the given award is not a bank award");
            return;
        }
        
        StartCoroutine(SlotUtils.rollup(oldBankAmount, campaignToShow.bankCoins, bankLabel, true, 2.0f, false, rollupOverrideSound:"RollupRichPass", rollupTermOverrideSound:"RollupTermRichPass"));
        StartCoroutine(AnimationListController.playListOfAnimationInformation(piggyBankClaimAnimationList));
    }

    protected override void onHide()
    {
        base.onHide();
        previousTier = campaignToShow.passType;
    }

    protected override void onShow()
    {
        base.onShow();
        if (previousTier != campaignToShow.passType && campaignToShow.isPurchased())
        {
            StartCoroutine(playUpgradeAnimations());

            if (openBankInfoPage != null)
            {
                Destroy(openBankInfoPage.gameObject);
                SlideController activeSlideController = currentView == DIALOG_VIEWS.REWARDS ? rewardTrack.rewardsSlideController : seasonalChallengesDisplay.challengesSlider;
                activeSlideController.enableScrolling();
            }
        }
    }
}
