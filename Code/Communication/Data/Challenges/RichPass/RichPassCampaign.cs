using System.Collections.Generic;
using System.Linq;
using Com.Scheduler;
using UnityEngine;

public class RichPassCampaign : SeasonalCampaign, IResetGame
{
    public static readonly HashSet<string> goldGameKeys = new HashSet<string>();
    public static readonly HashSet<string> silverGameKeys = new HashSet<string>();
    
    private const string CHALLENGE_COMPLETE_EVENT = "rp_challenge_complete";
    private const string PROGRESS_EVENT = "rp_challenge_progress";
    private const string CHALLENGE_RESET_EVENT = "rp_challenge_reset";
    public const string BANK_REWARD_EVENT = "rp_bank_reward";
    private const string INFO_EVENT = "rich_pass_info";
    private const string CHALLENGE_GROUP_UNLOCK_EVENT = "rp_challenge_group_unlock";
    private const string REWARD_NODE_UNLOCK_EVENT = "rp_reward_node_unlock";
    private const string REPEAT_NODE_UNLOCK_EVENT = "rp_repeatable_reward_node_unlock";

    public const string IN_GAME_PREFAB_PATH = "Features/Rich Pass/Prefabs/Rich Pass In Game Panel Item";
    private const string DEFAULT_DIALOG_BG = "Features/Rich Pass/Textures/Default Dialog Backgrounds/richpass_default_coins_only";
    private const string BUNDLE_NAME = "richpass/main_dialog";

    public delegate void RichPassObjectiveCompleteDelegate(Mission mission, Objective objective);

    private RichPassObjectiveCompleteDelegate onObjectiveCompleted;
    

    public delegate void passTypeChangeFunc(string newType);
    public passTypeChangeFunc onPassTypeChanged;
    
    public class RewardTrack
    {
        public string name { get; private set; }
        private Dictionary<long, List<PassReward>> items;

        public RewardTrack(string passType)
        {
            name = passType;
            items = new Dictionary<long, List<PassReward>>();
        }

        public void addItem(long pointValue, PassReward reward)
        {   
            List<PassReward> itemList = null;
            if (!items.TryGetValue(pointValue, out itemList))
            {
                items[pointValue] = new List<PassReward>();
            }
            items[pointValue].Add(reward);
        }

        public List<long> getAllPointValues()
        {
            if (items == null)
            {
                return null;
            }

            return new List<long>(items.Keys);
        }

        public void addRange(long pointValue, List<PassReward> rewards)
        {
            List<PassReward> itemList = null;
            if (!items.TryGetValue(pointValue, out itemList))
            {
                items[pointValue] = new List<PassReward>();
            }
            items[pointValue].AddRange(rewards);
        }
        
        public List<PassReward> getAllRewardsForPointTotal(long pointTotal)
        {
            return getAllRewardsInRange(0, pointTotal);
        }

        public PassReward getReward(long pointTotal, int id)
        {
            foreach (long pointValue in items.Keys)
            {
                if (pointValue == pointTotal)
                {
                    if (items[pointValue] == null)
                    {
                        Debug.LogWarning("No rewards found for point total: " + pointValue);
                        continue;
                    }

                    for (int i = 0; i < items[pointValue].Count; i++)
                    {
                        if (items[pointValue][i].id == id)
                        {
                            return items[pointValue][i];
                        }
                    }
                    
                }
            }

            return null;
        }
        
        public List<PassReward> getAllRewardsInRange(long minPoints, long maxPoints)
        {
            List<PassReward> rewards = new List<PassReward>();
            foreach (long pointValue in items.Keys)
            {
                if (pointValue <= maxPoints && pointValue >= minPoints)
                {
                    rewards.AddRange(items[pointValue]);
                }
            }

            return rewards;
        }

        public List<PassReward> getSingleRewardsList(long points)
        {
            List<PassReward> rewards;
            if (items.TryGetValue(points, out rewards))
            {
                return rewards;
            }

            return null;
        }

        public void claimReward(int id, long points)
        {
            if (points > CampaignDirector.richPass.maximumPointsRequired)
            {
                RichPassAction.claimRepeatableReward(name, id, points-CampaignDirector.richPass.maximumPointsRequired);
            }
            else
            {
                RichPassAction.claimReward(name, id, points);
            }
        }
    }
    
    public long pointsAcquired { get; private set; }
    public long maximumPointsRequired { get; private set; }
    public long repeatableRewardsPointsRequired { get; private set; }
    public long maxRepeatableRewards { get; private set; }

