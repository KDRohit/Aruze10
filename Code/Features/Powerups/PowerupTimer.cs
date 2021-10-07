using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zap.Automation;
using TMPro;

public class PowerupTimer : MonoBehaviour
{
    public UISprite circleTimerSprite;
    public UISprite frameSprite;
    public TextMeshPro timerText;
    public GameObject powerupIconContainer;
    public Animator powerupAnim;
    public Animator powerupContainerAnim;
    public Animator idleAnim;
    public ButtonHandler buttonHandler;
    public PowerupBase powerup;
    public GameObject meterObject;
    public bool destroyOnExpire = true;
    public PowerupTint powerupTint;
    public ObjectSwapper textSwapper;
    public GameObject iconObject;

    private bool isActive;
    private bool isRunning;
    private bool switchAnimOrange = false;
    private bool switchAnimRed = false;
    private long prevTime = -1;
    private bool isCollectedPowerup;
    private bool playActivateAnimation = false;
    private bool isClickable = true;

    private const string ANIM_ACTIVATE = "activate";
    private const string ANIM_DEACTIVATE = "deactivate";
    private const string ANIM_GRAY = "gray";
    private const string ANIM_IDLE_RED = "red idle";
    private const string ANIM_IDLE_ORANGE = "orange idle";
    private const string ANIM_IDLE_GREEN = "green idle";
    private const string ANIM_SWITCH_TO_ORANGE = "green to orange";
    private const string ANIM_SWITCH_TO_RED = "orange to red";
    private const string ANIM_REFILL_RED = "red refill";
    private const string ANIM_REFILL_ORANGE = "orange refill";
    private const string ANIM_REFILL_GREEN = "green refill";
    private const string ANIM_TURN_ON = "On";
    private const string ANIM_IDLE_ON = "on";
    private const int TIME_SWITCH_ORANGE = 180; //3 mins
    private const int TIME_SWITCH_RED = 60; //1 min
    private const float IDLE_ANIM_DELAY = 10.0f;
    
    
    public void init(PowerupBase powerup, bool isRunning = true, bool isActive = true, bool destroyOnExpire = true, bool delay = false, bool clickable = true)
    {
        timerText.text = CommonText.secondsFormatted(powerup.duration);

        this.powerup = powerup;
        playActivateAnimation = delay;
        
        if (powerup.isPending)
        {
            timerText.text = Localize.text("loading").ToUpper();
        }
        
        if (delay)
        {
            RoutineRunner.instance.StartCoroutine(delayedRefresh(powerup, isRunning, isActive, destroyOnExpire, clickable));
        }
        else
        {
            refresh(powerup, isRunning, isActive, destroyOnExpire, clickable);
        }
        
        if (buttonHandler != null)
        {
            buttonHandler.registerEventDelegate(onClick);
        }
        powerup.getPrefab(onPrefabLoaded);

        frameSprite.spriteName = PowerupBase.POWERUP_ICONS_FRAMES[(int)powerup.rarity - 1];

        updateMeterTint();
    }
    
    public void playIdleAnims()
    {
        idleAnim.Play(ANIM_IDLE_ON);
        StartCoroutine(playIdleAnimWithCooldown());
    }

    private IEnumerator playIdleAnimWithCooldown()
    {
        while (true)
        {
            yield return new WaitForSeconds(IDLE_ANIM_DELAY);
            idleAnim.Play(ANIM_IDLE_ON);
        }
    }

    private void updateMeterTint()
    {
        if (meterObject != null)
        {
            PowerupTint meterTint = meterObject.GetComponent<PowerupTint>();

            if (meterTint != null)
            {
                meterTint.setPowerup(powerup);
            }
        }
    }

    private IEnumerator delayedRefresh(PowerupBase powerup, bool isRunning = true, bool isActive = true,
        bool destroyOnExpire = true, bool clickable = true)
    {
        while (!PackDroppedDialog.completedPowerupDropRoutine)
        {
            yield return null;
        }

        yield return new WaitForSeconds(1);
        
        refresh(powerup, isRunning, isActive, destroyOnExpire, clickable);
    }

    private void onClick(Dict args = null)
    {
        if (SlotBaseGame.instance != null && SlotBaseGame.instance.isGameBusy)
        {
            return;
        }
        if (powerup != null && isClickable)
        {
            PowerupInfoDialog.showDialog(powerup);
        }
    }

    public void refresh(PowerupBase powerup, bool isRunning = true, bool isActive = true, bool destroyOnExpire = true,
        bool clickable = true)
    {
        this.destroyOnExpire = destroyOnExpire;
        this.powerup = powerup;
        this.isRunning = isRunning;
        this.isActive = isActive;
        this.isClickable = clickable;

        if (powerup == null)
        {
            return;
        }

        if (powerup.isPending)
        {
            timerText.text = Localize.text("loading").ToUpper();
        }
        else if (powerup.runningTimer != null)
        {
            powerup.runningTimer.registerLabel(timerText);
        }
        else
        {
            timerText.text = CommonText.secondsFormatted(powerup.duration);
        }

        circleTimerSprite.fillAmount = 1.0f;

        if (isRunning && powerup.runningTimer != null)
        {
            powerup.runningTimer.removeFunction(onPowerupExpire);
            powerup.runningTimer.registerFunction(onPowerupExpire);
        }

        updateMeterTint();

        setAnimation();
    }

