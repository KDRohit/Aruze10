using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Com.HitItRich.Feature.TimedBonus
{
	public class DailyBonusData : TimedBonusData
	{
		public const int MAX_DAYS = 7; //Max days to hit the multiplier streak
		
		//default fields
		public long vipBonusCredits;
		public string vipBonusPercentText;
		public long weeklyRaceBonus;
		public string weeklyRaceBonusPercentText;
		public long friendsBonusCredits;
		public long bonusDailyStreakCredits;

		//boost powerup fields
		public int boostStartTime;
		public int boostEndTime;
		public int boostFrequency;

		public DailyBonusData(JSON outcome)
		{
			if (outcome == null)
			{
				return;
			}

			vipBonusCredits = outcome.getLong("vip_level_bonus", 0);
			vipBonusPercentText = outcome.getString("vip_level_bonus_percentage", "0");
			friendsBonusCredits = outcome.getLong("active_friend_bonus", 0);
			bonusDailyStreakCredits = outcome.getLong("player_level_bonus", 0);
			weeklyRaceBonus = outcome.getInt("weekly_race_bonus", 0);
			weeklyRaceBonusPercentText = outcome.getString("weekly_race_bonus_percentage", "0");
			nextCollectTime = outcome.getInt("next_collect", 0);
			claimTime = GameTimer.currentTime;
			
			JSON[] outcomesJSON = outcome.getJsonArray("outcomes"); //Array with the wheel outcomes
			if (outcomesJSON != null && outcomesJSON.Length > 0)
			{
				if (outcomesJSON.Length > 1)
				{
					Debug.LogError("Server is sending us too many wheel outcomes.");
				}
				if (outcomesJSON[0] != null)
				{
					selectedWinId = getWinIdFromDailyBonusOutcomes(outcomesJSON);
					winIdRewardMap = new Dictionary<string, long>();
					
					SlotOutcome actualWheelOutcome = new SlotOutcome(outcomesJSON[0]); //Turn the first wheel outcome JSON into our SlotOutcome wrapper
					JSON paytable = actualWheelOutcome.getBonusGamePayTable();
					if (paytable != null)
					{
						JSON[] paytableRounds = paytable.getJsonArray("rounds");
						if (paytableRounds != null && paytableRounds.Length > 0 && paytableRounds[0] != null)
						{
							JSON[] wins = paytableRounds[0].getJsonArray("wins"); //Get the paytable info so we can figure out the slice to stop on and how to set up the wheel slices
							if (wins != null && wins.Length > 0)
							{
								List<string> allWinIds = new List<string>(wins.Length);
								for (int i = 0; i < wins.Length; i++)
								{
									string id = wins[i].getInt("id", -1).ToString();
									if (id != "-1")
									{
										winIdRewardMap[id] = wins[i].getLong("credits", 0L);
										allWinIds.Add(id);
									}
								}
								orderedWinIds = new ReadOnlyCollection<string>(allWinIds);
							}
						}
					}
				}
			}
			
			if (outcome != null && outcome.getJSON("daily_bonus_reduced_timer_buff") != null && !PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_DAILY_BONUS_KEY))
			{
				JSON boostEvent = outcome.getJSON("daily_bonus_reduced_timer_buff");
				if (boostEvent != null)
				{
					boostStartTime = boostEvent.getInt("start_time", 0);
					boostEndTime = boostEvent.getInt("end_time", 0);
					boostFrequency = boostEvent.getInt("frequency", 30);
				}
			}
		}

		public bool hasBoost
		{
			get
			{
				return boostStartTime > 0 && boostEndTime > 0;
			}
		}
		
		public override long totalWin
		{
			get
			{
				long win = 0L;
				if (winIdRewardMap.TryGetValue(selectedWinId, out win))
				{
					long totalCredits = vipBonusCredits + friendsBonusCredits + bonusDailyStreakCredits + win + weeklyRaceBonus;
					int currentDay = DailyBonusGameTimer.instance != null ? DailyBonusGameTimer.instance.day : 0;
					if (currentDay >= MAX_DAYS)
					{
						totalCredits *= Glb.ENGAGEMENT_REWARD_MULTIPLIER;
					}

					return totalCredits;
				}

				return 0;
			}
		}
		
		private string getWinIdFromDailyBonusOutcomes(JSON[] outcomesJSON)
		{
			if (outcomesJSON == null || outcomesJSON.Length == 0)
			{
				return null;
			}
		
			string winId = outcomesJSON[0].getInt("round_1_stop_id", -1).ToString(); //server is sending us the win ID for the current wheel outside of the nested wheel outcome

			if (winId != "-1") //If we could find the winId at the top level then we'll go into the nested wheel outcome for the wheel ID
			{
				JSON[] subWheelOutcomes = outcomesJSON[0].getJsonArray("outcomes");
				if (subWheelOutcomes != null && subWheelOutcomes.Length > 0 && subWheelOutcomes[0] != null)
				{
					winId = subWheelOutcomes[0].getInt("win_id", -1).ToString();
				}
			}

			return winId;
		}
	}
}

