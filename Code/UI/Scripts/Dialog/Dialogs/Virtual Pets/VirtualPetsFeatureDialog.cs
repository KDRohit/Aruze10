using System.Collections.Generic;
using Com.HitItRich.Feature.VirtualPets;
using Com.Rewardables;
using Com.Scheduler;
using UnityEngine;

public class VirtualPetsFeatureDialog : DialogBase, IResetGame
{
    [SerializeField] private VirtualPetsDialogTabMyPet myPetsTab;
    [SerializeField] private VirtualPetsDialogTabTricks tricksTab;
    [SerializeField] private VirtualPetsDialogTabTreats treatsTab;
    [SerializeField] private VirtualPet playerPet;
    [SerializeField] private ClickHandler petButton;
    [SerializeField] private VirtualPetEnergyMeter energyMeter;
    [SerializeField] private LabelWrapperComponent fedStreakLabel;
    [SerializeField] private LabelWrapperComponent nextFedStreakLabel;
    [SerializeField] private ButtonHandler infoButton;
    [SerializeField] private ObjectSwapper tabSwapper;

    private const string HEAVY_CLICK_SOUND = "CloseDialogueLckyPet";
    private const string SMALL_CLICK_SOUND = "InDialogueClickLuckyPet";
    private const string PET_CLICK_SOUND = "InDialoguePetClickLuckyPet";
    private const string HOME_INIT_SOUND = "PuppyHomeInitLuckyPet";
    private const string BACKGROUND_MUSIC = "PuppyHomeBGLuckyPet";

    private TabType currentTab = TabType.NONE;
    private VirtualPetsDialogTab currentTabObject;
    
    private const string NEXT_STREAK_REWARD_LOC = "virtual_pet_next_inbox_reward";
    private static bool waitingForStatus = false;
    private static TabType openTabType = TabType.NONE;
    
    public enum TabType
    {
        MY_PET,
        TRICKS,
        TREATS,
        NONE
    }

    public override void init()
    {
        myPetsTab.tabButton.registerEventDelegate(tabClicked, Dict.create(D.TYPE, TabType.MY_PET));
        tricksTab.tabButton.registerEventDelegate(tabClicked, Dict.create(D.TYPE, TabType.TRICKS));
        treatsTab.tabButton.registerEventDelegate(tabClicked, Dict.create(D.TYPE, TabType.TREATS));
        infoButton.registerEventDelegate(infoClicked);
        VirtualPetsFeature.instance.registerForHyperStatusChange(hyperStatusChanged);
        VirtualPetsFeature.instance.registerForStatusUpdate(updateEnergyVisuals);

        TabType startingTab = (TabType)dialogArgs.getWithDefault(D.MODE, TabType.NONE);
        
        //If no starting tab was passed in the dialog args, go to tricks if all tasks are complete, if not show the treats tab.
        if (startingTab == TabType.NONE)
        {
            startingTab = VirtualPetsFeature.instance.getNumCompletedTasks() == VirtualPetsFeature.instance.treatTasks.Count
                    ? TabType.TRICKS
                    : TabType.TREATS;
        }

        setCurrentTab(startingTab);
        
        petButton.registerEventDelegate(onPetClick);
        energyMeter.init(VirtualPetsFeature.instance.currentEnergy, VirtualPetsFeature.instance.hyperEndTime);
        fedStreakLabel.gameObject.SetActive(VirtualPetsFeature.instance.fullyFedStreak > 0);
        fedStreakLabel.text = CommonText.formatNumber(VirtualPetsFeature.instance.fullyFedStreak) + 'd';
        nextFedStreakLabel.gameObject.SetActive(VirtualPetsFeature.instance.nextStreakRewardDay > 0);
        nextFedStreakLabel.text = Localize.text(NEXT_STREAK_REWARD_LOC, VirtualPetsFeature.instance.nextStreakRewardDay);
        StartCoroutine(playerPet.playIdleAnimations());

        Audio.switchMusicKeyImmediate(BACKGROUND_MUSIC);
    }

    private void infoClicked(Dict args = null)
    {
        //Add video when available
        VideoDialog.showDialog(
            ExperimentWrapper.VirtualPets.videoUrl,
            summaryScreenImage:ExperimentWrapper.VirtualPets.videoSummaryPath,
            priority:SchedulerPriority.PriorityType.IMMEDIATE,statClass:"pet");
        
        StatsManager.Instance.LogCount("dialog", "pet", currentTab.ToString(), "question_mark", "", "click", VirtualPetsFeature.instance.currentEnergy);
    }