    private void setAnimation()
    {
        if (powerupAnim == null || powerup == null)
        {
            return;
        }
        
        if (isRunning)
        {
            if (powerup.runningTimer == null)
            {
                powerupAnim.Play(ANIM_DEACTIVATE);
                return;
            }
            
            updateMeterTint();
            powerupAnim.enabled = true;

            if (powerup.runningTimer.timeRemaining < TIME_SWITCH_RED)
            {
                powerupAnim.Play(ANIM_IDLE_RED);
                switchAnimRed = false;
            }
            else if (powerup.runningTimer.timeRemaining < TIME_SWITCH_ORANGE)
            {
                powerupAnim.Play(ANIM_IDLE_ORANGE);
                switchAnimOrange = false;
            }
            else
            {
                if (playActivateAnimation)
                {
                    powerupAnim.Play(ANIM_ACTIVATE);
                    powerupContainerAnim.Play(ANIM_TURN_ON);
                }
                else
                {
                    powerupAnim.Play(ANIM_IDLE_GREEN);
                }
                
                switchAnimRed = false;
                switchAnimOrange = false;
            }
        }
        else if (isActive && !isCollectedPowerup)
        {
            powerupAnim.Play(ANIM_DEACTIVATE);
        }
        else if (isCollectedPowerup)
        {
            powerupAnim.enabled = false;
            if (powerupAnim.gameObject != null && powerupAnim.gameObject.activeSelf)
            {
                powerupAnim.Play(ANIM_DEACTIVATE);    
            }
            
        }
        else
        {
            if (powerupAnim.gameObject != null && powerupAnim.gameObject.activeSelf)
            {
                powerupAnim.Play(ANIM_GRAY);    
            }
        }
    }

    private void onPowerupExpire(Dict args, GameTimerRange sender)
    {
        updateMeterTint();

        if (destroyOnExpire)
        {
            Destroy(gameObject);
        }
        else
        {
            isRunning = false;
            isActive = false;

            if (timerText != null)
            {
                timerText.text = CommonText.secondsFormatted(powerup.duration);
            }

            if (powerup != null && powerup.runningTimer != null)
            {
                powerup.runningTimer.removeFunction(onPowerupExpire);
                powerup.runningTimer = null;
            }

            if (powerupAnim != null)
            {
                powerupAnim.Play(ANIM_GRAY);
            }
        }
    }
    
    private void onPrefabLoaded(GameObject prefab)
    {
        iconObject = CommonGameObject.instantiate(prefab, powerupIconContainer.transform) as GameObject;

        if (iconObject != null)
        {
            powerupTint = iconObject.GetComponent<PowerupTint>();
        }

        if (useTextMasks)
        {
            setTextMasks();
        }
    }

    public void setTextMasks()
    {
        if (textSwapper != null)
        {
            textSwapper.setState("masked");
        }

        if (iconObject != null)
        {
            ObjectSwapper iconObjectSwapper = iconObject.GetComponent<ObjectSwapper>();

            if (iconObjectSwapper != null)
            {
                iconObjectSwapper.setState("masked");
            }
        }
    }

    /// <summary>
    /// setIsCollectedCard if this instance of the timer is showing from a collections card asset, and the card is collected
    /// </summary>
    /// <param name="isCollected"></param>
    public void setIsCollectedCard(bool isCollected)
    {
        if (powerupTint != null)
        {
            isCollectedPowerup = isCollected;

            powerupTint.isCollectedCardElement = isCollected;

            powerupTint.setTintColor(isCollected);
        }

        if (isCollected)
        {
            setAnimation();
        }
    }

    public void updateTint()
    {
        if (powerupTint != null)
        {
            powerupTint.setPowerup(powerup, isActive || powerupTint.isCollectedCardElement);
        }

        updateMeterTint();
    }
    
    

    public void Update()
    {
        if (isRunning && powerup != null && powerup.runningTimer != null && circleTimerSprite != null)
        {
            if (prevTime != -1 && powerup.runningTimer.timeRemaining > prevTime)
            {
                if (prevTime <= TIME_SWITCH_RED)
                {
                    powerupAnim.Play(ANIM_REFILL_RED);
                }
                else if (prevTime <= TIME_SWITCH_ORANGE)
                {
                    powerupAnim.Play(ANIM_REFILL_ORANGE);
                }
                else
                {
                    powerupAnim.Play(ANIM_REFILL_GREEN);
                }
                switchAnimRed = false;
                switchAnimOrange = false;
            }
            circleTimerSprite.fillAmount = (float)powerup.runningTimer.timeRemaining/(float)powerup.duration;
            if (!switchAnimRed && powerup.runningTimer.timeRemaining <= TIME_SWITCH_RED)
            {
                powerupAnim.Play(ANIM_SWITCH_TO_RED);
                switchAnimRed = true;
            }
            else if (!switchAnimOrange && powerup.runningTimer.timeRemaining <= TIME_SWITCH_ORANGE && powerup.runningTimer.timeRemaining > TIME_SWITCH_RED)
            {
                powerupAnim.Play(ANIM_SWITCH_TO_ORANGE);
                switchAnimOrange = true;
            }

            prevTime = powerup.runningTimer.timeRemaining;
        }
    }

    private void OnEnable()
    {
        setAnimation();
    }

    private void OnDestroy()
    {
        if (powerup != null && powerup.runningTimer != null)
        {
            powerup.runningTimer.removeLabel(timerText);
            powerup.runningTimer.removeFunction(onPowerupExpire);
        }
    }

    private bool _useTextMasks;
    public bool useTextMasks
    {
        get { return _useTextMasks; }
        set
        {
            _useTextMasks = value;

            if (_useTextMasks)
            {
                setTextMasks();
            }
        }
    }
}
