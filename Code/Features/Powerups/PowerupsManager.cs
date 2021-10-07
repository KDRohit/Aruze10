using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Rewardables;
using Zynga.Core.Util;

public class PowerupsManager : IResetGame
{
	public static List<PowerupBase> activePowerups = new List<PowerupBase>();
	public static readonly List<string> powerupsInStreak = new List<string>();
	private static Dictionary<string, int> powerupsStaticDurations = new Dictionary<string, int>();

	public delegate void OnPowerupActivatedDelegate(PowerupBase powerupBase);
	private static event OnPowerupActivatedDelegate onPowerupActivated;

	// =============================
	// CONST
	// =============================
	private const string BUFFS_KEY = "buffs";
	private const int STREAK_THRESHOLD = 3;

	public static void populateStaticData(JSON[] data)
	{
		if (data != null)
		{
			for (int i = 0; i < data.Length; i++)
			{
				string key = data[i].getString("key_name", "");
				int duration = data[i].getInt("duration", 0);
				powerupsStaticDurations.Add(key, duration);
			}
		}
	}

	/// <summary>
	/// Parses data from login for users current active powerups
	/// </summary>
	public static void init()
	{
		foreach (KeyValuePair<string, int> powerupData in powerupsStaticDurations)
		{
			PowerupBase powerup = PowerupBase.getPowerupByName(powerupData.Key);
			if (powerup != null)
			{
				powerup.apply(powerupData.Value, 0);
			}
		}
		
		if (Data.login != null)
		{
			RewardablesManager.addEventHandler(onRewardGranted);
			JSON playerJSON = Data.player;
			
			if (Data.login.hasKey("powerups_reward_values"))
			{
				PowerupBase.collectablesPowerupsMap = new Dictionary<string, string>(Data.login.getStringStringDict("powerups_reward_values"));
			}

			if (playerJSON != null)
			{
				if (!playerJSON.hasKey(BUFFS_KEY))
				{
					return;
				}

				JSON buffsData = playerJSON.getJSON(BUFFS_KEY);
				if (buffsData == null)
				{
					return;
				}

				foreach (string buffName in buffsData.getKeyList())
				{
					JSON buffDetails = buffsData.getJSON(buffName);
					createPowerup(buffName, buffDetails);
					
					
				}

				activePowerups.Sort(sortPowerupsByTime);
			}
		}
	}

	private static void onRewardGranted(Rewardable rewardable)
	{
		RewardPowerup rewardPowerup = rewardable as RewardPowerup;

		if (rewardPowerup != null)
		{
			powerupsInStreak.Clear();

			PowerupBase powerup = createPowerup(rewardPowerup.powerupName, rewardPowerup.data);

			if (powerup != null)
			{
				powerup.onActivated();
			}

			for (int i = 0; i < rewardPowerup.powerupsInStreak.Count; ++i)
			{
				string name = rewardPowerup.powerupsInStreak[i];

				powerupsInStreak.Add(name);
				createPowerup(name, rewardPowerup.streakData.getJSON(name));
			}

			rewardable.consume();
		}
	}

	/// <summary>
	/// Creates a powerup based on data
	/// </summary>
	/// <param name="name"></param>
	/// <param name="data"></param>t
	/// <returns></returns>
	public static PowerupBase createPowerup(string name, JSON data)
	{
		long endtime = data.getLong("end_ts", 0);
		int duration = 0;
		powerupsStaticDurations.TryGetValue(name, out duration);
		int timeRemaining = (int) (endtime - TimeUtil.CurrentTimestamp());

		if (!hasActivePowerupByName(name))
		{
			PowerupBase powerup = PowerupBase.createWithDesignation(name, duration, timeRemaining, data);

			if (powerup != null)
			{
				Dict args = Dict.create(D.OBJECT, powerup);

				powerup.runningTimer.registerFunction(onPowerupExpired, args);
				activePowerups.Add(powerup);
				notifyPowerup(powerup);
				InGameFeatureContainer.addInGameFeatures();
			}
			else
			{
				Debug.LogError("Powerup is not implemented: " + name);
			}

			return powerup;
		}

		PowerupBase activePowerup = getActivePowerup(name);
		if (activePowerup != null)
		{
			activePowerup.apply(duration, timeRemaining);
			notifyPowerup(activePowerup);
			return activePowerup;
		}

		return null;
	}