    private void onPetClick(Dict args = null)
    {
        Audio.play(PET_CLICK_SOUND);
        StatsManager.Instance.LogCount("dialog", "pet", currentTab.ToString(), "pet_body", "", "click", VirtualPetsFeature.instance.currentEnergy);
        if (VirtualPetsFeature.instance.isPettingRewardActive)
        {
            VirtualPetsFeature.instance.registerForAward(onPetRewardSuccess);
            VirtualPetsFeature.instance.claimPettingReward();
        }
        else
        {
            StartCoroutine(playerPet.playRandomNonRewardReaction());
        }
    }

    private void onPetRewardSuccess(Rewardable rewardable)
    {
        VirtualPetsFeature.instance.deregisterForAward(onPetRewardSuccess);
        if (rewardable.type == RewardCoins.TYPE)
        {
            StartCoroutine(playerPet.playPettingAnimation());
            SlotsPlayer.addNonpendingFeatureCredits(rewardable.data.getLong("value", 0), "virtual_pets_petting");
        }
        else if (rewardable.type == RewardablePetBasicEnergy.TYPE)
        {
            StartCoroutine(playerPet.playPettingEnergyRewardAnimation());
            StartCoroutine(energyMeter.updateEnergy(VirtualPetsFeature.instance.currentEnergy, VirtualPetsFeature.instance.hyperEndTime));
        }
        
    }

    private void tabClicked(Dict args = null)
    {
        TabType newTabType = (TabType)args.getWithDefault(D.TYPE, TabType.NONE);
        if (newTabType != TabType.NONE && newTabType != currentTab)
        {
            Audio.play(SMALL_CLICK_SOUND);
            setCurrentTab(newTabType);
        }
    }
    
    public override void onCloseButtonClicked(Dict args = null)
    {
        Audio.play(HEAVY_CLICK_SOUND);
        RoutineRunner.instance.StartCoroutine(Glb.restoreMusic(1.0f));
        base.onCloseButtonClicked(args);
    }

    private void setCurrentTab(TabType newTabType)
    {
        if (currentTabObject != null)
        {
            currentTabObject.hideTab();
        }

        currentTab = newTabType;
        string statsTab = null;
        switch (currentTab)
        {
            case TabType.MY_PET:
                currentTabObject = myPetsTab;
                statsTab = "my_pet";
                break;

            case TabType.TREATS:
                currentTabObject = treatsTab;
                statsTab = "treats";
                break;
            
            case TabType.TRICKS:
                currentTabObject = tricksTab;
                statsTab = "tricks";
                break;
        }

        if (currentTabObject != null)
        {
            currentTabObject.init(playerPet);
        }
        
        tabSwapper.setState(currentTab.ToString());

        if (!string.IsNullOrEmpty(statsTab))
        {
            StatsManager.Instance.LogCount("dialog", "pet", statsTab, "top_nav", "", "click", VirtualPetsFeature.instance.currentEnergy);
        }

    }
    
    protected override void playOpenSound()
    {
        base.playOpenSound();
        Audio.play(HOME_INIT_SOUND);
    }


    public void updateEnergyVisuals()
    {
        StartCoroutine(energyMeter.updateEnergy(VirtualPetsFeature.instance.currentEnergy, VirtualPetsFeature.instance.hyperEndTime));
    }
    
    public void hyperStatusChanged(bool isHyper)
    {
        StartCoroutine(playerPet.playIdleAnimations());

        if (!isHyper)
        {
            StartCoroutine(energyMeter.turnOffHyperMode());
        }
    }
    
    public override void close()
    {
        VirtualPetsFeature.instance.deregisterForAward(onPetRewardSuccess);
        VirtualPetsFeature.instance.deregisterForStatusUpdate(updateEnergyVisuals);
        VirtualPetsFeature.instance.deregisterForHyperStatusChange(hyperStatusChanged);
    }

    public static void showDialog(TabType startingTabType = TabType.NONE)
    {
        if (waitingForStatus)
        {
            return;
        }
        
        if (VirtualPetsFeature.instance == null)
        {
            Debug.LogWarning("Pets not active yet");
            return;
        }

        waitingForStatus = true;
        openTabType = startingTabType; 
        Server.registerEventDelegate(VirtualPetsFeature.PET_STATUS_UPDATE, onPetStatusUpdate, false);
        VirtualPetsActions.refreshPetStatus();   
    }

    private static void onPetStatusUpdate(JSON data)
    {
        waitingForStatus = false;
        Scheduler.addDialog(VirtualPetsFeature.DIALOG_KEY, Dict.create(D.SHROUD, false, D.MODE, openTabType));
    }
    
    public static void resetStaticClassData()
    {
        waitingForStatus = false;
        openTabType = TabType.NONE;
    }
}