    public string passType { get; private set; }
    private Dictionary<string, RewardTrack> passRewards;
    private Dictionary<string, JSON> claimableRewardEvents;
    private List<long> _allRewardKeys;
    private PassReward bankReward;
    private string bankRewardEventId;
    public long bankCoins { get; set; }
    private int latestSeasonalUnlockDate = -1;
    public bool hasNewChallenges { get; private set; }
    public bool hasNewPass{ get; private set; }
    public bool hasActivePiggybank{ get; private set; }
    public long finalPiggyBankValue { get; private set; }
    
    public static bool isLevelLocked()
    {
        return !EueFeatureUnlocks.isFeatureUnlocked("rich_pass");
    }

    public List<long> allRewardKeys
    {
        get
        {
            return new List<long>(_allRewardKeys);
        }
    }

    public RewardTrack silverTrack
    {
        get { return getRewardTrack("silver"); }
    }

    public RewardTrack goldTrack
    {
        get { return getRewardTrack("gold"); }
    }
    
    private RewardTrack getRewardTrack(string name)
    {
        if (passRewards == null)
        {
            Debug.LogError("Not initialized");
            return null;
        }
        
        RewardTrack track = null;
        if (!passRewards.TryGetValue(name, out track))
        {
            Debug.LogError("Can't find reward track: " + name);
            return null;
        }
        return track;
    }
    protected override void registerGame(string gameKey)
    {
        //Gold game keys get populated from s3 data thtat gets parsed before this class is instantiated
        //if this game is not a gold game add it to the silver game dictionary.  We will remove games from this dictionary if 
        //lobby data comes in and tells us it's a gold game.
        if (!goldGameKeys.Contains(gameKey) && !silverGameKeys.Contains(gameKey))
        {
            silverGameKeys.Add(gameKey);
        }
        
    }

    public void getClosestRewards(out long previousReward, out long nextReward)
    {
        List<long> allKeys = null;
        previousReward = 0;
        nextReward = 0;
        if (isPurchased())
        { 
            allKeys = allRewardKeys; //copy the list -- getter makes a copy
        }
        else
        {
            //just get silver keys
            allKeys = silverTrack.getAllPointValues();
        }

        if (allKeys != null)
        {
            allKeys.Sort();
            for (int i = 0; i < allKeys.Count; i++)
            {
                if (allKeys[i] > pointsAcquired)
                {
                    nextReward = allKeys[i];
                    return;
                }
                else
                {
                    previousReward = allKeys[i];
                }
            }
        }
    }
    
    public override bool isLobbyValid(LoLaLobby lobby = null)
    {
        //Rich pass doesn't depend on games being in a lobby so always return true
        lobbyValidState = ChallengeEvalState.VALID;
        return true;
    }

    public static bool isRichPassGame(string gameKey)
    {
        return goldGameKeys.Contains(gameKey) || silverGameKeys.Contains(gameKey);
    }

    public static bool isSilverGame(string gameKey)
    {
        return silverGameKeys.Contains(gameKey);
    }

    public static bool isGoldGame(string gameKey)
    {
        return goldGameKeys.Contains(gameKey);
    }
    
    public void registerForObjectiveComplete(RichPassObjectiveCompleteDelegate funcPointer)
    {
        onObjectiveCompleted -= funcPointer;
        onObjectiveCompleted += funcPointer;
    }

    public void unregisterForObjectiveComplete(RichPassObjectiveCompleteDelegate funcPointer)
    {
        onObjectiveCompleted -= funcPointer;
    }
    
    public void purchasePackage()
    {
        RichPassPackage package = getCurrentPackage();
        if (package != null && package.purchasePackage != null)
        {
            logPurchase();
            package.purchasePackage.makePurchase(0,false, -1, "RichPassPackage");
        }
    }

    public RichPassPackage getCurrentPackage()
    {
        PurchaseFeatureData data = PurchaseFeatureData.RichPass;
        if (data != null && data.richPassPackages != null && data.richPassPackages.Count >= 1)
        {
            for (int i = 0; i < data.richPassPackages.Count; i++)
            {
                if (data.richPassPackages[i].purchasePackage != null)
                {
                    if (data.richPassPackages[i].purchasePackage.keyName == ExperimentWrapper.RichPass.package)
                    {
                        return data.richPassPackages[i];
                    }
                }
            }
        }

        return null;
    }

    private void logPurchase()
    {
        //Currently no stats are being logged.
    }

    public bool isPurchased()
    {
        return "gold" == passType;
    }
    

