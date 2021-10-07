/**
 * Buff Feature Implementation - SIR-
 * 
 * 
 */ 

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Zynga.Core.Util;

public class Buff : IResetGame 
{

	// Events
	private const string BUFF_APPLIED_EVENT_NAME = "buff_applied";

	public GameTimerRange activeGameTimerRange { get; private set; }		// Current active timer range
	public BuffDef activeDef { get; private set; } // Current active def
	public string baseType { get; private set; } // base type
	public delegate void onDeactivateDelegate(BuffDef buffDef);
	public static event onDeactivateDelegate buffDeactivateEvent;
	public static bool isPlayerBuffsApplied;

	// Buffs that are on activation
	private Dictionary<GameTimerRange, BuffDef> gameTimers = new Dictionary<GameTimerRange, BuffDef>();
	private List<KeyValuePair<GameTimerRange, BuffDef>> sortedGameTimers = new List<KeyValuePair<GameTimerRange, BuffDef>>();

	// The buffs data keyed by buff type
	private static Dictionary<BuffType, Buff> buffs = new Dictionary<BuffType, Buff>();
	private static CustomLog.Tag logTag = new CustomLog.Tag("buffs", Color.cyan);

	// Returns true if a Buff is activated
	public bool isActivated
	{
		get 
		{
			return activeGameTimerRange != null && activeDef != null;
		}
	}

	// Returns value of an activated buff, 0 otherwise
	public int value
	{
		get 
		{
			return (isActivated) ? activeDef.value : 0;
		}
	}

	// Returns end timestamp of an activated buff, 0 otherwise
	public int endTimestamp
	{
		get 
		{
			return (isActivated) ? activeGameTimerRange.endTimestamp : 0;
		}
	}

	/**
	 * Add a game timer to the game timers list and activate the next buff in the list
	 * Base classes need to override for updating the activated state
	 */
	public bool addGameTimerAndActivateNext(BuffDef buffDef, int newEndTimestamp)
	{
		int nowInSecs = GameTimer.currentTime;
		return this.addGameTimerAndActivateNext(buffDef, nowInSecs, newEndTimestamp);
	}

	/**
	 * Add a game timer to the game timers list and activate the next buff in the list
	 * Base classes need to override for updating the activated state
	 */
	public virtual bool addGameTimerAndActivateNext(BuffDef buffDef, int newStartTimestamp, int newEndTimestamp)
	{
		GameTimerRange newGameTimerRange = new GameTimerRange(
						(int) newStartTimestamp,
						(int) newEndTimestamp,
						true
					);
		addGameTimer(newGameTimerRange, buffDef);

		bool canActivate = newGameTimerRange.isActive;

		if (isActivated)
		{
			canActivate = canActivate && (compare(buffDef, newEndTimestamp, activeDef, activeGameTimerRange.endTimestamp) > 0);
		}

		if (canActivate)
		{
			activateNext();
		}

		log("Buff.addGameTimerAndActivateNext buffDef key:{0}\ntype:{1},\nnewGameTimerRange.isActive:{2},\nisActivated:{3}\n(activeGameTimerRange == newGameTimerRange):{4},\n(activeDef == buffDef):{5}", 
								buffDef.keyName,
								buffDef.type.keyName,
								newGameTimerRange.isActive,
								isActivated,
								(activeGameTimerRange == newGameTimerRange),
								(activeDef == buffDef));

		return isActivated && (activeGameTimerRange == newGameTimerRange) && (activeDef == buffDef);
	}

	/**
	 * Deactivates the current active Buff timer
	 */
	public virtual bool deactivate(GameTimerRange gameTimerRange)
	{
		bool shouldDeactivate = isActivated && (gameTimerRange == activeGameTimerRange);
		if (shouldDeactivate)
		{
			if (buffDeactivateEvent != null)
			{
				buffDeactivateEvent(activeDef);
			}
			activeGameTimerRange = null;
			activeDef = null;
		}

		removeGameTimer(gameTimerRange);

		log("Buff.deactivate shouldDeactivate:{0} gameTimerRange:{1}", shouldDeactivate, gameTimerRange.endTimestamp);

		return shouldDeactivate;
	}

	public static void removeDeactivateDelegate(onDeactivateDelegate function)
	{
		buffDeactivateEvent -= function;
	}

	public static void registerDeactivateDelegate(onDeactivateDelegate function)
	{
		// prevent duplicates
		buffDeactivateEvent -= function;
		buffDeactivateEvent += function;
	}

	/**
	 * Init the buffs. Invoked once during game loading or after reset.
	 */
	public static void init(JSON[] playerBuffsDataJSON)
	{
		RoutineRunner.instance.StartCoroutine(applyPlayerLoginBuffs(playerBuffsDataJSON));
	}

	public static void registerEventDelegates()
	{
		Server.registerEventDelegate(BUFF_APPLIED_EVENT_NAME, onPlayerBuffApplied, true);
	}

