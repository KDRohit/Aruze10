using Com.Scheduler;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Com.HitItRich.Feature.VirtualPets;
using Com.Rewardables;
using TMPro;
using UnityEngine;

public class RichPassInGameCounter : InGameFeatureDisplay , IResetGame
{
    private const string dailyChallengeCompleteLocalization = "daily_challenge_complete";
    private const string seasonChallengeCompleteLocalization = "season_challenge_complete";
    private const string newDailyChallengeLocalization = "rp_new_daily_challenge";

    private const string POPOUT_COMPLETE_ANIM_NAME = "Popout Panel Complete";
    private const string CHALLENGE_COMPLETE_STATE = "challenge_complete";
    private const string IN_PROGRESS_STATE = "inprogress";
    private const string NO_DAILY_CHALLENGE_STATE = "no_active_daily_challenge";
    private const string REWARD_STATE = "reward_waiting";
    private const string CHALLENGE_RESET_STATE = "challenge_reset";
    private const string NEW_CHALLENGE_STATE = "new_challenge";
    private const string PET_DAILY_TASK_TYPE = "rp_periodic";
    

    //Is the current bet high enough to add progress to this challenge.
    private bool betIsQualifying = true;
    [SerializeField]private Animator qualifiedBetAnimator;
    [SerializeField] private Animator panelAnimator;
    [SerializeField] private GameObject petTaskObject;

    public static RichPassInGameCounter instance;

    private void Awake()
    {
        instance = this;
    }


    private bool buttonHandlerEnabled;
    private enum DisplayState
    {
        IN_PROGRESS,
        CHALLENGE_COMPLETE,
        PROGRESS_RESET,
        REWARD_WAITING,
        NO_DAILY_CHALLENGE,
        NEW_DAILY_CHALLENGE
    }

    private class PresentationData
    {
        public DisplayState state;
        public Objective objective;
    }

    [SerializeField] private ObjectSwapper objectSwap;
    [SerializeField] private ObjectSwapper passTypeSwap;
    [SerializeField] private TextMeshPro challengeLabel;
    [SerializeField] private LabelWrapperComponent challengeLabelShadow;
    [SerializeField] private TextMeshPro percentageLabel;
    [SerializeField] private TextMeshPro pointValueLabel;
    [SerializeField] private TextMeshPro popoutLabel;
    [SerializeField] private float challengeCompleteDisplayTime = 5.0f;

    [SerializeField] private GameObject notificationBubble;
    [SerializeField] private TextMeshPro notificationLabel;
    [SerializeField] private UIStretch notificationBubbleStretch;
    [SerializeField] private UIStretch notificationShadowStretch;
    [SerializeField] private GameObject popoutPanel;
    [SerializeField] private ObjectSwapper popoutSwapper;
    [SerializeField] private Animator popoutAnimator; //TODO: change to animation list
    [SerializeField] private UIStretch popoutShadowStretch;
    [SerializeField] private UIMeterNGUI progressMeter;
    [SerializeField] private ButtonHandler buttonHandler;
    [SerializeField] private UIAnchor popoutCheckmarkImageAnchor;
    [SerializeField] private UIAnchor popoutResetImageAnchor;
    [SerializeField] private UIStretchTextMeshPro popoutBackgroundStretch;
    [SerializeField] private Vector2 popOutPixelOffset;
    [SerializeField] private Vector2 popOutPixelOffsetNoIcon;

    private Objective defaultObjective = null;
    private Objective currentObjective = null;
    private long currentSpinCount = 0;
    private long currentProgress = 0;
    private bool playingPresentation = false;
    private bool eventSetup = false;
    private DisplayState state = DisplayState.IN_PROGRESS;
    private CampaignDirector.FeatureTask petTask;
    private static Queue<PresentationData> presentationQueue = new Queue<PresentationData>();

    public bool isPlayingPresentation
    {
        get
        {
            return playingPresentation || presentationQueue.Count > 0;
        }
    }