    public override void init(JSON data)
    {
        base.init(data);
        
        //enable based on experiment
        isEnabled = isEnabled && ExperimentWrapper.RichPass.isInExperiment && !isLevelLocked();

        pointsAcquired = data.getLong("pass_points", 0);
        passType = data.getString("pass_type", "silver");

        passRewards = new Dictionary<string, RewardTrack>();
        JSON rewards = data.getJSON("reward_track");

        List<string> keys = rewards.getKeyList();
        _allRewardKeys = new List<long>(keys.Count);
        for (int i=0; i<keys.Count; i++)
        {
            long points = 0;
            if (!long.TryParse(keys[i], out points))
            {
                Bugsnag.LeaveBreadcrumb("Invalid point value for rich pass reward");
                continue;
            };
            
            maximumPointsRequired = Mathf.Max((int)points, (int)maximumPointsRequired);
            bool hasActiveRewards = parseReward(points, rewards.getJSON(keys[i]));

            if (i == 0 && points != 0)
            {
                _allRewardKeys.Add(0); //Inject 0 to the start of the list to have a 0 starting node in the reward track
            }

            if (hasActiveRewards)
            {
                _allRewardKeys.Add(points);
            }
        }
        
        _allRewardKeys.Sort();
        calculateFinalBankValue();
        bankCoins = data.getLong("bank_coins", 0);
        
        //Setup the end of content, repeatable chest reward
        JSON repeatableRewardsData = data.getJSON("repeatable_rewards");
        if (repeatableRewardsData != null)
        {
            parseRepeatRewards(repeatableRewardsData);
        }

        checkForNewPassAndSeasonalChallenges();
        periodChallengesEnd.registerFunction(refreshChallenges);

        timerRange.registerFunction(onEventEnd);
    }
    
    private void calculateFinalBankValue()
    {
        for (int i = 0; i < _allRewardKeys.Count; i++)
        {
            updateFinalBankValue(_allRewardKeys[i], silverTrack);
            updateFinalBankValue(_allRewardKeys[i], goldTrack);
        }
    }

    private void updateFinalBankValue(long points, RewardTrack track)
    {
        List<PassReward> trackRewards = track.getSingleRewardsList(points);
        if (trackRewards != null && trackRewards.Count > 0)
        {
            for (int i = 0; i < trackRewards.Count; i++)
            {
                if (trackRewards[i].type == ChallengeReward.RewardType.BASE_BANK)
                {
                    finalPiggyBankValue += trackRewards[i].amount;
                }
                else if (trackRewards[i].type == ChallengeReward.RewardType.BANK_MULTIPLIER)
                {
                    finalPiggyBankValue *= trackRewards[i].amount;
                } 
            }
        }
    }

    public void onEventEnd(Dict args = null, GameTimerRange caller = null)
    {
        isEnabled = false;
        //Hide in the in-game UI
        InGameFeatureContainer.removeObjectsOfType("rich_pass");

        //Close the dialog if its open
        if (Dialog.instance.currentDialog != null && Dialog.instance.currentDialog.type.keyName == "rich_pass_dialog")
        {
            Dialog.close();
        }

        //Remove any queued dialogs
        if (Scheduler.hasTaskWith("rich_pass_dialog"))
        {
            Scheduler.removeDialog("rich_pass_dialog");
        }

        
        //Remove any queued or open upgrade dialogs
        List<DialogBase> upgradeDialogs = Dialog.instance.findOpenDialogsOfType("rich_pass_upgrade_to_gold_dialog");
        for (int i = 0; i < upgradeDialogs.Count; i++)
        {
            Dialog.close(upgradeDialogs[i]);
        }
        
        if (Scheduler.hasTaskWith("rich_pass_upgrade_to_gold_dialog"))
        {
            Scheduler.removeDialog("rich_pass_upgrade_to_gold_dialog");
        }

        //Remove the gold game from our lobby options list for the main lobby
        bool goldGameRemoved = removeGoldGame();

        removeRichPassUnlocks();
        
        //If we're in the main lobby, hide the rich pass button and replace it with weekly race
        refreshMainLobby(goldGameRemoved);
    }

    private bool removeGoldGame()
    {
        if (goldGameKeys != null && goldGameKeys.Count > 0)
        {
            LobbyInfo mainLobbyInfo = LobbyInfo.find(LobbyInfo.Type.MAIN);

            foreach(string gameKey in goldGameKeys)
            {
                //remove game from main lobby
                LobbyGame game = LobbyGame.find(gameKey);
                LobbyOption goldOption = LobbyOption.activeGameOptionInLobby(game, LobbyInfo.Type.MAIN);
                mainLobbyInfo.removeLobbyOption(goldOption);
            }
            
            //remove free spins from inbox
            InboxInventory.removeGoldPassSpins();

            mainLobbyInfo.organizeOptions();
            return true;
        }

        return false;
    }

