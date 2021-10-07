using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PowerupInGameUI : InGameFeatureDisplay
{
    // =============================
    // PRIVATE
    // =============================
    // Used to size background as timers are added or removed
    private bool isExpanded = false;
    private PowerupsLocation location;
    private List<string> newCollectedPowerups = new List<string>();
    private List<PowerupBase> pendingPowerups = new List<PowerupBase>();

    private List<KeyValuePair<PowerupBase, PowerupTimer>> powerups = new List<KeyValuePair<PowerupBase, PowerupTimer>>();

    // Keep a cached reference so we can add them as needed to the scene
    private GameObject cachedPowerupTimerObject;

    // =============================
    // PUBLIC
    // =============================
    public enum PowerupsLocation
    {
        IN_GAME,
        COLLECTIONS_DIALOG
    }

    [SerializeField] private ButtonHandler expandButton;
    [SerializeField] private ButtonHandler expandButtonWithNumber;
    [SerializeField] private ClickHandler collapseButton;
    [SerializeField] private UIGrid timerGrid;
    [SerializeField] private UIAnchor buttonAnchor;
    [SerializeField] private UIAnchor gridAnchor;
    [SerializeField] private GameObject container;
    [SerializeField] private GameObject smallPanel;
    [SerializeField] private GameObject largePanel;
    [SerializeField] private GameObject streakSmall;
    [SerializeField] private GameObject streakLarge;
    [SerializeField] private TextMeshPro expandCountLabel;
    [SerializeField] private Animator panelAnim;
    [SerializeField] private float builtInProgressiveGameOffset;

    // =============================
    // CONST
    // =============================
    private const string ANIM_IN = "in";
    private const string ANIM_OUT = "out";
    private const int SMALL_PANEL_DISPLAY_LIMIT = 3;
    public const string UI_PATH = "Features/PowerUps/Prefabs/PowerUps In Game Panel";

    public override void init(Dict args = null)
    {
        List<string> collectedPowerups = (List<string>) args.getWithDefault(D.DATA, null);
        PowerupsLocation argsLocation = (PowerupsLocation) args.getWithDefault(D.MODE, PowerupsLocation.IN_GAME);
        this.location = argsLocation;
        container.SetActive(false);
        CommonGameObject.setLayerRecursively(gameObject, getLayer());

        newCollectedPowerups = collectedPowerups;
        if (PowerupsManager.isPowerupStreakActive && collectedPowerups != null)
        {
            for (int i = 0; i < PowerupsManager.powerupsInStreak.Count; i++)
            {
                newCollectedPowerups.Add(PowerupsManager.powerupsInStreak[i]);
            }
            StartCoroutine(activateStreak());
        }
        AssetBundleManager.load(PowerupBase.POWERUP_ICON_CONTAINER_UI_PATH, timerLoadSuccess, timerLoadFailure);

        registerEvents();
        setButtons();
        toggleExpandedPanel(isExpanded);
        transitionPanel(true);
    }
    
    public void registerEvents()
    {
        PowerupsManager.addEventHandler(onPowerupActivated);
        expandButton.registerEventDelegate(onClickExpandButton);
        expandButtonWithNumber.registerEventDelegate(onClickExpandButton);
        collapseButton.registerEventDelegate(onClickExpandButton);
        SlotBaseGame.onSpinPressed += onSpinPressed;
        ResolutionChangeHandler.instance.addOnResolutionChangeDelegate(onResolutionChanged);
    }

    public void unregisterEvents()
    {
        PowerupsManager.removeEventHandler(onPowerupActivated);
        expandButton.unregisterEventDelegate(onClickExpandButton);
        expandButtonWithNumber.unregisterEventDelegate(onClickExpandButton);
        collapseButton.unregisterEventDelegate(onClickExpandButton);
        SlotBaseGame.onSpinPressed -= onSpinPressed;
        ResolutionChangeHandler.instance.removeOnResolutionChangeDelegate(onResolutionChanged);
    }

    public void onResolutionChanged()
    {
        gridAnchor.enabled = true;
    }

    public void onSpinPressed()
    {
        toggleExpandedPanel(false);
    }

    private void addAllPowerups()
    {
        for (int i = 0; i < PowerupBase.powerups.Count; ++i)
        {
            PowerupBase powerup = PowerupBase.powerups[i];

            if (!powerup.isDisplayablePowerup)
            {
                continue;
            }
            
            PowerupTimer timer = createPowerupAssets();

            bool isActive = PowerupsManager.hasActivePowerupByName(powerup.name);

            if (isActive)
            {
                powerup = PowerupsManager.getActivePowerup(powerup.name);
            }

            bool delay = location == PowerupsLocation.COLLECTIONS_DIALOG && newCollectedPowerups != null &&
                         newCollectedPowerups.Contains(powerup.name);

            if (newCollectedPowerups != null &&
                newCollectedPowerups.Contains(powerup.name) &&
                !PowerupsManager.hasActivePowerupByName(powerup.name))
            {
                powerup.isPending = true;
                pendingPowerups.Add(powerup);
            }
            else
            {
                powerup.isPending = false;
            }

            timer.init(powerup, isActive, isActive, false, delay);
            powerups.Add(new KeyValuePair<PowerupBase, PowerupTimer>(powerup, timer));

            if (isActive)
            {
                powerup.runningTimer.registerFunction(onPowerupExpire);
            }

            timer.gameObject.SetActive(isExpanded);
        }

        gridAnchor.enabled = location == PowerupsLocation.IN_GAME;
    }

    void Update()
    {
        if (Dialog.instance.isShowing)
        {
            if (isExpanded &&
                Dialog.instance.currentDialog.type.keyName != "powerups_info_dialog" &&
                Dialog.instance.currentDialog.type.keyName != "collectables_pack_dropped" &&
                location == PowerupsLocation.IN_GAME)
            {
                toggleExpandedPanel(false);
            }
        }
    }

    private void OnEnable()
    {
        sortDisplay();
        timerGrid.Reposition();
        buttonAnchor.reposition();
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<PowerupBase, PowerupTimer> timerPair in powerups)
        {
            if (timerPair.Value.isActiveAndEnabled)
            {
                timerPair.Key.runningTimer.removeFunction(onPowerupExpire);
            }
        }

        unregisterEvents();
    }

    public void transitionPanel(bool animateInPanel)
    {
        if (this == null || panelAnim == null)
        {
            return;
        }

        if (animateInPanel)
        {
            hideAllPowerups();
        }

        gridAnchor.enabled = true;
        panelAnim.Play(animateInPanel ? ANIM_IN : ANIM_OUT);

        StartCoroutine(delayShowingPowerups());
    }

    private void hideAllPowerups()
    {
        for (int i = 0; i < powerups.Count; ++i)
        {
            powerups[i].Value.gameObject.SetActive(false);
        }
    }

    private IEnumerator delayShowingPowerups()
    {
        yield return null;

        showActivePowerups();
    }

    private void showActivePowerups()
    {
        for (int i = 0; i < powerups.Count; ++i)
        {
            if (powerups[i].Value.gameObject.activeSelf != isExpanded && getActivePowerupByName(powerups[i].Key.name) == null)
            {
                powerups[i].Value.gameObject.SetActive(isExpanded);
            }
        }

        sortDisplay();
        timerGrid.Reposition();
        buttonAnchor.reposition();
    }

    private IEnumerator activateStreak()
    {
        while (!PackDroppedDialog.completedPowerupDropRoutine)
        {
            yield return null;
        }

        yield return new WaitForSeconds(1);
        setStreak();
    }

    public void onClickExpandButton(Dict args = null)
    {
        isExpanded = !isExpanded;
        toggleExpandedPanel(isExpanded);

        if (isExpanded)
        {
            Audio.play("PowerupOpenCollections");
        }
        else
        {
            Audio.play("PowerupCloseCollections");
        }
    }

    private bool streakActivated = false;

    private void setStreak()
    {
        bool streakActive = PowerupsManager.isPowerupStreakActive;
        if (streakActivated == streakActive)
        {
            return;
        }
        streakActivated = streakActive;

        if (streakSmall != null)
        {
            streakSmall.SetActive(streakActive);
        }

        if (streakLarge != null)
        {
            streakLarge.SetActive(streakActive);
        }
    }

    private void setButtons()
    {
        if (totalPowerups > SMALL_PANEL_DISPLAY_LIMIT)
        {
            expandButton.gameObject.SetActive(false);
            expandButtonWithNumber.gameObject.SetActive(true);
            expandCountLabel.text = (activePowerups.Count - SMALL_PANEL_DISPLAY_LIMIT).ToString();
        }
        else
        {
            expandButton.gameObject.SetActive(true);
            expandButtonWithNumber.gameObject.SetActive(false);
        }
    }

    private void toggleExpandedPanel(bool expand)
    {
        if (smallPanel != null && largePanel != null)
        {
            smallPanel.SetActive(!expand);
            largePanel.SetActive(expand);
            isExpanded = expand;

            showActivePowerups();
        }
        setStreak();
    }

    public void timerLoadSuccess(string assetPath, object loadedObj, Dict data = null)
    {
        // Cache this in case a powerup drops later
        cachedPowerupTimerObject = loadedObj as GameObject;
        addAllPowerups();

        for (int i = 0; i < PowerupsManager.activePowerups.Count; i++)
        {
            if (!PowerupsManager.activePowerups[i].runningTimer.isExpired)
            {
                activatePowerup(PowerupsManager.activePowerups[i]);
            }
        }

        if (location == PowerupsLocation.COLLECTIONS_DIALOG || totalPowerups > 0)
        {
            container.SetActive(true);
        }

        sortDisplay();
        resizeUI();
        toggleExpandedPanel(isExpanded);
    }

    public void timerLoadFailure(string assetPath, Dict data = null)
    {
        Debug.LogError("Failed to load timer at path " + assetPath);
    }

    public void onPowerupActivated(PowerupBase powerup)
    {
        if (location != PowerupsLocation.COLLECTIONS_DIALOG)
        {
            activatePowerup(powerup);
            setStreak();
        }
        else
        {
            StartCoroutine(activationDelay(powerup));
        }
    }

    public void activatePowerup(PowerupBase powerup)
    {
        bool delay = location == PowerupsLocation.COLLECTIONS_DIALOG && newCollectedPowerups != null && newCollectedPowerups.Contains(powerup.name);
        int layer = getLayer();
        
        for (int i = 0; i < powerups.Count; ++i)
        {
            if (powerups[i].Key.name == powerup.name)
            {
                if (!delay)
                {
                    powerups[i].Value.refresh(powerup, true, true, false);
                    powerups[i].Value.updateTint();
                }
                
                powerup.runningTimer.removeFunction(onPowerupExpire);
                powerup.runningTimer.registerFunction(onPowerupExpire);
            }

            if (powerups[i].Value != null && powerups[i].Value.powerupTint != null)
            {
                powerups[i].Value.powerupTint.updateLayer(layer);    
            }
            
        }

        container.SetActive(totalPowerups > 0);

        removePendingPowerup(powerup);

        sortDisplay();
        resizeUI();

        if (!delay)
        {
            setStreak();
        }

        setButtons();
    }

    private IEnumerator activationDelay(PowerupBase powerup)
    {
        yield return new WaitForSeconds(4);

        activatePowerup(powerup);

        setStreak();
    }

    private PowerupTimer createPowerupAssets()
    {
        GameObject timerInstance = CommonGameObject.instantiate(cachedPowerupTimerObject, timerGrid.gameObject.transform) as GameObject;
        CommonGameObject.setLayerRecursively(timerInstance, getLayer());
        PowerupTimer timer = timerInstance.GetComponent<PowerupTimer>();
        return timer;
    }

    private int getLayer()
    {
        int layer = Layers.ID_NGUI;
        if (location == PowerupsLocation.IN_GAME)
        {
            if (transform.parent != null && transform.parent.gameObject != null)
            {
                layer = transform.parent.gameObject.layer;
            }
            else
            {
                layer = Layers.ID_NGUI_OVERLAY;
            }
        }

        return layer;
    }

    private void resizeUI()
    {
        buttonAnchor.enabled = true;

        gridAnchor.enabled = location == PowerupsLocation.IN_GAME;
        
        sortDisplay();
    }

    private void onPowerupExpire(Dict args, GameTimerRange sender)
    {
        if (this != null && gameObject != null && container != null)
        {
            container.SetActive(activePowerups.Count > 0 && PowerupsManager.hasAnyPowerupsToDisplay());
            sortDisplay();
            resizeUI();
            setStreak();
            setButtons();
        }
    }

    /*=========================================================================================
    ANCILLARY
    =========================================================================================*/
    private void sortDisplay()
    {
        activePowerups.Sort(sortPowerupsByTime);
        for (int i = 0; i < powerups.Count; ++i)
        {
            bool setToActive = isExpanded || isSmallPanelPowerupVisible(powerups[i].Key.name);

            if (powerups[i].Value != null && powerups[i].Value.gameObject != null && powerups[i].Value.gameObject.activeSelf != setToActive)
            {
                powerups[i].Value.gameObject.SetActive(setToActive);
            }
        }

        timerGrid.Reposition();
    }

    private bool isSmallPanelPowerupVisible(string name)
    {
        int i;
        for (i = 0; i < activePowerups.Count; ++i)
        {
            if (activePowerups[i].Key.name == name && i < SMALL_PANEL_DISPLAY_LIMIT)
            {
                return true;
            }
        }

        for (i = 0; i < pendingPowerups.Count; ++i)
        {
            if (pendingPowerups[i].name == name && i < SMALL_PANEL_DISPLAY_LIMIT - activePowerups.Count)
            {
                return true;
            }
        }

        return false;
    }

    private PowerupBase getActivePowerupByName(string name)
    {
        for (int i = 0; i < activePowerups.Count; ++i)
        {
            if (activePowerups[i].Key.name == name)
            {
                return activePowerups[i].Key;
            }
        }

        return null;
    }

    private void removePendingPowerup(PowerupBase powerup)
    {
        for (int i = 0; i < pendingPowerups.Count; ++i)
        {
            if (pendingPowerups[i].name == powerup.name)
            {
                pendingPowerups.RemoveAt(i);
                break;
            }
        }
    }

    private int totalPowerups
    {
        get { return activePowerups.Count + pendingPowerups.Count; }
    }

    private List<KeyValuePair<PowerupBase, PowerupTimer>> _activePowerups = new List<KeyValuePair<PowerupBase, PowerupTimer>>();
    private List<KeyValuePair<PowerupBase, PowerupTimer>> activePowerups
    {
        get
        {
            if (_activePowerups.Count != PowerupsManager.activePowerups.Count)
            {
                _activePowerups.Clear();
                for (int i = 0; i < powerups.Count; ++i)
                {
                    if (PowerupsManager.hasActivePowerupByName(powerups[i].Key.name))
                    {
                        _activePowerups.Add(new KeyValuePair<PowerupBase, PowerupTimer>(powerups[i].Key, powerups[i].Value));
                    }
                }
            }

            return _activePowerups;
        }
    }

    private static int sortPowerupsByTime(KeyValuePair<PowerupBase, PowerupTimer> a, KeyValuePair<PowerupBase, PowerupTimer> b)
    {
        PowerupBase p1 = PowerupsManager.getActivePowerup(a.Key.name);
        PowerupBase p2 = PowerupsManager.getActivePowerup(b.Key.name);

        return PowerupsManager.sortPowerupsByTime(p1, p2);
    }
}