	/**
	 * Return the list of active buff defs
	 */
	public static List<BuffDef> findActiveBuffDefs()
	{
		List<BuffDef> activeBuffDefs = new List<BuffDef>();
		foreach (KeyValuePair<BuffType, Buff> entry in buffs)
		{
			Buff buff = entry.Value;
			if (buff.isActivated)
			{
				activeBuffDefs.Add(buff.activeDef);
			}
		}
		return activeBuffDefs;
	}

	/**
	 * Returns the buff for a given buff base type
	 */
	public static Buff find(BuffType buffType)
	{
		Buff buff = null;
		if (!buffs.TryGetValue(buffType, out buff))
		{
			log("Buff.find(), missing buff for buffType:{0}", buffType.keyName);
		}
		return buff; 
	}

	public static void log(string s, params object[] args)
	{
		CustomLog.Log.log(string.Format(s, args), logTag);
	}
	public static void logColor(string s, Color color, params object[] args)
	{
		CustomLog.Log.log(string.Format(s, args), color, "buffs");
	}

	/**
	 * Compare first and second and return 1 if first is better, 0 if equals, -1 otherwise
	 * A tie is resolved by comparing the endTimestamp
	 * Usually a higher value is "better"
	 * Certain buffs types, for which a lower value is "better" should override this method
	 * Subclasses need to override if required
	 */
	public virtual int compare(KeyValuePair<GameTimerRange, BuffDef> first, KeyValuePair<GameTimerRange, BuffDef> second)
	{
		if (first.Key.isActive && second.Key.isActive)
		{
			return compare(first.Value, first.Key.endTimestamp, second.Value, second.Key.endTimestamp);
		}
		else if (first.Key.isActive && !second.Key.isActive)
		{
			return 1;
		}
		else if (!first.Key.isActive && second.Key.isActive)
		{
			return -1;
		}
		return 0;
	}

	/**
	 * Compare first and second and return 1 if first is better, 0 if equals, -1 otherwise
	 * A tie is resolved by comparing the duration
	 * Usually a higher value is "better"
	 * Certain buffs types, for which a lower value is "better" should override this method
	 * Subclasses need to override if required
	 */
	public virtual int compare(BuffDef firstDef, int firstEndTimestamp, BuffDef secondDef, int secondEndTimestamp)
	{
		// Compare the BuffDef "value" fields
		int result = firstDef.value.CompareTo(secondDef.value) * (isLowerValueBetter ? -1 : 1);

		if (result == 0)
		{
			// tie breaker
			// Compare the GameTimerRange "duration" fields
			result = firstDef.duration.CompareTo(secondDef.duration);
		}
		return result;
	}

	/**
	 * Get or create a buff from the buffs Dictionary
	 */
	public static Buff getOrCreateBuff(BuffType buffType)
	{
		Buff buff = null;
		if(!buffs.TryGetValue(buffType, out buff))
		{
			// doesnt exist, create it
			buff = createBuff(buffType);
		}
		if (buffType != null && buff == null)
		{
			log("Buff.getOrCreateBuff(), null buff for buffType:{0}", buffType.keyName);
		}
		return buff;
	}

	//=========================================================================
	//	HELPER Methods
	//=========================================================================

	private void addGameTimer(GameTimerRange gameTimer, BuffDef buffDef)
	{
		gameTimer.registerFunction(buffTimerExpiredHandler);
		KeyValuePair<GameTimerRange, BuffDef> entry = new KeyValuePair<GameTimerRange, BuffDef>(gameTimer, buffDef);
		ICollection<KeyValuePair<GameTimerRange, BuffDef>> gameTimersCollection = (ICollection<KeyValuePair<GameTimerRange, BuffDef>>) gameTimers;
		gameTimersCollection.Add(entry);
		sortedGameTimers.Add(entry);
		sortedGameTimers.Sort(compare);
	}

	private void removeGameTimer(GameTimerRange gameTimer)
	{
		if (gameTimer != null)
		{
			BuffDef buffDef = null;
			if (gameTimers.ContainsKey(gameTimer))
			{
				buffDef = gameTimers[gameTimer];
				KeyValuePair<GameTimerRange, BuffDef> entry = new KeyValuePair<GameTimerRange, BuffDef>(gameTimer, buffDef);
				ICollection<KeyValuePair<GameTimerRange, BuffDef>> gameTimersCollection = (ICollection<KeyValuePair<GameTimerRange, BuffDef>>) gameTimers;
				gameTimersCollection.Remove(entry);
				sortedGameTimers.Remove(entry);
				sortedGameTimers.Sort(compare);
			}
			gameTimer.removeFunction(buffTimerExpiredHandler);
		}
	}