    private void refreshMainLobby(bool gamesRemoved)
    {
        if (MainLobby.instance != null)
        {
            MainLobbyBottomOverlay.instance.initializeRaces();
            Overlay.instance.topV2.hideWeeklyRaceButton();
            MainLobbyBottomOverlay.instance.refreshUI();
            if (gamesRemoved)
            {
                MainLobby.refresh(null);
            }
        }
    }

    private void removeRichPassUnlocks()
    {
        if (silverGameKeys != null && silverGameKeys.Count > 0)
        {
            foreach(string gameKey in silverGameKeys)
            {
                if (string.IsNullOrEmpty(gameKey))
                {
                    continue;
                }
                
                //remove game from main lobby
                LobbyGame game = LobbyGame.find(gameKey);
                if (game != null)
                {
                    game.removeRichPassUnlock();        
                }
            }
        }
    }

    private void parseRepeatRewards(JSON repeatableRewardsData)
    {
        repeatableRewardsPointsRequired = repeatableRewardsData.getLong("pass_points", 0);
        maxRepeatableRewards = repeatableRewardsData.getInt("max_rewards_count", 0);
            
        JSON repeatRewards = repeatableRewardsData.getJSON("rewards");
        if (repeatRewards != null)
        {
            List<string> repeatRewardKeys = repeatRewards.getKeyList();
            for (int i=0; i<repeatRewardKeys.Count; i++)
            {
                long points = 0;
                if (!long.TryParse(repeatRewardKeys[i], out points))
                {
                    Bugsnag.LeaveBreadcrumb("Invalid point value for rich pass reward");
                    continue;
                };
                points += maximumPointsRequired; //The point requirements here are meant to be post-regular reward track requirements
                parseReward(points, repeatRewards.getJSON(repeatRewardKeys[i]));
            }
        }
    }

    private void refreshChallenges(Dict args = null, GameTimerRange parentTimer = null)
    {
        if (RichPassFeatureDialog.instance != null && RichPassFeatureDialog.instance.gameObject != null)
        {
            RichPassFeatureDialog.instance.setPeriodicChallengeDisplay();
        }
        RichPassAction.refreshChallenges();
    }

    private void checkForNewPassAndSeasonalChallenges()
    {
        int lastUnlockDate = getLatestSeasonalDate();
        int lastSeenDate = CustomPlayerData.getInt(CustomPlayerData.LAST_SEEN_RICH_PASS_CHALLENGES_TIME, 0);
        if (lastUnlockDate > lastSeenDate)
        {
            enableNewChallenges(lastUnlockDate);
        }
        
        int lastSeenPass = CustomPlayerData.getInt(CustomPlayerData.LAST_SEEN_RICH_PASS_START_TIME, 0);
        if (lastSeenPass < timerRange.startTimestamp)
        {
            CustomPlayerData.setValue(CustomPlayerData.LAST_SEEN_RICH_PASS_START_TIME, timerRange.startTimestamp);
            hasNewPass = true;
        }
    }

    private void enableNewChallenges(int newDate)
    {
        CustomPlayerData.setValue(CustomPlayerData.LAST_SEEN_RICH_PASS_CHALLENGES_TIME, newDate);
        hasNewChallenges = true;
    }
    
    
    
    public override void registerEvents()
    {
        Server.registerEventDelegate(PROGRESS_EVENT, onProgressUpdate, true);
        Server.registerEventDelegate(CHALLENGE_RESET_EVENT, onProgressReset, true);
        Server.registerEventDelegate(CHALLENGE_COMPLETE_EVENT, onRPChallengeComplete, true);
        Server.registerEventDelegate(REWARD_NODE_UNLOCK_EVENT, onRewardNodeUnlocked, true);
        Server.registerEventDelegate(CHALLENGE_GROUP_UNLOCK_EVENT, onChallengeGroupUnlocked, true);
        Server.registerEventDelegate(INFO_EVENT, onRPGetInfo, true);
        Server.registerEventDelegate(REPEAT_NODE_UNLOCK_EVENT, onRewardNodeUnlocked, true);
    }
	
