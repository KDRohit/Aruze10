using System.Collections.Generic;
using UnityEngine;

namespace PrizePop
{
	public class PrizePopAction : ServerAction
	{
		//Actions
		private const string SPEND_PICK = "prize_pop_spend_pick";
		private const string START_BONUS_GAME = "prize_pop_start_bonus_game";
		private const string GET_CURRENT_JACKPOT = "prize_pop_get_current_jackpot";
		
		//Dev Actions
		private const string DEV_ADD_PICKS = "prize_pop_add_picks";
		private const string DEV_ADD_METER_PROGRESS = "prize_pop_add_meter";
		private const string DEV_CLEAR_EXTRA_PICKS = "prize_pop_clear_picks";
		private const string DEV_INITIALIZE = "prize_pop_initialize"; //Resets the feature to be like player is entering or the 1st time
		private const string DEV_RESET_GAME = "prize_pop_reset_game"; //Resets bonus game to round 1 but reuses same outcome as before
		private const string DEV_RESET_METER = "prize_pop_reset_meter";
		private const string DEV_RESET_ROUND = "prize_pop_reset_round";
		private const string DEV_SET_ECONOMY_VERSION = "prize_pop_set_economy_version";
		private const string DEV_START_BONUS_GAME = "prize_pop_start_bonus_game_dev";
		
		//Property Names
		private const string OBJECT_INDEX = "object_index";
		private const string PICKS = "picks";
		private const string ECONOMY_VERSION = "economy_version";
		private const string FORCE_BONUS = "force";
		private const string METER_POINTS = "points";
		private const string FORCE_START_TIME = "force_start_time";
		private const string FORCE_END_TIME = "force_end_time";
		private const string FORCE_VARIANT_VERSION = "force_variant_version";
		private const string FORCE_RESET = "reset";

		//Action Variables
		private int objectIndex = 0;
		private int picks = 0;
		private int economyVersion = 0;
		private bool forceBonus = false;
		private int meterPoints = 0;
		private int startTime = 0;
		private int endTime = 0;
		private string variantVersion = "";
		private bool forceReset = false;

		/** Constructor */
		private PrizePopAction(ActionPriority priority, string type) : base(priority, type) { }
		
		public static Dictionary<string, string[]> propertiesLookup
		{
			get
			{
				if (_propertiesLookup == null)
				{
					_propertiesLookup = new Dictionary<string, string[]>();
					_propertiesLookup.Add(SPEND_PICK, new string[] { OBJECT_INDEX });
					_propertiesLookup.Add(START_BONUS_GAME, new string[] {});
					_propertiesLookup.Add(GET_CURRENT_JACKPOT, new string[] {});
					_propertiesLookup.Add(DEV_ADD_PICKS, new string[] { PICKS });
					_propertiesLookup.Add(DEV_ADD_METER_PROGRESS, new string[] { METER_POINTS });
					_propertiesLookup.Add(DEV_CLEAR_EXTRA_PICKS, new string[] {});
					_propertiesLookup.Add(DEV_INITIALIZE, new string[] { FORCE_START_TIME, FORCE_END_TIME, FORCE_VARIANT_VERSION, FORCE_RESET });
					_propertiesLookup.Add(DEV_RESET_GAME, new string[] {});
					_propertiesLookup.Add(DEV_RESET_METER, new string[] {});
					_propertiesLookup.Add(DEV_RESET_ROUND, new string[] {});
					_propertiesLookup.Add(DEV_SET_ECONOMY_VERSION, new string[] { ECONOMY_VERSION });
					_propertiesLookup.Add(DEV_START_BONUS_GAME, new string[] { FORCE_BONUS });
				}
				return _propertiesLookup;
			}
		}
		
		private static Dictionary<string, string[]> _propertiesLookup = null;

		public static void spendPick(int index)
		{
			PrizePopAction action = new PrizePopAction(ActionPriority.IMMEDIATE, SPEND_PICK);
			action.objectIndex = index;
			processPendingActions();
		}

		public static void startBonusGame()
		{
			PrizePopAction action = new PrizePopAction(ActionPriority.IMMEDIATE, START_BONUS_GAME);
			processPendingActions();
		}

