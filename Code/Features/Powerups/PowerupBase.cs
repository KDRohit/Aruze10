using System;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using Zynga.Core.Util;
using Object = UnityEngine.Object;

public class PowerupBase
{
    // =============================
    // PRIVATE
    // =============================
    private GameObject prefab = null;

    // =============================
    // PUBLIC
    // =============================
    public int duration         = 0;
    public bool isPending       = false;
    public string name         { get; protected set; }
    public string source       { get; protected set; }
    public string uiPrefabName { get; protected set; }
    public string actionName   { get; protected set; }
    public Rarity rarity       { get; protected set; }
    public bool isDisplayablePowerup { get; protected set; }
    
    public GameTimerRange runningTimer = null;
    public delegate void PowerupIconDelgate(GameObject prefab);

    public static Dictionary<string, Type> typeDict { get; protected set; } // factory list associating name to powerup
    public static List<PowerupBase> powerups { get; protected set; } // list of each type of powerup

    public static List<KeyValuePair<string, string>> patternList = new List<KeyValuePair<string, string>>();

    // =============================
    // CONST
    // =============================
    // We path to the UI via this where the token = the name of the powerup
    public const string POWERUP_ICON_CONTAINER_UI_PATH = "Features/PowerUps/Prefabs/Instanced Prefabs/PowerUps Icon Container Item";
    public const string POWERUP_ICON_UI_PATH = "Features/PowerUps/Prefabs/Instanced Prefabs/PowerUp Icons/";
    public const string POWERUPS_SET_ASSET = "Features/PowerUps/Prefabs/Instanced Prefabs/Collections PowerUp Items/PowerUps Card Set Item";

    public static readonly string[] POWERUP_ICONS_FRAMES = { "PowerUp Icon Frame 1", "PowerUp Icon Frame 2", "PowerUp Icon Frame 3", "PowerUp Icon Frame 4", "PowerUp Icon Frame 5" };

    public const string POWER_UP_BUY_PAGE_KEY           = "powerup_purchase_bonus_15";
    public const string POWER_UP_DAILY_BONUS_KEY        = "powerup_dailybonus_every_15m";
    public const string POWER_UP_BIG_WINS_KEY           = "powerup_double_big_wins";
    public const string POWER_UP_DOUBLE_MAX_VOLTAGE_KEY = "powerup_double_max_voltage_token";
    public const string POWER_UP_DOUBLE_VIP_KEY         = "powerup_double_vip_token";
    public const string POWER_UP_EVEN_LEVELS_KEY        = "powerup_level_up_even";
    public const string POWER_UP_ODD_LEVELS_KEY         = "powerup_level_up_odd";
    public const string POWER_UP_FREE_SPINS_KEY         = "powerup_double_free_spin_gift_limit";
    public const string POWER_UP_ROYAL_RUSH_KEY         = "powerup_rr_score_25";
    public const string POWER_UP_TRIPLE_XP_KEY          = "powerup_triple_xp";
    public const string POWER_UP_VIP_BOOSTS_KEY         = "powerup_vip_boost";
    public const string POWER_UP_WEEKLY_RACE_KEY        = "powerup_wr_score_25";
    public const string LEVEL_LOTTO_TRIPLE_XP_KEY       = "level_lotto_triple_xp";
    public const string BUNDLE_SALE_DAILY_BONUS         = "dailybonus_every";
    public const string BOARDGAME_MINISLOT_BOOST        = "boardgame_minislot_1x";
    
    protected List<string> aliasKeys = new List<string>();

    public enum Rarity
    {
        NONE = 0,
        COMMON = 1,
        UNCOMMON = 2,
        RARE = 3,
        VERY_RARE = 4,
        EPIC = 5,
    }

    public static Dictionary<string, string> collectablesPowerupsMap = new Dictionary<string, string>();