    public override void init(Dict args = null)
    {
        Objective objective = (Objective)args.getWithDefault(D.DATA, null);
        bool playSound = (bool)args.getWithDefault(D.OPTION, false);

        //make the first objective we track the default objective
        if (defaultObjective == null)
        {
            defaultObjective = objective;
            buttonHandlerEnabled = true;
        }
        
        currentObjective = objective;

        //disable flyout
        popoutPanel.SetActive(false);

        //update notification badge
        int rewardsAvailable = CampaignDirector.richPass.getNumberOfUnclaimedRewards();
        updateNotificationBubble(rewardsAvailable);

        //register event handlers
        registerEvents();

        //show the default presentation
        setInitialState(rewardsAvailable, playSound);

        //update the mission counter and progress bar
        updateCounts();

        //Wait for the current bet to be set, then modify the state of the panel based on the current bet being eligible for progress or not.
        StartCoroutine(waitThenSetQualifyingBetStatus());
    }

    public static void resetStaticClassData()
    {
        presentationQueue.Clear();
    }

    public override void onStartNextAutoSpin()
    {
        runPresentation(true);
    }

    public override void setButtonsEnabled(bool enabled)
    {
        buttonHandlerEnabled = enabled;
        if (enabled)
        {
            if (presentationQueue.Count > 0)
            {
                runPresentation();
            }
        }
    }

    private void runPresentation(bool force = false)
    {
        if (presentationQueue.Count == 0 ||
            (!buttonHandlerEnabled && !force))
        {
            return;
        }

        PresentationData data = presentationQueue.Dequeue();
        setState(data.state, data.objective);
    }

    private void setInitialState(int rewardsClaimable, bool playSound = false)
    {
        if (rewardsClaimable > RichPassFeatureDialog.SeenUnclaimedPrizes)
        {
            setState(DisplayState.REWARD_WAITING, null, playSound);
        }
        else if (currentObjective == null || currentObjective.isComplete)
        {
            //set the progress meter to the total pass state
            setState(DisplayState.NO_DAILY_CHALLENGE);
        }
        else
        {
            setState(DisplayState.IN_PROGRESS);
        }
    }

    private void registerEvents()
    {
        if (!eventSetup)
        {
            buttonHandler.registerEventDelegate(onClick);
            RewardablesManager.addEventHandler(onRewardReceived);
            CampaignDirector.richPass.registerForObjectiveComplete(onChallengeComplete);
            eventSetup = true;
        }
    }

    private void onRewardReceived(Rewardable rewardable)
    {
        RewardRichPass richPassReward = rewardable as RewardRichPass;

        if (richPassReward != null)
        {
            updateNotificationBubble();

            if (playingPresentation)
            {
                return;
            }

            if (state == DisplayState.REWARD_WAITING)
            {
                int rewardsClaimable = CampaignDirector.richPass.getNumberOfUnclaimedRewards();
                setInitialState(rewardsClaimable);
            }
        }
    }


    private void onChallengeComplete(Mission mision, Objective objective)
    {
        if (playingPresentation || !buttonHandlerEnabled)
        {
            PresentationData data = new PresentationData();
            data.state = DisplayState.CHALLENGE_COMPLETE;
            data.objective = objective;
            presentationQueue.Enqueue(data);
            return;
        }
        
        setState(DisplayState.CHALLENGE_COMPLETE, objective); 
    }

    public override void refresh(Dict args)
    {
        bool betSelectorUsed = (bool)args.getWithDefault(D.BET_CREDITS, false);
        if (betSelectorUsed)
        {
            onBetSelectorClosed();
        }
        
        Objective newObjective = args.getWithDefault(D.DATA, null) as Objective;
        if (newObjective != null)
        {
            refreshPeriodicChallenge(newObjective);
        }

        bool updateText = (bool)args.getWithDefault(D.OPTION, false);
        if (updateText)
        {
            updateCounts();
        }
    }
        

    private void refreshPeriodicChallenge(Objective newObjective)
    {
        if (playingPresentation || !buttonHandlerEnabled)
        {
            PresentationData data = new PresentationData();
            data.state = DisplayState.NEW_DAILY_CHALLENGE;
            data.objective = newObjective;
            presentationQueue.Enqueue(data);
            return;
        }
        
        setState(DisplayState.NEW_DAILY_CHALLENGE, newObjective);
    }

