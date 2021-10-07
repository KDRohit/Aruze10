using System.Collections;
using FeatureOrchestrator;
using UnityEngine;

public class CasinoEmpireInGamePanel : InGameProtonFeatureDisplay
{
    [SerializeField] private LabelWrapperComponent diceLabel;
    [SerializeField] private UIMeterNGUI meter;
    [SerializeField] private AnimationListController.AnimationInformationList diceIdleAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList tokenIdleAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList rollButtonIntroAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList rollButtonIdleAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList rollButtonMaxIntroAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList rollButtonMaxIdleAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList rollButtonOffAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList meterUpdateOnSpinAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList meterUpdateOnSpinCompleteAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList diceCountUpdateAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList fullMeterDiceUpdateAnimList;
    [SerializeField] private AnimationListController.AnimationInformationList betDownAnimList;
    [SerializeField] private AnimatedParticleEffect diceAddTrail;
    [SerializeField] private Transform tokenParent;
    [SerializeField] private AnimationListController.AnimationInformationList eventEndingAnimList;
    [SerializeField] private LabelWrapperComponent durationLabel;

    //Add timer

    private ProgressCounter meterProgressCounter;
    private PickByPickClaimableBonusGameOutcome totalPicksCounter;
    private GameObject loadedToken;
    private int currentTokenType = -1;

    private long meterValue;
    private long diceCount;
    private long currentWager;
    private long maxDiceCount;

    private bool showingTooltip = false;
    private GameTimerRange eventTimer;
    private string diceSource = "";
    private bool meterUpdateAnimPlaying = false;

    private const string TOKEN_PREFAB_PATH = "Features/Board Game/Common/Prefabs/Instanced Prefabs/In Game Panel/{0} Token";
    private const string BONUS_DICE_LOC_KEY = "{0}_bonus_dice";
    private const string RANDOM_DICE_LOC_KEY = "add_random_dice";
    
    public override void init(Dict args = null)
    {
        base.init(args);
        if (parentComponent != null)
        {
            JSON data = parentComponent.jsonData;
            meterProgressCounter = data.jsonDict["meterProgress"] as ProgressCounter;
            totalPicksCounter = data.jsonDict["totalPicksCounter"] as PickByPickClaimableBonusGameOutcome;
            if (totalPicksCounter == null || meterProgressCounter == null)
            {
                Debug.LogError("Missing data object");
                return;
            }
            
            ProgressCounter diceCounter = data.jsonDict["diceCounter"] as ProgressCounter;
            maxDiceCount = diceCounter.completeValue;

            updateDiceCount(false);
            StartCoroutine(updateMeter(false));

            if (diceCount > 0)
            {
                StartCoroutine(AnimationListController.playListOfAnimationInformation(diceIdleAnimList));
            }
            else
            {
                StartCoroutine(AnimationListController.playListOfAnimationInformation(tokenIdleAnimList));
            }

            diceAddTrail.particleEffectStartedPrefabEvent.AddListener(particleStarted);
            currentWager = SpinPanel.instance.currentWager;

            loadToken();
            
            
            TimePeriod featureTimer = parentComponent.jsonData.jsonDict["timePeriod"] as TimePeriod;
            if (featureTimer != null)
            {
                eventTimer = featureTimer.durationTimer;
                if (eventTimer.timeRemaining < Common.SECONDS_PER_DAY)
                {
                    onEventEndingSoon();
                }
                else
                {
                    eventTimer.registerFunction(onEventEndingSoon, null, Common.SECONDS_PER_DAY);
                }
            }
        }
    }

    private void onEventEndingSoon(Dict args = null, GameTimerRange caller = null)
    {
        if (this == null)
        {
            return;
        }
        StartCoroutine(AnimationListController.playListOfAnimationInformation(eventEndingAnimList));
        eventTimer.registerLabel(durationLabel.tmProLabel, GameTimerRange.TimeFormat.REMAINING_HMS_FORMAT);
        
        eventTimer.registerFunction(onEventEnd);
    }

    private void onEventEnd(Dict args = null, GameTimerRange caller = null)
    {
        if (this == null)
        {
            return;
        }
        InGameFeatureContainer.removeObjectsOfType("hir_boardgame");
    }


    private void loadToken()
    {
        currentTokenType = CustomPlayerData.getInt(CustomPlayerData.CASINO_EMPIRE_BOARD_GAME_SELECTED_TOKEN, -1);
        if (!System.Enum.IsDefined(typeof(BoardGameModule.BoardTokenType), currentTokenType))
        {
	        return;
        }
        string tokenName = ((BoardGameModule.BoardTokenType) currentTokenType).ToString();

        if (loadedToken != null)
        {
            Destroy(loadedToken);
        }
        
        AssetBundleManager.load(this, string.Format(TOKEN_PREFAB_PATH, tokenName), loadTokenSuccess, loadTokenFailed, isSkippingMapping:true, fileExtension:".prefab");
    }

    private void loadTokenSuccess(string path, Object obj, Dict args)
    {
        loadedToken = NGUITools.AddChild(tokenParent, obj as GameObject);
    }
    
    private void loadTokenFailed(string path, Dict args)
    {
        Debug.LogWarning("Failed to load token at path: " + path);
    }

    public override void onStartNextSpin(long wager)
    {
        StartCoroutine(updateMeter(true, wager));
    }
    