	private void buffTimerExpiredHandler(Dict args = null, GameTimerRange sender = null)
	{
		log("Buff.buffTimerExpiredHandler timer expired:{0} buff def:{1}",
					sender.endTimestamp,gameTimers[sender].keyName);

		deactivate(sender);

		activateNext();
	}

	private KeyValuePair<GameTimerRange, BuffDef> nextBestInList()
	{
		KeyValuePair<GameTimerRange, BuffDef> best = new KeyValuePair<GameTimerRange, BuffDef>();
		if (gameTimers.Count > 0)
		{
			best = sortedGameTimers[0];
		}
		return best;
	}

	/**
	 * Activates the next timer based on the value
	 * if there is a tie break the later end timestamp wins
	 */
	private void activateNext()
	{
		if (gameTimers.Count > 0)
		{
			KeyValuePair<GameTimerRange, BuffDef> best = nextBestInList();
			activeGameTimerRange = best.Key;
			activeDef = best.Value;
		}
		log("Buff.activateNext activeGameTimerRange:{0}, activeDef:{1}", 
			(activeGameTimerRange == null ? 0 : activeGameTimerRange.endTimestamp), 
			(activeDef == null ? "<null>" : activeDef.keyName));
	}

	/** 
	 * Event handler BUFF_APPLIED_EVENT_NAME when the player successfully applies a Local Buff
	 */
	public static void onPlayerBuffApplied(JSON playerData)
	{
		log("onPlayerBuffApplied event. JSON = \n{0}\n", (playerData == null? "<null>" : playerData.ToString()));
		if (playerData != null)
		{
			JSON data = playerData.getJSON("buff");
			if (data != null)
			{
				applyPlayerBuff(data, true);
			}		
		}
	}

	/**
	 * Helper method to apply available player login JSON data to buffs
	 */
	private static IEnumerator applyPlayerLoginBuffs(JSON[] playerBuffsDataJSON)
	{
		isPlayerBuffsApplied = false;

		if (playerBuffsDataJSON != null)
		{
			if (playerBuffsDataJSON.Length > 0)
			{
				// isStaticBuffDataValid becomes true when the event is recieved from the server
				// we cannot apply buffs until that happens
				while (!BuffDef.isStaticBuffDataValid)
				{
					yield return null;
				}

				//Parse through the JSON Provided and create Buff Classes.
				foreach (JSON data in playerBuffsDataJSON)
				{
					applyPlayerBuff(data);
				}				
			}
		}

		isPlayerBuffsApplied = true;	
	}

	/**
	 * Helper method to apply player JSON data to buff
	 */
	private static void applyPlayerBuff(JSON data, bool trackMileStone = false)
	{
		log("Buff.applyPlayerBuff data:{0}", data.ToString());
		if (data != null)
		{
			string buffDefKey = data.getString("key_name", "");
			BuffDef buffDef = BuffDef.find(buffDefKey);
			if (buffDef != null)
			{
				BuffType buffType = buffDef.type;
				int endTimestamp = data.getInt("end_ts", 0);

				// only apply if timestamp is valid, the server sends down expired buffs that should be ignored
				if (endTimestamp > GameTimer.currentTime)
				{
					buffDef.apply(endTimestamp);

					if (trackMileStone)
					{
						int duration = buffDef.duration/60;
						string mileStone = data.getString("base_type", "");
						if (mileStone.Contains("multiplier"))
						{
							mileStone += "_" + buffDef.value;
						}
						mileStone += "_perk";

						StatsManager.Instance.LogMileStone(mileStone, duration);

						StatsManager.Instance.LogCount(counterName: "perk", kingdom: "buy_page_perk", phylum: mileStone, klass: buffDef.duration.ToString(), family:  buffDef.value.ToString(), genus:  duration.ToString());
				
					}
				}
			}			
		}
	}

	/**
	 * Helper method to create a new instance of the buff from the base type
	 */
	private static Buff createBuff(BuffType buffType)
	{
		Buff buff = null;
		string buffClassName = getBuffClassName(buffType);
		log("Buff.createBuff() creating buff type:{0} using class name:{1}", buffType.keyName, buffClassName);
		System.Type buffClassType = System.Type.GetType(buffClassName);
		if (buffClassType != null)
		{
			buff = System.Activator.CreateInstance(buffClassType) as Buff;
			buff.baseType = buffType.keyName;
			buffs[buffType] = buff;
		}
		else
		{
			log("Buffs.createBuff() - Couldn't find code class for base_type:{0}", buffType.keyName);
		}
		return buff;
	}

	private static string getBuffClassName(BuffType buffType)
	{
		return "Modify" + CommonText.snakeCaseToPascalCase(buffType.keyName) + "Buff";
	}

	// Implements IResetGame
	public static void resetStaticClassData()
	{
		buffs = new Dictionary<BuffType, Buff>();
	}

	protected virtual bool isLowerValueBetter
	{
		get {
			return false;
		}
	}

}