    private CampaignDirector.FeatureTask findDailyPetTask()
    {
        if (VirtualPetsFeature.instance == null || VirtualPetsFeature.instance.treatTasks == null)
        {
            return null;
        }
        for (int i = 0; i < VirtualPetsFeature.instance.treatTasks.Count; i++)
        {
            string taskId = VirtualPetsFeature.instance.treatTasks[i];
            CampaignDirector.FeatureTask activeTask = CampaignDirector.getTask(taskId);
            if (activeTask == null)
            {
                continue;
            }
            
            if (activeTask.type == PET_DAILY_TASK_TYPE)
            {
                return activeTask;
            }
        }

        return null;
    }

    private void updatePetTask()
    {
        if (VirtualPetsFeature.instance == null || !VirtualPetsFeature.instance.isEnabled || !VirtualPetsFeature.instance.ftueSeen)
        {
            SafeSet.gameObjectActive(petTaskObject, false);
            return;
        }

        if (petTask == null)
        {
            petTask = findDailyPetTask();
        }

        if (petTask != null)
        {
            SafeSet.gameObjectActive(petTaskObject, !petTask.isComplete);
        }
        
    }

    private void setState(DisplayState newState, object param = null, bool playSounds = false)
    {
        if (state == newState)
        {
            //don't do state switch
            updateNotificationBubble();
            return;
        }
        
        switch (newState)
        {
            case DisplayState.IN_PROGRESS:
                if (objectSwap != null)
                {
                    objectSwap.setState(IN_PROGRESS_STATE);    
                }
                if (progressMeter != null && progressMeter.gameObject != null)
                {
                    if (currentObjective != null)
                    {
                        progressMeter.gameObject.SetActive(true);
                        progressMeter.setState(currentObjective.currentAmount,currentObjective.progressBarMax);   
                    }
                    else
                    {
                        progressMeter.gameObject.SetActive(false);
                    }    
                }
                state = DisplayState.IN_PROGRESS;
                runPresentation();
                break;
            
            case DisplayState.REWARD_WAITING:
                if (playSounds)
                {
                    Audio.play("RewardAvailableRichPass");
                }

                if (objectSwap != null)
                {
                    objectSwap.setState(REWARD_STATE);    
                }
                if (progressMeter != null && progressMeter.gameObject != null)
                {
                    progressMeter.gameObject.SetActive(false);    
                }
                state = DisplayState.REWARD_WAITING;
                runPresentation();
                break;

            case DisplayState.NO_DAILY_CHALLENGE:
                {
                    if (objectSwap != null)
                    {
                        objectSwap.setState(NO_DAILY_CHALLENGE_STATE);    
                    }
                    if (passTypeSwap = null)
                    {
                        passTypeSwap.setState(CampaignDirector.richPass.passType == "gold" ? "gold" : "silver");    
                    }

                    if (progressMeter != null && progressMeter.gameObject != null)
                    {
                        progressMeter.gameObject.SetActive(true);
                        long prevPoints = 0;
                        long nextPoints = 0;
                        CampaignDirector.richPass.getClosestRewards(out prevPoints, out nextPoints);
                        if (nextPoints > 0)
                        {
                            long delta = nextPoints - prevPoints;
                            long current = CampaignDirector.richPass.pointsAcquired - prevPoints;
                            progressMeter.setState(current, delta);
                        }
                        else
                        {
                            //fill bar
                            progressMeter.setState(1,1);
                        }
                    }
                    state = DisplayState.NO_DAILY_CHALLENGE;
                    runPresentation();
                }
                
                break;

            case DisplayState.CHALLENGE_COMPLETE:
                {
                    Objective completedObjective = param as Objective;
                    if (completedObjective == null)
                    {
                        Debug.LogError("No completed objective");
                        return;
                    }
                    
                    playingPresentation = true;
                    currentObjective = completedObjective;
                    popoutSwapper.setState(CHALLENGE_COMPLETE_STATE);
                    popoutBackgroundStretch.pixelOffset = popOutPixelOffset;
                    if (CampaignDirector.richPass.getChallengeType(completedObjective) == SeasonalCampaign.ChallengeType.PERIODIC)
                    {
                        popoutLabel.text = Localize.text(dailyChallengeCompleteLocalization);
                    }
                    else
                    {
                        popoutLabel.text = Localize.text(seasonChallengeCompleteLocalization);
                    }
                    objectSwap.setState(CHALLENGE_COMPLETE_STATE);
                    progressMeter.gameObject.SetActive(false);
                    pointValueLabel.text = CommonText.formatNumber(currentObjective.getRewardAmount(ChallengeReward.RewardType.PASS_POINTS));
                    state = DisplayState.CHALLENGE_COMPLETE;
                    if (gameObject != null && gameObject.activeInHierarchy)
                    {
                        StartCoroutine(playCompletePresentation());
                    }
                }
                break;
            
            case DisplayState.PROGRESS_RESET:
                objectSwap.setState(IN_PROGRESS_STATE);
                popoutSwapper.setState(CHALLENGE_RESET_STATE);
                popoutBackgroundStretch.pixelOffset = popOutPixelOffset;
                popoutLabel.text = Localize.text(ChallengeCampaign.challengeResetLocalization);
                if (progressMeter != null && progressMeter.gameObject != null)
                {
                    if (currentObjective != null)
                    {
                        progressMeter.gameObject.SetActive(true);
                        progressMeter.setState(0,currentObjective.progressBarMax);   
                    }
                    else
                    {
                        progressMeter.gameObject.SetActive(false);
                    }    
                }
                state = DisplayState.PROGRESS_RESET;
                if (gameObject != null && gameObject.activeInHierarchy)
                {
                    StartCoroutine(playResetPresentation());
                }
                playingPresentation = true;
                break;
            
            case DisplayState.NEW_DAILY_CHALLENGE:
                popoutSwapper.setState(NEW_CHALLENGE_STATE);
                popoutLabel.text = Localize.text(newDailyChallengeLocalization);
                popoutBackgroundStretch.pixelOffset = popOutPixelOffsetNoIcon;
                Objective newObjective = param as Objective;
                defaultObjective = newObjective;
                currentObjective = newObjective;
                currentProgress = newObjective.currentAmount;
                updateCounts();
                if (gameObject != null && gameObject.activeInHierarchy)
                {
                    StartCoroutine(playCompletePresentation());
                }
                playingPresentation = true;
                break;

            
            default:
                Debug.LogWarning("Invalid in game ui state");
                break;
        }
    }
    