    public override void onSpinComplete()
    {
        if (diceCount != totalPicksCounter.availablePickCount)
        {
            if (diceSource == "random")
            {
                StartCoroutine(diceAddTrail.animateParticleEffect());
            }
            else
            {
                updateDiceCount(true);
            }
        }
        
        if (meterValue != meterProgressCounter.currentValue)
        {
            StartCoroutine(updateMeter(meterValue < meterProgressCounter.currentValue));
        }
    }

    public override void onBetChanged(long newWager)
    {
        if (newWager < currentWager && !showingTooltip)
        {
            RoutineRunner.instance.StartCoroutine(showTooltip());
        }

        currentWager = newWager;
    }

    public override void refresh(Dict args)
    {
        //Load the token if its not loaded yet or if the player picked a new token
	    if (loadedToken == null || CustomPlayerData.getInt(CustomPlayerData.CASINO_EMPIRE_BOARD_GAME_SELECTED_TOKEN, -1) != currentTokenType)
	    {
		    loadToken();
	    }

	    JSON data = (JSON)args.getWithDefault(D.DATA, null);
        if (data != null)
        {
            diceSource = data.getString("source", "");
        }

        //Don't update yet if we have a valid dice source. This means animations will play and update the dice then
        if (string.IsNullOrEmpty(diceSource) && diceCount != totalPicksCounter.availablePickCount)
        {
            //If adding dice show anim
            updateDiceCount(false);
            
            //If the dice count changed, then update the meter also incase we're at the max count now, or dipped below it
            StartCoroutine(updateMeter(false));
        }
    }

    private IEnumerator showTooltip()
    {
        showingTooltip = true; 
        yield return RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(betDownAnimList));
        showingTooltip = false;
    }

    public void particleStarted(GameObject particle)
    {
        LabelWrapperComponent particleLabel = particle.GetComponentInChildren<LabelWrapperComponent>(true);

        if (string.IsNullOrEmpty(diceSource) || diceSource == "fullMeter")
        {
            long amountToAdd = totalPicksCounter.availablePickCount - diceCount;
            particleLabel.text = Localize.text(BONUS_DICE_LOC_KEY, CommonText.formatNumber(amountToAdd));
        }
        else if (diceSource == "random")
        {
            particleLabel.text = Localize.text(RANDOM_DICE_LOC_KEY);
        }
    }

    public void particleFinishedEvent()
    {
        updateDiceCount(true);
    }

    private IEnumerator updateMeter(bool playAnimation, long wager = 0)
    {
        //Force the meter to be full if we're resetting to 0 and gaining dice, or if dice count is at max
        bool forceMaxMeter = diceCount >= maxDiceCount;
        //Leave meter as full if we're at the max amount of dice we can get from spins
        meterValue = forceMaxMeter ? meterProgressCounter.completeValue : meterProgressCounter.currentValue;
        meterValue += wager;
        meter.setState(meterValue, meterProgressCounter.completeValue);


        if (forceMaxMeter)
        {
            playAnimation = false; //Don't play the update animation when progress isn't being added because we're at the max dice count
        }
        //Check if a previous update animation is still playing
        //Mostly only happens during auto-spins and the update from the spin starting stomps on the previous spin complete animation
        if (playAnimation && !meterUpdateAnimPlaying)
        {
            meterUpdateAnimPlaying = true;
            if (wager > 0)
            {
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(meterUpdateOnSpinAnimList));
            }
            else
            {
                yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(meterUpdateOnSpinCompleteAnimList));
            }
            meterUpdateAnimPlaying = false;
        }
    }
    
    private void updateDiceCount(bool playUpdateAnimation)
    {
        long prevCount = diceCount;
        diceCount = totalPicksCounter.availablePickCount;

        string diceLabelText = diceCount >= maxDiceCount ? Localize.text("max") : CommonText.formatNumber(diceCount);
        diceLabel.text = diceLabelText;
        if (diceCount > 0)
        {
            //If previous count was 0, play the special animations of the button turning on
            //else, just play the count update animation
            if (prevCount == 0)
            {
                if (diceCount >= maxDiceCount)
                {
                    StartCoroutine(AnimationListController.playListOfAnimationInformation(rollButtonMaxIntroAnimList));
                }
                else
                {
                    StartCoroutine(AnimationListController.playListOfAnimationInformation(rollButtonIntroAnimList));
                }
            }
            else
            {
                if (playUpdateAnimation)
                {
                    StartCoroutine(AnimationListController.playListOfAnimationInformation(diceCountUpdateAnimList));
                }

                //Put the button into the correct idle state
                if (diceCount >= maxDiceCount)
                {
                    StartCoroutine(AnimationListController.playListOfAnimationInformation(rollButtonMaxIdleAnimList));

                }
                else
                {
                    StartCoroutine(AnimationListController.playListOfAnimationInformation(rollButtonIdleAnimList));
                }
            }

            if (playUpdateAnimation && diceSource == "fullMeter")
            {
                StartCoroutine(AnimationListController.playListOfAnimationInformation(fullMeterDiceUpdateAnimList));
            }
        }
        else
        {
            StartCoroutine(AnimationListController.playListOfAnimationInformation(rollButtonOffAnimList));
        }
        
        diceSource = ""; //reset this once presentation is over
    }

    public override void onShow()
    {
        SafeSet.gameObjectActive(gameObject, true);
    }

    public override void onHide()
    {
        SafeSet.gameObjectActive(gameObject, false);
    }

    public void OnDestroy()
    {
        if (eventTimer != null)
        {
            eventTimer.removeFunction(onEventEnd);
            eventTimer.removeFunction(onEventEndingSoon);
        }
    }
}