    /*=========================================================================================
    POWERUP INITIALIZATION/STATIC METHODS
    =========================================================================================*/
    private static void initPowerups()
    {
        typeDict = new Dictionary<string, Type>();
        powerups = new List<PowerupBase>();

        Assembly asm = ReflectionHelper.GetAssemblyByName(ReflectionHelper.ASSEMBLY_NAME_MAP[ReflectionHelper.ASSEMBLIES.PRIMARY]);

        if (asm != null)
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.BaseType == typeof(PowerupBase))
                {
                    PowerupBase powerup = Activator.CreateInstance(type) as PowerupBase;
                    powerup.init();
                    powerups.Add(powerup);
                    typeDict.Add(powerup.name, type);
                   
                    foreach (string alias in powerup.aliasKeys)
                    {
                        if(patternList != null && alias != null && alias != "")
                            patternList.Add(new KeyValuePair<string, string>(alias,powerup.name));
                    }
                }
            }
        }
    }

    public static void preloadAssets()
    {
        initPowerups();
    }

    public static PowerupBase getPowerupByName(string keyName)
    {
        for (int i = 0; i < powerups.Count; ++i)
        {
            if (powerups[i].name == keyName)
            {
                return powerups[i];
            }
        }

        return null;
    }

    public static void registerFunctionToPowerup(PowerupBase powerup, GameTimerRange.onExpireDelegate f)
    {
        if (powerup != null && powerup.runningTimer != null)
        {
            powerup.runningTimer.registerFunction(f);
        }
    }

    public static void unregisterFunctionToPowerup(PowerupBase powerup, GameTimerRange.onExpireDelegate f)
    {
        if (powerup != null && powerup.runningTimer != null)
        {
            powerup.runningTimer.removeFunction(f);
        }
    }

    /*=========================================================================================
    CLASS METHODS
    =========================================================================================*/
    protected virtual void init(JSON data = null)
    {
        isDisplayablePowerup = true;
    }
    
    public virtual void apply(int totalTime, int durationRemaining)
    {
        duration = totalTime;

        // expiration timer is slightly in front of server to give a small buffer for latency
        if (runningTimer != null)
        {
            runningTimer.updateEndTime(durationRemaining - Data.liveData.getInt("CLIENT_TIMER_EXPIRATION_BUFFER", 0));
        }
        else if(durationRemaining > 0)
        {
            runningTimer = GameTimerRange.createWithTimeRemaining(durationRemaining - Data.liveData.getInt("CLIENT_TIMER_EXPIRATION_BUFFER", 0));
        }
    }

    public virtual void onPowerupCreate(string key)
    {
        
    }

    public virtual void remove(Dict args = null, GameTimerRange sender = null)
    {

    }

    public virtual void onActivated()
    {

    }

    public virtual void doAction()
    {
        Dialog.close();
    }

    private static PowerupBase createPowerUp(string key, JSON data,int duration,int timeRemaining,string designation)
    {
        PowerupBase powerup = null;
        powerup = Activator.CreateInstance(typeDict[key]) as PowerupBase;
        powerup.init(data);
        powerup.onPowerupCreate(designation);
        powerup.apply(duration, timeRemaining);
        return powerup;
    }

    public static PowerupBase createWithDesignation(string designation, int duration, int timeRemaining, JSON data = null)
    {
        if (timeRemaining <= 0)
        {
            return null;
        }

        PowerupBase powerup = null;
        if (typeDict.ContainsKey(designation))
        {
            powerup = createPowerUp(designation, data, duration, timeRemaining, designation);
        }
        else
        {
            foreach (KeyValuePair<string,string> pattern in patternList)
            {
                if (designation.Contains(pattern.Key) && typeDict.ContainsKey(pattern.Value))
                {
                    powerup = createPowerUp(pattern.Value, data, duration, timeRemaining, designation);
                }
            }
        }

        return powerup;
    }

    public void getPrefab(PowerupIconDelgate onSuccessCallback = null)
    {
        if (prefab == null)
        {
            if (isDisplayablePowerup)
            {
                AssetBundleManager.load
                (
                    POWERUP_ICON_UI_PATH + uiPrefabName,
                    delegate(string path, Object o, Dict data)
                    {
                        onPrefabLoadSuccess(path, o, data);
                        if (onSuccessCallback != null)
                        {
                            onSuccessCallback(prefab);
                        }
                    },
                    onPrefabLoadFail
                );
            }
        }
        else if (onSuccessCallback != null)
        {
            onSuccessCallback(prefab);
        }
    }

    private void onPrefabLoadSuccess(string assetPath, object obj, Dict data = null)
    {
        prefab = obj as GameObject;
    }

    private void onPrefabLoadFail(string assetPath, Dict data = null)
    {
        Debug.LogError("Failed to load powerup icon: " + assetPath);
    }

    public virtual bool canPerformAction
    {
        get
        {
            return true;
        }
    }
}