    public override void unregisterEvents()
    {
        Server.unregisterEventDelegate(PROGRESS_EVENT, onProgressUpdate, true);
        Server.unregisterEventDelegate(CHALLENGE_RESET_EVENT, onProgressReset);
        Server.unregisterEventDelegate(CHALLENGE_COMPLETE_EVENT, onRPChallengeComplete, true);
        Server.unregisterEventDelegate(REWARD_NODE_UNLOCK_EVENT, onRewardNodeUnlocked, true);
        Server.unregisterEventDelegate(CHALLENGE_GROUP_UNLOCK_EVENT, onChallengeGroupUnlocked, true);
        Server.unregisterEventDelegate(INFO_EVENT, onRPGetInfo, true);
        Server.unregisterEventDelegate(REPEAT_NODE_UNLOCK_EVENT, onRewardNodeUnlocked, true);
    }

    public override void onProgressReset(JSON response)
    {
        base.onProgressReset(response);
        InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.RICH_PASS_KEY, Dict.create(D.OPTION, true));
    }

    public override void fullReset()
    {
        base.fullReset();
        expireGoldPass();
        pointsAcquired = 0;
        resetRewardTrack(silverTrack);
        resetRewardTrack(goldTrack);
    }

    private void resetRewardTrack(RewardTrack track)
    {
        List<PassReward> rewards = track.getAllRewardsForPointTotal(System.Int64.MaxValue);
        if (rewards != null)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i] == null)
                {
                    continue;
                }
                rewards[i].reset();
            }  
        }
    }

    private void onRPChallengeComplete(JSON data)
    {
        string challengeType = data.getString("challenge_type", "");
        int challengeStartTime = data.getInt("challenge_start_time", 0);
        int group_id = data.getInt("group_id", 0); //Corresponds to mission ID
        int id = data.getInt("id", 0); //Corresponds to objective id withing the mission
        int[] rewardsGranted = data.getIntArray("rewards_granted_ids");

        SortedDictionary<int, List<Mission>> allMissions = null;
        
        switch (challengeType)
        {
            case "seasonal":
                allMissions = seasonMissions;
                break;
            
            case "periodic":
                allMissions = periodicMissions;
                break;
        }

        if (allMissions == null)
        {
            Debug.LogError("Invalid mission type");
            return;
        }

        List<Mission> specificMissions = null;
        if (!allMissions.TryGetValue(challengeStartTime, out specificMissions))
        {
            Debug.LogError("Can't find mission group"); 
            return;
        }

        for (int i = 0; i < specificMissions.Count; i++)
        {
            SeasonMission theMission = specificMissions[i] as SeasonMission;
            if (theMission == null)
            {
                Debug.LogWarning("Invalid mission");
                continue;
            }
            if (theMission.id == group_id)
            {
                for(int objIndex=0; objIndex<theMission.objectives.Count; objIndex++)
                {
                    Objective o = theMission.objectives[objIndex];
                    if (o == null)
                    {
                        continue;
                    }
                    
                    if (o.id == id)
                    {
                        if (onObjectiveCompleted != null)
                        {
                            onObjectiveCompleted.Invoke(theMission, o);
                        }
                        theMission.updateObjectiveProgress(objIndex, o.amountNeeded, new List<long> {0L});
                        o.collectAllRewards("richPass");
                    }
                }
                break;
            }
        }
        InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.RICH_PASS_KEY, Dict.create(D.OPTION, true));
    }

    protected override void populateMissions(JSON data)
    {
        base.populateMissions(data);
        unlockSilverGames();
    }
    
    public void unlockSilverGames()
    {
        foreach(string gameKey in silverGameKeys)
        {
            LobbyOption option = LobbyOption.activeGameOption(gameKey);
            if (option != null && option.game != null && !option.game.isGoldPassGame)
            {
                option.game.setAsRichPassGame();
            }
        }
    }

    private void onRewardNodeUnlocked(JSON data)
    {
        long points = data.getLong("cumulative_points", 0);
        string passType = data.getString("pass_type", "");
        JSON nodeData = data.getJSON("unlocked_reward_data");
        string type = data.getString("type", "");
        if (nodeData == null)
        {
            Debug.LogError("Invalid node data");
            return;
        }

        if (type == REPEAT_NODE_UNLOCK_EVENT)
        {
            points += maximumPointsRequired;
        }

        RewardTrack track = null;
        switch (passType)
        {
            case "silver":
                track = CampaignDirector.richPass.silverTrack;
                break;
            
            case "gold":
                track = CampaignDirector.richPass.goldTrack;
                break;
        }

        if (track == null)
        {
            Debug.LogError("Invalid pass type");
            return;
        }

        JSON[] rewards = nodeData.getJsonArray("rewards");
        for (int i = 0; i < rewards.Length; i++)
        {
            int id = rewards[i].getInt("id", -1);
            PassReward reward = track.getReward(points, id);
            if (reward != null)
            {
                reward.unlock();    
            }
            else
            {
                if (type == REPEAT_NODE_UNLOCK_EVENT)
                {
                    //We don't get all the repeatable chests data at login so if one is unlocked but isn't already in our data, just populate it now
                    unlockRepeatReward(points, track, data, nodeData);
                }
                else
                {
                    Debug.LogWarningFormat("Couldn't find a reward to unlock at points value {0} with ID {1}", points, id);
                }
            }   
        }

        //update notification tags in main lobby overaly
        if (MainLobbyBottomOverlayV4.instance != null)
        {
            // Make sure that we update whether watch to earn is showing in the overlay.
            MainLobbyBottomOverlayV4.instance.setUpdateFlag();
        }

        InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.RICH_PASS_KEY, Dict.create(D.OPTION, true));
    }

    public int getNumberOfUnclaimedRewards(bool includeAllPassTypeRewards = false)
    {
        int unclaimed = 0;
        if (goldTrack == null || silverTrack == null)
        {
            return 0;
        }
        
        List<PassReward> allRewards = new List<PassReward>();
        allRewards.AddRange(silverTrack.getAllRewardsForPointTotal(pointsAcquired));
        if (isPurchased() || includeAllPassTypeRewards)
        {
            allRewards.AddRange(goldTrack.getAllRewardsForPointTotal(pointsAcquired));    
        }

        for (int i = 0; i < allRewards.Count; i++)
        {
            if (allRewards[i] == null)
            {
                continue;
            }
            
            if (allRewards[i].unlocked && !allRewards[i].claimed)
            {
                unclaimed++;
            }
        }
        
        return unclaimed;
    }

    private void onChallengeGroupUnlocked(JSON data)
    {
        if (data == null)
        {
            Debug.LogWarning("Invalid data");
            return;
        }
        
        string challengeType = data.getString("challenge_type", "");
        JSON unlockedData = data.getJSON("unlocked_challenge_data");
        if (unlockedData == null)
        {
            Debug.LogWarning("Invalid challenge data");
            return;
        }

        switch (challengeType)
        {
            case "periodic":
                addPeriodicChallenge(unlockedData);
                periodChallengesEnd.registerFunction(refreshChallenges);
                break;
            
            case "seasonal":
                bool challengesAddedSuccess = addSeasonalChallenge(unlockedData);
                if (challengesAddedSuccess)
                {
                    latestSeasonalUnlockDate = unlockedData.getInt("start_time", -1);
                    enableNewChallenges(latestSeasonalUnlockDate);
                    updateLockedDates(latestSeasonalUnlockDate);
                    RichPassNewSeasonalChallengesDialog.showDialog(latestSeasonalUnlockDate);
                }

                break;
        }
        
        unlockSilverGames();
    }

    protected override bool addPeriodicChallenge(JSON data)
    {
        if (!base.addPeriodicChallenge(data))
        {
            return false;
        }
        
        //Update the dialog with the new feature 
        if (RichPassFeatureDialog.instance != null && RichPassFeatureDialog.instance.gameObject != null)
        {
            RichPassFeatureDialog.instance.setPeriodicChallengeDisplay();
        }

        
        if (CampaignDirector.richPass != null)
        {
            Objective objectiveToDisplay = CampaignDirector.richPass.getCurrentPeriodicObjective();
            InGameFeatureContainer.refreshDisplay(InGameFeatureContainer.RICH_PASS_KEY, Dict.create(D.DATA, objectiveToDisplay));
        }

        return true;
    }

    //Called when the users claim reward is complete
    public static void onBankReward(JSON data)
    {
        JSON[] rewards = data.getJsonArray("rewards");
        string eventId = data.getString("event", "");
        long coinReward = 0;
        for (int i = 0; i < rewards.Length; i++)
        {
            string type = rewards[i].getString("type", "");
            switch (type)
            {
                case "coin":
                case "coins":
                    coinReward = rewards[i].getLong("value", 0);
                    break;
                default:
                    Debug.LogWarning("Unexpected Piggy Bank reward type: " + type);
                    break;
            }
        }
        
        RichPassBankRewardDialog.showDialog(coinReward, eventId);
    }

    private void onRPGetInfo(JSON data)
    {
        unregisterEvents();
        string newPassType = data.getString("pass_type", passType);
        if (newPassType != passType)
        {
            if (newPassType == "silver")
            {
                expireGoldPass();
            }
            else
            {
                upgradePass(newPassType);
            }
        }
        init(data);
    }

    public override void drawInDevGUI()
    {
        GUILayout.BeginVertical();
        GUILayout.Label(string.Format("Campaign ID: {0}", campaignID));
        GUILayout.Label(string.Format("isEnabled: {0}", isEnabled));
        GUILayout.Label(string.Format("Error string: {0}", campaignErrorString));
        GUILayout.Label(string.Format("isForceDisabled: {0}", isForceDisabled));
        GUILayout.Label(string.Format("Total Missions: {0}", missions.Count));
        GUILayout.Label(string.Format("Num Periodic Challenge Groups: {0}", periodicMissions.Count));
        GUILayout.Label(string.Format("Num Seasonal Challenge Groups: {0}", seasonMissions.Count));
        GUILayout.Label(string.Format("Pass Points: {0}", pointsAcquired));
        GUILayout.Label(string.Format("State: {0}", state));
        GUILayout.Label(string.Format("Range Active: {0}", timerRange.isActive));
        GUILayout.Label(string.Format("Range Left: {0}", timerRange.timeRemainingFormatted));
        GUILayout.EndVertical();
    }

    public void upgradePass(string newType)
    {
        passType = newType;
        
        foreach(string gameKey in goldGameKeys)
        {
            LobbyOption option = LobbyOption.activeGameOption(gameKey);
            if (option != null && !option.game.isUnlocked)
            {
                option.game.setIsUnlocked();
            }
        }

        if (RichPassFeatureDialog.instance == null)
        {
            RichPassFeatureDialog.showDialog(this, SchedulerPriority.PriorityType.IMMEDIATE, 0, "", true);
        }
        
        if (onPassTypeChanged != null)
        {
            onPassTypeChanged.Invoke(passType);
        }
    }

    public void expireGoldPass()
    {
        passType = "silver";
        if (onPassTypeChanged != null)
        {
            onPassTypeChanged.Invoke("silver");
        }
        
    }

    public void incrementPoints(long numPoints)
    {
        if (numPoints <= 0)
        {
            Debug.LogWarning("Invalid point increment amount");
            return;
        }
        
        pointsAcquired += numPoints;
    }
    
    public bool showVideo(string action = "", bool autoPopped = false, bool topOfList = false, string motdKey = "")
    {
        return VideoDialog.showDialog(
            ExperimentWrapper.RichPass.videoUrl, 
            action, 
            "Check it out!", 
            summaryScreenImage: ExperimentWrapper.RichPass.videoSummaryPath, 
            autoPopped: autoPopped,
            motdKey:motdKey,
            topOfList: topOfList,
            statName:"rich_pass_feature_video"
        );
    }

    public void markChallengesSeen()
    {
        hasNewChallenges = false;
    }

    private bool parseReward(long pointValue, JSON data)
    {
        if (data == null)
        {
            Bugsnag.LeaveBreadcrumb("Invalid rich pass reward");
            return false;
        }
        
        List<string> passTypes = data.getKeyList();
        bool hasActiveReward = false; //Some rewards are feature dependent, so don't add any rewards to our list for disabled features
        for (int j = 0; j < passTypes.Count; j++)
        {
            string passName = passTypes[j];
            JSON passJSON = data.getJSON(passName);
            
            List<PassReward> rewardItems = null;
            bool isUnlocked = passJSON.getBool("is_unlocked", false);
            JSON[] rewardJson = passJSON.getJsonArray("rewards");
            if (rewardJson != null && rewardJson.Length > 0)
            {
                rewardItems = new List<PassReward>(rewardJson.Length);
                for (int k = 0; k < rewardJson.Length; k++)
                {
                    switch (rewardJson[k].getString("type", ""))
                    {
                        case "chest":
                            PassChestReward chestReward = new PassChestReward(isUnlocked, rewardJson[k]);
                            bool activeChest = true;
                            if (chestReward != null && chestReward.rewards != null)
                            {
                                for (int rewardIndex = 0; rewardIndex < chestReward.rewards.Count; rewardIndex++)
                                {
                                    if (chestReward.rewards[rewardIndex].type == "card_packs" && !Collectables.isActive())
                                    {
                                        //Collecting a chest with a card pack fails if collections is disabled, so validate the chest is actually active
                                        activeChest = false;
                                        break;
                                    }
                                }
                            }

                            if (activeChest)
                            {
                                hasActiveReward = true;
                                rewardItems.Add(chestReward);
                            }

                            break;
                        
                        case "card_packs":
                            if (Collectables.isActive())
                            {
                                hasActiveReward = true;
                                rewardItems.Add(new PassReward(isUnlocked, rewardJson[k]));
                            }

                            break;

                        case "random_powerups":
                            if (Collectables.isActive() && PowerupsManager.isPowerupsEnabled)
                            {
                                hasActiveReward = true;
                                rewardItems.Add(new PassReward(isUnlocked, rewardJson[k]));
                            }
                            break;

                        case "base_bank_coins":
                            hasActivePiggybank = true;
                            hasActiveReward = true;
                            PassReward piggyBankBaseReward = new PassReward(isUnlocked, rewardJson[k]);
                            rewardItems.Add(piggyBankBaseReward);
                            break;
                        
                        case "bank_coins_multiplier":
                            hasActivePiggybank = true;
                            hasActiveReward = true;
                            PassReward piggyBankMultiReward = new PassReward(isUnlocked, rewardJson[k]);
                            rewardItems.Add(piggyBankMultiReward);
                            break;
                        
                        case "elite_point":
                            if (EliteManager.isActive)
                            {
                                hasActiveReward = true;
                                rewardItems.Add(new PassReward(isUnlocked, rewardJson[k]));
                            }
                            break;
                        
                        default:
                            hasActiveReward = true;
                            rewardItems.Add(new PassReward(isUnlocked, rewardJson[k]));
                            break;
                    }
                    
                }
            }
            RewardTrack track = null;
            if (!passRewards.TryGetValue(passName, out track))
            {
                track = new RewardTrack(passName);
                passRewards[passName] = track;
            }
            track.addRange(pointValue, rewardItems);
        }

        return hasActiveReward;
    }

    private void unlockRepeatReward(long points, RewardTrack track, JSON data, JSON unlockedData)
    {
        List<PassReward> rewardItems = null;
        bool isUnlocked = unlockedData.getBool("is_unlocked", false);
        JSON[] rewardJson = unlockedData.getJsonArray("rewards");
        if (rewardJson != null && rewardJson.Length > 0)
        {
            rewardItems = new List<PassReward>(rewardJson.Length);
            for (int k = 0; k < rewardJson.Length; k++)
            {
                switch (rewardJson[k].getString("type", ""))
                {
                    case "chest":
                        rewardItems.Add(new PassChestReward(isUnlocked, rewardJson[k]));
                        break;
                        
                    default:
                        rewardItems.Add(new PassReward(isUnlocked, rewardJson[k]));
                        break;
                }
                    
            }
        }

        track.addRange(points, rewardItems);
    }

    public int getLatestSeasonalDate()
    {
        if (latestSeasonalUnlockDate < 0)
        {
            int[] unlockDates = CampaignDirector.richPass.getSeasonalUnlockDates();
            if (unlockDates != null && unlockDates.Length > 0)
            {
                latestSeasonalUnlockDate = unlockDates[unlockDates.Length - 1];    
            }
            else
            {
                Debug.LogError("Invalid season unlock date");
            }
            
        }

        return latestSeasonalUnlockDate;
    }

    public long getCurrentRepeatableChestRequirement()
    {
        //Go through each chest reward and display the first one that isn't claimed
        for (int i = 1; i <= CampaignDirector.richPass.maxRepeatableRewards; i++)
        {
            long chestPoints = CampaignDirector.richPass.maximumPointsRequired + i * CampaignDirector.richPass.repeatableRewardsPointsRequired;
            
            List<PassReward> currNodeGoldRewards = goldTrack.getSingleRewardsList(chestPoints);
            if (currNodeGoldRewards != null && currNodeGoldRewards.Count > 0)
            {
                for (int rewardIndex = 0; rewardIndex < currNodeGoldRewards.Count; rewardIndex++)
                {
                    if (!currNodeGoldRewards[rewardIndex].claimed)
                    {
                        return chestPoints;
                    }
                }
            }
            else
            {
                //Server only sends us rewards data for the first chest and then any chests that have been unlocked
                //If all the chests that we have data for are claimed, assume the next chest to be unlocked is the next interval
                return chestPoints;
            }
        }
        return -1;
    }

    private void updateLockedDates(int unlockedDate)
    {
        lockedSeasonMissions.Remove(unlockedDate);
    }
    
    public string dialogBackgroundPath
    {
        get { return !string.IsNullOrEmpty(ExperimentWrapper.RichPass.dialogBgPath()) ? ExperimentWrapper.RichPass.dialogBgPath() : DEFAULT_DIALOG_BG; }
    }

    public static void onFeatureLoad()
    {
        if (!AssetBundleManager.isBundleCached(BUNDLE_NAME))
        {
            AssetBundleManager.downloadAndCacheBundle(BUNDLE_NAME, false, true, true);
        }
    }

    // Implements IResetGame
    public static void resetStaticClassData()
    {
	    goldGameKeys.Clear();
        silverGameKeys.Clear();
    }
}