    private void onRPFDialogOnCloseDelegate(Dict closeArgs)
    {
        //abort if we're already destroying this object
        if (this == null || this.gameObject == null)
        {
            return;
        }
        
        if (state == DisplayState.REWARD_WAITING)
        {
            if (currentObjective == null || currentObjective.isComplete)
            {
                //set the progress meter to the total pass state
                setState(DisplayState.NO_DAILY_CHALLENGE);
            }
            else
            {
                setState(DisplayState.IN_PROGRESS);
            }
        }
    }

    private void onClick(Dict args = null)
    {
        if (buttonHandlerEnabled && RichPassFeatureDialog.instance == null && !Scheduler.hasTaskWith("rich_pass"))
        {
            StatsRichPass.logInGameUIClick();
            RichPassFeatureDialog.showDialog(CampaignDirector.richPass,onRPFDialogOnCloseDelegate);
        }
        
    }

    private void updateNotificationBubble()
    {
        updateNotificationBubble(CampaignDirector.richPass.getNumberOfUnclaimedRewards());
    }

    private void updateNotificationBubble(int rewardsAvailable)
    {
        notificationBubble.SetActive(rewardsAvailable > 0);
        if (notificationLabel.gameObject != null)
        {
            notificationLabel.gameObject.SetActive(rewardsAvailable > 0);
            notificationLabel.text = CommonText.formatNumber(rewardsAvailable);
            
            //re-enable anchor to reposition text/shadow
            if (notificationBubbleStretch != null)
            {
                notificationBubbleStretch.enabled = true;
            }
            if (notificationShadowStretch != null)
            {
                notificationShadowStretch.enabled = true;
            }
        }
    }

