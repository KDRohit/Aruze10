using System.Collections;
using UnityEngine;
using Zynga.Core.Util;

/// <summary>
/// Class used to record information about the user's state.  Used to determine if the user is idle (ie game is loaded, but user isn't actively playing)
/// </summary>
public class UserActivityManager : IResetGame
{
    private const int WRITE_TO_PREFS_INTERVAL = 120;
    private const int DEFAULT_IDLE_MINUTES = 10;

    private int idleThreshold
    {
        get
        {
            if (ExperimentWrapper.VirtualPets.isInExperiment)
            {
                return Common.SECONDS_PER_MINUTE * ExperimentWrapper.VirtualPets.idleTime;
            }
            else
            {
                return Common.SECONDS_PER_MINUTE * DEFAULT_IDLE_MINUTES;
            }
        }
    }

    private GameTimerRange idleTimer;
    
    private UserActivityManager()
    {
        //default to a negative value until we get the login callback
        savedTime = -1;
        _usedAllPetCollectsTime = -1;
        _loginTime = -1;
        _lastTimerCollectTime = -1;
        didPetCollectThisSession = false;  
    }
    
    private static UserActivityManager _instance;
    public static UserActivityManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new UserActivityManager();
            }

            return _instance;
        }
    }

    public static bool hasInstance()
    {
        return _instance != null;
    }

    public delegate void OnIdleActivationDelegate();
    private event OnIdleActivationDelegate onUserIdle;


    private void onIdleActivation(Dict args = null, GameTimerRange originalTimer = null)
    {
        idleTimer = null;
        if (onUserIdle != null)
        {
            onUserIdle();
        }
    }

    public void registerForIdleEvent(OnIdleActivationDelegate func)
    {
        onUserIdle -= func;
        onUserIdle += func;
    }

    public void unregisterForIdleEvent(OnIdleActivationDelegate func)
    {
        onUserIdle -= func;
    }
    
    

    public int lastActionTime
    {
        get
        {
            return Mathf.Max(savedTime, _loginTime, _lastInputTime, _lastSpinTime, _lobbyTimeoutTime);
        }
    }

    public int savedTime { get; private set; }

    private int _lastInputTime;
    public int lastInputTime
    {
        get
        {
            return _lastInputTime;
        }
        private set
        {
            _lastInputTime = value;
            setUpdateTimer();
        }
    }

    private int _lastTimerCollectTime;
    public int lastTimerCollectTime
    {
        get
        {
            return _lastTimerCollectTime;
        }
        private set
        {
            _lastTimerCollectTime = value;
            setUpdateTimer();
        }
    }

    public bool didPetCollectThisSession
    {
        get; private set;
    }

    public bool isIdle
    {
        get
        {
            return (GameTimer.currentTime - lastActionTime) > idleThreshold;
        }
    }

    public bool wasIdleBeforeLogin
    {
        get
        {
#if !ZYNGA_PRODUCTION
            if (debugSavedTime.HasValue)
            {
                return (loginTime - debugSavedTime.Value) > idleThreshold;
            }
#endif
            return (loginTime - savedTime) > idleThreshold;
        }
    }

    public float timeUntilDiskWrite
    {
        get
        {
            if (writeToDiskTime < GameTimer.currentTime)
            {
                return 0;
            }
            return writeToDiskTime - GameTimer.currentTime ;
        }
    }

    public float timeUntilIdle
    {
        get
        {
            if (isIdle)
            {
                return 0;
            }

            return idleThreshold - (GameTimer.currentTime - _lastInputTime);
        }
    }

    private int _lastSpinTime;
    public int lastSpinTime
    {
        get
        {
            return _lastSpinTime;
        }
        private set
        {
            _lastSpinTime = value;
            setUpdateTimer();
        }
    }


    private int _loginTime;
    public int loginTime
    {
        get
        {
            return _loginTime;
        }
        private set
        {
            _loginTime = value;
            setUpdateTimer();
        }
    }

    private int _lobbyTimeoutTime;
    public int lobbyTimeoutTime
    {
        get
        {
            return _lobbyTimeoutTime;
        }
        private set
        {
            _lobbyTimeoutTime = value;
            setUpdateTimer();
        }
    }

    private int _usedAllPetCollectsTime;

    public int usedAllPetCollectsTime
    {
        get
        {
            return _usedAllPetCollectsTime;
        }
        set
        {
            _usedAllPetCollectsTime = value;
            saveDataToStorage(); //this happens infrequently, so save now
        }
    }

    private int writeToDiskTime = 0;
    private int lastUpdateTime = 0;
    

    public static void resetStaticClassData()
    {
        if (_instance != null)
        {
            //routine runner will stop an update routine in it's reset call so force save now
            _instance.saveDataToStorage();
        }
        _instance = null;
    }

    private void setUpdateTimer()
    {
        //if idle timer hasn't been created or it's not active
        if (idleTimer == null || !idleTimer.isActive)
        {
            idleTimer = GameTimerRange.createWithTimeRemaining(idleThreshold);
            idleTimer.registerFunction(onIdleActivation);
        }
        else
        {
            idleTimer.updateEndTime(GameTimer.currentTime + idleThreshold);
        }
        
        if (writeToDiskTime <= GameTimer.currentTime)
        {
            writeToDiskTime = GameTimer.currentTime + WRITE_TO_PREFS_INTERVAL;
            RoutineRunner.instance.StartCoroutine(saveDataRoutine(WRITE_TO_PREFS_INTERVAL));
        }
    }


    public void onMouseInput()
    {
        lastInputTime = GameTimer.currentTime;
    }

    public int getTimeBetweenLogin()
    {
        return loginTime - savedTime;
    }

    public void onSpinReels()
    {
        lastSpinTime = GameTimer.currentTime;
    }

    public void onLogin()
    {
        loginTime = GameTimer.currentTime;
        savedTime = CustomPlayerData.getInt(CustomPlayerData.USER_ACTIVITY_TIMESTAMP, GameTimer.currentTime);
        _usedAllPetCollectsTime = CustomPlayerData.getInt(CustomPlayerData.USED_ALL_PET_COLLECT_TIMESTAMP, 0);
        string lastCollectTime = DailyBonusGameTimer.instance != null ? DailyBonusGameTimer.instance.dateLastCollected : "";
        if (!string.IsNullOrEmpty(lastCollectTime))
        {
            System.DateTime dt = System.DateTime.Parse(DailyBonusGameTimer.instance.dateLastCollected);
            _lastTimerCollectTime = Common.convertToUnixTimestampSeconds(dt);
        }
        else
        {
            Debug.LogWarning("Could not read in last collect time, use current game time");
            _lastTimerCollectTime = GameTimer.currentTime;
        }
        
        Server.registerEventDelegate("timer_outcome", processTimerOutcome, true);
        Server.registerEventDelegate("collect_bonus", processTimerOutcome, true); //old variants will send this down, newer users won't get this event
        idleTimer = new GameTimerRange(GameTimer.currentTime, GameTimer.currentTime + idleThreshold);
        idleTimer.registerFunction(onIdleActivation); 
    }

    private void saveDataToStorage()
    {
        CustomPlayerData.setValue(CustomPlayerData.USER_ACTIVITY_TIMESTAMP, lastActionTime);
        CustomPlayerData.setValue(CustomPlayerData.USED_ALL_PET_COLLECT_TIMESTAMP, usedAllPetCollectsTime);
    }

    private void processTimerOutcome(JSON data)
    {
        //set bonus collect time
        lastTimerCollectTime = GameTimer.currentTime;
        
        //if this was a pet collect set the last pet collect time

        if (!didPetCollectThisSession && data != null && data.hasKey("outcomes"))
        {
            if (data.getBool("virtual_pet_collect", false))
            {
                didPetCollectThisSession = true;
            }
        }

        //need to force a write here so user can't quickly reload on another device
        saveDataToStorage();
    }

    public void forceWriteToDisk()
    {
        saveDataToStorage();
    }

    private IEnumerator saveDataRoutine(int time)
    {
        yield return new TIWaitForSeconds(time);
        saveDataToStorage();
    }
    
#if !ZYNGA_PRODUCTION
    private int? debugSavedTime;
    public void debugForceIdleBeforeLogin(bool enabled)
    {
        if (enabled)
        {
            debugSavedTime = 0;
        }
        else
        {
            debugSavedTime = null;
        }
    }

    public void debugForceIdleNow()
    {
        if (!isIdle)
        {
            _loginTime = 0;
            _lastInputTime = 0; 
            _lastSpinTime = 0;
            _lobbyTimeoutTime = 0;
            if (!wasIdleBeforeLogin)
            {
                savedTime = GameTimer.currentTime - (idleThreshold + 1);
            }
        }
        onIdleActivation();
    }

    public void debugDecrementSavedTime(int secondsToRemove)
    {
        savedTime = savedTime - secondsToRemove;
        if (savedTime < 0)
        {
            savedTime = 0;
        }

        CustomPlayerData.setValue(CustomPlayerData.USER_ACTIVITY_TIMESTAMP, savedTime);
    }
#endif
}