	/// <summary>
	/// Method called when powerup expires
	/// </summary>
	/// <param name="args"></param>
	/// <param name="sender"></param>
	private static void onPowerupExpired(Dict args, GameTimerRange sender)
	{
		if (args.containsKey(D.OBJECT))
		{
			PowerupBase powerup = (args[D.OBJECT] as PowerupBase);
			if (activePowerups.Contains(powerup))
			{
				activePowerups.Remove(powerup);
			}
			else
			{
				Debug.LogError("Missing boost " + powerup.name + " from list when we went to remove it");
			}
		}
		else
		{
			Debug.LogError("Missing object key when we went to remove a boost");
		}
	}

	/*=========================================================================================
	EVENT HANDLING
	=========================================================================================*/
	public static void addEventHandler(OnPowerupActivatedDelegate handler)
	{
		onPowerupActivated -= handler;
		onPowerupActivated += handler;
	}

	public static void removeEventHandler(OnPowerupActivatedDelegate handler)
	{
		onPowerupActivated -= handler;
	}

	private static void notifyPowerup(PowerupBase powerup)
	{
		if (onPowerupActivated != null)
		{
			onPowerupActivated(powerup);
		}
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	public static int sortPowerupsByTime(PowerupBase p1, PowerupBase p2)
	{
		//If for some reason p1 or p2 are null just return -1
		//Fixes HIR-88337 an NRE as seen on bugsnag
		if (p1 == null || p2 == null)
		{
			return -1;
		}
		if (p1.runningTimer != null && p2.runningTimer != null)
		{
			if (p1.runningTimer.timeRemaining == p2.runningTimer.timeRemaining)
			{
				return (int)p2.rarity - (int)p1.rarity;
			}
			return p1.runningTimer.timeRemaining.CompareTo(p2.runningTimer.timeRemaining);
		}

		if (p1.runningTimer == null && p2.runningTimer != null)
		{
			return 1;
		}

		if (p1.runningTimer != null && p2.runningTimer == null)
		{
			return -1;
		}

		return (int)p1.rarity - (int)p2.rarity;
	}

	public static bool hasAnyPowerupsToDisplay()
	{
		if (activePowerups == null || activePowerups.Count == 0)
		{
			return false;
		}

		bool result = false;
		
		for (int i = 0; i < activePowerups.Count; i++)
		{
			if (activePowerups[i].isDisplayablePowerup)
			{
				result = true;
				break;
			}
		}

		return result;
	}

	public static bool hasActivePowerupByName(string name)
	{
		return getActivePowerup(name) != null;
	}

	public static PowerupBase getActivePowerup(string name)
	{
		for (int i = 0;	i < activePowerups.Count; i++)
		{
			if (activePowerups[i].name == name)
			{
				return activePowerups[i];
			}
		}

		return null;
	}

	public static void overridePowerupTimers(int timeRemaining)
	{
#if UNITY_EDITOR
		for (int i = 0; i < activePowerups.Count; ++i)
		{
			updatePowerupTimer(activePowerups[i], timeRemaining);
		}
#endif
	}

	public static void updatePowerupTimer(PowerupBase powerup, int timeRemaining)
	{
#if UNITY_EDITOR
		powerup.apply(powerup.duration, timeRemaining);
#endif
	}

	public static bool isPowerupStreakActive
	{
		get
		{
			int streakEligible = 0;
			if (activePowerups != null)
			{
				for (int i = 0; i < activePowerups.Count; i++)
				{
					if (activePowerups[i].isDisplayablePowerup)
					{
						streakEligible++;
					}
				}
			}
			
			return streakEligible >= STREAK_THRESHOLD;
		}
	}

	public static bool isPowerupsEnabled
	{
		get { return ExperimentWrapper.Powerups.isInExperiment; }
	}

	public static void preLoadPrefabs()
	{
		AssetBundleManager.downloadAndCacheBundle("powerups", true, blockingLoadingScreen:false);

		for (int i = 0; i < PowerupBase.powerups.Count; ++i)
		{
			PowerupBase powerup = PowerupBase.powerups[i];
			powerup.getPrefab();
		}
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		activePowerups = new List<PowerupBase>();
		RewardablesManager.removeEventHandler(onRewardGranted);
		onPowerupActivated = null;
		powerupsStaticDurations = new Dictionary<string, int>();
	}
}