    public void updateCounts()
    {
        bool wasReset = false;
        if (isPlayingPresentation || !betIsQualifying)
        {
            return;
        }
        
        updateNotificationBubble();
        updatePetTask();

        if (currentObjective != null)
        {
            XinYObjective xInY = null;
            if (currentObjective.type == XinYObjective.X_COINS_IN_Y)
            {
                xInY = currentObjective as XinYObjective;
            }

            // If we haven't finished
            if (currentObjective.currentAmount < currentObjective.progressBarMax)
            {
                if (currentProgress > currentObjective.progressBarMax ||
                    (xInY != null && xInY.constraints != null && xInY.constraints.Count > 0 &&  currentSpinCount > xInY.constraints[0].amount))
                {
                    wasReset = true;
                }
                
                if (xInY != null && xInY.constraints != null && xInY.constraints.Count > 0)
                {
                    currentSpinCount = xInY.constraints[0].amount;
                }

                currentProgress = currentObjective.currentAmount;
                if (state == DisplayState.IN_PROGRESS)
                {
                    progressMeter.gameObject.SetActive(true);
                    progressMeter.setState(currentObjective.currentAmount,currentObjective.progressBarMax, true);    
                }

                if (wasReset)
                {
                    setState(DisplayState.PROGRESS_RESET);
                }
            }

            if (xInY != null)
            {
                percentageLabel.text = "";
                if (currentObjective.usesTwoPartLocalization())
                {
                    challengeLabel.text = currentObjective.getShortChallengeTypeActionHeader() + System.Environment.NewLine + xInY.getShortDescriptionWithCurrentAmountAndLimit("robust_challenges_desc_short_", true);    
                }
                else
                {
                    challengeLabel.text = currentObjective.getTinyDynamicChallengeDescription(true);
                }
            }
            else
            {
                percentageLabel.text = currentObjective.getProgressText();

                if (currentObjective.usesTwoPartLocalization())
                {
                    StringBuilder sb = new StringBuilder();
                    string header = currentObjective.getShortChallengeTypeActionHeader().ToLower();
                    sb.Append(System.Char.ToUpper(header[0])); 
                    sb.Append(header.Substring(1).ToLower());
                    bool addNewLine = objectiveRequiresNewLine(currentObjective.type);
                    if (addNewLine)
                    {
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.Append(" ");
                    }
                    string shortDesc = currentObjective.getShortDescriptionLocalization("robust_challenges_desc_tiny_", true);
                    sb.Append(shortDesc);
                    challengeLabel.text = sb.ToString();
                }
                else
                {
                    challengeLabel.text = currentObjective.getTinyDynamicChallengeDescription(true);
                }
                
                //Force update the meshes so the bounds update and we can check for overlapping text
                challengeLabel.ForceMeshUpdate();
                percentageLabel.ForceMeshUpdate();

                if (challengeLabel.textBounds.Intersects(percentageLabel.textBounds))
                {
                    challengeLabel.rectTransform.offsetMin = new Vector2(challengeLabel.rectTransform.offsetMin.x, percentageLabel.rectTransform.offsetMax.y);
                    challengeLabelShadow.tmProLabel.rectTransform.sizeDelta = challengeLabel.rectTransform.sizeDelta;
                }

            }

            //Hide the pecentage text for certain challenge types
            if (currentObjective.type == Objective.MAX_VOLTAGE_TOKENS_COLLECT || currentObjective.type == XinYObjective.X_COINS_IN_Y)
            {
                percentageLabel.text = "";
            }

        }

    }

    private bool objectiveRequiresNewLine(string type)
    {
        switch (type)
        {
            case Objective.BIG_WIN:
            case Objective.BONUS_GAME: 
            case CollectObjective.OF_A_KIND: 
            case CollectObjective.SYMBOL_COLLECT:
                return false;
            
            default:
                return true;
        }

    }