		public static void getCurrentJackpot()
		{
			PrizePopAction action = new PrizePopAction(ActionPriority.IMMEDIATE, GET_CURRENT_JACKPOT);
			processPendingActions();
		}
		
		public static void devAddPicks(int picksToAdd)
		{
			PrizePopAction action = new PrizePopAction(ActionPriority.IMMEDIATE, DEV_ADD_PICKS);
			action.picks = picksToAdd;
			processPendingActions();
		}
		
		public static void devAddMeterProgress(int amount)
		{
			PrizePopAction action = new PrizePopAction(ActionPriority.IMMEDIATE, DEV_ADD_METER_PROGRESS);
			action.meterPoints = amount;
			processPendingActions();
		}
		
		public static void devClearExtraPicks()
		{
			PrizePopAction action = new PrizePopAction(ActionPriority.IMMEDIATE, DEV_CLEAR_EXTRA_PICKS);
			processPendingActions();
		}
		
		public static void devInitialize(int startTime = -1, int endTime = -1, string variant = "", bool reset = false)
		{
			PrizePopAction action = new PrizePopAction(ActionPriority.IMMEDIATE, DEV_INITIALIZE);
			action.startTime = startTime;
			action.endTime = endTime;
			action.variantVersion = variant;
			action.forceReset = reset;
			processPendingActions();
		}
		
		public static void devResetBonusGame()
		{
			PrizePopAction action = new PrizePopAction(ActionPriority.IMMEDIATE, DEV_RESET_GAME);
			processPendingActions();
		}
		
		public static void devResetMeter()
		{
			PrizePopAction action = new PrizePopAction(ActionPriority.IMMEDIATE, DEV_RESET_METER);
			processPendingActions();
		}
		
		public static void devResetRound()
		{
			PrizePopAction action = new PrizePopAction(ActionPriority.IMMEDIATE, DEV_RESET_ROUND);
			processPendingActions();
		}
		
		public static void devSetEconomyVersion(int version)
		{
			PrizePopAction action = new PrizePopAction(ActionPriority.IMMEDIATE, DEV_SET_ECONOMY_VERSION);
			action.economyVersion = version;
			processPendingActions();
		}
		
		public static void devStartBonusGame(bool force)
		{
			PrizePopAction action = new PrizePopAction(ActionPriority.IMMEDIATE, DEV_START_BONUS_GAME);
			action.forceBonus = force;
			processPendingActions();
		}
		
		/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
		new public static void resetStaticClassData()
		{
			_propertiesLookup = null;
		}
		
		public override void appendSpecificJSON(System.Text.StringBuilder builder)
		{
			if (!propertiesLookup.ContainsKey(type))
			{
				Debug.LogError("No properties defined for action: " + type);
				return;
			}

			foreach (string property in propertiesLookup[type])
			{
				switch (property)
				{
					case OBJECT_INDEX:
						appendPropertyJSON(builder, property, objectIndex);
						break;
					case PICKS:
						appendPropertyJSON(builder, property, picks);
						break;
					case ECONOMY_VERSION:
						appendPropertyJSON(builder, property, economyVersion);
						break;
					case FORCE_BONUS:
						appendPropertyJSON(builder, property, forceBonus);
						break;
					case METER_POINTS:
						appendPropertyJSON(builder, property, meterPoints);
						break;
					case FORCE_START_TIME:
						if (startTime >= 0)
						{
							appendPropertyJSON(builder, property, startTime);
						}
						break;
					case FORCE_END_TIME:
						if (endTime >= 0)
						{
							appendPropertyJSON(builder, property, endTime);
						}
						break;
					case FORCE_VARIANT_VERSION:
						if (!string.IsNullOrEmpty(variantVersion))
						{
							appendPropertyJSON(builder, property, variantVersion);
						}
						break;
					case FORCE_RESET:
						appendPropertyJSON(builder, property, forceReset);
						break;
					default:
						Debug.LogWarning($"Unknown property for action={type} {property}");
						break;
				}
			}
		}
	}	
}