    private IEnumerator playCompletePresentation()
    {
        //show popout
        popoutPanel.SetActive(true);
        
        //animate and play audio
        popoutAnimator.Play(POPOUT_COMPLETE_ANIM_NAME);
        Audio.play("ChallengeCompleteRichPass");

        //wait for animation to start and reposition checkmark
        yield return new WaitForSeconds(0.1f);
        if (popoutCheckmarkImageAnchor != null)
        {
            popoutCheckmarkImageAnchor.enabled = true;
        }
        if (popoutShadowStretch != null)
        {
            popoutShadowStretch.enabled = true;
        }
        
        //yield for rest of animation
        yield return new WaitForSeconds(challengeCompleteDisplayTime);

        //turn off popout
        if (popoutPanel != null)
        {
            popoutPanel.SetActive(false);
        }

        playingPresentation = false;
        
        //re-init with daily objective
        init(Dict.create(D.DATA, defaultObjective, D.OPTION, true));   
    }
    
    
    private IEnumerator playResetPresentation()
    {
        popoutPanel.SetActive(true);
        popoutAnimator.Play(POPOUT_COMPLETE_ANIM_NAME);
        
        //reposition image
        yield return new WaitForSeconds(0.1f);
        if (popoutResetImageAnchor != null)
        {
            popoutResetImageAnchor.enabled = true;
        }
        if (popoutShadowStretch != null)
        {
            popoutShadowStretch.enabled = true;
        }
        
        yield return new WaitForSeconds(challengeCompleteDisplayTime);

        if (popoutPanel != null)
        {
            popoutPanel.SetActive(false);
        }

        playingPresentation = false;
        
        //re-init
        init(Dict.create(D.DATA,defaultObjective));
    }
    
    public void forceFinishedState(Objective objective)
    {
        percentageLabel.text = objective.getCompletedProgressText();
        challengeLabel.text = objective.getShortDescriptionLocalization();
    }

    private void OnDestroy()
    {
        if (eventSetup)
        {
            if (buttonHandler != null)
            {
                buttonHandler.unregisterEventDelegate(onClick);    
            }
            RewardablesManager.removeEventHandler(onRewardReceived);
            if (CampaignDirector.richPass != null)
            {
                CampaignDirector.richPass.unregisterForObjectiveComplete(onChallengeComplete);    
            }
            
        }

        instance = null;
    }

    private IEnumerator waitThenSetQualifyingBetStatus()
    {
        yield return new WaitForSeconds(1f);
        if (this == null || this.gameObject == null || ReelGame.activeGame == null || currentObjective == null)
        {
            yield break;
        }
        
        betIsQualifying = ReelGame.activeGame.betAmount >= currentObjective.minWager;

        if (!betIsQualifying && qualifiedBetAnimator != null)
        {
            qualifiedBetAnimator.Play("ineligible");
            showUnqualifiedBetText();
        }
        else
        {
            qualifiedBetAnimator.Play("eligible");
        }
    }

    private void onBetSelectorClosed()
    {
        //if the user has completed the daily activity, don't do anything.
        if (currentObjective == null)
        {
            return;
        }
        
        betIsQualifying = ReelGame.activeGame.betAmount >= currentObjective.minWager;

        if (!betIsQualifying && qualifiedBetAnimator != null)
        {
            qualifiedBetAnimator.Play("ineligible");
            showUnqualifiedBetText();
        }
        else
        {
            qualifiedBetAnimator.Play("eligible");
        }
    }
    
    public override void onBetChanged(long newWager)
    {
        if (this == null || this.gameObject == null || ReelGame.activeGame == null || currentObjective == null)
        {
            return;
        }
        
        if (betIsQualifying && ReelGame.activeGame.betAmount < currentObjective.minWager && qualifiedBetAnimator != null)
        {
            qualifiedBetAnimator.Play("ineligible");
            betIsQualifying = false;
            showUnqualifiedBetText();
        }
        else if (!betIsQualifying && ReelGame.activeGame.betAmount >= currentObjective.minWager && qualifiedBetAnimator != null)
        {
            qualifiedBetAnimator.Play("eligible");
            betIsQualifying = true;
            updateCounts();
        }
    }
    private void showUnqualifiedBetText()
    {
        challengeLabel.text = Localize.text("robust_challenges_min_bet") + System.Environment.NewLine + CreditsEconomy.multiplyAndFormatNumberAbbreviated(currentObjective.minWager,3,true);
    }

}
